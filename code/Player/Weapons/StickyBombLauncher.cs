using Sandbox;
using Amper.FPS;
using System.Collections.Generic;
using System.Linq;

namespace TFS2;

[Library( "tf_weapon_stickybomblauncher", Title = "Sticky Bomb Launcher" )]
public partial class StickyBombLauncher : TFWeaponBase, IChargeable, IPassiveChild
{
	public readonly Vector3 MuzzleOffset = new Vector3( 16, 8, -6 );
	public const string DetonationSound = "weapon_stickybomblauncher.detonate";
	public const string DetonationFailSound = "player_use_fail";
	public float DetonationInterval = 0.3f;
	public int StickyBombLimit = 8;

	[Net] public IList<StickyBomb> Bombs { get; set; }

	//
	// IChargeable
	//

	public float ChargeMaxTime => tf_stickybomblauncher_chargeup_time;
	public TimeSince TimeSinceStartCharge { get; set; }
	[Net, Predicted] public bool IsCharging { get; set; }

	public override void Attack()
	{
		if ( !Game.IsServer )
			return;

		var eyeRot = GetAttackRotation();
		Vector3 forward = eyeRot.Forward;
		Vector3 up = eyeRot.Up;
		Vector3 right = eyeRot.Right;

		// Forward force.
		var charger = this as IChargeable;
		Assert.NotNull( charger );

		var minSpeed = tf_projectile_stickybomb_min_speed;
		var maxSpeed = tf_projectile_stickybomb_max_speed;

		var currentCharge = charger.GetCurrentCharge();
		var force = currentCharge.RemapClamped( 0, 1, minSpeed, maxSpeed );

		GetProjectileFireSetup( MuzzleOffset, out var origin, out var direction );

		var velocity = direction * force
			+ up * 200
			+ right * Game.Random.Int( -10, 10 )
			+ up * Game.Random.Int( -10, 10 );

		var bomb = FireProjectile<StickyBomb>( origin, velocity, Data.Damage );
		Bombs.Add( bomb );

		// If we overflow the limit of stickies, detonate the oldest one.
		if ( Bombs.Count > StickyBombLimit )
		{
			var temp = Bombs.FirstOrDefault( x => x.IsValid );
			if ( temp.IsValid() )
				temp.Explode();
		}
	}

	public override void SecondaryAttack()
	{
		base.SecondaryAttack();

	}

	float NextDenySoundTime;
	public void PassiveSimulate( IClient cl )
	{
		if ( !WishSecondaryAttack() )
			return;

		// We have any bombs deployed.
		if ( Bombs.Count > 0 )
		{
			// If one or more pipebombs failed to detonate then play a sound.
			if ( DetonateAllStickyBombs( false ) )
			{
				if ( NextDenySoundTime <= Time.Now )
				{
					NextDenySoundTime = Time.Now + 1;
					PlaySound( DetonationFailSound );
				}
			}
			else
			{
				PlaySound( DetonationSound );
			}

		}
	}

	/// <summary>
	/// Returns true if at least one sticky failed to detonate.
	/// </summary>
	public virtual bool DetonateAllStickyBombs( bool fizzle = false )
	{
		// this launcher has no bombs
		if ( Bombs.Count == 0 )
			return false;

		bool failed = false;

		for ( var i = Bombs.Count - 1; i >= 0; i-- )
		{
			var bomb = Bombs[i];

			// Bomb is already null? Delete it from the original list.
			if ( !bomb.IsValid() )
			{
				Bombs.Remove( bomb );
				continue;
			}

			// We want to fizzle our bombs.
			if ( fizzle )
			{
				bomb.Fizzle();
				continue;
			}

			if ( !bomb.CanDetonate() )
			{
				// We failed to detonate this sticky.
				failed = true;
				continue;
			}

			if ( Game.IsServer )
			{
				bomb.Explode();
			}
		}

		return failed;
	}

	public virtual void OnStickyDestroyed( StickyBomb bomb )
	{
		if ( Game.IsServer )
			Bombs.Remove( bomb );
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
			DetonateAllStickyBombs( true );

		base.OnDestroy();
	}

	Sound ChargeUpSound { get; set; }

	public void OnStartCharge()
	{
		ChargeUpSound = PlaySound( "weapon_stickybomblauncher.chargeup" );
		SendViewModelAnimParameter( "b_charging" );
	}

	public void OnStopCharge()
	{
		ChargeUpSound.Stop();
		SendViewModelAnimParameter( "b_fire" );
	}

	public override bool CanReload()
	{
		if ( IsCharging )
			return false;

		return base.CanReload();
	}

	public override void SimulatePrimaryAttack()
	{
		if ( !HasEnoughAmmoToAttack() )
			return;

		var charger = (IChargeable)this;
		if ( WishPrimaryAttack() && CanPrimaryAttack() )
		{
			if ( !IsCharging )
			{
				charger.StartCharging();
				StopReload();
			}

			if ( !charger.IsCharged )
				return;
		}

		if ( charger.IsCharging )
		{
			PrimaryAttack();
			CalculateNextAttackTime();
			charger.StopCharging();
		}
	}

	public override void OnHolster( SDKPlayer owner )
	{
		base.OnHolster( owner );
		((IChargeable)this).StopCharging();
	}

	public override void OnDrop( SDKPlayer owner )
	{
		base.OnDrop( owner );
		DetonateAllStickyBombs( true );
	}

	[ConVar.Replicated] public static float tf_projectile_stickybomb_min_speed { get; set; } = 900f;
	[ConVar.Replicated] public static float tf_projectile_stickybomb_max_speed { get; set; } = 2400f;
	[ConVar.Replicated] public static float tf_stickybomblauncher_chargeup_time { get; set; } = 4f;
}
