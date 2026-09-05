namespace LoopedLoaded;

public sealed class ImpactFlash : Component
{
	[Property] public float Lifetime { get; set; } = 0.22f;
	[Property] public float StartRadius { get; set; } = 26f;
	[Property] public float EndRadius { get; set; } = 90f;
	[Property] public Color Tint { get; set; } = Color.White;

	GameObject shell;
	PointLight glow;
	float born;

	public static void Spawn( Scene scene, Vector3 position, Color tint, float scale = 1f )
	{
		var go = scene.CreateObject();
		go.Name = "Impact";
		go.WorldPosition = position;

		var flash = go.AddComponent<ImpactFlash>();
		flash.Tint = tint;
		flash.StartRadius *= scale;
		flash.EndRadius *= scale;
	}

	protected override void OnStart()
	{
		born = Time.Now;

		shell = Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, StartRadius, Tint );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = Tint * 12f;
		glow.Radius = EndRadius * 4f;
	}

	protected override void OnUpdate()
	{
		var progress = (Time.Now - born) / Lifetime;

		if ( progress >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		var fade = 1f - progress;

		if ( shell.IsValid() )
		{
			var diameter = MathX.Lerp( StartRadius, EndRadius, progress );
			var bounds = Blocks.Sphere.Bounds.Size.x;
			shell.WorldScale = bounds > 0.001f ? diameter / bounds : 1f;

			var renderer = shell.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				renderer.Tint = Tint.WithAlpha( fade );
		}

		if ( glow.IsValid() )
			glow.LightColor = Tint * (12f * fade * fade);
	}
}
