namespace LoopedLoaded;

public enum EnemyKind
{
	Chaser,
	Shield,
	Shooter
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
	static readonly Color HurtTint = new Color( 1f, 0.9f, 0.6f );

	GameObject body;
	GameObject shieldPlate;
	PolyLine outline;
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
		_ => ChaserTint
	};

	public void Setup( EnemyKind kind, Vector2 flat, int health )
	{
		Kind = kind;
		Flat = flat;
		Health = health;
		Alive = true;
		freezeUntil = 0f;
		freezeScale = 1f;
		shotAt = Time.Now + Game.Random.Float( 0.4f, 1.4f );
		telegraphUntil = 0f;
		Radius = kind == EnemyKind.Shield ? 62f : 52f;

		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
		RebuildVisuals();

		if ( body.IsValid() )
			body.Enabled = true;
	}

	public bool BlocksFrom( Vector2 incoming )
	{
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

		var world = Arena.Geometry.ToPlayWorld( Flat );
		Sound.Play( "sounds/effects/explosion/explosion_small.sound", world );
		ImpactFlash.Spawn( Scene, world, LiveTint, 2.2f );

		if ( Loop.IsValid() )
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

	void Move()
	{
		var frozen = Time.Now < freezeUntil;
		var scale = frozen ? freezeScale : 1f;
		var inner = Arena.Geometry.CoreRadius + 170f;
		var outer = Arena.Geometry.TrackRadius - 28f;

		switch ( Kind )
		{
			case EnemyKind.Chaser:
			{
				var outward = Flat.Length < 1f ? Vector2.Right : Flat.Normal;
				Flat += outward * (95f * scale * Time.Delta);
				if ( Flat.Length > outer )
					Flat = Flat.Normal * outer;
				break;
			}
			case EnemyKind.Shooter:
			{
				var orbit = 0.18f * scale * Time.Delta;
				var angle = ArenaGeometry.ToAngle( Flat ) - orbit;
				var radius = MathX.Lerp( inner + 40f, Arena.Geometry.TrackInner - 180f, 0.35f );
				Flat = ArenaGeometry.FromAngle( angle ) * radius;
				break;
			}
		}

		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
	}

	void ThinkShoot()
	{
		if ( Kind != EnemyKind.Shooter || !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		var interval = 2.35f;

		if ( Time.Now < telegraphUntil )
			return;

		if ( Time.Now >= shotAt && telegraphUntil <= 0f )
		{
			telegraphUntil = Time.Now + 0.38f;
			return;
		}

		if ( telegraphUntil > 0f && Time.Now >= telegraphUntil )
		{
			telegraphUntil = 0f;
			shotAt = Time.Now + interval;
			EnemyShot.Fire( Loop, Flat, (Loop.Runner.Flat - Flat).Normal, LiveTint );
		}
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
	}

	void RebuildVisuals()
	{
		if ( body.IsValid() && builtKind == Kind )
			return;

		body?.Destroy();
		shieldPlate?.Destroy();
		outline?.GameObject?.Destroy();
		outline = null;
		builtKind = Kind;

		body = Scene.CreateObject();
		body.Name = "Enemy Body";
		body.Parent = GameObject;

		var tint = LiveTint;
		Blocks.SpawnBox( body, "Torso", WorldPosition + Vector3.Up * 62f, Rotation.Identity, new Vector3( 74f, 74f, 124f ), tint );
		Blocks.SpawnBox( body, "Head", WorldPosition + Vector3.Up * 148f, Rotation.Identity, new Vector3( 44f, 44f, 44f ), tint * 1.4f );

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
