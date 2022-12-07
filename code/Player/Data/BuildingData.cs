using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2
{
	[GameResource("TF:S2 Building", "tfbuild", "A building for TF:S2", Icon = "construction", IconBgColor = "#ffc84f", IconFgColor = "#0e0e0e" )]
	public class BuildingData : GameResource
	{
		public static IReadOnlyList<BuildingData> All => _all;
		private static List<BuildingData> _all = new();
		public string Title { get; set; }
		/// <summary>
		/// Engine entity classname.
		/// </summary>
		public string EngineClass { get; set; }

		#region Levels

		public class LevelData
		{
			public float Health { get; set; } = 150f;
			[ResourceType( "vmdl" )]
			public string Model { get; set; }
		}
		
		public List<LevelData> Levels { get; set; }
		public LevelData GetLevel( int level )
		{
			if ( level < 0 )
				throw new ArgumentException( "Level cannot be negative!", nameof( level ) );

			if ( level == 0 )
			{
				// quality - 2022/12/4
				// We want to make the level passed as an argument match up with the actual building level
				// but someone might use this function like a regular list index accessor (0 = first element and therefore level 1)
				// since thats how it works for most other functions ill make it so using it in that way is also valid, might have to revisit this later.
				level = 1;
			}

			level -= 1;
			if ( level > Levels.Count )
			{
				throw new ArgumentException( "Level is higher then max level of building!", nameof( level ) );
			}

			return Levels[level];
		}
		#endregion Levels

		public int ConstructionCost { get; set; } = 100;
		public int UpgradeCost { get; set; } = 200;
		public float ConstructionTime { get; set; } = 1f;

		/// <summary>
		/// Get data for the specified building level
		/// (1 = level 1, 2 = level 2, etc)
		/// </summary>
		

		protected override void PostLoad()
		{
			if ( _all.Contains( this ) )
				return;

			_all.Add( this );
		}
	}
}
