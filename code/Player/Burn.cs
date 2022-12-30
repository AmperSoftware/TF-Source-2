using Sandbox;
using System.Linq;
using Amper.FPS;

namespace TFS2;

public partial class TFPlayer
{
	[ConVar.Server] public static float tf_flamethrower_afterburn_damage_rate { get; set; } = 10;

	float FlameAfterburnTime;
	float NextAfterburnDamageTime;
	TFPlayer OriginalBurnAttacker;
	TFPlayer BurnAttacker;
	TFWeaponBase BurnWeapon;

	const float AfterburnImmuneTime = 0.25f;
	const float AfterburnMinTime = 5;
	const float AfterburnFrequency = 0.5f;
	const float AfterburnDamage = 3;

	public bool CanBurn()
	{
		// Can't burn if i'm underwater.
		if ( WaterLevelType >= WaterLevelType.Waist )
			return false;

		return true;
	}

	public void BurnFromDamage( ExtendedDamageInfo info )
	{
		var attacker = info.Attacker as TFPlayer;
		var weapon = info.Weapon as TFWeaponBase;

		var damage = info.Damage;

		float burnTimeRate = 1 / tf_flamethrower_afterburn_damage_rate;
		var burnTime = damage * burnTimeRate;

		Burn( attacker, weapon, burnTime );
	}

	public void Burn( TFPlayer attacker, TFWeaponBase weapon, float burningTime = 0 )
	{
		if ( !Game.IsServer )
			return;

		// Can't be burnt if already dead.
		if ( !IsAlive )
			return;

		// No class selected.
		if ( !PlayerClass.IsValid() )
			return;

		// TODO:
		// Check if class is fire immune.

		// Remember the last attacker.
		BurnAttacker = attacker;
		BurnWeapon = weapon;

		var afterburnImmune = PlayerClass.Abilities.AfterBurnImmune;
		var setOnFire = false;

		// We are not currently burning.
		if ( !InCondition( TFCondition.Burning ) )
		{
			// Start burning.
			AddCondition( TFCondition.Burning, PermanentCondition, attacker );
			setOnFire = true;

			// Note the original attacker that put us in flames.
			if ( attacker.IsValid() && !afterburnImmune )
			{
				OriginalBurnAttacker = attacker;
			}
		}

		// If we are immune to afterburn, then burn
		// for possible minimun to let attacker know that
		// their hit went through.
		if ( afterburnImmune )
		{
			FlameAfterburnTime = AfterburnImmuneTime;
			return;
		}

		if ( setOnFire )
		{
			// If we were set on fire this time, then
			// burn for the bare minimum of 5s
			FlameAfterburnTime = AfterburnMinTime;
		}

		// If we have any more burn time to contribute, add it here.
		if ( burningTime > 0 )
		{
			FlameAfterburnTime += burningTime;
		}
	}

	public bool Extinguish( Entity extinguisher )
	{
		// I am not burning!
		if ( !InCondition( TFCondition.Burning ) )
			return false;

		PlaySound( "player.fire.extinguish" );
		RemoveCondition( TFCondition.Burning );

		return true;
	}
}
