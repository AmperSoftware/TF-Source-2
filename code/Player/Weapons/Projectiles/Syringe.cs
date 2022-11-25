using Sandbox;
using Amper.FPS;

namespace TFS2;

partial class Syringe : TFProjectile
{
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/weapons/w_models/w_syringe_proj.vmdl" );

		DamageFlags |= TFDamageFlags.PreventPhysicsForce;
		MoveType = ProjectileMoveType.Fly;
		Gravity = .3f;
	}

	public override bool CanBeDeflected => false;

	public override void OnTraceTouch( Entity other, TraceResult trace )
	{
		DeleteTrails();

		// try to hit touched entity in the direction we're currently facing.
		bool striked = DoSurfaceImpact( Position, Rotation, other, out var strikeTrace );

		// Check if owner still exists
		if ( Owner.IsValid() )
		{
			// By default, spawn blood wherever we hit with the syringe.
			// If we striked the player's body use the strike position as the endpoint.
			var hitPos = striked
				? strikeTrace.EndPosition
				: trace.EndPosition;

			var info = CreateDamageInfo()
				.UsingTraceResult( trace )
				.WithHitPosition( strikeTrace.HitPosition )
				.WithOriginPosition( OriginalPosition );

			// Deal damage to whatever we hit.
			// deal damage after we potentially striked into this entity, so that
			// if they die, we are removed as well.
			other.TakeDamage( info );
		}

		if ( !striked )
			Delete();
	}

	public bool DoSurfaceImpact( Vector3 position, Rotation rotation, Entity entity, out TraceResult strikeTrace )
	{
		strikeTrace = TraceIntoSurface( position, rotation.Forward );

		// we consider a hit only if whatever we hit is our entity.
		if ( strikeTrace.Entity != entity )
		{
			// if we didn't hit first time, try to trace into any hitbox.
			var center = entity.WorldSpaceBounds.Center;
			// shift center's Z value to whatever our position is
			var shiftedCenter = center.WithZ( position.z );

			var toShiftedCenter = (shiftedCenter - position).Normal;
			strikeTrace = TraceIntoSurface( position, toShiftedCenter );

			// If we couldn't hit it second time time, last attempt to trace to the center.
			if ( strikeTrace.Entity != entity )
			{
				var toCenter = (center - position).Normal;
				strikeTrace = TraceIntoSurface( position, toCenter );

				// If we couldn't hit it third time, return false and delete ourselves.
				if ( strikeTrace.Entity != entity )
					return false;
			}
		}

		// the direction in which the syringe is facing.
		// it might change direction if we decide to strike bbox center
		var direction = (strikeTrace.EndPosition - position).Normal;

		MoveType = ProjectileMoveType.None;
		Position = strikeTrace.EndPosition;
		Rotation = Rotation.LookAt( direction );

		SetParent( entity, strikeTrace.Bone );
		return true;
	}

	public TraceResult TraceIntoSurface( Vector3 position, Vector3 direction )
	{
		direction = direction.Normal;

		var origin = position - direction * 32;
		var target = position + direction * 64;

		var tr = SetupCollisionTrace( origin, target, 0, 0 )
			.UseHitboxes( true )
			.Run();

		return tr;
	}

	public override string TrailParticleName => $"particles/nailtrails/nailtrails_medic_{Team.GetName()}.vpcf";
	public override string CriticalTrailParticleName => $"particles/nailtrails/nailtrails_medic_{Team.GetName()}_crit.vpcf";
}
