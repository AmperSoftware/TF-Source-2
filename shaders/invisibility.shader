HEADER
{
    Description = "Invisibility Effect For TFS2";
}

MODES
{
    VrForward();
    ToolsVis( S_MODE_TOOLS_VIS );
    Depth( "vr_depth_only.vfx" );
    ProjectionDepth( "vr_depth_only.vfx" );
}

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
    #include "common/shared.hlsl"

    #define S_TRANSLUCENT 1
    #define BLEND_MODE_ALREADY_SET
    #define DEPTH_STATE_ALREADY_SET
}

struct VertexInput
{
    #include "common/vertexinput.hlsl"
};

struct PixelInput
{
    #include "common/pixelinput.hlsl"
};

VS
{
    #include "common/vertex.hlsl"

    PixelInput MainVs( INSTANCED_SHADER_PARAMS( VS_INPUT i ) )
    {
        PixelInput o = ProcessVertex( i );
        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    RenderState( DepthWriteEnable, true );

    float3 g_flBlurTint < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "TF:S2,10/Invis,10/1" );>;
    Float3Attribute(g_flBlurTint, g_flBlurTint);

    //Main input to control the invisibility effect
    float g_InvisBlend < Default( 1.0 ); Range( 0, 1 ); UiGroup( "TF:S2,10/Invis,10/1" );>;
    FloatAttribute(g_InvisBlend, g_InvisBlend);

    float g_SmoothStep < Default( 0.2 ); Range( 0, 1 ); UiGroup( "TF:S2,10/Invis,10/1" );>;
    FloatAttribute(g_SmoothStep, g_SmoothStep);

    float g_flBlurAmount< Default( 10.0f ); Range( 0.0f, 32.0f ); UiGroup( "TF:S2,10/Invis,10/1" ); >;

    //Get frame buffer texture, may just break in future ¯\_(ツ)_/¯
    BoolAttribute( bWantsFBCopyTexture, true );
    CreateTexture2D( g_tFrameBufferCopyTexture ) < Attribute( "FrameBufferCopyTexture" ); SrgbRead( true ); Filter( MIN_MAG_MIP_LINEAR ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    
    float4 g_vViewport < Source( Viewport ); >;

    //Blur taken from glass shader
    float3 GaussianBlur(float3 color, float2 uv, float2 size)
    {
        float Pi = 6.28318530718; // Pi*2
        float Directions = 16.0; // BLUR DIRECTIONS (Default 16.0 - More is better but slower)
        float Quality = 4.0; // BLUR QUALITY (Default 4.0 - More is better but slower)
        float taps = 1;

        // Blur calculations
        [unroll]
        for( float d=0.0; d<Pi; d+=Pi/Directions)
        {
            [unroll]
            for(float j=1.0/Quality; j<=1.0; j+=1.0/Quality)
            {
                taps += 1;
                color += Tex2D( g_tFrameBufferCopyTexture, uv + float2( cos(d), sin(d) ) * size * j ).rgb;    
            }
        }
        
        // Output to screen
        color /= taps;

        return color;
    }

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        Material material = GatherMaterial(i);

        float3 vPositionWs = i.vPositionWithOffsetWs.xyz;

        ShadingModelValveStandard shading;
        float4 o = FinalizePixelMaterial( i, material, shading );

        float2 vPositionUv = ScreenspaceCorrectionMultiview( CalculateViewportUv( i.vPositionSs ) );
        float3 vFbColor = Tex2D( g_tFrameBufferCopyTexture, vPositionUv ).rgb;
        float3 blur = GaussianBlur( vFbColor.rgb, vPositionUv.xy, g_flBlurAmount / g_vViewport.zw );
        blur *= g_flBlurTint;

        float normalDot = dot(normalize(i.vNormalWs), -normalize(g_vCameraDirWs));

        //Point where blur fade ends and blur to fully invisible begins.
        float g_flFadePoint = 0.5f;

        float fadeMul = 1.0f / g_flFadePoint;

        //Make blur val be from 0-1
        float blurBlend = (g_InvisBlend - g_flFadePoint) * fadeMul;
        float steppedBlurLerp = saturate(smoothstep(blurBlend - min(g_SmoothStep, min(blurBlend, 1.0f - blurBlend)), blurBlend, normalDot));

        //Show either the normal to blur colour, or the blur to fully invis colour.
        o.rgb = g_InvisBlend >= g_flFadePoint ? lerp(o.rgb, blur, steppedBlurLerp) : lerp(vFbColor, blur, g_InvisBlend * fadeMul);

        return o;
    }
}