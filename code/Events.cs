using Sandbox;
using Amper.FPS;
using System.Collections.Generic;

namespace TFS2;

#region Player

[EventDispatcherEvent]
public class PlayerSpawnEvent : DispatchableEventBase
{
	public IClient Client { get; set; }
	public TFTeam Team { get; set; }
	public PlayerClass Class { get; set; }
}

[EventDispatcherEvent]
public class PlayerDeathEvent : DispatchableEventBase
{
	public IClient Victim { get; set; }
	public IClient Attacker { get; set; }
	public IClient Assister { get; set; }
	public WeaponData Weapon { get; set; }
	public HashSet<string> Tags { get; set; }
	public HashSet<string> Flags
	{
		get
		{
			return Tags;
		}
		set
		{
			Tags = value;
		}
	}
	public Vector3 Position { get; set; }
	public float Damage { get; set; }
}

[EventDispatcherEvent]
public class PlayerHurtEvent : DispatchableEventBase
{
	public IClient Victim { get; set; }
	public IClient Attacker { get; set; }
	public IClient Assister { get; set; }
	public WeaponData Weapon { get; set; }
	public string[] Tags { get; set; }
	public Vector3 Position { get; set; }
	public float Damage { get; set; }
}

[EventDispatcherEvent]
public class PlayerChangeClassEvent : DispatchableEventBase
{
	public IClient Client { get; set; }
	public PlayerClass Class { get; set; }
}

[EventDispatcherEvent]
public class PlayerChangeTeamEvent : DispatchableEventBase
{
	public IClient Client { get; set; }
	public TFTeam Team { get; set; }
}

[EventDispatcherEvent]
public class PlayerRegenerateEvent : DispatchableEventBase
{
	public IClient Client { get; set; }
}

#endregion

#region Game

[EventDispatcherEvent]
public class GameRestartEvent : DispatchableEventBase { }

[EventDispatcherEvent]
public class GameOverEvent : DispatchableEventBase { }

#endregion

#region Control Points

[EventDispatcherEvent]
public class ControlPointCapturedEvent : DispatchableEventBase
{
	public ControlPoint Point { get; set; }
	public TFTeam NewTeam { get; set; }
	public IClient[] Cappers { get; set; }
}

#endregion
