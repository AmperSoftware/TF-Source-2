HEADER
{
	CompileTargets = ( IS_SM_50 && ( PC || VULKAN ) );
	Description = "Post processing screen overlay used during an Übercharge";
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

//=========================================================================================================================

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

//=========================================================================================================================

PS
{
    #include "postprocess/common.hlsl"

    CreateInputTexture2D( invulnOverlayNormalTex , Linear, 8, "NormalizeNormals", "",  ",10/10", Default4( 1, 1, 1, 1) );
    CreateTexture2D( invulnOverlayNormal ) < Channel( RGBA , Box( invulnOverlayNormalTex ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    TextureAttribute(invulnOverlayNormal, invulnOverlayNormal);

    CreateInputTexture2D( vignetteTex , Linear, 8, "", "",  ",10/10", Default3( 1, 1, 1) );
    CreateTexture2D( vignette ) < Channel( RGB , Box( vignetteTex ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    TextureAttribute(vignette, vignette);

    float3 g_colorTint < Default3( 0.0f, 0.0f, 1.0f ); UiType( Color ); UiGroup( "Uber" ); >;

    float g_scaleSpeed< Default( 5.5f ); Range(0.0f, 100.0f); UiGroup( "Uber" ); >;

    float2 g_scaleRange< Default2( 0.0f, 0.1f ); Range2(-1.0f, -1.0f, 2.0f, 2.0f); UiGroup( "Uber" ); >;

    RenderState( DepthWriteEnable, false );
    RenderState( DepthEnable, false );

    struct PixelOutput
    {
        float4 vColor : SV_Target0;
    };

    CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" );  	SrgbRead( true ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( MIRROR ); AddressV( MIRROR ); >;
    CreateTexture2D( g_tDepthBuffer ) < Attribute( "DepthBuffer" ); 	SrgbRead( false ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    DynamicCombo( D_ENABLED, 0..1, Sys( PC ) );

    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 o;
        float2 uv = i.vTexCoord.xy - g_vViewportOffset.xy / g_vRenderTargetSize;

        #if D_ENABLED
        float scale = lerp(g_scaleRange.x, g_scaleRange.y, sin(g_flTime * g_scaleSpeed) * 0.5 + 0.5);
        o = Tex2D(g_tColorBuffer, uv) * Tex2D(vignette, uv);
        float4 overlay = Tex2D(invulnOverlayNormal, uv);
        overlay.rg = overlay.rg * 2 - 1;

        float3 convertedTint = pow(g_colorTint, 2.2); //To linear

        //Get refracted color, and tint it
        float3 refracted = Tex2D(g_tColorBuffer, uv + (overlay.rg * scale)) * convertedTint;

        //Overlay blend tinted refaction onto normal frame color.
        //nvm multiply
        float3 overlayTint = refracted;//(o.vColor.rgb > 0.5) * (1 - (1-2*(o.vColor.rgb-0.5)) * (1-g_colorTint)) + (o.vColor.rgb <= 0.5) * ((2*o.vColor.rgb) * refracted);

        //lerp between normal frame and modified frame based on alpha
        o.rgb = lerp(o.rgb, overlayTint, overlay.a);

        #else
        o = Tex2D(g_tColorBuffer, uv);
        #endif
        return o;
    }
}