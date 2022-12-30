using Sandbox;
using System;
using Amper.FPS;
using System.Linq;

namespace TFS2;

partial class TFPlayer
{
	public const string CritTextParticle = "particles/crit/crit_text.vpcf";
	public const string CritHitSound = "player.crit_hit";
	public const string CritReceivedSound = "player.crit_received";

	public const string MiniCritTextParticle = "particles/crit/minicrit_text.vpcf";
	public const string MiniCritHitSound = "player.crit_hit.mini";

	[ConVar.Replicated] public static bool tf_preround_push_from_damage_enable { get; set; } = true;
	[ConVar.Replicated] public static float tf_damage_force_scale_other { get; set; } = 6;

	public override void ApplyOnPlayerDamageModifyRules( ref ExtendedDamageInfo info )
	{
		base.ApplyOnPlayerDamageModifyRules( ref info );
		var attacker = info.Attacker;

		if( IsInvulnerable())
		{
			// Cant take damage while invulnerable
			info.Damage = 0;
		}

		//
		// If we made a blast jump, provide passive resistance to the self inflicted damage.
		//

		if ( attacker == this && IsInAir && !InWater && info.HasTag( TFDamageFlags.Blast ) )
		{
			var res = PlayerClass.Abilities.BlastJumpDamageMultiplier;
			info.Damage *= res;
		}

		//
		// Ignite ourselves, if this damage has Ignite flag.
		//

		if ( info.HasTag( TFDamageFlags.Ignite ) )
			BurnFromDamage( info );
	}

	public override void ApplyPushFromDamage( ExtendedDamageInfo info )
	{
		if ( info.HasTag( TFDamageFlags.PreventPhysicsForce ) )
			return;

		// Player can't be pushed by damage if they cant move.
		if ( !tf_preround_push_from_damage_enable && !CanMove() )
			return;

		var inflictor = info.Inflictor;
		if ( !inflictor.IsValid() )
			return;

		// Grab the vector of the incoming attack. 
		// (Pretend that the inflictor is a little lower than it really is, so the body will tend to fly upward a bit).
		var direction = WorldSpaceBounds.Center - (inflictor.WorldSpaceBounds.Center + Vector3.Down * 10);
		direction = direction.Normal;

		var damageForForce = info.Damage;
		var hullSize = GetPlayerExtents( IsDucked );
		var forceScale = tf_damage_force_scale_other;

		// Modify the ducked hull size so we propel further.
		if ( IsDucked )
			hullSize.z = 55;
		
		if ( info.Attacker == this )
		{
			if ( info.HasTag( TFDamageFlags.Blast ) )
			{
				forceScale = IsGrounded
					? PlayerClass.Abilities.BlastJumpForceScaleGrounded
					: PlayerClass.Abilities.BlastJumpForceScale;
			}
		}

		var dmgForce = direction * TFGameRules.Current.DamageForce( hullSize, damageForForce, forceScale );

		// Class Push Resistance
		var classPushRes = PlayerClass.Abilities.DamagePushResistance;
		dmgForce *= classPushRes;

		ApplyAbsoluteImpulse( dmgForce );
	}

	public override void OnTakeDamageEffects( Entity attacker, Entity weapon, float damage, string[] tags, Vector3 position,  Vector3 force )
	{
		if ( IsLocalPawn )
		{
			// For the player that has recieved this damage we play recieve 
			// sound, to indicate that they have been hit with a crit.
			if ( tags.Contains( TFDamageFlags.Critical ) )
			{
				Sound.FromScreen( CritReceivedSound );
			}
		}

		if ( Game.LocalPawn == LastAttacker )
		{
			// If local player is the attacker, and we dealt crit damage,
			// play special reaction sounds as well as a particle.

			if ( tags.Contains( TFDamageFlags.Critical ) )
			{
				Particles.Create( CritTextParticle, this, "head" );
				Sound.FromScreen( CritHitSound );
			}
			else if ( tags.Contains( TFDamageFlags.MiniCritical ) )
			{
				Particles.Create( MiniCritTextParticle, this, "head" );
				Sound.FromScreen( MiniCritHitSound );
			}
		}
	}

	public override void ApplyViewPunchFromDamage( ExtendedDamageInfo info )
	{
		var anglePunch = ViewPunchAngle;
		anglePunch.x = -2;
		ViewPunchAngle = anglePunch;
	}

	public const string BloodImpactParticle = "particles/blood_impact/blood_impact_red_01.vpcf";
	public const string BloodWaterImpactParticle = "particles/blood_impact/water_blood_impact_red_01.vpcf";

	public const string BloodSprayParticle = "particles/blood_impact/blood_spray_red_01.vpcf";
	public const string BloodSprayFarParticle = "particles/blood_impact/blood_spray_red_01_far.vpcf";


	public override bool ShouldBleedFromDamage( ExtendedDamageInfo info )
	{
		// Dont bleed from burning.
		if ( info.HasTag( TFDamageFlags.Ignite ) || info.HasTag( TFDamageFlags.Burn ) )
			return false;

		return true;
	}

	public override void DispatchBloodEffects( Vector3 origin, Vector3 normal )
	{
		//
		// Play Blood Impact
		//

		var impactEffect = IsUnderwater 
			? BloodWaterImpactParticle 
			: BloodImpactParticle;

		var bloodImpact = Particles.Create( impactEffect, origin );
		bloodImpact.SetForward( 0, normal );

		//
		// Blood Spray
		//

		// if underwater, don't add additional spray
		if ( IsUnderwater )
			return;
		
		var distance = (origin - Game.LocalPawn.GetEyePosition()).Length;
		var lodDistance = 0.25f * (distance / 512);

		var vecForward = Game.LocalPawn.GetEyeRotation().Forward;
		var vecRight = Game.LocalPawn.GetEyeRotation().Right;
		var dot = normal.Dot( vecForward );

		if ( MathF.Abs( dot ) > 0.5f )
		{
			var push = Game.Random.Float( 0.5f, 1.5f ) + lodDistance;
			var rightDot = normal.Dot( vecRight );

			// If we're up close, randomly move it around. If we're at a distance, always push it to the side
			// Up close, this can move it back towards the view, but the random chance still looks better
			if ( (distance >= 512 && rightDot > 0) || (distance < 512 && Game.Random.Float( 0, 1 ) > 0.5f) ) 
			{
				// Turn it to the right
				normal += vecRight * push;
			}
			else
			{
				// Turn it to the left
				normal -= vecRight * push;
			}
		}

		var sprayEffect = distance < 400
			? BloodSprayParticle
			: BloodSprayFarParticle;

		var bloodSpray = Particles.Create( sprayEffect, origin );
		bloodSpray.SetForward( 0, normal );

	}

	public override void OnTakeDamageReaction( ExtendedDamageInfo info )
	{
		base.OnTakeDamageReaction( info );
		PainSound( info );
	}

	public void PainSound( ExtendedDamageInfo info )
	{
		SpeakConceptIfAllowed( TFResponseConcept.Pain );
	}
}
