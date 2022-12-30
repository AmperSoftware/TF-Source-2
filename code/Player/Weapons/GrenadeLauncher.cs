using Sandbox;

namespace TFS2;

[Library( "tf_weapon_grenadelauncher", Title = "Grenade Launcher" )]
public partial class GrenadeLauncher : TFWeaponBase
{
	public readonly Vector3 MuzzleOffset = new Vector3( 16, 8, -6 );

	public override void Attack()
	{
		if ( !Game.IsServer )
			return;

		var eyeRot = GetAttackRotation();
		var forward = eyeRot.Forward;
		var right = eyeRot.Right;
		var up = eyeRot.Up;

		GetProjectileFireSetup( MuzzleOffset, out var origin, out var direction );

		Vector3 velocity = direction * tf_projectile_grenade_speed
			+ up * 200
			+ right * Game.Random.Int( -10, 10 )
			+ up * Game.Random.Int( -10, 10 );

		var grenade = FireProjectile<Grenade>( origin, velocity, Data.Damage );
		grenade.ApplyLocalAngularImpulse( new Vector3( 600, Game.Random.Float( -1200, 1200 ), 0 ) );
	}

	[ConVar.Replicated] public static float tf_projectile_grenade_speed { get; set; } = 1216;
}
