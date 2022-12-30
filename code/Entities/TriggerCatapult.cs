using Sandbox;
using Editor;
using Amper.FPS;
using System.Collections.Generic;

namespace TFS2;

[Library( "tf_trigger_catapult" )]
[Title( "Catapult" )]
[Category( "Gameplay" )]
[Icon( "fast_forward" )]
[HammerEntity]
public partial class TriggerCatapult : BaseTrigger
{
	[Property] Vector3 Direction { get; set; }
	Dictionary<Entity, TimeSince> TimeSinceCatapulted { get; set; } = new();

	public override void Spawn()
	{
		base.Spawn();
		TimeSinceCatapulted.Clear();
	}

	TimeSince TimeSinceTick { get; set; }

	[Event.Tick.Server]
	public void Tick()
	{
		if ( TimeSinceTick < 0.05f ) return;
		TimeSinceTick = 0;

		foreach ( var entity in TouchingEntities )
		{
			if ( CanCatapult( entity ) )
			{
				CatapultByDirection( entity );
				OnCatapulted.Fire( entity );
			}
		}
	}

	public void CatapultByDirection( Entity entity )
	{
		var vecPush = Direction;

		if ( entity is SDKPlayer player )
		{
			if ( player.IsGrounded ) player.GroundEntity = null;
			vecPush += -Game.PhysicsWorld.Gravity.Normal * GameMovement.sv_gravity * SDKGame.Current.GetGravityMultiplier() * .5f;
			player.Velocity = vecPush;
		}
		else
		{
			if ( entity is ModelEntity model )
			{
				model.ApplyAbsoluteImpulse( vecPush );

				var angImpulse = new Vector3( Game.Random.Float( -150, 150 ), Game.Random.Float( -150, 150 ), Game.Random.Float( -150, 150 ) );
				model.ApplyLocalAngularImpulse( angImpulse );
			}
		}

		TimeSinceCatapulted[entity] = 0;
	}

	public bool CanCatapult( Entity player )
	{
		if ( TimeSinceCatapulted.TryGetValue( player, out var time ) )
		{
			return time > 0.5f;
		}

		return true;
	}

	Output OnCatapulted { get; set; }
}
