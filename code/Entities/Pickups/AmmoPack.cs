using Sandbox;
using Editor;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TFS2;

public abstract class AmmoPack : PickupItem
{
	public virtual float AmmoMultiplier => 1f;

	public override void OnPicked( TFPlayer player )
	{
		bool neededAmmo = false;

		// Get all the current weapon entries
		foreach ( var weapon in player.Children.OfType<TFWeaponBase>() ) 
		{
			if ( !weapon.IsInitialized )
				continue;

			// weapon doesnt have any ammo.
			if ( !weapon.NeedsAmmo() )
				continue;

			// if this is false, weapon is not supposed to be owned by his class.
			if ( !weapon.Data.TryGetOwnerDataForPlayerClass( player.PlayerClass, out var ownerData ) )
				continue;

			//
			// Restocking ammo
			//

			if ( ownerData.Reserve > 0 )
			{
				weapon.Reserve = CalculateAmmo( weapon.Reserve, ownerData.Reserve );
			}
			else
			{
				weapon.Clip = CalculateAmmo( weapon.Clip, weapon.Data.ClipSize );
			}

			int CalculateAmmo(int currentAmmo, int maxAmmo)
			{
				// this is how much we need to fully restock our ammo
				var need = maxAmmo - currentAmmo;

				// this is how much we can give
				var canGive = maxAmmo * AmmoMultiplier;

				// seeing how much will give.
				var willGive = Math.Min( need, canGive ).FloorToInt();

				if ( willGive > 0 )
				{
					currentAmmo += willGive;
					neededAmmo = true;
				}

				return currentAmmo;
			}
		}

		if ( !neededAmmo ) 
			return;

		Sound.FromEntity( "player.pickupammo", this );
		base.OnPicked( player );
	}
}

[Library( "tf_ammopack_small" )]
[Title("Small Ammo Pack")]
[Category( "Pickups" )]
[Icon( "backpack" )]
[EditorModel( "models/items/ammopack_small.vmdl" )]
[HammerEntity]
public class AmmoPackSmall : AmmoPack
{
	public override string ModelPath => "models/items/ammopack_small.vmdl";
	public override float AmmoMultiplier => 0.2f;
}

[Library( "tf_ammopack_medium" )]
[Title("Medium Ammo Pack")]
[Category( "Pickups" )]
[Icon( "backpack" )]
[EditorModel( "models/items/ammopack_medium.vmdl" )]
[HammerEntity]
public class AmmoPackMedium : AmmoPack
{
	public override string ModelPath => "models/items/ammopack_medium.vmdl";
	public override float AmmoMultiplier => 0.5f;
}

[Library( "tf_ammopack_full" )]
[Title("Full Ammo Pack")]
[Category( "Pickups" )]
[Icon("backpack")]
[EditorModel( "models/items/ammopack_large.vmdl" )]
[HammerEntity]
public class AmmoPackFull : AmmoPack
{
	public override string ModelPath => "models/items/ammopack_large.vmdl";
	public override float AmmoMultiplier => 1f;
}
