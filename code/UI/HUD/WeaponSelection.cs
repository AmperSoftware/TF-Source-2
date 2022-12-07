using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Amper.FPS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFS2.UI.Game;

partial class WeaponSelection : Panel
{
	bool IsEnabled { get; set; }
	TimeSince TimeSinceInteraction { get; set; }
	Dictionary<TFWeaponSlot, WeaponListItem> Items { get; set; } = new();
	TFWeaponSlot SelectedSlot { get; set; }
	private bool AttackInputHeld { get; set; }

	public WeaponSelection()
	{
		StyleSheet.Load( "/UI/HUD/WeaponSelection.scss" );
		BindClass( "red", () => TFPlayer.LocalPlayer.Team == TFTeam.Red );
		BindClass( "blue", () => TFPlayer.LocalPlayer.Team == TFTeam.Blue );
	}

	/// <summary>
	/// Create the weapon selection menu.
	/// </summary>
	public void Setup()
	{
		// Remove all children that might be there.
		DeleteChildren();
		Items.Clear();

		var player = TFPlayer.LocalPlayer;
		if ( !player.IsValid() )
			return;

		if ( !player.IsAlive )
			return;

		foreach ( TFWeaponSlot slot in Enum.GetValues( typeof( TFWeaponSlot ) ) )
		{
			var weapon = player.GetWeaponInSlot( slot );

			if ( !weapon.IsValid() )
				continue;

			if ( !weapon.IsInitialized )
				continue;

			if ( weapon.Data.Hidden )
				continue;

			var entry = new WeaponListItem
			{
				Parent = this,
				Slot = slot
			};

			entry.SetWeapon( weapon );
			Items[slot] = entry;
		}

		IsEnabled = true;
		TimeSinceInteraction = 0;
	}

	/// <summary>
	/// Hide the weapon selection menu.
	/// </summary>
	public void Close()
	{
		DeleteChildren();
		IsEnabled = false;
	}

	/// <summary>
	/// Check that the given weapon slot is selectable.
	/// </summary>
	public bool CanSelectSlot( TFWeaponSlot slot )
	{
		var player = TFPlayer.LocalPlayer;
		if ( !player.IsValid() )
			return false;

		// Can't select a slot if player doesn't have weapon of this slot.
		var weapon = player.GetWeaponInSlot( slot );

		if ( !weapon.IsValid() )
			return false;

		if ( !weapon.IsInitialized )
			return false;

		if ( weapon.Data.Hidden )
			return false;

		if ( !weapon.CanDeploy( player ) )
			return false;

		return true;
	}

	/// <summary>
	/// Process the highlighted weapon slot before it is selected.
	/// </summary>
	/// <param name="slot"></param>
	public void SelectSlot( TFWeaponSlot slot )
	{
		if ( !CanSelectSlot( slot ) )
			return;

		SelectedSlot = slot;

		if ( !FastWeaponSwitchEnabled )
		{
			if ( !IsEnabled )
				Setup();

			foreach ( var pair in Items )
				pair.Value.SetClass( "selected", pair.Key == slot );

			Sound.FromScreen( "ui.weaponlist.moveselect" );
			TimeSinceInteraction = 0;
		}
	}

	public override void Tick()
	{
		SetClass( "visible", IsEnabled );

		if ( !IsEnabled )
			return;

		if ( TimeSinceInteraction > AutoCloseTime )
			Close();
	}

	/// <summary>
	/// Processes user input while weapon selection is active.
	/// </summary>
	[Event.BuildInput]
	public void ProcessClientInput()
	{
		var player = TFPlayer.LocalPlayer;
		if ( !player.IsValid() )
			return;

		if ( FastWeaponSwitchEnabled )
		{
			// If fast weapon switch is enabled, we're always confirming our selection change.
			SelectSlot( player.GetActiveTFSlot() );
		}

		//
		// Mouse Wheel
		//
		var slotsCount = Enum.GetValues( typeof( TFWeaponSlot ) ).Length;
		var weaponCount = player.Children.OfType<TFWeaponBase>().Count();
		var slot = SelectedSlot;

		// Only do wheel checks if we have weapons in our inventory.
		// Otherwise we might cause an infinite loop.
		if ( weaponCount > 0 )
		{
			// Add a short cooldown so we don't calculate this every tick.
			if ( TimeSinceInteraction > .02f )
			{
				// If we have any mouse wheel input.
				if ( Input.MouseWheel != 0 )
				{
					var delta = -Input.MouseWheel;
					for ( int i = 0; i < slotsCount; i++ )
					{
						// Go through all the slots in the direction of delta and find any eligible weapons we can equip.
						slot += delta;

						// Put slot on the other side of the list if we overflow the list.
						if ( (int)slot >= slotsCount ) slot = 0;
						else if ( (int)slot < 0 ) slot = (TFWeaponSlot)slotsCount - 1;

						var weapon = player.GetWeaponInSlot( slot );
						if ( weapon == null )
							continue;

						if ( weapon.Data.Hidden )
							continue;

						SelectSlot( slot );
						break;
					}

					TimeSinceInteraction = 0;
				}
			}
		}

		//
		// Keyboard Switch
		//
		if ( Input.Pressed( InputButton.Slot1 ) ) SelectSlot( TFWeaponSlot.Primary );
		if ( Input.Pressed( InputButton.Slot2 ) ) SelectSlot( TFWeaponSlot.Secondary );
		if ( Input.Pressed( InputButton.Slot3 ) ) SelectSlot( TFWeaponSlot.Melee );
		if ( Input.Pressed( InputButton.Slot4 ) ) SelectSlot( TFWeaponSlot.PDA );
		if ( Input.Pressed( InputButton.Slot5 ) ) SelectSlot( TFWeaponSlot.PDA2 );
		if ( Input.Pressed( InputButton.Slot6 ) ) SelectSlot( TFWeaponSlot.Action );

		/*
		// NO SLOTS THAT CAN THEORETICALLY USE THIS YET, SO DON'T NEED TO CHECK.
		if ( input.Pressed( InputButton.Slot7 ) ) SelectSlot( keySlot );
		if ( input.Pressed( InputButton.Slot8 ) ) SelectSlot( keySlot );
		if ( input.Pressed( InputButton.Slot9 ) ) SelectSlot( keySlot );
		if ( input.Pressed( InputButton.Slot0 ) ) SelectSlot( keySlot );
		*/

		//
		// Confirmation
		//
		bool confirmChoice = false;
		if ( FastWeaponSwitchEnabled )
		{
			// If fast weapon switch is enabled, we're always confirming our selection change.
			confirmChoice = true;
		}
		else
		{
			// Otherwise see if weapon list menu is both visible and we're pressing attack to confirm.
			if ( IsEnabled )
			{
				if ( Input.Pressed( InputButton.PrimaryAttack ) )
				{
					confirmChoice = true;
					AttackInputHeld = true;
				}
			}

			// Don't allow player to use attack button if they press it to confirm selection and didn't release since then.
			if ( AttackInputHeld )
			{
				if ( Input.Down( InputButton.PrimaryAttack ) )
					Input.SetButton( InputButton.PrimaryAttack, false );
				else
					AttackInputHeld = false;
			}
		}

		if ( confirmChoice )
		{
			var weapon = player.GetWeaponInSlot( SelectedSlot );
			if ( weapon.IsValid() )
			{
				if ( weapon != player.ActiveWeapon )
					Input.ActiveChild = weapon;
			}

			Close();
		}
	}

	[ConVar.Client( "cl_fast_weapon_switch" )] public static bool FastWeaponSwitchEnabled { get; set; } = true;
	[ConVar.Client( "cl_hud_weaponlist_close_time" )] public static float AutoCloseTime { get; set; } = 20;
}

partial class WeaponListItem : Panel
{
	public TFWeaponBase Weapon { get; set; }
	public TFWeaponSlot Slot { get; set; }

	Label Index { get; set; }
	Label Name { get; set; }
	Image Image { get; set; }

	public WeaponListItem()
	{
		var container = Add.Panel( "container" );
		Index = container.Add.Label( "0", "index" );
		Name = container.Add.Label( "", "name" );
		Image = container.Add.Image( "", "image" );
	}

	public void SetWeapon( TFWeaponBase weapon )
	{
		// Check that the set weapon has been initialized.
		if ( weapon == null ) return;
		if ( !weapon.IsInitialized ) return;

		Weapon = weapon;
		Index.Text = $"{(int)Slot + 1}";
		Image.SetTexture( Util.JPGToPNG( Weapon.Data.InventoryIcon ) );
		Name.Text = Weapon.Data.Title;
	}
}
