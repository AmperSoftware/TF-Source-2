using Sandbox;
using System;
using System.Linq;

namespace TFS2;

partial class TFGameRules
{
	[Net] public TFGameType GameType { get; set; }

	//
	// Shortcuts to check what game type we're playing.
	//

	public bool IsPlayingClassic => GameType == TFGameType.None;
	public bool IsPlayingArena => GameType == TFGameType.Arena;
	public bool IsPlayingTDM => GameType == TFGameType.TeamDeathmatch;
	public bool IsPlayingKingOfTheHill => GameType == TFGameType.KingOfTheHill;


	//
	// GameType entities 
	// for further references
	//

	[Net] public Arena ArenaLogic { get; set; }
	[Net] public TeamDeathmatch TeamDeathmatchLogic { get; set; }
	[Net] public KingOfTheHill KingOfTheHillLogic { get; set; }

	public override void ResetObjectives()
	{
		FlagCaptures.Clear();


		// reset all objectives ents
		foreach ( var flag in All.OfType<Flag>() ) flag.Reset();
		foreach ( var point in ControlPoint.All ) point.Reset();
		foreach ( var cart in All.OfType<Cart>() ) cart.Reset();
	}

	public override void CalculateObjectives()
	{
		// This function helps define what game type are we currently playing.

		//
		// Objectives
		//

		MapHasFlags = All.OfType<Flag>().Any();
		MapHasControlPoints = All.OfType<ControlPoint>().Any();
		MapHasCarts = All.OfType<Cart>().Any();

		//
		// Logic Entities
		// 

		ArenaLogic = All.OfType<Arena>().FirstOrDefault();
		KingOfTheHillLogic = All.OfType<KingOfTheHill>().FirstOrDefault();
		TeamDeathmatchLogic = All.OfType<TeamDeathmatch>().FirstOrDefault();

		GameType = CalculateAutomaticGameType();

		Log.Info( $"We're playing: {GameType}" );
	}

	public bool AreObjectivesActive()
	{
		// If round is not active, objectives can't be interacted with.
		if ( !IsRoundActive )
			return false;

		// if we're waiting for players, we can't cap.
		if ( IsWaitingForPlayers )
			return false;

		return true;
	}

	/// <summary>
	/// A map can support multiple game types, this function decides which one we're playing by default.
	/// Player can override this with their own value using tf_game_type convar.
	/// </summary>
	/// <returns></returns>
	public TFGameType CalculateAutomaticGameType()
	{
		var types = Enum.GetValues( typeof( TFGameType ) ).Cast<TFGameType>();

		// we reverse so that game types that game types from logic entities 
		// have priority over general objectives game types.
		types = types.Reverse();

		foreach ( var type in types )
		{
			// classic gamemode is always checked last.
			if ( type == TFGameType.None ) continue;

			if ( MapSupportsGameType( type ) )
				return type;
		}

		// If map supports none of the game types, we're playing classic.
		return TFGameType.None;
	}

	public virtual bool MapSupportsGameType( TFGameType type )
	{
		switch ( type )
		{
			case TFGameType.Arena:
				return ArenaLogic != null;

			case TFGameType.TeamDeathmatch:
				return TeamDeathmatchLogic != null;

			case TFGameType.KingOfTheHill:
				return KingOfTheHillLogic != null;

			default: return false;
		}
	}

	public override void SimulateGameplay()
	{
		base.SimulateGameplay();

		if ( !Game.IsServer )
			return;

		CheckWinConditions();
	}

	public void DeclareWinner( TFTeam team, TFWinReason reason )
	{
		DeclareWinner( (int)team, (int)reason );
	}

}
