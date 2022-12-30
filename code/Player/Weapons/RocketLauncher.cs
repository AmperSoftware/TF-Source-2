using Sandbox;

namespace TFS2;

[Library( "tf_weapon_rocketlauncher", Title = "Rocket Launcher" )]
public class RocketLauncher : TFWeaponBase
{
	public readonly Vector3 MuzzleOffset = new Vector3( 23.5f, 12, -3 );

	public override void Attack()
	{
		if ( !Game.IsServer )
			return;

		GetProjectileFireSetup( MuzzleOffset, out var origin, out var direction, false );
		var velocity = direction * tf_projectile_rocket_speed;

		FireProjectile<Rocket>( origin, velocity, Data.Damage );
	}

	[ConVar.Replicated] public static float tf_projectile_rocket_speed { get; set; } = 1100;
}
