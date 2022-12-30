using Sandbox;
using Amper.FPS;
using System.Linq;

namespace TFS2;

public partial class TFMeleeBase : TFWeaponBase
{
	[Net, Predicted] public float? SmackTime { get; set; }

	// Melees don't have ammo.
	public override bool ShowAmmoOnHud() => false;
	public override bool NeedsAmmo() => false;
	public override int GetRange() => 48;

	public override void OnHolster( SDKPlayer owner )
	{
		SmackTime = null;
		base.OnHolster( owner );
	}

	public override void PrimaryAttack()
	{
		Swing();
	}

	public override void SendAnimParametersOnAttack()
	{
		SendPlayerAnimParameter( "b_fire" );
		SendViewModelAnimParameter( IsCurrentAttackCritical ? "b_fire_critical" : "b_fire" );
	}

	public override void SimulateAttack()
	{
		base.SimulateAttack();

		if ( SmackTime.HasValue && Time.Now > SmackTime )
			Smack();
	}

	public virtual void Swing()
	{
		CalculateIsAttackCritical();

		// send all the appropriate weapon animations.
		SendAnimParametersOnAttack();

		CalculateNextAttackTime();

		PlayAttackSound();

		SmackTime = Time.Now + Data.SmackTime;
	}

	public virtual void Smack()
	{
		var damage = GetDamage();
		FireBullet( damage );
		SmackTime = null;
	}

	public override TraceResult TraceFireBullet( int seedOffset = 0 )
	{
		Game.Random.SetSeed( Time.Tick + seedOffset );

		var range = GetRange();
		var spread = GetSpread();

		Vector3 origin = GetAttackOrigin();
		Vector3 direction = GetAttackDirectionWithSpread( spread );
		var target = origin + direction * range;

		TraceResult tr = default;
		var all = SetupFireBulletTrace( origin, target ).RunAll();

		if ( all != null )
		{
			// Try to find non teammate hits first.
			var enemyHit = all.Where( x => x.Entity != null && !ITeam.IsSame( x.Entity, TFOwner ) ).FirstOrDefault();
			if ( enemyHit.Hit )
			{
				tr = enemyHit;
			}
			else
			{
				// If we didn't hit an enemy, get the first trace.
				// Those would be our teammates.

				var teammateHit = all.FirstOrDefault();
				if ( teammateHit.Hit )
				{
					tr = teammateHit;
				}
			}
		}
		else
		{
			tr.StartPosition = origin;
			tr.EndPosition = target;
		}

		if ( sv_debug_hitscan_hits )
		{
			DebugOverlay.Line( tr.StartPosition, tr.EndPosition, Game.IsServer ? Color.Yellow : Color.Green, 5f, true );
			DebugOverlay.Sphere( tr.StartPosition, 2f, Color.Cyan, 5, true );
			DebugOverlay.Sphere( tr.EndPosition, 2f, Color.Red, 5, true );
			DebugOverlay.Text( $"{tr.Distance}", tr.EndPosition, 5f );
		}

		return tr;
	}

	public override void OnHitEntity( Entity entity, TraceResult tr )
	{
		base.OnHitEntity( entity, tr );
		PlayImpactSound( entity );
	}

	public virtual void PlayImpactSound( Entity entity )
	{
		if ( entity is SDKPlayer )
			PlaySound( Data.SoundHitFlesh );
		else
			PlaySound( Data.SoundHitWorld );
	}

	public override Trace SetupFireBulletTrace( Vector3 Origin, Vector3 Target )
	{
		return base.SetupFireBulletTrace( Origin, Target )
			.Size( 32 )
			.UseHitboxes( false );
	}

}
