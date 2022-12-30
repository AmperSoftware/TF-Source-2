using Sandbox;
using System.Linq;

namespace TFS2;

public partial class TFPlayer
{
	public TFTeam Team => (TFTeam)TeamNumber;

	public bool IsEnemy( TFPlayer player ) => Team != player.Team;
	public bool IsAlly( TFPlayer player ) => Team == player.Team;

	public bool ChangeTeam( TFTeam team, bool autoTeam, bool silent, bool autoBalance = false )
	{
		return ChangeTeam( (int)team, autoTeam, silent, autoBalance );
	}

	public TFTeam GetAutoTeam( TFTeam preferredTeam = TFTeam.Unassigned )
	{
		var players = All.OfType<TFPlayer>();
		var redPlayersCount = players.Where( x => x.Team == TFTeam.Red ).Count();
		var bluePlayersCount = players.Where( x => x.Team == TFTeam.Blue ).Count();

		// blue has less players than red.
		if ( bluePlayersCount < redPlayersCount )
			return TFTeam.Blue;

		// red has less players than blue.
		if ( redPlayersCount < bluePlayersCount )
			return TFTeam.Red;

		// AutoTeam should give new players to the attackers on A/D maps if the teams are even
		if ( TFGameRules.Current.IsAttackDefenseGameType() )
			return TFTeam.Blue;

		// we don't have a preferred team, pick a random one.
		if ( preferredTeam == TFTeam.Unassigned )
			return Game.Random.Int( 0, 1 ) == 1 ? TFTeam.Red : TFTeam.Blue;

		// return our preference.
		return preferredTeam;
	}

	[ConCmd.Server( "tf_join_team" )]
	public static void Command_SetTeam( TFTeam team )
	{
		var player = ConsoleSystem.Caller.Pawn as TFPlayer;
		if ( player == null ) 
			return;

		//
		// Auto Team
		//

		var autoTeamed = false;
		if ( team == TFTeam.Unassigned )
		{
			team = player.GetAutoTeam();
			autoTeamed = true;
		}

		// Actually change the team.
		if ( !player.ChangeTeam( team, autoTeamed, false ) )
			return;

		// If player joined a playable team, show them class selection menu.
		if ( team.IsPlayable() )
		{
			TFGameRules.Current.ShowClassSelectionMenu( To.Single( player.Client ) );
		}
	}
}
