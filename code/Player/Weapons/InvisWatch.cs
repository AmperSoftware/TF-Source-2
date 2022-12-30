using Amper.FPS;
using Sandbox;

namespace TFS2;

[Library( "tf_weapon_inviswatch", Title = "Invisibility Watch" )]

public partial class InvisWatch : TFMeleeBase, IPassiveChild
{
	public enum InvisWatchTypes
	{ 
		Invis_Normal,	// Stock Invisibility Watch.
		Invis_Motion,	// Cloak and Dagger.
		Invis_Feign,	// Dead Ringer.
	};

	// Stock Invis Watch for now.
	public InvisWatchTypes InvisWatchType { get; set; } = InvisWatchTypes.Invis_Normal;

	public override bool CanPrimaryAttack() => false;

	float StealthNextChangeTime;
	public void PassiveSimulate( IClient cl )
	{
		if ( !WishSecondaryAttack() )
			return;

		if ( StealthNextChangeTime <= Time.Now )
		{
			ActivateInvisibilityWatch();
		}
	}

	public override void Regenerate()
	{
		TFOwner.Invisibility = 0.0f;
	}

	public void ActivateInvisibilityWatch()
	{
		bool ChangedState = false;
		
		if ( TFOwner.InCondition( TFCondition.Cloaked ) )
		{
			TFOwner.FadeInvis( TFPlayer.tf_spy_invis_unstealth_time );
			ChangedState = true;
		}
		else if ( TFOwner.CanGoInvisible() && TFOwner.Invisibility == 0f )
		{
			TFOwner.AddCondition( TFCondition.Cloaked );
			ChangedState = true;
		}

		if ( ChangedState )
			StealthNextChangeTime = Time.Now + TFPlayer.tf_spy_invis_time;
	}

	public void RemoveInvisibility()
	{
		if ( !TFOwner.InCondition( TFCondition.Cloaked ) )
			return;

		TFOwner.FadeInvis( TFPlayer.tf_spy_invis_time );
	}
}
