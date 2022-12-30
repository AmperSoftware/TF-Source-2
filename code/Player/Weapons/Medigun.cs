using Sandbox;
using Amper.FPS;
using System;

namespace TFS2;

[Library( "tf_weapon_medigun", Title = "Medigun" )]
public partial class Medigun : TFHoldWeaponBase //, IPassiveChild
{
	public override bool NeedsAmmo() => false;

	[Net, Predicted] public bool IsHealing { get; set; }
	[Net, Predicted] public TFPlayer Patient { get; set; }
	public bool HasPatient => Patient != null;

	//
	// Attaching
	//

	/// <summary>
	/// This handles the attachment feature of the medigun. Player
	/// can hold the attack key to change their healing target.
	/// </summary>
	public override void SimulateAttack()
	{
		base.SimulateAttack();

		if ( IsHealing )
			SimulateHealing();
	}

	public override void OnHoldStart()
	{
		// Force an attempt to connect to hovered entity.
		NextAttachmentAttemptTime = -1;
		TryAttachToHoveredEntity();
	}

	public override void OnHolding()
	{
		if ( !HasPatient )
			TryAttachToHoveredEntity();
	}

	public override void OnIdling()
	{
		if ( HasPatient && !OwnersWantsAutoHeal() )
			StopHealing();
	}

	public float NextAttachmentAttemptTime { get; set; }
	public float NextAttachErrorTime { get; set; }

	public void TryAttachToHoveredEntity()
	{
		if ( NextAttachmentAttemptTime > Time.Now )
			return;

		NextAttachmentAttemptTime = Time.Now + 0.1f;

		// getting entity we're pointing at (with enabled lag compensation)
		if ( FindHoveredPatientCandidate( out var player ) )
		{
			StartHealing( player );
		}
		else
		{
			StopHealing();

			if ( IsLocalPawn && Time.Now >= NextAttachErrorTime )
			{
				Sound.FromScreen( FailSoundEffect );
				NextAttachErrorTime = Time.Now + 1;
			}
		}
	}

	public void StartHealing( TFPlayer target )
	{
		// Stop whatever connection we already have.
		StopHealing( true );
		Patient = target;
		IsHealing = true;

		// Play animations.
		SendPlayerAnimParameter( "b_fire_hold", true );
		SendViewModelAnimParameter( "b_beam_attach" );
	}

	public void StopHealing( bool silent = false )
	{
		// We're already not healing anyone.
		if ( !IsHealing )
			return;

		// Stop actually healing.
		if ( Patient.IsValid() )
		{
			Patient.StopHealingFrom( this );

			if ( IsReleasingCharge )
				DetachTimes[Patient] = Time.Now;
		}

		Patient = null;
		IsHealing = false;

		// Play animations.
		SendPlayerAnimParameter( "b_fire_hold", false );
		SendViewModelAnimParameter( "b_beam_detach" );
	}

	public bool FindHoveredPatientCandidate( out TFPlayer patient )
	{
		patient = null;

		var tr = TraceFireBullet();
		var hoveredPlayer = tr.Entity as TFPlayer;
		if ( !hoveredPlayer.IsValid() )
			return false;

		if ( !CanConnectToTarget( hoveredPlayer ) )
			return false;

		patient = hoveredPlayer;
		return true;
	}

	public bool CanConnectToTarget( TFPlayer target )
	{
		if ( !IsValidTarget( target ) )
			return false;

		// Should be close enough.
		if ( Vector3.DistanceBetween( target.Position, Owner.Position ) > GetRange() )
			return false;

		// Should be in LOS.
		if ( !IsInLOS( target ) )
			return false;

		return true;
	}

	public bool IsInLOS( TFPlayer target )
	{
		var tr = Trace.Ray( Owner.GetEyePosition(), target.EyePosition )
			.Ignore( this )
			.Ignore( Owner )
			.WithoutTags( CollisionTags.Player )
			.Run();

		return !tr.Hit;
	}

	public bool IsValidTarget( TFPlayer target )
	{
		if ( !target.IsValid() )
			return false;

		// Only can hear ents of our team.
		if ( !ITeam.IsSame( Owner, target ) )
			return false;

		// Can only connect to alive players.
		if ( !target.IsAlive )
			return false;

		return true;
	}

	//
	// Healing
	//

	public float NextConnectionCheckTime { get; set; }

	public void SimulateHealing()
	{
		// Patient stopped being a valid target? Stop healing.
		// (died, changed team, disconnected.)
		if ( !IsValidTarget( Patient ) )
		{
			StopHealing();
			return;
		}

		// Make sure we can persist connection with our patient.
		if ( Time.Now >= NextConnectionCheckTime )
		{
			NextConnectionCheckTime = Time.Now + .5f;
			if ( !CanPersistConnection( Patient ) )
			{
				StopHealing();
				return;
			}
		}

		//
		// Applying healing and charging is not
		// predicted, only calculate it on the server.
		//

		if ( Game.IsServer )
		{
			Patient.Heal( this, GetHealRate(), 1, 1 );

			// Charge up our power if we're not releasing it, and our target
			// isn't receiving any benefit from our healing.
			if ( !IsReleasingCharge )
			{
				BuildCharge();
			}
		}
	}

	public float GetStickRange()
	{
		return GetRange() * 1.2f;
	}

	public bool CanPersistConnection( TFPlayer target )
	{
		// Should be close enough.
		if ( Vector3.DistanceBetween( target.Position, Owner.Position ) > GetStickRange() )
			return false;

		// Should be in LOS.
		if ( !IsInLOS( target ) )
			return false;

		return true;
	}

	public float GetHealRate() => Data.Damage;

	protected override void DebugScreenText( float interval )
	{
		DebugOverlay.ScreenText(
			$"[MEDIGUN]\n" +
			$"Patient:               {Patient}\n" +
			$"IsAttacking:           {IsHolding}\n" +
			$"IsHealing:             {IsHealing}\n" +
			$"" );
	}

	public override void OnHolster( SDKPlayer owner )
	{
		base.OnHolster( owner );

		StopHealing( true );

		HealSound?.Stop();
		HealSound = null;

		MuzzleSound?.Stop();
		MuzzleSound = null;
	}

	public override Trace SetupFireBulletTrace( Vector3 Origin, Vector3 Target )
	{
		return base.SetupFireBulletTrace( Origin, Target )
			.UseHitboxes( false );
	}

	[ConVar.ClientData] public static bool tf_medigun_autoheal { get; set; } = true;
	public bool OwnersWantsAutoHeal()
	{
		return Owner?.Client?.GetClientData( "tf_medigun_autoheal", "1" ).ToBool() ?? false;
	}

	public override bool ShowAmmoOnHud() => false;
}
