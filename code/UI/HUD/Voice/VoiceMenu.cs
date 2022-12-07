using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;

namespace TFS2;

[UseTemplate]
public partial class VoiceMenu : Panel
{
	[ConVar.Client] public static float hud_voicemenu_dismiss_time { get; set; } = 20;
	public static VoiceMenu Current { get; set; }

	public static readonly List<List<(TFResponseConcept, string)>> PageDefinition = new()
	{
		// Voice Menu 1
		new()
		{
			( TFResponseConcept.VoiceMedic,             "MEDIC!" ),
			( TFResponseConcept.VoiceThanks,            "Thanks!" ),
			( TFResponseConcept.VoiceGo,                "Go Go Go!" ),
			( TFResponseConcept.VoiceMoveUp,            "Move Up!" ),
			( TFResponseConcept.VoiceGoLeft,            "Go Left" ),
			( TFResponseConcept.VoiceGoRight,           "Go Right" ),
			( TFResponseConcept.VoiceYes,               "Yes" ),
			( TFResponseConcept.VoiceNo,                "No" )
		},
		// Voice Menu 2
		new()
		{
			( TFResponseConcept.VoiceIncoming,          "Incoming" ),
			( TFResponseConcept.VoiceSpy,               "Spy!" ),
			( TFResponseConcept.VoiceSentryAhead,       "Sentry Ahead!" ),
			( TFResponseConcept.VoiceTeleporterHere,    "Teleporter Here" ),
			( TFResponseConcept.VoiceDispenserHere,     "Dispenser Here" ),
			( TFResponseConcept.VoiceSentryHere,        "Sentry Here" ),
			( TFResponseConcept.VoiceActivateUbercharge,"Activate ÜberCharge!" ),
			( TFResponseConcept.VoiceUberchargeReady,   "MEDIC: ÜberCharge Ready" )
		},
		// Voice Menu 3
		new()
		{
			( TFResponseConcept.VoiceHelp,              "Help!" ),
			( TFResponseConcept.VoiceBattleCry,         "Battle Cry" ),
			( TFResponseConcept.VoiceCheers,            "Cheers" ),
			( TFResponseConcept.VoiceJeers,             "Jeers" ),
			( TFResponseConcept.VoicePositive,          "Positive" ),
			( TFResponseConcept.VoiceNegative,          "Negative" ),
			( TFResponseConcept.VoiceNiceShot,          "Nice Shot" ),
			( TFResponseConcept.VoiceGoodJob,           "Good Job" )
		}
	};

	readonly Dictionary<InputButton, int> SlotButtons = new()
	{
		{ InputButton.Slot1, 0 },
		{ InputButton.Slot2, 1 },
		{ InputButton.Slot3, 2 },
		{ InputButton.Slot4, 3 },
		{ InputButton.Slot5, 4 },
		{ InputButton.Slot6, 5 },
		{ InputButton.Slot7, 6 },
		{ InputButton.Slot8, 7 },
		{ InputButton.Slot9, 8 }
	};

	Panel PageContainer { get; set; }
	Panel InfoContainer { get; set; }
	bool Shown;
	int ActivePage;
	public float? AutoDismissTime;
	List<VoiceMenuPage> Pages = new();

	public VoiceMenu()
	{
		Current = this;

		var i = 0;
		foreach ( var pageDef in PageDefinition )
		{
			i++;
			var pagePanel = AddPage( $"Voice Menu {i}" );

			var j = 0;
			foreach ( var pair in pageDef )
			{
				j++;
				var concept = pair.Item1;
				var name = pair.Item2;
				pagePanel.Add.Label( $"{j}. {name}", "option" );
			}

			Pages.Add( pagePanel );
		}
	}

	public VoiceMenuPage AddPage( string name )
	{
		var page = PageContainer.AddChild<VoiceMenuPage>();
		page.Add.Label( name, "header" );
		return page;
	}

	public override void Tick()
	{
		if ( !Shown )
			return;

		if ( AutoDismissTime.HasValue && Time.Now >= AutoDismissTime )
			Close();
	}

	public void SwitchToPage( int index )
	{
		if ( Pages.Count == 0 )
			return;

		index = Math.Clamp( index, 0, Pages.Count - 1 );

		for ( var i = 0; i < Pages.Count; i++ )
		{
			var page = Pages[i];

			if ( index == i )
				page.Show();
			else
				page.Hide();
		}

		ActivePage = index;
		ExtendAutoDismissTime();
	}

	public void Show( int menu = 0 )
	{
		InfoContainer.SetClass( "visible", Input.GetButtonOrigin( InputButton.View ) != null );
		Shown = true;
		AddClass( "visible" );
		SwitchToPage( menu );
	}

	public void Toggle( int menu = 0 )
	{
		// If we're trying to open the currently active page...
		if ( Shown && ActivePage == menu )
			Close(); // just close the menu.
		else
			Show( menu );
	}

	public void NextPage()
	{
		if ( !Shown )
		{
			Show();
			return;
		}

		var nextPage = ActivePage + 1;
		if ( nextPage >= Pages.Count )
			nextPage = 0;

		SwitchToPage( nextPage );
	}

	public void Close()
	{
		if ( !Shown )
			return;

		RemoveClass( "visible" );
		Shown = false;
		AutoDismissTime = null;
	}

	public void ExtendAutoDismissTime()
	{
		AutoDismissTime = null;

		if ( hud_voicemenu_dismiss_time <= 0 )
			return;

		AutoDismissTime = Time.Now + hud_voicemenu_dismiss_time;
	}

	[Event.BuildInput]
	public void ProcessClientInput()
	{
		if ( Input.Pressed( InputButton.View ) )
			NextPage();

		if ( !Shown )
			return;

		// If we've pressed Slot0 close the menu right away, this button means cancel.
		if ( Input.Pressed( InputButton.Slot0 ) )
		{
			Input.SuppressButton( InputButton.Slot0 );
			Close();
			return;
		}

		foreach ( var pair in SlotButtons )
		{
			if ( Input.Pressed( pair.Key ) )
			{
				ButtonPressed( pair.Key );
				Input.SuppressButton( pair.Key );
			}
		}
	}

	public void ButtonPressed( InputButton button )
	{
		if ( !Shown )
			return;

		if ( !SlotButtons.TryGetValue( button, out var index ) )
			return;

		ConsoleSystem.Run( "voicemenu", ActivePage, index );
		Close();
	}

	[ConCmd.Client( "voice_menu" )]
	public static void Command_VoiceMenu( int menu )
	{
		Current?.Toggle( menu );
	}
}
