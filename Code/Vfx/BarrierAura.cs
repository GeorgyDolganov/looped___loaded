namespace LoopedLoaded;

public sealed class BarrierAura : Component
{
	const int Segs = 28;
	static readonly Color Gold = new( 1f, 0.82f, 0.18f );

	GameLoop loop;
	PolyLine halo;
	PolyLine core;
	float born;
	readonly List<Vector3> points = new( Segs + 1 );

	public static BarrierAura Spawn( GameLoop loop )
	{
		if ( !loop.IsValid() || !loop.Scene.IsValid() )
			return null;

		var go = loop.Scene.CreateObject();
		go.Name = "Barrier";
		var aura = go.AddComponent<BarrierAura>();
		aura.loop = loop;
		return aura;
	}

	protected override void OnStart()
	{
		born = Time.Now;
		halo = Line( "Halo" );
		core = Line( "Core" );
	}

	protected override void OnUpdate()
	{
		if ( !loop.IsValid() || !loop.Runner.IsValid() || !loop.BarrierUp )
		{
			GameObject.Destroy();
			return;
		}

		var left = loop.BarrierLeft;
		var blink = left < 0.7f ? 0.55f + 0.45f * MathF.Sin( Time.Now * 28f ) : 1f;
		var pulse = 0.5f + 0.5f * MathF.Sin( (Time.Now - born) * 7f );
		var origin = loop.Geometry.ToPlayWorld( loop.Runner.Flat ) + Vector3.Up * 8f;
		var radius = loop.Runner.PlayerRadius * (1.45f + 0.05f * pulse);

		Paint( halo, origin, radius, Gold.WithAlpha( 0.22f * blink ), 30f + 8f * pulse );
		Paint( core, origin, radius, Color.Lerp( Gold, Color.White, 0.35f ).WithAlpha( 0.95f * blink ), 6f );
	}

	PolyLine Line( string name )
	{
		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = GameObject;

		var line = go.AddComponent<PolyLine>();
		line.HardCaps = true;
		line.Apply();
		return line;
	}

	void Paint( PolyLine line, Vector3 origin, float radius, Color color, float width )
	{
		if ( !line.IsValid() )
			return;

		points.Clear();
		for ( var i = 0; i <= Segs; i++ )
		{
			var ang = MathF.Tau * i / Segs;
			points.Add( origin + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, 0f ) );
		}

		line.HeadTint = color;
		line.TailTint = color;
		line.HeadWidth = width;
		line.TailWidth = width;
		line.SetPoints( points );
		line.Apply();
	}
}
