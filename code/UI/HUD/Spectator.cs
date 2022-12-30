using Sandbox;
using Sandbox.UI;
using Amper.FPS;

namespace TFS2.UI;

[UseTemplate]
internal partial class Spectator : Panel
{
	private Label Timer { get; set; }
	private Label MapName { get; set; }
	private Label Target { get; set; }

	public Spectator()
	{
		MapName.Text = Util.GetMapDisplayName( Sandbox.Game.Server.MapIdent );
	}

	public override void Tick()
	{
		base.Tick();
		SetClass( "visible", ShouldDraw() );

		if ( !IsVisible )
			return;

		if ( Sandbox.Game.LocalPawn is not TFPlayer player )
			return;

		UpdateRespawnTimeLabel( player );
		UpdateObserverLabel( player );
	}

	/// <summary>
	/// Update the respawn timer.
	/// </summary>
	public void UpdateRespawnTimeLabel( TFPlayer player )
	{
		if ( player.Team.IsPlayable() )
		{
			if ( TFGameRules.Current.State == GameState.RoundEnd )
			{
				Timer.Text = "You will be respawned next round.";
				return;
			}

			if ( !TFGameRules.Current.AreRespawnsAllowed() )
			{
				Timer.Text = "Respawns are not allowed right now.";
				return;
			}

			if ( !TFGameRules.Current.CanTeamRespawn( player.Team ) )
			{
				Timer.Text = "Your team cannot respawn right now.";
				return;
			}

			if ( !TFGameRules.Current.CanPlayerRespawn( player ) )
			{
				Timer.Text = "You cannot respawn at the moment.";
				return;
			}

			// Update the respawn timer.
			var respawnTime = TFGameRules.Current.GetNextPlayerRespawnWaveTime( player ) - Time.Now;
			Timer.Text = (respawnTime <= 0) ? "Prepare to respawn." : $"Respawning in: {respawnTime.CeilToInt()} second(s).";
			return;
		}

		Timer.Text = "Spectator Mode";
		return;
	}

	/// <summary>
	/// Update the observer state.
	/// </summary>
	public void UpdateObserverLabel( TFPlayer player )
	{
		if ( player.IsObserver )
		{
			var target = player.ObserverTarget;
			var showTargetName = false;
			var label = "";

			switch ( player.ObserverMode )
			{
				case ObserverMode.Roaming:
					label = "Free roam mode";
					break;

				case ObserverMode.Chase:
					label = "Third person view";
					showTargetName = true;
					break;

				case ObserverMode.InEye:
					label = "First person view";
					showTargetName = true;
					break;
			}

			if ( showTargetName && target != null && target.Client != null )
				label += $" ( {target.Client.Name} )";

			Target.Text = label;
			return;
		}

		Target.Text = "";
		return;
	}

	/// <summary>
	/// Determines if the element should be displayed on screen.
	/// </summary>
	public bool ShouldDraw()
	{
		var player = TFPlayer.LocalPlayer;

		if ( !player.IsValid() )
			return false;

		return player.IsSpectating;
	}
}
