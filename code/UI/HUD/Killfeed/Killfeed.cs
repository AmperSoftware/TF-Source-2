using Sandbox;
using Sandbox.UI;
using System.IO;
using System.Linq;
using Amper.FPS;

namespace TFS2.UI;

[UseTemplate]
internal partial class TFKillFeed : Panel
{
	public static string DefaultKillIcon => "ui/deathnotice/skull.png";
	public static string CaptureIconRed => "ui/deathnotice/captured_red.png";
	public static string CaptureIconBlue => "ui/deathnotice/captured_blue.png";
	public static string BlockedIconRed => "ui/deathnotice/blocked_red.png";
	public static string BlockedIconBlue => "ui/deathnotice/blocked_blue.png";

	public TFKillFeed()
	{
		EventDispatcher.Subscribe<PlayerDeathEvent>( OnDeath, this );
		EventDispatcher.Subscribe<ControlPointCapturedEvent>( OnPointCaptured, this );
	}

	[Event.Tick.Server]
	public override void Tick()
	{
		base.Tick();

		// Move the arena player count down if in specator mode.
		SetClass( "if-spectator", true );
	}

	public void OnPointCaptured( ControlPointCapturedEvent args )
	{
		var cappers = args.Cappers;
		var newTeam = args.NewTeam;
		var entry = new TFKillFeedEntry();

		if ( cappers.Contains( Sandbox.Game.LocalClient ) )
		{
			entry.SetClass( "is_local_player_involved", true );
			entry.LifeTimeMultiplier = 2;
		}

		// Set the control point name.
		entry.VictimName.Text = $"Captured the {args.Point.PrintName}";
		entry.SetClass( "has_victim", true );

		// Set the cappers.
		entry.AttackerName.SetClass( "red", newTeam == TFTeam.Red );
		entry.AttackerName.SetClass( "blue", newTeam == TFTeam.Blue );
		entry.AttackerName.Text = string.Join( ", ", cappers.Select( x => x.Name ) );
		entry.SetClass( "has_attacker", true );

		// Set the icon. This will fallback to red icon in case spectators somehow end up being put as point owners.
		entry.Icon.SetTexture( newTeam == TFTeam.Blue ? CaptureIconBlue : CaptureIconRed );

		AddChild( entry );
	}

	public void OnDeath( PlayerDeathEvent args )
	{
		var local = Sandbox.Game.LocalClient;

		// Event arguments
		var attacker = args.Attacker;
		var victim = args.Victim;
		var assister = args.Assister;
		var weapon = args.Weapon;
		var is_crit = args.Flags.HasFlag( TFDamageFlags.Critical );
		var is_mini_crit = args.Flags.HasFlag( TFDamageFlags.MiniCritical );
		var killIcon = DefaultKillIcon;
		var entry = new TFKillFeedEntry();

		// Local Player Involved?
		var localPlayerInvolved = local == attacker || local == victim || local == assister;
		if ( localPlayerInvolved )
		{
			entry.SetClass( "is_local_player_involved", true );
			entry.LifeTimeMultiplier = 2;
		}

		//
		// Victim
		//
		entry.VictimName.Text = victim.Name;
		var victimTeam = victim.GetTeam();
		entry.VictimName.SetClass( "red", victimTeam == TFTeam.Red );
		entry.VictimName.SetClass( "blue", victimTeam == TFTeam.Blue );
		entry.SetClass( "has_victim", true );

		//
		// Attacker
		//
		if ( args.Flags.HasFlag( DamageFlags.Fall ) )
			// If there is no attacker that means the world has killed us.
			entry.PostVictimMessage.Text = " fell to a clumsy, painful death";
		else if ( victim == attacker && weapon == null )
			// attacker has dealt damage to themselves with no weapon, they must have suicided.
			entry.PostVictimMessage.Text = " bid farewell, cruel world";
		else if ( attacker.IsValid() )
		{
			// someone else killed us.
			entry.AttackerName.Text = attacker.Name;

			var attackerTeam = attacker.GetTeam();
			entry.AttackerName.SetClass( "red", attackerTeam == TFTeam.Red );
			entry.AttackerName.SetClass( "blue", attackerTeam == TFTeam.Blue );
			entry.SetClass( "has_attacker", true );
		}

		//
		// Weapon Icon
		//
		if ( weapon != null )
		{
			// if we have a weapon, put it's kill icon in the feed.
			var normalIcon = weapon.KillFeedIcon;
			var specialIcon = weapon.KillFeedIconSpecial;
			var weaponIcon = normalIcon;

			if ( is_crit && !string.IsNullOrEmpty( specialIcon ) )
				weaponIcon = specialIcon;

			if ( !string.IsNullOrEmpty( weaponIcon ) )
				killIcon = weaponIcon;
		}

		// If local player is involved, we need to switch the image to an inverted variation.
		if ( localPlayerInvolved )
		{
			var noExtension = Path.Combine( Path.GetDirectoryName( killIcon ), Path.GetFileNameWithoutExtension( killIcon ) );
			var extension = Path.GetExtension( killIcon );

			killIcon = $"{noExtension}_neg{extension}";
		}

		entry.Icon.SetTexture( Util.JPGToPNG( killIcon ) );

		//
		// Backdrop
		//
		entry.SetClass( "is_crit", is_crit );
		entry.SetClass( "is_minicrit", is_mini_crit );

		AddChild( entry );
	}
}
