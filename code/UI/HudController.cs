using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

public partial class TFHud : HudEntity<RootPanel>
{
	public static TFHud Instance { get; set; }

	public TFHud()
	{
		Instance = this;

		if ( Game.IsClient )
			RootPanel.SetTemplate( "/ui/HudController.html" );
	}

	[Event.Client.Frame]
	public void OnHudChangeEnabled()
	{
		if ( Enabled )
		{
			if ( !RootPanel.IsVisible )
				RootPanel.Style.Set( "display", "flex" );
		}
		else
		{
			if ( RootPanel.IsVisible )
				RootPanel.Style.Set( "display", "none" );
		}
	}

	[ConVar.Client( "cl_drawhud" )] public static bool Enabled { get; set; } = true;
}
