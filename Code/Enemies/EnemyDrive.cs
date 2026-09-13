namespace LoopedLoaded;

public sealed class EnemyDrive
{
	public Vector2 Velocity { get; private set; }
	public Vector2 Heading { get; private set; } = Vector2.Right;
	public Vector2 Look { get; private set; } = Vector2.Right;
	public bool Squeezed { get; private set; }

	const int FanCount = 6;
	const float FanStep = 0.26f;
	const float TurnRate = 9f;
	const float LookRate = 11f;
	const float Accel = 1600f;
	const float Brake = 2800f;
	const float SideHold = 0.6f;
	const float ProbeSpan = 0.7f;

	float side;
	float sideUntil;
	float probeAt;
	Vector2 probeMark;
	float pace;

	public void Reset( Vector2 heading )
	{
		Heading = heading.Length > 0.01f ? heading.Normal : Vector2.Right;
		Look = Heading;
		Velocity = Vector2.Zero;
		Squeezed = false;
		side = 0f;
		sideUntil = 0f;
		probeAt = 0f;
		probeMark = Vector2.Zero;
		pace = 0f;
	}

	public void Shift( float dt )
	{
		if ( sideUntil > 0f )
			sideUntil += dt;

		if ( probeAt > 0f )
			probeAt += dt;
	}

	public void FlipSide( Vector2 flat )
	{
		side = side >= 0f ? -1f : 1f;
		sideUntil = Time.Now + SideHold * 2f;
		probeAt = Time.Now + ProbeSpan;
		probeMark = flat;
	}

	public Vector2 Step( ArenaGeometry geo, Vector2 flat, Vector2 goal, float radius, float speed, float minRadius, float maxRadius )
	{
		var dt = Time.Delta;
		if ( dt <= 0.0001f )
			return flat;

		var toGoal = goal - flat;
		var span = toGoal.Length;
		var goalDir = span > 1f ? toGoal.Normal : Heading;
		var look = Math.Clamp( radius * 2.2f + speed * 0.45f, 150f, 380f );
		var reach = MathF.Min( look, MathF.Max( radius + 30f, span ) );
		var want = Avoid( geo, flat, goalDir, radius, reach );
		want = Band( want, flat, radius, minRadius, maxRadius );

		Heading = Turn( Heading, want, TurnRate * dt );

		var clearance = geo.Clearance( flat, Heading, radius, reach );
		var target = speed
			* Math.Clamp( (clearance - radius * 0.3f) / MathF.Max( 1f, reach * 0.6f ), 0.2f, 1f )
			* MathX.Lerp( 0.5f, 1f, Math.Clamp( ArenaGeometry.Dot( Heading, want ), 0f, 1f ) )
			* Math.Clamp( span / MathF.Max( 48f, radius ), 0.1f, 1f );

		pace = target > pace
			? MathF.Min( target, pace + Accel * dt )
			: MathF.Max( target, pace - Brake * dt );

		var moved = flat;
		geo.MoveBody( ref moved, Heading * (pace * dt), radius, true, 520f * dt );
		moved = Ring( moved, minRadius, maxRadius, dt );

		var travel = (moved - flat) / dt;
		Squeezed = pace > 40f && travel.Length < pace * 0.25f;
		Velocity += (travel - Velocity) * (1f - MathF.Exp( -14f * dt ));
		Look = Turn( Look, Velocity.Length > 16f ? Velocity : Heading, LookRate * dt );
		Track( moved, radius );
		return moved;
	}

	void Track( Vector2 flat, float radius )
	{
		if ( probeAt <= 0f )
		{
			probeAt = Time.Now + ProbeSpan;
			probeMark = flat;
			return;
		}

		if ( Time.Now < probeAt )
			return;

		var travelled = (flat - probeMark).Length;
		probeAt = Time.Now + ProbeSpan;
		probeMark = flat;

		if ( side != 0f && travelled < radius * 0.6f )
			FlipSide( flat );
	}

	Vector2 Avoid( ArenaGeometry geo, Vector2 flat, Vector2 goalDir, float radius, float reach )
	{
		var straight = geo.Clearance( flat, goalDir, radius, reach );
		if ( straight >= reach - 1f )
		{
			if ( Time.Now >= sideUntil )
				side = 0f;

			return goalDir;
		}

		if ( side == 0f )
			side = PickSide( geo, flat, goalDir, radius, reach );

		sideUntil = MathF.Max( sideUntil, Time.Now + SideHold );

		var heading = ArenaGeometry.ToAngle( goalDir );
		var best = Vector2.Zero;
		var bestClear = straight;

		for ( var i = 1; i <= FanCount; i++ )
		{
			var dir = ArenaGeometry.FromAngle( heading + side * i * FanStep );
			var clear = geo.Clearance( flat, dir, radius, reach );
			if ( clear >= reach - 1f )
				return dir;

			if ( clear <= bestClear + 6f )
				continue;

			bestClear = clear;
			best = dir;
		}

		if ( best.Length > 0.01f )
			return best;

		return Slide( geo, flat, goalDir, radius, reach );
	}

	float PickSide( ArenaGeometry geo, Vector2 flat, Vector2 goalDir, float radius, float reach )
	{
		var heading = ArenaGeometry.ToAngle( goalDir );
		var left = 0f;
		var right = 0f;

		for ( var i = 2; i <= FanCount; i += 2 )
		{
			left += geo.Clearance( flat, ArenaGeometry.FromAngle( heading + i * FanStep ), radius, reach );
			right += geo.Clearance( flat, ArenaGeometry.FromAngle( heading - i * FanStep ), radius, reach );
		}

		if ( MathF.Abs( left - right ) < reach * 0.12f )
			return ArenaGeometry.Cross( goalDir, Heading ) >= 0f ? 1f : -1f;

		return left >= right ? 1f : -1f;
	}

	Vector2 Slide( ArenaGeometry geo, Vector2 flat, Vector2 goalDir, float radius, float reach )
	{
		if ( !geo.TraceDisk( flat, goalDir, reach, radius, true, out var hit ) )
			return goalDir;

		var tangent = new Vector2( -hit.Normal.y, hit.Normal.x );
		if ( ArenaGeometry.Cross( goalDir, tangent ) * side < 0f )
			tangent = -tangent;

		var away = tangent + hit.Normal * 0.3f;
		return away.Length > 0.01f ? away.Normal : goalDir;
	}

	static Vector2 Band( Vector2 want, Vector2 flat, float radius, float minRadius, float maxRadius )
	{
		var length = flat.Length;
		if ( length < 1f )
			return Vector2.Right;

		var margin = MathF.Max( 40f, radius );
		var push = 0f;

		if ( length < minRadius + margin )
			push = Math.Clamp( (minRadius + margin - length) / margin, 0f, 1.3f );
		else if ( length > maxRadius - margin )
			push = -Math.Clamp( (length - (maxRadius - margin)) / margin, 0f, 1.3f );

		if ( MathF.Abs( push ) < 0.01f )
			return want;

		var mixed = want + flat.Normal * push;
		return mixed.Length > 0.01f ? mixed.Normal : want;
	}

	static Vector2 Ring( Vector2 flat, float minRadius, float maxRadius, float dt )
	{
		var length = flat.Length;
		if ( length < 1f )
			return Vector2.Right * minRadius;

		var target = Math.Clamp( length, minRadius, maxRadius );
		if ( MathF.Abs( target - length ) < 0.05f )
			return flat;

		return flat.Normal * MathX.Lerp( length, target, 1f - MathF.Exp( -14f * dt ) );
	}

	static Vector2 Turn( Vector2 from, Vector2 to, float maxTurn )
	{
		if ( to.Length < 0.01f )
			return from;

		to = to.Normal;
		if ( from.Length < 0.01f )
			return to;

		from = from.Normal;
		var angle = MathF.Acos( Math.Clamp( ArenaGeometry.Dot( from, to ), -1f, 1f ) );
		if ( angle <= maxTurn )
			return to;

		var sign = ArenaGeometry.Cross( from, to ) >= 0f ? 1f : -1f;
		return ArenaGeometry.FromAngle( ArenaGeometry.ToAngle( from ) + sign * maxTurn );
	}
}
