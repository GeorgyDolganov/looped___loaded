HEADER
{
	Description = "Quake III arena look";
}

MODES
{
	Default();
	Forward();
}

COMMON
{
	#include "postprocess/shared.hlsl"
}

struct VertexInput
{
	float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
	float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
	float2 vTexCoord : TEXCOORD0;

	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
	#endif

	#if ( PROGRAM == VFX_PROGRAM_PS )
		float4 vPositionSs : SV_Position;
	#endif
};

VS
{
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o;
		o.vPositionPs = float4( i.vPositionOs.xy, 0.0f, 1.0f );
		o.vTexCoord = i.vTexCoord;
		return o;
	}
}

PS
{
	#include "postprocess/common.hlsl"
	#include "postprocess/functions.hlsl"

	Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead( true ); >;

	float g_flIntensity < Attribute( "intensity" ); Default( 1.0f ); >;
	float g_flContrast < Attribute( "contrast" ); Default( 1.2f ); >;
	float g_flSaturation < Attribute( "saturate" ); Default( 1.32f ); >;
	float g_flOverbright < Attribute( "overbright" ); Default( 0.34f ); >;
	float g_flSplit < Attribute( "split" ); Default( 0.26f ); >;
	float3 g_vShadowTint < Attribute( "shadowTint" ); Default3( 0.38f, 0.72f, 1.0f ); >;
	float3 g_vHighlightTint < Attribute( "highlightTint" ); Default3( 1.0f, 0.56f, 0.2f ); >;
	float g_flDither < Attribute( "dither" ); Default( 0.04f ); >;
	float g_flQuantize < Attribute( "quantize" ); Default( 48.0f ); >;
	float g_flScanlines < Attribute( "scanlines" ); Default( 0.08f ); >;
	float g_flChromatic < Attribute( "chromatic" ); Default( 0.62f ); >;
	float g_flVignette < Attribute( "vignette" ); Default( 0.4f ); >;
	float g_flSharpen < Attribute( "sharpen" ); Default( 0.24f ); >;
	float g_flBarrel < Attribute( "barrel" ); Default( 0.03f ); >;
	float g_flHurt < Attribute( "hurt" ); Default( 0.0f ); >;

	static const float Bayer4[16] =
	{
		0.0f, 8.0f, 2.0f, 10.0f,
		12.0f, 4.0f, 14.0f, 6.0f,
		3.0f, 11.0f, 1.0f, 9.0f,
		15.0f, 7.0f, 13.0f, 5.0f
	};

	float2 DistortUv( float2 uv )
	{
		float2 centered = uv - 0.5f;
		float r2 = dot( centered, centered );
		return 0.5f + centered * ( 1.0f + g_flBarrel * r2 );
	}

	float4 Fetch( float2 uv )
	{
		return g_tColorBuffer.SampleLevel( g_sBilinearMirror, uv, 0 );
	}

	float4 FetchSplit( float2 uv )
	{
		float2 offset = ( uv - 0.5f ) * g_flChromatic * 0.0085f;
		float r = Fetch( uv - offset ).r;
		float4 mid = Fetch( uv );
		float b = Fetch( uv + offset ).b;
		return float4( r, mid.g, b, mid.a );
	}

	float3 Sharpen( float2 uv, float3 color )
	{
		float2 texel = rcp( g_vRenderTargetSize );
		float3 blur = (
			Fetch( uv + float2( 0.0f, -texel.y ) ).rgb +
			Fetch( uv + float2( 0.0f, texel.y ) ).rgb +
			Fetch( uv + float2( texel.x, 0.0f ) ).rgb +
			Fetch( uv + float2( -texel.x, 0.0f ) ).rgb
		) * 0.25f;

		float edge = saturate( abs( GetLuminance( color ) - GetLuminance( blur ) ) * 4.0f );
		return color + ( color - blur ) * g_flSharpen * edge;
	}

	float3 Grade( float3 color )
	{
		float3 graded = ( color - 0.5f ) * g_flContrast + 0.52f;
		float3 s = saturate( graded );
		s = s * s * ( 3.0f - 2.0f * s );
		graded = lerp( saturate( graded ), s, 0.28f );

		float lum = GetLuminance( graded );
		graded = lerp( lum.xxx, graded, g_flSaturation );
		graded *= 1.0f + g_flOverbright * graded;

		float shadowMask = saturate( 1.0f - smoothstep( 0.08f, 0.42f, lum ) );
		float highlightMask = saturate( smoothstep( 0.38f, 0.88f, lum ) );
		graded = lerp( graded, graded * g_vShadowTint, shadowMask * g_flSplit );
		graded = lerp( graded, graded * g_vHighlightTint * 1.12f, highlightMask * g_flSplit );
		return graded;
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 uv = DistortUv( CalculateViewportUv( i.vPositionSs.xy ) );
		float4 source = FetchSplit( uv );
		float3 color = Sharpen( uv, source.rgb );
		float3 graded = Grade( color );

		float2 pixel = i.vPositionSs.xy;
		float scan = 0.5f + 0.5f * sin( pixel.y * 3.14159265f );
		graded *= 1.0f - g_flScanlines * ( 1.0f - scan ) * 0.85f;

		float2 vignetteOffset = ( uv - 0.5f ) * float2( g_vRenderTargetSize.x / g_vRenderTargetSize.y, 1.0f );
		float vignette = saturate( length( vignetteOffset ) * ( 0.72f + g_flVignette ) );
		vignette = pow( vignette, 1.65f );
		graded *= 1.0f - vignette * g_flVignette * 0.85f;

		if ( g_flQuantize > 1.5f )
		{
			uint2 cell = uint2( pixel ) & 3;
			float bayer = ( Bayer4[cell.y * 4 + cell.x] + 0.5f ) / 16.0f - 0.5f;
			graded = saturate( ( floor( graded * g_flQuantize + bayer * g_flDither * g_flQuantize ) + 0.5f ) / g_flQuantize );
		}

		graded = lerp( graded, graded * float3( 1.2f, 0.22f, 0.2f ), g_flHurt * 0.62f );
		graded *= 1.0f - g_flHurt * vignette * 0.35f;

		return float4( lerp( source.rgb, saturate( graded ), g_flIntensity ), source.a );
	}
}
