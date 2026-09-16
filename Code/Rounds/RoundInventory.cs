namespace LoopedLoaded;

public sealed class RoundInventory : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public GameLoop Loop { get; set; }

	public RunLoadout Loadout { get; } = new();
	public List<RoundProjectile> Live { get; } = new();
	public float ReloadLeft { get; private set; }
	public float ReloadFor { get; private set; }
	public int BurstLeft { get; private set; }
	public bool Beaming { get; private set; }
	public float BeamHeld { get; private set; }
	public bool Ready => ReloadLeft <= 0.001f && !Beaming;
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

	public void ResetLoadout()
	{
		ClearShots();
		Loadout.Clear();
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
	}

	public void ChamberAll()
	{
		ClearShots();
		ReloadLeft = 0f;
		ReloadFor = 0f;
		BurstLeft = 0;
		cycleLeft = 0f;
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
			if ( down && Ready && cycleLeft <= 0.001f )
			{
				if ( BurstLeft <= 0 )
					BurstLeft = recipe.Burst;

				FireVolley( recipe );
				BurstLeft--;
				cycleLeft = recipe.Cycle;
				if ( BurstLeft <= 0 )
					BeginReload( recipe.Reload );
			}
			else if ( !down && BurstLeft > 0 && BurstLeft < recipe.Burst )
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

	void BeginReload( float duration )
	{
		ReloadFor = MathF.Max( GameSettings.Traits.ReloadMin, duration );
		ReloadLeft = ReloadFor;
		BurstLeft = 0;
	}

	void BeginBeam( GunRecipe recipe )
	{
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
		StopBeam();
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

	void FireVolley( GunRecipe recipe )
	{
		if ( !Loop.IsValid() || !Aim.IsValid() )
			return;

		var count = Math.Max( 1, recipe.Count );
		var cone = recipe.Cone;
		for ( var i = 0; i < count; i++ )
		{
			var yaw = 0f;
			if ( count > 1 && cone > 0.01f )
				yaw = -cone * 0.5f + cone * i / (count - 1);

			var go = Loop.Scene.CreateObject();
			go.Name = "Shot";
			var projectile = go.AddComponent<RoundProjectile>();
			projectile.Radius = recipe.Radius;
			projectile.Launch( Loop, ToFlight( recipe ), Aim.Muzzle, Turn( Aim.Direction, yaw ) );
			Live.Add( projectile );
		}

		Loop.NoteShot();
		ArenaSounds.Fire( Aim.MuzzleWorld );
		ImpactFlash.Spawn( Loop.Scene, Aim.MuzzleWorld, ShotColors.Player, recipe.Nail ? 0.55f : 0.8f );
	}

	static RoundFlight ToFlight( GunRecipe recipe ) => new()
	{
		Tint = ShotColors.Player,
		Damage = recipe.Damage,
		PierceCharges = recipe.Pierce,
		MaxBounces = recipe.Bounces,
		Energy = recipe.Energy,
		SpeedScale = recipe.SpeedScale,
		ExplosiveRadius = recipe.Splash,
		FriendlySplash = recipe.FriendlySplash,
		Falloff = recipe.Falloff,
		StickTime = recipe.StickTime,
		Nail = recipe.Nail
	};

	static Vector2 Turn( Vector2 dir, float degrees )
	{
		if ( MathF.Abs( degrees ) < 0.01f )
			return dir.Normal;

		var ang = MathX.DegreeToRadian( degrees );
		var c = MathF.Cos( ang );
		var s = MathF.Sin( ang );
		return new Vector2( dir.x * c - dir.y * s, dir.x * s + dir.y * c ).Normal;
	}

	void Prune()
	{
		for ( var i = Live.Count - 1; i >= 0; i-- )
		{
			if ( !Live[i].IsValid() )
				Live.RemoveAt( i );
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
