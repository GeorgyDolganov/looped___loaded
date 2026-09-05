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
		AddPanels();

		for ( var i = 0; i < Walls.Count; i++ )
		{
			if ( Walls[i].Kind == WallKind.Boundary )
				boundaryWalls.Add( i );
			else if ( Walls[i].Kind == WallKind.Core )
				coreWalls.Add( i );
		}
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

	void AddPanels()
	{
		AddPanel( 0.35f, 620f, 55f, 300f );
		AddPanel( 2.45f, 640f, -35f, 340f );
		AddPanel( 4.35f, 600f, 20f, 280f );
	}

	void AddPanel( float angle, float radius, float tiltDegrees, float length )
	{
		var center = FromAngle( angle ) * radius;
		var facing = angle + MathF.PI * 0.5f + MathX.DegreeToRadian( tiltDegrees );
		var along = FromAngle( facing );
		Walls.Add( new WallSegment( center - along * length * 0.5f, center + along * length * 0.5f, Vector2.Zero, WallKind.Panel ) );
	}

	public bool TraceRay( Vector2 origin, Vector2 direction, float maxDistance, out ArenaHit hit )
	{
		hit = default;

		var closest = maxDistance;
		var found = false;

		for ( var i = 0; i < Walls.Count; i++ )
		{
			var wall = Walls[i];
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

		foreach ( var index in coreWalls )
		{
			var distance = Walls[index].SignedDistance( flat );

			if ( distance <= deepest )
				continue;

			deepest = distance;
			deepestIndex = index;
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
}
