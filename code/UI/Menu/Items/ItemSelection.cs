using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

[UseTemplate]
partial class ItemSelection : MenuOverlay
{
	Label ClassName { get; set; }
	Label SlotName { get; set; }
	Panel ItemsContainer { get; set; }
	PlayerClass PlayerClass { get; set; }
	TFWeaponSlot Slot { get; set; }
	Label PlayerName { get; set; }
	Image PlayerAvatar { get; set; }

	public ItemSelection( PlayerClass pclass, TFWeaponSlot slot )
	{
		PlayerClass = pclass;
		Slot = slot;

		SetupPage();
	}

	public override void Tick()
	{
		if ( !IsVisible ) return;

		PlayerName.Text = Game.LocalClient.Name;
		PlayerAvatar.SetTexture( $"avatarbig:{Game.LocalClient.SteamId}" );
	}

	public async void SetupPage()
	{
		ItemsContainer.DeleteChildren( true );

		if ( PlayerClass == null )
			return;

		ClassName.Text = PlayerClass.Title;
		SlotName.Text = $"{Slot}";

		var loadout = Loadout.LocalLoadout;
		await loadout.Load();

		// Add the currently equipped weapon.
		var equipped = await loadout.GetLoadoutItem( PlayerClass, Slot );
		if ( equipped != null )
			AddItem( equipped, true );

		// If our currently equipped weapon is not stock, put stock.
		var stock = PlayerClass.GetDefaultWeaponForSlot( Slot );
		if ( stock != null && stock != equipped )
			AddItem( stock );

		// Put in all remaining weapons.
		foreach ( var weapon in WeaponData.FindAllForClassAndSlot( PlayerClass, Slot ) )
		{
			if ( weapon == equipped || weapon == stock )
				continue;

			AddItem( weapon );
		}
	}

	public void AddItem( WeaponData data, bool equipped = false )
	{
		var item = new ItemPicker
		{
			PlayerClass = PlayerClass,
			Parent = ItemsContainer,
			//IsEquipped = equipped
		};

		item.SetWeaponData( data );

		item.OnClicked += () =>
		{
			Loadout.LocalLoadout.SetLoadoutItem( PlayerClass, Slot, data );
			OnClickBack();
		};
	}

	public void OnClickBack()
	{
		Open( new ClassLoadout() );
	}
}
