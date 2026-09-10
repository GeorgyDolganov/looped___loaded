HEADER
{
	Description = "Lung breathing";
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

RenderState( CullMode, NONE );

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

	float g_flBreathPhase < Attribute( "BreathPhase" ); Default( 0.0 ); >;
	float g_flBreathRate < Attribute( "BreathRate" ); Default( 0.28 ); >;
	float g_flBreathStrength < Attribute( "BreathStrength" ); Default( 0.08 ); >;
	float g_flLungMirrorX < Attribute( "LungMirrorX" ); Default( 0.0 ); >;
	float g_flLungMirrorY < Attribute( "LungMirrorY" ); Default( 0.0 ); >;
	float g_flLungMirrorZ < Attribute( "LungMirrorZ" ); Default( 0.0 ); >;

	float Breath( float cycle )
	{
		float inhale = smoothstep( 0.0, 0.34, cycle );
		float exhale = 1.0 - smoothstep( 0.42, 0.96, cycle );
		return min( inhale, exhale );
	}

	PixelInput MainVs( VertexInput i )
	{
		if ( g_flLungMirrorX > 0.5 )
		{
			i.vPositionOs.x = -i.vPositionOs.x;
			i.vNormalOs.x = -i.vNormalOs.x;
		}
		if ( g_flLungMirrorY > 0.5 )
		{
			i.vPositionOs.y = -i.vPositionOs.y;
			i.vNormalOs.y = -i.vNormalOs.y;
		}
		if ( g_flLungMirrorZ > 0.5 )
		{
			i.vPositionOs.z = -i.vPositionOs.z;
			i.vNormalOs.z = -i.vNormalOs.z;
		}

		float3 pos = i.vPositionOs.xyz;
		float globalCycle = frac( g_flTime * g_flBreathRate + g_flBreathPhase );
		float sideDelay = 0.07 * saturate( pos.x * 0.75 + 0.5 );
		float localCycle = frac( g_flTime * g_flBreathRate + g_flBreathPhase - sideDelay );
		float globalAir = Breath( globalCycle );
		float localAir = Breath( localCycle );
		float outer = saturate( abs( pos.x ) * 1.15 );
		float air = globalAir * 0.72 + localAir * outer * 0.28;
		i.vPositionOs.xyz *= 1.0 + g_flBreathStrength * air;

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
		m.Roughness = 0.6;
		m.Normal = i.vNormalWs;
		m.AmbientOcclusion = 1.0;
		m.Opacity = 1.0;
		return ShadingModelStandard::Shade( m );
	}
}
