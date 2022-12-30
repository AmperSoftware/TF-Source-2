using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;
using Amper.FPS;

//
// TODO: Redo the scoreboard entirely.
// Current implementation is rushed and can be done a lot better.
//
namespace TFS2.UI;

[UseTemplate]
public partial class Scoreboard : Panel
{
	private Panel Container { get; set; }
	private Dictionary<TFTeam, Dictionary<IClient, Panel>> Players { get; set; } = new();
	private Dictionary<TFTeam, Panel> Lists { get; set; } = new();

	public Scoreboard()
	{
		EventDispatcher.Subscribe<PlayerRegenerateEvent>( OnRegenerate, this );
	}

	private Label BlueTeamScore { get; set; }
	private Label RedTeamScore { get; set; }
	private Label BluePlayerCount { get; set; }
	private Label RedPlayerCount { get; set; }
	private Label ServerTimeLeft { get; set; }
	private Panel BluePlayerList { get; set; }
	private Panel RedPlayerList { get; set; }
	private Image ClassImage { get; set; }
	private Label PlayerName { get; set; }
	private Label Kills { get; set; }
	private Label Deaths { get; set; }
	private Label Assists { get; set; }
	private Label Destruction { get; set; }
	private Label Captures { get; set; }
	private Label Defenses { get; set; }
	private Label Domination { get; set; }
	private Label Revenge { get; set; }
	private Label Invulns { get; set; }
	private Label Headshots { get; set; }
	private Label Teleports { get; set; }
	private Label Healing { get; set; }
	private Label Backstabs { get; set; }
	private Label Bonus { get; set; }
	private Label Support { get; set; }
	private Label Damage { get; set; }
	private Label MapName { get; set; }
	private Label ModeName { get; set; }
	private Image ModeLogo { get; set; }

	public override void Tick()
	{
		SetClass( "visible", Input.Down( InputButton.Score ) );

		if ( !IsVisible )
			return;

		Lists[TFTeam.Blue] = BluePlayerList;
		Lists[TFTeam.Red] = RedPlayerList;

		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( !Players.ContainsKey( team ) )
				Players[team] = new();

			var teamClients = Sandbox.Game.Clients.Where( x => x.GetTeam() == team );

			foreach ( var client in teamClients.Except( Players[team].Keys ) )
				AddClient( client, team );

			foreach ( var client in Players[team].Keys.Except( teamClients ) )
				RemoveClient( client, team );
		}
		UpdateModeInfo();
		UpdatePlayerPreview();

		#region Stats

		var cl = Sandbox.Game.LocalClient;
		Kills.Text = cl.GetKills().ToString();
		Deaths.Text = cl.GetDeaths().ToString();
		Assists.Text = cl.GetAssists().ToString();
		Destruction.Text = cl.GetDestructions().ToString();
		Captures.Text = cl.GetCaptures().ToString();
		Defenses.Text = cl.GetDefenses().ToString();
		Domination.Text = cl.GetDominations().ToString();
		Revenge.Text = cl.GetRevenges().ToString();
		Invulns.Text = cl.GetInvulns().ToString();
		Headshots.Text = cl.GetHeadshots().ToString();
		Teleports.Text = cl.GetTeleports().ToString();
		Healing.Text = cl.GetHealing().ToString();
		Backstabs.Text = cl.GetBackstabs().ToString();
		Bonus.Text = cl.GetBonus().ToString();
		Support.Text = cl.GetSupport().ToString();
		Damage.Text = cl.GetDamage().ToString();

		#endregion Stats
	}

	public void AddClient( IClient client, TFTeam team )
	{
		if ( team.IsPlayable() )
		{
			Players[team][client] = new ScoreboardPlayerEntry
			{
				Client = client,
				Parent = Lists[team]
			};
		}
	}

	public void RemoveClient( IClient client, TFTeam team )
	{
		if ( Players[team].TryGetValue( client, out var row ) )
		{
			row?.Delete();
			Players[team].Remove( client );
		}
	}

	private void OnRegenerate( PlayerRegenerateEvent args )
	{
		if ( args.Client != Sandbox.Game.LocalClient )
			return;

		UpdatePlayerPreview();
	}

	public void UpdatePlayerPreview()
	{
		MapName.Text = $"Playing on {Util.GetMapDisplayName( Sandbox.Game.Server.MapIdent )}";

		var player = TFPlayer.LocalPlayer;
		if ( player == null )
			return;

		var team = player.Team;
		var playerClass = player.PlayerClass;

		PlayerName.Text = Sandbox.Game.UserName;
		PlayerName.Style.Set( "color", team == TFTeam.Red ? "#ea5251" : "#9ccbf5" );

		if ( playerClass != null )
			ClassImage.SetTexture( $"/ui/hud/class/{playerClass.ResourceName}_{team}.png" );
	}

	/// <summary>
	/// Updates the game mode icon and text seen on the scoreboard.
	/// </summary>
	public void UpdateModeInfo()
	{
		// Set the game mode label.

		if ( !TFGameRules.Current.GameType.ToString().Equals( "None" ) )
		{
			ModeName.Text = TFGameRules.Current.GameType switch
			{
				TFGameType.CaptureTheFlag => "Capture The Flag",
				TFGameType.ControlPoints => "Control Points",
				TFGameType.TeamDeathmatch => "Team Deathmatch",
				TFGameType.KingOfTheHill => "King of the Hill",
				_ => TFGameRules.Current.GameType.ToString()
			};
		}
		else ModeName.Text = string.Empty;

		// Set the game mode icon.
		if ( TFGameRules.Current.GameType == TFGameType.KingOfTheHill )
			ModeLogo.SetTexture( "/ui/hud/scoreboard/icon_mode_koth.png" );
		else if ( TFGameRules.Current.GameType == TFGameType.Payload )
			ModeLogo.SetTexture( "/ui/hud/scoreboard/icon_mode_payload.png" );
		else if ( TFGameRules.Current.GameType == TFGameType.ControlPoints )
			ModeLogo.SetTexture( "/ui/hud/scoreboard/icon_mode_control.png" );
		else
			ModeLogo.SetTexture( "/ui/icons/empty.png" );
	}
}

public class ScoreboardPlayerEntry : Panel
{
	public IClient Client { get; set; }
	private TimeSince TimeSinceUpdate { get; set; }
	private Image Avatar { get; set; }
	private Label Name { get; set; }
	private Image ClassIcon { get; set; }
	private Label Ping { get; set; }

	public ScoreboardPlayerEntry()
	{
		Avatar = Add.Image( $"", "avatar" );
		Name = Add.Label( "", "name" );
		ClassIcon = Add.Image( $"", "classicon" );
		Ping = Add.Label( "0", "ping" );
	}

	public override void Tick()
	{
		base.Tick();
		if ( Client == null || !Client.IsValid() )
			return;

		Avatar.SetTexture( $"avatar:{Client.SteamId}" );
		Name.Text = Client.Name;
		Ping.Text = $"{Client.Ping}";
		TimeSinceUpdate = 0;
	}
}
