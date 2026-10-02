namespace LoopedLoaded;

public sealed class RepulseWave : Component
{
	static readonly Color Edge = new Color( 0.58f, 0.22f, 1f );
	static readonly Color Trail = new Color( 0.95f, 0.38f, 0.78f );

	[Property] public float Lifetime { get; set; } = 0.42f;
	[Property] public float Radius { get; set; } = 48f;

	const int RingSegs = 28;

	PolyLine front;
	PolyLine wake;
	PointLight glow;
	float born;
	readonly List<Vector3> ring = new( RingSegs + 1 );
	Vector3 origin;

	public static void Spawn( Scene scene, Vector3 position, float radius )
	{
		var go = scene.CreateObject();
		go.Name = "Repulse";
		go.WorldPosition = position;

		var wave = go.AddComponent<RepulseWave>();
		wave.Radius = MathF.Max( 18f, radius );
	}

	protected override void OnStart()
	{
		born = Time.Now;
		origin = WorldPosition + Vector3.Up * 8f;
		front = Line( "Front" );
		wake = Line( "Wake" );
		glow = GraphicsApply.AddShotLight( GameObject, Edge * 10f, Radius * 1.6f );
	}

	protected override void OnUpdate()
	{
		var t = (Time.Now - born) / Lifetime;
		if ( t >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		var push = 1f - MathF.Pow( 1f - Math.Clamp( t / 0.55f, 0f, 1f ), 3f );
		var fade = t < 0.62f ? 1f : 1f - (t - 0.62f) / 0.38f;
		var reach = Radius * (0.06f + 0.94f * push );
		var behind = reach * 0.72f;

		Paint( front, reach, Edge.WithAlpha( fade ), MathX.Lerp( 14f, 3.5f, t ) );
		Paint( wake, behind, Trail.WithAlpha( fade * 0.7f ), MathX.Lerp( 8f, 2f, t ) );

		if ( glow.IsValid() )
		{
			glow.LightColor = Color.Lerp( Trail, Edge, push ) * (12f * fade * fade);
			glow.Radius = reach * 1.35f * GraphicsProfile.ShotLightScale;
		}
	}

	PolyLine Line( string name )
	{
		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = GameObject;

		var line = go.AddComponent<PolyLine>();
		line.HardCaps = true;
		line.Solid = true;
		line.Apply();
		return line;
	}

	void Paint( PolyLine line, float radius, Color color, float width )
	{
		if ( !line.IsValid() )
			return;

		if ( ring.Count != RingSegs + 1 )
		{
			ring.Clear();
			for ( var n = 0; n <= RingSegs; n++ )
				ring.Add( Vector3.Zero );
		}

		for ( var i = 0; i <= RingSegs; i++ )
		{
			var ang = MathF.Tau * i / RingSegs;
			ring[i] = origin + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, 0f );
		}

		line.HeadTint = color;
		line.TailTint = color;
		line.HeadWidth = width;
		line.TailWidth = width;
		line.SetPoints( ring );
		line.Apply();
	}
}
