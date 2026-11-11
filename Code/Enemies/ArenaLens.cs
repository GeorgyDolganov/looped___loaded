namespace LoopedLoaded;

public sealed class ArenaLens : Component
{
	static readonly Color LensTint = new Color( 0.55f, 0.85f, 0.95f );

	Enemy body;
	GameLoop loop;
	float shotAt;
	int lastPhase = 1;
	int lapPeak = 1;
	readonly List<LensShield> shields = new();

	public int Phase
	{
		get
		{
			if ( body is null || body.MaxHealth <= 0 )
				return 1;

			return GameSettings.Boss.PhaseOf( body.Health / (float)body.MaxHealth );
		}
	}

	public void Arm( Enemy enemy )
	{
		body = enemy;
		loop = enemy.Loop;
		shotAt = Time.Now + GameSettings.Boss.Lens.FirstShot;
		lastPhase = 1;
		lapPeak = loop.IsValid() && loop.Runner.IsValid() ? loop.Runner.Lap : 1;
		RaiseShields();
	}

	public void ShiftTime( float dt )
	{
		shotAt += dt;
	}

	protected override void OnUpdate()
	{
		if ( !body.IsValid() || !body.Alive || !loop.IsValid() )
			return;

		if ( loop.IsFrozen )
			return;

		TickLaps();

		var phase = Phase;
		if ( phase != lastPhase )
		{
			lastPhase = phase;
			loop.RerollBoard();
			ArenaSounds.Warn();
			ImpactFlash.Spawn( Scene, Vector3.Up * 80f, LensTint, 3.2f );
		}

		ThinkShoot( phase, body.Frozen ? body.SlowScale : 1f );
	}

	protected override void OnDestroy() => ClearShields();

	// Each new full lap (never a repeat of one already counted) raises a fresh ring of shields.
	void TickLaps()
	{
		if ( !loop.Runner.IsValid() )
			return;

		var lap = loop.Runner.Lap;
		if ( lap <= lapPeak )
			return;

		lapPeak = lap;
		RaiseShields();
	}

	void RaiseShields()
	{
		ClearShields();
		if ( !loop.IsValid() )
			return;

		var lens = GameSettings.Boss.Lens;
		var count = Math.Max(3, lens.ShieldCount);
		var radius = MathF.Max(1f, lens.ShieldRadius);

		// Flat shields touching a circle of this radius meet corner to corner, so the
		// side of the polygon is the longest a shield can be; the gap keeps neighbours apart.
		var side = 2f * radius * MathF.Tan( MathF.PI / count );
		var length = MathF.Max( 20f, side - lens.ShieldGap );
		var offset = Game.Random.Float( 0f, MathF.Tau / count );

		for ( var i = 0; i < count; i++ )
		{
			var angle = offset + MathF.Tau * i / count;
			var normal = ArenaGeometry.FromAngle( angle );
			var along = new Vector2( -normal.y, normal.x );
			shields.Add( LensShield.Spawn( GameObject, loop, normal * radius, along, length, lens.ShieldThickness, lens.ShieldHeight ) );
		}

		ArenaSounds.Tele();
	}

	void ClearShields()
	{
		foreach ( var shield in shields )
		{
			if ( shield.IsValid() )
				shield.GameObject.Destroy();
		}

		shields.Clear();
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
