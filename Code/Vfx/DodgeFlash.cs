namespace LoopedLoaded;

public sealed class DodgeFlash : Component
{
	public const float Duration = 0.6f;

	const int RingSegs = 20;
	const int ArcSegs = 8;
	const float ArcSpan = 1.9f;

	GameObject follow;
	Vector2 toward;
	float reach;
	float height;
	float born;
	PolyLine ring;
	PolyLine arc;
	PointLight glow;
	readonly List<Vector3> points = new( RingSegs + 1 );

	public static void Spawn( Scene scene, GameObject follow, Vector3 position, Vector2 toward, float reach )
	{
		if ( !scene.IsValid() )
			return;

		var go = scene.CreateObject();
		go.Name = "Dodge";
		go.WorldPosition = position;

		var flash = go.AddComponent<DodgeFlash>();
		flash.follow = follow;
		flash.toward = toward.Length > 0.01f ? toward.Normal : Vector2.Zero;
		flash.reach = MathF.Max( 16f, reach );
		flash.height = position.z;
	}

	protected override void OnStart()
	{
		born = Time.Now;
		ring = Line( "Ring" );
		arc = Line( "Arc" );
		glow = GraphicsApply.AddShotLight( GameObject, ShotColors.Dodge * 12f, reach * 6f );
	}

	protected override void OnUpdate()
	{
		var t = (Time.Now - born) / Duration;
		if ( t >= 1f )
		{
			GameObject.Destroy();
			return;
		}

		if ( follow.IsValid() )
		{
			var at = follow.WorldPosition;
			WorldPosition = new Vector3( at.x, at.y, height );
		}

		var origin = WorldPosition + Vector3.Up * 6f;
		var fade = 1f - t;
		var grow = 1f - MathF.Pow( 1f - t, 3f );

		Paint( ring, origin, reach * (0.9f + 1.7f * grow), 0f, MathF.Tau, RingSegs, ShotColors.Dodge.WithAlpha( fade ), MathX.Lerp( 16f, 3f, t ) );

		if ( toward.Length > 0.01f )
		{
			var face = MathF.Atan2( toward.y, toward.x );
			var bright = Color.Lerp( Color.White, ShotColors.Dodge, t );
			Paint( arc, origin, reach * (1.3f + 0.45f * grow), face - ArcSpan * 0.5f, ArcSpan, ArcSegs, bright.WithAlpha( fade * fade ), MathX.Lerp( 22f, 4f, t ) );
		}
		else if ( arc.IsValid() )
		{
			arc.Clear();
		}

		if ( glow.IsValid() )
			glow.LightColor = ShotColors.Dodge * (14f * fade * fade);
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

	void Paint( PolyLine line, Vector3 origin, float radius, float start, float span, int segs, Color color, float width )
	{
		if ( !line.IsValid() )
			return;

		points.Clear();
		for ( var i = 0; i <= segs; i++ )
		{
			var ang = start + span * i / segs;
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
