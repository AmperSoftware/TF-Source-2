using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using static Sandbox.Clothing;

namespace TFS2.UI;

[UseTemplate]
partial class ClassLoadout : MenuOverlay
{
	PlayerClass PlayerClass { get; set; }
	Label ClassName { get; set; }
	Panel WeaponSlots { get; set; }
	List<ItemPicker> Slots { get; set; } = new();
	Label PlayerName { get; set; }
	Image PlayerAvatar { get; set; }

	public ClassLoadout()
	{
		PlayerClass = Sandbox.Game.LocalClient.GetPlayerClass();
		SetupPage();
	}

	public ClassLoadout( PlayerClass playerClass )
	{
		PlayerClass = playerClass;
		SetupPage();
	}

	public override void Tick()
	{
		if ( !IsVisible ) return;

		PlayerName.Text = Sandbox.Game.LocalClient.Name;
		PlayerAvatar.SetTexture( $"avatarbig:{Sandbox.Game.LocalClient.SteamId}" );
	}

	public async void SetupPage()
	{
		if ( PlayerClass == null )
			return;

		ClassName.Text = PlayerClass.Title;
		WeaponSlots.DeleteChildren();
		Slots.Clear();

		var loadout = Loadout.LocalLoadout;

		foreach ( TFWeaponSlot slot in Enum.GetValues( typeof( TFWeaponSlot ) ) )
		{
			// Don't show loadout slots past PDA2.
			// TODO: Make this better next time we're touching weapons.
			if ( slot >= TFWeaponSlot.PDA2 )
				break;

			var weapon = await loadout.GetLoadoutItem( PlayerClass, slot );
			if ( weapon == null )
			{
				var defaultweapon = PlayerClass.GetDefaultWeaponForSlot( slot );
				if ( defaultweapon == null ) // No default weapon for this slot, skipping
					continue;

				weapon = defaultweapon;
			}

			AddLoadoutSlot( WeaponSlots, slot, weapon );
		}
	}

	public void AddLoadoutSlot( Panel parent, TFWeaponSlot slot, WeaponData weapon )
	{
		var panel = new ItemPicker
		{
			Slot = slot,
			PlayerClass = PlayerClass,
			Parent = parent
		};

		Slots.Add( panel );
		panel.SetWeaponData( weapon );

		panel.OnClicked += () =>
		{
			Open( new ItemSelection( PlayerClass, slot ) );
		};
	}

	protected override void PostTemplateApplied()
	{
		base.PostTemplateApplied();
		SetupPage();
	}

	public override void OnHotloaded()
	{
		base.OnHotloaded();
	}

	public void OnClickBack()
	{
		Close();
	}
}
