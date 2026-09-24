namespace LoopedLoaded;

public sealed class LaserBeam : Component
{
	static readonly Color ChargeFull = new Color( 0.15f, 0.90f, 0.85f );
	static readonly Color ChargeEmpty = new Color( 0.95f, 0.22f, 0.16f );

	GameLoop loop;
	GunRecipe recipe;
	Vector2 origin;
	readonly List<PolyLine> lines = new();
	readonly List<(Vector2 A, Vector2 B)> rays = new();
	readonly List<Latch> latches = new();
	readonly Dictionary<(int Bolt, Enemy Body), int> sear = new();
	readonly HashSet<Enemy> lastMains = new();
	int ticksLeft;
	int tickBudget;
	float nextTick;
	bool steer = true;
	Vector2 aimDir = Vector2.Right;
	PolyLine splashRing;
	Vector2 splashAt;
	bool splashLive;

	public int TicksLeft => ticksLeft;
	public int TickBudget => tickBudget;
	public bool Spent => tickBudget > 0 && ticksLeft <= 0;

	public void Arm( GameLoop host, GunRecipe gun )
	{
		loop = host;
		recipe = gun;
		tickBudget = Math.Max( 1, gun.BeamTicks );
		ticksLeft = tickBudget;
		nextTick = Time.Now + MathF.Max( 0.05f, gun.BeamTick );
		steer = true;
		sear.Clear();
		lastMains.Clear();
	}

	public void FreezeAim() => steer = false;

	public void Aim( Vector2 muzzle, Vector2 dir, GunRecipe gun )
	{
		recipe = gun;
		origin = muzzle;
		aimDir = dir.Normal;
		Build( aimDir );
	}

	public bool Near( Vector2 flat, float radius )
	{
		foreach ( var ray in rays )
		{
			if ( RoundCombat.PointSegment( flat, ray.A, ray.B ) <= radius + recipe.BeamWidth )
				return true;
		}

		return false;
	}

	protected override void OnUpdate()
	{
		if ( !loop.IsValid() || loop.IsFrozen || !loop.Aim.IsValid() )
			return;

		if ( steer )
			Aim( loop.Aim.Muzzle, loop.Aim.Direction, recipe );
		else
			Build( aimDir );
		Strike();
	}

	void Build( Vector2 dir )
	{
		rays.Clear();
		latches.Clear();
		var count = Math.Max( 1, recipe.Count );
		var cone = recipe.Cone;
		var geometry = loop.Geometry;
		var rank = Math.Max( 1, recipe.BeamRank );
		var seed = (int)(Time.Now * (9f + rank * 7f));
		var bolts = new List<List<Vector2>>();
		var range = recipe.BeamRange;
		if ( recipe.PointAim && loop.Aim.IsValid() )
			range = MathF.Min( range, (loop.Aim.Cursor - origin).Length );
		var width = MathF.Max( 8f, recipe.BeamWidth );
		splashLive = false;

		for ( var i = 0; i < count; i++ )
		{
			var yaw = ShotSpread.Yaw( i, count, cone );
			var heading = ShotSpread.Turn( dir, yaw );
			var end = origin + heading * range;
			var blocked = false;
			var wall = default( ArenaHit );
			if ( geometry is not null && geometry.TraceRay( origin, heading, range, out wall ) )
			{
				end = wall.Position;
				blocked = true;
			}

			var storm = LightningPath.Storm( origin, end, rank, seed + i * 31, recipe.BeamFork ? 1 : 0 );
			Enemy mainBody = null;
			var mainAt = end;
			var hitMain = false;
			if ( recipe.BeamFork && storm.Count > 0 )
			{
				if ( TouchOne( storm[0], width, out var body, out var touch ) )
				{
					CutOne( storm[0], heading, touch );
					mainBody = body;
					mainAt = touch;
					end = touch;
					hitMain = true;
					latches.Add( new Latch { Body = body, At = touch, Bolt = i } );
				}

				for ( var f = 1; f < storm.Count; f++ )
				{
					if ( !TouchOne( storm[f], width, out var forkBody, out var forkAt ) )
						continue;

					CutOne( storm[f], heading, forkAt );
					if ( forkBody == mainBody )
						continue;

					latches.Add( new Latch { Body = forkBody, At = forkAt, Bolt = 100 + i * 8 + f } );
				}
			}
			else if ( FirstTouch( loop, storm, origin, width, out var body, out var touch ) )
			{
				CutPast( storm, origin, heading, touch );
				mainBody = body;
				mainAt = touch;
				end = touch;
				hitMain = true;
				latches.Add( new Latch { Body = body, At = touch, Bolt = i } );
			}

			if ( !hitMain && blocked && loop.Arena.IsValid() )
			{
				if ( wall.Kind == WallKind.Panel )
					loop.Arena.StrikeBoard( wall.WallIndex, wall.Position, wall.Normal, 0f, false );
				else if ( wall.Kind == WallKind.Spin )
					loop.Arena.PushSpinner( wall.WallIndex, wall.Position, heading );
			}

			if ( hitMain && recipe.BeamArc > 1f )
				Jump( mainAt, mainBody, i, seed, bolts );

			if ( i == count / 2 )
			{
				splashAt = end;
				splashLive = recipe.Splash > 1f;
			}

			foreach ( var bolt in storm )
				bolts.Add( bolt );
		}

		foreach ( var bolt in bolts )
		{
			for ( var i = 1; i < bolt.Count; i++ )
				rays.Add( (bolt[i - 1], bolt[i]) );
		}

		Draw( bolts, rank );
		PaintSplash();
	}

	void Strike()
	{
		if ( ticksLeft <= 0 || Time.Now < nextTick )
			return;

		var hit = Math.Max( 1, recipe.BeamHit );
		var staged = new List<(int Bolt, Enemy Body, int Next)>();
		var mains = new List<Enemy>();
		Enemy nearest = null;
		var nearestDist = float.MaxValue;
		Vector2 nearestAt = default;

		foreach ( var latch in latches )
		{
			var enemy = latch.Body;
			if ( !enemy.IsValid() || !enemy.Alive )
				continue;

			var incoming = latch.At - origin;
			if ( incoming.Length < 1f )
				incoming = aimDir;
			else
				incoming = incoming.Normal;

			if ( enemy.BlocksFrom( incoming, null, recipe.Pierce > 0, latch.At, recipe.BeamShunt ) )
			{
				if ( Locations.IsBoss( enemy.Kind ) )
					loop.NoteArmor();
				continue;
			}

			var dist = (latch.At - origin).Length;
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				nearest = enemy;
				nearestAt = latch.At;
			}

			var key = (latch.Bolt, enemy);
			sear.TryGetValue( key, out var prior );
			var damage = hit;
			if ( recipe.BeamSear && prior > 0 )
				damage += 1;

			enemy.Damage( damage, 0f, 1f );
			if ( recipe.StickTime > 0.01f )
				PinLinger.Hang( enemy, 1, recipe.StickTime );

			staged.Add( (latch.Bolt, enemy, prior + 1) );
			if ( latch.Bolt < 100 && !mains.Contains( enemy ) )
				mains.Add( enemy );
		}

		var kiln = recipe.BeamKiln > 0f && recipe.BeamKiln < 0.999f && MainsMatch( mains );
		sear.Clear();
		foreach ( var step in staged )
			sear[(step.Bolt, step.Body)] = step.Next;

		lastMains.Clear();
		foreach ( var body in mains )
			lastMains.Add( body );

		ticksLeft--;
		var gap = MathF.Max( 0.05f, recipe.BeamTick );
		if ( kiln )
			gap *= recipe.BeamKiln;
		nextTick = Time.Now + gap;

		if ( recipe.Splash > 1f && nearest.IsValid() )
			RoundCombat.Blast( loop, nearestAt, recipe.Splash, Math.Max( 1, recipe.SplashDamage ), null, ShotColors.Player, recipe.FriendlySplash );
	}

	bool MainsMatch( List<Enemy> mains )
	{
		if ( mains.Count == 0 || mains.Count != lastMains.Count )
			return false;

		foreach ( var body in mains )
		{
			if ( !lastMains.Contains( body ) )
				return false;
		}

		return true;
	}

	bool TouchOne( List<Vector2> bolt, float width, out Enemy enemy, out Vector2 at )
	{
		var wrap = new List<List<Vector2>> { bolt };
		return FirstTouch( loop, wrap, origin, width, out enemy, out at );
	}

	void CutOne( List<Vector2> bolt, Vector2 heading, Vector2 stop )
	{
		var wrap = new List<List<Vector2>> { bolt };
		CutPast( wrap, origin, heading, stop );
		if ( wrap.Count == 0 )
			bolt.Clear();
	}

	void Jump( Vector2 from, Enemy main, int bolt, int seed, List<List<Vector2>> drawn )
	{
		Enemy pick = null;
		var best = recipe.BeamArc;
		foreach ( var candidate in loop.Enemies )
		{
			if ( !candidate.IsValid() || !candidate.Alive || candidate == main )
				continue;

			var dist = (candidate.Flat - from).Length;
			if ( dist > best )
				continue;

			best = dist;
			pick = candidate;
		}

		if ( !pick.IsValid() )
			return;

		latches.Add( new Latch { Body = pick, At = pick.Flat, Bolt = bolt } );
		drawn.Add( LightningPath.Jag( from, pick.Flat, seed + 90 + bolt, 4, 24f ) );
	}

	void Draw( List<List<Vector2>> bolts, int rank )
	{
		var wave = MathF.Abs( MathF.Sin( Time.Now * (14f + rank * 5f) ) );
		var pulse = 0.2f + 0.8f * wave;
		var charge = tickBudget <= 1 ? 0f : (tickBudget - Math.Max( ticksLeft, 1 )) / (float)(tickBudget - 1);
		var hue = Color.Lerp( ChargeFull, ChargeEmpty, charge );
		var core = hue * pulse;
		var glow = hue * (0.15f + pulse * 0.85f);
		var width = (rank >= 3 ? 9f : rank >= 2 ? 7f : 5f) * (0.45f + wave);

		while ( lines.Count < bolts.Count )
		{
			var go = Scene.CreateObject();
			go.Name = "Bolt Ray";
			go.Parent = GameObject;
			var line = go.AddComponent<PolyLine>();
			line.HeadTint = core;
			line.TailTint = glow;
			line.HeadWidth = width;
			line.TailWidth = 2.5f;
			line.Apply();
			lines.Add( line );
		}

		for ( var i = 0; i < lines.Count; i++ )
		{
			if ( i >= bolts.Count )
			{
				lines[i].Clear();
				continue;
			}

			var world = new List<Vector3>( bolts[i].Count );
			foreach ( var point in bolts[i] )
				world.Add( loop.Geometry.ToPlayWorld( point ) );

			lines[i].HeadTint = core;
			lines[i].TailTint = glow;
			lines[i].HeadWidth = width;
			lines[i].TailWidth = (rank >= 2 ? 3f : 2.2f) * (0.45f + wave);
			lines[i].Apply();
			lines[i].SetPoints( world );
		}
	}

	void PaintSplash()
	{
		if ( !splashLive || loop?.Geometry is null )
		{
			splashRing?.Clear();
			return;
		}

		if ( !splashRing.IsValid() )
		{
			var go = Scene.CreateObject();
			go.Name = "Splash";
			go.Parent = GameObject;
			splashRing = go.AddComponent<PolyLine>();
		}

		var tint = RoundCombat.RingTint( recipe.FriendlySplash );
		splashRing.HeadTint = tint;
		splashRing.TailTint = tint * 0.35f;
		splashRing.HeadWidth = 3.5f;
		splashRing.TailWidth = 3.5f;
		splashRing.Apply();
		splashRing.SetPoints( RoundCombat.Circle( loop.Geometry, splashAt, recipe.Splash ) );
	}

	public static Vector2 Reach( GameLoop loop, ArenaGeometry geometry, Vector2 origin, Vector2 heading, float range, float width )
	{
		if ( range <= 1f || heading.Length <= 0.01f )
			return origin;

		var dir = heading.Normal;
		var end = origin + dir * range;
		if ( geometry is not null && geometry.TraceRay( origin, dir, range, out var hit ) )
			end = hit.Position;

		return FirstBody( loop, origin, end, width, out _ );
	}

	static Vector2 FirstBody( GameLoop loop, Vector2 from, Vector2 to, float width, out Enemy enemy )
	{
		enemy = null;
		var span = to - from;
		var length = span.Length;
		if ( !loop.IsValid() || length < 1f )
			return to;

		var dir = span / length;
		var best = length;
		var at = to;
		foreach ( var candidate in loop.Enemies )
		{
			if ( !candidate.IsValid() || !candidate.Alive )
				continue;

			var reach = width + candidate.Radius;
			var rel = candidate.Flat - from;
			var along = ArenaGeometry.Dot( rel, dir );
			var perp = (rel - dir * along).Length;
			if ( perp > reach )
				continue;

			var offset = MathF.Sqrt( MathF.Max( 0f, reach * reach - perp * perp ) );
			var entry = along - offset;
			if ( entry < 0f )
			{
				if ( along + offset < 0f )
					continue;
				entry = 0f;
			}

			if ( entry > length || entry >= best )
				continue;

			best = entry;
			enemy = candidate;
			at = from + dir * entry;
		}

		return enemy.IsValid() ? at : to;
	}

	static bool FirstTouch( GameLoop loop, List<List<Vector2>> bolts, Vector2 origin, float width, out Enemy enemy, out Vector2 at )
	{
		enemy = null;
		at = default;
		if ( !loop.IsValid() )
			return false;

		var best = float.MaxValue;
		foreach ( var bolt in bolts )
		{
			for ( var i = 1; i < bolt.Count; i++ )
			{
				var a = bolt[i - 1];
				var b = bolt[i];
				var span = b - a;
				var length = span.Length;
				if ( length < 0.001f )
					continue;

				var dir = span / length;
				foreach ( var candidate in loop.Enemies )
				{
					if ( !candidate.IsValid() || !candidate.Alive )
						continue;

					var reach = width + candidate.Radius;
					var rel = candidate.Flat - a;
					var along = ArenaGeometry.Dot( rel, dir );
					var perp = (rel - dir * along).Length;
					if ( perp > reach )
						continue;

					var offset = MathF.Sqrt( MathF.Max( 0f, reach * reach - perp * perp ) );
					var entry = along - offset;
					if ( entry < 0f )
					{
						if ( along + offset < 0f )
							continue;
						entry = 0f;
					}
					else if ( entry > length )
						continue;

					var touch = a + dir * entry;
					var dist = (touch - origin).Length;
					if ( dist >= best )
						continue;

					best = dist;
					enemy = candidate;
					at = touch;
				}
			}
		}

		return enemy.IsValid();
	}

	struct Latch
	{
		public Enemy Body;
		public Vector2 At;
		public int Bolt;
	}

	static void CutPast( List<List<Vector2>> bolts, Vector2 origin, Vector2 heading, Vector2 stop )
	{
		var limit = ArenaGeometry.Dot( stop - origin, heading );
		for ( var b = bolts.Count - 1; b >= 0; b-- )
		{
			var bolt = bolts[b];
			if ( bolt.Count == 0 || ArenaGeometry.Dot( bolt[0] - origin, heading ) > limit + 0.75f )
			{
				bolts.RemoveAt( b );
				continue;
			}

			var kept = new List<Vector2> { bolt[0] };
			for ( var i = 1; i < bolt.Count; i++ )
			{
				var a = bolt[i - 1];
				var point = bolt[i];
				var alongA = ArenaGeometry.Dot( a - origin, heading );
				var alongB = ArenaGeometry.Dot( point - origin, heading );
				if ( alongB <= limit )
				{
					kept.Add( point );
					continue;
				}

				var span = alongB - alongA;
				var t = MathF.Abs( span ) > 0.001f ? (limit - alongA) / span : 0f;
				kept.Add( a + (point - a) * Math.Clamp( t, 0f, 1f ) );
				break;
			}

			if ( kept.Count < 2 )
			{
				bolts.RemoveAt( b );
				continue;
			}

			bolt.Clear();
			bolt.AddRange( kept );
		}
	}
}

public static class LightningPath
{
	public static List<List<Vector2>> Storm( Vector2 from, Vector2 to, int rank, int seed, int forkFloor = 0 )
	{
		rank = Math.Clamp( rank, 1, 3 );
		var bolts = new List<List<Vector2>>();
		var steps = rank <= 1 ? 7 : rank == 2 ? 11 : 15;
		var amp = rank <= 1 ? 52f : rank == 2 ? 92f : 140f;
		var main = Jag( from, to, seed, steps, amp );
		bolts.Add( main );

		var forks = rank <= 1 ? 0 : rank == 2 ? 1 : 2;
		if ( forks < forkFloor )
			forks = forkFloor;
		for ( var f = 0; f < forks; f++ )
		{
			if ( main.Count < 4 )
				break;

			var at = 2 + Hash( seed, 40 + f ) % (main.Count - 3);
			var origin = main[at];
			var along = to - from;
			if ( along.Length < 8f )
				continue;

			var dir = along.Normal;
			var perp = new Vector2( -dir.y, dir.x );
			var side = Hash( seed, 70 + f ) % 2 == 0 ? 1f : -1f;
			var reach = 90f + Hash( seed, 90 + f ) % 90;
			var tip = origin + dir * (reach * 0.4f) + perp * side * reach;
			bolts.Add( Jag( origin, tip, seed + 17 * (f + 1), 4 + rank, 40f + rank * 16f ) );
		}

		return bolts;
	}

	public static List<Vector2> Jag( Vector2 from, Vector2 to, int seed, int steps, float amp )
	{
		steps = Math.Max( 3, steps );
		var points = new List<Vector2>( steps );
		var span = to - from;
		var len = span.Length;
		if ( len < 4f )
		{
			points.Add( from );
			points.Add( to );
			return points;
		}

		var dir = span / len;
		var perp = new Vector2( -dir.y, dir.x );
		points.Add( from );
		for ( var i = 1; i < steps - 1; i++ )
		{
			var t = i / (float)(steps - 1);
			var fall = 1f - MathF.Abs( t * 2f - 1f );
			var unit = (Hash( seed, i ) % 1000) / 500f - 1f;
			points.Add( from + dir * (len * t) + perp * (unit * amp * MathF.Max( 0.18f, fall )) );
		}

		points.Add( to );
		return points;
	}

	static int Hash( int seed, int salt )
	{
		var n = seed * 16777619 ^ salt * 374761393;
		n = (n ^ (n >> 13)) * 1274126177;
		return n & 0x7fffffff;
	}
}

public sealed class PinLinger : Component
{
	Enemy target;
	int damage;
	float due;

	public static void Hang( Enemy enemy, int amount, float delay )
	{
		if ( !enemy.IsValid() || amount <= 0 || delay <= 0.01f )
			return;

		foreach ( var child in enemy.GameObject.Children )
		{
			if ( child.GetComponent<PinLinger>().IsValid() )
				return;
		}

		var go = enemy.Scene.CreateObject();
		go.Name = "Pin";
		go.Parent = enemy.GameObject;
		var hang = go.AddComponent<PinLinger>();
		hang.target = enemy;
		hang.damage = amount;
		hang.due = Time.Now + delay;
	}

	protected override void OnUpdate()
	{
		if ( Time.Now < due )
			return;

		if ( target.IsValid() && target.Alive )
			target.Damage( damage, 0f, 1f );

		GameObject.Destroy();
	}
}
