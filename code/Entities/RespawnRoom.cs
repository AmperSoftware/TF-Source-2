using Sandbox;
using Editor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace TFS2;

[Library( "tf_func_respawnroom" )]
[Title("Respawn Room")]
[Category("Gameplay")]
[Icon("Home")]
[HammerEntity]
public partial class RespawnRoom : BaseTrigger
{
	[Property( "Team", Title = "Default Team" )] 
	public HammerTFTeamOption DefaultTeamOption { get; set; }

	/// <summary>
	/// The tf_control_point associated with this respawn room. Ownership of control points will control this spawn point's enabled state.
	/// </summary>
	[Property, FGDType( "target_destination" )]
	public string AssociatedControlPoint { get; set; }

	List<RespawnRoomVisualizer> Visualizers { get; set; } = new();
	List<TeamSpawnPoint> SpawnPoints { get; set; } = new();

	public ControlPoint ControlPoint { get; set; }
	public HammerTFTeamOption TeamOption { get; set; }

	public bool IsInside( TFPlayer ply ) => TouchingEntities.Contains( ply );

	public override void Spawn()
	{
		base.Spawn();
		TeamOption = DefaultTeamOption;
	}

	[Event.Entity.PostSpawn]
	public void PostLevelSetup()
	{
		ControlPoint = FindByName( AssociatedControlPoint ) as ControlPoint;
	}

	public override bool PassesTriggerFilters( Entity other )
	{
		if ( other is TFPlayer ply )
			return TeamOption.Is( ply.Team );

		return false;
	}

	public void AddVisualizer( RespawnRoomVisualizer visualizer )
	{
		Visualizers.Add( visualizer );
	}

	public void AddSpawnPoint( TeamSpawnPoint point )
	{
		SpawnPoints.Add( point );
	}

	/// <summary>
	/// If player is inside their team's respawn room.
	/// </summary>
	public static bool IsInsideTeamRoom( TFPlayer player )
	{
		foreach ( var room in All.OfType<RespawnRoom>() )
		{
			if ( room.IsInside( player ) )
				return true;

		}
		return false;
	}

	[Event.Tick.Server]
	public void Tick()
	{
		if ( !TeamSpawnPoint.tf_debug_spawnpoints )
			return;

		DebugOverlay.Text( 
			$"TeamOption: {TeamOption}\n" +
			$"ControlPoint: {ControlPoint}\n" +
			$"Visualizers: {Visualizers.Count}\n" +
			$"Spawn Points: {SpawnPoints.Count}",
			WorldSpaceBounds.Center );

	}
}
