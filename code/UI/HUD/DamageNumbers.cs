using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using Amper.FPS;

namespace TFS2.UI;

public class DamageNumbers : Panel
{
	public List<DamageNumberInstance> Instances { get; set; } = new();
	public TimeSince TimeSinceDing { get; set; }

	public DamageNumbers()
	{
		StyleSheet.Load( "/ui/hud/DamageNumbers.scss" );
		EventDispatcher.Subscribe<PlayerDeathEvent>( OnDeath, this );
		EventDispatcher.Subscribe<PlayerHurtEvent>( OnHurt, this );
	}

	void OnHurt( PlayerHurtEvent args )
	{
		var attacker = args.Attacker;
		var victim = args.Victim;

		// If we are not the Attacker, ignore.
		if ( Sandbox.Game.LocalClient != attacker )
			return;

		// If we hit ourselves, ignore.
		if ( attacker == victim )
			return;

		// Getting the target of the damage.
		var damage = args.Damage;

		// Play hit sound, if the player is alive.
		if ( victim.IsAlive() )
		{
			if ( TimeSinceDing > 0.1f )
			{
				Sound.FromScreen( "ui.hitsound.default" );
				TimeSinceDing = 0;
			}
		}

		// Add a damage number instance
		AddDamageNumber( damage, victim.Pawn );
	}

	public void AddDamageNumber( float damage, Entity entity )
	{
		// See if we can reuse some other instance.
		var current = FindReusableEntryFor( entity );
		if ( current != null )
		{
			current.AddDamage( damage );
			return;
		}

		// Make a new entry
		var entry = new DamageNumberInstance( entity, damage, this );
		Instances.Add( entry );
		AddChild( entry );
	}

	public DamageNumberInstance FindReusableEntryFor( Entity entity )
	{
		return Instances.Find( x => x.Target == entity && x.CanBeReused );
	}

	void OnDeath( PlayerDeathEvent args )
	{
		// If we're not the damage dealer, ignore.
		if ( args.Attacker != Sandbox.Game.LocalClient )
			return;

		// If we hit ourselves, ignore.
		if ( args.Attacker == args.Victim )
			return;

		Sound.FromScreen( "ui.killsound.default" );
	}

	public void OnInstanceDestroyed( DamageNumberInstance instance )
	{
		Instances.Remove( instance );
	}
}

public class DamageNumberInstance : Label
{
	public Vector3 Position { get; set; }
	public Entity Target { get; set; }
	public float Damage { get; set; }
	public TimeSince TimeSinceCreated { get; set; }
	public DamageNumbers Container { get; set; }
	public bool CanBeReused => TimeSinceCreated < tf_hud_damage_number_reuse_time;

	[ConVar.Client] public static float tf_hud_damage_number_reuse_time { get; set; } = 0.6f;
	[ConVar.Client] public static float tf_hud_damage_number_lifetime { get; set; } = 2f;
	[ConVar.Client] public static float tf_hud_damage_number_fade_time { get; set; } = 1;

	public DamageNumberInstance( Entity target, float damage, DamageNumbers container )
	{
		Target = target;
		Container = container;

		AddClass( "number" );
		AddDamage( damage );

		Position = Target.WorldSpaceBounds.Center.WithZ( Target.WorldSpaceBounds.Maxs.z + 10 );
	}

	public void AddDamage( float damage )
	{
		Damage += damage;
		SetText( $"{(Damage > 0 ? "-" : "+")}{MathF.Ceiling( Damage )}" );
		TimeSinceCreated = 0;
	}

	public override void Tick()
	{
		if ( TimeSinceCreated > tf_hud_damage_number_lifetime )
		{
			Delete();
			return;
		}

		// Calculate opacity
		float time = TimeSinceCreated;
		float opacityLerp = 1 - time.LerpInverse( tf_hud_damage_number_lifetime - tf_hud_damage_number_fade_time, tf_hud_damage_number_lifetime );
		Style.Opacity = opacityLerp;

		// Position 
		var screenPos = Position.ToScreen();
		var panelPos = Container.ScreenPositionToPanelPosition( screenPos );
		var left = panelPos.x * 100;
		Style.Left = Length.Percent( left );

		var top = panelPos.y * 100 - TimeSinceCreated * 10;
		Style.Top = Length.Percent( top );

		Style.Dirty();
	}

	public override void OnDeleted()
	{
		Container.OnInstanceDestroyed( this );
		base.OnDeleted();
	}
}
