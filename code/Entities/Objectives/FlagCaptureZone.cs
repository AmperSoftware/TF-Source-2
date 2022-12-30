using Sandbox;
using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amper.FPS;

namespace TFS2;

[Library( "tf_flag_capturezone" )]
[Title("FLag Capture Zone")]
[Category("Objectives")]
[Icon("pin_drop")]
[HammerEntity]
public partial class FlagCaptureZone : BaseTrigger, ITeam
{
	/// <summary>
	/// If set to a playable team, reset flags from this team only.
	/// </summary>
	[Property, Net] public TFTeam Team { get; set; }
	public int TeamNumber => (int)Team;

	TimeSince TimeSinceTouchingEnemyZoneWarning { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		// Always transmit so that players always know where it is.
		Transmit = TransmitType.Always;
	}

	public override void Touch( Entity other )
	{
		var player = other as TFPlayer;
		if ( player == null ) return;

		if ( player.PickedItem is Flag flag )
		{
			if ( Team != TFTeam.Unassigned )
			{
				if ( player.Team != TFTeam.Unassigned && !ITeam.IsSame( this, player ) ) 
				{
					if ( TimeSinceTouchingEnemyZoneWarning > 5 )
					{
						TFGameRules.SendHUDAlertToPlayer( player, "Take the INTELLIGENCE back to YOUR BASE!", "/ui/Icons/ico_flag_moving.png", 3 );
						TimeSinceTouchingEnemyZoneWarning = 0;
					}

					return;
				}
			}

			if ( TFGameRules.Current.FlagsCanBeCapped() )
			{
				Capture( player );
			}
		}
	}

	public void Capture( TFPlayer player )
	{
		if ( player.PickedItem is Flag flag && TFGameRules.Current.CanFlagBeCaptured( flag ) ) 
		{
			flag.Capture( player, this );
		}
	}


	[Event.Tick.Server]
	public void Tick()
	{
		foreach ( var toucher in TouchingEntities )
		{
			Touch( toucher );
		}
	}
}
