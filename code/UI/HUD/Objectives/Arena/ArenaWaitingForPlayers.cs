using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

[UseTemplate]
partial class ArenaWaitingForPlayers : Panel
{
	public override void Tick()
	{
		base.Tick();
		SetClass( "visible", ShouldDraw() );
		if ( !IsVisible ) return;

		SetClass( "red", ((TFPlayer)Sandbox.Game.LocalPawn).Team == TFTeam.Red );
		SetClass( "blue", ((TFPlayer)Sandbox.Game.LocalPawn).Team == TFTeam.Blue );

		// Move the arena player count down if in specator mode.
		SetClass( "if-spectator", ((TFPlayer)Sandbox.Game.LocalPawn).Team == TFTeam.Spectator );
	}

	public bool ShouldDraw()
	{
		return TFGameRules.Current.GameType == TFGameType.Arena && TFGameRules.Current.IsWaitingForPlayers && !Input.Down( InputButton.Score );
	}
}
