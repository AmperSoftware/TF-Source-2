using Sandbox;
using Editor;
using Amper.FPS;
using System;
using System.Collections.Generic;

namespace TFS2;

[Library( "tf_logic_koth" )]
[Title( "King of the Hill" )]
[Category( "Gamemode" )]
[Icon( "terrain" )]
[EditorSprite( "materials/editor/tf_logic_koth.vmat" )]
[HammerEntity]

public partial class KingOfTheHill : BaseGameLogic
{
	[Property] public float TimerLength { get; set; } = 180;
	[Property] public float ControlPointEnableTime { get; set; } = 30;
	[Property, FGDType( "target_destination" )] public string PointName { get; set; }
	ControlPoint Point { get; set; }

	Dictionary<TFTeam, TFTimer> Timers { get; set; } = new();

	public KingOfTheHill()
	{
		EventDispatcher.Subscribe<ControlPointCapturedEvent>( OnPointCapture, this );
	}

	public override void PostLevelSetup()
	{
		// try to find our previous points
		Point = FindByName( PointName ) as ControlPoint;
		Reset();
	}

	public override void Reset()
	{
		// lock the control point
		if ( Point != null )
		{
			Point.StartLocked = true;
			Point.Lock();
		}

		//
		// Timers
		//

		foreach ( var timer in Timers.Values )
			timer.Delete();

		Timers.Clear();
	}

	public override void Tick()
	{
		if ( CanUnlockPoint() )
			Point.Unlock( 5 );

		foreach ( var pair in Timers )
		{
			var team = pair.Key;
			var timer = pair.Value;

			if ( timer.GetRemainingTime() == 0 )
			{
				TFGameRules.Current.DeclareWinner( team, TFWinReason.AllPointsCaptured );
				break;
			}
		}
	}

	public bool CanUnlockPoint()
	{
		if ( Point == null )
			return false;

		// Point is already unlocked
		if ( !Point.Locked )
			return false;

		// Point is already being unlocked.
		if ( Point.IsBeingUnlocked )
			return false;

		// objectives are disabled.
		if ( !TFGameRules.Current.AreObjectivesActive() )
			return false;

		// there should be at least 1 second delay after round is active.
		float actualEnableTime = MathF.Max( 1, ControlPointEnableTime - 5 );
		return TFGameRules.Current.TimeSinceStateChange > actualEnableTime;
	}

	public override void RoundActivate( RoundActiveEvent args )
	{
		base.RoundActivate( args );

		if ( !Game.IsServer )
			return;

		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( !team.IsPlayable() )
				continue;

			Timers[team] = new()
			{
				Name = $"@{team.GetName()}_koth_timer",
				AbsoluteTime = TimerLength,
				Paused = true,
				OwnerTeam = team
			};
		}
	}

	public void OnPointCapture( ControlPointCapturedEvent args )
	{
		var point = args.Point;
		if ( Point != point )
			return;

		SetTeamTimerActive( args.NewTeam );
	}

	public void SetTeamTimerActive( TFTeam team )
	{
		foreach ( var pair in Timers )
		{
			var timerTeam = pair.Key;
			var timer = pair.Value;

			if ( timerTeam == team ) timer.Start();
			else timer.Pause();
		}
	}
}
