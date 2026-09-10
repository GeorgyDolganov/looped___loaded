namespace LoopedLoaded;

public static class RoundCombat
{
	public static void Blast( GameLoop loop, Vector2 origin, float radius, int damage, RoundProjectile source, Color tint )
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
	}

	public static void Lightning( GameLoop loop, Vector2 origin, int jumps, int damage, RoundProjectile source, HashSet<Enemy> ignore )
	{
		if ( loop is null || jumps <= 0 )
			return;

		var taken = ignore ?? new HashSet<Enemy>();
		var from = origin;

		for ( var i = 0; i < jumps; i++ )
		{
			Enemy best = null;
			var bestDist = 220f;

			foreach ( var enemy in loop.Enemies )
			{
				if ( !enemy.IsValid() || !enemy.Alive || taken.Contains( enemy ) )
					continue;

				var dist = (enemy.Flat - from).Length;
				if ( dist >= bestDist )
					continue;

				bestDist = dist;
				best = enemy;
			}

			if ( best is null )
				return;

			taken.Add( best );
			best.Damage( damage, source );
			var world = loop.Geometry.ToPlayWorld( best.Flat );
			ArenaSounds.Crack( world );
			ImpactFlash.Spawn( loop.Scene, world, new Color( 0.55f, 0.9f, 1f ), 0.85f );
			from = best.Flat;
		}
	}

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
