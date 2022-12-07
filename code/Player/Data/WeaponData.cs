using Sandbox;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TFS2;

public enum TFAttributes
{
	Damage
}


/// <summary>
/// This represents information about a Weapon. Information is sourced from .weapon assets.
/// </summary>

[GameResource( "TF:S2 Weapon Data", "tfweapon", "Team Fortress: Source 2 weapons definitions", Icon = "🔫", IconBgColor = "#ff6861", IconFgColor = "#0e0e0e" )]
public class WeaponData : GameResource
{
	/// <summary>
	/// Title of the weapon that will be displayed to the client.
	/// </summary>
	public string Title { get; set; }
	/// <summary>
	/// Engine entity classname.
	/// </summary>
	public string EngineClass { get; set; }
	/// <summary>
	/// Is this weapon hidden from the selection menu?
	/// </summary>
	public bool Hidden { get; set; }
	/// <summary>
	/// Model of the weapon. Will be used for both viewmodel and worldmodel.
	/// </summary>
	[ResourceType( "vmdl" )]
	public string WorldModel { get; set; }

	[ResourceType( "tfclass" )]
	public Dictionary<string, WeaponOwnerData> Owners { get; set; }


	//
	// Properties
	//

	/// <summary>
	/// Base damage of this weapon. This will be modified by the in-game effects: i.e. crits, penalties, etc.
	/// </summary>
	public float Damage { get; set; }
	/// <summary>
	/// Maximum range of this weapon.
	/// </summary>
	public int Range { get; set; } = 4096;
	/// <summary>
	/// Amount of bullets that can fit inside the clip of this weapon.
	/// </summary>
	public int ClipSize { get; set; }
	/// <summary>
	/// How much bullets are being shot per one attack.
	/// </summary>
	public int BulletsPerShot { get; set; }
	/// <summary>
	/// How much do bullets spread around.
	/// </summary>
	public float BulletSpread { get; set; }
	/// <summary>
	/// How much does the player view is punched when making an attack?
	/// </summary>
	public float PunchAngle { get; set; }
	/// <summary>
	/// How much ammo is consumed by one attack.
	/// </summary>
	public int AmmoPerShot { get; set; } = 1;
	/// <summary>
	/// This defines how this weapon will be reloaded.
	/// </summary>
	public bool ReloadsEntireClip { get; set; }

	//
	// Distance Mod
	//

	/// <summary>
	/// Damage Falloff mechanic: Apply damage decrease at far range.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[Category( "Distance Mod" )]
	public bool UseFalloff { get; set; } = true;
	/// <summary>
	/// Maximum far range damage decrease. Default: 50%.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[Category( "Distance Mod" )]
	public float FalloffMultiplier { get; set; } = .5f;
	/// <summary>
	/// Damage Rampup mechanic: Apply damage increase at close range.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[Category( "Distance Mod" )]
	public bool UseRampup { get; set; } = true;
	/// <summary>
	/// Maximum close range damage increase. Default: 150%.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[Category( "Distance Mod" )]
	public float RampupMultiplier { get; set; } = 1.5f;

	//
	// Timings
	//

	/// <summary>
	/// Amount of time this weapon requires to be deployed. We cannot use and fire this weapon until we wait this amount of time after switching to it.
	/// </summary>
	[Category( "Timings" )]
	public float DeployTime { get; set; }
	/// <summary>
	/// Time that one shot requires to be made. If we fire, we will not be able to make another shot
	/// unti we wait this amount of time.
	/// </summary>
	[Category( "Timings" )]
	public float AttackTime { get; set; }
	/// <summary>
	/// Amount of time that this weapon needs to take to be reloaded. If a shot is being made before this time passes during a reload, it will be cancelled.
	/// </summary>
	[Category( "Timings" )]
	public float ReloadTime { get; set; }
	/// <summary>
	/// A small delay before reload cycle starts. This is commonly used for weapons that reload one clip at a time, to give to for reload_start sequence to be played.
	/// </summary>
	[Category( "Timings" )]
	public float ReloadStartTime { get; set; }
	[Category( "Timings" )]
	public float SmackTime { get; set; } = 2;


	//
	// Images
	//


	/// <summary>
	/// Main image, used to represent the weapon. Is used in the weapon selection menu.
	/// </summary>
	[Category( "Images" )]
	[ResourceType( "png" )]
	public string InventoryIcon { get; set; }

	/// <summary>
	/// This icon is the generic kill icon, used whenever a player makes a kill with this 
	/// weapon. This is also a fallback to all other kill icon types in case those are missing.
	/// </summary>
	[Category( "Images" )]
	[ResourceType( "png" )]
	public string KillFeedIcon { get; set; }
	/// <summary>
	/// This icon is used for special kills of this weapon. For instance: backstabs and headshots count as "special" kills.
	/// </summary>
	[Category( "Images" )]
	[ResourceType( "png" )]
	public string KillFeedIconSpecial { get; set; }
	[Category( "Images" )]
	[ResourceType( "png" )]
	public string Crosshair { get; set; }


	//
	// Particles
	//


	[Category( "Visuals" )]
	public int TracerFrequency { get; set; } = 2;
	[Category( "Visuals" )]
	[ResourceType( "vpcf" )]
	public string TracerRed { get; set; } = "particles/bullet_tracers/bullet_tracer01_red.vpcf";
	[Category( "Visuals" )]
	[ResourceType( "vpcf" )]
	public string TracerBlue { get; set; } = "particles/bullet_tracers/bullet_tracer01_blue.vpcf";
	[Category( "Visuals" )]
	[ResourceType( "vpcf" )]
	public string TracerRedCritical { get; set; } = "particles/bullet_tracers/bullet_tracer01_red_crit.vpcf";
	[Category( "Visuals" )]
	[ResourceType( "vpcf" )]
	public string TracerBlueCritical { get; set; } = "particles/bullet_tracers/bullet_tracer01_blue_crit.vpcf";
	[Category( "Visuals" )]
	[ResourceType( "vpcf" )]
	public string MuzzleFlash { get; set; }


	//
	// Sounds
	//


	/// <summary>
	/// Sound that is played when we made a single shot.
	/// </summary>
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	[Title( "Single Shot" )]
	public string SoundSingle { get; set; }
	/// <summary>
	/// Sound that is played when we made a single critical shot.
	/// </summary>
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	[Title( "Crit Shot" )]
	public string SoundCrit { get; set; }
	/// <summary>
	/// Sound that is played when we made hit world as melee weapons.
	/// </summary>
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	[Title( "Melee Hits World" )]
	public string SoundHitWorld { get; set; }
	/// <summary>
	/// Sound that is played when we made hit flesh as melee weapons.
	/// </summary>
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	[Title( "Melee Hits Flesh" )]
	public string SoundHitFlesh { get; set; }

	protected override void PostLoad()
	{
		Precache.Add( WorldModel );

		// Add this asset to the registry.
		All.Add( this );
	}

	/// <summary>
	/// Creates an instance of this weapon.
	/// </summary>
	/// <returns></returns>
	public TFWeaponBase CreateInstance()
	{
		if ( string.IsNullOrEmpty( EngineClass ) )
			return null;

		var type = TypeLibrary.GetDescription<TFWeaponBase>( EngineClass ).TargetType;
		if ( type == null )
			return null;

		var weapon = TypeLibrary.Create<TFWeaponBase>( EngineClass );
		weapon.Initialize( this );

		return weapon;
	}

	public bool CanBeOwnedByPlayerClass( PlayerClass pclass )
	{
		return Owners?.ContainsKey( pclass.ResourcePath ) ?? false;
	}

	public bool TryGetOwnerDataForPlayerClass( PlayerClass pclass, out WeaponOwnerData data )
	{
		data = default;
		return Owners?.TryGetValue( pclass.ResourcePath, out data ) ?? false;
	}

	/// <summary>
	/// All registered Player Classes are here.
	/// </summary>
	public static List<WeaponData> All { get; set; } = new();

	/// <summary>
	/// Find all weapons that a class can place in one of their slots.
	/// </summary>
	/// <param name="pclass"></param>
	/// <param name="slot"></param>
	/// <returns></returns>
	public static IEnumerable<WeaponData> FindAllForClassAndSlot( PlayerClass pclass, TFWeaponSlot slot )
	{
		return All.Where( x => x.TryGetOwnerDataForPlayerClass( pclass, out var ownerData ) && ownerData.Slot == slot );
	}

	//
	// General
	//

}

public enum ViewModelModeChoices
{
	ParentWeaponToHands,
	ParentHandsToWeapon,
	WeaponOnly
}

public struct WeaponOwnerData
{
	/// <summary>
	/// Slot to assign this weapon to.
	/// </summary>
	public TFWeaponSlot Slot { get; set; }
	/// <summary>
	/// Hold Pose for Animgraph.
	/// </summary>
	public TFHoldPose HoldPose { get; set; }
	/// <summary>
	/// Maximum carried ammo for this weapon when class wears this weapon.
	/// </summary>
	public int Reserve { get; set; }
	/// <summary>
	/// If true, will use "c_" model system
	/// </summary>
	public bool AttachToHands { get; set; }
	/// <summary>
	/// This is the view model of the weapon.<br/>
	/// <b>It will only be used for if "View Model Mode" is set to either "Parent Hands To Weapon" or "Weapon Only".</b>
	/// </summary>
	[ResourceType( "vmdl" )]
	public string ViewModel { get; set; }
}
