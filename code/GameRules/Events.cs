using Amper.FPS;
using Sandbox;
using System.Linq;
using TFS2.UI;

namespace TFS2;

partial class TFGameRules
{
	public override void PlayerHurt( SDKPlayer player, ExtendedDamageInfo info )
	{
		base.PlayerHurt( player, info );

		var victim = player.Client;
		var attacker = info.Attacker?.Client;
		var weaponData = (info.Weapon as TFWeaponBase)?.Data;

		EventDispatcher.InvokeEvent( new PlayerHurtEvent()
		{
			Victim = victim,
			Attacker = attacker,
			Assister = null,
			Weapon = weaponData,
			Tags = info.Tags.ToArray(),
			Position = info.ReportPosition,
			Damage = info.Damage
		} );
	}

	public override void PlayerRespawn( SDKPlayer player )
	{
		base.PlayerRespawn( player );

		if ( player is not TFPlayer tfPlayer )
			return;

		EventDispatcher.InvokeEvent( new PlayerSpawnEvent()
		{
			Client = player.Client,
			Team = tfPlayer.Team,
			Class = tfPlayer.PlayerClass
		} );
	}

	public override void PlayerDeath( SDKPlayer player, ExtendedDamageInfo info )
	{
		var victim = player.Client;
		var attacker = info.Attacker?.Client;
		var weaponData = (info.Weapon as TFWeaponBase)?.Data;

		//
		// Dispatch event about this.
		//

		EventDispatcher.InvokeEvent( new PlayerDeathEvent()
		{
			Victim = victim,
			Attacker = attacker,
			Assister = null,
			Weapon = weaponData,
			Flags = info.Flags,
			Position = info.ReportPosition,
			Damage = info.Damage
		} );

		if ( ShouldAnnounceFirstBlood() && !FirstBloodAnnounced )
		{
			FirstBloodAnnounced = true;
			PlaySoundToAll( "announcer.first_blood", SoundBroadcastChannel.Announcer );
		}
	}

	public override void PlayerChangeTeam( SDKPlayer player, int team )
	{
		Game.AssertServer();

		base.PlayerChangeTeam( player, team );
		TFChatBox.AddInformation( To.Everyone, $"{player.Client.Name} joined team {((TFTeam)team).GetTitle()}" );

		EventDispatcher.InvokeEvent( new PlayerChangeTeamEvent
		{
			Client = player.Client,
			Team = (TFTeam)team
		} );
	}
}
