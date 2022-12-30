using Sandbox;
using Sandbox.UI;
using Amper.FPS;

namespace TFS2.UI;

[UseTemplate]
partial class Alert : Panel
{
	public static Alert Instance { get; set; }

	Label Text { get; set; }
	Image ImagePanel { get; set; }
	TimeSince TimeSinceAppeared { get; set; }
	float ShowTime { get; set; }

	public Alert()
	{
		Instance = this;
	}

	public override void Tick()
	{
		base.Tick();
		SetClass( "visible", ShouldDraw() );
	}

	public bool ShouldDraw()
	{
		if ( SDKGame.Current.State == GameState.RoundEnd )
			return false;

		return TimeSinceAppeared < ShowTime;
	}

	public void Setup( string message, string icon, float time = 5, TFTeam team = TFTeam.Unassigned )
	{
		TimeSinceAppeared = 0;
		ShowTime = time;
		Text.Text = message;
		ImagePanel.SetTexture( icon );

		// If we're unassigned, use our current player's team.
		if ( team == TFTeam.Unassigned )
		{
			if ( Sandbox.Game.LocalPawn is TFPlayer player )
				team = player.Team;
		}

		SetClass( "red", team == TFTeam.Red );
		SetClass( "blue", team == TFTeam.Blue );
	}

	[ClientRpc]
	public static void Show( string message, string icon, float time = 5, TFTeam team = TFTeam.Unassigned )
	{
		Instance?.Setup( message, icon, time, team );
	}
}
