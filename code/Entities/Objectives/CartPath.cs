using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;
using Editor;

namespace TFS2
{
	[Library("tf_cart_path"), Path("tf_cart_path_node")]
	[HammerEntity]
	public class CartPath : BasePathEntity<CartPathNode>
	{
		/// <summary>
		/// The mode of a section of the track
		/// </summary>
		public class Section
		{
			/// <summary>
			/// The distance of this section as fraction (0..1)
			/// </summary>
			public float Distance { get; init; }
			public PathNodeMode Mode { get; init; }
		}
		public class ControlPointInfo
		{
			/// <summary>
			/// The distance of this section as fraction (0..1)
			/// </summary>
			public float Distance { get; init; }
			public ControlPoint Point { get; init; }
		}
		public Section[] GetSections()
		{
			var sections = new List<Section>();

			CartPathNode start = PathNodes[0];
			PathNodeMode currentMode = PathNodes[0].Mode;
			foreach (var node in PathNodes)
			{
				var mode = node.Mode;
				if(currentMode != mode)
				{
					sections.Add( new Section
					{
						Distance = GetFraction(GetCurveLength(start, node, 10 )),
						Mode = currentMode
					} );

					start = node;
					currentMode = node.Mode;
				}
			}

			CartPathNode lastNode = PathNodes.Last();
			if (start != lastNode)
			{
				// Add last section
				sections.Add( new Section
				{
					Distance = GetFraction( GetCurveLength( start, lastNode, 10 ) ),
					Mode = currentMode
				} );
			}

			return sections.ToArray();
		}
		public IEnumerable<ControlPointInfo> GetControlPoints()
		{
			var cpNodes = PathNodes.Where( node => node.GetControlPoint() != null );
			return cpNodes.Select( node => new ControlPointInfo()
			{
				Distance = GetNodeDistance( node ),
				Point = node.GetControlPoint()
			} );
		}
		/// <summary>
		/// How much of the path a certain distance takes up.
		/// </summary>
		/// <param name="distance">Distance in Hammer Units</param>
		/// <returns>Distance in a fraction between 0 to 1 (float)</returns>
		public float GetFraction( float distance )
		{
			return MathX.Clamp( distance / GetFullLength(), 0f, 1f );
		}

		public float GetFraction( CartPathNode node )
		{
			return GetFraction( GetNodeDistance( node ) );
		}
		public float GetFullLength()
		{
			float length = 0;
			for ( int i = 0; i < PathNodes.Count - 1; i++ )
			{
				length += GetCurveLength( PathNodes[i], PathNodes[i + 1], 10 );
			}

			return length;
		}
		
		public float GetNodeDistance(CartPathNode node)
		{
			float length = 0;
			for ( int i = 0; i < PathNodes.Count - 1; i++ )
			{
				if ( PathNodes[i] == node )
				{
					return length;
				}
				length += GetCurveLength( PathNodes[i], PathNodes[i + 1], 10 );
			}

			return length;
		}

		public override void DrawPath( int segments, bool drawTangents = false )
		{
			for ( var nodeid = 0; nodeid < PathNodes.Count; nodeid++ )
			{
				BasePathNode node = PathNodes[nodeid];

				Vector3 nodePos = node.WorldPosition;

				// Nodes & IDs
				DebugOverlay.Sphere( nodePos, 4, Color.White );
				DebugOverlay.Text( $"{nodeid + 1}", nodePos + Vector3.Up * 12, 0, Color.White, 0, 2500 );
				if(node is CartPathNode cartNode)
				{
					DebugOverlay.Text( $"Mode: {cartNode.Mode}", nodePos + Vector3.Up * 12, 1, Color.White, 0, 2500 );
					if ( !string.IsNullOrEmpty(cartNode.LinkedControlPoint) )
					{
						DebugOverlay.Text( $"Linked CP Name: {cartNode.LinkedControlPoint}", nodePos + Vector3.Up * 12, 2, Color.White, 0, 2500 );
						if(cartNode.GetControlPoint() != null)
							DebugOverlay.Text( $"Control Point: {cartNode.GetControlPoint().PrintName}", nodePos + Vector3.Up * 12, 3, Color.White, 0, 2500 );
					}
				}
				

				// Tangents
				if ( drawTangents )
				{
					Vector3 nodeTanIn = node.WorldTangentIn;
					DebugOverlay.Sphere( nodeTanIn, 2, Color.Yellow );
					DebugOverlay.Line( nodePos, nodeTanIn, Color.Yellow );

					Vector3 nodeTanOut = node.WorldTangentOut;
					DebugOverlay.Sphere( nodeTanOut, 6, Color.Orange );
					DebugOverlay.Line( nodePos, nodeTanOut, Color.Orange );
				}

				// The path itself
				BasePathNode nodeNext = (nodeid + 1 < PathNodes.Count) ? PathNodes[nodeid + 1] : null;
				if ( nodeNext == null ) continue;

				for ( int i = 1; i <= segments; i++ ) // Starting from i = 1 because i = 0 is start.Position
				{
					var lerpPos = GetPointBetweenNodes( node, nodeNext, (float)i / segments );

					DebugOverlay.Line( nodePos, lerpPos, Color.Green );

					nodePos = lerpPos;
				}
			}
		}
	}

	[Library("tf_cart_path_node"), PathNode]
	public partial class CartPathNode : BasePathNode
	{

		[Property( "CartMode", Title = "Movement Mode" )]
		public PathNodeMode Mode { get; set; } = PathNodeMode.Default;
		protected CartPath Path => PathEntity as CartPath;

		/// <summary>
		/// This control point gets captured if a cart passes over this node.
		/// </summary>
		[Property, FGDType("target_destination")]
		public string LinkedControlPoint { get; set; }

		public ControlPoint GetControlPoint()
		{
			return Entity.FindByName( LinkedControlPoint ) as ControlPoint;
		}


		/// <summary>
		/// Gets the next node in the current path.
		/// </summary>
		/// <returns></returns>
		public CartPathNode GetNextNode()
		{
			if ( Path == null ) return null;

			int index = Path.PathNodes.IndexOf( this );
			if ( index == -1 ) return null;

			index++;
			if ( index >= Path.PathNodes.Count ) return null;

			return Path.PathNodes[index] as CartPathNode;
		}
		/// <summary>
		/// Gets the previous node in the current path.
		/// </summary>
		/// <returns></returns>
		public CartPathNode GetPreviousNode()
		{
			int index = GetIndex() - 1;
			if ( index < 0 )
				return null;
			return Path.PathNodes[index];
		}

		public int GetIndex()
		{
			return Path.PathNodes.IndexOf( this );
		}
	}

	public enum PathNodeMode
	{
		Default,
		RollBack,
		RollForward
	}
}
