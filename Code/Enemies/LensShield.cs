namespace LoopedLoaded;

// Placeholder shield for the Lens boss: a flattened box that player rounds break in one hit.
// Boss shots never look at these, so they pass straight through.
public sealed class LensShield : Component
{
	static readonly Color ShieldTint = new Color( 0.55f, 0.85f, 0.95f );

	GameLoop loop;
	Vector2 center;
	Vector2 along;
	float halfLength;
	float halfThick;

	public static LensShield Spawn( GameObject parent, GameLoop loop, Vector2 center, Vector2 along, float length, float thickness, float height )
	{
		var go = parent.Scene.CreateObject();
		go.Name = "Lens Shield";
		go.Parent = parent;

		var shield = go.AddComponent<LensShield>();
		shield.loop = loop;
		shield.center = center;
		shield.along = along.Normal;
		shield.halfLength = length * 0.5f;
		shield.halfThick = thickness * 0.5f;

		var world = loop.Geometry.ToPlayWorld( center );
		go.WorldPosition = world;
		Blocks.SpawnBox( go, "Shield Box", world, Blocks.FlatFacing( shield.along ),
			new Vector3( length, thickness, height ), ShieldTint, false );

		loop.Shields.Add( shield );
		return shield;
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

	public void Shatter()
	{
		if ( !GameObject.IsValid() )
			return;

		if ( loop.IsValid() )
		{
			loop.Shields.Remove( this );
			var world = loop.Geometry.ToPlayWorld( center );
			ImpactFlash.Spawn( Scene, world, ShieldTint, 1.3f );
			ArenaSounds.Crack( world );
		}

		GameObject.Destroy();
	}

	protected override void OnDestroy()
	{
		if ( loop.IsValid() )
			loop.Shields.Remove( this );
	}
}
