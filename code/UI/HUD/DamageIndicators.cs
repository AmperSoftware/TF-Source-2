using Sandbox;
using Sandbox.UI;
using Amper.FPS;
using System;

namespace TFS2.UI;

public class DamageIndicators : Panel
{
	public DamageIndicators()
	{
		StyleSheet.Load( "/ui/hud/DamageIndicators.scss" );
		EventDispatcher.Subscribe<PlayerHurtEvent>( OnHurt, this );
	}

	public void OnHurt( PlayerHurtEvent args )
	{
		if ( args.Victim != Sandbox.Game.LocalClient )
			return;

		// If local client is not alive
		if ( !Sandbox.Game.LocalClient.IsAlive() )
			return;

		AddChild( new DamageIndicatorEntry( (args.Position - Sandbox.Game.LocalPawn.GetEyePosition()).Normal, args.Damage ) );
	}
}

public class DamageIndicatorEntry : Panel
{
	Vector3 Offset { get; set; }
	float Damage { get; set; }
	TimeSince TimeSinceCreated { get; set; }
	float Deviation { get; set; }

	public DamageIndicatorEntry( Vector3 direction, float damage )
	{
		TimeSinceCreated = 0;
		Deviation = Sandbox.Game.Random.Float( -3, 3 );
	}

	public override void Tick()
	{
		if ( Sandbox.Game.LocalPawn is not TFPlayer pawn ) return;

		float time = TimeSinceCreated;
		var origin = pawn.EyePosition + Offset;

		// rotation
		var vecFromEyes = pawn.EyeRotation.Forward.WithZ( 0 ).Normal;
		var vecToOrigin = (origin - pawn.EyePosition).WithZ( 0 ).Normal;

		float radFromEyes = MathF.Atan2( vecFromEyes.x, vecFromEyes.y );
		float radToOrigin = MathF.Atan2( vecToOrigin.x, vecToOrigin.y );

		var deg = (radToOrigin - radFromEyes) * 180 / MathF.PI + Deviation;
		Style.Set( "transform", $"rotate({deg}deg)" );

		// height
		float width = Damage * 2.5f;
		width = Math.Clamp( width, 40, 180 );
		Style.Set( "width", $"{width}px" );

		// width
		float height = 900 - time.LerpInverse( 0, 0.2f ) * 100;
		Style.Set( "height", $"{height}px" );

		// opacity
		float opacity = time.LerpInverse( 0, .2f ) - time.LerpInverse( 2f, 3f );
		Style.Opacity = opacity;
		Style.Set( "opacity", $"{opacity}" );
	}
}
