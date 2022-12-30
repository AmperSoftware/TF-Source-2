using Sandbox;
using Amper.FPS;
using System.Collections.Generic;
using System;
using Sandbox.Diagnostics;

namespace TFS2;

[Library( "tf_weapon_flamethrower", Title = "Flame Thrower" )]
public partial class FlameThrower : TFHoldWeaponBase
{
	public readonly Vector3 MuzzleOffset = new Vector3( 70, 12, -12 );
	[Net] public float LastFlameContactTime { get; set; }
	public float TimeStopHittingTarget;

	public override void Attack()
	{
		if ( !Game.IsServer )
			return;

		//
		// Origin
		//

		var eyeRot = GetAttackRotation();
		var origin = GetAttackOrigin();

		var forward = eyeRot.Forward;
		var right = eyeRot.Right;
		var up = eyeRot.Up;

		// Flame appears in front of our eyes and go forward.
		origin += forward * tf_flamethrower_forward_distance;

		//
		// Velocity
		//

		var speed = tf_flamethrower_velocity;
		var velocity = forward * speed;
		velocity += Vector3.Random * tf_flamethrower_vecrand * speed;

		var damagePerSec = Data.Damage;
		var attackInterval = GetAttackTime();
		var damage = damagePerSec * attackInterval;

		var flame = FireProjectile<FlameEntity>( origin, velocity, damage );
		flame.AttackerVelocity = Owner.Velocity;
	}

	public override void OnHoldStart()
	{
		// Enable flame particle if we've started holding this weapon.
		SendAnimParameter( "b_fire_hold", true );
	}

	public override void OnHoldStop()
	{
		// Disable flame particle if we stopped holding.
		SendAnimParameter( "b_fire_hold", false );
	}

	public override bool CanHold()
	{
		// We can't hold down the attack button if we can't 
		// attack.
		if ( !CanAttack() )
			return false;

		// We don't have enough ammo to attack.
		if ( !HasEnoughAmmoToAttack() )
			return false;

		// If there is surface ahead of us, don't attack.
		if ( IsSurfaceAhead() )
			return false;

		return true;
	}

	/// <summary>
	/// Is there a surface ahead of us?
	/// </summary>
	public bool IsSurfaceAhead()
	{
		var origin = GetAttackOrigin();

		var eyeRot = GetAttackRotation();
		var forward = eyeRot.Forward;
		var right = eyeRot.Right;
		var up = eyeRot.Up;

		var target = origin +
			forward * MuzzleOffset.x +
			right * MuzzleOffset.y +
			up * MuzzleOffset.z;

		var tr = SetupFireBulletTrace( origin, target )

			// Ignore players
			.WithoutTags( CollisionTags.Player )

			.Run();

		return tr.Hit;
	}

	//
	// Flame
	//

	List<FlameEntity> ActiveFlameEntities = new();

	public void AddActiveFlameEntity( FlameEntity entity )
	{
		if ( ActiveFlameEntities.Contains( entity ) )
			return;

		ActiveFlameEntities.Add( entity );
	}

	public void RemoveActiveFlameEntity( FlameEntity entity )
	{
		ActiveFlameEntities.Remove( entity );
	}

	[ConVar.Server] public static float tf_flamethrower_velocity { get; set; } = 1400;
	[ConVar.Server] public static float tf_flamethrower_vecrand { get; set; } = 0.05f;
	[ConVar.Server] public static float tf_flamethrower_forward_distance { get; set; } = 70;

	[ConVar.Server] public static float tf_flamethrower_airblast_boxsize { get; set; } = 128;
	[ConVar.Server] public static float tf_flamethrower_airblast_force { get; set; } = 600;
	[ConVar.Server] public static float tf_flamethrower_airblast_min_z_force { get; set; } = 280;
	[ConVar.Server] public static int tf_flamethrower_airblast_cost { get; set; } = 20;
	[ConVar.Server] public static float tf_flamethrower_airblast_refire_time { get; set; } = .75f;
	[ConVar.Server] public static float tf_flamethrower_airblast_extinguish_reward { get; set; } = 20;

	[ConVar.Server] public static bool tf_flamethrower_airblast_reflective { get; set; }
	[ConVar.Server] public static float tf_flamethrower_airblast_reflective_distance { get; set; } = 100;
	[ConVar.Server] public static float tf_flamethrower_airblast_reflective_target_range { get; set; } = 150;
	[ConVar.Server] public static float tf_flamethrower_airblast_reflective_force { get; set; } = 400;

	public void NoteHitTarget()
	{
		LastFlameContactTime = Time.Now;
	}

	//
	// Airblast
	//

	public bool CanAirblast()
	{
		return true;
	}

	public override void SecondaryAttack()
	{
		var player = TFOwner;
		if ( !player.IsValid() )
			return;

		// This weapon can't airblast.
		if ( !CanAirblast() )
			return;

		// Can't airblast underwater.
		if ( player.WaterLevelType >= WaterLevelType.Eyes )
			return;

		// Check if we have enough ammo to make airblast attack.
		var ammoPerBlast = tf_flamethrower_airblast_cost;
		if ( !HasEnoughAmmo( ammoPerBlast ) )
			return;

		// Take cost from ammo to make airblast.
		TakeAmmo( ammoPerBlast );

		// Make weapon unusable for some time.
		NextAttackTime = Time.Now + tf_flamethrower_airblast_refire_time;
		FireAirblast();
	}

	public const string AirblastSound = "weapon_flamethrower.airblast";

	public void FireAirblast()
	{
		SendAnimParameter( "b_fire_secondary" );
		CreateAirblastEffect();
		PlaySound( AirblastSound );

		DeflectEntities();
	}

	[ClientRpc]
	public void CreateAirblastEffect()
	{
		this.CreateParticle( "particles/flamethrower/pyro_blast.vpcf", "muzzle" );
	}

	public void DeflectEntities()
	{
		if ( !Game.IsServer )
			return;

		var player = TFOwner;
		if ( !player.IsValid() )
			return;

		using var _ = LagCompensation();
		using var _2 = Prediction.Off();

		var eyePos = player.EyePosition;
		var eyeRot = player.EyeRotation;
		var forward = eyeRot.Forward;

		var boxSize = tf_flamethrower_airblast_boxsize;
		var size = new Vector3( boxSize );

		var center = eyePos + forward * boxSize;
		var mins = center - size;
		var maxs = center + size;

		var bbox = new BBox( mins, maxs );
		var entities = FindInBox( bbox );

		var deflectedPlayer = false;

		foreach ( var ent in entities )
		{
			// Dont deflect ourselves.
			if ( ent == this )
				continue;

			// Dont deflect our owner.
			if ( ent == Owner )
				continue;

			// Dont deflect dead stuff.
			if ( ent.LifeState != LifeState.Alive )
				continue;

			// See if we have a line of sight to that entity.
			var tr = Trace.Ray( player.EyePosition, ent.GetEyePosition() )
				.Ignore( Owner )
				.Ignore( ent )
				.WithAnyTags( CollisionTags.Solid )
				.Run();

			// Trace didn't go all the way through.
			if ( tr.Fraction < 1 )
				continue;

			// If entity is a player.
			if ( ent is TFPlayer entPlayer )
			{
				if ( DeflectPlayer( entPlayer, forward, center, size ) )
					deflectedPlayer = true;
			}

			// If entity is a player.
			if ( ent is TFProjectile entProjectile )
			{
				TryDeflectProjectile( entProjectile, forward, center, size );
			}
		}

		if ( deflectedPlayer )
			PlayUnpredictedSound( AirblastPlayerImpactSound );
	}

	public const string AirblastPlayerImpactSound = "player.airblast_impact";
	const float AirblastDeflectTraceRange = 2048;

	public bool TryDeflectProjectile( TFProjectile target, Vector3 forward, Vector3 center, Vector3 size )
	{
		// Can't deflect our team's stuff. Some projectiles cannot be deflected at all (ex. syringes and flames)
		if ( target.Team == Team || !target.CanBeDeflected )
			return false;

		var curSpeed = target.Velocity.Length;
		var startPos = Owner.GetEyePosition();
		var endPos = startPos + forward * AirblastDeflectTraceRange;

		var tr = Trace.Ray( startPos, endPos )
			.WithAnyTags( CollisionTags.Solid )

			.WithoutTags( CollisionTags.Player )
			.WithoutTags( CollisionTags.Weapon )
			.WithoutTags( CollisionTags.Projectile )
			// TODO: Buildings

			.Run();

		var vecDir = tr.EndPosition - target.WorldSpaceBounds.Center;

		// If projectile has physics movement and it's currently stationary
		if ( target.MoveType == ProjectileMoveType.Physics )
		{
			curSpeed = target.PhysicsBody.Velocity.Length;

			if ( !target.PhysicsEnabled )
			{
				vecDir = target.WorldSpaceBounds.Center - Owner.WorldSpaceBounds.Center;
				target.PhysicsEnabled = true;
			}
		}

		vecDir = vecDir.Normal;
		var vecVel = vecDir * curSpeed;

		target.Velocity = vecVel;
		target.Deflected( this, TFOwner ); ;

		return true;
	}

	public bool DeflectPlayer( TFPlayer target, Vector3 forward, Vector3 center, Vector3 size )
	{
		Assert.NotNull( target );

		// Can't deflect spectators.
		if ( !target.Team.IsPlayable() )
			return false;

		//
		// If we're deflecting a teammate.
		//

		if ( Owner != target && ITeam.IsSame( target, Owner ) )
		{
			if ( target.Extinguish( this ) )
			{
				// Give us a reward for helping our teammate.
				var reward = tf_flamethrower_airblast_extinguish_reward;
				if ( reward > 0 )
				{
					TFOwner?.GiveHealth( reward );
				}
			}

			return false;
		}

		// Target is immune to airblasting.
		if ( target.IsImmuneToAirBlasts( this, TFOwner ) )
			return false;

		var targetPos = target.WorldSpaceBounds.Center;
		var vecToTarget = targetPos - Owner.WorldSpaceBounds.Center;
		vecToTarget = vecToTarget.Normal;

		// if we are airblasting ourselves, airblast us in the durection of where we're looking.
		if ( target == Owner )
			vecToTarget = forward;

		var force = vecToTarget * tf_flamethrower_airblast_force;

		// EXPERIMENTAL: Airblasting reflects from surfaces.
		if ( tf_flamethrower_airblast_reflective )
		{
			var startPos = Owner.GetEyePosition();
			var endPos = startPos + forward * tf_flamethrower_airblast_reflective_distance;

			var tr = Trace.Ray( Owner.GetEyePosition(), endPos )
				.WithAnyTags( CollisionTags.Solid )
				.WithoutTags( CollisionTags.Player )
				.Ignore( Owner )
				.Run();

			if ( tr.Hit && tr.Entity != target )
			{
				var reflectToTarget = targetPos - tr.EndPosition;
				reflectToTarget = reflectToTarget.Normal;

				// Reflection power scales by distance
				var distToTarget = targetPos.Distance( tr.EndPosition );
				var reflectForce = distToTarget.RemapVal(
					0, tf_flamethrower_airblast_reflective_target_range,
					tf_flamethrower_airblast_reflective_force, 0 );

				if ( reflectForce > 0 )
				{
					force += reflectToTarget * reflectForce;
				}
			}
		}

		if ( force.Length > tf_flamethrower_airblast_force )
			force = force.Normal * tf_flamethrower_airblast_force;

		force.z = MathF.Max( force.z, tf_flamethrower_airblast_min_z_force );

		target.ApplyViewPunchImpulse( Game.Random.Float( 10, 15 ) );
		target.ApplyAbsoluteImpulse( force );
		return true;
	}

	//
	// Visuals
	//


	ParticleContainer FlameParticle;

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		FlameParticle = new( this, "muzzle" );
		FlameParticle.Bind( GetFlameParticle, 0, () => IsHolding );
		FlameParticle.Bind( GetCritFlameParticle, 1, () => IsHolding && IsCurrentAttackCritical );
	}

	public const string FlameParticleRedEffect = "particles/flamethrower/flamethrower.vpcf";
	public const string FlameParticleBlueEffect = "particles/flamethrower/flamethrower_blue.vpcf";
	public const string FlameParticleCritRedEffect = "particles/flamethrower/flamethrower_crit_red.vpcf";
	public const string FlameParticleCritBlueEffect = "particles/flamethrower/flamethrower_crit_blue.vpcf";

	public string GetFlameParticle()
	{
		return Team == TFTeam.Blue
			? FlameParticleBlueEffect
			: FlameParticleRedEffect;
	}

	public string GetCritFlameParticle()
	{
		return Team == TFTeam.Blue
			? FlameParticleCritBlueEffect
			: FlameParticleCritRedEffect;
	}

	//
	// Sounds
	//

	public const string AirBlastSound = "weapon_flamethrower.airblast";
	public const string FlameLoopSoundName = "weapon_flamethrower.loop";
	public const string FlameLoopHitSoundName = "weapon_flamethrower.loop.hit";
	public const string FlameLoopCritSoundName = "weapon_flamethrower.loop.crit";
	public const string FlameLoopEndSoundName = "weapon_flamethrower.loop.end";

	bool firingCritSound;

	Sound? FireLoopSound;
	Sound? FireHitLoopSound;

	public override void ClientTick()
	{
		base.ClientTick();

		// Handle flamethrower looping sounds.
		if ( IsHolding )
		{
			// Check if we need to change our sounds.
			if ( FireLoopSound == null || IsCurrentAttackCritical != firingCritSound )
			{
				var sound = IsCurrentAttackCritical
					? FlameLoopCritSoundName
					: FlameLoopSoundName;

				FireLoopSound?.Stop();
				FireLoopSound = PlaySound( sound );

				firingCritSound = IsCurrentAttackCritical;
			}

			// Play sound when we hit an enemy with our flame.
			if ( IsLocalPawn )
			{
				// Hitting target means we dealt damage in the past 0.2s
				var isHittingTarget = Time.Now - LastFlameContactTime < 0.2f;

				if ( FireHitLoopSound.HasValue != isHittingTarget )
				{
					FireHitLoopSound?.Stop();
					FireHitLoopSound = null;

					if ( isHittingTarget )
					{
						FireHitLoopSound?.Stop();
						FireHitLoopSound = PlaySound( FlameLoopHitSoundName );
					}
				}
			}
		}
		else
		{
			if ( FireLoopSound != null )
			{
				// Play flame loop end sound name.
				PlaySound( FlameLoopEndSoundName );

				FireLoopSound?.Stop();
				FireLoopSound = null;
			}

			FireHitLoopSound?.Stop();
			FireHitLoopSound = null;
		}
	}

	public override void OnHolster( SDKPlayer owner )
	{
		base.OnHolster( owner );

		FireHitLoopSound?.Stop();
		FireHitLoopSound = null;

		FireLoopSound?.Stop();
		FireLoopSound = null;
	}
}


/*
#region Airblast

/// <summary>
/// Clip cost of one airblast.
/// </summary>
public virtual int AirblastCost => 20;
	/// <summary>
	/// Time that one airblast takes to perform.
	/// </summary>
	public virtual float AirBlastTime => 1f;
	/// <summary>
	/// Is this Flame Thrower currently busy airblasting?
	/// </summary>
	public bool IsAirblasting => TimeSinceSecondaryAttack < AirBlastTime;
	/// <summary>
	/// Airblast push force.
	/// </summary>
	protected virtual float PushForce => 800;

	public override bool CanSecondaryAttack()
	{
		if ( Clip < AirblastCost ) return false;
		return base.CanSecondaryAttack();
	}

	public override void AttackSecondary()
	{
		TimeSinceSecondaryAttack = 0;
		PerformAirblast();
	}

	public override float GetSecondaryAttackTime()
	{
		return AirBlastTime;
	}

	[ConVar.Replicated] public static bool tf_dev_airblast_enabled { get; set; }

	public virtual void PerformAirblast()
	{
		if ( !tf_dev_airblast_enabled )
			return;

		SetAnimParameter( "b_fire_secondary", true );
		ParentViewModel?.SetAnimParameter( "b_fire_secondary", true );
		(Owner as AnimatedEntity).SetAnimParameter( "b_fire_secondary", true );

		PlaySound( "weapon_flamethrower.airblast" );

		Clip -= AirblastCost;


		// This part is calculated server only.
		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{
				// Getting all the vector forwards.
				Vector3 forward = Owner.EyeRotation.Forward;

				// Sizes of the detection hull.
				var halfSize = new Vector3( 128, 128, 96 );

				// Muzzle flash world position.
				Vector3 muzzle = GetMuzzleWorldPosition();

				// DebugOverlay.Sphere( muzzle, 1f, Color.Red, true, 10f );

				Vector3 eyeOrigin = Owner.EyePosition;
				Vector3 boxOrigin = eyeOrigin + forward * halfSize.x;

				// DebugOverlay.Box( 5f, boxOrigin, Owner.EyeRotation, -halfSize, halfSize, Color.Red, true );
				var bbox = new BBox( boxOrigin - halfSize, boxOrigin + halfSize );

				// Finding all entities witin this box.
				foreach ( var ent in FindInBox( bbox ) )
				{
					// Can't push ourselves.
					if ( ent == this ) continue;

					// Can't push our owner.
					if ( ent == Owner ) continue;

					// Can't push if ent is not a model.
					if ( ent is not ModelEntity model ) continue;

					// Can't push if model doesn't have physbody.
					if ( model.PhysicsBody == null ) continue;

					// Trace a ray to see if we can truly reach that element.
					var tr = Trace.Ray( muzzle, model.PhysicsBody.MassCenter )
						.UseHitboxes()
						.Run();

					if ( !tr.Hit ) continue;

					// Figure out if the target is within the cone of our vision.
					var direction = tr.EndPosition - Owner.EyePosition;
					float dot = Vector3.Dot( Owner.EyeRotation.Forward, direction.Normal );
					if ( MathF.Abs( dot ) < 0.75f ) continue;

					// Calculate force.
					var force = direction * PushForce;
					force *= model.PhysicsBody.Mass * .3f;

					// Apply force.
					model.ApplyLocalImpulse( force );
				}
			}
		}
	}
	#endregion
}
*/
