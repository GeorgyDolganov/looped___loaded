HEADER
{
	Description = "Heart beat vertex pulse";
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

	float g_flHeartPhase < Attribute( "HeartPhase" ); Default( 0.0 ); >;
	float g_flHeartRate < Attribute( "HeartRate" ); Default( 1.15 ); >;
	float g_flHeartStrength < Attribute( "HeartStrength" ); Default( 0.1 ); >;
	float g_flHeartGap < Attribute( "HeartGap" ); Default( 0.18 ); >;
	float g_flHeartNoise < Attribute( "HeartNoise" ); Default( 0.18 ); >;

	float Hash12( float2 p )
	{
		float3 p3 = frac( float3( p.xyx ) * 0.1031 );
		p3 += dot( p3, p3.yzx + 33.33 );
		return frac( ( p3.x + p3.y ) * p3.z );
	}

	float Noise3( float3 p )
	{
		float3 c = floor( p );
		float3 f = frac( p );
		f = f * f * ( 3.0 - 2.0 * f );

		float n000 = Hash12( c.xy + c.z * 17.0 );
		float n100 = Hash12( c.xy + float2( 1.0, 0.0 ) + c.z * 17.0 );
		float n010 = Hash12( c.xy + float2( 0.0, 1.0 ) + c.z * 17.0 );
		float n110 = Hash12( c.xy + float2( 1.0, 1.0 ) + c.z * 17.0 );
		float n001 = Hash12( c.xy + ( c.z + 1.0 ) * 17.0 );
		float n101 = Hash12( c.xy + float2( 1.0, 0.0 ) + ( c.z + 1.0 ) * 17.0 );
		float n011 = Hash12( c.xy + float2( 0.0, 1.0 ) + ( c.z + 1.0 ) * 17.0 );
		float n111 = Hash12( c.xy + float2( 1.0, 1.0 ) + ( c.z + 1.0 ) * 17.0 );

		float n00 = lerp( n000, n100, f.x );
		float n10 = lerp( n010, n110, f.x );
		float n01 = lerp( n001, n101, f.x );
		float n11 = lerp( n011, n111, f.x );
		return lerp( lerp( n00, n10, f.y ), lerp( n01, n11, f.y ), f.z );
	}

	float Beat( float cycle, float gap )
	{
		float d0 = cycle;
		d0 -= floor( d0 + 0.5 );
		float t0 = exp( -( d0 / 0.065 ) * ( d0 / 0.065 ) );

		float d1 = cycle - gap;
		d1 -= floor( d1 + 0.5 );
		float t1 = exp( -( d1 / 0.055 ) * ( d1 / 0.055 ) );

		return 0.7 * t0 + t1;
	}

	PixelInput MainVs( VertexInput i )
	{
		float3 pos = i.vPositionOs.xyz;
		float along = saturate( pos.z * 0.7 + 0.5 );

		float globalCycle = frac( g_flTime * g_flHeartRate + g_flHeartPhase );
		float globalBeat = Beat( globalCycle, g_flHeartGap );

		float localCycle = frac( g_flTime * g_flHeartRate + g_flHeartPhase - along * 0.34 );
		float localBeat = Beat( localCycle, g_flHeartGap );

		float n2 = Noise3( pos * 9.0 - float3( g_flTime * 1.8, 0.0, g_flTime * 1.1 ) );
		float quiver = ( n2 * 2.0 - 1.0 ) * localBeat;
		float pulse = g_flHeartStrength * (
			globalBeat * 0.7
			+ localBeat * 0.55
			+ 0.08 * quiver * g_flHeartNoise );

		i.vPositionOs.xyz *= 1.0 + pulse;
		float3 dir = pos / max( length( pos ), 0.001 );
		i.vPositionOs.xyz += dir * g_flHeartStrength * g_flHeartNoise * 0.16 * quiver;

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

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init( i );
		m.Albedo = g_tColor.Sample( TextureFilter, i.vTextureCoords.xy ).rgb;
		m.Metalness = 0.0;
		m.Roughness = 0.55;
		m.Normal = i.vNormalWs;
		m.AmbientOcclusion = 1.0;
		m.Opacity = 1.0;
		return ShadingModelStandard::Shade( m );
	}
}
