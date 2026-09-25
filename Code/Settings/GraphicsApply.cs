namespace LoopedLoaded;

public static class GraphicsApply
{
	sealed class SunSnap
	{
		public DirectionalLight Light;
		public bool Shadows;
		public int Cascades;
		public bool Contact;
	}

	sealed class LocalSnap
	{
		public Light Light;
		public bool Shadows;
		public Light.FogInfluence Fog;
	}

	sealed class BloomSnap
	{
		public Bloom Bloom;
		public bool Enabled;
		public float Strength;
	}

	sealed class ToneSnap
	{
		public Tonemapping Tone;
		public bool AutoExposure;
	}

	sealed class LookSnap
	{
		public QuakeArenaLook Look;
		public float Intensity;
		public float Chromatic;
		public float Sharpen;
	}

	static readonly List<SunSnap> suns = new();
	static readonly List<LocalSnap> locals = new();
	static readonly List<BloomSnap> blooms = new();
	static readonly List<ToneSnap> tones = new();
	static readonly List<LookSnap> looks = new();
	static readonly Dictionary<PointLight, float> shotRadius = new();
	static bool captured;

	public static PointLight AddShotLight( GameObject host, Color color, float radius )
	{
		if ( !GraphicsProfile.ShotLights || !host.IsValid() || radius <= 1f )
			return null;

		if ( shotRadius.Count > 48 )
			DropDeadShots();

		var glow = host.AddComponent<PointLight>();
		glow.LightColor = color;
		glow.Shadows = false;
		glow.FogMode = Light.FogInfluence.Disabled;
		glow.Radius = radius * GraphicsProfile.ShotLightScale;
		shotRadius[glow] = radius;
		return glow;
	}

	public static void Push( Scene scene )
	{
		if ( !scene.IsValid() )
			return;

		if ( !captured )
			Capture( scene );

		Apply();

		foreach ( var board in scene.GetAllComponents<CityBoard>() )
		{
			if ( board.IsValid() )
				board.ApplyOrganMotion();
		}
	}

	static void Capture( Scene scene )
	{
		captured = true;

		foreach ( var light in scene.GetAllComponents<DirectionalLight>() )
		{
			if ( !light.IsValid() )
				continue;

			suns.Add( new SunSnap
			{
				Light = light,
				Shadows = light.Shadows,
				Cascades = light.ShadowCascadeCount,
				Contact = light.ContactShadows
			} );
		}

		foreach ( var light in scene.GetAllComponents<SpotLight>() )
			RememberLocal( light );

		foreach ( var light in scene.GetAllComponents<PointLight>() )
			RememberLocal( light );

		foreach ( var bloom in scene.GetAllComponents<Bloom>() )
		{
			if ( !bloom.IsValid() )
				continue;

			blooms.Add( new BloomSnap
			{
				Bloom = bloom,
				Enabled = bloom.Enabled,
				Strength = bloom.Strength
			} );
		}

		foreach ( var tone in scene.GetAllComponents<Tonemapping>() )
		{
			if ( !tone.IsValid() )
				continue;

			tones.Add( new ToneSnap
			{
				Tone = tone,
				AutoExposure = tone.AutoExposureEnabled
			} );
		}

		foreach ( var look in scene.GetAllComponents<QuakeArenaLook>() )
		{
			if ( !look.IsValid() )
				continue;

			looks.Add( new LookSnap
			{
				Look = look,
				Intensity = look.Intensity,
				Chromatic = look.Chromatic,
				Sharpen = look.Sharpen
			} );
		}
	}

	static void RememberLocal( Light light )
	{
		if ( !light.IsValid() )
			return;

		locals.Add( new LocalSnap
		{
			Light = light,
			Shadows = light.Shadows,
			Fog = light.FogMode
		} );
	}

	static void Apply()
	{
		foreach ( var sun in suns )
		{
			if ( !sun.Light.IsValid() )
				continue;

			sun.Light.Shadows = GraphicsProfile.SunShadows && sun.Shadows;
			sun.Light.ContactShadows = GraphicsProfile.ContactShadows && sun.Contact;
			sun.Light.ShadowCascadeCount = GraphicsProfile.ShadowCascades > 0
				? GraphicsProfile.ShadowCascades
				: sun.Cascades;
		}

		foreach ( var local in locals )
		{
			if ( !local.Light.IsValid() )
				continue;

			local.Light.Shadows = GraphicsProfile.LocalShadows && local.Shadows;
			local.Light.FogMode = GraphicsProfile.LocalFog ? local.Fog : Light.FogInfluence.Disabled;
		}

		foreach ( var bloom in blooms )
		{
			if ( !bloom.Bloom.IsValid() )
				continue;

			bloom.Bloom.Enabled = GraphicsProfile.BloomEnabled && bloom.Enabled;
			bloom.Bloom.Strength = bloom.Strength * GraphicsProfile.BloomScale;
		}

		foreach ( var tone in tones )
		{
			if ( !tone.Tone.IsValid() )
				continue;

			tone.Tone.AutoExposureEnabled = GraphicsProfile.AutoExposure && tone.AutoExposure;
		}

		foreach ( var look in looks )
		{
			if ( !look.Look.IsValid() )
				continue;

			look.Look.Intensity = GraphicsProfile.PostLook ? look.Intensity : 0f;
			look.Look.Chromatic = look.Chromatic * GraphicsProfile.ChromaticScale;
			look.Look.Sharpen = look.Sharpen * GraphicsProfile.SharpenScale;
		}

		SyncShots();
	}

	static void SyncShots()
	{
		foreach ( var pair in shotRadius )
		{
			if ( !pair.Key.IsValid() )
				continue;

			pair.Key.Enabled = GraphicsProfile.ShotLights;
			pair.Key.Shadows = false;
			pair.Key.FogMode = Light.FogInfluence.Disabled;
			pair.Key.Radius = pair.Value * GraphicsProfile.ShotLightScale;
		}

		DropDeadShots();
	}

	static void DropDeadShots()
	{
		List<PointLight> dead = null;
		foreach ( var pair in shotRadius )
		{
			if ( pair.Key.IsValid() )
				continue;

			dead ??= new();
			dead.Add( pair.Key );
		}

		if ( dead is null )
			return;

		foreach ( var light in dead )
			shotRadius.Remove( light );
	}
}
