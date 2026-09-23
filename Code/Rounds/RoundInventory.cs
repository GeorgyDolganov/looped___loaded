namespace LoopedLoaded;

public sealed class RoundInventory : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float PickupRadius { get; set; } = 145f;

	public RunLoadout Loadout { get; } = new();
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
	public bool Ready => ReloadLeft <= 0.001f && !Beaming && MagLoaded > 0 && doubleLeft <= 0.001f;
	public float Reload01
	{
		get
		{
			if ( Beaming )
			{
				var cap = MathF.Max( 0.01f, Loadout.Recipe().BeamMaxHold );
				return Math.Clamp( BeamHeld / cap, 0f, 1f );
			}

			if ( ReloadFor <= 0.01f )
				return Ready ? 1f : 0f;

			return 1f - Math.Clamp( ReloadLeft / ReloadFor, 0f, 1f );
		}
	}

	float cycleLeft;
	LaserBeam beam;
	GameObject readyMarker;
	bool recovering;
	float doubleLeft;
	GunRecipe doubleRecipe;
	ShotVolley doubleVolley;

	public void ResetLoadout()
	{
		ClearShots();
		ClearDropped();
		Loadout.Clear();
		MagCap = 1;
		MagLoaded = 1;
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
		doubleLeft = 0f;
		doubleVolley = null;
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
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
		doubleLeft = 0f;
		doubleVolley = null;
		recovering = false;
	}

	public void ClearShots()
	{
		doubleLeft = 0f;
		doubleVolley = null;
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

		ReloadLeft = MathF.Max( 0f, ReloadLeft - Time.Delta );
		cycleLeft = MathF.Max( 0f, cycleLeft - Time.Delta );

		if ( doubleLeft > 0.001f )
		{
			doubleLeft = MathF.Max( 0f, doubleLeft - Time.Delta );
			if ( doubleLeft <= 0.001f && doubleVolley is not null )
			{
				doubleVolley.Hold = false;
				FireVolley( doubleRecipe, false, doubleVolley );
				doubleVolley = null;
				BeginReload( doubleRecipe.Reload );
			}

			return;
		}

		if ( Loop.BlocksShot )
			return;

		var recipe = Loadout.Recipe();
		var down = Input.Down( "Attack1" );
		var pressed = Input.Pressed( "Attack1" );

		if ( Beaming )
		{
			if ( !down )
			{
				EndBeam( recipe );
				return;
			}

			BeamHeld = MathF.Min( recipe.BeamMaxHold, BeamHeld + Time.Delta );
			if ( Aim.IsValid() )
				beam?.Aim( Aim.Muzzle, Aim.Direction, recipe );
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
			var finishing = recipe.CommitBurst && BurstLeft > 0 && BurstLeft < recipe.Burst && MagLoaded > 0;
			if ( (down || finishing) && Ready && cycleLeft <= 0.001f )
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
			else if ( BurstLeft > 0 && BurstLeft < recipe.Burst && ((!down && !recipe.CommitBurst) || MagLoaded <= 0) )
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
		if ( recipe.DoublePump )
		{
			doubleRecipe = recipe;
			doubleVolley = Live.Count > 0 ? Live[^1].Flight.Volley : null;
			if ( doubleVolley is not null )
				doubleVolley.Hold = true;
			doubleLeft = MathF.Max( 0.05f, GameSettings.Traits.DoubleGap );
			return;
		}

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

		if ( dashArc > 0.001f )
			CatchDashed( playerBefore, playerArc, dashArc, track );

		var player = Runner.Angle;
		for ( var i = Dropped.Count - 1; i >= 0; i-- )
		{
			var drop = Dropped[i];
			if ( !drop.IsValid() )
			{
				Dropped.RemoveAt( i );
				continue;
			}

			drop.Roll( player, dashArc, track );
		}
	}

	void CatchDashed( float playerBefore, float playerArc, float dashArc, float track )
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
				if ( gap >= bestGap || !drop.Crosses( gap, playerArc, dashArc, track ) )
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
					if ( drop.Crosses( gap, playerArc, dashArc, track ) )
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
		var go = Loop.Scene.CreateObject();
		go.Name = "Laser Beam";
		beam = go.AddComponent<LaserBeam>();
		beam.Arm( Loop, recipe );
		if ( Aim.IsValid() )
			beam.Aim( Aim.Muzzle, Aim.Direction, recipe );
		Loop.NoteShot();
		ArenaSounds.Fire( Aim.IsValid() ? Aim.MuzzleWorld : Vector3.Zero );
	}

	void EndBeam( GunRecipe recipe )
	{
		var held = BeamHeld;
		var origin = Runner.IsValid() ? Runner.Flat : Vector2.Zero;
		StopBeam();
		DropSpent( origin );
		BeginReload( recipe.BeamPad + recipe.BeamPerSecond * held + recipe.BoreWait );
	}

	void StopBeam()
	{
		Beaming = false;
		BeamHeld = 0f;
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

		if ( spendMag )
		{
			if ( MagLoaded <= 0 )
				return;

			MagLoaded--;
		}

		var count = Math.Max( 1, recipe.Count );
		var cone = recipe.Cone;
		if ( volleyIndex > 0 && recipe.Sight )
			cone *= 0.5f;
		if ( recipe.WalkStep > 0f )
			cone += volleyIndex * recipe.WalkStep;
		var reach = 0f;
		if ( recipe.PointAim )
			reach = (Aim.Cursor - Aim.Muzzle).Length;
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
			if ( recipe.PointAim )
			{
				flight.PointAim = true;
				flight.Mark = Aim.Muzzle + heading * reach;
			}

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
		Energy = recipe.Energy,
		SpeedScale = recipe.SpeedScale,
		SpinSpeed = recipe.SpinSpeed,
		ExplosiveRadius = recipe.Splash,
		SplashDamage = recipe.SplashDamage,
		FriendlySplash = recipe.FriendlySplash,
		IgnoreArmor = recipe.IgnoreArmor,
		RampPierce = recipe.RampPierce,
		Falloff = recipe.Falloff,
		MeatRange = recipe.MeatRange,
		MeatBonus = recipe.MeatBonus,
		KickForce = recipe.KickForce,
		KickRange = recipe.KickRange,
		StunTime = recipe.StunTime,
		StunRange = recipe.StunRange,
		StickTime = recipe.StickTime,
		Nail = recipe.Nail,
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

	protected override void OnUpdate()
	{
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
