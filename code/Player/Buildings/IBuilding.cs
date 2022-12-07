using Amper.FPS;
using Sandbox.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2
{
	public interface IBuilding : IHasMaxHealth, ITeam
	{
		public TFPlayer GetOwner();
	}
}
