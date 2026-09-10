namespace LoopedLoaded;

public sealed class QuakeArenaLook : BasePostProcess<QuakeArenaLook>
{
	[Property] public GameLoop Loop { get; set; }
	[Property, Range( 0f, 1f )] public float Intensity { get; set; } = 1f;
	[Property, Range( 0.6f, 2f )] public float Contrast { get; set; } = 1.06f;
	[Property, Range( 0.4f, 2.2f )] public float Saturation { get; set; } = 1.12f;
	[Property, Range( 0f, 1.2f )] public float Overbright { get; set; } = 0.12f;
	[Property, Range( 0f, 1f )] public float Split { get; set; } = 0.1f;
	[Property] public Color ShadowTint { get; set; } = new Color( 0.78f, 0.9f, 1f );
	[Property] public Color HighlightTint { get; set; } = new Color( 1f, 0.9f, 0.78f );
	[Property, Range( 0f, 0.2f )] public float Dither { get; set; } = 0f;
	[Property, Range( 0f, 96f )] public float Quantize { get; set; } = 0f;
	[Property, Range( 0f, 0.4f )] public float Scanlines { get; set; } = 0f;
	[Property, Range( 0f, 2f )] public float Chromatic { get; set; } = 0.18f;
	[Property, Range( 0f, 1f )] public float Vignette { get; set; } = 0.16f;
	[Property, Range( 0f, 1f )] public float Sharpen { get; set; } = 0.12f;
	[Property, Range( 0f, 0.12f )] public float Barrel { get; set; } = 0f;

	static Material material;

	public override void Render()
	{
		if ( Intensity <= 0.001f )
			return;

		material ??= Material.FromShader( "shaders/postprocess/quake_arena.shader" );
		if ( !material.IsValid() )
			return;

		Attributes.Set( "intensity", Intensity );
		Attributes.Set( "contrast", Contrast );
		Attributes.Set( "saturate", Saturation );
		Attributes.Set( "overbright", Overbright );
		Attributes.Set( "split", Split );
		Attributes.Set( "shadowTint", new Vector3( ShadowTint.r, ShadowTint.g, ShadowTint.b ) );
		Attributes.Set( "highlightTint", new Vector3( HighlightTint.r, HighlightTint.g, HighlightTint.b ) );
		Attributes.Set( "dither", Dither );
		Attributes.Set( "quantize", Quantize );
		Attributes.Set( "scanlines", Scanlines );
		Attributes.Set( "chromatic", Chromatic );
		Attributes.Set( "vignette", Vignette );
		Attributes.Set( "sharpen", Sharpen );
		Attributes.Set( "barrel", Barrel );
		Attributes.Set( "hurt", Loop.IsValid() ? Loop.HurtAmount : 0f );

		Blit( BlitMode.WithBackbuffer( material, Sandbox.Rendering.Stage.AfterPostProcess, 200, false ), "QuakeArenaLook" );
	}
}
