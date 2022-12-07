using Sandbox;
using Amper.FPS;
using System;

namespace TFS2;

partial class TFGameMovement
{
	public override void WalkMove()
	{
		var oldGround = Player.GroundEntity;

		var vecForward = Input.AnalogMove.Forward;
		var vecRight = Input.AnalogMove.Right;

		// Get the movement angles.
		vecForward = vecForward.WithZ( 0 );
		vecRight = vecRight.WithZ( 0 );

		vecForward = vecForward.Normal;
		vecRight = vecRight.Normal;

		// Copy movement amounts
		float flForwardMove = ForwardMove;
		float flSideMove = SideMove;

		// Find the direction,velocity in the x,y plane.
		var vecWishDirection = new Vector3(
			(vecForward.x * flForwardMove) + (vecRight.x * flSideMove),
			(vecForward.y * flForwardMove) + (vecRight.y * flSideMove),
			0.0f
		);

		// Calculate the speed and direction of movement, then clamp the speed.
		float flWishSpeed = vecWishDirection.Length;
		vecWishDirection = vecWishDirection.Normal;
		flWishSpeed = Math.Clamp( flWishSpeed, 0.0f, MaxSpeed );

		// Accelerate in the x,y plane.
		Velocity = Velocity.WithZ( 0 );

		float flAccelerate = sv_accelerate;

		// if our wish speed is too low (attributes), we must increase acceleration or we'll never overcome friction
		// Reverse the basic friction calculation to find our required acceleration
		var wishspeedThreshold = 100 * sv_friction / sv_accelerate;
		if ( flWishSpeed > 0 && flWishSpeed < wishspeedThreshold )
		{
			float speed = Velocity.Length;
			float flControl = (speed < sv_stopspeed) ? sv_stopspeed : speed;
			flAccelerate = (flControl * sv_friction) / flWishSpeed + 1;
		}

		Accelerate( vecWishDirection, flWishSpeed, flAccelerate );

		// Clamp the players speed in x,y.
		float flNewSpeed = Velocity.Length;
		if ( flNewSpeed > MaxSpeed )
		{
			float flScale = MaxSpeed / flNewSpeed;
			Velocity = Velocity.WithX( Velocity.x * flScale ).WithY( Velocity.y * flScale );
		}

		// Now reduce their backwards speed to some percent of max, if they are traveling backwards
		// unless they are under some minimum, to not penalize deployed snipers or heavies
		if ( tf_clamp_back_speed < 1 && Velocity.Length > tf_clamp_back_speed_min )
		{
			float flDot = Vector3.Dot( vecForward, Velocity );

			// are we moving backwards at all?
			if ( flDot < 0 )
			{
				var vecBackMove = vecForward * flDot;
				var vecRightMove = vecRight * Vector3.Dot( vecRight, Velocity );

				// clamp the back move vector if it is faster than max
				float flBackSpeed = vecBackMove.Length;
				float flMaxBackSpeed = MaxSpeed * tf_clamp_back_speed;

				if ( flBackSpeed > flMaxBackSpeed )
				{
					vecBackMove *= flMaxBackSpeed / flBackSpeed;
				}

				// reassemble velocity	
				Velocity = vecBackMove + vecRightMove;

				// Re-run this to prevent crazy values (clients can induce this via usercmd viewangles hacking)
				flNewSpeed = Velocity.Length;
				if ( flNewSpeed > MaxSpeed )
				{
					float flScale = MaxSpeed / flNewSpeed;
					Velocity = Velocity.WithX( Velocity.x * flScale ).WithY( Velocity.y * flScale );
				}
			}
		}

		Velocity += Player.BaseVelocity;

		// Calculate the current speed and return if we are not really moving.
		if ( Velocity.Length < 1 )
		{
			Velocity = 0;
			Velocity -= Player.BaseVelocity;
			return;
		}

		// Calculate the destination.
		Vector3 vecDestination = Position + Velocity * Time.Delta;
		vecDestination = vecDestination.WithZ( Position.z );

		// Try moving to the destination.
		var trace = TraceBBox( Position, vecDestination );
		if ( trace.Fraction == 1.0f )
		{
			// Made it to the destination (remove the base velocity).
			Position = trace.EndPosition;
			Velocity -= Player.BaseVelocity;
			StayOnGround();
			return;
		}

		if ( !oldGround.IsValid() && Player.WaterLevelType == WaterLevelType.NotInWater )
		{
			Velocity -= Player.BaseVelocity;
			return;
		}

		// Now try and do a step move.
		StepMove();
		Velocity -= Player.BaseVelocity;

		StayOnGround();
	}
}
