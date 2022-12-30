using Sandbox;
using Amper.FPS;
using System.Linq;

namespace TFS2;

partial class TFPlayer : IResponseSpeaker<TFResponseConcept, TFResponseContext>
{
	public ResponseController<TFResponseConcept, TFResponseContext> ResponseController { get; set; }

	public void ModifyResponseCriteria( ResponseCriteria<TFResponseContext> criteriaSet )
	{
		var hoveredEntity = HoveredEntity;
		if ( hoveredEntity is TFPlayer hoveredPlayer )
		{
			var pClass = hoveredPlayer.PlayerClass;
			if ( pClass.IsValid() )
				criteriaSet.Set( TFResponseContext.LookAtClass, pClass.ResourceName );

			// Check if we're looking at an enemy.
			criteriaSet.Set( TFResponseContext.LookAtEnemy, !ITeam.IsSame( this, hoveredPlayer ) );
		}
	}

	float NextResponseTime;
	public void SpeakConceptIfAllowed( TFResponseConcept concept )
	{
		if ( !Game.IsServer )
			return;

		if ( !IsAlive )
			return;

		if ( NextResponseTime > Time.Now )
			return;

		ResponseController.Speak( concept );
		NextResponseTime = Time.Now + .5f;
	}

	[ConCmd.Server( "voicemenu" )]
	public static void Command_VoiceMenu( int menu, int concept )
	{
		var player = ConsoleSystem.Caller.Pawn as TFPlayer;
		if ( !player.IsValid() )
			return;

		player.PlayVoiceCommand( menu, concept );
	}

	public void PlayVoiceCommand( int menu, int concept )
	{
		var pageDefs = VoiceMenu.PageDefinition;
		if ( pageDefs == null )
			return;

		if ( menu < 0 || menu >= pageDefs.Count )
			return;

		var conceptDict = pageDefs[menu];
		if ( conceptDict == null )
			return;

		var concepts = conceptDict.Select( x => x.Item1 ).ToArray();
		if ( concept < 0 || concept >= concepts.Length )
			return;

		var finalConceptEntry = concepts[concept];
		SpeakConceptIfAllowed( finalConceptEntry );
	}

	Sound ResponseSound { get; set; }

	public void PlayResponse( ResponseController<TFResponseConcept, TFResponseContext>.Response response )
	{
		if ( !Game.IsServer )
			return;

		// Stop response if we're already playing another one.
		ResponseSound.Stop();

		using ( Prediction.Off() )
		{
			ResponseSound = PlaySound( response.SoundEvent )
				.SetVolume( 1.5f )
				.SetPosition( EyePosition );

		}

		PlayGestureFromResponseContext( response.Concept );
		SendResponseChatNotification( response.Concept );
	}

	#region GestureTriggerSubstitute
	// For some reason, AutoReset on b_gesture specifically is broken. Until a fix or even the cause of the problem is found, this will work as an alternative.

	public TimeSince TimeSinceGesture { get; set; }
	/// <summary>
	/// If shorter than gesture anims, affects how fast you can switch inbetween different animations. Gestures of the same kind will always wait for the current animation to end. If longer, affects how often the player will gesture alongside a response.
	/// </summary>
	[ConVar.Replicated] public static float sv_gestureswitchcooldown { get; set; } = 1f;
	public bool IsGesturing { get; set; }
	public void SimulateGesture()
	{
		if ( !Game.IsServer ) return;

		if ( TimeSinceGesture >= sv_gestureswitchcooldown )
		{
			IsGesturing = false;
			SetAnimParameter( "b_gesture", false );
		}
	}
	#endregion

	public void PlayGestureFromResponseContext( TFResponseConcept concept )
	{
		if ( !IsGesturing )             //Part of GestureTriggerSubstitute
		{
			switch ( concept )
			{
				case TFResponseConcept.VoiceHelp:
				case TFResponseConcept.VoiceMedic:
					PlayGesture( GestureType.Help );
					break;

				case TFResponseConcept.VoiceThanks:
					PlayGesture( GestureType.Thanks );
					break;

				case TFResponseConcept.VoiceGo:
				case TFResponseConcept.VoiceMoveUp:
					PlayGesture( GestureType.Go );
					break;

				case TFResponseConcept.VoiceBattleCry:
				case TFResponseConcept.VoiceCheers:
					PlayGesture( GestureType.Cheers );
					break;
			}
		}
	}

	public void SendResponseChatNotification( TFResponseConcept concept )
	{
		return;

		switch ( concept )
		{
			case TFResponseConcept.VoiceMedic:
			case TFResponseConcept.VoiceThanks:
			case TFResponseConcept.VoiceGo:
			case TFResponseConcept.VoiceMoveUp:
			case TFResponseConcept.VoiceYes:
			case TFResponseConcept.VoiceNo:

			case TFResponseConcept.VoiceIncoming:
			case TFResponseConcept.VoiceSpy:

				// get teammates
				var clients = Game.Clients.Where( x => x.GetTeam() == Team );

				string message = "";
				bool found = false;
				foreach ( var page in VoiceMenu.PageDefinition )
				{
					foreach ( var pair in page )
					{
						if ( pair.Item1 == concept )
						{
							message = pair.Item2;
							found = true;
							break;
						}
					}
				}

				if ( !found )
					return;

				UI.TFChatBox.AddClientVoiceCommand( To.Multiple( clients ), Client, message );
				break;
		}
	}

	public void PlayGesture( GestureType gesture )
	{
		TimeSinceGesture = 0f;      //Part of GestureTriggerSubstitute
		IsGesturing = true;         //Part of GestureTriggerSubstitute
		SetAnimParameter( "gesture_type", (int)gesture );
		SetAnimParameter( "b_gesture", true );
	}

	public enum GestureType
	{
		Go,
		Help,
		Thanks,
		Cheers
	}
}
