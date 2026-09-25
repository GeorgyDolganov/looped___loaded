HEADER
{
	Description = "Unbuilt organ frame";
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

	float g_flOrganMotion < Attribute( "OrganMotion" ); Default( 1.0 ); >;
	float g_flLungMirrorX < Attribute( "LungMirrorX" ); Default( 0.0 ); >;
	float g_flLungMirrorY < Attribute( "LungMirrorY" ); Default( 0.0 ); >;
	float g_flLungMirrorZ < Attribute( "LungMirrorZ" ); Default( 0.0 ); >;

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
		float wobble = g_flOrganMotion > 0.001
			? sin( g_flTime * 2.4 + dot( pos, float3( 1.7, 2.3, 1.1 ) ) ) * 0.012 * g_flOrganMotion
			: 0.0;
		i.vPositionOs.xyz *= 1.0 + wobble;

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

	RenderState( CullMode, NONE );

	float g_flCraft < Attribute( "Craft" ); Default( 0.0 ); >;
	float g_flCraftFloor < Attribute( "CraftFloor" ); Default( 8.0 ); >;
	float g_flCraftSpan < Attribute( "CraftSpan" ); Default( 64.0 ); >;
	float g_flCraftR < Attribute( "CraftR" ); Default( 0.75 ); >;
	float g_flCraftG < Attribute( "CraftG" ); Default( 0.9 ); >;
	float g_flCraftB < Attribute( "CraftB" ); Default( 1.0 ); >;

	float Hash12( float2 p )
	{
		float3 p3 = frac( float3( p.xyx ) * 0.1031 );
		p3 += dot( p3, p3.yzx + 33.33 );
		return frac( ( p3.x + p3.y ) * p3.z );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 tex = g_tColor.Sample( TextureFilter, i.vTextureCoords.xy ).rgb;
		float luma = dot( tex, float3( 0.30, 0.59, 0.11 ) );
		float3 tint = float3( g_flCraftR, g_flCraftG, g_flCraftB );
		float3 world = i.vPositionWithOffsetWs.xyz;
		float3 viewDir = normalize( g_vCameraPositionWs.xyz - world );
		float facing = saturate( abs( dot( normalize( i.vNormalWs ), viewDir ) ) );
		float fres = pow( 1.0 - facing, 2.2 );

		float h = saturate( ( world.z - g_flCraftFloor ) / max( g_flCraftSpan, 1.0 ) );
		float fill = g_flCraft <= 0.001 ? 0.0 : 1.0 - smoothstep( g_flCraft - 0.02, g_flCraft + 0.04, h );
		float seam = g_flCraft <= 0.001 ? 0.0 : exp( -pow( ( h - g_flCraft ) / 0.018, 2.0 ) );

		float scan = frac( h * 2.4 - g_flTime * 0.28 );
		float band = exp( -pow( ( scan - 0.5 ) / 0.05, 2.0 ) );
		float ribs = smoothstep( 0.46, 0.5, frac( h * 16.0 ) ) * ( 1.0 - fill );

		float2 cell = floor( world.xy * 0.09 );
		float speckle = step( 0.84, Hash12( cell + floor( world.z * 0.04 ) ) ) * ( 1.0 - fill );
		float grid = max(
			smoothstep( 0.46, 0.5, frac( world.x * 0.07 ) ),
			smoothstep( 0.46, 0.5, frac( world.y * 0.07 ) ) );
		grid *= ( 1.0 - fill ) * 0.45;

		float3 ghost = lerp( luma.xxx * 0.18, tint, 0.62 );
		ghost += tint * ( fres * 1.25 + ribs * 0.55 + grid );
		ghost += float3( 0.9, 0.97, 1.0 ) * ( band * 0.75 + seam * 1.35 + speckle * 0.7 );

		float3 albedo = lerp( ghost, tex, fill );
		float emit = ( fres * 0.9 + band * 1.15 + seam * 1.6 + ribs * 0.3 + speckle * 0.8 + grid ) * ( 1.0 - fill * 0.85 );

		Material m = Material::Init( i );
		m.Albedo = albedo;
		m.Emission = lerp( tint, float3( 0.85, 0.95, 1.0 ), 0.4 ) * emit;
		m.Metalness = 0.04;
		m.Roughness = lerp( 0.32, 0.55, fill );
		m.Normal = i.vNormalWs;
		m.AmbientOcclusion = 1.0;
		m.Opacity = 1.0;
		return ShadingModelStandard::Shade( m );
	}
}
