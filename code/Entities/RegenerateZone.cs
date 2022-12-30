using Sandbox;
using Editor;

namespace TFS2;

/// <summary>
/// Players inside this zone will automatically be regenerated. Usually combined with a prop,
/// this is used to create the Resupply Lockers, seen in the spawn rooms.
/// </summary>
[Library( "tf_func_regenerate" )]
[Title( "Resupply Zone" )]
[Category( "Gameplay" )]
[Icon( "checkroom" )]
[Solid]
[HammerEntity]
partial class RegenerateZone : BaseTrigger
{
	[Property( "associated_model", Title = "Associated Locker Model" ), FGDType( "target_destination" )]
	public string AssociatedName { get; set; }
	[Net] public AnimatedEntity AssociatedModel { get; set; }
	[Property, Net] public HammerTFTeamOption Team { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		EnableTouchPersists = true;
		Transmit = TransmitType.Always;
	}

	[Event.Entity.PostSpawn]
	public void OnLevelCreated()
	{
		AssociatedModel = FindByName( AssociatedName ) as AnimatedEntity;
	}

	public override void Touch( Entity other )
	{
		if ( !Game.IsServer )
			return;

		if ( other is not TFPlayer player )
			return;

		if ( !CanRegenerate( player ) )
			return;

		Regenerate( player );
	}

	public bool CanRegenerate( TFPlayer player )
	{
		// Can't regenerate player of other team.
		if ( !Team.Is( player.Team ) )
			return false;

		// Can't regenerate player that regenerated recently.
		return player.TimeSinceRenegeration >= tf_regeneration_interval;
	}

	public void Regenerate( TFPlayer player )
	{
		player.TimeSinceRenegeration = 0;
		player.Regenerate();

		RegenerateEffects( player );
	}

	[ClientRpc]
	public void RegenerateEffects( TFPlayer player )
	{
		AssociatedModel?.SetAnimParameter( "b_open", true );
		Sound.FromEntity( "player.regenerate", player );
	}

	[ConVar.Replicated] public static float tf_regeneration_interval { get; set; } = 3f;
}
