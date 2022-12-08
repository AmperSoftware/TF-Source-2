using Sandbox;
using Amper.FPS;

namespace TFS2;

public partial class TFProjectile : Projectile
{
	public TFTeam Team => (TFTeam)TeamNumber;

	public override void OnInitialized()
	{
		base.OnInitialized();
		SetMaterialGroup( Team == TFTeam.Blue ? 1 : 0 );
	}

	Particles CriticalTrail { get; set; }

	public override void CreateTrails()
	{
		base.CreateTrails();

		if ( DamageFlags.HasFlag( TFDamageFlags.Critical ) )
		{
			if ( !string.IsNullOrEmpty( CriticalTrailParticleName ) )
				CriticalTrail = Particles.Create( CriticalTrailParticleName, this, "criticaltrail" );
		}
	}

	public override void DeleteTrails( bool immediate = false )
	{
		base.DeleteTrails( immediate );
		CriticalTrail?.Destroy( immediate );
	}

	public virtual bool CanBeDeflected => true;

	public const string DeflectedSound = "weapon_flamethrower.reflect_projectile";
	public const string DeflectedEffect = "particles/rocketjumptrail/deflect_fx.vpcf";
	Sound DeflectSound;

	public virtual void Deflected( DeflectingContext ctx )
	{
		Owner = ctx.Who;
		Launcher = ctx.Weapon;

		if ( ctx.ShouldChangeTeam )
		{
			TeamNumber = ctx.Who.TeamNumber;
			SetMaterialGroup( Team == TFTeam.Blue ? 1 : 0 );
		}

		if ( ctx.ShouldApplyBoost )
		{
			// Reflects make projectiles inflict minicritical damage.
			if ( ShouldMiniCritOnDeflection() )
				DamageFlags |= TFDamageFlags.MiniCritical;

			// Pyro was critboosted, projectiles retain the boost.
			if ( ctx.Who.IsCritBoosted )
				DamageFlags |= TFDamageFlags.Critical;
		}

		CreateTrails();
		DeflectedEffects();
		DeflectSound = PlaySound( DeflectedSound );
	}

	public virtual bool ShouldMiniCritOnDeflection() => true;

	protected override void OnDestroy()
	{
		// HACK: killing reflected projectile plays the sound at world origin
		// stop the sound manually for now.

		DeflectSound.Stop();
	}

	[ClientRpc]
	public virtual void DeflectedEffects()
	{
		Particles.Create( DeflectedEffect, this );
	}

	public virtual string CriticalTrailParticleName => "";
	public override string ExplosionParticleName => "particles/explosion/explosioncore_wall.vpcf";
	public override string ExplosionSoundEffect => "weapon.explosion";
}
