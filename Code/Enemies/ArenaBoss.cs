namespace LoopedLoaded;

public sealed class ArenaBoss : Component
{
	static readonly Color CoreTint = new Color( 0.78f, 0.18f, 0.32f );
	static readonly Color ArmorTint = new Color( 0.55f, 0.62f, 0.78f );
	static readonly Color SpokeTint = new Color( 0.32f, 0.82f, 0.9f );

	Enemy body;
	GameLoop loop;
	PolyLine pulseLine;
	readonly List<GameObject> spokes = new();
	readonly List<int> spokeWalls = new();
	float spin;
	float shotAt;
	float pulseAt;
	float pulseRadius = -1f;
	int lastPhase = 1;
	bool summoned;

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
		shotAt = Time.Now + GameSettings.Boss.Core.FirstShot;
		pulseAt = Time.Now + GameSettings.Boss.Core.FirstPulse;
		pulseRadius = -1f;
		spin = 0f;
		summoned = false;
		lastPhase = 1;

		var ring = Scene.CreateObject();
		ring.Name = "Pulse";
		ring.Parent = GameObject;
		pulseLine = ring.AddComponent<PolyLine>();
		pulseLine.HeadWidth = 10f;
		pulseLine.TailWidth = 10f;
		pulseLine.HeadTint = CoreTint;
		pulseLine.TailTint = CoreTint;
		pulseLine.Apply();
	}

	public void ShiftTime( float dt )
	{
		shotAt += dt;
		pulseAt += dt;
	}

	protected override void OnUpdate()
	{
		if ( !body.IsValid() || !body.Alive || !loop.IsValid() )
			return;

		if ( loop.IsFrozen )
		{
			PaintPulse();
			return;
		}

		var phase = Phase;
		if ( phase != lastPhase )
		{
			lastPhase = phase;
			loop.Announce( GameSettings.Text.F( GameSettings.Text.Announce.CorePhase, phase ) );
			ArenaSounds.Warn();
			ImpactFlash.Spawn( Scene, Vector3.Up * 80f, CoreTint, 3.2f );
		}

		var frozen = body.Frozen;
		var scale = frozen ? body.SlowScale : 1f;
		spin += (GameSettings.Boss.Core.Spin + phase * GameSettings.Boss.Core.SpinPerPhase) * scale * Time.Delta;

		SyncSpokes( phase );
		ThinkShoot( phase, scale );
		ThinkPulse( phase, scale );
		PaintPulse();
	}

	protected override void OnDestroy()
	{
		if ( loop.IsValid() && loop.Geometry is not null )
			loop.Geometry.ClearBossWalls();

		foreach ( var spoke in spokes )
		{
			if ( spoke.IsValid() )
				spoke.Destroy();
		}

		spokes.Clear();
		spokeWalls.Clear();
	}

	void SyncSpokes( int phase )
	{
		var count = phase >= 3 ? GameSettings.Boss.Core.SpokesPhase3 : phase >= 2 ? GameSettings.Boss.Core.SpokesPhase2 : GameSettings.Boss.Core.SpokesPhase1;
		var geo = loop.Geometry;

		while ( spokes.Count > count )
		{
			var last = spokes[^1];
			spokes.RemoveAt( spokes.Count - 1 );
			if ( last.IsValid() )
				last.Destroy();
		}

		while ( spokes.Count < count )
		{
			var go = Blocks.SpawnBox( GameObject, $"Spoke {spokes.Count}", Vector3.Zero, Rotation.Identity,
				new Vector3( 260f, 22f, 90f ), SpokeTint );
			spokes.Add( go );
			spokeWalls.Add( geo.AddBossPanel( Vector2.Zero, Vector2.Right ) );
		}

		if ( count == 0 )
		{
			if ( spokeWalls.Count > 0 )
			{
				geo.ClearBossWalls();
				spokeWalls.Clear();
			}

			return;
		}

		var inner = geo.CoreRadius + GameSettings.Boss.Core.SpokeInnerPad;
		var outer = inner + GameSettings.Boss.Core.SpokeLength;

		for ( var i = 0; i < count; i++ )
		{
			var angle = spin + MathF.Tau * i / count;
			var dir = ArenaGeometry.FromAngle( angle );
			var a = dir * inner;
			var b = dir * outer;
			geo.WriteBossPanel( spokeWalls[i], a, b );

			var mid = (a + b) * 0.5f;
			if ( !spokes[i].IsValid() )
				continue;

			spokes[i].WorldPosition = new Vector3( mid.x, mid.y, 50f );
			spokes[i].WorldRotation = Blocks.FlatFacing( dir );
			var along = (outer - inner);
			var bounds = Blocks.Box.Bounds.Size;
			spokes[i].WorldScale = new Vector3(
				bounds.x > 0.001f ? along / bounds.x : 1f,
				bounds.y > 0.001f ? 22f / bounds.y : 1f,
				bounds.z > 0.001f ? 90f / bounds.z : 1f );
		}
	}

	void ThinkShoot( int phase, float scale )
	{
		if ( Time.Now < shotAt || scale < GameSettings.Boss.Core.FreezeShotLock )
			return;

		var threat = loop.Threat;
		var core = GameSettings.Boss.Core;
		var interval = MathF.Max( core.ShotFloor, (phase == 1 ? core.ShotPhase1 : phase == 2 ? core.ShotPhase2 : core.ShotPhase3) / MathF.Sqrt( threat ) );
		shotAt = Time.Now + interval;

		var origin = ArenaGeometry.FromAngle( spin ) * core.AimedOrigin;
		var aimed = Lead();
		var aimedSpeed = core.AimedSpeed * threat;
		EnemyShot.Fire( loop, origin, aimed, aimedSpeed );

		if ( phase < 2 )
			return;

		var burst = phase >= 3 ? core.BurstPhase3 : core.BurstPhase2;
		for ( var i = 0; i < burst; i++ )
		{
			var angle = spin + MathF.Tau * i / burst;
			EnemyShot.Fire( loop, ArenaGeometry.FromAngle( angle ) * core.BurstOrigin, ArenaGeometry.FromAngle( angle ), core.BurstSpeed * threat );
		}

		if ( phase >= 3 && !summoned )
		{
			summoned = true;
			SpawnGuard();
		}
	}

	void ThinkPulse( int phase, float scale )
	{
		if ( phase < 2 )
		{
			pulseRadius = -1f;
			return;
		}

		if ( pulseRadius < 0f )
		{
			if ( Time.Now >= pulseAt )
				pulseRadius = loop.Geometry.CoreRadius;
			return;
		}

		pulseRadius += GameSettings.Boss.Core.PulseSpeed * scale * Time.Delta;
		var reach = loop.Runner.PlayerRadius + GameSettings.Boss.Core.PulseReachPad;

		if ( MathF.Abs( pulseRadius - loop.Runner.Radius ) <= reach )
			loop.TryHurt();

		if ( pulseRadius >= loop.Geometry.TrackOuter + GameSettings.Boss.Core.PulseEndPad )
		{
			pulseRadius = -1f;
			pulseAt = Time.Now + (phase >= 3 ? GameSettings.Boss.Core.PulsePhase3 : GameSettings.Boss.Core.PulsePhase2);
		}
	}

	void PaintPulse()
	{
		if ( !pulseLine.IsValid() )
			return;

		if ( pulseRadius < 0f )
		{
			pulseLine.Clear();
			return;
		}

		const int segments = 48;
		var points = new List<Vector3>( segments + 1 );
		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			points.Add( loop.Geometry.ToWorld( ArenaGeometry.FromAngle( angle ) * pulseRadius, 18f ) );
		}

		pulseLine.HeadTint = CoreTint;
		pulseLine.TailTint = CoreTint;
		pulseLine.Apply();
		pulseLine.SetPoints( points );
	}

	Vector2 Lead()
	{
		var runner = loop.Runner;
		var origin = Vector2.Zero;
		var to = runner.Flat - origin;
		var speed = GameSettings.Boss.Core.AimedSpeed * loop.Threat;
		var travel = to.Length / speed;
		var pace = runner.Speed;
		if ( runner.Slowing )
			pace *= runner.SlowSpeedScale;

		var predicted = runner.Flat + runner.Tangent * (pace * travel);
		return predicted.Length > 1f ? predicted.Normal : to.Normal;
	}

	void SpawnGuard()
	{
		var go = loop.Scene.CreateObject();
		go.Name = "Core Guard";

		var enemy = go.AddComponent<Enemy>();
		enemy.Arena = loop.Arena;
		enemy.Loop = loop;
		enemy.Setup( EnemyKind.Shield, ArenaGeometry.FromAngle( loop.Runner.Angle + MathF.PI ) * (loop.Geometry.CoreRadius + GameSettings.Boss.Core.GuardPad), Progression.EnemyHealth( GameSettings.Boss.Core.GuardHealth, loop.Lap, loop.LocationIndex ) );
		loop.Enemies.Add( enemy );
	}
}
