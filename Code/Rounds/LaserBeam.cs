namespace LoopedLoaded;

public sealed class LaserBeam : Component
{
	static readonly Color BoltCore = new Color( 0.82f, 0.94f, 1f );
	static readonly Color BoltGlow = new Color( 0.38f, 0.52f, 1f );

	GameLoop loop;
	GunRecipe recipe;
	Vector2 origin;
	readonly List<PolyLine> lines = new();
	readonly List<(Vector2 A, Vector2 B)> rays = new();
	readonly Dictionary<Enemy, float> nextHit = new();
	PolyLine splashRing;
	Vector2 splashAt;
	bool splashLive;
	float nextSplash;

	public void Arm( GameLoop host, GunRecipe gun )
	{
		loop = host;
		recipe = gun;
	}

	public void Aim( Vector2 muzzle, Vector2 dir, GunRecipe gun )
	{
		recipe = gun;
		origin = muzzle;
		Build( dir.Normal );
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

		Aim( loop.Aim.Muzzle, loop.Aim.Direction, recipe );
		Strike();
	}

	void Build( Vector2 dir )
	{
		rays.Clear();
		var count = Math.Max( 1, recipe.Count );
		var cone = recipe.Cone;
		var geometry = loop.Geometry;
		var rank = Math.Max( 1, recipe.BeamRank );
		var seed = (int)(Time.Now * (9f + rank * 7f));
		var bolts = new List<List<Vector2>>();
		var range = recipe.BeamRange;
		if ( recipe.PointAim && loop.Aim.IsValid() )
			range = MathF.Min( range, (loop.Aim.Cursor - origin).Length );
		splashLive = false;

		for ( var i = 0; i < count; i++ )
		{
			var yaw = ShotSpread.Yaw( i, count, cone );
			var heading = ShotSpread.Turn( dir, yaw );
			var end = origin + heading * range;
			if ( geometry is not null && geometry.TraceRay( origin, heading, range, out var hit ) )
			{
				end = hit.Position;
				if ( hit.Kind == WallKind.Panel && loop.Arena.IsValid() )
					loop.Arena.StrikeBoard( hit.WallIndex, hit.Position, hit.Normal, 0f, false );
				else if ( hit.Kind == WallKind.Spin && loop.Arena.IsValid() )
					loop.Arena.PushSpinner( hit.WallIndex, hit.Position, heading );
			}

			if ( i == count / 2 )
			{
				splashAt = end;
				splashLive = recipe.Splash > 1f;
			}

			foreach ( var bolt in LightningPath.Storm( origin, end, rank, seed + i * 31 ) )
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
		var width = MathF.Max( 8f, recipe.BeamWidth );
		var tick = MathF.Max( 0.05f, recipe.BeamTick );
		var hit = Math.Max( 1, recipe.BeamHit );
		Enemy nearest = null;
		var nearestDist = float.MaxValue;
		Vector2 nearestAt = default;

		foreach ( var enemy in loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive )
				continue;

			var along = false;
			Vector2 hitAt = enemy.Flat;
			foreach ( var ray in rays )
			{
				if ( RoundCombat.PointSegment( enemy.Flat, ray.A, ray.B ) > width + enemy.Radius )
					continue;

				along = true;
				hitAt = Closest( enemy.Flat, ray.A, ray.B );
				break;
			}

			if ( !along )
				continue;

			var incoming = (hitAt - origin);
			if ( incoming.Length < 1f )
				incoming = loop.Aim.Direction;
			else
				incoming = incoming.Normal;

			if ( enemy.BlocksFrom( incoming, null, recipe.Pierce > 0, hitAt ) )
			{
				if ( Locations.IsBoss( enemy.Kind ) )
					loop.NoteArmor();
				continue;
			}

			var dist = (hitAt - origin).Length;
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				nearest = enemy;
				nearestAt = hitAt;
			}

			if ( nextHit.TryGetValue( enemy, out var due ) && Time.Now < due )
				continue;

			nextHit[enemy] = Time.Now + tick;
			enemy.Damage( hit, 0f, 1f );

			if ( recipe.StickTime > 0.01f )
				PinLinger.Hang( enemy, 1, recipe.StickTime );
		}

		if ( recipe.Splash > 1f && nearest.IsValid() && Time.Now >= nextSplash )
		{
			nextSplash = Time.Now + tick;
			RoundCombat.Blast( loop, nearestAt, recipe.Splash, Math.Max( 1, recipe.SplashDamage ), null, ShotColors.Player, recipe.FriendlySplash );
		}
	}

	void Draw( List<List<Vector2>> bolts, int rank )
	{
		var pulse = 0.62f + 0.38f * MathF.Abs( MathF.Sin( Time.Now * (18f + rank * 8f) ) );
		var core = Color.Lerp( BoltGlow, BoltCore, pulse );
		var glow = BoltGlow * (0.35f + pulse * 0.45f );
		var width = rank >= 3 ? 9f : rank >= 2 ? 7f : 5f;

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
			lines[i].TailWidth = rank >= 2 ? 3f : 2.2f;
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

	static Vector2 Closest( Vector2 point, Vector2 a, Vector2 b )
	{
		var span = b - a;
		var length = span.Length;
		if ( length < 0.001f )
			return a;

		var t = Math.Clamp( ArenaGeometry.Dot( point - a, span ) / (length * length), 0f, 1f );
		return a + span * t;
	}
}

public static class LightningPath
{
	public static List<List<Vector2>> Storm( Vector2 from, Vector2 to, int rank, int seed )
	{
		rank = Math.Clamp( rank, 1, 3 );
		var bolts = new List<List<Vector2>>();
		var steps = rank <= 1 ? 7 : rank == 2 ? 11 : 15;
		var amp = rank <= 1 ? 18f : rank == 2 ? 32f : 48f;
		var main = Jag( from, to, seed, steps, amp );
		bolts.Add( main );

		var forks = rank <= 1 ? 0 : rank == 2 ? 1 : 2;
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
			bolts.Add( Jag( origin, tip, seed + 17 * (f + 1), 4 + rank, 14f + rank * 6f ) );
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
