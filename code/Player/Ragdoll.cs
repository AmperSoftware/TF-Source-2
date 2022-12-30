using Sandbox;
using Amper.FPS;

namespace TFS2;

partial class TFPlayer
{
	public ModelEntity Ragdoll { get; set; }

	public void CreateDeathEntities( ExtendedDamageInfo info )
	{
		if ( ShouldGib( info ) )
		{
			CreateGibs();
			return;
		}

		CreateRagdoll();
	}

	[ConVar.Client] public static float tf_playergib_force { get; set; } = 500;
	[ConVar.Client] public static float tf_playergib_maxspeed { get; set; } = 400;

	[ClientRpc]
	public void CreateGibs()
	{
		if ( Model == null )
			return;

		var angImpulse = new Vector3( Game.Random.Float( 0, 120 ), Game.Random.Float( 0, 120 ), 0 );

		var breakList = Model.GetData<ModelBreakPiece[]>();
		foreach ( var gib in breakList )
		{
			var model = new TFGib();
			model.Position = Position;
			model.Rotation = Rotation;
			model.Scale = Scale;

			model.SetModel( gib.Model );
			model.CopyMaterialGroup( this );

			var breakVelocity = Vector3.Zero;
			breakVelocity.z = Game.Random.Float( 0, tf_playergib_force );
			breakVelocity.x = Game.Random.Float( -tf_playergib_force / 2, tf_playergib_force / 2 );
			breakVelocity.y = Game.Random.Float( -tf_playergib_force / 2, tf_playergib_force / 2 );

			model.ApplyAbsoluteImpulse( breakVelocity );
			model.ApplyLocalAngularImpulse( angImpulse );
		}
	}

	[ClientRpc]
	public void CreateRagdoll()
	{
		if ( Model == null )
			return;

		if ( Ragdoll.IsValid() )
		{
			Ragdoll.Delete();
			Ragdoll = null;
		}

		//
		// Create a corpse of ourselves
		//

		var model = new TFRagdoll();
		model.Position = Position;
		model.Rotation = Rotation;
		model.Scale = Scale;

		model.SetModel( GetModelName() );
		model.CopyBonesFrom( this );
		model.CopyBodyGroups( this );
		model.CopyMaterialGroup( this );
		model.TakeDecalsFrom( this );

		model.ApplyAbsoluteImpulse( Velocity );
		Ragdoll = model;
	}

	/// <summary>
	/// Returns true if this damage should gib.
	/// </summary>
	public virtual bool ShouldGib( ExtendedDamageInfo info )
	{
		if ( !tf_playergib )
			return false;

		// This damage never gibs.
		if ( info.Flags.HasFlag( TFDamageFlags.DoNotGib ) )
			return false;

		// This damage always gibs.
		if ( info.Flags.HasFlag( TFDamageFlags.AlwaysGib ) )
			return true;

		// Only blast damage can gib.
		if ( !info.Flags.HasFlag( TFDamageFlags.Blast ) )
			return false;

		// Explosive crits always gib.
		if ( info.Flags.HasFlag( TFDamageFlags.Critical ) )
			return true;

		// Hard hits also gib.
		if ( Health < -10 )
			return true;

		return false;
	}

	[ConVar.Replicated] public static bool tf_playergib { get; set; } = true;
}

partial class TFRagdoll : ModelEntity
{
	[ConVar.Replicated] public static float tf_ragdoll_lifetime { get; set; } = 10;

	public override void Spawn()
	{
		// This is a client only entity.
		Game.AssertClient();

		Tags.Add( CollisionTags.Debris );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;
		EnableAllCollisions = true;

		DeleteAsync( tf_ragdoll_lifetime );
	}
}

partial class TFGib : ModelEntity
{
	public override void Spawn()
	{
		// This is a client only entity.
		Game.AssertClient();

		Tags.Add( CollisionTags.Debris );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;
		EnableAllCollisions = true;

		// Facepunch pls fix
		Particles.Create( "particles/blood_trail/blood_trail_red_01_goop.vpcf", this, "bloodpoint", true );

		DeleteAsync( 10 );
	}

	Vector3 LastDecalPosition { get; set; }
	[ConVar.Client] public static float tf_gibs_decal_interval { get; set; } = 16;

	[Event.Client.Frame]
	public void Frame()
	{
		//
		// Temp solution because OnPhysicsCollisions are not called on client only entities.
		//

		if ( !PhysicsBody.IsValid() )
			return;

		var center = PhysicsBody.MassCenter;
		if ( LastDecalPosition.Distance( center ) < tf_gibs_decal_interval )
			return;

		var hit = false;

		// Physics doesn't work clientside, trace rays in 6 directions to determine if we hit anything.
		for ( var i = 0; i < 6; i++ )
		{
			var dir = Vector3.Zero;
			switch ( i )
			{
				case 0: dir = Vector3.Up; break;
				case 1: dir = Vector3.Down; break;
				case 2: dir = Vector3.Left; break;
				case 3: dir = Vector3.Right; break;
				case 4: dir = Vector3.Forward; break;
				case 5: dir = Vector3.Backward; break;
			}

			var tr = Trace.Ray( center, center + dir * 12 )
				.WorldOnly()
				.Run();

			if ( tr.Hit )
			{
				if ( ResourceLibrary.TryGet<DecalDefinition>( "data/decal/blood.decal", out var decal ) )
					Decal.Place( decal, tr );

				hit = true;
			}
		}

		if ( hit )
		{
			LastDecalPosition = center;
		}
	}
}
