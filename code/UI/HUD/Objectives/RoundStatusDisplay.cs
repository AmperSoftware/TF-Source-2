using Sandbox;
using Sandbox.UI;
using Amper.FPS;
using System.Collections.Generic;
using System.Linq;

namespace TFS2.UI;

[UseTemplate]
public partial class RoundStatusDisplay : Panel
{
	Label GameStateLabel { get; set; }
	Panel TimersContainer { get; set; }
	Dictionary<TFTimer, RoundStatusTimerEntry> Timers { get; set; } = new();

	public override void Tick()
	{
		SetClass( "visible", ShouldDraw() );
		if ( !IsVisible )
			return;

		UpdateGameStateLabel();
		UpdateTimers();
	}

	public bool ShouldDraw()
	{
		return !Input.Down( InputButton.Score );
	}

	public void UpdateGameStateLabel()
	{
		string value = "";

		// Show a message for waiting for players, unless we play arena (it has it's own message)
		if ( SDKGame.Current.IsWaitingForPlayers && !TFGameRules.Current.IsPlayingArena )
			value = $"Waiting For Players";

		GameStateLabel.Text = value;
	}

	public void UpdateTimers()
	{
		var timers = Timer.All.OfType<TFTimer>().Where( x => x.IsVisibleOnHUD );
		var keys = Timers.Keys;

		foreach ( var entry in timers.Except( keys ) ) AddTimer( entry );
		foreach ( var entry in keys.Except( timers ) ) RemoveTimer( entry );
	}

	public void AddTimer( TFTimer timer )
	{
		Timers[timer] = new RoundStatusTimerEntry
		{
			Timer = timer,
			Parent = TimersContainer
		};
		ReorderTimers();
	}

	public void RemoveTimer( TFTimer timer )
	{
		if ( Timers.TryGetValue( timer, out var entry ) )
		{
			entry?.Delete();
			Timers.Remove( timer );
			ReorderTimers();
		}
	}

	public void ReorderTimers()
	{
		TimersContainer.SortChildren( ( x, y ) =>
		{
			if ( x is not RoundStatusTimerEntry entry1 || y is not RoundStatusTimerEntry entry2 )
				return 0;

			var timer1 = entry1.Timer;
			var timer2 = entry2.Timer;
			if ( timer1 == null || timer2 == null )
				return 0;

			// Sort in DESCENDING order.
			return timer1.OwnerTeam < timer2.OwnerTeam ? 1 : -1;
		} );
	}
}

class RoundStatusTimerEntry : Label
{
	public TFTimer Timer { get; set; }

	public override void Tick()
	{
		if ( Timer == null )
			return;

		Text = Timer.GetTimeString();
		SetClass( "active", !Timer.Paused );
	}
}

class RoundStatusPlayers : Panel
{
	public TFTeam Team { get; set; }
	public Dictionary<IClient, RoundStatusPlayersEntry> Players { get; set; } = new();

	public RoundStatusPlayers()
	{
		BindClass( "red", () => Team == TFTeam.Red );
		BindClass( "blue", () => Team == TFTeam.Blue );
	}

	public override void Tick()
	{
		var teamClients = Sandbox.Game.Clients.Where( x => x.GetTeam() == Team );
		var keyClients = Players.Keys;

		foreach ( var client in teamClients.Except( keyClients ) ) AddClient( client );
		foreach ( var client in keyClients.Except( teamClients ) ) RemoveClient( client );
	}

	public void AddClient( IClient client )
	{
		Players[client] = new RoundStatusPlayersEntry
		{
			Client = client,
			Parent = this
		};
	}

	public void RemoveClient( IClient client )
	{
		if ( Players.TryGetValue( client, out var entry ) )
		{
			entry?.Delete( true );
			Players.Remove( client );
		}
	}
}

public class RoundStatusPlayersEntry : Panel
{
	public IClient Client { get; set; }
	Panel Portrait { get; set; }

	public RoundStatusPlayersEntry()
	{
		Portrait = Add.Panel( "portrait" );
	}

	public override void Tick()
	{
		var local = Sandbox.Game.LocalClient;
		if ( local == null )
			return;

		var ourTeam = local.GetTeam();
		var theirTeam = Client.GetTeam();
		var anonymous = true;

		if ( ourTeam == theirTeam )
		{
			var theirClass = Client.GetPlayerClass();
			if ( theirClass != null )
			{
				anonymous = false;
				Portrait.Style.Set( "background-image", $"url(/ui/hud/classportraits/{theirClass.Title}_{theirTeam.GetName()}.png)" );
			}
		}

		if ( anonymous )
			Portrait.Style.Set( "background-image", $"url(/ui/hud/classportraits/silhouette.png)" );

		Portrait.SetClass( "dead", !Client.IsAlive() );
		Portrait.SetClass( "us", local == Client );
	}
}
