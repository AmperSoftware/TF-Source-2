using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

/// <summary>
/// Popup dialog box asking the player if they want to quit i.e. MessagePanel.cs
/// </summary>
[UseTemplate]
public class QuitDialog : MenuOverlay
{
	public void OnClickCancel()
	{
		Close();
	}

	public void OnClickQuit()
	{
		Game.LocalClient.Kick();
	}
}
