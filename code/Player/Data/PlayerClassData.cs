using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace TFS2;

public enum TFPlayerClass
{
	Undefined = -1,
	Scout,
	Soldier,
	Pyro,
	Demoman,
	Heavy,
	Engineer,
	Medic,
	Sniper,
	Spy
}

/// <summary>
/// This represents the Team Fortress player class. TFPlayer.PlayerClass is set to this when player chooses the class they want to play.
/// These are defined by creating .tfclass assets in the gamemode working directory.
/// </summary>
[GameResource( "TF:S2 Player Class", "tfclass", "Team Fortress: Source 2 player class definition", Icon = "accessibility_new", IconBgColor = "#ffc84f", IconFgColor = "#0e0e0e" )]
public class PlayerClass : GameResource
{
	/// <summary>
	/// All registered Player Classes are here.
	/// </summary>
	public static Dictionary<string, PlayerClass> All { get; set; } = new();

	/// <summary>
	/// TFPlayerClass -> string associations.
	/// </summary>
	public static Dictionary<TFPlayerClass, string> Names { get; set; } = new()
	{
		{ TFPlayerClass.Undefined, "undefined" },
		{ TFPlayerClass.Scout, "scout" },
		{ TFPlayerClass.Soldier, "soldier" },
		{ TFPlayerClass.Pyro, "pyro" },
		{ TFPlayerClass.Demoman, "demoman" },
		{ TFPlayerClass.Heavy, "heavy" },
		{ TFPlayerClass.Engineer, "engineer" },
		{ TFPlayerClass.Medic, "medic" },
		{ TFPlayerClass.Sniper, "sniper" },
		{ TFPlayerClass.Spy, "spy" }
	};

	public static Dictionary<string, TFPlayerClass> Entries { get; set; } = Names.ToDictionary( x => x.Value, x => x.Key );
	[HideInEditor] public TFPlayerClass Entry => Entries.TryGetValue( ResourceName, out var entry ) ? entry : TFPlayerClass.Undefined;

	//
	// General
	//

	public string Title { get; set; }
	public float MaxHealth { get; set; }
	public float MaxSpeed { get; set; }
	public float EyeHeight { get; set; } = 75;

	//
	// Models
	//

	[ResourceType( "vmdl" )] public string Model { get; set; }
	[ResourceType( "vmdl" )] public string Hands { get; set; }
	[ResourceType( "tftalker" )] public string Responses { get; set; }

	//
	// Weapons
	//

	[ResourceType( "tfweapon" )] public List<string> DefaultWeapons { get; set; }
	[ResourceType( "building" )] public List<string> Buildings { get; set; }

	//
	// Abilities
	//

	public PlayerClassAbilities Abilities { get; set; }
	public struct PlayerClassAbilities
	{
		public PlayerClassAbilities() { }

		/// <summary>
		/// How many jumps mid-air this class will be able to perform?
		/// </summary>
		public int AirBorneJumps { get; set; }
		/// <summary>
		/// How much metal does this class have?
		/// </summary>
		public int Metal { get; set; }
		/// <summary>
		/// Is this class immune to afterburn?
		/// </summary>
		public bool AfterBurnImmune { get; set; }
		/// <summary>
		/// How many "cappers" does this class contribute to the control point capturing? (i.e. Scout counts as 2 players in live.)
		/// </summary>
		public int CaptureValue { get; set; }
		/// <summary>
		/// Max health regen rate.
		/// </summary>
		public float AutoRegenHealth { get; set; }
		/// <summary>
		/// Time to reach max health regen after taking damage.
		/// </summary>
		public float AutoRegenPeakTime { get; set; }
		/// <summary>
		/// Can this class see the enemy health?
		/// </summary>
		public bool CanSeeEnemyHealth { get; set; }
		/// <summary>
		/// Multiplier for the damage that this class will be taking from blasts, invoked by owned weapons.
		/// </summary>
		public float BlastJumpDamageMultiplier { get; set; } = 1;
		/// <summary>
		/// How much we scale our blast force, calculated from damage while being airborne.
		/// </summary>
		public float BlastJumpForceScale { get; set; } = 9;
		/// <summary>
		/// How much we scale our blast force, calculated from damage while we are on the ground at the moment of 
		/// explosion.
		/// </summary>
		public float BlastJumpForceScaleGrounded { get; set; } = 9;
		/// <summary>
		/// How much should resist to being pushed by weapon damage.
		/// </summary>
		public float DamagePushResistance { get; set; } = 1;
		[HideInEditor] public bool HasMetal => Metal > 0;
	}

	//
	// Visuals
	//

	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconRed { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconBlue { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconPortraitRed { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconPortraitBlue { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconPortraitInactive { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconSelectionRed { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconSelectionBlue { get; set; }
	[Category( "Icons" )]
	[ResourceType( "png" )]
	public string IconSelectionInactive { get; set; }

	protected override void PostLoad()
	{
		base.PostLoad();
		Precache.Add( Model );
		Precache.Add( Hands );

		// Get lowercase class name
		string classname = ResourceName.ToLower();
		All[classname] = this;
	}

	public WeaponData GetDefaultWeaponForSlot( TFWeaponSlot slot )
	{
		var weapons = DefaultWeapons.Select( x => ResourceLibrary.Get<WeaponData>( x ) );

		foreach ( var weapon in weapons )
		{
			if ( weapon == null )
				continue;

			if ( !weapon.TryGetOwnerDataForPlayerClass( this, out var data ) )
				continue;

			if ( data.Slot == slot )
				return weapon;
		}

		return null;
	}

	public bool IsDefaultWeapon( WeaponData data )
	{
		return DefaultWeapons.Any( x => x == data.ResourcePath );
	}

	public string GetTag()
	{
		return $"class_{ResourceName}";
	}

	public static bool IsValid( string name )
	{
		name = name.ToLower();

		return All.ContainsKey( name );
	}

	public static PlayerClass Get( string name )
	{
		name = name.ToLower();

		if ( !IsValid( name ) )
			return null;

		return All[name];
	}

	public static PlayerClass Get( TFPlayerClass pclass )
	{
		string name = Names[pclass];
		if ( !IsValid( name ) )
			return null;

		return All[name];
	}
}
