using Sandbox;
using Editor;
using Amper.FPS;
using System;

namespace TFS2;

[Library( "tf_func_respawnroomvisualizer" )]
[Title("Respawn Room Visualizer")]
[Category("Gameplay")]
[Icon( "fence" )]
[AutoApplyMaterial( "materials/overlays/no_entry.vmat" )]
[Solid]
[HammerEntity]
public partial class RespawnRoomVisualizer : ModelEntity
{
	[Property, FGDType( "target_destination" )]
	public string AssociatedRespawnRoom { get; set; }
	public RespawnRoom Room { get; set; }

	[Net] HammerTFTeamOption TeamOption { get; set; }

	[Event.Entity.PostSpawn]
	public void PostLevelSetup()
	{
		Room = FindByName( AssociatedRespawnRoom ) as RespawnRoom;

		if ( Room != null )
		{
			Room.AddVisualizer( this );
			SetTeamOption( Room.TeamOption );
		}
	}

	public override void Spawn()
	{
		base.Spawn();

		UsePhysicsCollision = true;
		Tags.Add( CollisionTags.PlayerClip );
		Tags.Add( TFCollisionTags.TeamBarrier );
	}

	public void SetTeamOption( HammerTFTeamOption option )
	{
		TeamOption = option;

		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( !team.IsPlayable() )
				continue;

			Tags.Set( $"team_barrier_{team.GetName()}", TeamOption.Is( team ) );
			// Log.Info( $"Visualizer (room \"{AssociatedRespawnRoom}\") accepts team \"{team}\": {TeamOption.Is( team )}" );
		}
	}

	[Event.Client.Frame]
	public void Frame()
	{
		EnableDrawing = IsVisibleForLocalPlayer();
	}

	public bool IsVisibleForLocalPlayer()
	{
		if ( TFGameRules.Current.AreRespawnRoomsOpen() ) 
			return false;

		var player = TFPlayer.LocalPlayer;
		if ( player == null )
			return false;

		return !TeamOption.Is( player.Team );
	}
}
