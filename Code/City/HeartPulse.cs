namespace LoopedLoaded;

public sealed class HeartPulse : Component
{
	[Property] public float RestScale { get; set; } = 1f;
	[Property] public float Strength { get; set; } = 0.1f;
	[Property] public float Rate { get; set; } = 1.15f;
	[Property] public int Seed { get; set; }

	float phase;
	float rateScale = 1f;
	float gap = 0.18f;
	int appliedSeed = int.MinValue;

	protected override void OnStart() => Apply();

	protected override void OnUpdate() => Apply();

	public void Apply()
	{
		Roll();
		GameObject.WorldScale = RestScale;

		var renderer = GetComponent<ModelRenderer>();
		if ( !renderer.IsValid() )
			return;

		renderer.Attributes.Set( "HeartPhase", phase );
		renderer.Attributes.Set( "HeartRate", Rate * rateScale );
		renderer.Attributes.Set( "HeartStrength", Strength );
		renderer.Attributes.Set( "HeartGap", gap );
		renderer.Attributes.Set( "HeartNoise", 0.18f );
	}

	void Roll()
	{
		if ( appliedSeed == Seed )
			return;

		appliedSeed = Seed;
		phase = Frac( Seed * 0.173f + 0.41f );
		rateScale = 0.84f + Frac( Seed * 0.271f + 0.17f ) * 0.34f;
		gap = 0.155f + Frac( Seed * 0.419f + 0.08f ) * 0.07f;
	}

	static float Frac( float value )
	{
		return value - MathF.Floor( value );
	}
}
