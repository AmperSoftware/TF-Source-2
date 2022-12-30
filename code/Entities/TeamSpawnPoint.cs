using Amper.FPS;
using Sandbox;
using Editor;
using System.Linq;

namespace TFS2;

/// <summary>
/// Spawn point marker for Team Fortress: Source 2 gamemode. 
/// </summary>
[Library( "tf_player_teamspawn" )]
[Title("Spawn Point")]
[Category("Gameplay")]
[Icon("meeting_room")]
[EditorModel( "models/editor/team_player_start.vmdl" )]
[DrawAngles]
[HammerEntity]
public partial class TeamSpawnPoint : SDKSpawnPoint
{
	/// <summary>
	/// The default team option for this spawn room. If "Associated Respawn Room" is set, it will 
	/// stay in sync with the value of that respawn room.
	/// </summary>
	[Property( "Team", Title = "Default Team" )]
	public HammerTFTeamOption DefaultTeamOption { get; set; }

	/// <summary>
	/// Respawn room this spawn point belongs to. The team value of the respawn room will be used for this spawn point.
	/// </summary>
	[Property, FGDType( "target_destination" )]
	public string AssociatedRespawnRoom { get; set; }

	public RespawnRoom Room { get; set; }
	public HammerTFTeamOption TeamOption { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		TeamOption = DefaultTeamOption;
	}

	[Event.Entity.PostSpawn]
	public void PostLevelSetup()
	{
		Room = FindByName( AssociatedRespawnRoom ) as RespawnRoom;

		if ( Room != null )
		{
			Room.AddSpawnPoint( this );
			TeamOption = Room.TeamOption;
		}
	}

	public override bool CanSpawn( SDKPlayer player )
	{
		var playerTeam = (TFTeam)player.TeamNumber;
		if ( !TeamOption.Is( playerTeam ) )
			return false;

		if ( Room != null && Room.ControlPoint != null )  
		{
			var point = Room.ControlPoint;
			if ( point != TFGameRules.Current.GetFarthestOwnedControlPointWithRespawnRoom( playerTeam ) )
				return false;
		}

		return true;
	}

	[Event.Tick.Server]
	public void Tick()
	{
		if ( !tf_debug_spawnpoints )
			return;

		DebugOverlay.Sphere( Position, 10, CanSpawn( All.OfType<TFPlayer>().First() ) ? Color.Green : Color.Red, 0, false );
		DebugOverlay.Line( Position, Position + Vector3.Up * 64, Color.Yellow );
		DebugOverlay.Line( Position, Position - Vector3.Up * 64, Color.Yellow );
		DebugOverlay.Text(
			$"Room Name: {AssociatedRespawnRoom}\n" +
			$"Room: {Room}\n" +
			$"TeamOption: {TeamOption}\n",
			Position + Vector3.Up * 50 
			);

	}

	[ConVar.Replicated] public static bool tf_debug_spawnpoints { get; set; }
}
