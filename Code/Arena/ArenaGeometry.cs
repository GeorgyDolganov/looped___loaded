namespace LoopedLoaded;

public struct ArenaHit
{
	public float Distance;
	public Vector2 Position;
	public Vector2 Normal;
	public int WallIndex;
	public WallKind Kind;
}

public sealed class ArenaGeometry
{
	public float TrackRadius { get; set; } = 1000f;
	public float TrackWidth { get; set; } = 190f;
	public float BoundaryRadius { get; set; } = 1170f;
	public float CoreRadius { get; set; } = 230f;
	public float PlayHeight { get; set; } = 40f;

	public List<WallSegment> Walls { get; } = new();
	public bool CoreSolid { get; set; } = true;

	public float TrackInner => TrackRadius - TrackWidth * 0.5f;
	public float TrackOuter => TrackRadius + TrackWidth * 0.5f;

	const float SurfaceTolerance = 0.05f;

	readonly List<int> boundaryWalls = new();
	readonly List<int> coreWalls = new();

	public static Vector2 FromAngle( float radians ) => new Vector2( MathF.Cos( radians ), MathF.Sin( radians ) );

	public static float ToAngle( Vector2 v ) => MathF.Atan2( v.y, v.x );

	public static float Dot( Vector2 a, Vector2 b ) => a.x * b.x + a.y * b.y;

	public static float Cross( Vector2 a, Vector2 b ) => a.x * b.y - a.y * b.x;

	public static Vector2 Reflect( Vector2 direction, Vector2 normal ) => direction - normal * (2f * Dot( direction, normal ));

	public Vector3 ToWorld( Vector2 flat, float height ) => new Vector3( flat.x, flat.y, height );

	public Vector3 ToPlayWorld( Vector2 flat ) => new Vector3( flat.x, flat.y, PlayHeight );

	public Vector2 TrackPoint( float radians ) => FromAngle( radians ) * TrackRadius;

	public void Rebuild()
	{
		Walls.Clear();
		boundaryWalls.Clear();
		coreWalls.Clear();

		AddRing( BoundaryRadius, 24, MathF.PI / 24f, WallKind.Boundary );
		AddRing( CoreRadius, 8, 0f, WallKind.Core );

		for ( var i = 0; i < Walls.Count; i++ )
		{
			if ( Walls[i].Kind == WallKind.Boundary )
				boundaryWalls.Add( i );
			else if ( Walls[i].Kind == WallKind.Core )
				coreWalls.Add( i );
		}
	}

	public void GeneratePanels( int lap, int seed )
	{
		for ( var i = Walls.Count - 1; i >= 0; i-- )
		{
			if ( Walls[i].Kind == WallKind.Panel )
				Walls.RemoveAt( i );
		}

		var rng = new Random( unchecked( seed * 48611 + Math.Max( 1, lap ) * 7919 ) );
		var count = Math.Clamp( 1 + Math.Max( 1, lap ), 2, 6 );
		var origin = (float)rng.NextDouble() * MathF.Tau;
		var inner = CoreRadius + 110f;
		var outer = TrackInner - 95f;
		var slice = MathF.Tau / count;

		for ( var i = 0; i < count; i++ )
		{
			var angle = origin + slice * i + ( (float)rng.NextDouble() - 0.5f ) * slice * 0.42f;
			var radial = lap >= 2 && rng.NextDouble() < 0.22 + lap * 0.05;

			if ( radial )
			{
				var start = inner + 16f + (float)rng.NextDouble() * 50f;
				var end = outer - 12f - (float)rng.NextDouble() * 40f;
				if ( end - start < 150f )
					end = start + 150f;

				AddRadial( angle, start, MathF.Min( end, outer ) );
				continue;
			}

			var radius = inner + ( outer - inner ) * ( 0.18f + (float)rng.NextDouble() * 0.64f );
			var tilt = ( (float)rng.NextDouble() - 0.5f ) * 72f;
			var length = MathX.Lerp( 340f, 210f, ( count - 2 ) / 4f );
			length += ( (float)rng.NextDouble() - 0.5f ) * 48f;
			AddPanel( angle, radius, tilt, length );
		}
	}

	void AddRadial( float angle, float inner, float outer )
	{
		var dir = FromAngle( angle );
		PlacePanel( dir * inner, dir * outer );
	}

	void AddRing( float radius, int sides, float offset, WallKind kind )
	{
		var step = MathF.Tau / sides;
		var outward = kind == WallKind.Core;

		for ( var i = 0; i < sides; i++ )
		{
			var a = FromAngle( offset + step * i ) * radius;
			var b = FromAngle( offset + step * (i + 1) ) * radius;
			var facing = ((a + b) * 0.5f).Normal * (outward ? 1f : -1f);
			Walls.Add( new WallSegment( a, b, facing, kind ) );
		}
	}

	void AddPanel( float angle, float radius, float tiltDegrees, float length )
	{
		var center = FromAngle( angle ) * radius;
		var facing = angle + MathF.PI * 0.5f + MathX.DegreeToRadian( tiltDegrees );
		var along = FromAngle( facing );
		PlacePanel( center - along * length * 0.5f, center + along * length * 0.5f );
	}

	void PlacePanel( Vector2 a, Vector2 b )
	{
		a = ClampPlay( a );
		b = ClampPlay( b );

		if ( ( b - a ).Length < 90f )
			return;

		var span = b - a;
		var facing = new Vector2( -span.y, span.x ).Normal;
		Walls.Add( new WallSegment( a, b, facing, WallKind.Panel ) );
	}

	Vector2 ClampPlay( Vector2 point )
	{
		var min = CoreRadius + 70f;
		var max = TrackInner - 70f;
		var length = point.Length;

		if ( length < 1f )
			return FromAngle( 0f ) * min;

		if ( length < min )
			return point.Normal * min;

		if ( length > max )
			return point.Normal * max;

		return point;
	}

	public void Eject( ref Vector2 flat, float radius )
	{
		for ( var pass = 0; pass < 4; pass++ )
		{
			var pushed = false;

			for ( var i = 0; i < Walls.Count; i++ )
			{
				var wall = Walls[i];
				if ( wall.Kind == WallKind.Core && !CoreSolid )
					continue;

				if ( wall.Kind == WallKind.Boss )
					continue;

				var length = wall.Length;
				if ( length < 0.001f )
					continue;

				var along = Math.Clamp( Dot( flat - wall.A, wall.Direction ), 0f, length );
				var closest = wall.A + wall.Direction * along;
				var offset = flat - closest;
				var distance = offset.Length;

				if ( distance >= radius - SurfaceTolerance )
					continue;

				var normal = distance < 0.001f ? wall.Normal : offset.Normal;
				flat += normal * ( radius - distance + 1.5f );
				pushed = true;
			}

			if ( !pushed )
				return;
		}
	}

	public bool TraceRay( Vector2 origin, Vector2 direction, float maxDistance, out ArenaHit hit )
	{
		hit = default;

		var closest = maxDistance;
		var found = false;

		for ( var i = 0; i < Walls.Count; i++ )
		{
			var wall = Walls[i];
			if ( wall.Kind == WallKind.Core && !CoreSolid )
				continue;

			var span = wall.Delta;
			var denominator = Cross( direction, span );

			if ( MathF.Abs( denominator ) < 0.0000001f )
				continue;

			var offset = wall.A - origin;
			var travel = Cross( offset, span ) / denominator;
			var along = Cross( offset, direction ) / denominator;

			if ( travel <= 0f || travel >= closest )
				continue;

			if ( along < 0f || along > 1f )
				continue;

			var normal = wall.Normal;
			if ( Dot( normal, direction ) > 0f )
				normal = -normal;

			closest = travel;
			found = true;

			hit = new ArenaHit
			{
				Distance = travel,
				Position = origin + direction * travel,
				Normal = normal,
				WallIndex = i,
				Kind = wall.Kind
			};
		}

		return found;
	}

	public bool Contain( ref Vector2 flat, ref Vector2 direction, float radius )
	{
		var corrected = false;

		foreach ( var index in boundaryWalls )
		{
			var wall = Walls[index];
			var distance = wall.SignedDistance( flat );

			if ( distance >= radius - SurfaceTolerance )
				continue;

			flat += wall.Facing * (radius - distance);

			if ( Dot( direction, wall.Facing ) < 0f )
				direction = Reflect( direction, wall.Facing ).Normal;

			corrected = true;
		}

		var deepest = -float.MaxValue;
		var deepestIndex = -1;

		if ( CoreSolid )
		{
			foreach ( var index in coreWalls )
			{
				var distance = Walls[index].SignedDistance( flat );

				if ( distance <= deepest )
					continue;

				deepest = distance;
				deepestIndex = index;
			}
		}

		if ( deepestIndex >= 0 && deepest < radius - SurfaceTolerance )
		{
			var wall = Walls[deepestIndex];
			flat += wall.Facing * (radius - deepest);

			if ( Dot( direction, wall.Facing ) < 0f )
				direction = Reflect( direction, wall.Facing ).Normal;

			corrected = true;
		}

		return corrected;
	}

	public List<Vector2> PredictPath( Vector2 origin, Vector2 direction, float radius, float firstLegLimit, float bounceLegLength )
	{
		var path = new List<Vector2> { origin };

		if ( !TraceRay( origin, direction, firstLegLimit + radius, out var hit ) )
		{
			path.Add( origin + direction * firstLegLimit );
			return path;
		}

		var contact = hit.Position + hit.Normal * radius;
		path.Add( contact );

		if ( bounceLegLength <= 0f )
			return path;

		var bounced = Reflect( direction, hit.Normal ).Normal;
		var length = bounceLegLength;

		if ( TraceRay( contact, bounced, bounceLegLength + radius, out var second ) )
			length = MathF.Max( 0f, second.Distance - radius );

		path.Add( contact + bounced * length );

		return path;
	}

	public int AddBossPanel( Vector2 a, Vector2 b )
	{
		Walls.Add( new WallSegment( a, b, Vector2.Zero, WallKind.Boss ) );
		return Walls.Count - 1;
	}

	public void WriteBossPanel( int index, Vector2 a, Vector2 b )
	{
		if ( index < 0 || index >= Walls.Count )
			return;

		Walls[index] = new WallSegment( a, b, Vector2.Zero, WallKind.Boss );
	}

	public void ClearBossWalls()
	{
		for ( var i = Walls.Count - 1; i >= 0; i-- )
		{
			if ( Walls[i].Kind == WallKind.Boss )
				Walls.RemoveAt( i );
		}
	}
}
