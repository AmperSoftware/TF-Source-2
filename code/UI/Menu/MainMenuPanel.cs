using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

[UseTemplate]
public class MainMenuPanel : Panel
{
	public TimeSince TimeSinceInteraction { get; set; }

	Label PlayerName { get; set; }
	Image PlayerAvatar { get; set; }
	bool Enabled { get; set; }

	public override void Tick()
	{
		if ( TimeSinceInteraction > 0.1f )
		{
			if ( Input.Pressed( InputButton.Menu ) )
			{
				TimeSinceInteraction = 0;
				Toggle();
			}
		}

		if ( !IsVisible )
			return;

		PlayerName.Text = Sandbox.Game.LocalClient.Name;
		PlayerAvatar.SetTexture( $"avatarbig:{Sandbox.Game.LocalClient.SteamId}" );
	}

	public void OnClickResumeGame()
	{
		Hide();
	}

	public void OnClickJoinGame()
	{
		MenuOverlay.Open<JoinGameDialog>();
	}

	public void OnClickSettings()
	{
		MenuOverlay.Open<Settings>();
	}

	public void OnClickLoadout()
	{
		MenuOverlay.Open<ClassLoadout>();
	}

	public void OnClickQuit()
	{
		MenuOverlay.Open<QuitDialog>();
	}

	public void OnClickClassSelection()
	{
		Hide();
		MenuOverlay.Open<ClassSelection>();
	}

	public void OnClickTeamSelection()
	{
		Hide();
		MenuOverlay.Open<TeamSelection>();
	}

	public bool ShouldDraw()
	{
		return Enabled;
	}

	public void Toggle()
	{
		Enabled = !Enabled;

		if ( Enabled )
			Show();
		else
			Hide();
	}

	public void Hide()
	{
		Enabled = false;
		SetClass( "visible", false );
		GameHUD.Enabled = true;
		MenuOverlay.CloseActive();
	}

	public void Show()
	{
		Enabled = true;
		SetClass( "visible", true );
		GameHUD.Enabled = false;
	}
}
