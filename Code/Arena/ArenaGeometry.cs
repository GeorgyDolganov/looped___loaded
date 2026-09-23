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
	public bool GlassRules { get; set; }
	public int AuthoredCount { get; private set; }
	public int GlassBroken { get; private set; }

	public float TrackInner => TrackRadius - TrackWidth * 0.5f;
	public float TrackOuter => TrackRadius + TrackWidth * 0.5f;

	const float SurfaceTolerance = 0.05f;
	const float SkinWidth = 0.75f;

	readonly List<int> boundaryWalls = new();
	readonly List<int> coreWalls = new();
	readonly Dictionary<int, int> panelKicks = new();
	readonly Dictionary<int, int> panelHits = new();
	readonly Dictionary<int, float> shardUntil = new();
	readonly List<Spinner> spinners = new();

	public static Vector2 FromAngle( float radians ) => new Vector2( MathF.Cos( radians ), MathF.Sin( radians ) );

	public static float ToAngle( Vector2 v ) => MathF.Atan2( v.y, v.x );

	public static float Dot( Vector2 a, Vector2 b ) => a.x * b.x + a.y * b.y;

	public static float Cross( Vector2 a, Vector2 b ) => a.x * b.y - a.y * b.x;

	public static Vector2 Reflect( Vector2 direction, Vector2 normal ) => direction - normal * (2f * Dot( direction, normal ));

	public static Vector2 AgainstClock( Vector2 pos )
	{
		var len = pos.Length;
		if ( len < 0.01f )
			return new Vector2( -1f, 0f );

		return new Vector2( -pos.y / len, pos.x / len );
	}

	public static Vector2 DriftHeading( Vector2 pos, Vector2 direction, float flightSpeed, float spinSpeed )
	{
		var dir = direction.Length > 0.01f ? direction.Normal : Vector2.Right;
		var v = dir * MathF.Max( 0.2f, flightSpeed );
		if ( spinSpeed > 0.01f )
			v += AgainstClock( pos ) * spinSpeed;

		var mag = v.Length;
		return mag < 0.001f ? dir : v / mag;
	}

	public Vector3 ToWorld( Vector2 flat, float height ) => new Vector3( flat.x, flat.y, height );

	public Vector3 ToPlayWorld( Vector2 flat ) => new Vector3( flat.x, flat.y, PlayHeight );

	public Vector2 TrackPoint( float radians ) => FromAngle( radians ) * TrackRadius;

	public void Rebuild() => ApplyAuthored( null );

	public void ApplyAuthored( IReadOnlyList<WallSegment> walls )
	{
		Walls.Clear();
		boundaryWalls.Clear();
		coreWalls.Clear();
		panelKicks.Clear();
		panelHits.Clear();
		shardUntil.Clear();
		spinners.Clear();
		GlassBroken = 0;

		if ( walls is null || walls.Count == 0 )
		{
			AddRing( BoundaryRadius, 24, MathF.PI / 24f, WallKind.Boundary );
			AddRing( CoreRadius, 8, 0f, WallKind.Core );
		}
		else
		{
			foreach ( var wall in walls )
				Walls.Add( wall );
		}

		for ( var i = 0; i < Walls.Count; i++ )
		{
			if ( Walls[i].Kind == WallKind.Boundary )
				boundaryWalls.Add( i );
			else if ( Walls[i].Kind == WallKind.Core )
				coreWalls.Add( i );
		}

		AuthoredCount = Walls.Count;
	}

	public void ClearGeneratedPanels()
	{
		for ( var i = Walls.Count - 1; i >= AuthoredCount; i-- )
		{
			var kind = Walls[i].Kind;
			if ( kind == WallKind.Panel || kind == WallKind.Shard || kind == WallKind.Spin )
				Walls.RemoveAt( i );
		}

		panelKicks.Clear();
		panelHits.Clear();
		shardUntil.Clear();
		spinners.Clear();
		GlassBroken = 0;
	}

	public void GeneratePanels( int lap, int seed )
	{
		ClearGeneratedPanels();

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

	public Vector2 ClampField( Vector2 point ) => ClampPlay( point );

	public bool PanelCracked( int index )
	{
		if ( index < 0 || index >= Walls.Count || Walls[index].Kind != WallKind.Panel )
			return false;

		return panelHits.TryGetValue( index, out var hits ) && hits >= 1 && Walls[index].Length >= 1f;
	}

	public int AddShard( Vector2 a, Vector2 b, float diesAt )
	{
		a = ClampPlay( a );
		b = ClampPlay( b );
		if ( (b - a).Length < 40f )
			return -1;

		var span = b - a;
		var facing = new Vector2( -span.y, span.x ).Normal;
		Walls.Add( new WallSegment( a, b, facing, WallKind.Shard ) );
		var index = Walls.Count - 1;
		shardUntil[index] = diesAt;
		return index;
	}

	public void CollectExpiredShards( float now, List<int> expired )
	{
		expired.Clear();
		for ( var i = 0; i < Walls.Count; i++ )
		{
			if ( Walls[i].Kind != WallKind.Shard )
				continue;

			if ( !shardUntil.TryGetValue( i, out var until ) || until > now )
				continue;

			Walls[i] = new WallSegment( Vector2.Zero, Vector2.Zero, Vector2.Zero, WallKind.Shard );
			shardUntil.Remove( i );
			expired.Add( i );
		}
	}

	public GlassHit StrikePanel( int index, Vector2 hitPos, Vector2 hitNormal, float extraDegrees, bool allowSecond )
	{
		if ( index < 0 || index >= Walls.Count )
			return GlassHit.None;

		var wall = Walls[index];
		if ( wall.Kind == WallKind.Shard || wall.Length < 1f )
			return GlassHit.None;

		if ( wall.Kind != WallKind.Panel )
			return GlassHit.None;

		if ( !GlassRules )
		{
			KickPanel( index, hitPos, hitNormal, extraDegrees, allowSecond );
			return GlassHit.Kick;
		}

		panelHits.TryGetValue( index, out var hits );
		hits++;
		panelHits[index] = hits;

		if ( hits >= 2 )
		{
			Walls[index] = new WallSegment( Vector2.Zero, Vector2.Zero, Vector2.Zero, WallKind.Panel );
			panelHits.Remove( index );
			panelKicks.Remove( index );
			GlassBroken++;
			return GlassHit.Shatter;
		}

		return GlassHit.Crack;
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

	public void Eject( ref Vector2 flat, float radius, bool includeBoss = false )
		=> Eject( ref flat, radius, includeBoss, flat );

	public void Eject( ref Vector2 flat, float radius, bool includeBoss, Vector2 preferFrom )
	{
		for ( var pass = 0; pass < 6; pass++ )
		{
			var pushed = false;

			for ( var i = 0; i < Walls.Count; i++ )
			{
				var wall = Walls[i];
				if ( !BlocksWalk( wall.Kind, includeBoss ) )
					continue;

				if ( !OverlapObb( flat, radius, wall, preferFrom, out var push ) )
					continue;

				flat += push;
				pushed = true;
			}

			if ( !pushed )
				return;
		}
	}

	public bool EjectSoft( ref Vector2 flat, float radius, bool includeBoss, Vector2 preferFrom, float maxStep )
	{
		var settled = flat;
		Eject( ref settled, radius, includeBoss, preferFrom );

		var shift = settled - flat;
		var depth = shift.Length;
		if ( depth < 0.01f )
			return false;

		if ( maxStep <= 0f || depth <= maxStep || depth > radius )
		{
			flat = settled;
			return true;
		}

		flat += shift.Normal * maxStep;
		return true;
	}

	public void MoveBody( ref Vector2 flat, Vector2 delta, float radius, bool includeBoss )
		=> MoveBody( ref flat, delta, radius, includeBoss, 0f );

	public void MoveBody( ref Vector2 flat, Vector2 delta, float radius, bool includeBoss, float maxCorrection )
	{
		var from = flat;
		Settle( ref flat, radius, includeBoss, from, maxCorrection );

		var remain = delta.Length;
		if ( remain < 0.001f )
			return;

		var dir = delta.Normal;

		for ( var pass = 0; pass < 2; pass++ )
		{
			if ( remain < 0.001f )
				break;

			if ( !TraceDisk( flat, dir, remain + 2f, radius, includeBoss, out var hit ) )
			{
				flat += dir * remain;
				break;
			}

			var travel = MathF.Min( remain, MathF.Max( 0f, hit.Distance - 0.35f ) );
			flat += dir * travel;
			remain -= travel;
			if ( remain < 0.001f )
				break;

			var tangent = new Vector2( -hit.Normal.y, hit.Normal.x );
			if ( Dot( tangent, delta ) < 0f )
				tangent = -tangent;

			dir = tangent;
		}

		Settle( ref flat, radius, includeBoss, from, maxCorrection );
	}

	void Settle( ref Vector2 flat, float radius, bool includeBoss, Vector2 preferFrom, float maxStep )
	{
		if ( maxStep <= 0f )
			Eject( ref flat, radius, includeBoss, preferFrom );
		else
			EjectSoft( ref flat, radius, includeBoss, preferFrom, maxStep );
	}

	public bool TraceRay( Vector2 origin, Vector2 direction, float maxDistance, out ArenaHit hit )
	{
		hit = default;

		var closest = maxDistance;
		var found = false;

		for ( var i = 0; i < Walls.Count; i++ )
		{
			var wall = Walls[i];
			if ( wall.Length < 1f )
				continue;

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

	public bool SightBlocked( Vector2 from, Vector2 to, out ArenaHit hit )
	{
		hit = default;
		var delta = to - from;
		var dist = delta.Length;
		if ( dist < 1f )
			return false;

		if ( !TraceRay( from, delta.Normal, dist, out hit ) )
			return false;

		return hit.Distance + 12f < dist;
	}

	public float Clearance( Vector2 origin, Vector2 direction, float radius, float look, bool includeBoss = true )
	{
		if ( direction.Length < 0.01f || look <= 0f )
			return look;

		return TraceDisk( origin, direction, look, radius, includeBoss, out var hit )
			? MathF.Max( 0f, hit.Distance )
			: look;
	}

	public void WallEnds( ArenaHit hit, float radius, out Vector2 a, out Vector2 b )
	{
		if ( hit.WallIndex < 0 || hit.WallIndex >= Walls.Count || Walls[hit.WallIndex].Length < 1f )
		{
			var across = new Vector2( -hit.Normal.y, hit.Normal.x ) * (radius * 2f + 60f);
			a = BypassPoint( hit.Position + across, radius );
			b = BypassPoint( hit.Position - across, radius );
			return;
		}

		var wall = Walls[hit.WallIndex];
		WallBox( wall, radius, out var center, out var axisX, out _, out var hx, out _ );
		var lead = hx + radius * 0.4f + 20f;
		a = BypassPoint( center - axisX * lead, radius );
		b = BypassPoint( center + axisX * lead, radius );
	}

	Vector2 BypassPoint( Vector2 point, float radius )
	{
		var spot = ClampPlay( point );
		Eject( ref spot, radius, true, spot );
		return spot;
	}

	public bool TraceDisk( Vector2 origin, Vector2 direction, float maxDistance, float radius, out ArenaHit hit )
		=> TraceDisk( origin, direction, maxDistance, radius, true, out hit );

	public bool TraceDisk( Vector2 origin, Vector2 direction, float maxDistance, float radius, bool includeBoss, out ArenaHit hit )
	{
		hit = default;
		if ( direction.Length < 0.01f || maxDistance <= 0f )
			return false;

		direction = direction.Normal;
		var best = maxDistance;
		var found = false;

		for ( var i = 0; i < Walls.Count; i++ )
		{
			var wall = Walls[i];
			if ( wall.Length < 1f || !BlocksWalk( wall.Kind, includeBoss ) )
				continue;

			WallBox( wall, radius, out var center, out var axisX, out var axisY, out var hx, out var hy );
			if ( !RayObb( origin, direction, best, center, axisX, axisY, hx, hy, out var travel, out var normal ) )
				continue;

			best = MathF.Max( 0f, travel );
			found = true;
			hit = new ArenaHit
			{
				Distance = best,
				Position = origin + direction * best,
				Normal = normal,
				WallIndex = i,
				Kind = wall.Kind
			};
		}

		return found;
	}

	bool BlocksWalk( WallKind kind, bool includeBoss )
	{
		if ( kind == WallKind.Boundary )
			return false;

		if ( kind == WallKind.Core && !CoreSolid )
			return false;

		if ( kind == WallKind.Boss && !includeBoss )
			return false;

		return true;
	}

	static float WallThickness( WallKind kind ) => kind switch
	{
		WallKind.Core => 36f,
		WallKind.Boundary => 32f,
		WallKind.Spin => 5f,
		_ => 26f
	};

	static void WallBox( WallSegment wall, float bodyRadius, out Vector2 center, out Vector2 axisX, out Vector2 axisY, out float hx, out float hy )
	{
		var thick = WallThickness( wall.Kind );
		center = wall.Center;
		axisX = wall.Direction;
		axisY = wall.Normal;
		hx = wall.Length * 0.5f + thick * 0.5f + bodyRadius + 2f;
		hy = thick * 0.5f + bodyRadius + 2f;
	}

	static bool OverlapObb( Vector2 point, float bodyRadius, WallSegment wall, Vector2 preferFrom, out Vector2 push )
	{
		push = Vector2.Zero;
		if ( wall.Length < 1f )
			return false;

		WallBox( wall, bodyRadius, out var center, out var axisX, out var axisY, out var hx, out var hy );
		var to = point - center;
		var lx = Dot( to, axisX );
		var ly = Dot( to, axisY );
		var depthX = hx - MathF.Abs( lx );
		var depthY = hy - MathF.Abs( ly );
		if ( depthX <= 0f || depthY <= 0f )
			return false;

		var from = preferFrom - center;
		var fromX = Dot( from, axisX );
		var fromY = Dot( from, axisY );
		var cameAcross = MathF.Abs( fromY ) >= hy;
		var camePastEnd = MathF.Abs( fromX ) >= hx;
		var costX = depthX + (camePastEnd ? 0f : 30f);
		var costY = depthY + (cameAcross ? 0f : 30f);

		if ( costX < costY )
		{
			var side = ExitSide( lx, camePastEnd ? fromX : 0f );
			push = axisX * (side * (depthX + SkinWidth));
			return true;
		}

		var across = ExitSide( ly, cameAcross ? fromY : 0f );
		push = axisY * (across * (depthY + SkinWidth));
		return true;
	}

	static float ExitSide( float local, float prefer )
	{
		if ( MathF.Abs( prefer ) > 0.01f )
			return prefer >= 0f ? 1f : -1f;

		return local >= 0f ? 1f : -1f;
	}

	public static bool SweepBox( Vector2 origin, Vector2 direction, float maxDistance, Vector2 center, Vector2 axisX, Vector2 axisY, float hx, float hy, out float travel, out Vector2 normal )
		=> RayObb( origin, direction, maxDistance, center, axisX, axisY, hx, hy, out travel, out normal );

	static bool RayObb( Vector2 origin, Vector2 dir, float maxDistance, Vector2 center, Vector2 axisX, Vector2 axisY, float hx, float hy, out float travel, out Vector2 normal )
	{
		travel = 0f;
		normal = axisY;
		var to = origin - center;
		var ox = Dot( to, axisX );
		var oy = Dot( to, axisY );

		if ( MathF.Abs( ox ) < hx - 1f && MathF.Abs( oy ) < hy - 1f )
		{
			var px = hx - MathF.Abs( ox );
			var py = hy - MathF.Abs( oy );
			normal = px < py
				? axisX * (ox >= 0f ? 1f : -1f)
				: axisY * (oy >= 0f ? 1f : -1f);
			return true;
		}

		var dx = Dot( dir, axisX );
		var dy = Dot( dir, axisY );
		var tmin = 0f;
		var tmax = maxDistance;
		var nmin = axisY;

		if ( !Slab( ox, dx, hx, axisX, ref tmin, ref tmax, ref nmin ) )
			return false;

		if ( !Slab( oy, dy, hy, axisY, ref tmin, ref tmax, ref nmin ) )
			return false;

		if ( tmax < 0f || tmin > maxDistance )
			return false;

		travel = tmin >= 0f ? tmin : 0f;
		if ( travel > maxDistance )
			return false;

		normal = nmin;
		if ( Dot( normal, dir ) > 0f )
			normal = -normal;

		return true;
	}

	static bool Slab( float origin, float speed, float half, Vector2 axis, ref float tmin, ref float tmax, ref Vector2 nmin )
	{
		if ( MathF.Abs( speed ) < 0.000001f )
			return MathF.Abs( origin ) <= half;

		var inv = 1f / speed;
		var t1 = (-half - origin) * inv;
		var t2 = (half - origin) * inv;
		var n1 = -axis;
		var n2 = axis;
		if ( t1 > t2 )
		{
			(t1, t2) = (t2, t1);
			(n1, n2) = (n2, n1);
		}

		if ( t1 > tmin )
		{
			tmin = t1;
			nmin = n1;
		}

		if ( t2 < tmax )
			tmax = t2;

		return tmin <= tmax;
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

	public List<Vector2> PredictPath( Vector2 origin, Vector2 direction, float radius, float firstLegLimit, float bounceLegLength, int extraBounces = 1, float spinSpeed = 0f, float flightSpeed = 950f )
	{
		var path = new List<Vector2> { origin };
		var dir = direction.Length > 0.01f ? direction.Normal : Vector2.Right;
		var pos = origin;
		var first = true;
		var bounces = Math.Max( 0, extraBounces );

		if ( spinSpeed <= 1f )
		{
			for ( var i = 0; i <= bounces; i++ )
			{
				var limit = first ? firstLegLimit : bounceLegLength;
				if ( limit <= 1f )
					break;

				if ( !TraceRay( pos, dir, limit + radius, out var hit ) )
				{
					path.Add( pos + dir * limit );
					return path;
				}

				var contact = hit.Position + hit.Normal * radius;
				path.Add( contact );
				pos = contact;
				dir = Reflect( dir, hit.Normal ).Normal;
				first = false;
			}

			return path;
		}

		var left = firstLegLimit;
		const float step = 22f;
		while ( left > 1f && path.Count < 72 && bounces >= 0 )
		{
			var heading = DriftHeading( pos, dir, flightSpeed, spinSpeed );
			var chunk = MathF.Min( step, left );
			if ( TraceRay( pos, heading, chunk + radius, out var hit ) )
			{
				var contact = hit.Position + hit.Normal * radius;
				path.Add( contact );
				pos = contact;
				dir = Reflect( heading, hit.Normal ).Normal;
				bounces--;
				left = bounceLegLength;
				continue;
			}

			pos += heading * chunk;
			left -= chunk;
			if ( (pos - path[^1]).LengthSquared > 1600f )
				path.Add( pos );
		}

		if ( (pos - path[^1]).LengthSquared > 1f )
			path.Add( pos );

		return path;
	}

	public int AddSpinner( Vector2 center, float length, float angle )
	{
		var dir = FromAngle( angle );
		var half = MathF.Max( 40f, length ) * 0.5f;
		var facing = new Vector2( -dir.y, dir.x );
		Walls.Add( new WallSegment( center - dir * half, center + dir * half, facing, WallKind.Spin ) );
		var index = Walls.Count - 1;
		spinners.Add( new Spinner { Index = index, Center = center, Length = half * 2f, Angle = angle } );
		return index;
	}

	public void PushSpinner( int index, Vector2 hitPos, Vector2 incoming )
	{
		if ( incoming.Length < 0.01f )
			return;

		for ( var i = 0; i < spinners.Count; i++ )
		{
			var spin = spinners[i];
			if ( spin.Index != index || spin.Index < 0 || spin.Index >= Walls.Count )
				continue;

			var wall = Walls[spin.Index];
			var along = wall.Direction;
			var face = new Vector2( -along.y, along.x );
			var side = Dot( hitPos - wall.Center, along );
			var push = Dot( incoming.Normal, face );
			spin.Speed = Math.Clamp( spin.Speed + side * push * 1.5f, -240f, 240f );
			spinners[i] = spin;
			return;
		}
	}

	public void AdvanceSpinners( float dt )
	{
		if ( dt <= 0f || spinners.Count == 0 )
			return;

		var drag = 36f * dt;
		for ( var i = 0; i < spinners.Count; i++ )
		{
			var spin = spinners[i];
			if ( spin.Index < 0 || spin.Index >= Walls.Count )
				continue;

			if ( MathF.Abs( spin.Speed ) <= drag )
				spin.Speed = 0f;
			else
				spin.Speed -= MathF.Sign( spin.Speed ) * drag;

			if ( MathF.Abs( spin.Speed ) < 0.01f )
			{
				spinners[i] = spin;
				continue;
			}

			spin.Angle += MathX.DegreeToRadian( spin.Speed ) * dt;
			var dir = FromAngle( spin.Angle );
			var half = spin.Length * 0.5f;
			var facing = new Vector2( -dir.y, dir.x );
			Walls[spin.Index] = new WallSegment( spin.Center - dir * half, spin.Center + dir * half, facing, WallKind.Spin );
			spinners[i] = spin;
		}
	}

	public void CollectSpinners( List<int> indices )
	{
		indices.Clear();
		foreach ( var spin in spinners )
			indices.Add( spin.Index );
	}

	public void KickPanel( int index, Vector2 hitPos, Vector2 hitNormal, float extraDegrees = 0f, bool allowSecond = false )
	{
		if ( index < 0 || index >= Walls.Count )
			return;

		var wall = Walls[index];
		if ( wall.Kind != WallKind.Panel )
			return;

		panelKicks.TryGetValue( index, out var kicks );
		kicks++;
		panelKicks[index] = kicks;

		var extra = 0f;
		if ( kicks == 1 || (allowSecond && kicks == 2) )
			extra = extraDegrees;

		var n = hitNormal.Length > 0.01f ? hitNormal.Normal : wall.Normal;
		var look = -n;
		var right = new Vector2( look.y, -look.x );
		var side = Dot( hitPos - wall.Center, right );
		var yaw = MathX.DegreeToRadian( (15f + extra) * (side >= 0f ? 1f : -1f) );
		var cos = MathF.Cos( yaw );
		var sin = MathF.Sin( yaw );
		var c = wall.Center;
		var a = Turn( wall.A - c, cos, sin ) + c;
		var b = Turn( wall.B - c, cos, sin ) + c;
		a = ClampPlay( a );
		b = ClampPlay( b );

		if ( (b - a).Length < 90f )
			return;

		var span = b - a;
		var facing = new Vector2( -span.y, span.x ).Normal;
		Walls[index] = new WallSegment( a, b, facing, WallKind.Panel );
	}

	static Vector2 Turn( Vector2 p, float cos, float sin )
		=> new Vector2( p.x * cos - p.y * sin, p.x * sin + p.y * cos );

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

	struct Spinner
	{
		public int Index;
		public Vector2 Center;
		public float Length;
		public float Angle;
		public float Speed;
	}
}
