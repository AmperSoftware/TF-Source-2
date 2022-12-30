using Sandbox;
using System;
using Amper.FPS;

namespace TFS2;

[Library( "tf_weapon_sniperrifle", Title = "Sniper Rifle" )]
partial class SniperRifle : TFWeaponBase
{
	[ConVar.Replicated] public static float tf_sniperrifle_charge_time { get; set; } = 4;
	[ConVar.Client] public static float tf_sniperrifle_zoom_sensitivity { get; set; } = 0.3f;

	public const float ScopedMaxSpeed = 80;

	/// <summary>
	/// Cooldown for scoping.
	/// </summary>
	public virtual float ZoomTime => 0.3f;
	/// <summary>
	/// How much is damage multiplied when we're fully charged?
	/// </summary>
	public virtual float ChargeMultiplier => 3;
	/// <summary>
	/// How many zooms can we do?
	/// </summary>
	public virtual int MaxZoomLevel => 1;
	/// <summary>
	/// Are we ready to change zoom level?
	/// </summary>
	public bool CanChangeZoomLevel => TimeSinceChangedZoomLevel >= ZoomTime;
	/// <summary>
	/// Are we zoomed right now?
	/// </summary>
	public bool IsZoomed => ZoomLevel > 0;

	/// <summary>
	/// The currect level of zoom.
	/// </summary>
	[Net, Predicted] public int ZoomLevel { get; set; }
	/// <summary>
	/// How much time has passed since we changed zoom level.
	/// </summary>
	[Net, Predicted] TimeSince TimeSinceChangedZoomLevel { get; set; }

	/// <summary>
	/// If this has value, the rifle will automatically unzoom after the time exceeds the value in this variable.
	/// </summary>
	float? AutoZoomOutTime { get; set; }
	/// <summary>
	/// If true, the rifle will automatically zoom in on the next possible case.
	/// </summary>
	bool WillAutoZoomIn { get; set; }

	public override void SimulateAttack()
	{
		base.SimulateAttack();

		if ( WishSecondaryAttack() )
		{
			// Cancel auto zoom in if we click secondary again.
			if ( WillAutoZoomIn )
				WillAutoZoomIn = false;
		}
	}

	public override void SecondaryAttack()
	{
		base.SecondaryAttack();
		ToggleZoom();
	}

	public override void PrimaryAttack()
	{
		base.PrimaryAttack();

		if ( IsZoomed )
		{
			if ( OwnerWantsAutoRezoom() )
				WillAutoZoomIn = true;

			ResetCharge();
			ZoomOutDelayed( 0.5f );
		}
	}

	public bool OwnerWantsAutoRezoom()
	{
		return Owner?.Client?.GetClientData( "tf_sniper_autoscope_enabled", "1" ).ToBool() ?? false;
	}

	public void ZoomOutDelayed( float delay )
	{
		AutoZoomOutTime = Time.Now + delay;
	}

	/// <summary>
	/// Toggles zoom 
	/// </summary>
	public void ToggleZoom()
	{
		if ( !CanChangeZoomLevel )
			return;

		if ( ZoomLevel < MaxZoomLevel )
			ZoomIn();
		else
			ZoomOut();
	}

	public bool CanZoomIn()
	{
		// Only allow zoom in if we can attack.
		if ( !CanPrimaryAttack() )
			return false;

		if ( !HasEnoughAmmoToAttack() )
			return false;

		return true;
	}

	public bool CanZoomOut()
	{
		return true;
	}

	public void ZoomIn()
	{
		if ( ZoomLevel >= MaxZoomLevel )
			return;

		if ( !CanZoomIn() )
			return;

		ZoomLevel++;
		TimeSinceChangedZoomLevel = 0;
		WillAutoZoomIn = false;

		TFOwner?.SetFieldOfView( this, 30, 0.1f );
	}

	public void ZoomOut()
	{
		if ( ZoomLevel <= 0 )
			return;

		if ( !CanZoomOut() )
			return;

		TimeSinceChangedZoomLevel = 0;
		ZoomLevel = 0;
		AutoZoomOutTime = null;
		ResetCharge();

		TFOwner?.ResetFieldOfViewFromRequester( this, 0.1f );
	}

	public override void ApplyDamageModifications( Entity victim, ref ExtendedDamageInfo info, TraceResult trace )
	{
		base.ApplyDamageModifications( victim, ref info, trace );

		//
		// Charge damage bonus
		//

		var frac = GetChargeFraction();
		var baseDamage = info.Damage;
		var chargeDamage = baseDamage * ChargeMultiplier;
		info.Damage = baseDamage.LerpTo( chargeDamage, frac );

		//
		// Headshotting
		//

		// Should this be moved to base?
		// in case if other weapons needs headshotting.

		var hitBox = info.Hitbox;
		if ( CanHeadshotEntity( victim, hitBox ) )
		{
			info = info.WithFlag( TFDamageFlags.Critical );
			TFOwner.Headshots++;
		}
	}

	public override void BuildInput()
	{
		if ( IsZoomed )
		{
			Input.AnalogLook *= tf_sniperrifle_zoom_sensitivity;
		}
	}

	public override void ModifyOwnerMaxSpeed( ref float speed )
	{
		if ( IsZoomed )
			speed = ScopedMaxSpeed;
	}

	public override void Simulate( IClient client )
	{
		base.Simulate( client );

		if ( ShouldZoomOut() )
			ZoomOut();

		if ( ShouldZoomIn() )
			ZoomIn();

		//
		// Charge
		//
		const float CHARGE_DELAY = 1.3f;
		if ( IsZoomed && CanPrimaryAttack() & TimeSinceChangedZoomLevel >= CHARGE_DELAY )
		{
			Charge = Charge.Approach( tf_sniperrifle_charge_time, Time.Delta );

			if ( !ChargeBellPlayed && Charge >= tf_sniperrifle_charge_time )
				PlayChargeBell();
		}
	}

	public bool ShouldZoomOut()
	{
		// if we are scoped, but can't scope anymore, unscope.
		if ( IsZoomed && !CanZoomIn() && CanChangeZoomLevel )
			return true;

		// unzoom the rifle after a certain time point in time.
		if ( AutoZoomOutTime.HasValue && Time.Now > AutoZoomOutTime.Value )
			return true;

		return false;
	}

	public bool ShouldZoomIn()
	{
		return WillAutoZoomIn && CanAutoZoom();
	}

	protected override void DebugScreenText( float interval )
	{
		DebugOverlay.ScreenText(
			$"[SNIPER RIFLE]\n" +
			$"MaxZoomLevel          {MaxZoomLevel}\n" +
			$"ZoomLevel             {ZoomLevel}\n" +
			$"IsZoomed              {IsZoomed}\n" +
			$"CanChangeZoomLevel    {CanChangeZoomLevel}\n" +
			$"WillAutoZoomIn        {WillAutoZoomIn}\n" +
			$"AutoZoomOutTime       {AutoZoomOutTime}\n" +
			$"Charge                {Charge}\n" +
			$"ChargeBellPlayed      {ChargeBellPlayed}\n" +
			$"\n" +
			$"CanZoomIn()           {CanZoomIn()}\n" +
			$"CanZoomOut()          {CanZoomOut()}\n",
			interval
		);
	}

	#region Charging
	public bool ChargeBellPlayed { get; set; }
	public float Charge { get; set; }

	public void PlayChargeBell()
	{
		// only play recharge sound to local owner pawn
		if ( Owner.IsLocalPawn )
			PlayUnpredictedSound( "player.recharged" );

		ChargeBellPlayed = true;
	}

	public void ResetCharge()
	{
		ChargeBellPlayed = false;
		Charge = 0;
	}

	public float GetChargeFraction()
	{
		if ( !IsZoomed )
			return 0;

		if ( tf_sniperrifle_charge_time == 0 )
			return 1;

		return Math.Clamp( Charge / tf_sniperrifle_charge_time, 0, 1 );
	}
	#endregion

	public bool CanHeadshotEntity( Entity entity, Hitbox hitBox )
	{
		if ( !IsZoomed )
			return false;

		const float ZOOM_HEADSHOT_DELAY = 0.2f;
		if ( TimeSinceChangedZoomLevel < ZOOM_HEADSHOT_DELAY )
			return false;

		if ( entity is not TFPlayer )
			return false;

		return hitBox.HasTag( "head" );
	}

	public bool CanAutoZoom()
	{
		return !IsZoomed && CanPrimaryAttack();
	}

	public override void OnHolster( SDKPlayer owner )
	{
		base.OnHolster( owner );
		ZoomOut();

		WillAutoZoomIn = false;
		AutoZoomOutTime = null;

		owner?.ResetFieldOfViewFromRequester( this );
	}
}
