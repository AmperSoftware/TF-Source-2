using Sandbox;
using Amper.FPS;
using System;

namespace TFS2;

partial class TFWeaponBase
{
	[ConVar.Client] public static bool cl_crosshair { get; set; } = true;
	[ConVar.Client] public static float cl_crosshair_scale { get; set; } = 24;
	[ConVar.Client] public static float cl_crosshair_color_r { get; set; } = 255;
	[ConVar.Client] public static float cl_crosshair_color_g { get; set; } = 255;
	[ConVar.Client] public static float cl_crosshair_color_b { get; set; } = 255;
	[ConVar.Client] public static float cl_crosshair_alpha { get; set; } = 255;

	public override void DrawCrosshair( Vector2 screenSize, Vector2 center )
	{
		if ( !cl_crosshair )
			return;

		if ( !ShouldDrawCrosshair() )
			return;

		var path = Data.Crosshair;

		if ( string.IsNullOrEmpty( path ) )
			return;

		var imageSize = MathF.Max( cl_crosshair_scale, 1 ).ResY();
		imageSize *= CrosshairScale();

		var x = center.x - imageSize / 2;
		var y = center.y - imageSize / 2;

		var r = Math.Clamp( cl_crosshair_color_r, 0, 255 ) / 255;
		var g = Math.Clamp( cl_crosshair_color_g, 0, 255 ) / 255;
		var b = Math.Clamp( cl_crosshair_color_b, 0, 255 ) / 255;
		var a = Math.Clamp( cl_crosshair_alpha, 0, 255 ) / 255;
		var color = new Color( r, g, b, a );
		
		var mat = Material.UI.Basic;
		var tex = Texture.Load( FileSystem.Mounted, Util.JPGToPNG( path ) );
		// TODO: Render crosshair again

		Rect size = new( x, y, imageSize, imageSize );
		//Vertex
		Graphics.DrawQuad( size, mat, color);

		//draw.Rect( x, y, imageSize, imageSize );
	}

	public virtual bool ShouldDrawCrosshair() => true;
	public virtual float CrosshairScale() => 1;
}

/*
	Kept to help with future crosshair implementation

public static void Rect( this Render.Render2D draw, float x, float y, float width, float height )
	{
		var lt = new Vector2( x, y );
		var ltUv = new Vector2( 0, 0 );
		var rt = new Vector2( x + width, y );
		var rtUv = new Vector2( 1, 0 );
		var lb = new Vector2( x, y + height );
		var lbUv = new Vector2( 0, 1 );
		var rb = new Vector2( x + width, y + height );
		var rbUv = new Vector2( 1, 1 );
		draw.MeshStart();
		var color = draw.Color;
		draw.AddVertex( in lt, in color, in ltUv );
		color = draw.Color;
		draw.AddVertex( in rt, in color, rtUv );
		color = draw.Color;
		draw.AddVertex( in lb, in color, lbUv );
		color = draw.Color;
		draw.AddVertex( in rt, in color, rtUv );
		color = draw.Color;
		draw.AddVertex( in lb, in color, lbUv );
		color = draw.Color;
		draw.AddVertex( in rb, in color, rbUv );
		draw.MeshEnd();
	}
*/
