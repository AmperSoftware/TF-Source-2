HEADER
{
	Description = "TF2 basic water";
}

MODES
{
	VrForward();
    Depth( "vr_depth_only.vfx" );
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.vfx" );
	ToolsShadingComplexity( "vr_tools_shading_complexity.vfx" );
}

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#include "common/shared.hlsl"

    #define S_TRANSLUCENT 1
    #define DEPTH_STATE_ALREADY_SET 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PS_INPUT
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VertexInput i ) )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

	RenderState( DepthEnable, true );
	RenderState( DepthWriteEnable, false );

    float flRefraction < Default( 1 ); UiGroup( "TF:S2,10/,10/1" );>;
    FloatAttribute(flRefraction, flRefraction);

    float4 g_FogColorTint < Default4( 0.2f, 0.2f, 0.2f, 0.5f ); UiType( Color ); UiGroup( "TF:S2,10/,10/1" ); >;
    Float4Attribute(g_FogColorTint, g_FogColorTint);

    float2 g_fDepthScale < Default2( 0, 100 ); Range2(0, 0, 500, 500); UiGroup( "TF:S2,10/,10/1" );>;
    Float2Attribute(g_fDepthScale, g_fDepthScale);

    float flAlphaFade < Default( 5 ); Range(0, 50); UiGroup( "TF:S2,10/,10/1" );>;
    FloatAttribute(flAlphaFade, flAlphaFade);

    float2 g_fAnimatedGrid < Default2( 2.0, 2.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    Float2Attribute(g_fAnimatedGrid, g_fAnimatedGrid);

    float g_fNumAnimationCells < Range( 0.0, 100.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    FloatAttribute(g_fNumAnimationCells, g_fNumAnimationCells);

    float g_fAnimationTimePerFrame < Range( 0.0, 100.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    FloatAttribute(g_fAnimationTimePerFrame, g_fAnimationTimePerFrame);

    float2 g_fNormalScroll < Default2(0.0, 0.0); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    FloatAttribute(g_fNormalScroll, g_fNormalScroll);

    float4 g_vViewport < Source( Viewport ); >;

	CreateTexture2D( g_tDepthBufferCopyTexture ) <AsSceneDepth; SrgbRead( false );    AddressU( CLAMP ); AddressV( CLAMP ); Filter( POINT ); >;
    BoolAttribute( bWantsFBCopyTexture, true );
    CreateTexture2D( g_tFrameBufferCopyTexture ) < Attribute( "FrameBufferCopyTexture" ); SrgbRead( false );  AddressU( CLAMP ); AddressV( CLAMP ); Filter( MIN_MAG_MIP_LINEAR ); >;

    float FetchDepth( float2 vTexCoord )
    {
        float flProjectedDepth = Tex2D(g_tDepthBufferCopyTexture, vTexCoord.xy);
        // Remap depth to viewport depth range
        flProjectedDepth = RemapValClamped( flProjectedDepth, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0 );

        float flZScale = g_vInvProjRow3.z;
        float flZTran = g_vInvProjRow3.w;

        float flDepthRelativeToRayLength = 1.0 / ( ( flProjectedDepth * flZScale + flZTran ) );

        return flDepthRelativeToRayLength;
    }

	float4 MainPs( PS_INPUT i ) : SV_TARGET0
	{   
		Material m = GatherMaterial( i );

        //Animated Normal
        //float2 scaledWorldPos = ((i.vPositionWithOffsetWs.xy + g_vCameraPositionWs.xy) * g_fScale);
        float2 texUv_scroll = i.vTextureCoords.xy + (g_fNormalScroll * g_flTime);
        float2 uv = (texUv_scroll - floor(texUv_scroll));

		float2 scale = float2(1 / g_fAnimatedGrid.x, 1 / g_fAnimatedGrid.y);
		uv *= scale;
		int currentFrame = (g_flTime / g_fAnimationTimePerFrame) % (g_fNumAnimationCells - 1);
		int yRow = currentFrame / g_fAnimatedGrid.y;
		float2 uvStart = float2((currentFrame % g_fAnimatedGrid.x) / g_fAnimatedGrid.x, yRow / g_fAnimatedGrid.y);

        //Sample normal with new animated uvs
        float3 sampledNormal = Tex2DS(g_tNormal, TextureFiltering, clamp(uv + uvStart, float2(0.001, 0.001), float2(0.999, 0.999))).rgb;

        m.Normal = normalize(sampledNormal);

        //Refraction
        float3 vPositionWs = i.vPositionWithOffsetWs.xyz + g_vCameraPositionWs;

        float4 vPositionPs = Position3WsToPs( vPositionWs + ((float3(m.Normal.x, m.Normal.y, 0) * 2.0f - 1.0f) * flRefraction) );
        vPositionPs.xy /= vPositionPs.w;
        float2 vPositionSs_refracted = PsToSs( vPositionPs );
        vPositionSs_refracted.x = 1.0f - vPositionSs_refracted.x;

        //Getting depth for normal refracted pixel position
        float water_depth = i.vPositionSs.w;
        float refracted_scene_depth = FetchDepth(vPositionSs_refracted);
        
        //Get depth again for the unaltered fragment pixel position
        vPositionPs = Position3WsToPs( vPositionWs );
        vPositionPs.xy /= vPositionPs.w;
        float2 vPositionSs = PsToSs( vPositionPs );
        vPositionSs.x = 1.0f - vPositionSs.x;
        float real_scene_depth = FetchDepth(vPositionSs);

        //Use the unaltered depth + screen space pos if the refracted one is in front of the water.
        bool depthBehindWater = refracted_scene_depth > water_depth;
        float2 position_ss = depthBehindWater ? vPositionSs_refracted : vPositionSs;
        float scene_depth = depthBehindWater ? refracted_scene_depth : real_scene_depth;

        float depth_difference = smoothstep(g_fDepthScale.x, g_fDepthScale.y, abs(water_depth - scene_depth));
        float3 frameBuffer = Tex2D(g_tFrameBufferCopyTexture, position_ss);

        //Set albedo based on the final fog value (depth and alpha)
        float effectiveFog = (depth_difference * g_FogColorTint.a);
        m.Albedo = m.Albedo * lerp(frameBuffer, g_FogColorTint.rgb, effectiveFog);
        //Make the water emissive based on fog amount
        m.Emission = m.Albedo * (1 - effectiveFog);
        //Smoothly step into water using alpha. Helps mitigate the harsh transition
        m.Opacity = smoothstep(0, flAlphaFade, abs(water_depth - scene_depth));

        return FinalizePixelMaterial( i, m );
	}
}