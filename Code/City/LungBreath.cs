namespace LoopedLoaded;

public sealed class LungBreath : Component
{
	[Property] public float RestScale { get; set; } = 1f;
	[Property] public Vector3 Mirror { get; set; }
	[Property] public float Strength { get; set; } = 0.08f;
	[Property] public float Rate { get; set; } = 0.28f;
	[Property] public int Seed { get; set; }

	float phase;
	float rateScale = 1f;
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

		renderer.Attributes.Set( "BreathPhase", phase );
		renderer.Attributes.Set( "BreathRate", Rate * rateScale );
		renderer.Attributes.Set( "BreathStrength", Strength );
		renderer.Attributes.Set( "LungMirrorX", Mirror.x );
		renderer.Attributes.Set( "LungMirrorY", Mirror.y );
		renderer.Attributes.Set( "LungMirrorZ", Mirror.z );
	}

	void Roll()
	{
		if ( appliedSeed == Seed )
			return;

		appliedSeed = Seed;
		phase = Frac( Seed * 0.211f + 0.33f );
		rateScale = 0.9f + Frac( Seed * 0.307f + 0.14f ) * 0.2f;
	}

	static float Frac( float value )
	{
		return value - MathF.Floor( value );
	}
}
