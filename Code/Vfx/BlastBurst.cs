namespace LoopedLoaded;

public sealed class BlastBurst : Component
{
	static readonly Color Hot = new Color( 1f, 0.78f, 0.32f );
	static readonly Color Core = new Color( 1f, 0.96f, 0.86f );

	[Property] public float Lifetime { get; set; } = 0.46f;
	[Property] public float Radius { get; set; } = 48f;
	[Property] public Color Tint { get; set; } = Color.White;

	const int RingSegs = 32;
	const int Spokes = 12;

	PolyLine shock;
	PolyLine edge;
	PolyLine heart;
	readonly List<PolyLine> spokes = new();
	PointLight glow;
	float born;
	float shockWidth;
	float edgeWidth;
	float heartWidth;
	float spokeWidth;
	Vector3 origin;

	public static void Spawn( Scene scene, Vector3 position, float radius, Color tint )
	{
		var go = scene.CreateObject();
		go.Name = "Blast";
		go.WorldPosition = position;

		var burst = go.AddComponent<BlastBurst>();
		burst.Radius = MathF.Max( 18f, radius );
		burst.Tint = tint;
	}

	protected override void OnStart()
	{
		born = Time.Now;
		origin = WorldPosition + Vector3.Up * 6f;

		var bulk = Math.Clamp( Radius / 40f, 1.2f, 3.2f );
		shockWidth = 16f * bulk;
		edgeWidth = 7f * bulk;
		heartWidth = 20f * bulk;
		spokeWidth = 11f * bulk;
		shock = Ring( "Shock", shockWidth );
		edge = Ring( "Edge", edgeWidth );
		heart = Ring( "Heart", heartWidth );

		for ( var i = 0; i < Spokes; i++ )
			spokes.Add( Spoke( spokeWidth, 2f ) );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = Core * 18f;
		glow.Radius = Radius * 5f;
	}

	protected override void OnUpdate()
	{
		var t = (Time.Now - born) / Lifetime;
		if ( t >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		var grow = 1f - MathF.Pow( 1f - Math.Clamp( t / 0.4f, 0f, 1f ), 3f );
		var fade = t < 0.42f ? 1f : 1f - (t - 0.42f) / 0.58f;
		var shockRadius = MathF.Max( 10f, Radius * (0.22f + 1.35f * grow) );
		var heartRadius = MathF.Max( 8f, Radius * (0.34f * (1f - t) + 0.06f) );

		var shockTint = Color.Lerp( Hot, Tint, Math.Clamp( t * 1.4f, 0f, 1f ) );
		Paint( shock, shockRadius, Shown( shockTint, fade ), MathX.Lerp( shockWidth, shockWidth * 0.32f, t ) );
		Paint( edge, Radius, Shown( Tint, 0.85f * fade ), edgeWidth );
		Paint( heart, heartRadius, Shown( Core, fade ), MathX.Lerp( heartWidth, heartWidth * 0.22f, t ) );

		for ( var i = 0; i < spokes.Count; i++ )
		{
			var ang = MathF.Tau * i / spokes.Count + t * 0.35f;
			var dir = new Vector3( MathF.Cos( ang ), MathF.Sin( ang ), 0f );
			var len = shockRadius * (i % 2 == 0 ? 0.92f : 0.62f );
			var tint = i % 2 == 0 ? Shown( Core, fade ) : Shown( shockTint, fade );
			PaintSegment( spokes[i], origin + dir * (heartRadius * 0.35f), origin + dir * len, tint, MathX.Lerp( spokeWidth, 2f, t ) );
		}

		if ( glow.IsValid() )
		{
			glow.LightColor = Color.Lerp( Core, Tint, t ) * (20f * fade * fade);
			glow.Radius = Radius * (3.2f + 3.4f * grow);
		}
	}

	PolyLine Ring( string name, float width )
	{
		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = GameObject;

		var line = go.AddComponent<PolyLine>();
		line.HeadWidth = width;
		line.TailWidth = width;
		line.HardCaps = true;
		line.Solid = true;
		line.Apply();
		return line;
	}

	PolyLine Spoke( float head, float tail )
	{
		var go = Scene.CreateObject();
		go.Name = "Spoke";
		go.Parent = GameObject;

		var line = go.AddComponent<PolyLine>();
		line.HeadWidth = head;
		line.TailWidth = tail;
		line.Solid = true;
		line.Apply();
		return line;
	}

	void Paint( PolyLine line, float radius, Color color, float width )
	{
		if ( !line.IsValid() )
			return;

		var points = new List<Vector3>( RingSegs + 1 );
		for ( var i = 0; i <= RingSegs; i++ )
		{
			var ang = MathF.Tau * i / RingSegs;
			points.Add( origin + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, 0f ) );
		}

		line.HeadTint = color;
		line.TailTint = color;
		line.HeadWidth = width;
		line.TailWidth = width;
		line.SetPoints( points );
		line.Apply();
	}

	static void PaintSegment( PolyLine line, Vector3 from, Vector3 to, Color color, float width )
	{
		if ( !line.IsValid() )
			return;

		line.HeadTint = color;
		line.TailTint = color.WithAlpha( color.a * 0.25f );
		line.HeadWidth = width;
		line.TailWidth = MathF.Max( 1.5f, width * 0.28f );
		line.SetPoints( new List<Vector3> { from, to } );
		line.Apply();
	}

	static Color Shown( Color color, float fade )
	{
		var amount = Math.Clamp( fade, 0f, 1f );
		return Color.Lerp( new Color( 0.02f, 0.025f, 0.03f ), color, amount ).WithAlpha( amount );
	}
}
