using Sandbox;
using Sandbox.UI;

namespace TFS2.UI;

[UseTemplate]
partial class TeamGoalDisplay : Panel
{
	Image Icon { get; set; }
	Label Goal { get; set; }

	public override void Tick()
	{
		var canSee = false;

		if ( canSee )
		{
			if ( Sandbox.Game.LocalPawn is TFPlayer player && player.IsAlive && player.Team.IsPlayable() )
			{
				if ( TFGameRules.Current.TeamGoal.ContainsKey( player.Team ) )
				{
					var goal = TFGameRules.Current.TeamGoal[player.Team];
					if ( string.IsNullOrEmpty( goal ) )
					{
						if ( !IsVisible )
						{
							var role = TFGameRules.Current.TeamRole[player.Team];
							Icon.SetTexture( TeamRoleIcons[(int)role] );

							Goal.Text = goal;
							SetClass( "visible", true );
						}
					}
				}
			}
		}
		else
			SetClass( "visible", false );
	}

	static string[] TeamRoleIcons { get; set; } = new string[] {
		"/ui/Icons/hud_icon_attack.png",
		"/ui/Icons/hud_icon_capture.png",
		"/ui/Icons/hud_icon_defend.png"
	};
}
