using Sandbox;
using Amper.FPS;

namespace TFS2;

public class HeatmapRecorder
{
	public enum SaveMode
	{
		GameEnd,
		RoundEnd
	}

	public bool Stopped { get; set; } = false;

	protected Heatmap recording = new();
	/// <summary>
	/// When to stop the recording
	/// </summary>
	protected SaveMode Mode;
	protected string SaveFile;

	public HeatmapRecorder( string saveFile, SaveMode m )
	{
		Event.Register( this );

		EventDispatcher.Subscribe<RoundEndEvent>( OnRoundEnd, this );
		EventDispatcher.Subscribe<GameOverEvent>( OnGameEnd, this );

		EventDispatcher.Subscribe<PlayerDeathEvent>( e =>
		{
			if ( Game.IsClient )
				// We only record on the server
				return;

			Log.Info( "PlayerDeath" );

			if ( !TFGameRules.Current.IsRoundActive )
				// dont count humiliation and pregame kills
				return;

			recording.AddData( "PlayerDeath", e.Victim.Pawn.Position );
		}, this );

		SaveFile = saveFile;
		Mode = m;
	}

	public void Stop()
	{
		Stopped = true;
		Event.Unregister( this );

		EventDispatcher.Unsubscribe<PlayerDeathEvent>( this );
		EventDispatcher.Unsubscribe<GameOverEvent>( this );
		EventDispatcher.Unsubscribe<RoundEndEvent>( this );

		recording.SaveToFile(SaveFile);
	}

	void OnGameEnd( GameOverEvent args )
	{
		if(Mode == SaveMode.GameEnd)
			Stop();
	}

	void OnRoundEnd( RoundEndEvent args )
	{
		if ( Mode == SaveMode.RoundEnd )
			Stop();
	}

	#region Recording via Command
	/// <summary>
	/// The recorder created via cmds
	/// </summary>
	private static HeatmapRecorder cmdRecorder;
	[ConCmd.Server("tf_heatmap_record")]
	public static void Record(string name, SaveMode mode = SaveMode.GameEnd, int grid = 16, int gridvertical = 32)
	{
		if(cmdRecorder != null)
		{
			StopCurrent();
		}

		cmdRecorder = new( name, mode );
		Log.Info( "Started recording heatmap..." );
	}

	[ConCmd.Server("tf_heatmap_stop")]
	public static void StopCurrent()
	{
		if(cmdRecorder == null)
		{
			Log.Warning( $"Tried to stop with no running heatmap recording!" );
			return;
		}

		cmdRecorder.Stop();
		cmdRecorder = null;

		Log.Info( "Stopped recording heatmap" );
	}

	#endregion
}
