using Sandbox;
using Amper.FPS;
using Sandbox.Diagnostics;

namespace TFS2;

public partial class Medigun
{
	public const string HealSoundEffect = "weapon_medigun.heal";
	public const string DetachSoundEffect = "weapon_medigun.detach";
	public const string ChargedSoundEffect = "weapon_medigun.charged";

	public const string FailSoundEffect = "weapon_medigun.notarget";
	public const string ReleaseFailSoundEffect = "player_use_fail";

	Sound? HealSound { get; set; }
	Sound? MuzzleSound { get; set; }

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		MuzzleParticle = new( this, "muzzle" );
		MuzzleParticle.Bind( GetMuzzleChargeEffect, 0, () => IsCharged || IsReleasingCharge );

		BeamParticle = new( this, "muzzle" );
		BeamParticle.Bind( GetBeamEffectPath, 0, () => HasPatient, OnBeamCreated );
		BeamParticle.Bind( GetChargedBeamEffectPath, 1, () => HasPatient && IsReleasingCharge, OnBeamCreated );
	}

	//
	// Charge
	//

	public const string ChargeEffectRed = "particles/medicgun_beam/medicgun_invulnstatus_fullcharge_red.vpcf";
	public const string ChargeEffectBlue = "particles/medicgun_beam/medicgun_invulnstatus_fullcharge_blue.vpcf";

	ParticleContainer MuzzleParticle;

	public string GetMuzzleChargeEffect()
	{
		return Team == TFTeam.Blue
			? ChargeEffectBlue
			: ChargeEffectRed;
	}

	//
	// Beam
	//

	public const string BeamEffectRed = "particles/medicgun_beam/medicgun_beam_red.vpcf";
	public const string BeamEffectBlue = "particles/medicgun_beam/medicgun_beam_blue.vpcf";
	public const string BeamEffectChargedRed = "particles/medicgun_beam/medicgun_beam_red_invun.vpcf";
	public const string BeamEffectChargedBlue = "particles/medicgun_beam/medicgun_beam_blue_invun.vpcf";

	ParticleContainer BeamParticle;

	public string GetBeamEffectPath()
	{
		return Team == TFTeam.Blue
			? BeamEffectBlue
			: BeamEffectRed;
	}

	public string GetChargedBeamEffectPath()
	{
		return Team == TFTeam.Blue
			? BeamEffectChargedBlue
			: BeamEffectChargedRed;
	}

	TFPlayer BeamTarget;

	/// <summary>
	/// Beam was created because we are healing someone, attach the particle to our patient.
	/// </summary>
	public void OnBeamCreated( EntityParticle beam )
	{
		Assert.NotNull( Patient );

		BeamTarget = Patient;
		beam.SetControlPoint( 1, Patient, "back_lower" );
	}

	public override void ClientTick()
	{
		base.ClientTick();

		// If we are not healing anyone,
		// reset beam target.
		if ( HasPatient )
		{
			if ( BeamTarget != Patient )
			{
				if ( Patient.IsValid() )
				{
					BeamParticle.RestartEffect();

					HealSound?.Stop();
					HealSound = PlaySound( HealSoundEffect );
				}
			}
		}
		else 
		{ 
			BeamTarget = null;

			if ( HealSound != null )
			{
				HealSound?.Stop();
				HealSound = null;
				PlaySound( DetachSoundEffect );
			}
		}

		var muzzleVisible = MuzzleParticle.Particle?.Particle?.EnableDrawing ?? false;
		if ( MuzzleSound.HasValue != muzzleVisible )
		{
			if ( muzzleVisible )
			{
				MuzzleSound = PlaySound( ChargedSoundEffect );
			}
			else
			{
				MuzzleSound?.Stop();
				MuzzleSound = null;
			}
		}
	}
}
