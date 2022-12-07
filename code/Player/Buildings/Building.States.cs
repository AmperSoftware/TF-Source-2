using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2
{
	public partial class Building
	{
		public BuildingState State { get; set; }
		public virtual void StateChanged( BuildingState oldState, BuildingState newState )
		{
			OnStateChanged?.Invoke( oldState, newState );
		}
		public event Action<BuildingState, BuildingState> OnStateChanged;
	}

	public enum BuildingState
	{
		Invalid = 0,
		Idle = 1,
		Building = 2,
		Upgrading = 3,
		Disabled = 4
	}
}
