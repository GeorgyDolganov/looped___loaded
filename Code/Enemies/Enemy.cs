namespace LoopedLoaded;

public enum EnemyKind
{
	Chaser,
	Shield,
	Shooter,
	Core
}

public sealed class Enemy : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public GameLoop Loop { get; set; }

	public EnemyKind Kind { get; private set; }
	public bool Alive { get; private set; }
	public int Health { get; private set; }
	public float Radius { get; private set; } = 52f;
	public Vector2 Flat { get; private set; }

	static readonly Color ChaserTint = new Color( 0.95f, 0.28f, 0.22f );
	static readonly Color ShieldTint = new Color( 0.55f, 0.72f, 0.95f );
	static readonly Color ShooterTint = new Color( 1f, 0.55f, 0.18f );
	static readonly Color CoreTint = new Color( 0.78f, 0.18f, 0.32f );
	static readonly Color ArmorTint = new Color( 0.55f, 0.62f, 0.78f );
	static readonly Color HurtTint = new Color( 1f, 0.9f, 0.6f );

	GameObject body;
	GameObject shieldPlate;
	PolyLine outline;
	PolyLine hpBack;
	PolyLine hpFill;
	EnemyKind builtKind;
	float hitAt = -99f;
	float freezeUntil;
	float freezeScale = 1f;
	float shotAt = -99f;
	float telegraphUntil;

	public Color LiveTint => Kind switch
	{
		EnemyKind.Shield => ShieldTint,
		EnemyKind.Shooter => ShooterTint,
		EnemyKind.Core => CoreTint,
		_ => ChaserTint
	};

	public bool Frozen => Time.Now < freezeUntil;
	public float SlowScale => Frozen ? freezeScale : 1f;
	public int MaxHealth { get; private set; }

	public void Setup( EnemyKind kind, Vector2 flat, int health )
	{
		Kind = kind;
		Flat = flat;
		Health = health;
		MaxHealth = health;
		Alive = true;
		freezeUntil = 0f;
		freezeScale = 1f;
		shotAt = Time.Now + Game.Random.Float( 0.12f, 0.45f );
		telegraphUntil = 0f;
		Radius = kind switch
		{
			EnemyKind.Core => Arena.IsValid() ? Arena.Geometry.CoreRadius : 230f,
			EnemyKind.Shield => 62f,
			_ => 52f
		};

		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
		RebuildVisuals();

		if ( body.IsValid() )
			body.Enabled = true;
	}

	public bool BlocksFrom( Vector2 incoming, RoundProjectile source )
	{
		if ( Kind == EnemyKind.Core )
			return source is null || source.Ricochets <= 0;

		if ( Kind != EnemyKind.Shield || !Loop.IsValid() || !Loop.Runner.IsValid() )
			return false;

		var facing = ShieldFacing;
		return ArenaGeometry.Dot( incoming, facing ) < -0.22f;
	}

	public void Damage( int amount, RoundProjectile source )
	{
		if ( !Alive )
			return;

		Health -= amount;
		hitAt = Time.Now;

		if ( source is not null && source.Flight.FreezeDuration > 0f )
		{
			freezeUntil = MathF.Max( freezeUntil, Time.Now + source.Flight.FreezeDuration );
			freezeScale = MathF.Min( freezeScale, source.Flight.FreezeScale );
		}

		var world = Arena.Geometry.ToPlayWorld( Flat );
		Sound.Play( "sounds/impacts/bullets/impact-bullet-flesh.sound", world );
		ImpactFlash.Spawn( Scene, world, HurtTint, 1.1f );

		if ( Health > 0 )
			return;

		Die();
	}

	void Die()
	{
		Alive = false;

		if ( body.IsValid() )
			body.Enabled = false;

		outline?.Clear();
		hpBack?.Clear();
		hpFill?.Clear();
		shieldPlate?.Destroy();

		var world = Arena.Geometry.ToPlayWorld( Flat );
		Sound.Play( "sounds/effects/explosion/explosion_small.sound", world );
		ImpactFlash.Spawn( Scene, world, LiveTint, Kind == EnemyKind.Core ? 4.5f : 2.2f );

		if ( !Loop.IsValid() )
			return;

		if ( Kind == EnemyKind.Core )
			Loop.BeatBoss();
		else
			Loop.RegisterKill();
	}

	Vector2 ShieldFacing
	{
		get
		{
			if ( Loop.IsValid() && Loop.Runner.IsValid() )
			{
				var toPlayer = Loop.Runner.Flat - Flat;
				if ( toPlayer.Length > 1f )
					return toPlayer.Normal;
			}

			return Flat.Length > 1f ? Flat.Normal : Vector2.Up;
		}
	}

	protected override void OnUpdate()
	{
		if ( !Arena.IsValid() || !Alive )
			return;

		if ( Loop.IsValid() && Loop.IsFrozen )
		{
			Paint();
			return;
		}

		Move();
		ThinkShoot();
		Paint();
	}

	public void ShiftTime( float dt )
	{
		hitAt += dt;
		shotAt += dt;
		if ( freezeUntil > 0f )
			freezeUntil += dt;
		if ( telegraphUntil > 0f )
			telegraphUntil += dt;
	}

	float Pressure => Loop.IsValid() ? Loop.Threat : 1f;

	void Move()
	{
		if ( !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		var frozen = Time.Now < freezeUntil;
		var scale = frozen ? freezeScale : 1f;
		var inner = Arena.Geometry.CoreRadius + 160f;
		var track = Arena.Geometry.TrackRadius - 28f;

		switch ( Kind )
		{
			case EnemyKind.Core:
				break;
			case EnemyKind.Chaser:
			{
				var lead = Loop.Runner.Tangent * (160f + 50f * Pressure);
				Seek( Loop.Runner.Flat + lead, 185f * Pressure * scale, inner, track );
				break;
			}
			case EnemyKind.Shield:
			{
				Seek( Loop.Runner.Flat, 170f * Pressure * scale, inner, Arena.Geometry.TrackRadius + 8f );
				break;
			}
			case EnemyKind.Shooter:
			{
				var orbit = 0.34f * Pressure * scale * Time.Delta;
				var angle = ArenaGeometry.ToAngle( Flat ) - orbit;
				var radius = MathX.Lerp( inner + 30f, Arena.Geometry.TrackInner - 140f, 0.42f );
				Flat = ArenaGeometry.FromAngle( angle ) * radius;
				break;
			}
		}

		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
	}

	void Seek( Vector2 goal, float speed, float minRadius, float maxRadius )
	{
		var to = goal - Flat;
		if ( to.Length > 6f )
			Flat += to.Normal * speed * Time.Delta;

		var length = Flat.Length;
		if ( length < 1f )
		{
			Flat = Vector2.Right * minRadius;
			return;
		}

		if ( length < minRadius )
			Flat = Flat.Normal * minRadius;
		else if ( length > maxRadius )
			Flat = Flat.Normal * maxRadius;
	}

	void ThinkShoot()
	{
		if ( Kind != EnemyKind.Shooter || !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		var interval = MathF.Max( 0.8f, 1.65f / Pressure );
		var telegraph = MathF.Max( 0.14f, 0.28f / MathF.Sqrt( Pressure ) );

		if ( Time.Now < telegraphUntil )
			return;

		if ( Time.Now >= shotAt && telegraphUntil <= 0f )
		{
			telegraphUntil = Time.Now + telegraph;
			return;
		}

		if ( telegraphUntil > 0f && Time.Now >= telegraphUntil )
		{
			telegraphUntil = 0f;
			shotAt = Time.Now + interval;
			EnemyShot.Fire( Loop, Flat, LeadDirection(), 560f * Pressure );
		}
	}

	Vector2 LeadDirection()
	{
		var runner = Loop.Runner;
		var to = runner.Flat - Flat;
		var dist = MathF.Max( 80f, to.Length );
		var shotSpeed = 560f * Pressure;
		var travel = dist / shotSpeed;
		var pace = runner.Speed;
		if ( runner.Slowing )
			pace *= runner.SlowSpeedScale;

		var predicted = runner.Flat + runner.Tangent * (pace * travel);
		var lead = predicted - Flat;
		return lead.Length > 1f ? lead.Normal : to.Normal;
	}

	void Paint()
	{
		RebuildVisuals();

		var flash = MathF.Max( 0f, 1f - (Time.Now - hitAt) * 6f );
		var frozen = Time.Now < freezeUntil;
		var tint = Color.Lerp( LiveTint, HurtTint, flash );

		if ( frozen )
			tint = Color.Lerp( tint, new Color( 0.45f, 0.9f, 1f ), 0.55f );

		if ( Kind == EnemyKind.Shooter && telegraphUntil > Time.Now )
			tint = Color.Lerp( tint, Color.White, 0.45f );

		if ( body.IsValid() )
		{
			foreach ( var renderer in body.GetComponentsInChildren<ModelRenderer>() )
				renderer.Tint = tint;
		}

		if ( shieldPlate.IsValid() )
		{
			var facing = ShieldFacing;
			shieldPlate.WorldPosition = WorldPosition + new Vector3( facing.x, facing.y, 0f ) * 36f + Vector3.Up * 70f;
			shieldPlate.WorldRotation = Blocks.FlatFacing( facing );

			var plate = shieldPlate.GetComponent<ModelRenderer>();
			if ( plate.IsValid() )
				plate.Tint = Color.Lerp( ShieldTint * 1.3f, HurtTint, flash );
		}

		if ( outline.IsValid() )
		{
			outline.HeadTint = tint;
			outline.TailTint = tint;
			outline.Apply();
			outline.SetPoints( BuildCircle() );
		}

		PaintHealth( flash );
	}

	void RebuildVisuals()
	{
		if ( body.IsValid() && builtKind == Kind )
			return;

		body?.Destroy();
		shieldPlate?.Destroy();
		outline?.GameObject?.Destroy();
		hpBack?.GameObject?.Destroy();
		hpFill?.GameObject?.Destroy();
		outline = null;
		hpBack = null;
		hpFill = null;
		builtKind = Kind;

		body = Scene.CreateObject();
		body.Name = "Enemy Body";
		body.Parent = GameObject;

		var tint = LiveTint;

		if ( Kind == EnemyKind.Core )
		{
			var span = Radius * 1.85f;
			Blocks.SpawnBox( body, "Nucleus", WorldPosition + Vector3.Up * 90f, Rotation.Identity, new Vector3( span, span, 180f ), tint );
			Blocks.SpawnBox( body, "Crown", WorldPosition + Vector3.Up * 200f, Rotation.Identity, new Vector3( span * 0.55f, span * 0.55f, 70f ), tint * 1.35f );
			Blocks.SpawnBox( body, "Armor", WorldPosition + Vector3.Up * 70f, Rotation.Identity, new Vector3( span * 1.12f, span * 1.12f, 40f ), ArmorTint );
		}
		else
		{
			Blocks.SpawnBox( body, "Torso", WorldPosition + Vector3.Up * 62f, Rotation.Identity, new Vector3( 74f, 74f, 124f ), tint );
			Blocks.SpawnBox( body, "Head", WorldPosition + Vector3.Up * 148f, Rotation.Identity, new Vector3( 44f, 44f, 44f ), tint * 1.4f );
		}

		if ( Kind == EnemyKind.Shield )
		{
			shieldPlate = Blocks.SpawnBox( GameObject, "Shield", WorldPosition + Vector3.Up * 70f, Rotation.Identity, new Vector3( 18f, 110f, 150f ), ShieldTint * 1.3f );
		}

		if ( Kind == EnemyKind.Shooter )
		{
			Blocks.SpawnBox( body, "Barrel", WorldPosition + new Vector3( 40f, 0f, 70f ), Rotation.Identity, new Vector3( 70f, 16f, 16f ), tint * 0.5f );
		}

		var outlineObject = Scene.CreateObject();
		outlineObject.Name = "Hit Circle";
		outlineObject.Parent = GameObject;

		outline = outlineObject.AddComponent<PolyLine>();
		outline.HeadWidth = 3f;
		outline.TailWidth = 3f;
		outline.HeadTint = tint;
		outline.TailTint = tint;
		outline.Apply();

		var bar = Kind == EnemyKind.Core ? 16f : 9f;
		hpBack = MakeLine( "Hp Back", bar, new Color( 0.07f, 0.09f, 0.11f ) );
		hpFill = MakeLine( "Hp Fill", bar - 2f, tint );
	}

	PolyLine MakeLine( string name, float width, Color tint )
	{
		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = GameObject;

		var line = go.AddComponent<PolyLine>();
		line.HeadWidth = width;
		line.TailWidth = width;
		line.HeadTint = tint;
		line.TailTint = tint;
		line.Apply();
		return line;
	}

	void PaintHealth( float flash )
	{
		if ( !hpBack.IsValid() || !hpFill.IsValid() )
			return;

		var ratio = MaxHealth <= 0 ? 0f : Math.Clamp( Health / (float)MaxHealth, 0f, 1f );
		var tint = Color.Lerp( HealthTint( ratio ), HurtTint, flash );
		var camera = Scene.Camera;
		var rot = camera.IsValid() ? camera.WorldRotation : Rotation.Identity;
		var lift = Kind == EnemyKind.Core ? 220f : 98f;
		var half = Kind == EnemyKind.Core ? 110f : 42f;
		var center = WorldPosition + rot.Up * lift - rot.Forward * 24f;
		var left = center - rot.Right * half;
		var right = center + rot.Right * half;

		hpBack.HeadTint = new Color( 0.07f, 0.09f, 0.11f );
		hpBack.TailTint = hpBack.HeadTint;
		hpBack.Apply();
		hpBack.SetPoints( new List<Vector3> { left, right } );

		if ( ratio <= 0.02f )
		{
			hpFill.Clear();
			return;
		}

		hpFill.HeadTint = tint;
		hpFill.TailTint = tint;
		hpFill.Apply();
		hpFill.SetPoints( new List<Vector3> { left, left + rot.Right * (half * 2f * ratio) } );
	}

	static Color HealthTint( float ratio )
	{
		if ( ratio > 0.55f )
			return Color.Lerp( new Color( 1f, 0.72f, 0.22f ), new Color( 0.35f, 0.9f, 0.45f ), (ratio - 0.55f) / 0.45f );

		return Color.Lerp( new Color( 0.95f, 0.28f, 0.22f ), new Color( 1f, 0.72f, 0.22f ), ratio / 0.55f );
	}

	List<Vector3> BuildCircle()
	{
		const int segments = 20;
		var points = new List<Vector3>( segments + 1 );

		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			points.Add( Arena.Geometry.ToWorld( Flat + ArenaGeometry.FromAngle( angle ) * Radius, 14f ) );
		}

		return points;
	}
}
