using Amper.FPS;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2
{
	public abstract partial class Building : AnimatedEntity, IBuilding, IAcceptsExtendedDamageInfo
	{
		public int Level { get; set; }
		public BuildingData.LevelData LevelData => Data.GetLevel( Level );
		public float MaxHealth { get; set; }
		public BuildingData Data { get; set; }
		public bool IsInitialized { get; protected set; } = false;
		

		public override void Spawn()
		{
			Tags.Add( CollisionTags.Solid ); // TODO: Should we make this a "player"?
		}

		public virtual void Initialize(TFPlayer owner, BuildingData data)
		{
			Owner = owner;

			Data = data;

			IsInitialized = true;
		}
		public int TeamNumber => GetOwner().TeamNumber;

		public TFPlayer GetOwner()
		{
			return (TFPlayer)Owner;
		}

		public void TakeDamage( ExtendedDamageInfo info )
		{
			// Buildings dont take crits
			// Buildings dont take falloff
			info = info.WithoutFlag( TFDamageFlags.Critical | TFDamageFlags.MiniCritical | TFDamageFlags.UseDistanceMod );

			Health -= info.Damage;
		}
	}
}
