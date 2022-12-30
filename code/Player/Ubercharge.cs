using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace TFS2;

partial class TFPlayer
{
	List<TFCondition> ActiveUberchargeEffects { get; set; } = new();

	public void TickUberchargeEffects()
	{
		if ( !Game.IsServer )
			return;

		//
		// Calculate ubercharge effects
		//

		Dictionary<TFCondition, Entity> newProvidedEffects = new();

		// Checking what effects I provide right now.
		var medigun = GetWeaponOfType<Medigun>();
		if ( medigun.IsValid() && medigun.IsReleasingCharge )
		{
			var effect = medigun.GetChargeType();
			newProvidedEffects[effect] = medigun;
		}

		// Checking what effects healers provide right now.
		foreach ( var healer in Healers )
		{
			// Check if the healer is the player.
			medigun = healer.Key as Medigun;
			if ( !medigun.IsValid() )
				continue;

			if ( !medigun.IsReleasingCharge )
				continue;

			// Get the effect type of this medigun.
			var effect = medigun.GetChargeType();
			newProvidedEffects[effect] = medigun;
		}

		ApplyUberchargeEffects( newProvidedEffects );
	}

	public void ApplyUberchargeEffects( Dictionary<TFCondition, Entity> effects )
	{
		// Apply all new effects.
		foreach ( var pair in effects )
		{
			var effect = pair.Key;
			var provider = pair.Value;

			AddUberchargeEffect( effect, provider );
		}

		// Get rid of all actively provided effects that we're no longer 
		// being provided.
		var newEffects = effects.Keys;
		var oldEffects = ActiveUberchargeEffects;

		// We're getting "Collection modified" error in the loop below if we don't make
		// a clone of this enumeration.
		var expiredEffects = oldEffects.Except( newEffects ).ToArray();

		foreach ( var effect in expiredEffects )
		{
			RemoveUberchargeEffect( effect );
		}
	}

	public void AddUberchargeEffect( TFCondition effect, Entity provider )
	{
		// We're already in condition.
		if ( InCondition( effect ) )
			return;

		AddCondition( effect, PermanentCondition, provider );
		ActiveUberchargeEffects.Add( effect );
	}

	public void RemoveUberchargeEffect( TFCondition effect )
	{
		RemoveCondition( effect );
		ActiveUberchargeEffects.Remove( effect );
	}

	public bool IsInvulnerable()
	{
		return InCondition( TFCondition.Invulnerable )
			|| InCondition( TFCondition.InvulnerableWearoff );
	}
}
