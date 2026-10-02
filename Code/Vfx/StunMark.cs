namespace LoopedLoaded;

public sealed class StunMark : Component
{
	static readonly Color Spark = new Color( 1f, 0.9f, 0.28f );
	static readonly Color Halo = new Color( 1f, 0.58f, 0.12f );

	const int RingSegs = 18;
	const int StarCount = 3;
	const float Orbit = 54f;
	const float StarSize = 18f;

	readonly PolyLine[] stars = new PolyLine[StarCount];
	readonly List<Vector3> ringPts = new( RingSegs + 1 );
	readonly List<Vector3> starPts = new( 9 );
	PolyLine ring;
	PointLight glow;
	float until;
	float pop;

	public static StunMark Spawn( Enemy enemy, float height )
	{
		var go = enemy.Scene.CreateObject();
		go.Name = "Stun";
		go.Parent = enemy.GameObject;
		go.LocalPosition = Vector3.Up * MathF.Max( 48f, height );
		return go.AddComponent<StunMark>();
	}

	public void Arm( float duration )
	{
		var end = Time.Now + duration;
		if ( end > until )
			until = end;

		if ( pop <= 0f )
			pop = Time.Now;
	}

	public void Shift( float dt )
	{
		if ( until > 0f )
			until += dt;

		if ( pop > 0f )
			pop += dt;
	}

	protected override void OnStart()
	{
		ring = Line( "Halo" );
		for ( var i = 0; i < StarCount; i++ )
			stars[i] = Line( "Star" );

		glow = GraphicsApply.AddShotLight( GameObject, Spark * 8f, 180f );
	}

	protected override void OnUpdate()
	{
		if ( until <= 0f )
			return;

		var enemy = GameObject.Parent.IsValid() ? GameObject.Parent.GetComponent<Enemy>() : null;
		if ( !enemy.IsValid() || !enemy.Alive || Time.Now >= until )
		{
			GameObject.Destroy();
			return;
		}

		var left = until - Time.Now;
		var fade = left < 0.12f ? MathF.Max( 0f, left / 0.12f ) : 1f;
		var kick = Math.Clamp( (Time.Now - pop) / 0.08f, 0f, 1f );
		var grow = 1f - MathF.Pow( 1f - kick, 3f );
		var spin = Time.Now * 8.5f;
		var origin = WorldPosition;
		var reach = Orbit * MathX.Lerp( 0.4f, 1f, grow );

		PaintRing( origin, reach * (1f + 0.07f * MathF.Sin( spin * 2f )), Halo.WithAlpha( fade ), MathX.Lerp( 4f, 12f, fade ) );

		for ( var i = 0; i < StarCount; i++ )
		{
			var ang = spin + MathF.Tau * i / StarCount;
			var bob = MathF.Sin( spin * 1.7f + i * 1.4f ) * 12f;
			var at = origin + new Vector3( MathF.Cos( ang ) * reach, MathF.Sin( ang ) * reach, bob );
			PaintStar( stars[i], at, StarSize * MathF.Max( 0.35f, grow ), ang * 2.4f, Spark.WithAlpha( fade ) );
		}

		if ( glow.IsValid() )
		{
			var blink = 0.55f + 0.45f * MathF.Sin( spin * 3f );
			glow.LightColor = Spark * (12f * fade * blink);
			glow.Radius = (140f + reach) * GraphicsProfile.ShotLightScale;
		}
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

	void PaintRing( Vector3 origin, float radius, Color color, float width )
	{
		if ( !ring.IsValid() )
			return;

		if ( ringPts.Count != RingSegs + 1 )
		{
			ringPts.Clear();
			for ( var n = 0; n <= RingSegs; n++ )
				ringPts.Add( Vector3.Zero );
		}

		for ( var i = 0; i <= RingSegs; i++ )
		{
			var ang = MathF.Tau * i / RingSegs;
			var wobble = 1f + 0.08f * MathF.Sin( ang * 3f + Time.Now * 16f );
			ringPts[i] = origin + new Vector3( MathF.Cos( ang ) * radius * wobble, MathF.Sin( ang ) * radius * wobble, 0f );
		}

		ring.HeadTint = color;
		ring.TailTint = color;
		ring.HeadWidth = width;
		ring.TailWidth = width;
		ring.Apply();
		ring.SetPoints( ringPts );
	}

	void PaintStar( PolyLine line, Vector3 center, float size, float yaw, Color color )
	{
		if ( !line.IsValid() )
			return;

		starPts.Clear();
		for ( var n = 0; n < 8; n++ )
		{
			var radius = (n & 1) == 0 ? size : size * 0.32f;
			var ang = yaw + MathF.Tau * n / 8f;
			starPts.Add( center + new Vector3( MathF.Cos( ang ) * radius, MathF.Sin( ang ) * radius, 0f ) );
		}

		starPts.Add( starPts[0] );
		line.HeadTint = color;
		line.TailTint = color;
		line.HeadWidth = 8f;
		line.TailWidth = 8f;
		line.Apply();
		line.SetPoints( starPts );
	}
}
