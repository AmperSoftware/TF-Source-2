using Sandbox;
using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using Amper.FPS;

namespace TFS2;

[Library( "tf_control_point", Title = "Control Point", Group = "Objectives" )]
[Title( "Control Point" )]
[Category( "Objectives" )]
[Icon( "my_location" )]
[HammerEntity]
public partial class ControlPoint : BaseTrigger
{
	public new static List<ControlPoint> All = new();

	[Property] public bool StartLocked { get; set; }
	public enum ControlPointOwner
	{
		Neither,
		Red,
		Blue
	}
	[Property, Net] public ControlPointOwner DefaultOwner { get; set; }
	/// <summary>
	/// Name to print on the HUD
	/// </summary>
	[Property, Net] public string PrintName { get; set; }

	/// <summary>
	/// The point that RED are supposed to own to be able to capture this one.
	/// </summary>
	[Property, FGDType( "target_destination" )]
	public string PreviousRedPointName { get; set; }
	/// <summary>
	/// The point that BLUE are supposed to own to be able to capture this one.
	/// </summary>
	[Property, FGDType( "target_destination" )]
	public string PreviousBluePointName { get; set; }

	[Property] public bool CanRedCapture { get; set; } = true;
	[Property] public bool CanBlueCapture { get; set; } = true;
	[Property] public int NumberOfRedToCapture { get; set; } = 1;
	[Property] public int NumberOfBlueToCapture { get; set; } = 1;

	[Property] public float RedSpawnAdjust { get; set; }
	[Property] public float BlueSpawnAdjust { get; set; }

	[Property, Net] public float TimeToCapture { get; set; } = 5;

	[Net] public ControlPoint PreviousRedPoint { get; private set; }
	[Net] public ControlPoint PreviousBluePoint { get; private set; }

	/// <summary>
	/// The team that currently owns this point.
	/// </summary>
	[Net] public TFTeam OwnerTeam { get; set; }

	/// <summary>
	/// The team that currently contests the ownership of this point.
	/// </summary>
	[Net] public TFTeam CapturingTeam { get; set; }
	[Net] IDictionary<TFTeam, int> NumberTouchers { get; set; }
	[Net] IDictionary<TFTeam, int> NumberBlockers { get; set; }
	[Net] public float TimeRemaining { get; protected set; }
	[Net] public bool Blocked { get; protected set; }
	[Net] public bool Locked { get; protected set; }

	public float CapturePercentage => 1 - Math.Clamp( TimeRemaining / TimeToCapture, 0, 1 );
	public bool IsBeingCaptured => CapturingTeam != TFTeam.Unassigned;

	TFTeam TeamInZone { get; set; }
	public bool IsFromStartTouch { get; set; }


	[Event.Entity.PostSpawn]
	public void PostLevelSetup()
	{
		// try to find our previous points
		PreviousRedPoint = FindByName( PreviousRedPointName ) as ControlPoint;
		PreviousBluePoint = FindByName( PreviousBluePointName ) as ControlPoint;
	}

	public ControlPoint()
	{
		All.Add( this );
	}

	protected override void OnDestroy()
	{
		All.Remove( this );
		base.OnDestroy();
	}

	public override void Spawn()
	{
		base.Spawn();

		// Always transmit so that players always know where it is.
		Transmit = TransmitType.Always;
		Reset();
	}

	TimeSince TimeSinceLastThink { get; set; }
	TimeSince TimeSinceLastReduction { get; set; }

	int LastAnnouncerSecond { get; set; }

	[Event.Tick.Server]
	public void CaptureThink()
	{
		if ( TimeSinceLastThink < 0.1f )
			return;

		TimeSinceLastThink = 0;

		//
		// Unlock
		//

		if ( Locked && UnlockTime > 0 && TFGameRules.Current.AreObjectivesActive() )
		{
			float remaining = UnlockTime - Time.Now;

			if ( remaining > 0 )
			{
				int seconds = remaining.CeilToInt();
				if ( seconds != LastAnnouncerSecond )
				{
					LastAnnouncerSecond = seconds;
					TFGameRules.PlaySoundToAll( $"announcer.begins.{seconds}sec", SoundBroadcastChannel.Soundtrack );

				}
			}
			else
			{
				Unlock();
			}

			return;
		}

		// TODO: round check

		// Points aren't allowed to be captured. If we were 
		// being captured, we need to clean up and reset.
		if ( !TFGameRules.Current.PointsMayBeCaptured() )
		{
			if ( IsBeingCaptured ) BreakCapture( false );
			return;
		}

		// get all the players that are touching the point right now.
		var players = TouchingEntities.OfType<TFPlayer>();

		//
		// Reset the data
		//

		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			NumberTouchers[team] = 0;
			NumberBlockers[team] = 0;
		}

		//
		// Calculate players that are touching this point right now.
		//

		foreach ( var player in players )
		{
			// alive players can't capture
			if ( !player.IsAlive ) continue;

			var team = player.Team;
			if ( !team.IsPlayable() ) continue;

			// If a team's not allowed to cap a point, don't count players in it at all
			if ( !TFGameRules.Current.TeamMayCapturePoint( team, this ) )
				continue;

			// if player can't capture, see if maybe they can block it.
			if ( !TFGameRules.Current.PlayerMayCapturePoint( player, this ) )
			{
				// player can block this point.
				if ( TFGameRules.Current.PlayerMayBlockPoint( player, this ) )
				{
					// player can block this point.
					if ( TFGameRules.Current.PlayerMayBlockPoint( player, this ) )
					{
						// if ( NumberBlockers[team] == 0 && NumberTouchers[team] == 0 ) firstTouchers[team] = player;
						TryAwardDefensePoints( player );
						NumberBlockers[team] += TFGameRules.Current.GetCaptureValueForPlayer( player );
					}
				}
				continue;
			}

			// if ( NumberBlockers[team] == 0 && NumberTouchers[team] == 0 ) firstTouchers[team] = player;
			NumberTouchers[team] += TFGameRules.Current.GetCaptureValueForPlayer( player );
		}

		// finding out how many teams are in the zone.
		int iTeamsInZone = 0;
		TeamInZone = TFTeam.Unassigned;

		int iTouchingTeams = 0;
		int iBlockingTeams = 0;
		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( NumberTouchers[team] > 0 )
			{
				iTouchingTeams++;
				TeamInZone = team;
			}

			if ( NumberBlockers[team] > 0 )
			{
				iBlockingTeams++;
			}
		}

		// TeamInZone always contains a team if it's alone in the zone.
		// if there are more touching teams, reset it.
		if ( iTouchingTeams > 1 )
		{
			// so reset this var if it's unassigned.
			TeamInZone = TFTeam.Unassigned;
		}

		iTeamsInZone = iTouchingTeams + iBlockingTeams;

		//
		// The point is being contested, increase / decrease time.
		//

		if ( IsBeingCaptured )
		{
			float flDelta = TimeSinceLastReduction;
			TimeSinceLastReduction = 0;
			// Log.Info( $"Delta: {flDelta}" );

			var numPlayersToCap = GetNumberOfTeamPlayersRequiredToCap( CapturingTeam );
			float reduction = flDelta;

			if ( CaptureModeScalesWithPlayers() )
			{
				// Increase the reduction harmonically (https://en.wikipedia.org/wiki/Harmonic_number)
				for ( int i = 1; i < NumberTouchers[TeamInZone]; i++ )
				{
					reduction += flDelta / (i + 1);
				}

				// divide by capturing team amount
				reduction /= numPlayersToCap;
			}

			// if there are multiple teams in the zone, that means that we're being blocked.
			if ( iTeamsInZone > 1 )
			{
				// if we weren't blocked
				if ( !Blocked )
				{
					// mark ourselves as blocked and play the blocking loop sound.
					Blocked = true;
					PlayCaptureBlockLoopSound();
				}

				// Award points to blockers
				AwardDefensePointsToTouchers();
				return;
			}

			// if we reached here, we are not blocke danymore
			if ( Blocked )
			{
				Blocked = false;
				PlayCaptureNormalLoopSound();
			}

			if ( CapturingTeam == TeamInZone )
			{
				// the only team at the point right now is the one that is capturing the point
				ReduceCaptureTime( reduction );
			}
			else if ( OwnerTeam == TFTeam.Unassigned && TeamInZone != TFTeam.Unassigned )
			{
				// if point doesnt belong to anyone and some other team is on the point, revert the capture time
				IncreaseCaptureTime( reduction );
			}
			else
			{
				// if none of the teams are on the point, or it belongs to someone
				if ( TFGameRules.Current.TeamMayCapturePoint( CapturingTeam, this ) )
				{
					// passively revert the progress
					float flDecrease = TimeToCapture / mp_capdeteriorate_time / numPlayersToCap;
					flDecrease *= flDelta;

					IncreaseCaptureTime( flDecrease );
				}
				else
				{
					// if team can't capture the point anymore, reset to full capture time
					// so we break capture next tick
					TimeRemaining = TimeToCapture;
				}
			}

			if ( TimeRemaining <= 0 )
			{
				FinishCapturing( CapturingTeam );
				return;
			}
			else
			{
				// Avoid issues when multiple players are on the point at the same time when it is enabled
				if ( !IsFromStartTouch )
				{
					if ( TimeRemaining >= TimeToCapture )
					{
						BreakCapture( false );
						return;
					}
				}
			}
		}
		else
		{
			// If there are any teams in the zone that aren't the owner, try to start capping
			if ( iTeamsInZone > 0 )
			{
				foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
				{
					if ( !CanTeamCapture( team ) || OwnerTeam == team )
						continue;

					if ( NumberTouchers[team] == 0 )
						continue;

					StartCapturing( team );
					break;
				}
			}
		}

		/*
		DebugOverlay.Text( WorldSpaceBounds.Center,
			$"NumberTouchers[Red]:  {NumberTouchers[TFTeam.Red]}\n" +
			$"NumberTouchers[Blue]: {NumberTouchers[TFTeam.Blue]}\n" +
			$"NumberBlockers[Red]:  {NumberBlockers[TFTeam.Red]}\n" +
			$"NumberBlockers[Blue]: {NumberBlockers[TFTeam.Blue]}\n\n" +

			$"OwnerTeam:            {OwnerTeam}\n" +
			$"ContestingTeam:       {CapturingTeam}\n" +
			$"TimeRemaining:        {TimeRemaining}\n" +

			$"NextPoint:            {NextPoint}\n" +
			$"TimeToCapture:        {TimeToCapture}\n" +
			$"PreviousPoints:       {PreviousPoints.Count}\n\n" +
			$"Blockers:             {Blockers.Count}\n" );*/
	}

	/// <summary>
	/// Time it takes for a full capture point to deteriorate.
	/// </summary>
	[ConVar.Replicated] public static float mp_capdeteriorate_time { get; set; } = 90;

	public bool IsTouching( Entity entity )
	{
		return TouchingEntities.Contains( entity );
	}

	public bool CanPlayerCapture( TFPlayer player )
	{
		if ( !player.IsAlive )
			return false;

		if ( !TFGameRules.Current.TeamMayCapturePoint( player.Team, this ) )
			return false;

		if ( !TFGameRules.Current.PlayerMayCapturePoint( player, this ) )
			return false;

		return true;
	}

	public void StartCapturing( TFTeam team )
	{
		Game.AssertServer();

		// owner team cannot start contesting this point.
		if ( team == OwnerTeam )
			return;

		switch ( team )
		{
			case TFTeam.Red: OnRedStartedCapture.Fire( this ); break;
			case TFTeam.Blue: OnBlueStartedCapture.Fire( this ); break;
		}

		OnStartedCapture.Fire( this );
		CapturingTeam = team;
		TimeRemaining = TimeToCapture;
		TimeSinceLastReduction = 0;

		Blocked = false;
		PlayCaptureNormalLoopSound();
	}

	public void FinishCapturing( TFTeam team )
	{
		Game.AssertServer();

		switch ( team )
		{
			case TFTeam.Red: OnRedCaptured.Fire( this ); break;
			case TFTeam.Blue: OnBlueCaptured.Fire( this ); break;
		}

		var lastTeam = OwnerTeam;

		SetOwnerTeam( team );
		CapturingTeam = TFTeam.Unassigned;
		TimeRemaining = 0;

		var cappers = TouchingEntities.OfType<TFPlayer>().Where( x => x.Team == team ).Select( x => x.Client ).ToArray();

		// Let SDKGame know about this.
		TFGameRules.Current.ControlPointCaptured( this, lastTeam, team, cappers );

		PlayCaptureFinishSound();
	}

	public void BreakCapture( bool bNotEnoughPlayers )
	{
		Game.AssertServer();

		if ( !IsBeingCaptured )
			return;

		// Remap team to get first game team = 1
		switch ( CapturingTeam )
		{
			case TFTeam.Red: OnRedBrokenCapture.Fire( this ); break;
			case TFTeam.Blue: OnBlueBrokenCapture.Fire( this ); break;
		}

		OnBrokenCapture.Fire( this );
		CapturingTeam = TFTeam.Unassigned;

		TimeRemaining = 0;
		PlayCaptureFinishSound();
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( !Game.IsServer )
			return;

		if ( other is TFPlayer player )
		{
			player.ControlPoint = this;

			// TODO: 
			// Come up with a better system
			// Currently this is so the amount of players in the capture zone is updated instantly.

			IsFromStartTouch = true;
			CaptureThink();
			IsFromStartTouch = false;
		}
	}

	public override void EndTouch( Entity other )
	{
		base.EndTouch( other );

		if ( !Game.IsServer )
			return;

		if ( other is TFPlayer player )
		{
			if ( player.ControlPoint == this )
				player.ControlPoint = null;
		}
	}

	public void ReduceCaptureTime( float delta ) { TimeRemaining -= delta; }
	public void IncreaseCaptureTime( float delta ) { TimeRemaining += delta; }

	public int GetNumberOfTeamPlayersRequiredToCap( TFTeam team )
	{
		switch ( team )
		{
			case TFTeam.Red: return NumberOfRedToCapture;
			case TFTeam.Blue: return NumberOfBlueToCapture;
			default: return 0;
		}
	}

	public bool CanTeamCapture( TFTeam team )
	{
		switch ( team )
		{
			case TFTeam.Red: return CanRedCapture;
			case TFTeam.Blue: return CanBlueCapture;
			default: return false;
		}
	}

	public ControlPoint GetPreviousPointForTeam( TFTeam team )
	{
		switch ( team )
		{
			case TFTeam.Red: return PreviousRedPoint;
			case TFTeam.Blue: return PreviousBluePoint;
			default: return null;
		}
	}

	public ControlPoint GetNextControlPointForTeam( TFTeam team )
	{
		switch ( team )
		{
			case TFTeam.Red: return All.FirstOrDefault( x => x.PreviousRedPoint == this );
			case TFTeam.Blue: return All.FirstOrDefault( x => x.PreviousBluePoint == this );
			default: return null;
		}
	}

	public int GetNumberPlayersInArea( TFTeam team )
	{
		NumberTouchers.TryGetValue( team, out var num );
		return num;
	}
	[Input]
	public void Reset()
	{
		Game.AssertServer();

		CapturingTeam = TFTeam.Unassigned;
		TimeRemaining = 0;
		LastAnnouncerSecond = 0;
		UnlockTime = -1;
		StopLoopingSounds();

		SetOwnerTeam( GetDefaultTeamOwner() );

		SetLocked( StartLocked );
	}

	public TFTeam GetDefaultTeamOwner()
	{
		switch ( DefaultOwner )
		{
			case ControlPointOwner.Red: return TFTeam.Red;
			case ControlPointOwner.Blue: return TFTeam.Blue;
			default: return TFTeam.Unassigned;
		}
	}

	/// <summary>
	/// Sets the owner team and stops current capture progress.
	/// </summary>
	/// <param name="team">The new owner team</param>
	[Input( "SetOwner" )]
	public void SetOwnerTeam( TFTeam team )
	{
		BreakCapture( false );
		HandleRespawnTimeAdjustments( OwnerTeam, team );

		// prevent setting team value to spectator accidentally.
		// spectators can't control this point.
		if ( team == TFTeam.Spectator )
			team = TFTeam.Unassigned;

		if ( team != OwnerTeam )
		{
			switch ( team )
			{
				case TFTeam.Unassigned:
					OnOwnerReset.Fire( this );
					break;

				case TFTeam.Red:
					OnOwnerChangedToRed.Fire( this );
					break;

				case TFTeam.Blue:
					OnOwnerChangedToBlue.Fire( this );
					break;
			}
		}

		OwnerTeam = team;
	}
	[Input]
	public void SetLocked( bool locked )
	{
		Game.AssertServer();

		if ( locked ) Lock();
		else Unlock();
	}

	public void Lock()
	{
		Game.AssertServer();

		if ( !Locked )
			OnLocked.Fire( this );

		Locked = true;
	}

	[Net] public float UnlockTime { get; set; }

	/// <summary>
	/// If the point was called to be unlocked after some time, this is going to be true
	/// while timer is still going.
	/// </summary>
	public bool IsBeingUnlocked => UnlockTime > 0;

	public void Unlock( float time = 0 )
	{
		Game.AssertServer();

		if ( time > 0 )
		{
			LastAnnouncerSecond = -1;
			UnlockTime = Time.Now + time;
			return;
		}

		if ( Locked )
		{
			OnUnlocked.Fire( this );

			if ( TFGameRules.Current.AreObjectivesActive() )
			{
				// only play this response if the round is active.
				SDKGame.PlaySoundToAll( "announcer.point.enabled", SoundBroadcastChannel.Announcer );
			}
		}

		UnlockTime = -1;
		Locked = false;
	}

	void AwardDefensePointsToTouchers()
	{
		foreach ( var ply in TouchingEntities.OfType<TFPlayer>().Where( p => p.Team == OwnerTeam ) )
			TryAwardDefensePoints( ply );
	}

	Dictionary<TFPlayer, TimeSince> timeSinceBlock = new();
	const float defensePointResetTime = 30;
	void TryAwardDefensePoints( TFPlayer ply )
	{
		if ( !timeSinceBlock.ContainsKey( ply ) )
			timeSinceBlock.Add( ply, defensePointResetTime + 1 );

		if ( CapturePercentage > 0.5f && timeSinceBlock[ply] >= defensePointResetTime )
		{
			ply.Defenses++;
			timeSinceBlock[ply] = 0;
		}
	}

	void HandleRespawnTimeAdjustments( TFTeam oldTeam, TFTeam newTeam )
	{
		// TODO: Change respawn times on cap
	}

	public bool CaptureModeScalesWithPlayers() => true;

	Sound LoopingSound { get; set; }

	public void StopLoopingSounds()
	{
		LoopingSound.Stop();
	}

	public void PlayCaptureNormalLoopSound()
	{
		// no need to stop looping sound here, PlayCaptureStartSound() already does this.
		PlayCaptureStartSound();
		LoopingSound = Sound.FromEntity( "hologram.move", this );
	}

	public void PlayCaptureBlockLoopSound()
	{
		StopLoopingSounds();
		LoopingSound = Sound.FromEntity( "hologram.malfunction", this );
	}

	public void PlayCaptureFinishSound()
	{
		StopLoopingSounds();
		Sound.FromEntity( "hologram.stop", this );
	}

	public void PlayCaptureStartSound()
	{
		StopLoopingSounds();
		Sound.FromEntity( "hologram.start", this );
	}

	protected Output OnOwnerChangedToRed { get; set; }
	protected Output OnOwnerChangedToBlue { get; set; }
	protected Output OnOwnerReset { get; set; }

	protected Output OnLocked { get; set; }
	protected Output OnUnlocked { get; set; }

	protected Output OnCaptured { get; set; }
	protected Output OnRedCaptured { get; set; }
	protected Output OnBlueCaptured { get; set; }

	protected Output OnStartedCapture { get; set; }
	protected Output OnRedStartedCapture { get; set; }
	protected Output OnBlueStartedCapture { get; set; }

	protected Output OnBrokenCapture { get; set; }
	protected Output OnRedBrokenCapture { get; set; }
	protected Output OnBlueBrokenCapture { get; set; }
}
