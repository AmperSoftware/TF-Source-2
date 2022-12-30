using Sandbox;
using Amper.FPS;

namespace TFS2;

partial class TFPlayer
{
	private const string Invisibility_Attribute = "invisibility";

	[ConVar.Replicated] public static float tf_invulnerability_wearoff_time { get; set; } = 1;

	public void SubscribeToConditionEvents()
	{
		// TFCondition.Invulnerable
		SubscribeToConditionAdded( TFCondition.Invulnerable, OnInvulnerableAdded );
		SubscribeToConditionRemoved( TFCondition.Invulnerable, OnInvulnerableRemoved );

		// TFCondition.InvulnerableWearoff
		SubscribeToConditionRemoved( TFCondition.InvulnerableWearoff, OnInvulnerableWearoffRemoved );

		// TFCondition.Burning
		SubscribeToConditionTick( TFCondition.Burning, OnBurningTick );
		SubscribeToConditionAdded( TFCondition.Burning, OnBurningAdded );
		SubscribeToConditionRemoved( TFCondition.Burning, OnBurningRemoved );

		// TFCondition.Cloaked/CloakedBlink
		SubscribeToConditionAdded( TFCondition.Cloaked, OnCloakedAdded );
		SubscribeToConditionRemoved( TFCondition.Cloaked, OnCloakedRemoved );
	}

	//
	// Uber/Invulnerability
	//

	public void OnInvulnerableAdded()
	{
		// If the user is only being worn off, they still have the effect
		// don't play the sound.

		if ( !InCondition( TFCondition.InvulnerableWearoff ) )
		{
			// Play uber sound activation effect.
			if ( IsLocalPawn )
				PlaySound( "player.invulnerability.on" );
		}

		RemoveCondition( TFCondition.InvulnerableWearoff );
		UpdateMaterialGroup();
	}

	public void OnInvulnerableRemoved()
	{
		// Play uber sound deactivation effect.
		if ( IsLocalPawn )
			PlaySound( "player.invulnerability.off" );

		AddCondition(
			TFCondition.InvulnerableWearoff,
			tf_invulnerability_wearoff_time,
			GetConditionProvider( TFCondition.Invulnerable )
		);
	}

	public void OnInvulnerableWearoffRemoved()
	{
		UpdateMaterialGroup();
	}

	//
	// Burning
	//

	public void OnBurningTick()
	{
		// If i am now invincible to fire, put me out.
		if ( !CanBurn() )
		{
			RemoveCondition( TFCondition.Burning );
			return;
		}

		//
		// Reduce afterburn time.
		//

		var reductionScale = 1f;

		// if we are being healed, afterburn time reduces even faster
		if ( Healers.Count > 0 )
			reductionScale = 2;

		FlameAfterburnTime -= Time.Delta * reductionScale;

		// Burn time has expired, put us out.
		if ( FlameAfterburnTime <= 0 )
		{
			RemoveCondition( TFCondition.Burning );
			FlameAfterburnTime = 0;
			return;
		}

		//
		// Deal burn damage
		//

		if ( PlayerClass?.Abilities.AfterBurnImmune ?? false )
			return;

		if ( NextAfterburnDamageTime > Time.Now )
			return;

		var dmgInfo = ExtendedDamageInfo.Create( AfterburnDamage )
			.WithInflictor( BurnAttacker )
			.WithAttacker( BurnAttacker )
			.WithWeapon( BurnWeapon )
			.WithAllPositions( WorldSpaceBounds.Center )
			.WithFlag( TFDamageFlags.Burn | TFDamageFlags.PreventPhysicsForce );

		TakeDamage( dmgInfo );

		NextAfterburnDamageTime = Time.Now + AfterburnFrequency;
	}

	EntityParticle BurningEffect;
	Particles ScreenOverlayBurning;

	public const string BurningParticleEffectRed = "particles/burningplayer/burningplayer_red.vpcf";
	public const string BurningParticleEffectBlue = "particles/burningplayer/burningplayer_blue.vpcf";
	public const string BurningScreenParticle = "particles/screen_fx/screen_burningplayer.vpcf";

	public void OnBurningAdded()
	{
		if ( Game.IsServer )
		{
			// On the server we only play the sound.
			PlaySound( "player.fire.engulf" );
			return;
		}

		// On the client we play particles.
		if ( BurningEffect == null )
		{
			var effect = Team == TFTeam.Blue
				? BurningParticleEffectBlue
				: BurningParticleEffectRed;

			BurningEffect = this.CreateParticle( effect );
			BurningEffect.MakePersistent(); // Disposed only when we stop burning
		}

		if ( IsLocalPawn && ScreenOverlayBurning == null )
		{
			ScreenOverlayBurning = Particles.Create( BurningScreenParticle, this );
		}
	}

	public void OnBurningRemoved()
	{
		if ( Game.IsClient )
		{
			BurningEffect?.Destroy();
			BurningEffect = null;

			ScreenOverlayBurning?.Destroy();
			ScreenOverlayBurning = null;
		}

		BurnAttacker = null;
		BurnWeapon = null;
		OriginalBurnAttacker = null;
	}

	//
	// Cloaking
	//

	public const string CloakSound = "player.cloak";
	public const string UnCloakSound = "player.uncloak";

	public void OnCloakedAdded()
	{
		// Make invis Sound
		PlaySound( CloakSound );

		RemoveAllDecals();

		InvisChangeCompleteTime = Time.Now + tf_spy_invis_time;

		// Set speed here
		CalculateMaxSpeed();

		UpdateMaterialGroup();
	}

	public void OnCloakedRemoved()
	{
		// make uninvis sound
		PlaySound( UnCloakSound );

		InvisChangeCompleteTime = Time.Now + tf_spy_invis_time;

		// update teleport effect

		// set speed here
		CalculateMaxSpeed();

		UpdateMaterialGroup();
	}
}
