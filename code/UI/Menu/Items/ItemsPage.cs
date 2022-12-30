using Sandbox;
using Sandbox.UI;
using System;

namespace TFS2.UI.Menu.Items;

[UseTemplate]
partial class ItemsPage : MenuOverlay
{
	Label PlayerName { get; set; }
	Image PlayerAvatar { get; set; }

	public override void Tick()
	{
		if ( !IsVisible ) return;

		PlayerName.Text = Game.LocalClient.Name;
		PlayerAvatar.SetTexture( $"avatarbig:{Game.LocalClient.SteamId}" );
	}

	public void OnClickBack()
	{
		Close();
	}

	public void OnClickClassButton( string className )
	{
		if ( !Enum.TryParse<TFPlayerClass>( className, out var item ) )
			return;

		var playerClass = PlayerClass.Get( item );
		if ( playerClass == null )
			return;

		Open( new ClassLoadout( playerClass ) );
	}
}
