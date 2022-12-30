using Sandbox;
using Editor;

namespace TFS2;

/// <summary>
/// An entity which handles in-map timers
/// (Will probably be replaced by an fp ent sometime)
/// </summary>
[Library( "ent_timer" )]
[Title("Round Timer")]
[Category("Gameplay")]
[Icon("timer")]
[VisGroup( VisGroup.Logic )]
[EditorSprite( "materials/editor/ent_logic.vmat" )]
[HammerEntity]
public partial class LogicTimer : Entity
{
	/// <summary>
	/// The (initial) enabled state of the logic entity.
	/// </summary>
	[Property]
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// Enables the entity.
	/// </summary>
	[Input]
	public void Enable()
	{
		Enabled = true;
	}

	/// <summary>
	/// Disables the entity, so that it would not fire any outputs.
	/// </summary>
	[Input]
	public void Disable()
	{
		Enabled = false;
	}

	/// <summary>
	/// Toggles the enabled state of the entity.
	/// </summary>
	[Input]
	public void Toggle()
	{
		Enabled = !Enabled;
	}
	/// <summary>
	/// If the time should be randomly picked. If not checked, the timer will always be MaxTime long.
	/// </summary>
	public bool RandomizeTime { get; set; } = false;
	public float MaxTime { get; set; } = 10f;
	public float MinTime { get; set; }
	public override void Spawn()
	{
		base.Spawn();

		PickTime();
	}

	TimeSince timeSinceStart;
	bool paused;
	float timePassed;
	float timeToPass;
	[Event.Tick.Server]
	private void Tick()
	{
		if(Enabled)
		{
			if(paused)
			{
				Unpause();
			}
			if(timeSinceStart >= timeToPass)
			{
				Finish();
			}
		}
		else
		{
			if(!paused)
			{
				Pause();
			}
		}
	}
	protected void PickTime()
	{
		if(RandomizeTime)
		{
			timeToPass = Game.Random.Float( MinTime, MaxTime ).Clamp(0, MaxTime);
		}
		else
		{
			timeToPass = MaxTime;
		}
	}
	protected void Finish()
	{
		OnFire.Fire( null );

		PickTime();
	}
	protected void Unpause()
	{
		paused = false;
		timeSinceStart = -timePassed;
	}
	protected void Pause()
	{
		paused = true;
		timePassed = timeSinceStart;
	}
	protected Output OnFire { get; set; }
}
