namespace LoopedLoaded;

// Placeholder shield for the Lens boss: a flattened box that player rounds break in one hit.
// Boss shots never look at these, so they pass straight through.
public sealed class LensShield : Component
{
	static readonly Color ShieldTint = new Color( 0.55f, 0.85f, 0.95f );
	static readonly Color CrackedTint = new Color( 0.95f, 0.98f, 1f );

	int hitsLeft = 2;
	float lastHit = -99f;

	GameLoop loop;
	GameObject visual;
	Vector2 center;
	Vector2 along;
	float angle;
	float radius;
	float turn;
	float halfLength;
	float halfThick;

	// turn: +1 orbits counter-clockwise, -1 clockwise, at a fixed speed from the boss config.
	public static LensShield Spawn( GameObject parent, GameLoop loop, float angle, float radius, float turn, float length, float thickness, float height )
	{
		var go = parent.Scene.CreateObject();
		go.Name = "Lens Shield";
		go.Parent = parent;

		var shield = go.AddComponent<LensShield>();
		shield.loop = loop;
		shield.angle = angle;
		shield.radius = radius;
		shield.turn = turn;
		shield.halfLength = length * 0.5f;
		shield.halfThick = thickness * 0.5f;
		shield.hitsLeft = Math.Max( 1, GameSettings.Boss.Lens.ShieldHits );
		shield.Place();

		var world = loop.Geometry.ToPlayWorld( shield.center );
		go.WorldPosition = world;
		shield.visual = Blocks.SpawnBox( go, "Shield Box", world, Blocks.FlatFacing( shield.along ),
			new Vector3( length, thickness, height ), ShieldTint, false );

		loop.Shields.Add( shield );
		return shield;
	}

	void Place()
	{
		var normal = ArenaGeometry.FromAngle( angle );
		center = normal * radius;
		along = new Vector2( -normal.y, normal.x );
	}

	protected override void OnUpdate()
	{
		if ( !loop.IsValid() || loop.IsFrozen )
			return;

		var degrees = GameSettings.Boss.Lens.ShieldDegreesPerSecond;
		angle += turn * MathX.DegreeToRadian( degrees ) * RealTime.Delta;
		Place();

		var world = loop.Geometry.ToPlayWorld( center );
		GameObject.WorldPosition = world;
		if ( visual.IsValid() )
		{
			visual.WorldPosition = world;
			visual.WorldRotation = Blocks.FlatFacing( along );
		}
	}

	// Nearest shield touched by something of the given radius travelling a -> b, with the distance to first contact.
	public static LensShield Nearest( GameLoop loop, Vector2 a, Vector2 b, float radius, out float distance )
	{
		distance = 0f;
		if ( !loop.IsValid() || loop.Shields.Count == 0 )
			return null;

		LensShield best = null;
		var bestAt = 2f;
		foreach ( var shield in loop.Shields )
		{
			if ( !shield.IsValid() || !shield.Crosses( a, b, radius, out var at ) || at >= bestAt )
				continue;

			best = shield;
			bestAt = at;
		}

		if ( best is not null )
			distance = bestAt * (b - a).Length;

		return best;
	}

	// True when a round of the given radius travelling a -> b touches this shield.
	public bool Crosses( Vector2 a, Vector2 b, float radius ) => Crosses( a, b, radius, out _ );

	public bool Crosses( Vector2 a, Vector2 b, float radius, out float at )
	{
		at = 0f;
		var across = new Vector2( -along.y, along.x );
		var pa = new Vector2( ArenaGeometry.Dot( a - center, along ), ArenaGeometry.Dot( a - center, across ) );
		var pb = new Vector2( ArenaGeometry.Dot( b - center, along ), ArenaGeometry.Dot( b - center, across ) );
		var delta = pb - pa;
		var hx = halfLength + radius;
		var hy = halfThick + radius;

		var enter = 0f;
		var leave = 1f;
		if ( !Slab( pa.x, delta.x, hx, ref enter, ref leave ) || !Slab( pa.y, delta.y, hy, ref enter, ref leave ) )
			return false;

		at = enter;
		return true;
	}

	static bool Slab( float start, float delta, float half, ref float enter, ref float leave )
	{
		if ( MathF.Abs( delta ) < 0.0001f )
			return MathF.Abs( start ) <= half;

		var t0 = (-half - start) / delta;
		var t1 = (half - start) / delta;
		if ( t0 > t1 )
			(t0, t1) = (t1, t0);

		enter = MathF.Max( enter, t0 );
		leave = MathF.Min( leave, t1 );
		return enter <= leave;
	}

	// One hit on the shield. Breaks it once the configured number of hits has landed.
	// cooldown > 0 ignores hits that arrive sooner than that after the last one (for continuous beams).
	public void Hit( float cooldown = 0f )
	{
		if ( !GameObject.IsValid() )
			return;

		if ( cooldown > 0f && RealTime.Now - lastHit < cooldown )
			return;

		lastHit = RealTime.Now;
		hitsLeft--;

		if ( hitsLeft > 0 )
		{
			Crack();
			return;
		}

		if ( loop.IsValid() )
		{
			loop.Shields.Remove( this );
			var world = loop.Geometry.ToPlayWorld( center );
			ImpactFlash.Spawn( Scene, world, ShieldTint, 1.3f );
			ArenaSounds.Crack( world );
		}

		GameObject.Destroy();
	}

	void Crack()
	{
		if ( loop.IsValid() )
		{
			var world = loop.Geometry.ToPlayWorld( center );
			ImpactFlash.Spawn( Scene, world, CrackedTint, 0.8f );
			ArenaSounds.Crack( world );
		}

		var renderer = visual.IsValid() ? visual.GetComponent<ModelRenderer>() : null;
		if ( renderer.IsValid() )
			renderer.Tint = CrackedTint;
	}

	protected override void OnDestroy()
	{
		if ( loop.IsValid() )
			loop.Shields.Remove( this );
	}
}
