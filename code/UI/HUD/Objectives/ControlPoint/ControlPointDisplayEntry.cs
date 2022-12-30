using Sandbox;
using Sandbox.UI;
using System;

namespace TFS2.UI;

[UseTemplate]
partial class ControlPointDisplayEntry : Panel
{
	public ControlPoint Point { get; set; }
	Label CappersCount { get; set; }
	Label Timer { get; set; }
	Panel ProgressArrow { get; set; }
	Panel Pulser { get; set; }
	Panel BlockedPanel { get; set; }
	Panel OwnerProgressPanel { get; set; }
	Panel CapperProgressPanel { get; set; }
	Label PointerMessage { get; set; }

	public bool IsPointLocked()
	{
		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( !team.IsPlayable() )
				continue;

			if ( team == Point.OwnerTeam )
				continue;

			if ( TFGameRules.Current.TeamMayCapturePoint( team, Point ) )
				return false;
		}

		return true;
	}

	public Vector2 GetArrowDirectionForTeam( TFTeam team )
	{
		return team switch
		{
			TFTeam.Red => Vector2.Left,
			TFTeam.Blue => Vector2.Right,
			_ => Vector2.Zero,
		};
	}

	bool IsPulsing { get; set; }
	float PulseTime { get; set; }
	float HalfPulseTime { get; set; }
	TimeSince TimeSincePulseStart { get; set; }
	float LastPulseOpacity { get; set; }

	public void Pulse( float time )
	{
		IsPulsing = true;
		TimeSincePulseStart = 0;
		PulseTime = time;
		HalfPulseTime = time / 2;
	}

	public void UpdatePointer( TFPlayer player )
	{
		var iOwnerTeam = Point.OwnerTeam;
		var iCappingTeam = Point.CapturingTeam;
		var iPlayerTeam = player.Team;
		var bCapBlocked = Point.Blocked;

		PointerMessage.Text = GetPointerText( player );

		if ( !bCapBlocked && iCappingTeam != TFTeam.Unassigned && iCappingTeam != iOwnerTeam && iCappingTeam == iPlayerTeam )
		{
			CapperProgressPanel.SetClass( "red", iCappingTeam == TFTeam.Red );
			CapperProgressPanel.SetClass( "blue", iCappingTeam == TFTeam.Blue );
			CapperProgressPanel.SetClass( "visible", true );

			OwnerProgressPanel.SetClass( "red", iCappingTeam == TFTeam.Red );
			OwnerProgressPanel.SetClass( "blue", iCappingTeam == TFTeam.Blue );
			OwnerProgressPanel.SetClass( "visible", true );

			BlockedPanel.SetClass( "visible", false );
			PointerMessage.SetClass( "visible", false );
		}
		else
		{
			CapperProgressPanel.SetClass( "visible", false );
			OwnerProgressPanel.SetClass( "visible", false );

			BlockedPanel.SetClass( "visible", true );
			PointerMessage.SetClass( "visible", true );

			PointerMessage.Text = GetPointerText( player );
		}
	}

	public string GetPointerText( TFPlayer player )
	{
		var owningTeam = Point.OwnerTeam;
		var cappingTeam = Point.CapturingTeam;
		var playerTeam = player.Team;

		if ( !TFGameRules.Current.PointsMayBeCaptured() )
			return "No capturing at this time!";

		if ( Point.Locked )
			return "No capturing at this time.";

		if ( cappingTeam != TFTeam.Unassigned && cappingTeam != playerTeam )
		{
			if ( Point.Blocked )
				return "Blocking enemy capture!";
			else if ( Point.Blocked )
				return "Reverting capture!";
		}

		if ( owningTeam == playerTeam )
		{
			if ( playerTeam != TFTeam.Unassigned )
			{
				var iEnemyTeam = (playerTeam == TFTeam.Red) ? TFTeam.Blue : TFTeam.Red;
				if ( !TFGameRules.Current.TeamMayCapturePoint( iEnemyTeam, Point ) )
					return "Capture Point already owned.";
			}

			return "Defend this point!";
		}

		if ( !TFGameRules.Current.TeamMayCapturePoint( playerTeam, Point ) )
			return "Preceding point not owned!";

		return "";
	}

	public override void Tick()
	{
		if ( !IsVisible )
			return;

		if ( Sandbox.Game.LocalPawn is TFPlayer player )
		{
			var isPointerVisible = player.ControlPoint == Point;
			SetClass( "show_pointer", isPointerVisible );
			if ( isPointerVisible )
				UpdatePointer( player );
		}

		//
		// Timer
		//
		var showTimer = false;
		if ( Point.IsBeingUnlocked )
		{
			var seconds = (Point.UnlockTime - Time.Now).CeilToInt();
			showTimer = seconds > 0;

			if ( showTimer )
				Timer.Text = seconds.ToString();
		}
		SetClass( "show_timer", showTimer );

		//
		// Lock
		//
		SetClass( "is_locked", IsPointLocked() && !Point.IsBeingUnlocked );

		//
		// Team color
		//
		SetClass( "red", Point.OwnerTeam == TFTeam.Red );
		SetClass( "blue", Point.OwnerTeam == TFTeam.Blue );

		//
		// Players in area count
		//
		var capteam = Point.CapturingTeam;
		int players = Point.GetNumberPlayersInArea( capteam );

		SetClass( "has_cappers", players > 0 );
		SetClass( "has_multiple_cappers", players > 1 );

		// Update if we have players in area
		if ( players > 0 ) CappersCount.Text = players.ToString();

		//
		// Progress Arrow
		//
		var isCapped = Point.IsBeingCaptured;
		SetClass( "is_capping", isCapped );
		if ( isCapped )
		{
			float remainingTime = Point.TimeRemaining;
			float totalTime = Point.TimeToCapture;
			float perc = 1 - Math.Clamp( remainingTime / totalTime, 0, 1 );

			// original size
			float fullsize = -1.35f;
			float delta = fullsize * perc;
			float pos = delta;

			var dir = GetArrowDirectionForTeam( capteam );
			dir *= pos;
			dir += fullsize;
			dir -= 0.01f;

			ProgressArrow.Style.Set( "left", $"{dir.x * 100}%" );
			// ProgressIndicator.Style.Set( "top", $"{dir.y * 100}%" );

			if ( !IsPulsing ) Pulse( .8f );
		}

		//
		// Pulsing 
		//
		if ( IsPulsing )
		{
			float opacity = 0;
			float time = TimeSincePulseStart;

			if ( time <= HalfPulseTime )
				opacity = time.LerpInverse( 0, HalfPulseTime );
			else if ( time <= PulseTime )
				opacity = 1 - time.LerpInverse( HalfPulseTime, PulseTime );
			else
				IsPulsing = false;

			Pulser.Style.Opacity = opacity;
		}
		else
		{
			if ( LastPulseOpacity > 0 )
			{
				Pulser.Style.Opacity = 0;
				LastPulseOpacity = 0;
			}
		}
	}
}
