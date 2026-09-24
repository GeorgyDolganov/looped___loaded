namespace LoopedLoaded;

public sealed class RoundProjectile : Component
{
	[Property] public float BaseSpeed { get; set; } = 950f;
	[Property] public float Radius { get; set; } = 13f;

	public GameLoop Loop { get; private set; }
	public RoundFlight Flight { get; private set; }
	public Vector2 Flat { get; private set; }
	public Vector2 Direction { get; private set; }
	public float Speed { get; private set; }
	public int BouncesLeft { get; private set; }
	public float EnergyLeft { get; private set; }
	public int TargetsHit { get; private set; }
	public int Ricochets { get; private set; }
	public int Kills { get; private set; }
	public Color Tint => Flight.Tint;

	const float StepLength = 18f;
	const int TrailPoints = 48;

	readonly List<Vector3> trail = new();
	readonly HashSet<Enemy> struck = new();
	ArenaGeometry geometry;
	PolyLine trailLine;
	PolyLine splashRing;
	int pierceLeft;
	int bored;
	bool lensInside;
	float travelled;

	public void Launch( GameLoop loop, RoundFlight flight, Vector2 origin, Vector2 direction )
	{
		Loop = loop;
		Flight = flight;
		geometry = loop.Geometry;
		Flat = origin;
		Direction = direction.Normal;
		Speed = BaseSpeed * MathF.Max( 0.2f, flight.SpeedScale );
		BouncesLeft = flight.MaxBounces;
		EnergyLeft = flight.Energy;
		pierceLeft = flight.PierceCharges;
		bored = 0;
		Ricochets = 0;
		TargetsHit = 0;
		Kills = 0;
		travelled = 0f;
		struck.Clear();
		trail.Clear();
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	public bool ConsumeShred() => false;

	protected override void OnStart()
	{
		var size = Flight.Nail ? 16f : 26f;
		Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, size, ShotColors.Player );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Player * (Flight.Nail ? 3.5f : 6f);
		glow.Radius = Flight.Nail ? 260f : 420f;

		var trailObject = Scene.CreateObject();
		trailObject.Name = "Trail";
		trailObject.Parent = GameObject;

		trailLine = trailObject.AddComponent<PolyLine>();
		trailLine.HeadTint = ShotColors.Player;
		trailLine.TailTint = ShotColors.Player * 0.08f;
		trailLine.HeadWidth = Flight.Nail ? 6f : 12f;
		trailLine.TailWidth = 1f;
		trailLine.Apply();
	}

	protected override void OnUpdate()
	{
		if ( geometry is null || !Loop.IsValid() )
			return;

		if ( !Loop.IsFrozen )
		{
			var toTravel = Speed * Time.Delta;
			while ( toTravel > 0.001f )
			{
				var step = MathF.Min( StepLength, toTravel );
				toTravel -= step;
				if ( !Step( step ) )
					return;
			}

			Contain();
			if ( HitTarget() )
				return;

			WorldPosition = geometry.ToPlayWorld( Flat );
			PushTrail( WorldPosition );
		}

		PaintSplash();
	}

	public void NudgeOut()
	{
		if ( geometry is null )
			return;

		var flat = Flat;
		geometry.Eject( ref flat, Radius );
		Flat = flat;
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	bool Step( float step )
	{
		var travel = step;
		var arrive = false;
		if ( Flight.PointAim )
		{
			var along = ArenaGeometry.Dot( Flight.Mark - Flat, Direction );
			if ( along <= step )
			{
				travel = MathF.Max( 0f, along );
				arrive = true;
			}
		}

		if ( geometry.TraceRay( Flat, Direction, travel + Radius, out var hit ) )
		{
			if ( !BounceWall( hit, Direction ) )
				return false;
		}
		else
		{
			EnergyLeft -= travel;
			travelled += travel;
			Flat += Direction * travel;
			if ( arrive )
			{
				Die( true );
				return false;
			}
		}

		BendLens();
		Contain();
		if ( HitTarget() )
			return false;

		if ( BouncesLeft < 0 || EnergyLeft <= 0f )
		{
			Die( true );
			return false;
		}

		return true;
	}

	bool BounceWall( ArenaHit hit, Vector2 incoming )
	{
		EnergyLeft -= MathF.Max( 0f, hit.Distance - Radius );
		travelled += MathF.Max( 0f, hit.Distance );
		PushTrail( geometry.ToPlayWorld( hit.Position ) );

		var normal = hit.Normal;
		Flat = hit.Position + normal * Radius;
		Direction = ArenaGeometry.Reflect( incoming, normal ).Normal;
		Ricochets++;
		BouncesLeft--;

		if ( hit.Kind == WallKind.Panel && Loop.Arena.IsValid() )
		{
			Loop.Arena.StrikeBoard( hit.WallIndex, hit.Position, hit.Normal, 0f, false );
			NudgeOut();
		}
		else if ( hit.Kind == WallKind.Spin && Loop.Arena.IsValid() )
			Loop.Arena.PushSpinner( hit.WallIndex, hit.Position, incoming );

		var world = geometry.ToPlayWorld( Flat );
		ArenaSounds.Ricochet( world );
		ImpactFlash.Spawn( Scene, world, ShotColors.Player, 0.65f );

		if ( BouncesLeft < 0 )
		{
			Die( true );
			return false;
		}

		return true;
	}

	void Contain()
	{
		var flat = Flat;
		var direction = Direction;
		if ( !geometry.Contain( ref flat, ref direction, Radius ) )
			return;

		Flat = flat;
		Direction = direction;
	}

	void BendLens()
	{
		if ( !Loop.InBossFight || Loop.Location != RunLocation.Glass )
		{
			lensInside = false;
			return;
		}

		var inside = Flat.Length < geometry.CoreRadius;
		if ( inside && !lensInside )
		{
			var ang = MathX.DegreeToRadian( 15f );
			var c = MathF.Cos( -ang );
			var s = MathF.Sin( -ang );
			Direction = new Vector2( Direction.x * c - Direction.y * s, Direction.x * s + Direction.y * c ).Normal;
			ArenaSounds.Crack( geometry.ToPlayWorld( Flat ) );
		}

		lensInside = inside;
	}

	bool HitTarget()
	{
		Loop.BeginEnemyScan();
		try
		{
			return ScanTargets();
		}
		finally
		{
			if ( Loop.IsValid() )
				Loop.EndEnemyScan();
		}
	}

	bool ScanTargets()
	{
		foreach ( var target in Loop.Enemies )
		{
			if ( !target.IsValid() || !target.Alive || struck.Contains( target ) )
				continue;

			var offset = Flat - target.Flat;
			var reach = target.Radius + Radius;
			var dist = offset.Length;
			if ( dist > reach )
				continue;

			var normal = dist < 0.01f ? -Direction : offset.Normal;
			PushTrail( geometry.ToPlayWorld( Flat ) );

			if ( target.BlocksFrom( Direction, this ) )
			{
				if ( Locations.IsBoss( target.Kind ) )
					Loop.NoteArmor();

				if ( !BounceOff( target.Flat, normal, reach ) )
					return true;

				continue;
			}

			var damage = ShotDamage();
			if ( damage > 0 && Flight.RampPierce && pierceLeft > 0 )
				damage += bored;
			if ( damage > 0 && Flight.Bite && Loop.Inventory.IsValid() && Loop.Inventory.BiteReady( target, Flight.BurstId, Flight.VolleyIndex ) )
				damage += 1;
			if ( Loop.Inventory.IsValid() )
				Loop.Inventory.NoteBite( target, Flight.BurstId, Flight.VolleyIndex );
			struck.Add( target );
			if ( damage > 0 )
				target.Damage( damage, this );

			TargetsHit++;
			if ( Flight.StickTime > 0.01f )
				PinLinger.Hang( target, 1, Flight.StickTime );

			if ( Flight.Volley is { } crowd && crowd.TryCrowd( target ) )
			{
				if ( Flight.KickForce > 1f && travelled <= Flight.KickRange )
				{
					var away = Loop.Runner.IsValid() ? target.Flat - Loop.Runner.Flat : -Direction;
					if ( away.Length < 0.01f )
						away = normal;
					target.Shove( away.Normal * Flight.KickForce );
				}

				if ( Flight.StunTime > 0.01f && travelled <= Flight.StunRange )
					target.Stun( Flight.StunTime );
			}

			if ( Flight.ExplosiveRadius > 1f && (Flight.Volley is null || Flight.Volley.TrySplash()) )
				RoundCombat.Blast( Loop, target.Flat, Flight.ExplosiveRadius, SplashDamage(), this, ShotColors.Player, Flight.FriendlySplash );

			if ( !target.Alive )
				Kills++;

			if ( pierceLeft > 0 )
			{
				pierceLeft--;
				bored++;
				Flat = target.Flat + Direction * (reach + 4f);
				continue;
			}

			if ( !BounceOff( target.Flat, normal, reach ) )
				return true;
		}

		return false;
	}

	bool BounceOff( Vector2 from, Vector2 normal, float reach )
	{
		var incoming = Direction;
		Flat = from + normal * (reach + 1f);
		Direction = ArenaGeometry.Reflect( incoming, normal ).Normal;
		BouncesLeft--;
		Ricochets++;

		var world = geometry.ToPlayWorld( Flat );
		ArenaSounds.Ricochet( world );
		ImpactFlash.Spawn( Scene, world, ShotColors.Player, 0.9f );

		if ( BouncesLeft < 0 )
		{
			Die();
			return false;
		}

		return true;
	}

	int ShotDamage()
	{
		if ( Flight.Falloff > 1f && travelled > Flight.Falloff )
			return 0;

		var damage = Math.Max( 0, Flight.Damage );
		if ( Flight.MeatBonus > 0 && Flight.MeatRange > 1f && travelled <= Flight.MeatRange )
			damage += Flight.MeatBonus;

		return damage;
	}

	int SplashDamage() => Math.Max( 1, Flight.SplashDamage );

	void Die( bool spent = false )
	{
		if ( spent && Flight.ExplosiveRadius > 1f && (Flight.Volley is null || Flight.Volley.TrySplash()) )
			RoundCombat.Blast( Loop, Flat, Flight.ExplosiveRadius, SplashDamage(), this, ShotColors.Player, Flight.FriendlySplash );

		if ( Flight.Volley is { } volley )
		{
			volley.LastFlat = Flat;
			volley.Alive--;
			if ( volley.Alive <= 0 && !volley.Closed && !volley.Hold )
			{
				volley.Closed = true;
				if ( Loop.IsValid() )
					Loop.Inventory?.DropSpent( Flat );
			}
		}

		GameObject.Destroy();
	}

	void PushTrail( Vector3 point )
	{
		trail.Add( point );

		while ( trail.Count > TrailPoints )
			trail.RemoveAt( 0 );

		trailLine?.SetPoints( trail );
	}

	void PaintSplash()
	{
		if ( Flight.ExplosiveRadius <= 1f || geometry is null )
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

		var tint = RoundCombat.RingTint( Flight.FriendlySplash );
		splashRing.HeadTint = tint;
		splashRing.TailTint = tint * 0.35f;
		splashRing.HeadWidth = 3.5f;
		splashRing.TailWidth = 3.5f;
		splashRing.Apply();
		splashRing.SetPoints( RoundCombat.Circle( geometry, Flat, Flight.ExplosiveRadius ) );
	}
}
