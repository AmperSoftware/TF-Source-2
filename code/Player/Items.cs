using Sandbox;

namespace TFS2
{
	partial class TFPlayer
	{
		[Net] public Item PickedItem { get; set; }
		public bool HasPickedItem => PickedItem != null;

		public virtual bool WishDrop()
		{
			return Input.Pressed( InputButton.Drop );
		}

		public virtual void SimulateItems()
		{
			if ( Game.IsServer && PickedItem != null )
			{
				if ( WishDrop() )
				{
					DropPickedItem();
				}
			}
		}

		/// <summary>
		/// See if we can pickup a specific pickup.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual bool CanPickup( Item item )
		{
			return IsAlive && PickedItem == null;
		}

		public void DropPickedItem()
		{
			if ( !Game.IsServer )
				return;

			if ( PickedItem == null )
				return;

			using ( Prediction.Off() )
			{
				PickedItem.Drop( this, true, true );
			}
		}
	}
}
