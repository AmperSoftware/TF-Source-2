using Sandbox;
using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amper.FPS;

namespace TFS2
{
	/// <summary>
	/// A payload cart.
	/// </summary>
	[Library("tf_cart", Title = "Payload Cart" ,Group = "Objectives")]
	[Model(Model = "models/props_trainyard/bomb_cart.vmdl" )]
	[HammerEntity]
	public partial class Cart : AnimatedEntity, ITeam
	{
		[ConVar.Replicated] public static bool tf_debug_cart { get; set; } = false;

		#region Properties
		public int TeamNumber => (int)Team;
		[Property("Team")]
		public TFTeam Team { get; set; } = TFTeam.Blue;

		[Property("Path", Title = "Linked Path"), FGDType("target_destination")]
		public string LinkedCartPath { get; set; }
		public CartPath Path { get; protected set; }
		
		[Category( "Speed" ), Property( "Level1Speed", Title = "x1 Speed" )]
		public float Level1Speed { get; set; } = 50f;
		[Category( "Speed" ), Property( "Level2Speed", Title = "x2 Speed" )]
		public float Level2Speed { get; set; } = 70f;
		[Category( "Speed" ), Property( "Level3Speed", Title = "x3 Speed" )]
		public float Level3Speed { get; set; } = 90f;
		/// <summary>
		/// How much the cart can change its speed in HU/s
		/// </summary>
		[Category("Speed"), Property("Accelleration", Title = "Cart Accelleration")]
		public float Acceleration { get; set; } = 70f;

		/// <summary>
		/// After how many seconds the carts starts rolling back automatically.
		/// </summary>
		[Category("Speed/Rollback"), Property("IdleTime", Title = "Automatic Rollback Time")]
		public float IdleTime { get; set; } = 30f;
		[Category( "Speed/Rollback" ), Property( "BackwardsSpeed", Title = "Rollback Speed" )]
		public float BackwardsSpeed { get; set; } = 9f;

		[Category( "Sound" ), Property( "CartMoveSound", Title = "Move Sound" ), FGDType( "sound" )]
		public string MoveSound { get; set; }
		[Category("Sound"), Property("CartStartMoveSound", Title = "Start Move Sound"), FGDType( "sound" )]
		public string StartMoveSound { get; set; }
		[Category( "Sound" ), Property("CartStopMoveSound", Title = "Stop Move Sound" ), FGDType( "sound" )]
		public string StopMoveSound { get; set; }
		/// <summary>
		/// Sound which plays when the cart rolls in a rollback/forward zone.
		/// </summary>
		[Category( "Sound" ), Property("CartGrindSound", Title = "Rollback Sound" ), FGDType( "sound" )]
		public string RollbackSound { get; set; }
		#endregion

		public IReadOnlyList<TFPlayer> Pushers => pushers;
		public IReadOnlyList<TFPlayer> Blockers => blockers;
		protected List<TFPlayer> pushers = new();
		protected List<TFPlayer> blockers = new();
		
		/// <summary>
		/// What node we are currently moving away from
		/// </summary>
		public CartPathNode CurrentNode { get; protected set; }
		/// <summary>
		/// How far away we are from the current node (in percentage from 0 to 1)
		/// </summary>
		public float CurrentFraction { get; protected set; }

		/// <summary>
		/// The time since this cart was last pushed.
		/// </summary>
		public TimeSince TimeSincePush { get; protected set; } = 0;

		/// <summary>
		/// Distance from the current node to the next node
		/// </summary>
		protected float NodeDistance;
		/// <summary>
		/// The speed the cart is currently travelling at.
		/// </summary>
		protected float CurrentSpeed;
		protected bool IsAtEnd = false;

		public override void Spawn()
		{
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			EnableAllCollisions = true;

			Tags.Add( CollisionTags.Solid );
		}

		[Event.Entity.PostSpawn]
		public void PostLevelSetup()
		{
			Path = FindByName( LinkedCartPath ) as CartPath;

			if ( Path == null )
				throw new ArgumentNullException( "Cart HAS to have a cart path set!" );

			Reset();
		}

		public void Reset()
		{
			var firstNode = Path.PathNodes.First();
			Position = firstNode.WorldPosition;
			var dir = Position - Path.GetPointBetweenNodes( firstNode, firstNode.GetNextNode(), 0.05f );
			Rotation = Rotation.LookAt( dir ).Angles().WithRoll( 0 ).ToRotation();

			pushers.Clear();
			blockers.Clear();

			CurrentNode = firstNode;
			CurrentFraction = 0;
			TimeSincePush = 0;
			NodeDistance = Path.GetCurveLength( CurrentNode, CurrentNode.GetNextNode(), 10 );
			CurrentSpeed = 0;
			IsAtEnd = false;
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if ( Path == null )
				Log.Error( $"Cart {this} has no path set!" );

			DoMovement();

			if ( tf_debug_cart )
			{
				Path.DrawPath( 10 );
				var textpos = Position + Vector3.Up * 32f;

				DebugOverlay.Sphere( GetPathPosition(), 8f, Color.Orange );
				
				DebugOverlay.Text( $"[CART]", textpos, 0, Color.Blue );
				DebugOverlay.Text( $"Team: {Team}", textpos, 1, Color.Cyan );
				DebugOverlay.Text( $"Pushers: {pushers.Count}", textpos, 2, Color.Cyan );
				DebugOverlay.Text( $"Blockers: {blockers.Count}", textpos, 3, Color.Cyan );
				DebugOverlay.Text( $"CanPush: {CanPush()}", textpos, 4, CanPush() ? Color.Green : Color.Red );

				DebugOverlay.Text( $"CurrentNode: {CurrentNode} ({CurrentNode.GetIndex()+1}/{Path.PathNodes.Count}, {CurrentNode.GetNextNode()})", textpos, 6, !IsAtEnd ? Color.Green : Color.Red );
				DebugOverlay.Text( $"CurrentFraction: {CurrentFraction}", textpos, 7, Color.Cyan );
				DebugOverlay.Text( $"NodeDistance: {NodeDistance}", textpos, 8, Color.Cyan );
				DebugOverlay.Text( $"CurrentSpeed: {CurrentSpeed} ({GetSpeedFraction(CurrentSpeed * Time.Delta)})", textpos, 9, Color.Cyan );

				DebugOverlay.Text( $"Time Since Push: {TimeSincePush}", textpos, 11, Color.Cyan );
				DebugOverlay.Text( $"CanMove: {CanMove()}", textpos, 12, CanMove() ? Color.Green : Color.Red );
				DebugOverlay.Text( $"CanRollforward: {CanRollforward()}", textpos, 13, CanRollforward() ? Color.Green : Color.Red );
				DebugOverlay.Text( $"CanRollback: {CanRollback()}", textpos, 14, CanRollback() ? Color.Green : Color.Red );
			}
		}

		protected virtual void DoMovement()
		{
			if ( !CanMove() )
				return;

			bool isRolling = false;
			if(CanPush())
			{
				float maxSpeed = 0;
				switch (GetCapRate())
				{
					case 0:
						break;
					case 1:
						maxSpeed = Level1Speed;
						break;
					case 2:
						maxSpeed = Level2Speed;
						break;
					default:
						maxSpeed = Level3Speed;
						break;
				}

				CurrentSpeed += Acceleration * Time.Delta;
				CurrentSpeed = MathF.Min( maxSpeed, CurrentSpeed );

				TimeSincePush = 0;
			}
			else if(CanRollforward())
			{
				isRolling = true;
				float maxSpeed = Level3Speed;

				CurrentSpeed += Acceleration * Time.Delta;
				CurrentSpeed = MathF.Min( maxSpeed, CurrentSpeed );
			}
			else if(CanRollback())
			{
				isRolling = true;
				float minSpeed = -BackwardsSpeed;
				CurrentSpeed -= Acceleration * Time.Delta;
				CurrentSpeed = MathF.Max( minSpeed, CurrentSpeed );
			}
			else
			{
				float minSpeed = 0f;
				CurrentSpeed -= Acceleration * Time.Delta;
				CurrentSpeed = MathF.Max( minSpeed, CurrentSpeed );
			}

			if ( CurrentSpeed < 0 && CurrentNode.GetIndex() == 0 )
			{
				CurrentSpeed = 0;
				return;
			}
			
			if (CurrentSpeed != 0)
			{
				if(isRolling)
				{
					RollbackSounds();
				}
				MoveSounds();

				float remainingDistance = CurrentSpeed * Time.Delta;
				while ( !remainingDistance.AlmostEqual(0.01f) )
				{
					float usedDistance = MathF.Min( remainingDistance * Time.Delta, remainingDistance );
					CurrentFraction += GetSpeedFraction( remainingDistance * Time.Delta );
					remainingDistance -= usedDistance;
					
					if(CurrentFraction >= 1)
					{
						CurrentNode = CurrentNode.GetNextNode();
						CurrentFraction -= 1;
						OnNodeChanged( CurrentNode );

						if ( CurrentNode.GetNextNode() == null )
						{
							IsAtEnd = true;
							StopMoveSounds();
							OnReachEnd.Fire( this );
							Log.Info( $"Reached end at: {CurrentNode.GetIndex()+1}" );
							break;
						}
					}
				}

				var newpos = GetPathPosition();

				var dir = Position - newpos;
				Rotation = Rotation.LookAt( dir ).Angles().WithRoll( 0 ).ToRotation();
				//Position = Position.LerpTo( newpos, Time.Delta );
				Position = newpos;
				if(!IsAtEnd)
					NodeDistance = Path.GetCurveLength( CurrentNode, CurrentNode.GetNextNode(), 10 );
			}
		}
		/// <summary>
		/// Returns the position according to the current node and fraction
		/// </summary>
		/// <returns></returns>
		protected Vector3 GetPathPosition(bool reverse = false)
		{
			if ( IsAtEnd )
				return CurrentNode.WorldPosition;

			return Path.GetPointBetweenNodes( CurrentNode, CurrentNode.GetNextNode(), CurrentFraction, reverse );
		}
		protected float GetSpeedFraction(float speed)
		{
			return speed / NodeDistance;
		}
		
		protected virtual void OnNodeChanged( CartPathNode node )
		{
			//node.OnPass.Fire( this );
			if ( node.GetControlPoint() != null )
			{
				var cp = node.GetControlPoint();

				// Let gamerules know about this.
				TFGameRules.Current.ControlPointCaptured( cp, cp.OwnerTeam, Team, pushers.Select(ply => ply.Client).ToArray() );
				cp.SetOwnerTeam( Team );
			}
		}

		public int GetCapRate()
		{
			return pushers.Sum( ply => TFGameRules.Current.GetCaptureValueForPlayer(ply) );
		}

		public bool CanMove()
		{
			if ( IsAtEnd )
				return false;

			return TFGameRules.Current.PointsMayBeCaptured();
		}
		public bool CanPush()
		{
			if ( blockers.Any() )
				return false;

			return pushers.Any();
		}
		public bool CanRollforward()
		{
			if ( CurrentNode.Mode == PathNodeMode.RollForward )
				return true;
			return false;
		}
		public bool CanRollback()
		{
			if ( CurrentNode.Mode == PathNodeMode.RollBack )
				return true;
			
			if(TimeSincePush >= IdleTime)
			{
				if ( CurrentNode.GetControlPoint() != null && CurrentFraction <= GetSpeedFraction(CurrentSpeed+1) )
				{
					return false;
				}

				return true;
			}

			return false;
		}

		protected virtual void StartPush(TFPlayer ply)
		{
			if ( !pushers.Any() )
			{
				OnStartPush.Fire( ply );
				if ( CanMove() )
					StartMoveSounds();

				if ( blockers.Any() )
					OnStartBlock.Fire( ply );
			}

			pushers.Add( ply );
		}

		protected virtual void StopPush(TFPlayer ply)
		{
			pushers.Remove( ply );
			if ( CanMove() )
				StopMoveSounds();

			if ( !pushers.Any() )
				OnStopPush.Fire(ply);
		}

		protected virtual void StartBlock(TFPlayer ply)
		{
			if ( pushers.Any() )
			{
				if ( CanMove() )
					StartMoveSounds();
				
				OnStartBlock.Fire( ply );
			}

			blockers.Add( ply );
		}

		protected virtual void StopBlock(TFPlayer ply)
		{
			blockers.Remove( ply );

			if ( !blockers.Any() )
			{
				if ( CanMove() )
					StopMoveSounds();
				OnStopBlock.Fire( ply );
			}
		}

		public override void StartTouch( Entity other )
		{
			//Log.Info( "StartTouch" );
			if (other is TFPlayer ply)
			{
				if ( ply.Team == Team )
				{
					StartPush( ply );
				}
				else
				{
					StartBlock( ply );
				}
			}
		}

		public override void EndTouch( Entity other )
		{
			if(other is TFPlayer ply)
			{
				// Remove from both collections in case of team change
				if ( pushers.Contains( ply ) )
					StopPush( ply );
				if ( blockers.Contains( ply ) )
					StopBlock( ply );
			}
		}

		public Output OnStartPush { get; set; }
		public Output OnStopPush { get; set; }
		/// <summary>
		/// Called when 
		/// </summary>
		public Output OnStartBlock { get; set; }
		public Output OnStopBlock { get; set; }
		public Output OnReachEnd { get; set; }
	}
}
