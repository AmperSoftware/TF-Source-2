HEADER
{
	Description = "Post processing screen overlay used for the Sniper Rifle scope";
}

FEATURES
{
}

MODES
{
    VrForward();
    Default();
}

//=========================================================================================================================
COMMON
{
	#include "postprocess/shared.hlsl"
}

//=========================================================================================================================

struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    float2 vTexCoord : TEXCOORD0;

	// VS only
	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs		: SV_Position;
	#endif

	// PS only
	#if ( ( PROGRAM == VFX_PROGRAM_PS ) )
		float4 vPositionSs		: SV_ScreenPosition;
	#endif
};

VS
{
    PixelInput MainVs( VertexInput i )
    {
        PixelInput o;
        o.vPositionPs = float4(i.vPositionOs.xyz, 1.0f);
        o.vTexCoord = i.vTexCoord;
        return o;
    }
}

PS
{
    #include "postprocess/common.hlsl"

    CreateInputTexture2D( scopewarpTexture , Linear, 8, "NormalizeNormals", "",  ",10/10", Default3( 1, 1, 1) );
    CreateTexture2D( scopewarp ) < Channel( RGB , Box( scopewarpTexture ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    TextureAttribute(scopewarp, scopewarp);

    CreateInputTexture2D( scopeoverlayTexture , Linear, 8, "", "",  ",10/10", Default3( 1, 1, 1) );
    CreateTexture2D( scopeoverlay ) < Channel( RGB , None( scopeoverlayTexture ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
    TextureAttribute(scopeoverlay, scopeoverlay);

    float g_warpScale< Default( 0.15f ); Range(0.0f, 2.0f); UiGroup( "" ); >;

    RenderState( DepthWriteEnable, false );
    RenderState( DepthEnable, false );

    CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" );  	SrgbRead( true ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( MIRROR ); AddressV( MIRROR ); >;
    CreateTexture2D( g_tDepthBuffer ) < Attribute( "DepthBuffer" ); 	SrgbRead( false ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    DynamicCombo( D_ENABLED, 0..1, Sys( PC ) );

    struct PixelOutput
    {
        float4 vColor : SV_Target0;
    };

    //get distance between two float2s
    float2 GetDistance(float2 a, float2 b)
    {
        return abs(a - b);
    }

    //get magnitude of float2
    float GetMagnitude(float2 a)
    {
        return sqrt(a.x * a.x + a.y * a.y);
    }

    //scale uv from center
    float2 ScaleUV(float2 uv, float2 center, float2 scale)
    {
        float2 scaled = uv - center;
        scaled *= scale;
        scaled += center;
        return scaled;
    }

    //Inverse lerp
    float InverseLerp(float a, float b, float x)
    {
        return (x - a) / (b - a);
    }

    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 o;
       
        float2 uv = i.vTexCoord.xy - g_vViewportOffset.xy / g_vRenderTargetSize;
        #if D_ENABLED
        float2 frameDimensions;
        g_tColorBuffer.GetDimensions(frameDimensions.x, frameDimensions.y);
        float2 scopeDimensions = float2(1024, 1024);

        //Find largest and smallest dimensions.
        bool landscape = frameDimensions.x > frameDimensions.y;
        float maxdim = landscape ? frameDimensions.x : frameDimensions.y;
        float mindim = landscape ? frameDimensions.y : frameDimensions.x;

        float2 scopeWarpUvScale = float2(frameDimensions.x / mindim, frameDimensions.y / mindim);
        //Make a new scaled uv to keep the applied overlay and normal as a centered square.
        float2 scaledScopeUV = clamp(ScaleUV(uv.xy, float2(0.5, 0.5), scopeWarpUvScale), float2(0.001, 0.001), float2(0.999, 0.999));
        //Correct the normal to be from -1 to 1
        float2 warpSample = Tex2D(scopewarp, scaledScopeUV).rg * 2.0f - 1.0f;

        //Move the UV based on the normal sample
        o = Tex2D(g_tColorBuffer, uv.xy + (warpSample * g_warpScale));
        //Tint output using overlay texture.
        o.rgb *= Tex2D(scopeoverlay, scaledScopeUV).rgb;
        #else
        o = Tex2D(g_tColorBuffer, uv.xy);
        #endif
        return o;
    }
}