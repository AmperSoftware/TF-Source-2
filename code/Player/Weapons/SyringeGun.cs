using Sandbox;

namespace TFS2;

[Library( "tf_weapon_syringegun", Title = "Syringe Gun" )]
public partial class SyringeGun : TFWeaponBase
{
	public readonly Vector3 MuzzleOffset = new Vector3( 16, 6, -8 );

	public override void Attack()
	{
		// Syringes should be predictable, but we don't support that right now
		// so just leave it as is.
		// TODO: Implement clientside predicted projectiles.
		if ( !Game.IsServer )
			return;

		GetProjectileFireSetup( MuzzleOffset, out var origin, out var direction );
		var velocity = direction * tf_projectile_syringe_speed;

		FireProjectile<Syringe>( origin, velocity, Data.Damage );
	}

	[ConVar.Replicated] public static float tf_projectile_syringe_speed { get; set; } = 1000;
}
