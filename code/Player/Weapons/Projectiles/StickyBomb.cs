using Sandbox;
using Amper.FPS;

namespace TFS2;

public partial class StickyBomb : TFProjectile
{
	[Net] public bool IsDeployed { get; set; }

	[Net] public float TimeUntilRestick { get; set; }
	[Net] public float TimeUntilDeflectReset { get; set; }

	/// <summary>
	/// How long after being detached will we re-stick to the world.
	/// </summary>
	[ConVar.Replicated] public static float tf_grenade_force_sleeptime { get; set; } = 1.0f;
	[ConVar.Replicated] public static float tf_pipebomb_deflect_reset_time { get; set; } = 10.0f;

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/weapons/w_models/w_stickybomb.vmdl" );
		Health = 1;

		MoveType = ProjectileMoveType.Physics;
		DamageFlags |= DamageFlags.Blast;
		FaceVelocity = false;
		AutoDestroyTime = null;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( !CanStickOnEntity( eventData.Other.Entity ) )
			return;

		MoveType = ProjectileMoveType.None;
		SetParent( eventData.This.Entity );
	}

	public bool CanStickOnEntity( Entity entity )
	{
		// Only allow sticking to walls for now.
		return entity.IsWorld && TimeUntilRestick < Time.Now;
	}

	public override void Deflected( DeflectingContext ctx )
	{
		const int DeflectionForce = 500;

		ctx.ShouldChangeTeam = false;
		ctx.ShouldApplyBoost = false;

		if ( MoveType == ProjectileMoveType.None || TimeUntilRestick != 0 )
		{
			// The sticky bomb has touched a surface at least once, let's apply velocity manually
			MoveType = ProjectileMoveType.Physics;

			var vecDir = WorldSpaceBounds.Center - ctx.Who.WorldSpaceBounds.Center;
			vecDir = vecDir.Normal;
			PhysicsBody.Velocity = vecDir * DeflectionForce;
		}

		TimeUntilRestick = Time.Now + tf_grenade_force_sleeptime;
		TimeUntilDeflectReset = Time.Now + tf_pipebomb_deflect_reset_time;

		base.Deflected( ctx );
	}

	public override void TakeDamage( DamageInfo info )
	{
		// Bombs can only be destroyed by bullets.
		if ( !info.Flags.HasFlag( TFDamageFlags.Bullet ) )
			return;

		base.TakeDamage( info );
	}

	public override void Tick()
	{
		base.Tick();

		if ( !IsDeployed && TimeSinceCreated > GetDeployTime() )
		{
			OnDeployed();
		}

		if ( Owner != OriginalOwner && TimeUntilDeflectReset < Time.Now )
		{
			Owner = OriginalOwner;
			Launcher = OriginalLauncher;
		}
	}

	protected override void OnDestroy()
	{
		// Notify the launcher that we have been destroyed.
		if ( Launcher is StickyBombLauncher launcher )
		{
			launcher.OnStickyDestroyed( this );
		}

		base.OnDestroy();
	}

	public bool CanDetonate()
	{
		return IsDeployed;
	}

	public void OnDeployed()
	{
		IsDeployed = true;
		DeployEffects();
	}

	[ClientRpc]
	public void DeployEffects()
	{
//		DeleteTrails();
		Trail = Particles.Create( $"particles/stickybomb/stickybomb_pulse_{Team.GetName()}.vpcf", this );
	}

#if false
	public override void DeleteTrails( bool immediate = false )
	{
		if ( !IsClient )
			return;

		Trail?.Destroy( immediate );
		// Don't destroy crit particle.
	}
#endif

	public float GetDeployTime()
	{
		// TODO: Consider attributes.
		return 1f;
	}

	public void Fizzle()
	{
		if ( !IsServer )
			return;

		// TODO: Particles?

		Delete();
	}

	public const float ShakeAmplitude = 10;
	public const float ShakeFrequency = 150;
	public const float ShakeDuration = 1;
	public const float ShakeRadius = 300;

	public override void Explode( TraceResult trace )
	{
		base.Explode( trace );
		ScreenShake.Shake( Position, ShakeAmplitude, ShakeFrequency, ShakeDuration, ShakeRadius, ShakeCommand.Start );
	}

	public override string TrailParticleName => $"particles/stickybomb/stickybombtrail_{Team.GetName()}.vpcf";
	public override string CriticalTrailParticleName => $"particles/stickybomb/critical_pipe_{Team.GetName()}.vpcf";
}
