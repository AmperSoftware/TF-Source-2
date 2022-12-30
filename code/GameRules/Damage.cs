using Amper.FPS;
using Sandbox;
using System;

namespace TFS2;

partial class TFGameRules
{
	public const float CritDamageMultiplier = 3f;
	public const float MiniCritDamageMultiplier = 1.35f;

	/// <summary>
	/// Modifies dealt damage using global game rules. This is applied to all taken damage,
	/// regardless or who to where.
	/// </summary>
	public override void ApplyOnDamageModifyRules( ref ExtendedDamageInfo info, Entity victim )
	{
		// If damage is critical, it can't be mini-crit.
		// Remove mini crit flag, if we already have crit.
		if ( info.HasTag( TFDamageFlags.Critical ) )
		{
			info = info.WithoutTags( TFDamageFlags.MiniCritical );
		}

		//
		// Critical Damage adds 3X to the base damage.
		//
		
		if ( info.HasTag( TFDamageFlags.Critical ) )
			info.Damage *= CritDamageMultiplier;

		//
		// Distance Mod (Damage Falloff / Rampup)
		//

		if ( info.UsesDistanceMod() ) 
			ApplyDamageDistanceMod( ref info, victim );

		//
		// Mini-Crit adds 1.35X to the falloff'd damage.
		//

		if ( info.HasTag( TFDamageFlags.MiniCritical ) )
			info.Damage *= MiniCritDamageMultiplier;
	}

	public void ApplyDamageDistanceMod( ref ExtendedDamageInfo info, Entity victim )
	{
		//
		// Chart: https://wiki.teamfortress.com/wiki/Damage#Distance_and_randomness_modifier
		//

		var attacker = info.Attacker;
		var weapon = info.Weapon as TFWeaponBase;
		var fromWeapon = weapon != null && weapon.IsInitialized;

		// Early out if no attacker, or we're damaging ourselves.
		if ( attacker == null || attacker == victim ) 
			return;

		// The distance at which we will be dealing 100% of damage.
		var optimalDist = tf_damage_distancemod_optimal_distance;

		//
		// Calculate the mins and maxs of the chart
		//

		// Default values for the distance mod calculation
		var closeMult = tf_damage_distancemod_maximum_multiplier;
		var farMult = tf_damage_distancemod_minimum_multiplier;

		// If the damage comes from weapon, pull the multipliers from the weapon's data.
		if ( fromWeapon )
		{
			closeMult = weapon.Data.RampupMultiplier;
			farMult = weapon.Data.FalloffMultiplier;
		}

		var attackerPos = attacker.WorldSpaceBounds.Center;
		var victimPos = victim.WorldSpaceBounds.Center;
		var distance = Math.Max( 1, attackerPos.Distance( victimPos ) );

		//
		// Figure our the modificator to the damage based on our distance to the target.
		//

		// This is the 0-1 fraction of our position on the dmg distance mod chart
		// 1 being the closest and 0 being the furthest
		var distLerp = (distance / optimalDist).RemapClamped( 0, 2, 1, 0 );

		// If damage was set to not use rampup, always treat our damage as on optimal distance if it's closer.
		if ( !info.HasTag( TFDamageFlags.UseRampup ) )
			distLerp = MathF.Min( 0.5f, distLerp );
		
		// If damage was set to not use falloff, always treat our damage as on optimal distance if it's farther.
		if ( !info.HasTag( TFDamageFlags.UseFalloff ) )
			distLerp = MathF.Max( 0.5f, distLerp );

		// Apply an easing function to make the chart curve.
		var easedLerp = distLerp * Util.SimpleSpline( distLerp );

		// Turn the values that we have in the chart's value.
		var distMod = easedLerp.RemapClamped( 0, 1, farMult, closeMult );

		//
		// Critical or Mini-Critical damage always deals at least 100% damage regardless of the falloff
		//
		
		if ( info.HasTag( TFDamageFlags.Critical ) || info.HasTag( TFDamageFlags.MiniCritical ) )
			distMod = Math.Max( distMod, 1 );

		info.Damage *= distMod;
	}

	[ConVar.Replicated] public static float tf_damage_distancemod_optimal_distance { get; set; } = 512;
	[ConVar.Replicated] public static float tf_damage_distancemod_maximum_multiplier { get; set; } = 1.5f;
	[ConVar.Replicated] public static float tf_damage_distancemod_minimum_multiplier { get; set; } = 0.5f;
}

/// <summary>
/// A complete list of damage flags, used by TF:S2.
/// Some flags are aliases of same flags, as seen in sbox.
/// Some reuse sbox flags that are not used in TF:S2 as is.
/// https://wiki.teamfortress.com/wiki/Damage#Damage_types
/// </summary>
public static class TFDamageFlags
{
	public const string Generic = DamageFlags.Generic;
	public const string Crush = "crush";
	public const string Bullet = DamageFlags.Bullet;
	public const string Slash = DamageFlags.Slash;
	public const string Burn = DamageFlags.Burn;
	public const string Ignite = "ignite";
	public const string Vehicle = DamageFlags.Vehicle;
	public const string Fall = DamageFlags.Fall;
	public const string Blast = DamageFlags.Blast;
	public const string Melee = DamageFlags.Blunt;
	public const string Shock = DamageFlags.Shock;
	public const string Drown = DamageFlags.Drown;

	public const string AlwaysGib = DamageFlags.AlwaysGib;
	public const string DoNotGib = DamageFlags.DoNotGib;

	public const string PreventPhysicsForce = "no_physics";

	// Critical Stuff
	public const string Critical = "critical";
	public const string MiniCritical = "mini_critical";
	
	// Distance Mod Stuff
	public const string UseRampup = "rampup";
	public const string UseFalloff = "falloff";
	public static bool UsesDistanceMod(this ExtendedDamageInfo info)
	{
		return info.HasTag( UseRampup ) || info.HasTag( UseFalloff );
	}
	public static bool UsesDistanceMod(this DamageInfo info)
	{
		return info.HasTag( UseRampup ) || info.HasTag( UseFalloff );
	}
}
