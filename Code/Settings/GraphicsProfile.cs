namespace LoopedLoaded;

public enum GraphicsPreset
{
	Low = 0,
	Medium = 1,
	High = 2
}

public static class GraphicsProfile
{
	public static GraphicsPreset Preset { get; private set; } = GraphicsPreset.High;

	public static int GibAliveCap { get; private set; } = 56;
	public static int GibFrameCap { get; private set; } = 32;
	public static int FlashCap { get; private set; } = 3;
	public static int BoneAliveCap { get; private set; } = 20;
	public static int BoneFrameCap { get; private set; } = 8;
	public static int CircleSegments { get; private set; } = 20;
	public static int TrailPoints { get; private set; } = 48;
	public static bool SplashRings { get; private set; } = true;
	public static bool ShotLights { get; private set; } = true;
	public static float ShotLightScale { get; private set; } = 1f;
	public static int MaxParticles { get; private set; } = 64;
	public static int HudFaceStride { get; private set; } = 1;
	public static int AvoidStride { get; private set; } = 3;
	public static int MaxProjectileSteps { get; private set; } = 4;
	public static float OrganMotion { get; private set; } = 1f;
	public static int GlassBits { get; private set; } = 18;
	public static bool GlassRests { get; private set; } = true;
	public static bool FinishPulse { get; private set; } = true;
	public static bool SunShadows { get; private set; } = true;
	public static int ShadowCascades { get; private set; } = 0;
	public static bool ContactShadows { get; private set; } = true;
	public static bool LocalShadows { get; private set; } = true;
	public static bool LocalFog { get; private set; } = true;
	public static bool BloomEnabled { get; private set; } = true;
	public static float BloomScale { get; private set; } = 1f;
	public static bool AutoExposure { get; private set; } = true;
	public static bool PostLook { get; private set; } = true;
	public static float ChromaticScale { get; private set; } = 1f;
	public static float SharpenScale { get; private set; } = 1f;

	public static void Use( GraphicsPreset preset )
	{
		Preset = preset;
		switch ( preset )
		{
			case GraphicsPreset.Low:
				Fill( 8, 4, 1, 6, 2, 0, 8, false, false, 0f, 0, 0, 6, 2, 0f, 0, false, false,
					false, 1, false, false, false, false, 0f, false, false, 0f, 0f );
				break;
			case GraphicsPreset.Medium:
				Fill( 24, 12, 2, 10, 4, 12, 16, true, true, 0.5f, 16, 2, 4, 4, 1f, 8, false, true,
					true, 1, false, false, false, true, 0.5f, false, true, 0f, 0f );
				break;
			default:
				Fill( 56, 32, 3, 20, 8, 20, 48, true, true, 1f, 64, 1, 3, 4, 1f, 18, true, true,
					true, 0, true, true, true, true, 1f, true, true, 1f, 1f );
				break;
		}
	}

	static void Fill(
		int gibAlive, int gibFrame, int flash, int boneAlive, int boneFrame,
		int circle, int trail, bool splash, bool shotLights, float shotScale, int particles,
		int faceStride, int avoidStride, int projectileSteps, float organMotion,
		int glassBits, bool glassRests, bool finishPulse,
		bool sunShadows, int cascades, bool contact, bool localShadows, bool localFog,
		bool bloom, float bloomScale, bool autoExposure, bool postLook, float chromatic, float sharpen )
	{
		GibAliveCap = gibAlive;
		GibFrameCap = gibFrame;
		FlashCap = flash;
		BoneAliveCap = boneAlive;
		BoneFrameCap = boneFrame;
		CircleSegments = circle;
		TrailPoints = trail;
		SplashRings = splash;
		ShotLights = shotLights;
		ShotLightScale = shotScale;
		MaxParticles = particles;
		HudFaceStride = faceStride;
		AvoidStride = avoidStride;
		MaxProjectileSteps = projectileSteps;
		OrganMotion = organMotion;
		GlassBits = glassBits;
		GlassRests = glassRests;
		FinishPulse = finishPulse;
		SunShadows = sunShadows;
		ShadowCascades = cascades;
		ContactShadows = contact;
		LocalShadows = localShadows;
		LocalFog = localFog;
		BloomEnabled = bloom;
		BloomScale = bloomScale;
		AutoExposure = autoExposure;
		PostLook = postLook;
		ChromaticScale = chromatic;
		SharpenScale = sharpen;
	}
}
