namespace LoopedLoaded;

public sealed class LaserBeam : Component
{
	GameLoop loop;
	GunRecipe recipe;
	Vector2 origin;
	readonly List<PolyLine> lines = new();
	readonly List<(Vector2 A, Vector2 B)> rays = new();
	readonly Dictionary<Enemy, float> nextHit = new();
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

		for ( var i = 0; i < count; i++ )
		{
			var yaw = 0f;
			if ( count > 1 && cone > 0.01f )
				yaw = -cone * 0.5f + cone * i / (count - 1);

			var heading = Turn( dir, yaw );
			var end = origin + heading * recipe.BeamRange;
			if ( geometry is not null && geometry.TraceRay( origin, heading, recipe.BeamRange, out var hit ) )
			{
				end = hit.Position;
				if ( hit.Kind == WallKind.Panel && loop.Arena.IsValid() )
					loop.Arena.StrikeBoard( hit.WallIndex, hit.Position, hit.Normal, 0f, false );
			}

			rays.Add( (origin, end) );
		}

		Draw();
	}

	void Strike()
	{
		var width = MathF.Max( 8f, recipe.BeamWidth );
		var tick = MathF.Max( 0.05f, recipe.BeamTick );
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
			enemy.Damage( recipe.Damage, 0f, 1f );

			if ( recipe.StickTime > 0.01f )
				PinLinger.Hang( enemy, 1, recipe.StickTime );
		}

		if ( recipe.Splash > 1f && nearest.IsValid() && Time.Now >= nextSplash )
		{
			nextSplash = Time.Now + tick;
			RoundCombat.Blast( loop, nearestAt, recipe.Splash, 1, null, ShotColors.Player, recipe.FriendlySplash );
		}
	}

	void Draw()
	{
		while ( lines.Count < rays.Count )
		{
			var go = Scene.CreateObject();
			go.Name = "Beam Ray";
			go.Parent = GameObject;
			var line = go.AddComponent<PolyLine>();
			line.HeadTint = new Color( 1f, 0.35f, 0.72f );
			line.TailTint = new Color( 1f, 0.35f, 0.72f ) * 0.15f;
			line.HeadWidth = recipe.BeamWidth;
			line.TailWidth = 3f;
			line.Apply();
			lines.Add( line );
		}

		for ( var i = 0; i < lines.Count; i++ )
		{
			if ( i >= rays.Count )
			{
				lines[i].Clear();
				continue;
			}

			var a = loop.Geometry.ToPlayWorld( rays[i].A );
			var b = loop.Geometry.ToPlayWorld( rays[i].B );
			lines[i].HeadWidth = recipe.BeamWidth;
			lines[i].Apply();
			lines[i].SetPoints( new List<Vector3> { a, b } );
		}
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

	static Vector2 Turn( Vector2 dir, float degrees )
	{
		if ( MathF.Abs( degrees ) < 0.01f )
			return dir.Normal;

		var ang = MathX.DegreeToRadian( degrees );
		var c = MathF.Cos( ang );
		var s = MathF.Sin( ang );
		return new Vector2( dir.x * c - dir.y * s, dir.x * s + dir.y * c ).Normal;
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
