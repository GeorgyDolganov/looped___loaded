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
	public bool Armed => leftCatchZone;
	public int TargetsHit { get; private set; }
	public int Ricochets { get; private set; }
	public int Kills { get; private set; }
	public Color Tint => Flight.Tint;
	public int SlotIndex => Flight.SlotIndex;

	const float StepLength = 18f;
	const int TrailPoints = 48;

	readonly List<Vector3> trail = new();
	readonly List<Vector2> crumbs = new();
	readonly HashSet<Enemy> struck = new();
	readonly HashSet<Enemy> grazed = new();
	readonly HashSet<Enemy> ribboned = new();
	ArenaGeometry geometry;
	PolyLine trailLine;
	int pierceLeft;
	int breachLeft;
	int shredLeft;
	int rehitLeft;
	bool leftCatchZone;
	bool usedWind;
	bool boomeranging;
	bool echoSpawned;
	float launchedAt;
	float stutterUntil;
	float stickUntil;
	float skimUntil;
	float homingUntil;
	Vector2 stickDir;
	int boomIndex;

	public void Launch( GameLoop loop, RoundFlight flight, Vector2 origin, Vector2 direction )
	{
		Loop = loop;
		Flight = flight;
		geometry = loop.Geometry;
		Flat = origin;
		Direction = direction.Normal;
		Speed = BaseSpeed * MathF.Max( 0.2f, flight.SpeedScale );
		if ( loop.SnapBoost > 1.001f )
			Speed *= loop.SnapBoost;

		BouncesLeft = flight.MaxBounces;
		EnergyLeft = flight.Energy;
		pierceLeft = flight.PierceCharges;
		breachLeft = flight.BreachCharges;
		shredLeft = flight.ShredCharges;
		rehitLeft = flight.RehitCharges;
		Ricochets = 0;
		TargetsHit = 0;
		Kills = 0;
		leftCatchZone = false;
		usedWind = false;
		boomeranging = false;
		echoSpawned = false;
		launchedAt = Time.Now;
		stutterUntil = 0f;
		stickUntil = 0f;
		skimUntil = 0f;
		homingUntil = 0f;
		struck.Clear();
		grazed.Clear();
		ribboned.Clear();
		trail.Clear();
		crumbs.Clear();
		DropCrumb( origin );
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	public bool ConsumeShred()
	{
		if ( shredLeft <= 0 )
			return false;

		shredLeft--;
		return true;
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 26f, ShotColors.Player );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Player * 6f;
		glow.Radius = 420f;

		var trailObject = Scene.CreateObject();
		trailObject.Name = "Trail";
		trailObject.Parent = GameObject;

		trailLine = trailObject.AddComponent<PolyLine>();
		trailLine.HeadTint = ShotColors.Player;
		trailLine.TailTint = ShotColors.Player * 0.08f;
		trailLine.HeadWidth = Flight.RibbonWidth > 1f ? Flight.RibbonWidth * 0.7f : 12f;
		trailLine.TailWidth = 1f;
		trailLine.Apply();
	}

	protected override void OnUpdate()
	{
		if ( geometry is null || !Loop.IsValid() || Loop.IsFrozen )
			return;

		if ( !echoSpawned && Flight.EchoKeep > 0.01f && Time.Now >= launchedAt + 0.35f )
		{
			echoSpawned = true;
			var freeze = Flight.EchoFreeze ? Flight.FreezeDuration * 0.5f : 0f;
			RoundEcho.Spawn( Loop, crumbs, Flight.EchoKeep, 1, freeze, Flight.FreezeScale );
		}

		if ( Time.Now < stutterUntil || Time.Now < stickUntil )
		{
			if ( Time.Now >= stickUntil && stickDir.Length > 0.01f && Time.Now >= stutterUntil )
			{
				Direction = stickDir;
				Speed *= Flight.StickSpeed;
				stickDir = Vector2.Zero;
			}

			WorldPosition = geometry.ToPlayWorld( Flat );
			return;
		}

		if ( boomeranging )
		{
			FollowBoomerang();
			return;
		}

		var toTravel = Speed * Time.Delta;
		if ( Time.Now < skimUntil )
			toTravel *= Flight.SkimBoost;

		while ( toTravel > 0.001f )
		{
			var step = MathF.Min( StepLength, toTravel );
			toTravel -= step;

			if ( !Step( step ) )
				return;
		}

		Steer();
		Ribbon();
		LinkDropped();
		WorldPosition = geometry.ToPlayWorld( Flat );
		PushTrail( WorldPosition );
		DropCrumb( Flat );
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
		if ( geometry.TraceRay( Flat, Direction, step + Radius, out var hit ) )
		{
			if ( hit.Kind == WallKind.Panel && breachLeft > 0 )
			{
				breachLeft--;
				EnergyLeft -= MathF.Max( 0f, hit.Distance );
				Flat = hit.Position + Direction * (Radius + 6f);
			}
			else if ( !BounceWall( hit ) )
				return false;
		}
		else
		{
			EnergyLeft -= step;
			Flat += Direction * step;
		}

		Contain();
		if ( HitTarget() )
			return false;

		if ( BouncesLeft < 0 || EnergyLeft <= 0f )
		{
			Stall();
			return false;
		}

		if ( TryCatch() )
			return false;

		return true;
	}

	bool BounceWall( ArenaHit hit )
	{
		EnergyLeft -= MathF.Max( 0f, hit.Distance - Radius );
		PushTrail( geometry.ToPlayWorld( hit.Position ) );

		var wall = (hit.WallIndex >= 0 && hit.WallIndex < geometry.Walls.Count)
			? geometry.Walls[hit.WallIndex]
			: default;

		var normal = hit.Normal;
		if ( Flight.CueDegrees > 0.01f && wall.Length > 1f )
			normal = ArenaGeometry.Dot( wall.Normal, Direction ) > 0f ? -wall.Normal : wall.Normal;

		var skim = false;
		if ( Flight.SkimDegrees > 0.01f && wall.Length > 1f )
		{
			var along = MathF.Abs( ArenaGeometry.Dot( Direction, wall.Direction ) );
			var deg = MathX.RadianToDegree( MathF.Acos( Math.Clamp( along, 0f, 1f ) ) );
			skim = deg <= Flight.SkimDegrees;
		}

		Flat = hit.Position + normal * Radius;
		var bounced = ArenaGeometry.Reflect( Direction, normal ).Normal;

		if ( Flight.CushionDegrees > 0.01f && Ricochets == 0 && wall.Length > 1f )
		{
			var tangent = wall.Direction;
			if ( ArenaGeometry.Dot( tangent, bounced ) < 0f )
				tangent = -tangent;

			var hug = MathX.DegreeToRadian( Flight.CushionDegrees );
			bounced = (bounced + tangent * MathF.Tan( hug )).Normal;
		}

		Direction = bounced;
		Ricochets++;

		if ( !skim )
			BouncesLeft--;
		else
			skimUntil = Time.Now + 0.20f;

		if ( Flight.StutterTime > 0.01f )
			stutterUntil = Time.Now + Flight.StutterTime;

		if ( Flight.StickTime > 0.01f && wall.Length > 1f && Loop.Aim.IsValid() )
		{
			var along = wall.Direction;
			if ( ArenaGeometry.Dot( along, Loop.Aim.Direction ) < 0f )
				along = -along;

			stickDir = along;
			stickUntil = Time.Now + Flight.StickTime;
			Direction = along;
		}

		if ( hit.Kind == WallKind.Panel && Loop.Arena.IsValid() && breachLeft >= 0 )
		{
			Loop.Arena.KickPanel( hit.WallIndex, hit.Position, hit.Normal, Flight.KickExtra, Flight.KickSecond );
			NudgeOut();
		}

		if ( Flight.AccelMul > 1.001f )
			Boost();

		if ( rehitLeft > 0 )
		{
			struck.Clear();
			rehitLeft--;
		}

		if ( BouncesLeft <= 1 && Flight.HomingLead > 0.01f )
			homingUntil = Time.Now + Flight.HomingLead;

		var world = geometry.ToPlayWorld( Flat );
		Sound.Play( "sounds/impacts/bullets/impact-bullet-metal.sound", world );
		ImpactFlash.Spawn( Scene, world, ShotColors.Player, 0.65f );

		if ( BouncesLeft < 0 )
		{
			Stall( true );
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

	bool HitTarget()
	{
		foreach ( var target in Loop.Enemies )
		{
			if ( !target.IsValid() || !target.Alive )
				continue;

			var offset = Flat - target.Flat;
			var reach = target.Radius + Radius;
			var dist = offset.Length;

			if ( Flight.GrazePad > 0.01f && dist > reach && dist <= reach + Flight.GrazePad && !grazed.Contains( target ) )
			{
				grazed.Add( target );
				target.Damage( 1, this );
				if ( !target.Alive )
					OnKill( target );
				continue;
			}

			if ( dist > reach || struck.Contains( target ) )
				continue;

			var normal = dist < 0.01f ? -Direction : offset.Normal;
			PushTrail( geometry.ToPlayWorld( Flat ) );

			if ( target.BlocksFrom( Direction, this ) )
			{
				Flat = target.Flat + normal * (reach + 1f);
				Direction = ArenaGeometry.Reflect( Direction, normal ).Normal;
				BouncesLeft--;

				var world = geometry.ToPlayWorld( Flat );
				Sound.Play( "sounds/impacts/bullets/impact-bullet-metal.sound", world );
				ImpactFlash.Spawn( Scene, world, ShotColors.Player, 0.9f );

				if ( target.Kind == EnemyKind.Core )
					Loop.NoteArmor();

				if ( BouncesLeft < 0 )
				{
					Stall();
					return true;
				}

				continue;
			}

			var damage = Flight.Damage > 0 ? Flight.Damage : 1;
			if ( target.Marked )
				damage += Flight.MarkBonus;

			struck.Add( target );
			target.Damage( damage, this );
			TargetsHit++;
			target.Mark( 2.5f );
			target.Shove( Direction * (Flight.HookPush + Flight.HeavyPush) );

			RoundCombat.Lightning( Loop, target.Flat, Flight.ElectricJumps, 1, this, struck );

			if ( !target.Alive )
				OnKill( target );

			if ( pierceLeft > 0 )
			{
				pierceLeft--;
				Flat = target.Flat + Direction * (reach + 4f);
				continue;
			}

			Flat = target.Flat + normal * (reach + 1f);
			Direction = ArenaGeometry.Reflect( Direction, normal ).Normal;
			Speed *= Flight.PinballKeep;
			if ( !Flight.PinballFree )
				BouncesLeft--;

			if ( BouncesLeft < 0 )
			{
				Stall();
				return true;
			}
		}

		return false;
	}

	void OnKill( Enemy target )
	{
		Kills++;
		if ( Flight.AccelMul > 1.001f )
			Boost();

		if ( Flight.ExplosiveRadius > 1f )
			RoundCombat.Blast( Loop, target.Flat, Flight.ExplosiveRadius, 1, this, ShotColors.Player );

		if ( Flight.HomingLead > 0.01f )
			homingUntil = Time.Now + Flight.HomingLead;

		if ( Flight.StepDistance > 1f )
			StepForward( Flight.StepDistance );

		Redirect();
	}

	void StepForward( float distance )
	{
		if ( geometry.TraceRay( Flat, Direction, distance + Radius, out var hit ) )
		{
			Flat = hit.Position + hit.Normal * Radius;
			Direction = ArenaGeometry.Reflect( Direction, hit.Normal ).Normal;
			BouncesLeft--;
			Ricochets++;
			return;
		}

		Flat += Direction * distance;
	}

	void Redirect()
	{
		if ( Flight.RedirectRange <= 1f )
			return;

		Enemy best = null;
		var bestDist = Flight.RedirectRange;
		var cone = Flight.RedirectCone <= 0.01f ? 1.2f : Flight.RedirectCone;

		foreach ( var enemy in Loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || struck.Contains( enemy ) )
				continue;

			var to = enemy.Flat - Flat;
			var dist = to.Length;
			if ( dist < 1f || dist > bestDist )
				continue;

			var ang = MathF.Acos( Math.Clamp( ArenaGeometry.Dot( Direction, to.Normal ), -1f, 1f ) );
			if ( ang > cone * 0.5f )
				continue;

			bestDist = dist;
			best = enemy;
		}

		if ( best is not null )
			Direction = (best.Flat - Flat).Normal;
	}

	void Steer()
	{
		if ( Flight.IncurveRate > 0.0001f && Flat.Length > 8f )
		{
			var inward = -Flat.Normal;
			var turn = 1f - MathF.Exp( -Flight.IncurveRate * Time.Delta );
			Direction = (Direction + inward * turn).Normal;
		}

		if ( Flight.ClockwiseRate > 0.0001f )
		{
			var ang = Flight.ClockwiseRate * Time.Delta;
			var c = MathF.Cos( -ang );
			var s = MathF.Sin( -ang );
			Direction = new Vector2( Direction.x * c - Direction.y * s, Direction.x * s + Direction.y * c ).Normal;
		}

		if ( Flight.CornerRadius > 1f )
			PullCorner();

		if ( Flight.MarkTurn > 0.0001f )
			PullMark();

		SteerHome();
	}

	void PullCorner()
	{
		if ( Loop.Inventory.IsValid() && Loop.Inventory.InCatchZone( Flat, Radius, Flight.CatchBonus ) )
			return;

		WallSegment best = default;
		var bestDist = Flight.CornerRadius;
		var found = false;

		foreach ( var wall in geometry.Walls )
		{
			if ( wall.Kind != WallKind.Panel || wall.Length < 1f )
				continue;

			var along = Math.Clamp( ArenaGeometry.Dot( Flat - wall.A, wall.Direction ), 0f, wall.Length );
			var closest = wall.A + wall.Direction * along;
			var dist = (Flat - closest).Length;
			if ( dist >= bestDist )
				continue;

			var parallel = MathF.Abs( ArenaGeometry.Dot( Direction, wall.Direction ) );
			if ( parallel < 0.55f )
				continue;

			bestDist = dist;
			best = wall;
			found = true;
		}

		if ( !found )
			return;

		var end = (Flat - best.A).Length <= (Flat - best.B).Length ? best.A : best.B;
		var to = end - Flat;
		if ( to.Length < 1f )
			return;

		var turn = 1f - MathF.Exp( -Flight.CornerPull * Time.Delta );
		Direction = (Direction + to.Normal * turn).Normal;
	}

	void PullMark()
	{
		if ( Loop.Inventory.IsValid() && Loop.Inventory.InCatchZone( Flat, Radius, Flight.CatchBonus ) )
			return;

		Enemy best = null;
		var bestDist = 220f;

		foreach ( var enemy in Loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || !enemy.Marked )
				continue;

			var dist = (enemy.Flat - Flat).Length;
			if ( dist >= bestDist )
				continue;

			bestDist = dist;
			best = enemy;
		}

		if ( best is null )
			return;

		var turn = 1f - MathF.Exp( -Flight.MarkTurn * Time.Delta );
		Direction = (Direction + (best.Flat - Flat).Normal * turn).Normal;
	}

	void SteerHome()
	{
		if ( Flight.MagnetRadius <= 0f || !Armed || !Loop.Inventory.IsValid() )
			return;

		var lateClosed = Flight.LateMag && BouncesLeft > 1 && Kills <= 0 && Time.Now > homingUntil;
		if ( lateClosed )
			return;

		var toCatch = Loop.Inventory.CatchPoint - Flat;
		var distance = toCatch.Length;
		var radius = Flight.MagnetRadius;
		if ( Time.Now < homingUntil )
			radius *= 1.35f;

		if ( distance < 1f || distance > radius )
			return;

		var weight = 1f - distance / radius;
		var turn = 1f - MathF.Exp( -Flight.MagnetPull * weight * Time.Delta );
		Direction = (Direction + toCatch.Normal * turn).Normal;
	}

	void Ribbon()
	{
		if ( Flight.RibbonWidth <= 1f || crumbs.Count < 2 )
			return;

		var from = crumbs[^Math.Min( crumbs.Count, 6 )];
		var to = crumbs[^1];

		foreach ( var enemy in Loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || ribboned.Contains( enemy ) )
				continue;

			if ( RoundCombat.PointSegment( enemy.Flat, from, to ) > Flight.RibbonWidth + enemy.Radius )
				continue;

			ribboned.Add( enemy );
			enemy.Damage( 1, Flight.RibbonSlow, 0.7f );
		}
	}

	void LinkDropped()
	{
		var reach = Loop.Inventory.IsValid() ? Loop.Inventory.Loadout.LinkRadius : 0f;
		if ( reach <= 1f || !Loop.Inventory.IsValid() )
			return;

		foreach ( var slot in Loop.Inventory.Slots )
		{
			if ( slot.Status != RoundStatus.Dropped || !slot.Lost.IsValid() )
				continue;

			if ( (slot.Lost.Flat - Flat).Length > reach + Radius )
				continue;

			Loop.RecoverDropped( slot, "LINKED" );
		}
	}

	bool TryCatch()
	{
		if ( !Loop.Inventory.IsValid() )
			return false;

		var inside = Loop.Inventory.InCatchZone( Flat, Radius, Flight.CatchBonus );
		if ( !leftCatchZone )
		{
			if ( !inside )
				leftCatchZone = true;
			return false;
		}

		if ( !inside )
			return false;

		Loop.CatchRound( this );
		return true;
	}

	bool Stall( bool lastBounce = false )
	{
		if ( lastBounce && Flight.ExplosiveRadius > 1f )
			RoundCombat.Blast( Loop, Flat, Flight.ExplosiveRadius, 1, this, ShotColors.Player );

		if ( !usedWind && Flight.SecondWindKeep > 0.01f )
		{
			usedWind = true;
			BouncesLeft = Math.Max( BouncesLeft, 0 );
			EnergyLeft = MathF.Max( EnergyLeft, 400f );
			Speed = MathF.Max( Speed, BaseSpeed * Flight.SecondWindKeep );
			var ring = Flat.Length < 1f ? Vector2.Right : Flat.Normal;
			var goal = ring * geometry.TrackRadius;
			Direction = (goal - Flat).Length > 1f ? (goal - Flat).Normal : Direction;
			return false;
		}

		if ( !boomeranging && Flight.BoomerangKeep > 0.01f && crumbs.Count > 4 )
		{
			boomeranging = true;
			boomIndex = Math.Max( 1, (int)MathF.Floor( crumbs.Count * Flight.BoomerangKeep ) ) - 1;
			return false;
		}

		if ( Flight.FuseRadius > 1f )
			RoundCombat.Blast( Loop, Flat, Flight.FuseRadius, 1, this, ShotColors.Player );

		Loop.LoseRound( this );
		return true;
	}

	void FollowBoomerang()
	{
		if ( boomIndex < 0 || crumbs.Count == 0 )
		{
			Stall();
			return;
		}

		var remain = Speed * Time.Delta;
		while ( remain > 0.001f && boomIndex >= 0 )
		{
			var goal = crumbs[boomIndex];
			var to = goal - Flat;
			var dist = to.Length;
			if ( dist < 6f )
			{
				boomIndex--;
				continue;
			}

			var step = MathF.Min( remain, dist );
			Direction = to.Normal;
			Flat += Direction * step;
			remain -= step;
		}

		WorldPosition = geometry.ToPlayWorld( Flat );
		PushTrail( WorldPosition );

		if ( TryCatch() )
			return;

		if ( boomIndex < 0 )
			Stall();
	}

	void Boost()
	{
		var cap = BaseSpeed * Flight.AccelCap;
		Speed = MathF.Min( cap, Speed * MathF.Max( 1f, Flight.AccelMul ) );
	}

	void DropCrumb( Vector2 flat )
	{
		if ( crumbs.Count > 0 && (crumbs[^1] - flat).Length < 18f )
			return;

		crumbs.Add( flat );
		while ( crumbs.Count > 80 )
			crumbs.RemoveAt( 0 );
	}

	void PushTrail( Vector3 point )
	{
		trail.Add( point );

		while ( trail.Count > TrailPoints )
			trail.RemoveAt( 0 );

		trailLine?.SetPoints( trail );
	}
}
