using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Amper.FPS;
using System;
using System.Linq;

namespace TFS2;

[UseTemplate]
partial class WinPanel : Panel
{
	Label ScoreBlue { get; set; }
	Label ScoreRed { get; set; }
	Label WinnerName { get; set; }
	Label WinReason { get; set; }
	Label LastCapper { get; set; }
	Panel Players { get; set; }
	TimeSince TimeSinceSetup { get; set; }
	bool WillScoreAnimate { get; set; }

	public override void Tick()
	{
		var shouldDraw = ShouldDraw();

		if ( !IsVisible )
		{
			if ( shouldDraw )
				SetupWinPanel();
		}
		else
		{
			if ( shouldDraw )
			{
				if ( WillScoreAnimate && TimeSinceSetup > 1 )
				{
					Sound.FromScreen( "ui.scored" );
					WillScoreAnimate = false;

					ScoreRed.Text = GetTeamScore( TFTeam.Red ).ToString();
					ScoreBlue.Text = GetTeamScore( TFTeam.Blue ).ToString();
				}
			}
		}

		SetClass( "visible", shouldDraw );
	}

	/// <summary>
	/// Setup the win panel to be shown at the end of a round.
	/// </summary>
	public void SetupWinPanel()
	{
		// Get the winning team.
		var winner = (TFTeam)TFGameRules.Current.Winner;
		WinnerName.Text = $"{winner.GetTitle()} WINS!";

		// Update the RED team score.
		var redScore = GetTeamScore( TFTeam.Red );
		if ( winner == TFTeam.Red ) redScore = Math.Max( 0, redScore - 1 );
		ScoreRed.Text = redScore.ToString();

		// Update the BLU team score.
		var blueScore = GetTeamScore( TFTeam.Blue );
		if ( winner == TFTeam.Blue ) blueScore = Math.Max( 0, blueScore - 1 );
		ScoreBlue.Text = blueScore.ToString();

		// Get the win reason.
		WinReason.Text = GetWinReasonMessage( winner, (TFWinReason)TFGameRules.Current.WinReason );

		// Get the round MVPs.
		var mvps = Game.Clients.Where( cl => cl.GetTeam() == winner )
							 .Distinct() // sometimes we get duplicate players
							 .OrderByDescending( cl => cl.GetPoints() )
							 .Take( 3 );
		Players.DeleteChildren( true );

		foreach ( var ply in mvps )
			Players.AddChild( new WinPanelPlayer( ply ) );

		// Set the team panel to the winning team color.
		SetClass( "red", winner == TFTeam.Red );
		SetClass( "blue", winner == TFTeam.Blue );

		// Reset for next round.
		TimeSinceSetup = 0;
		WillScoreAnimate = true;
	}

	/// <summary>
	/// Get the the team's current score.
	/// </summary>
	public int GetTeamScore( TFTeam team )
	{
		TFGameRules.Current.Score.TryGetValue( (int)team, out int score );
		return score;
	}

	/// <summary>
	/// Determines if the element should be displayed on screen.
	/// </summary>
	public bool ShouldDraw()
	{
		return SDKGame.Current.State == GameState.RoundEnd;
	}

	/// <summary>
	/// Get the reason for why the team has won the round.
	/// </summary>
	public string GetWinReasonMessage( TFTeam winner, TFWinReason reason )
	{
		return reason switch
		{
			TFWinReason.FlagCaptureLimit => $"{winner.GetTitle()} captured the enemy intelligence {TFGameRules.tf_flag_caps_per_round} times",
			TFWinReason.AllPointsCaptured => $"{winner.GetTitle()} captured all the control points",
			TFWinReason.OpponentsDead => $"{winner.GetTitle()} killed all opponents",
			TFWinReason.FragLimit => $"{winner.GetTitle()} reached the frag limit first",
			_ => "",
		};
	}
}

partial class WinPanelPlayer : Panel
{
	public Image PlayerAvatar { get; set; }
	public Label NameLabel { get; set; }
	public Label ClassLabel { get; set; }
	public Label PointsLabel { get; set; }
	
	public WinPanelPlayer( IClient cl )
	{
		PlayerAvatar = Add.Image( $"avatar:{cl.SteamId}", "avatar" );
		NameLabel = Add.Label( cl.Name, "name text" );
		ClassLabel = Add.Label( cl.GetPlayerClass().Title, "pclass text" );
		PointsLabel = Add.Label( cl.GetPoints().ToString(), "points text" );
	}
}
