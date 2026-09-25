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

			return GameSettings.Boss.PhaseOf( body.Health / (float)body.MaxHealth );
		}
	}

	public int BrokenNeed => Phase == 1 ? GameSettings.Boss.LensBreakPhase1 : GameSettings.Boss.LensBreakLater;

	public bool Open => loop.IsValid() && loop.Arena.IsValid() && loop.Arena.GlassBroken >= BrokenNeed;

	public void Arm( Enemy enemy )
	{
		body = enemy;
		loop = enemy.Loop;
		shotAt = Time.Now + GameSettings.Boss.Lens.FirstShot;
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
			loop.Announce( GameSettings.Text.F( GameSettings.Text.Announce.LensPhase, phase ) );
			ArenaSounds.Warn();
			ImpactFlash.Spawn( Scene, Vector3.Up * 80f, LensTint, 3.2f );
		}

		if ( Open && !announcedOpen )
		{
			announcedOpen = true;
			loop.Announce( GameSettings.Text.Announce.LensOpen );
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
		if ( Time.Now < shotAt || scale < GameSettings.Boss.Lens.FreezeShotLock )
			return;

		var threat = loop.Threat;
		var lens = GameSettings.Boss.Lens;
		var interval = MathF.Max( lens.ShotFloor, (phase == 1 ? lens.ShotPhase1 : phase == 2 ? lens.ShotPhase2 : lens.ShotPhase3) / MathF.Sqrt( threat ) );
		shotAt = Time.Now + interval;

		EnemyShot.Fire( loop, ArenaGeometry.FromAngle( Time.Now * lens.AimedSpin ) * lens.AimedOrigin, Lead(), lens.AimedSpeed * threat );

		if ( phase < 2 )
			return;

		var burst = phase >= 3 ? lens.BurstPhase3 : lens.BurstPhase2;
		for ( var i = 0; i < burst; i++ )
		{
			var angle = Time.Now * lens.BurstSpin + MathF.Tau * i / burst;
			EnemyShot.Fire( loop, ArenaGeometry.FromAngle( angle ) * lens.BurstOrigin, ArenaGeometry.FromAngle( angle ), lens.BurstSpeed * threat );
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
		var speed = GameSettings.Boss.Lens.AimedSpeed * loop.Threat;
		var travel = to.Length / MathF.Max( GameSettings.Boss.Lens.LeadMinSpeed, speed );
		var predicted = runner.Flat + runner.Tangent * (runner.Speed * travel);
		return predicted.Length > 1f ? predicted.Normal : to.Normal;
	}
}
