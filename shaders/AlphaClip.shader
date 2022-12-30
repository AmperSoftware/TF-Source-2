HEADER
{
	Description = "TF2 Alpha Clip Implementation";
}

FEATURES
{
    #include "common/features.hlsl"

    Feature( F_SOFT_EDGE, 0..1, "Distance Alpha" );
    Feature( F_OUTLINE, 0..1, "Distance Alpha" );
    Feature( F_GLOW, 0..1, "Distance Alpha" );
}


MODES
{
    VrForward();													// Indicates this shader will be used for main rendering
    Depth( "vr_depth_only.vfx" ); 									// Shader that will be used for shadowing and depth prepass
    ToolsVis( S_MODE_TOOLS_VIS ); 									// Ability to see in the editor
    ToolsWireframe( "vr_tools_wireframe.vfx" ); 					// Allows for mat_wireframe to work
	ToolsShadingComplexity( "vr_tools_shading_complexity.vfx" ); 	// Shows how expensive drawing is in debug view
}

//=========================================================================================================================
COMMON
{
	#include "common/shared.hlsl"

    StaticCombo( S_SOFT_EDGE, F_SOFT_EDGE, Sys( PC ) );

    #if S_SOFT_EDGE
    #define S_TRANSLUCENT 1
    #endif
}

//=========================================================================================================================

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

//=========================================================================================================================

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"
	//
	// Main
	//
	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VS_INPUT i ) )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here
		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{
    #include "common/pixel.hlsl"
	
    StaticCombo( S_SOFT_EDGE, F_SOFT_EDGE, Sys( PC ) );
    StaticCombo( S_OUTLINE, F_OUTLINE, Sys( PC ) );
    StaticCombo( S_GLOW, F_GLOW, Sys( PC ) );

    float g_flUnlitPower< Default( 0.0f ); Range(0.0f, 1.0f); UiGroup( "Alpha Clip" ); >;
    float2 g_flFadeRange< Default2( 200.0f, 250.0f ); Range2(0.0f, 0.0f, 1000.0f, 1000.0f); UiGroup( "Alpha Clip" ); >;

    #if S_SOFT_EDGE
    float2 g_flSoftEdgeRange< Default2( 0.46f, 0.5f ); Range2(0.0f, 0.0f, 1.0f, 1.0f); UiGroup( "Soft Edges" ); >;
    #else
    float g_flClipOpacity< Default( 0.5f ); Range(0.0f, 1.0f); UiGroup( "Alpha Clip" ); >;
    #endif

    #if S_OUTLINE
    float2 g_flOutlineRange< Default2( 0.45f, 0.55f ); Range2(0.0f, 0.0f, 1.0f, 1.0f); UiGroup( "Outline" ); >;
    float3 g_flOutlineColor< Default3( 0.0f, 0.0f, 0.0f ); UiType( Color ); UiGroup( "Outline" ); >;
    #endif

    #if S_GLOW
    float2 g_flGlowOffset< Default2( 0.0f, 0.0f ); Range2(-1.0f, -1.0f, 1.0f, 1.0f); UiGroup( "Glow" ); >;
    float2 g_flGlowRange< Default2( 0.0f, 0.0f ); Range2(0.0f, 0.0f, 1.0f, 1.0f); UiGroup( "Glow" ); >;
    float4 g_flGlowColor< Default4( 0.0f, 0.0f, 0.0f, 1.0f ); UiType( Color ); UiGroup( "Glow" ); >;
    #endif

    CreateInputTexture2D( BaseTexture, Srgb, 8, "", "", "Alpha Clip", Default4( 1.0, 1.0, 1.0, 1.0 ) );
    CreateTexture2D( g_tBaseTexture ) < Channel( RGBA, None( BaseTexture ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); Filter(BILINEAR); >;

	float4 MainPs( PixelInput i ) : SV_TARGET0
	{
		Material m = GatherMaterial( i );

        float4 baseSample = Tex2D(g_tBaseTexture, i.vTextureCoords.xy);
        m.Albedo = baseSample.rgb;

        //Correct alpha to be from 0-1 for some things
        #if S_SOFT_EDGE
        float colorA = baseSample.a / g_flSoftEdgeRange.y;
        #else
        float colorA = baseSample.a / g_flClipOpacity;
        #endif

        #if S_OUTLINE
        //If alpha is in outline range
        if((baseSample.a >= g_flOutlineRange.x) && (baseSample.a <= g_flOutlineRange.y))
        {
            float oFactor = 1.0f;
            if(baseSample.a <= g_flOutlineRange.y)
            {
                oFactor = smoothstep(g_flOutlineRange.x, g_flOutlineRange.y, baseSample.a);
            }
            else
            {
                oFactor = smoothstep(g_flOutlineRange.y, g_flOutlineRange.x, baseSample.a);
            }
            m.Albedo = lerp(m.Albedo, g_flOutlineColor, oFactor);
        }
        #endif

        #if S_SOFT_EDGE
        m.Opacity = baseSample.a * smoothstep(g_flSoftEdgeRange.x, g_flSoftEdgeRange.y, baseSample.a);
        #else
        m.Opacity = baseSample.a >= g_flClipOpacity;
        clip(baseSample.a - g_flClipOpacity);
        #endif

        #if S_GLOW
    	float4 glowTexel = Tex2D(g_tBaseTexture, i.vTextureCoords.xy + g_flGlowOffset);
        float4 glowc = g_flGlowColor * smoothstep(g_flGlowRange.x, g_flGlowRange.y, glowTexel.a);
        //float glowRatio = smoothstep(g_flGlowRange.x, g_flGlowRange.y, glowTexel.a);

        #if S_SOFT_EDGE
        float isGlow = baseSample.a < g_flGlowRange.y;
        #else
        float isGlow = m.Opacity;
        #endif

		m.Albedo = lerp(m.Albedo, glowc.rgb, isGlow);
		m.Opacity = lerp(m.Opacity, glowc.a, isGlow);
        #endif

        //Distance fade
        float distanceFromCamera = length(i.vPositionWithOffsetWs.xyz);
        m.Opacity = lerp(m.Opacity, 0, smoothstep(g_flFadeRange.x, g_flFadeRange.y, distanceFromCamera));

		ShadingModelValveStandard sm;
		float4 o = FinalizePixelMaterial( i, m, sm );

        #if S_SOFT_EDGE
        o.a = m.Opacity;
        #endif

        o.rgb = lerp(o.rgb, m.Albedo, g_flUnlitPower);

        return o;
	}
}