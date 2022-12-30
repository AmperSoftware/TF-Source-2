using Sandbox;
using System;
using TFS2.UI;
using Amper.FPS;

namespace TFS2;

partial class TFPlayer
{
	[Net] public int ClassChanges { get; set; }
	[Net] public PlayerClass PlayerClass { get; set; }
	[Net] public PlayerClass DesiredPlayerClass { get; set; }

	/// <summary>
	/// Initialize and apply all class-specific properties. This is called only during spawn.
	/// This will not be run if player touches resupply locker.
	/// </summary>
	private void SetupPlayerClass()
	{
		// We didn't choose any class. We can't regenerate.
		if ( !PlayerClass.IsValid() ) 
			return;

		//
		// Player Model
		//

		// Change the model 
		SetModel( PlayerClass.Model );
		SetMaterialGroup( TeamNumber - 2 );

		if ( ResourceLibrary.TryGet<TFResponseData>( PlayerClass.Responses, out var responseData ) ) 
			ResponseController.Load( responseData );
	}

	public void SetClass( PlayerClass pclass )
	{
		// TODO: Same but for player class.

		/*
		// game rules don't allow to change team
		if ( !TFGameRules.Current.CanChangeTeamFrom( Team ) )
			return;

		// see if gamemode wants to override our team with something else.
		team = TFGameRules.Current.GetTeamAssignmentOverride( this, team, autoBalance );
		*/

		var lastClass = PlayerClass;
		if ( lastClass == pclass )
			return;

		ClassChanges++;
		DesiredPlayerClass = pclass;
		TFGameRules.Current.PlayerChangeClass( this, pclass );


		//
		// RESPAWN
		//

		var shouldRespawn = lastClass == null
							|| tf_class_change_instant_respawn
							|| RespawnRoom.IsInsideTeamRoom( this );

		if ( shouldRespawn )
		{
			Respawn();
			return;
		}

		CommitSuicide( false );
		TFChatBox.AddInformation( To.Single( Client ), $"* You will respawn as {pclass.Title}" );
		Log.Info( $"{Client.Name} changed their class {(lastClass != null ? $"from {lastClass.Title} " : "")}to {pclass.Title}" );
	}

	public void SetRandomClass()
	{
		// all classes minus undefined.
		var count = Enum.GetValues( typeof( TFPlayerClass ) ).Length - 1;
		var random = Game.Random.Int( 0, count - 1 );
		var pclass = PlayerClass.Get( (TFPlayerClass)random );

		if ( pclass == null )
		{
			Log.Info( $"SetRandomClass() - Failed to compute random class." );
			return;
		}

		SetClass( pclass );
	}

	[ConCmd.Server( "tf_join_class" )]
	public static void Command_SetClass( string name )
	{
		var player = ConsoleSystem.Caller.Pawn as TFPlayer;
		if ( player == null ) return;

		//
		// Don't allow class change if the game is over.
		//

		if ( TFGameRules.Current.State == GameState.GameOver )
			return;

		//
		// If we are not playing as a playable team, don't allow changing classes.
		//

		if ( !player.Team.IsPlayable() )
			return;

		// assume all classes are lowercase (SOLDIER, Soldier and soldier are all one class.)
		name = name.ToLower();

		// see if we've chosen to select a random class.
		bool selectRandom = name == "auto" || name == "random";

		if( selectRandom )
		{
			player.SetRandomClass();
			return;
		} 

		// Currently nothing ever stops players from choosing any class.
		var pclass = PlayerClass.Get( name );

		// TODO: Don't allow randomly picking class that we already play as.
		// TODO: Check SDKGame for class limit?

		player.SetClass( pclass );
	}

	[ConVar.Replicated] public static bool tf_class_change_instant_respawn { get; set; }

}

