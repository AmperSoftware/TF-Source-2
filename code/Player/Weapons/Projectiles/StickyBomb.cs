using Sandbox;
using Amper.FPS;

namespace TFS2;

public partial class StickyBomb : TFProjectile
{
	[Net] public bool IsDeployed { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/weapons/w_models/w_stickybomb.vmdl" );
		Health = 1;

		MoveType = ProjectileMoveType.Physics;
		DamageFlags |= DamageFlags.Blast;
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
		return entity.IsWorld;
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
		if ( !Game.IsClient )
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
		if ( !Game.IsServer )
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
