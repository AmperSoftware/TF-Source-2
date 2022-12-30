using Sandbox;
using Amper.FPS;
using System;
using System.Collections.Generic;

namespace TFS2;

public partial class FlameEntity : TFProjectile
{
	public Vector3 AttackerVelocity;

	FlameThrower FlameThrower;
	HashSet<Entity> BurntEntities = new();

	public override void Spawn()
	{
		base.Spawn();

		SetBBox( tf_flamethrower_boxsize );
		MoveType = ProjectileMoveType.Custom;

		DamageFlags |= TFDamageFlags.Ignite;
		DamageFlags |= TFDamageFlags.PreventPhysicsForce;

		AutoDestroyTime = tf_flamethrower_flametime;
	}

	public override bool CanBeDeflected => false;

	public override void OnInitialized()
	{
		base.OnInitialized();

		FlameThrower = Launcher as FlameThrower;
		FlameThrower?.AddActiveFlameEntity( this );
		BaseVelocity = Velocity;
	}

	protected override void OnDestroy()
	{
		FlameThrower?.RemoveActiveFlameEntity( this );
	}

	public override void MoveCustom()
	{
		// Reduce our base velocity by the air drag constant
		BaseVelocity *= tf_flamethrower_drag;
		Velocity = BaseVelocity;

		// Add our float upward velocity
		Velocity += Vector3.Up * tf_flamethrower_float;

		// Compensate for attacker's velocity.
		Velocity += AttackerVelocity;

		if ( tf_debug_flamethrower )
		{
			DebugOverlay.Box( Position, Mins, Maxs, Color.Green );
		}
	}

	public override void OnTraceTouch( Entity other, TraceResult result )
	{
		if ( other.IsWorld )
			return;

		// remember that we've burnt this player
		if ( !BurntEntities.Add( other ) )
			return;

		FlameThrower?.NoteHitTarget();

		var distance = Position.Distance( OriginalPosition );
		var distScale = distance.RemapClamped( tf_flamethrower_maxdamagedist / 2, tf_flamethrower_maxdamagedist, 1, 0.7f );
		var damage = Damage * distScale;
		damage = MathF.Max( damage, 1 );

		var dmgInfo = CreateDamageInfo( damage )
			.WithHitPosition( Position )
			.WithOriginPosition( OriginalPosition )
			.WithReportPosition( Owner.GetEyePosition() );

		other.TakeDamage( dmgInfo );
	}

	public override Trace SetupCollisionTrace( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		return base.SetupCollisionTrace( start, end, mins, maxs );
		//.WithoutTags( CollisionTags.Player );
	}
	[ConVar.Server] public static bool tf_debug_flamethrower { get; set; }
	[ConVar.Server] public static float tf_flamethrower_drag { get; set; } = .85f;
	[ConVar.Server] public static float tf_flamethrower_float { get; set; } = 50;
	[ConVar.Server] public static float tf_flamethrower_boxsize { get; set; } = 24;
	[ConVar.Server] public static float tf_flamethrower_maxdamagedist { get; set; } = 250f;
	[ConVar.Server] public static float tf_flamethrower_flametime { get; set; } = .5f;
}
