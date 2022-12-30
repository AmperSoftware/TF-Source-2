using Sandbox;
using Editor;

namespace TFS2;

/// <summary>
/// Filter to only allow players of one specific team. 
/// </summary>
[Library( "tf_filter_activator_team" )]
[Title( "Team Filter" )]
[Category( "Filters" )]
[Icon("group")]
[HammerEntity]
partial class FilterTeam : Filter
{
	/// <summary>
	/// Filter out all other teams except this one.
	/// </summary>
	[Property] public HammerTFTeamOption Team { get; set; }

	public override bool Test( Entity entity )
	{
		var player = entity as TFPlayer;

		// only accepts players.
		if ( player == null )
			return false;

		if ( TFGameRules.Current.AreRespawnRoomsOpen() )
			return true;

		return Team.Is( player.Team );
	}
}
