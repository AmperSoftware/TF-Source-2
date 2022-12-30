using Sandbox;
using Amper.FPS;

namespace TFS2;

[Library( "tf_weapon_knife" )]
public partial class Knife : TFMeleeBase
{
	public override void SimulateAttack()
	{
		BackstabVMThink();
		base.SimulateAttack();
	}

	public void BackstabVMThink()
	{
		var foundTarget = false;

		if ( CanAttack() )
		{
			var target = FindBackstabTarget();
			if ( target.IsValid() )
				foundTarget = true;
		}

		SendViewModelAnimParameter( "b_hoverback", foundTarget );
	}

	TFPlayer BackstabVictim { get; set; }
	public bool IsBackstab => BackstabVictim.IsValid();

	public override void PrimaryAttack()
	{
		BackstabVictim = FindBackstabTarget();

		Swing();
		Smack();
		SmackTime = null;
	}

	public override bool CalculateIsAttackCriticalHelper()
	{
		if ( IsBackstab )
			return true;

		return base.CalculateIsAttackCriticalHelper();
	}

	public bool IsBehindAndFacingTarget( SDKPlayer target )
	{
		// Get a vector from owner origin to target origin
		var vecToTarget = (target.WorldSpaceBounds.Center - Owner.WorldSpaceBounds.Center).WithZ( 0 ).Normal;

		// Get owner forward view vector
		var vecOwnerForward = Owner.GetEyeRotation().Forward.WithZ( 0 ).Normal;

		// Get target forward view vector
		var vecTargetForward = target.GetEyeRotation().Forward.WithZ( 0 ).Normal;

		// Make sure owner is behind, facing and aiming at target's back
		float flPosVsTargetViewDot = vecToTarget.Dot( vecTargetForward ); // Behind?
		float flPosVsOwnerViewDot = vecToTarget.Dot( vecOwnerForward );   // Facing?
		float flViewAnglesDot = vecTargetForward.Dot( vecOwnerForward );  // Facestab?

		return flPosVsTargetViewDot > 0 && flPosVsOwnerViewDot > 0.5f && flViewAnglesDot > -0.3f;
	}

	public virtual TFPlayer FindBackstabTarget()
	{
		var tr = TraceFireBullet();
		if ( tr.Entity is TFPlayer player )
		{
			if ( CanPerformBackstabAgainstTarget( player ) )
				return player;
		}

		return null;
	}

	public bool CanPerformBackstabAgainstTarget( SDKPlayer entity )
	{
		if ( !entity.IsValid() )
			return false;

		if ( !SDKGame.mp_friendly_fire && ITeam.IsSame( Owner, entity ) )
			return false;

		return IsBehindAndFacingTarget( entity );
	}

	public override void ApplyDamageModifications( Entity victim, ref ExtendedDamageInfo info, TraceResult trace )
	{
		if ( IsBackstab && victim == BackstabVictim )
			info.Damage = victim.Health;

		base.ApplyDamageModifications( victim, ref info, trace );
	}
}
