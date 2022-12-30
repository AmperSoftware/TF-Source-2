HEADER
{
	Description = "Überchage invulnerable shader effect for player models";
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
FEATURES
{
    #include "common/features.hlsl"

    Feature(F_ANIMATED_NORMAL, 0..1, "TF:S2");
}

//=========================================================================================================================
COMMON
{
	#include "common/shared.hlsl"

    #define VS_INPUT_HAS_TANGENT_BASIS 1
	#define PS_INPUT_HAS_TANGENT_BASIS 1
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
	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VertexInput i ) )
	{
		PixelInput o = ProcessVertex( i );

		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{
    #include "common/pixel.hlsl"
    #include "common.fxc"

    float3 g_HighlightColour < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    Float3Attribute(g_HighlightColour, g_HighlightColour);

    //Floor value for bias check, values under go to 0.
    float g_HighlightBlend < Default( 0.9 ); Range( 0, 1 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    FloatAttribute(g_HighlightBlend, g_HighlightBlend);

    float g_ReflectionStrength < Default( 1.0 ); Range( 0, 1 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    FloatAttribute(g_ReflectionStrength, g_ReflectionStrength);

    CreateInputTextureCube( reflectionCube , Srgb, 8, "", "_cube",  "TF:S2,10/Invun,10/1", Default3( 1, 1, 1) );
    CreateTextureCube( reflection ) < Channel( RGBA, None( reflectionCube ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); UiGroup( "TF2,10/Invun,10/1" );>;
    TextureAttribute(reflection, reflection);

	//x is value at facing angle, y is value 50% between facing/grazing angle, z is value grazing angle
	float3 g_FresnelRange < Default3( 0.05, 0.5, 1 );Range3( 0, 0, 0, 50.0, 50.0, 50.0 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    Float3Attribute(g_FresnelRange, g_FresnelRange);

	float3 g_IllumMinMaxExp < Default3( 0, 18, 13 );Range3( 0, 0, 0, 50.0, 50.0, 50.0 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    Float3Attribute(g_IllumMinMaxExp, g_IllumMinMaxExp);

	//Uber level, x >= 0.5 is on, x < 0.5 is flashing
    float g_UberLevel < Default( 1.0 ); Range( 0, 1.0 ); UiGroup( "TF:S2,10/Invun,10/1" );>;
    FloatAttribute(g_UberLevel, g_UberLevel);

    //Colour used for the Rimlight effect.
    float3 g_RimlightColor < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "TF:S2,,10/Rimlight,10/1" );>;
    Float3Attribute(g_RimlightColor, g_RimlightColor);

    //Direction to bias against
    float3 g_RimlightDirectionBias < UiType( VectorText ); Default3( 0, 0, 1.0); UiGroup( "TF:S2,10/Rimlight,10/1" );>;
    Float3Attribute(g_RimlightDirectionBias, g_RimlightDirectionBias);

    //Floor value for bias check, values under go to 0.
    float g_RimlightDirectionBiasFloor < Default( 0.25 ); Range( 0, 1 ); UiGroup( "TF:S2,10/Rimlight,10/1" );>;
    FloatAttribute(g_RimlightDirectionBiasFloor, g_RimlightDirectionBiasFloor);

	float g_RimlightExponent < Default( 2 ); Range( 0, 10 ); UiGroup( "TF:S2,10/Rimlight,10/1" );>;
    FloatAttribute(g_RimlightExponent, g_RimlightExponent);

    StaticCombo( S_ANIMATED_NORMAL, F_ANIMATED_NORMAL, Sys( PC ) );

    #if S_ANIMATED_NORMAL

    CreateInputTexture2D( AnimatedNormal,           Linear, 8, "NormalizeNormals", "_normal", "TF:S2,10/Animated Normal,10/1", Default3( 0.5, 0.5, 1.0 ) );
    CreateTexture2DWithoutSampler( g_tAnimatedNormal ) < Channel( RGBA, Box( AnimatedNormal ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

    float2 g_AnimatedGrid < Default2( 2.0, 2.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    Float2Attribute(g_AnimatedGrid, g_AnimatedGrid);

    float g_NumAnimationCells < Range( 0.0, 100.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    FloatAttribute(g_NumAnimationCells, g_NumAnimationCells);

    float g_AnimationTimePerFrame < Range( 0.0, 100.0 ); UiGroup( "TF:S2,10/Animated Normal,10/1" );>;
    FloatAttribute(g_AnimationTimePerFrame, g_AnimationTimePerFrame);

    #endif
	
	//Valve Fresnel Range Function
	float FresnelRange(float3 vNormal, float3 vEyeDir, float3 vRanges)
	{
		float f = 1 + dot(vNormal, vEyeDir);			// Traditional Fresnel
        // Blend between mid and high values or low and mid values
        return f > 0.5 ? lerp(vRanges.y, vRanges.z, (2*f)-1) : lerp(vRanges.x, vRanges.y, 2*f);
	}

	//Custom Exponent Function for illum, returns facing value
	float IllumFresnelExp(float3 vNormal, float3 vEyeDir, float exp)
	{
		float f = 1 + dot(vNormal, vEyeDir);			// Traditional Fresnel
		float f2 = f * f;
		// This is not the right math, but it works for our purposes
		return pow(1 - f2, exp * 2);
	}

	//Sine func that takes Valve-material-proxy inputs
	float sine(float min, float max, float offset, float period)
	{
		float sineOut = (((max-min)/2)*sin((g_flTime - offset )/(period/2)) + ((max-min)/2 + min));
		return sineOut;
	}

	float4 MainPs(PixelInput i) : SV_Target0
	{
		Material material = GatherMaterial(i);

		//Animate normal by changing uv sample start and scale down "size"
		#if S_ANIMATED_NORMAL

		float2 uv = i.vTextureCoords.xy;
		float2 scale = float2(1 / g_AnimatedGrid.x, 1 / g_AnimatedGrid.y);
		uv *= scale;
		int currentFrame = (g_flTime / g_AnimationTimePerFrame) % (g_NumAnimationCells - 1);
		int yRow = currentFrame / g_AnimatedGrid.y;
		float2 uvStart = float2((currentFrame % g_AnimatedGrid.x) / g_AnimatedGrid.x, yRow / g_AnimatedGrid.y);

        //Sample normal and blend via "whiteout" blend method
        float3 animatedNormalSample = Tex2DS(g_tAnimatedNormal, TextureFiltering, clamp(uv + uvStart, float2(0.001, 0.001), float2(0.999, 0.999))).rgb * 2 - 1;
        float3 normalAdj = material.Normal * 2.0f - 1.0f;
        float3 r = normalize(float3(normalAdj.xy + animatedNormalSample.xy, normalAdj.z * animatedNormalSample.z));
		material.Normal = r * 0.5f + 0.5f;

        #endif

        //Get normal including the normal map
		float3 reflectionNormal = Vec3TsToWsNormalized(material.Normal * 2.0f - 1.0f, i.vNormalWs.xyz, i.vTangentUWs, i.vTangentVWs);
		
		//Get fresnel value using normal vector
		float3 dirToCamera = normalize(i.vPositionWithOffsetWs);
		float fresnel = FresnelRange(reflectionNormal, dirToCamera, g_FresnelRange);

		//Reflect the camera to pixel vector and sample from the cubemap.
		float3 cubeSamplePos = reflect(i.vPositionWithOffsetWs, reflectionNormal);
		float3 reflectionColor = TexCube(reflection, cubeSamplePos).rgb * g_ReflectionStrength;

        //Front highlight, needs to be replaced with light-based highlight
        float fresnelHighlight = smoothstep(g_HighlightBlend, 1, 1 - fresnel);

		//Uber flashing static values
		float uber_highlightMax = 1;
		float uber_offset = 0;
		float uber_period = 0.3;

		//Uber flashing max/min values
        bool uberLow = g_UberLevel < 0.5;
		float uber_highlightMin = uberLow ? 0 : 1;
		float uber_illumFresnelMax = uberLow ? clamp( g_IllumMinMaxExp.y - 49, 0, g_IllumMinMaxExp.y) : g_IllumMinMaxExp.y;
		float uber_illumFresnelExponent = uberLow ? 1 : g_IllumMinMaxExp.z;

		float uber_highlightSine = sine( uber_highlightMin, uber_highlightMax, uber_offset, uber_period );
		float uber_illumFresnelMaxSine = sine( uber_illumFresnelMax, g_IllumMinMaxExp.y, uber_offset, uber_period );
		float uber_illumFresnelExponentSine = sine( uber_illumFresnelExponent, g_IllumMinMaxExp.z, uber_offset, uber_period );

		float fresnelIllum = IllumFresnelExp(reflectionNormal, dirToCamera, uber_illumFresnelExponentSine);

		material.Albedo += lerp(float3(0,0,0), g_HighlightColour, fresnelHighlight);
		material.Emission = material.Albedo * clamp( (fresnelIllum * uber_illumFresnelMaxSine), g_IllumMinMaxExp.x, g_IllumMinMaxExp.y);

		//Rimlight

		//Get a direction mutitplier using a dot agaisnt the biast, and step using the bias floor.
		float rimlightDirectionMultiplier = smoothstep((g_RimlightDirectionBiasFloor * 0.5), g_RimlightDirectionBiasFloor, dot(reflectionNormal, normalize(g_RimlightDirectionBias)));
		
		//Get fresnel color result
		float3 rimlight = g_RimlightColor * pow(saturate(fresnel), g_RimlightExponent * 2 );

		//Multiply the rimlight colour by the direction bias result, reduces the fresnel effect for directions that are further from the bias.
		rimlight *= rimlightDirectionMultiplier;

		//Finalise output, add reflection to lit output via "lighten" mode, add rimlight
		float4 o = FinalizePixelMaterial( i, material );
		o.rgb = float3(max(reflectionColor.x, o.x), max(reflectionColor.y, o.y), max(reflectionColor.z, o.z));
		o.rgb += rimlight.rgb;

        return o;
	}
}