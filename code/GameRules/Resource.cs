using Sandbox;
using Amper.FPS;

namespace TFS2;

partial class TFGameRules
{
	public override void UpdateClientData(IClient client, SDKPlayer player )
	{
		base.UpdateClientData( client, player );

		if ( player is TFPlayer pawn )
			UpdateClientData( client, pawn );
	}

	public void UpdateClientData( IClient client, TFPlayer player )
	{
		var playerClass = "";
		if ( player.PlayerClass != null )
			playerClass = player.PlayerClass.ResourcePath;

		client.SetValue( "s_playerclass", playerClass );

		#region Stats
		client.SetInt( "i_points", player.GetPoints() );
		client.SetInt( "i_kills", player.Kills );
		client.SetInt( "i_deaths", player.Deaths );
		client.SetInt( "i_assists", player.Assists );
		client.SetInt( "i_destructions", player.Destructions );
		client.SetInt( "i_captures", player.Captures );
		client.SetInt( "i_defenses", player.Defenses );
		client.SetInt( "i_dominations", player.Dominations );
		client.SetInt( "i_revenges", player.Revenges );
		client.SetInt( "i_invulns", player.Invulns );
		client.SetInt( "i_headshots", player.Headshots );
		client.SetInt( "i_teleports", player.Teleports );
		client.SetInt( "i_healing", player.Healing );
		client.SetInt( "i_backstabs", player.Backstabs );
		client.SetInt( "i_bonus", player.Bonus );
		client.SetInt( "i_support", player.Support );
		client.SetInt( "i_damage", player.DamageScore );
		#endregion
	}
}

public static class ClientExtensions
{
	public static float GetHealth( this IClient client ) => client.GetValue<float>( "f_health" );
	public static bool IsAlive( this IClient client ) => client.GetValue<bool>( "b_alive" );
	public static TFTeam GetTeam( this IClient client ) => (TFTeam)client.GetValue<int>( "n_teamnumber" );
	public static PlayerClass GetPlayerClass( this IClient client ) => ResourceLibrary.Get<PlayerClass>( client.GetValue<string>( "s_playerclass" ) );

	#region Stats
	public static int GetPoints( this IClient client ) => client.GetInt( "i_points" );
	public static int GetKills( this IClient client ) => client.GetInt( "i_kills" );
	public static int GetDeaths( this IClient client ) => client.GetInt( "i_deaths" );
	public static int GetAssists( this IClient client ) => client.GetInt( "i_assists" );
	public static int GetDestructions( this IClient client ) => client.GetInt( "i_destructions" );
	public static int GetCaptures( this IClient client ) => client.GetInt( "i_captures" );
	public static int GetDefenses( this IClient client ) => client.GetInt( "i_defenses" );
	public static int GetDominations( this IClient client ) => client.GetInt( "i_dominations" );
	public static int GetRevenges( this IClient client ) => client.GetInt( "i_revenges" );
	public static int GetInvulns( this IClient client ) => client.GetInt( "i_invulns" );
	public static int GetHeadshots( this IClient client ) => client.GetInt( "i_headshots" );
	public static int GetTeleports( this IClient client ) => client.GetInt( "i_teleports" );
	public static int GetHealing( this IClient client ) => client.GetInt( "i_healing" );
	public static int GetBackstabs( this IClient client ) => client.GetInt( "i_backstabs" );
	public static int GetBonus( this IClient client ) => client.GetInt( "i_bonus" );
	public static int GetSupport( this IClient client ) => client.GetInt( "i_support" );
	public static int GetDamage( this IClient client ) => client.GetInt( "i_damage" );
	#endregion
}
