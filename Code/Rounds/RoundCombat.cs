namespace LoopedLoaded;

public static class RoundCombat
{
	public static void Blast( GameLoop loop, Vector2 origin, float radius, int damage, RoundProjectile source, Color tint, bool hurtPlayer = false )
	{
		if ( loop is null || radius <= 1f || damage <= 0 )
			return;

		var world = loop.Geometry.ToPlayWorld( origin );
		ArenaSounds.Explode( world );
		ImpactFlash.Spawn( loop.Scene, world, tint, MathF.Max( 1.4f, radius / 70f ) );

		foreach ( var enemy in loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive )
				continue;

			if ( (enemy.Flat - origin).Length > radius + enemy.Radius )
				continue;

			enemy.Damage( damage, source );
		}

		if ( !hurtPlayer || !loop.Runner.IsValid() )
			return;

		if ( (loop.Runner.Flat - origin).Length <= radius + loop.Runner.PlayerRadius )
			loop.TryHurt();
	}

	public static List<Vector3> Circle( ArenaGeometry geometry, Vector2 flat, float radius )
	{
		const int segments = 20;
		var points = new List<Vector3>( segments + 1 );
		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			points.Add( geometry.ToPlayWorld( flat + ArenaGeometry.FromAngle( angle ) * radius ) );
		}

		return points;
	}

	public static Color RingTint( bool friendly )
		=> friendly ? new Color( 1f, 0.42f, 0.18f ) : new Color( 0.45f, 0.86f, 1f );

	public static float PointSegment( Vector2 point, Vector2 a, Vector2 b )
	{
		var span = b - a;
		var length = span.Length;
		if ( length < 0.001f )
			return (point - a).Length;

		var t = Math.Clamp( ArenaGeometry.Dot( point - a, span ) / (length * length), 0f, 1f );
		return (point - (a + span * t)).Length;
	}
}
