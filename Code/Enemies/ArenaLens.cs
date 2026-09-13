namespace LoopedLoaded;

public sealed class ArenaLens : Component
{
	static readonly Color LensTint = new Color( 0.55f, 0.85f, 0.95f );

	Enemy body;
	GameLoop loop;
	GameObject disk;
	PolyLine rim;
	float shotAt;
	int lastPhase = 1;
	bool announcedOpen;

	public int Phase
	{
		get
		{
			if ( body is null || body.MaxHealth <= 0 )
				return 1;

			var part = body.Health / (float)body.MaxHealth;
			if ( part > 0.66f )
				return 1;
			if ( part > 0.33f )
				return 2;
			return 3;
		}
	}

	public int BrokenNeed => Phase == 1 ? 2 : 3;

	public bool Open => loop.IsValid() && loop.Arena.IsValid() && loop.Arena.GlassBroken >= BrokenNeed;

	public void Arm( Enemy enemy )
	{
		body = enemy;
		loop = enemy.Loop;
		shotAt = Time.Now + 0.85f;
		lastPhase = 1;
		announcedOpen = false;

		disk = Blocks.SpawnSphere( GameObject, "Lens Disk", Vector3.Up * 40f,
			loop.IsValid() ? loop.Geometry.CoreRadius * 2f : 460f,
			new Color( 0.45f, 0.78f, 0.92f, 0.22f ), false );

		var ring = Scene.CreateObject();
		ring.Name = "Lens Rim";
		ring.Parent = GameObject;
		rim = ring.AddComponent<PolyLine>();
		rim.HeadWidth = 8f;
		rim.TailWidth = 8f;
		rim.HeadTint = LensTint;
		rim.TailTint = LensTint;
		rim.Apply();
	}

	public void ShiftTime( float dt )
	{
		shotAt += dt;
	}

	protected override void OnUpdate()
	{
		if ( !body.IsValid() || !body.Alive || !loop.IsValid() )
			return;

		PaintRim();

		if ( loop.IsFrozen )
			return;

		var phase = Phase;
		if ( phase != lastPhase )
		{
			lastPhase = phase;
			announcedOpen = false;
			loop.RerollBoard();
			loop.Announce( phase == 3 ? "LENS PHASE 3  ·  NEW GLASS" : "LENS PHASE 2  ·  NEW GLASS" );
			ArenaSounds.Warn();
			ImpactFlash.Spawn( Scene, Vector3.Up * 80f, LensTint, 3.2f );
		}

		if ( Open && !announcedOpen )
		{
			announcedOpen = true;
			loop.Announce( "LENS OPEN  ·  HIT THE SIDE" );
			ArenaSounds.Tele();
		}

		ThinkShoot( phase, body.Frozen ? body.SlowScale : 1f );
	}

	protected override void OnDestroy()
	{
		disk?.Destroy();
		rim?.GameObject?.Destroy();
	}

	void ThinkShoot( int phase, float scale )
	{
		if ( Time.Now < shotAt || scale < 0.2f )
			return;

		var threat = loop.Threat;
		var interval = MathF.Max( 0.6f, (phase == 1 ? 2.0f : phase == 2 ? 1.45f : 1.1f) / MathF.Sqrt( threat ) );
		shotAt = Time.Now + interval;

		EnemyShot.Fire( loop, ArenaGeometry.FromAngle( Time.Now * 0.4f ) * 36f, Lead(), 500f * threat );

		if ( phase < 2 )
			return;

		var burst = phase >= 3 ? 6 : 4;
		for ( var i = 0; i < burst; i++ )
		{
			var angle = Time.Now * 0.35f + MathF.Tau * i / burst;
			EnemyShot.Fire( loop, ArenaGeometry.FromAngle( angle ) * 44f, ArenaGeometry.FromAngle( angle ), 460f * threat );
		}
	}

	void PaintRim()
	{
		if ( !rim.IsValid() || !loop.IsValid() )
			return;

		var radius = loop.Geometry.CoreRadius;
		const int segments = 48;
		var points = new List<Vector3>( segments + 1 );
		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			points.Add( loop.Geometry.ToWorld( ArenaGeometry.FromAngle( angle ) * radius, 22f ) );
		}

		var glow = Open ? Color.Lerp( LensTint, Color.White, 0.45f ) : LensTint * 0.7f;
		rim.HeadTint = glow;
		rim.TailTint = glow;
		rim.Apply();
		rim.SetPoints( points );

		if ( disk.IsValid() )
		{
			var renderer = disk.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				renderer.Tint = new Color( glow.r, glow.g, glow.b, Open ? 0.34f : 0.16f );
		}
	}

	Vector2 Lead()
	{
		var runner = loop.Runner;
		var to = runner.Flat;
		var speed = 500f * loop.Threat;
		var travel = to.Length / MathF.Max( 80f, speed );
		var pace = runner.Speed;
		if ( runner.Slowing )
			pace *= runner.SlowSpeedScale;

		var predicted = runner.Flat + runner.Tangent * (pace * travel);
		return predicted.Length > 1f ? predicted.Normal : to.Normal;
	}
}
