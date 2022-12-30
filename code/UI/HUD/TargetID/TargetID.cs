using Sandbox;
using Sandbox.UI;
using Amper.FPS;
using TFS2.UI;
using System;

namespace TFS2;

[UseTemplate]
public partial class TargetID : Panel
{
	public ITargetID Target { get; set; }
	public HealthCross Cross { get; set; }
	public Image Avatar { get; set; }
	public Label Name { get; set; }
	public Label Subtext { get; set; }
	public Label Pretext { get; set; }
	public InputGlyph InteractionButton { get; set; }
	public Label InteractionText { get; set; }

	public override void Tick()
	{
		var player = TFPlayer.LocalPlayer;
		if ( !player.IsValid() )
			return;

		Target = FindTarget();
		SetClass( "hidden", !ShouldDraw() );

		// Interactions are always calculated even if the panel is not visible so that indicator always goes off while it's invisible.
		SetClass( "has_interaction", UpdateInteraction() );

		if ( !IsVisible )
			return;

		if ( !Target.IsValid() )
			return;

		UpdateTeam();
		UpdateName( Name );

		SetClass( "has_maxhealth", UpdateHealthCross( Cross ) );
		SetClass( "has_subtext", UpdateSubtext( Subtext ) );
		SetClass( "has_avatar", UpdateAvatar( Avatar ) );
		SetClass( "has_pretext", UpdatePretext( Pretext ) );
	}

	public virtual void UpdateTeam()
	{
		SetClass( "red", Target.Team == TFTeam.Red );
		SetClass( "blue", Target.Team == TFTeam.Blue );
	}

	public virtual bool UpdateHealthCross( HealthCross cross )
	{
		cross.DesiredTarget = Target as IHasMaxHealth;
		return cross.DesiredTarget.IsValid();
	}

	public virtual void UpdateName( Label label )
	{
		label.Text = Target?.Name;
	}

	public virtual bool UpdateAvatar( Image image )
	{
		var entity = Target.Entity;
		if ( !entity.IsValid() )
			return false;

		var client = entity.Client;
		if ( entity is TFPlayer && client.IsValid() )
		{
			if ( !CanSeeAvatarOf( client ) )
			{
				if ( !hud_target_id_mask_hidden_avatars )
					return false;

				if ( entity is TFPlayer player )
				{
					var pclass = player.PlayerClass;
					var icon = player.Team == TFTeam.Blue
						? pclass.IconPortraitBlue
						: pclass.IconPortraitRed;

					image.SetTexture( Util.JPGToPNG( icon ) );
				}
			}

			image.SetTexture( Target.Avatar );
			return true;
		}

		image.SetTexture( Util.JPGToPNG( Target.Avatar ) );
		return true;
	}

	public virtual bool UpdatePretext( Label label ) => false;

	public virtual bool UpdateSubtext( Label label )
	{
		if ( Target is TFPlayer player )
		{
			var medigun = player.GetWeaponOfType<Medigun>();
			if ( medigun.IsValid() )
			{
				label.Text = $"ÜberCharge: {MathF.Floor( medigun.ChargeLevel )}%";
				return true;
			}
		}

		return false;
	}

	public virtual bool UpdateInteraction()
	{
		var player = TFPlayer.LocalPlayer;
		if ( !player.IsValid() )
			return false;

		if ( Target is not IInteractableTargetID interactable )
			return false;

		if ( !interactable.CanInteract( player ) )
			return false;

		var entity = Target.Entity;
		if ( entity.IsValid() )
		{
			if ( !player.CanUse( entity ) )
				return false;
		}

		InteractionButton.Button = interactable.InteractButton;
		InteractionText.Text = interactable.InteractText;

		return true;
	}

	public bool ShouldDraw()
	{
		return Target.IsValid();
	}

	public virtual ITargetID FindTarget() => null;

	public enum AvatarVisibility
	{
		Everyone,
		Friends,
		Noone
	}

	public bool CanSeeAvatarOf( IClient client )
	{
		switch ( hud_target_id_show_avatars )
		{
			case AvatarVisibility.Everyone:
				return true;

			case AvatarVisibility.Friends:
				var friend = new Friend( client.SteamId );
				return friend.IsFriend;

			default:
				return false;
		}
	}

	[ConVar.Client] public static AvatarVisibility hud_target_id_show_avatars { get; set; } = AvatarVisibility.Friends;
	[ConVar.Client] public static bool hud_target_id_mask_hidden_avatars { get; set; } = true;
}
