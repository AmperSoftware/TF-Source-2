using Sandbox;
using System;
using System.Collections.Generic;

namespace TFS2;

partial class TFPlayer
{
	[ConVar.Replicated] public static float tf_max_overheal { get; set; } = 1.5f;
	[ConVar.Replicated] public static float tf_boost_drain_time { get; set; } = 15;
	TimeSince TimeSinceHealthRegenerated { get; set; }

	public struct Healer
	{
		public float Amount { get; set; }
		public float HealAccumulated { get; set; }
		public float KillsWhileBeingHealed { get; set; }
		public float OverhealBonus { get; set; }
		public float OverhealDecayMultiplier { get; set; }
		public float HealedLastSecond { get; set; }
	}

	[Net] public IDictionary<Entity, Healer> Healers { get; private set; }

	public void Heal( Entity healer, float amount, float overhealBonus, float overhealDecayMult )
	{
		if ( !Game.IsServer )
			return;

		float accumHeal = StopHealingFrom( healer );
		if ( !healer.IsValid() )
			Log.Info( "NULL in Heal()" );

		Healers[healer] = new Healer
		{
			Amount = amount,
			HealAccumulated = accumHeal,
			OverhealBonus = overhealBonus,
			KillsWhileBeingHealed = 0,
			OverhealDecayMultiplier = overhealDecayMult,
			HealedLastSecond = 0
		};

		// Recalculate speed?
	}

	public float StopHealingFrom( Entity healer )
	{
		if ( !Game.IsServer )
			return 0;

		if ( !healer.IsValid() )
		{
			Log.Info( "NULL in StopHealingFrom()" );
			return 0;
		}

		float healingDone = 0;
		if ( Healers.TryGetValue( healer, out Healer data ) )
		{
			healingDone = data.HealAccumulated;
			Healers.Remove( healer );
		}

		return healingDone;
	}

	public bool IsHealedBy( Entity entity )
	{
		return Healers.ContainsKey( entity );
	}

	public float GetMaxOverheal()
	{
		var health = GetMaxHealth() * tf_max_overheal;

		// round the health value to fives.
		health = MathF.Floor( health / 5 ) * 5;

		return health;
	}


	public void TickHealing()
	{
		if ( !Game.IsServer )
			return;

		// go through all current healers
		float totalHealthToAdd = 0;

		if ( Healers.Count > 0 )
		{
			foreach ( var pair in Healers )
			{
				var healer = pair.Key;

				if ( !healer.IsValid() )
				{
					StopHealingFrom( healer );
					Healers.Remove( healer );
					continue;
				}

				var data = pair.Value;

				if ( !healer.IsValid() )
					Log.Info( "NULL in SimulateHealing()" );

				var healthToAdd = data.Amount * Time.Delta;
				var givenHealth = GiveHealth( healthToAdd, true );

				data.HealAccumulated += givenHealth;

				Healers[healer] = data;
				totalHealthToAdd += healthToAdd;
			}

		}
		else
		{
			// If we're not being healed by anything decay our overheal.
			DecayOverheal();
		}

		//
		// Passive health regen
		//

		if ( Health < GetMaxHealth() )
		{
			var healthRegenAmount = PlayerClass.Abilities.AutoRegenHealth;
			if ( healthRegenAmount > 0 && TimeSinceHealthRegenerated >= 1 )
			{
				// Log.Info($"Base health regen: {amount}");
				var totalTime = PlayerClass.Abilities.AutoRegenPeakTime;
				var mult = Math.Clamp( TimeSinceTakeDamage / totalTime, 0.5f, 1.0f );

				// Log.Info($"Time since damage: {TimeSinceTakeDamage}\nMult: {mult}");
				healthRegenAmount *= mult;

				// Log.Info($"Health added: {amount}");
				TimeSinceHealthRegenerated = 0f;
				GiveHealth( healthRegenAmount );
			}
		}
	}

	public void DecayOverheal()
	{
		var maxHealth = GetMaxHealth();
		var maxOverheal = GetMaxOverheal();

		if ( Health <= maxHealth )
			return;

		// Items exist that get us over max health, without ever being healed, in which case our m_flBestOverhealDecayMult will still be -1.
		float flDrainMult = 1; // (m_flBestOverhealDecayMult == -1) ? 1.0 : m_flBestOverhealDecayMult;
		float flBoostMaxAmount = maxOverheal - maxHealth;
		float flDrain = flBoostMaxAmount / (tf_boost_drain_time * flDrainMult);

		var nHealthToDrain = flDrain * Time.Delta;
		if ( nHealthToDrain > 0 )
		{
			// Manually subtract the health so we don't generate pain sounds / etc
			Health -= nHealthToDrain;
		}
	}

	public float GiveHealth( float flHealth, bool ignoreMaxHealth = false, bool ignoreMaxOverheal = false )
	{
		var newHealth = Health + flHealth;

		if ( !ignoreMaxOverheal )
		{
			var maxOverheal = GetMaxOverheal();
			newHealth = MathF.Min( newHealth, maxOverheal );
		}

		if ( !ignoreMaxHealth )
		{
			var maxHealth = GetMaxHealth();
			newHealth = MathF.Min( newHealth, maxHealth );
		}

		float result = newHealth - Health;
		result = MathF.Max( result, 0 );

		Health += result;

		return result;
	}
}
