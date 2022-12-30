using Sandbox;
using Amper.FPS;
using System.Linq;

namespace TFS2;

partial class TFGameRules
{
	public override bool CanPlayerRespawn( SDKPlayer player )
	{
		var pawn = player as TFPlayer;
		if ( pawn == null )
			return base.CanPlayerRespawn( player );

		if ( pawn.Team == TFTeam.Unassigned )
			return false;

		if ( pawn.PlayerClass == null )
			return false;

		return true;
	}

	public bool CanTeamRespawn( TFTeam team ) => true;

	public virtual TFTeam GetTeamAssignmentOverride( TFPlayer player, TFTeam team, bool autoBalance ) => team;
	public virtual bool CanChangeTeamFrom( TFTeam currentTeam ) => true;

	public virtual PlayerClass GetPlayerClassAssignmentOverride( TFPlayer player, PlayerClass pclass, bool autoBalance ) => pclass;
	public virtual bool CanChangePlayerClassFrom( PlayerClass currentTeam ) => true;

	public override bool IsEnoughPlayersInTeamToStartRound( int team )
	{
		if ( TeamWipeCausesRoundEnd() )
		{
			// In gamemodes where team wipe causes round end, in each playable
			// team there should be at least one player.
			if ( TeamManager.IsPlayable( team ) )
			{
				if ( TeamManager.GetPlayers( team ).Count() < 1 )
					return false;
			}
		}

		return true;
	}

	public bool IsAttackDefenseGameType()
	{
		// escort
		// cps
		return false;
	}

	public override bool TeamWipeCausesRoundEnd()
	{
		// Only in arena.
		if ( GameType == TFGameType.Arena )
			return true;

		// TODO: More logic?

		return false;
	}

	/// <summary>
	/// If the kill was done now, would it force "first blood" announcement?
	/// </summary>
	/// <returns></returns>
	public bool ShouldAnnounceFirstBlood()
	{
		// Only announce first blood when round is active.
		if ( !IsRoundActive ) 
			return false;

		// announce first blood on all gamemodes that cause team wipe to end the round.
		return TeamWipeCausesRoundEnd();
	}

	public bool ShouldPlayGameStartSong()
	{
		// announce first blood on all gamemodes that cause team wipe to end the round.
		return !TeamWipeCausesRoundEnd();
	}

	public override float GetPlayerFallDamage( SDKPlayer player, float velocity )
	{
		var pawn = player as TFPlayer;
		if ( pawn == null )
			return 0;

		if ( velocity <= player.MaxSafeFallSpeed )
			return 0;

		// Old TFC damage formula
		float fallDamage = 5 * velocity / 300;

		// Fall damage needs to scale according to the player's max health, or
		// it's always going to be much more dangerous to weaker classes than larger.
		float ratio = pawn.MaxHealth / 100;
		fallDamage *= ratio;

		// TODO: Maybe remove randomness here? (Or allow it to be toggled)
		fallDamage *= Game.Random.Float( 0.8f, 1.2f );
		return fallDamage;
	}

	public bool AreRespawnRoomsOpen() => IsRoundEnded;
}
