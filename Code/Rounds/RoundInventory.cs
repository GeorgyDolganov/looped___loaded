namespace LoopedLoaded;

public sealed class RoundInventory : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float PickupRadius { get; set; } = 145f;

	public RunLoadout Loadout { get; } = new();
	public ActiveSkills Skills { get; } = new();
	public List<RoundProjectile> Live { get; } = new();
	public List<DroppedRound> Dropped { get; } = new();
	public int MagCap { get; private set; } = 1;
	public int MagLoaded { get; private set; } = 1;
	public int MagDropped => Dropped.Count;
	public int MagFlight => Math.Max( 0, MagCap - MagLoaded - MagDropped );
	public float ReloadLeft { get; private set; }
	public float ReloadFor { get; private set; }
	public int BurstLeft { get; private set; }
	public int BurstId { get; private set; }
	readonly Dictionary<Enemy, int> burstHits = new();
	public bool Beaming { get; private set; }
	public float BeamHeld { get; private set; }
	bool beamTail;
	public bool Ready => ReloadLeft <= 0.001f && !Beaming && MagLoaded > 0;
	public float Reload01
	{
		get
		{
			if ( Beaming && beam.IsValid() && beam.TickBudget > 0 )
				return Math.Clamp( beam.TicksLeft / (float)beam.TickBudget, 0f, 1f );

			if ( ReloadFor <= 0.01f )
				return Ready ? 1f : 0f;

			return 1f - Math.Clamp( ReloadLeft / ReloadFor, 0f, 1f );
		}
	}

	float cycleLeft;
	LaserBeam beam;
	TurretDrone turret;
	GameObject readyMarker;
	bool recovering;

	public void ResetLoadout()
	{
		ClearShots();
		ClearDropped();
		Loadout.Clear();
		Skills.Clear();
		MagCap = 1;
		MagLoaded = 1;
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
	}

	public void GrowMag( int amount )
	{
		var room = Progression.MaxSlots - MagCap;
		if ( room <= 0 || amount <= 0 )
			return;

		var add = Math.Min( amount, room );
		MagCap += add;
		MagLoaded = Math.Min( MagCap, MagLoaded + add );
	}

	public void ChamberAll()
	{
		recovering = true;
		ClearShots();
		ClearDropped();
		MagLoaded = MagCap;
		Skills.ReadyAll();
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
		recovering = false;
	}

	public void ClearShots()
	{
		StopBeam();
		foreach ( var shot in Live )
		{
			if ( shot.IsValid() )
				shot.GameObject.Destroy();
		}

		Live.Clear();
	}

	void ClearDropped()
	{
		foreach ( var drop in Dropped )
		{
			if ( drop.IsValid() )
				drop.GameObject.Destroy();
		}

		Dropped.Clear();
	}

	public bool Reveals( Vector2 flat, float radius )
	{
		if ( Beaming && beam.IsValid() && beam.Near( flat, radius ) )
			return true;

		foreach ( var shot in Live )
		{
			if ( shot.IsValid() && (shot.Flat - flat).Length <= radius )
				return true;
		}

		return false;
	}

	public void TickGun()
	{
		Prune();

		if ( !Loop.IsValid() || Loop.IsFrozen )
			return;

		var play = RealTime.Delta;
		ReloadLeft = MathF.Max( 0f, ReloadLeft - play );
		cycleLeft = MathF.Max( 0f, cycleLeft - play );

		if ( Loop.BlocksShot )
			return;

		var recipe = Loadout.Recipe();
		var down = Input.Down( "Attack1" );
		var pressed = Input.Pressed( "Attack1" );

		if ( Beaming )
		{
			if ( !beam.IsValid() || beam.Spent )
			{
				EndBeam( recipe );
				return;
			}

			if ( !down )
			{
				if ( recipe.BeamLinger && !beamTail && beam.TicksLeft > 0 )
				{
					beamTail = true;
					beam.FreezeAim();
				}
				else if ( !beamTail )
				{
					EndBeam( recipe );
					return;
				}
			}

			if ( !beamTail && Aim.IsValid() )
				beam.Aim( Aim.Muzzle, Aim.Direction, recipe );
			return;
		}

		if ( recipe.Beam )
		{
			if ( pressed && Ready )
				BeginBeam( recipe );
			else if ( pressed )
				ArenaSounds.Deny();
			return;
		}

		if ( recipe.Auto )
		{
			if ( down && Ready && cycleLeft <= 0.001f )
			{
				if ( BurstLeft <= 0 )
				{
					BurstLeft = recipe.Burst;
					OpenBurst();
				}

				var index = recipe.Burst - BurstLeft;
				FireVolley( recipe, volleyIndex: index );
				BurstLeft--;
				cycleLeft = recipe.Cycle;
				if ( BurstLeft <= 0 || MagLoaded <= 0 )
					BeginReload( recipe.Reload );
			}
			else if ( BurstLeft > 0 && BurstLeft < recipe.Burst && (!down || MagLoaded <= 0) )
			{
				BurstLeft = 0;
				BeginReload( recipe.Reload );
			}

			return;
		}

		if ( !pressed )
			return;

		if ( !Ready )
		{
			ArenaSounds.Deny();
			return;
		}

		FireVolley( recipe );
		BeginReload( recipe.Reload );
	}

	public void TryPickup()
	{
		if ( !Loop.IsValid() || !Runner.IsValid() || Loop.IsFrozen )
			return;

		PruneDropped();
		var reach = PickupRadius;
		var flat = Runner.Flat;
		for ( var i = Dropped.Count - 1; i >= 0; i-- )
		{
			var drop = Dropped[i];
			if ( !drop.IsValid() )
			{
				Dropped.RemoveAt( i );
				continue;
			}

			if ( (drop.Flat - flat).Length > reach )
				continue;

			if ( !Pocket( drop ) )
				break;

			Dropped.RemoveAt( i );
		}
	}

	public void AdvanceDropped( float playerBefore, float playerArc, float dashArc )
	{
		if ( !Loop.IsValid() || !Runner.IsValid() || Loop.IsFrozen || !Arena.IsValid() || Arena.Geometry is null )
			return;

		PruneDropped();
		var track = Arena.Geometry.TrackRadius;
		if ( track < 1f )
			return;

		var pace = Loadout.Recipe().Pickup;
		if ( pace <= 0f )
			pace = 1f;

		if ( dashArc > 0.001f )
			CatchDashed( playerBefore, playerArc, dashArc, track, pace );

		var player = Runner.Angle;
		for ( var i = Dropped.Count - 1; i >= 0; i-- )
		{
			var drop = Dropped[i];
			if ( !drop.IsValid() )
			{
				Dropped.RemoveAt( i );
				continue;
			}

			drop.Roll( player, dashArc, track, pace );
		}
	}

	void CatchDashed( float playerBefore, float playerArc, float dashArc, float track, float pace )
	{
		while ( true )
		{
			var best = -1;
			var bestGap = float.MaxValue;
			for ( var i = 0; i < Dropped.Count; i++ )
			{
				var drop = Dropped[i];
				if ( !drop.IsValid() )
					continue;

				var gap = drop.GapTo( playerBefore );
				if ( gap >= bestGap || !drop.Crosses( gap, playerArc, dashArc, track, pace ) )
					continue;

				best = i;
				bestGap = gap;
			}

			if ( best < 0 )
				return;

			if ( !Pocket( Dropped[best] ) )
			{
				var hold = Runner.Angle;
				for ( var i = 0; i < Dropped.Count; i++ )
				{
					var drop = Dropped[i];
					if ( !drop.IsValid() )
						continue;

					var gap = drop.GapTo( playerBefore );
					if ( drop.Crosses( gap, playerArc, dashArc, track, pace ) )
						drop.ParkShort( hold, track );
				}

				return;
			}

			Dropped.RemoveAt( best );
		}
	}

	bool Pocket( DroppedRound drop )
	{
		if ( MagLoaded >= MagCap || !drop.IsValid() )
			return false;

		MagLoaded++;
		Loop.NoteCatch();
		ArenaSounds.Pickup();
		drop.GameObject.Destroy();
		return true;
	}

	public bool CatchRound()
	{
		if ( MagLoaded >= MagCap )
			return false;

		MagLoaded++;
		Loop.NoteCatch();
		ArenaSounds.Pickup();
		return true;
	}

	public void DropSpent( Vector2 from )
	{
		if ( recovering || !Loop.IsValid() )
			return;

		if ( MagLoaded + Dropped.Count >= MagCap )
			return;

		var go = Loop.Scene.CreateObject();
		go.Name = "Dropped Round";
		var drop = go.AddComponent<DroppedRound>();
		drop.Place( Loop, Loop.SnapToTrack( from ), 0 );
		Dropped.Add( drop );
	}

	public void NudgeDropped()
	{
		if ( !Arena.IsValid() )
			return;

		foreach ( var drop in Dropped )
		{
			if ( !drop.IsValid() )
				continue;

			var ang = MathF.Atan2( drop.Flat.y, drop.Flat.x );
			drop.SetFlat( Arena.Geometry.TrackPoint( ang ) );
		}
	}

	void BeginReload( float duration )
	{
		ReloadFor = MathF.Max( GameSettings.Traits.ReloadMin, duration );
		ReloadLeft = ReloadFor;
		BurstLeft = 0;
	}

	void BeginBeam( GunRecipe recipe )
	{
		if ( MagLoaded <= 0 )
			return;

		MagLoaded--;
		Beaming = true;
		BeamHeld = 0f;
		beamTail = false;
		var go = Loop.Scene.CreateObject();
		go.Name = "Laser Beam";
		beam = go.AddComponent<LaserBeam>();
		beam.Arm( Loop, recipe );
		if ( Aim.IsValid() )
			beam.Aim( Aim.Muzzle, Aim.Direction, recipe );
		Loop.NoteShot();
		ArenaSounds.Fire( Aim.IsValid() ? Aim.MuzzleWorld : Vector3.Zero );
		if ( Runner.IsValid() )
			Runner.PlayShoot();
	}

	void EndBeam( GunRecipe recipe )
	{
		var origin = Runner.IsValid() ? Runner.Flat : Vector2.Zero;
		StopBeam();
		DropSpent( origin );
		BeginReload( recipe.Reload );
	}

	void StopBeam()
	{
		Beaming = false;
		BeamHeld = 0f;
		beamTail = false;
		if ( beam.IsValid() )
			beam.GameObject.Destroy();
		beam = null;
	}

	void OpenBurst()
	{
		BurstId++;
		burstHits.Clear();
	}

	public bool BiteReady( Enemy enemy, int burstId, int volleyIndex )
	{
		if ( burstId != BurstId || enemy is null )
			return false;

		return burstHits.TryGetValue( enemy, out var seen ) && seen < volleyIndex;
	}

	public void NoteBite( Enemy enemy, int burstId, int volleyIndex )
	{
		if ( burstId != BurstId || enemy is null )
			return;

		if ( !burstHits.TryGetValue( enemy, out var seen ) || volleyIndex > seen )
			burstHits[enemy] = volleyIndex;
	}

	void FireVolley( GunRecipe recipe, bool spendMag = true, ShotVolley volley = null, int volleyIndex = 0 )
	{
		if ( !Loop.IsValid() || !Aim.IsValid() )
			return;

		ShotRange.Apply( ref recipe, Loop );

		if ( spendMag )
		{
			if ( MagLoaded <= 0 )
				return;

			MagLoaded--;
		}

		var count = Math.Max( 1, recipe.Count );
		var cone = recipe.Cone;
		if ( recipe.WalkStep > 0f )
			cone += volleyIndex * recipe.WalkStep;
		volley ??= new ShotVolley { Alive = count, PerPellet = recipe.PerPelletSplash };
		if ( !spendMag )
			volley.Alive += count;

		for ( var i = 0; i < count; i++ )
		{
			var yaw = ShotSpread.Yaw( i, count, cone );
			var heading = ShotSpread.Turn( Aim.Direction, yaw );
			var flight = ToFlight( recipe, volley );
			flight.Damage = PelletShare( recipe.Damage, i, count );
			flight.Bite = recipe.Bite;
			flight.BurstId = BurstId;
			flight.VolleyIndex = volleyIndex;

			var go = Loop.Scene.CreateObject();
			go.Name = "Shot";
			var projectile = go.AddComponent<RoundProjectile>();
			projectile.Radius = recipe.Radius;
			projectile.Launch( Loop, flight, Aim.Muzzle, heading );
			Live.Add( projectile );
		}

		Loop.NoteShot();
		ArenaSounds.Fire( Aim.MuzzleWorld );
		ImpactFlash.Spawn( Loop.Scene, Aim.MuzzleWorld, ShotColors.Player, recipe.Nail ? 0.55f : 0.8f );
		if ( Runner.IsValid() )
			Runner.PlayShoot();
	}

	static int PelletShare( int total, int index, int count )
	{
		if ( count <= 1 )
			return Math.Max( 1, total );

		if ( total < count )
			return 1;

		var share = total / count;
		var extra = total % count;
		return share + (index < extra ? 1 : 0);
	}

	static RoundFlight ToFlight( GunRecipe recipe, ShotVolley volley ) => new()
	{
		Tint = ShotColors.Player,
		Damage = recipe.Damage,
		PierceCharges = recipe.Pierce,
		MaxBounces = recipe.Bounces,
		BounceDamage = recipe.BounceDamage,
		BounceSpeed = recipe.BounceSpeed,
		Energy = recipe.Energy,
		SpeedScale = recipe.SpeedScale,
		ExplosiveRadius = recipe.Splash,
		SplashDamage = recipe.SplashDamage,
		ExtraSplashes = recipe.ExtraSplash,
		FriendlySplash = recipe.FriendlySplash,
		RampPierce = recipe.RampPierce,
		Falloff = recipe.Falloff,
		KickForce = recipe.KickForce,
		KickRange = recipe.KickRange,
		StunTime = recipe.StunTime,
		StunRange = recipe.StunRange,
		StickTime = recipe.StickTime,
		Nail = recipe.Nail,
		Fetch = recipe.Fetch,
		Ghost = recipe.Ghost,
		Volley = volley
	};

	void Prune()
	{
		for ( var i = Live.Count - 1; i >= 0; i-- )
		{
			if ( !Live[i].IsValid() )
				Live.RemoveAt( i );
		}

		PruneDropped();
	}

	void PruneDropped()
	{
		for ( var i = Dropped.Count - 1; i >= 0; i-- )
		{
			if ( !Dropped[i].IsValid() )
				Dropped.RemoveAt( i );
		}
	}

	protected override void OnStart()
	{
		readyMarker = Blocks.SpawnSphere( GameObject, "Ready", Vector3.Zero, 20f, ShotColors.Player );
	}

	void SyncTurret()
	{
		var level = Loop.IsValid() && !Loop.InCity && !Loop.InMenu ? Loadout.TraitLevel( Trinkets.Find( TurretDrone.TrinketId ) ) : 0;
		if ( level <= 0 )
		{
			if ( turret.IsValid() )
				turret.GameObject.Destroy();
			turret = null;
			return;
		}

		if ( !turret.IsValid() )
			turret = TurretDrone.Spawn( Loop, GameObject );

		turret.Level = level;
	}

	protected override void OnUpdate()
	{
		SyncTurret();
		Skills.Sync( Loadout );

		if ( Loop.IsValid() && (Loop.InCity || Loop.InMenu) )
		{
			if ( readyMarker.IsValid() )
				readyMarker.Enabled = false;
			return;
		}

		if ( !Arena.IsValid() || !Runner.IsValid() || !readyMarker.IsValid() )
			return;

		var lit = Ready;
		readyMarker.Enabled = lit;
		readyMarker.WorldPosition = Arena.Geometry.ToPlayWorld( Runner.Flat ) + Vector3.Up * 140f;
		var renderer = readyMarker.GetComponent<ModelRenderer>();
		if ( renderer.IsValid() )
			renderer.Tint = lit ? ShotColors.Player : new Color( 0.35f, 0.45f, 0.55f );
	}
}

public static class ShotSpread
{
	public static float Yaw( int index, int count, float cone )
	{
		if ( count <= 1 || cone <= 0.01f )
			return 0f;

		return -cone * 0.5f + cone * index / (count - 1);
	}

	public static Vector2 Turn( Vector2 dir, float degrees )
	{
		if ( MathF.Abs( degrees ) < 0.01f )
			return dir.Normal;

		var ang = MathX.DegreeToRadian( degrees );
		var c = MathF.Cos( ang );
		var s = MathF.Sin( ang );
		return new Vector2( dir.x * c - dir.y * s, dir.x * s + dir.y * c ).Normal;
	}
}
