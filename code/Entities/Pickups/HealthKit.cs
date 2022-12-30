using Sandbox;
using Editor;
using System;
using System.ComponentModel;

namespace TFS2;

public abstract class HealthKit : PickupItem
{
	public virtual float HealthMultiplier => 1f;

	public override void OnPicked( TFPlayer player )
	{
		if ( player.Health >= player.GetMaxHealth() ) // We dont need to give health to people who are already at max
			return;

		player.GiveHealth( MathF.Ceiling( player.GetMaxHealth() * HealthMultiplier ) );
		base.OnPicked( player );
		Sound.FromEntity( "Player.PickupHealth", this );
	}

	protected override void RespawnPickup()
	{
		base.RespawnPickup();
		Sound.FromEntity( "Pickup.Respawn", this );
	}
}

[Library( "tf_healthkit_small" )]
[Title("Small Health Kit")]
[Category( "Pickups" )]
[Icon("local_hospital")]
[EditorModel( "models/items/medkit_small.vmdl" )]
[HammerEntity]
public class HealthKitSmall : HealthKit
{
	public override string ModelPath => "models/items/medkit_small.vmdl";
	public override float HealthMultiplier => 0.2f;

}

[Library( "tf_healthkit_medium" )]
[Title( "Medium Health Kit" )]
[Category( "Pickups" )]
[Icon( "local_hospital" )]
[EditorModel( "models/items/medkit_medium.vmdl" )]
[HammerEntity]
public class HealthKitMedium : HealthKit
{
	public override string ModelPath => "models/items/medkit_medium.vmdl";
	public override float HealthMultiplier => 0.5f;
}

[Library( "tf_healthkit_full")]
[Title( "Large Health Kit" )]
[Category( "Pickups" )]
[Icon( "local_hospital" )]
[EditorModel( "models/items/medkit_large.vmdl" )]
[HammerEntity]
public class HealthKitFull : HealthKit
{
	public override string ModelPath => "models/items/medkit_large.vmdl";
	public override float HealthMultiplier => 1f;
}
