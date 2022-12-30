using Sandbox;
using Amper.FPS;
using System.Linq;

namespace TFS2;

partial class TFPlayer
{
	[ConCmd.Admin( "tf_give_ammo" )]
	private static void Command_GiveAmmo( int count )
	{
		if ( ConsoleSystem.Caller.Pawn is TFPlayer player )
		{
			if ( player.ActiveWeapon is TFWeaponBase weapon )
				weapon.Reserve += count;
		}
	}

	[ConCmd.Admin( "tf_regenerate" )]
	public static void Command_Regenerate()
	{
		(ConsoleSystem.Caller.Pawn as TFPlayer)?.Regenerate();
	}

	[ConCmd.Admin( "tf_deleteweapons" )]
	public static void Command_DeleteWeapons()
	{
		(ConsoleSystem.Caller.Pawn as TFPlayer)?.DeleteAllWeapons();
	}

	[ConCmd.Admin( "hurtme" )]
	public static void Command_HurtMe( int damage = 10 )
	{
		if ( ConsoleSystem.Caller.Pawn is TFPlayer player )
		{
			var dmgInfo = ExtendedDamageInfo.Create( damage )
				.WithAttacker( player )
				.WithInflictor( player )
				.WithAllPositions( player.Position )
				.WithFlag( TFDamageFlags.PreventPhysicsForce );

			player.TakeDamage( dmgInfo );
		}
	}

	[ConCmd.Admin( "burn" )]
	public static void Command_Burn( int time = 5 )
	{
		if ( ConsoleSystem.Caller.Pawn is TFPlayer player )
			player.Burn( player, null, time );
	}

	[ConCmd.Admin( "bot_voicecommand" )]
	public static void Command_BotCommand( string botname, int menu = -1, int concept = -1 )
	{
		var bot = Game.Clients.FirstOrDefault( x => x.IsBot && x.Name == botname );
		if ( bot == null )
		{
			Log.Error( $"Bot named \"{botname}\" was not found" );
			return;
		}

		var botPlayer = bot.Pawn as TFPlayer;
		if ( !botPlayer.IsValid() )
			return;

		if ( menu < 0 ) menu = Game.Random.Int( 0, 2 );
		if ( concept < 0 ) concept = Game.Random.Int( 0, 8 );

		botPlayer.PlayVoiceCommand( menu, concept );
	}
}
