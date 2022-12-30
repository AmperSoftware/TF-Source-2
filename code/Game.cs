using Sandbox;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Amper.FPS;
using TFS2.UI;

namespace TFS2;

[Display( Name = "Team Fortress" )]
public partial class TFGameRules : SDKGame
{
	public static new TFGameRules Current { get; set; }

	public TFGameRules()
	{
		Current = this;
		Movement = new TFGameMovement();

		if ( Game.IsServer )
		{
			_ = new TFHud();
		}

		if ( Game.IsClient )
		{
			PostProcessingManager = new TFPostProcessingManager();
		}
	}

	public override void Tick()
	{
		base.Tick();

		if ( Game.IsServer )
			TickRespawnWaves();
	}

	public override void ClientJoined( IClient client )
	{
		var player = new TFPlayer();
		player.Respawn();
		client.Pawn = player;

		ShowServerMessage( To.Single( client ) );

		// send this to everyone's chat
		TFChatBox.AddInformation( To.Everyone, $"{client.Name} has joined the game" );

		//
		// TODO:
		// Delete this when we have NextBots
		//

		if ( client.IsBot )
		{
			player.PlayerClass = PlayerClass.Get( tf_bot_force_class );
			player.ChangeTeam( player.GetAutoTeam(), true, false );
		}

	}

	[ConVar.Replicated] public static TFPlayerClass tf_bot_force_class { get; set; } = TFPlayerClass.Scout;


	public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( client, reason );

		// send this to everyone's chat
		TFChatBox.AddInformation( To.Everyone, $"{client.Name} has left the game" );
	}

	public virtual void PlayerChangeClass( TFPlayer player, PlayerClass pclass )
	{
		if ( !Game.IsServer )
			return;

		// Call a game event.
		EventDispatcher.InvokeEvent( new PlayerChangeClassEvent
		{
			Client = player.Client,
			Class = pclass
		} );
	}

	public virtual void PlayerRegenerate( TFPlayer player, bool full )
	{
		if ( !Game.IsServer )
			return;

		// Call a game event.
		EventDispatcher.InvokeEvent( new PlayerRegenerateEvent
		{
			Client = player.Client
		} );
	}

	bool FirstBloodAnnounced { get; set; }


	//private Dictionary<TFPlayer, int> recentDamage = new();
	//const int scoreDamage = 600;

	//
	// UI Panels
	//

	[ClientRpc] public void ShowTeamSelectionMenu() { MenuOverlay.Open<TeamSelection>(); }
	[ClientRpc] public void ShowClassSelectionMenu() { MenuOverlay.Open<ClassSelection>(); }
	[ClientRpc] public void ShowServerMessage() { MenuOverlay.Open<ServerMessage>(); }

	TFTimer WaitingForPlayersTimer { get; set; }

	public override void OnWaitingForPlayersStarted()
	{
		WaitingForPlayersTimer = new()
		{
			Name = "@timer_waiting_for_players",
			AbsoluteTime = WaitingForPlayersTime,
			Paused = false
		};
	}

	public override void OnWaitingForPlayersEnded()
	{
		WaitingForPlayersTimer?.Delete();
	}

	public override void OnChatMessageSent( IClient sender, string message, ChatType type )
	{
		base.OnChatMessageSent( sender, message, type );

		var clients = Game.Clients;

		switch ( type )
		{
			case ChatType.Team:
				clients = clients.Where( x => ITeam.IsSame( x.Pawn, sender.Pawn ) ).ToArray();
				break;
		}

		TFChatBox.AddClientMessage( To.Multiple( clients ), sender, message, type );
	}

	public override Trace SetupSpawnTrace( SDKPlayer player, Vector3 from, Vector3 to, Vector3 mins, Vector3 maxs )
	{
		return base.SetupSpawnTrace( player, from, to, mins, maxs )
			.WithoutTags( TFCollisionTags.TeamBarrier );
	}
}

public enum TFGameType
{
	None,

	// Derived from objective ents
	CaptureTheFlag,
	ControlPoints,
	Payload,

	// Derived from logic entities
	Arena,
	TeamDeathmatch,
	KingOfTheHill
}

public enum TFWinReason
{
	None,
	AllPointsCaptured,
	OpponentsDead,
	FragLimit,
	FlagCaptureLimit,
	DefendUntilTimeLimit,
	Stalemate,
	Timelimit,
	Winlimit
}

public enum TFTeamRole
{
	None,
	Defenders,
	Attackers
}

public static class TFCollisionTags
{
	public const string TeamBarrier = "team_barrier";
}
