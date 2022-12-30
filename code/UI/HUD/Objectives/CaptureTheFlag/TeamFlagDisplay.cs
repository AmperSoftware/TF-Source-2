using Sandbox;
using Sandbox.UI;
using Amper.FPS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFS2.UI;

[UseTemplate]
partial class TeamFlagDisplay : Panel
{
	Dictionary<Flag, TeamFlagCompass> Flags { get; set; } = new();
	Panel ZoneCompassContainer { get; set; }
	Panel FlagCompassContainer { get; set; }
	Label Limit { get; set; }
	Label ScoreBlue { get; set; }
	Label ScoreRed { get; set; }
	TeamFlagCompass ZoneCompass { get; set; }
	bool IsOutlineShown { get; set; }
	TimeSince TimeSinceZoneSetup { get; set; }

	public override void Tick()
	{
		SetClass( "visible", ShouldDraw() );

		if ( !IsVisible )
			return;

		TFGameRules.Current.FlagCaptures.TryGetValue( TFTeam.Red, out int redScore );
		ScoreRed.Text = redScore.ToString();

		TFGameRules.Current.FlagCaptures.TryGetValue( TFTeam.Blue, out int blueScore );
		ScoreBlue.Text = blueScore.ToString();

		Limit.Text = $"Playing to: {TFGameRules.tf_flag_caps_per_round}";

		var allFlags = Entity.All.OfType<Flag>();
		var ourFlags = Flags.Keys;

		foreach ( var item in allFlags.Except( ourFlags ) ) AddFlag( item );
		foreach ( var item in ourFlags.Except( allFlags ) ) RemoveFlag( item );

		// Local Flag
		var hasFlag = TryGetLocalPlayerPickedFlag( out var flag );
		if ( hasFlag )
		{
			var sameFlag = ZoneCompass != null && ZoneCompass.Target != flag;
			if ( !sameFlag )
				SetupZoneCompass( flag );
		}
		else
		{
			if ( ZoneCompass != null )
				DestroyZoneCompass();
		}

		// Outline
		if ( TimeSinceZoneSetup >= 2 && IsOutlineShown )
		{
			SetClass( "show_outline", false );
			IsOutlineShown = false;
		}
		else if ( TimeSinceZoneSetup < 2 && !IsOutlineShown )
		{
			SetClass( "show_outline", true );
			IsOutlineShown = true;
		}

		SetClass( "has_flag", hasFlag );
	}

	public bool TryGetLocalPlayerPickedFlag( out Flag flag )
	{
		if ( Game.LocalPawn is TFPlayer player )
		{
			if ( player.PickedItem is Flag ent )
			{
				flag = ent;
				return true;
			}
		}

		flag = null;
		return false;
	}

	public void SetupZoneCompass( Flag flag )
	{
		DestroyZoneCompass();
		SetClass( "has_flag_red", flag.Team == TFTeam.Red );
		SetClass( "has_flag_blue", flag.Team == TFTeam.Blue );

		if ( Game.LocalPawn is TFPlayer player )
		{
			var zone = Entity.All.OfType<FlagCaptureZone>().Where( x => x.Team == player.Team ).FirstOrDefault();
			if ( zone != null )
			{
				ZoneCompass = new TeamFlagCompass
				{
					Target = zone,
					Parent = ZoneCompassContainer
				};
			}
		}

		TimeSinceZoneSetup = 0;
	}

	public void DestroyZoneCompass()
	{
		ZoneCompass?.Delete( true );
		ZoneCompass = null;
	}

	public bool ShouldDraw() => TFGameRules.Current.MapHasFlags;

	public void AddFlag( Flag flag )
	{
		Flags[flag] = new TeamFlagCompass
		{
			Target = flag,
			Parent = FlagCompassContainer
		};

		Reorder();
	}

	public void RemoveFlag( Flag flag )
	{
		if ( Flags.TryGetValue( flag, out var row ) )
		{
			row?.Delete();
			Flags.Remove( flag );
		}

		Reorder();
	}

	public void Reorder()
	{
		FlagCompassContainer.SortChildren( ( Panel x, Panel y ) =>
		{
			if ( x is not TeamFlagCompass x1 || y is not TeamFlagCompass y1 )
				return 0;

			if ( x1.Team == y1.Team )
				return 0;

			var sX = -1;
			var sY = -1;

			// blue gray red
			sX = x1.Team switch
			{
				TFTeam.Blue => 0,
				TFTeam.Red => 2,
				_ => 1,
			};
			sY = y1.Team switch
			{
				TFTeam.Blue => 0,
				TFTeam.Red => 2,
				_ => 1,
			};

			int diff = sY - sX;
			var r = diff / Math.Abs( diff );
			return -r;
		} );
	}
}

partial class TeamFlagCompass : Panel
{
	public Entity Target { get; set; }
	Flag Flag => Target as Flag;
	public TFTeam Team => (TFTeam)((ITeam)Target).TeamNumber;
	Panel Compass { get; set; }
	Panel Briefcase { get; set; }
	Panel Status { get; set; }
	Panel Outline { get; set; }

	public TeamFlagCompass()
	{
		BindClass( "red", () => Team == TFTeam.Red );
		BindClass( "blue", () => Team == TFTeam.Blue );
		BindClass( "hide_briefcase", () => Target is FlagCaptureZone );

		Compass = Add.Panel( "compass" );
		Briefcase = Add.Panel( "briefcase" );
		Status = Add.Panel( "status" );
	}

	public override void Tick()
	{
		if ( !IsVisible )
			return;

		if ( Sandbox.Game.LocalPawn is not TFPlayer pawn )
			return;

		// rotation
		var vecFromEyes = pawn.EyeRotation.Forward.WithZ( 0 ).Normal;
		var vecToOrigin = (Target.Position - pawn.EyePosition).WithZ( 0 ).Normal;

		float radFromEyes = MathF.Atan2( vecFromEyes.x, vecFromEyes.y );
		float radToOrigin = MathF.Atan2( vecToOrigin.x, vecToOrigin.y );

		var deg = (radToOrigin - radFromEyes).RadianToDegree();
		Compass.Style.Set( "transform", $"rotate({deg}deg)" );

		// Flag
		Status.SetClass( "home", Flag?.State == Flag.FlagState.Home );
		Status.SetClass( "carried", Flag?.State == Flag.FlagState.Carried );
		Status.SetClass( "dropped", Flag?.State == Flag.FlagState.Dropped );
	}
}
