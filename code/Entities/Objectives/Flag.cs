using Sandbox;
using Editor;
using Amper.FPS;

namespace TFS2;

[Library( "tf_item_teamflag", Description = "Flag which can be picked up and captured in a Flag Capture zone" )]
[Title( "Flag" )]
[Category( "Objectives" )]
[Icon( "tour" )]
[EditorModel( "models/flag/briefcase.vmdl" )]
[HammerEntity]
public partial class Flag : Item, ITeam
{
	[Property] public HammerTFTeamOption DefaultTeam { get; set; }
	[Property] public bool StartsDisabled { get; set; }

	[Net] public TFTeam Team { get; set; }
	public int TeamNumber => (int)Team;

	public enum FlagState
	{
		Home,
		Carried,
		Dropped
	}

	/// <summary>
	/// Current item state.
	/// </summary>
	[Net] public FlagState State { get; set; }
	[Net] public bool Disabled { get; set; }
	[Net] public Entity LastOwner { get; set; }
	[Net] public TFPlayer InitialPlayer { get; set; }
	[Net] public bool AllowOwnerPickup { get; set; }
	[Net] public TimeSince TimeSincePickup { get; set; }
	[Net] public TimeSince TimeSinceDropped { get; set; }

	public float OwnerPickupTime => 3;

	public override void Spawn()
	{
		base.Spawn();

		/*
		GlowActive = true;
		GlowColor = Team.GetColor().ToColor();
		GlowDistanceEnd = 4096;
		GlowState = GlowStates.On;
		*/

		// Always transmit so that players always know where it is.
		Transmit = TransmitType.Always;

		SetModel( "models/flag/briefcase.vmdl" );

		Disabled = StartsDisabled;
		StartsDisabled = false;
		AllowOwnerPickup = true;
		EnableShadowInFirstPerson = false;
		Team = DefaultTeam.ToTFTeam();

		StartSpinning();
	}

	public override void Tick()
	{
		base.Tick();

		if ( !AllowOwnerPickup )
		{
			if ( State == FlagState.Dropped && TimeSinceDropped > OwnerPickupTime )
			{
				AllowOwnerPickup = true;
			}
		}

		if ( State == FlagState.Dropped && TimeSinceDropped > ReturnTime ) Return();
	}

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );

		int skin;
		switch ( Team )
		{
			case TFTeam.Red: skin = 0; break;
			case TFTeam.Blue: skin = 1; break;
			default: skin = 2; break;
		}

		SetMaterialGroup( skin );
	}

	public override void StartTouch( Entity other )
	{
		if ( !Game.IsServer )
			return;

		if ( other is TFPlayer player )
		{
			if ( !CanBePickedBy( player ) )
				return;

			if ( !player.CanPickup( this ) )
				return;

			Pickup( player );
		}
	}

	public override bool CanBePickedBy( TFPlayer player )
	{
		if ( Disabled )
			return false;

		if ( ITeam.IsSame( player, this ) )
			return false;

		if ( !TFGameRules.Current.FlagsCanBePickedUp() )
			return false;

		if ( player == LastOwner && !AllowOwnerPickup )
			return false;

		return base.CanBePickedBy( player );
	}

	public override void Pickup( TFPlayer player )
	{
		base.Pickup( player );
		if ( !Game.IsServer ) return;

		// GlowActive = false;
		SetParent( player, "flag", Transform.Zero );

		// TODO: remove spy's disguise

		// TODO: switch to brighter picked up skin

		LastOwner = player;
		AllowOwnerPickup = true;
		if ( InitialPlayer == null ) InitialPlayer = player;

		TimeSincePickup = 0;
		State = FlagState.Carried;

		// Let SDKGame know about this.
		TFGameRules.Current.FlagPickedUp( this, player );

		StopSpinning();
		CreateTrails();
	}

	public void Capture( TFPlayer player, FlagCaptureZone zone )
	{
		if ( !Game.IsServer ) return;

		base.Drop( player, false, false );
		Reset();

		TFGameRules.Current.FlagCaptured( this, player, zone );
	}

	public void StartSpinning()
	{
		CurrentSequence.Name = "spin";
	}

	public void StopSpinning()
	{
		CurrentSequence.Name = "idle";
	}

	public void Reset()
	{
		if ( !Game.IsServer ) return;

		if ( Owner is TFPlayer player )
		{
			Drop( player, false, false );
		}

		Transform = SpawnState;

		State = FlagState.Home;
		InitialPlayer = null;
		AllowOwnerPickup = true;
		LastOwner = null;
		// GlowActive = true;

		StartSpinning();
		DeleteTrails();
	}

	public void Return()
	{
		if ( !Game.IsServer ) return;

		// We can't return if we are being held by someone.
		if ( Owner != null ) return;

		Reset();

		// Let SDKGame know about this.
		TFGameRules.Current.FlagReturned( this );
	}

	public override void Drop( TFPlayer player, bool dropped, bool message )
	{
		if ( !Game.IsServer ) return;
		if ( Disabled ) return;

		base.Drop( player, dropped, message );
		// GlowActive = true;

		var origin = player.Position;
		var target = origin + Vector3.Down * 8000;

		var mins = CollisionBounds.Mins;
		var maxs = CollisionBounds.Maxs;

		var extents = mins.Abs() + maxs.Abs();

		// todo: box

		var tr = Trace.Ray( origin, target )
			.WorldOnly()
			.Run();

		// todo: wiggle

		if ( tr.StartedSolid )
		{
			Log.Info( "Couldn't find a safe place to drop the flag!\nDropping at the player's center!\n" );
			Position = player.WorldSpaceBounds.Center;
		}
		else
		{
			Position = tr.EndPosition + Vector3.Up + DistanceOverGround;
		}

		Rotation = Rotation.Identity;
		State = FlagState.Dropped;
		TimeSinceDropped = 0;
		AllowOwnerPickup = false;

		if ( message )
		{
			// Let SDKGame know about this.
			TFGameRules.Current.FlagDropped( this, player );
		}

		StartSpinning();
		DeleteTrails();
	}

	public override void OnDropped()
	{
		DeleteTrails();
	}

	public override void OnReturned()
	{
		DeleteTrails();
	}

	Particles PapersTrail { get; set; }
	Particles ColorTrail { get; set; }

	[ClientRpc]
	public void CreateTrails()
	{
		DeleteTrails();
		PapersTrail = Particles.Create( "particles/flag_particles/player_intel_papertrail.vpcf", this );

		// create the trail of the team of the current owner.
		if ( Owner is TFPlayer player && player.Team.IsPlayable() )
		{
			ColorTrail = Particles.Create( $"particles/flag_particles/player_intel_trail_{player.Team.GetName()}.vpcf", this );
		}
	}

	[ClientRpc]
	public void DeleteTrails()
	{
		PapersTrail?.Destroy( true );
		ColorTrail?.Destroy( true );
	}
}
