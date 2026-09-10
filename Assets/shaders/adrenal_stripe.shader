HEADER
{
	Description = "Adrenal yellow scan stripe";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
}

COMMON
{
	#include "common/shared.hlsl"
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

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/pixel.hlsl"

	CreateInputTexture2D( TextureColor, Srgb, 8, "", "", "Color,10/10", Default3( 1.0, 1.0, 1.0 ) );
	Texture2D g_tColor < Channel( RGBA, Box( TextureColor ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
	SamplerState TextureFilter < Filter( Anisotropic ); AddressU( WRAP ); AddressV( WRAP ); >;

	float g_flStripeSpeed < Attribute( "StripeSpeed" ); Default( 0.55 ); >;
	float g_flStripeWidth < Attribute( "StripeWidth" ); Default( 0.045 ); >;
	float g_flStripeStrength < Attribute( "StripeStrength" ); Default( 1.0 ); >;
	float g_flStripePhase < Attribute( "StripePhase" ); Default( 0.0 ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 albedo = g_tColor.Sample( TextureFilter, i.vTextureCoords.xy ).rgb;
		float axis = i.vTextureCoords.y;
		float scan = frac( g_flTime * g_flStripeSpeed + g_flStripePhase );
		float delta = abs( frac( axis - scan + 0.5 ) - 0.5 );
		float band = exp( -( delta / max( g_flStripeWidth, 0.001 ) ) * ( delta / max( g_flStripeWidth, 0.001 ) ) );
		float trail = exp( -( delta / max( g_flStripeWidth * 2.4, 0.001 ) ) * ( delta / max( g_flStripeWidth * 2.4, 0.001 ) ) ) * 0.35;
		float stripe = ( band + trail ) * g_flStripeStrength;
		float3 yellow = float3( 1.0, 0.82, 0.12 );

		Material m = Material::Init( i );
		m.Albedo = albedo + yellow * stripe * 0.85;
		m.Emission = yellow * stripe * 1.6;
		m.Metalness = 0.0;
		m.Roughness = 0.5;
		m.Normal = i.vNormalWs;
		m.AmbientOcclusion = 1.0;
		m.Opacity = 1.0;
		return ShadingModelStandard::Shade( m );
	}
}
