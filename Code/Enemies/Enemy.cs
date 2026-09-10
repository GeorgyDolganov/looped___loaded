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
	static readonly Color HurtTint = new Color( 1f, 0.9f, 0.6f );

	GameObject body;
	GameObject shieldPlate;
	PolyLine outline;
	PolyLine hpBack;
	PolyLine hpFill;
	SkinnedModelRenderer terry;
	EnemyKind builtKind;
	Vector2 moveVelocity;
	float hitAt = -99f;
	readonly List<(ModelRenderer Renderer, Color Tint)> dressed = new();
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
		markUntil = 0f;
		shotAt = Time.Now + Game.Random.Float( 0.12f, 0.45f );
		telegraphUntil = 0f;
		Radius = kind switch
		{
			EnemyKind.Core => Arena.IsValid() ? Arena.Geometry.CoreRadius : 230f,
			EnemyKind.Shield => 72f,
			_ => 64f
		};

		if ( Arena.IsValid() && Kind != EnemyKind.Core )
		{
			var pos = Flat;
			Arena.Geometry.Eject( ref pos, Radius, true, pos );
			Flat = pos;
		}

		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
		RebuildVisuals();

		if ( body.IsValid() )
			body.Enabled = true;
	}

	public bool Marked => Time.Now < markUntil;

	float markUntil;

	public bool BlocksFrom( Vector2 incoming, RoundProjectile source )
	{
		if ( Kind == EnemyKind.Core )
			return source is null || source.Ricochets <= 0;

		if ( Kind != EnemyKind.Shield )
			return false;

		if ( source is not null && source.ConsumeShred() )
			return false;

		if ( !Loop.IsValid() || !Loop.Runner.IsValid() )
			return false;

		var facing = ShieldFacing;
		return ArenaGeometry.Dot( incoming, facing ) < -0.22f;
	}

	public void Mark( float duration )
	{
		if ( Kind == EnemyKind.Core || duration <= 0f )
			return;

		markUntil = MathF.Max( markUntil, Time.Now + duration );
	}

	public void Shove( Vector2 delta )
	{
		if ( !Alive || Kind == EnemyKind.Core || delta.Length < 0.01f || !Arena.IsValid() )
			return;

		var pos = Flat;
		Arena.Geometry.MoveBody( ref pos, delta, Radius, true );
		var length = pos.Length;
		var inner = Arena.Geometry.CoreRadius + 160f;
		var track = Arena.Geometry.TrackRadius - 28f;
		if ( length < 1f )
			pos = Vector2.Right * inner;
		else if ( length < inner )
			pos = pos.Normal * inner;
		else if ( length > track )
			pos = pos.Normal * track;

		Flat = pos;
		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
	}

	public void Damage( int amount, float freezeDuration, float freezeScale )
	{
		if ( freezeDuration > 0f )
		{
			freezeUntil = MathF.Max( freezeUntil, Time.Now + freezeDuration );
			this.freezeScale = MathF.Min( this.freezeScale, freezeScale <= 0.01f ? 1f : freezeScale );
		}

		Damage( amount, null );
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
		ArenaSounds.Hit();
		ArenaSounds.Flesh( world );
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
		ArenaSounds.Explode( world );
		ImpactFlash.Spawn( Scene, world, LiveTint, Kind == EnemyKind.Core ? 4.5f : 2.2f );

		if ( !Loop.IsValid() )
			return;

		if ( Kind == EnemyKind.Core )
			Loop.BeatBoss();
		else
			Loop.RegisterKill();
	}

	Vector2 LookFlat
	{
		get
		{
			if ( Kind == EnemyKind.Core )
				return ArenaGeometry.FromAngle( Time.Now * 0.35f );

			if ( Kind == EnemyKind.Shield )
				return ShieldFacing;

			if ( moveVelocity.Length > 8f )
				return moveVelocity.Normal;

			if ( Loop.IsValid() && Loop.Runner.IsValid() )
			{
				var to = Loop.Runner.Flat - Flat;
				if ( to.Length > 1f )
					return to.Normal;
			}

			return new Vector2( -1f, 0f );
		}
	}

	void PaintDressed( float flash, bool frozen )
	{
		if ( !body.IsValid() )
			return;

		var renderers = body.GetComponentsInChildren<ModelRenderer>( true );
		if ( dressed.Count != renderers.Count() )
		{
			dressed.Clear();
			foreach ( var renderer in renderers )
				dressed.Add( (renderer, renderer.Tint) );
		}

		var overlay = HurtTint;
		var blend = flash;
		if ( frozen )
		{
			overlay = new Color( 0.45f, 0.9f, 1f );
			blend = MathF.Max( blend, 0.55f );
		}

		if ( Kind == EnemyKind.Shooter && telegraphUntil > Time.Now )
		{
			overlay = Color.White;
			blend = MathF.Max( blend, 0.45f );
		}

		foreach ( var mesh in dressed )
		{
			if ( !mesh.Renderer.IsValid() )
				continue;

			var painted = Color.Lerp( mesh.Tint, LiveTint, 0.88f );
			mesh.Renderer.Tint = Color.Lerp( painted, overlay, blend );
		}
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
		var before = Flat;

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
				var want = ArenaGeometry.FromAngle( angle ) * radius;
				var travel = (want - Flat).Length;
				var pace = Time.Delta > 0.0001f ? travel / Time.Delta : 160f;
				Seek( want, MathF.Max( 120f, pace ), inner, Arena.Geometry.TrackInner - 40f );
				break;
			}
		}

		moveVelocity = Time.Delta > 0.0001f ? (Flat - before) / Time.Delta : Vector2.Zero;
		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
	}

	void Seek( Vector2 goal, float speed, float minRadius, float maxRadius )
	{
		var geo = Arena.Geometry;
		var from = Flat;
		var pos = Flat;
		var to = goal - pos;
		if ( to.Length > 6f )
		{
			var dir = geo.SteerAround( pos, to.Normal, goal, Radius );
			if ( dir.Length > 0.01f )
				geo.MoveBody( ref pos, dir.Normal * (speed * Time.Delta), Radius, true );
		}

		var length = pos.Length;
		if ( length < 1f )
			pos = Vector2.Right * minRadius;
		else if ( length < minRadius )
			pos = pos.Normal * minRadius;
		else if ( length > maxRadius )
			pos = pos.Normal * maxRadius;

		geo.Eject( ref pos, Radius, true, from );
		Flat = pos;
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

		var look = LookFlat;
		if ( look.Length > 0.1f && body.IsValid() )
			body.WorldRotation = Blocks.FlatFacing( look );

		if ( terry.IsValid() )
		{
			var hold = Kind == EnemyKind.Shooter ? 1 : Kind == EnemyKind.Shield ? 5 : 0;
			TerryLook.Drive( terry, new Vector3( moveVelocity.x, moveVelocity.y, 0f ), new Vector3( look.x, look.y, 0f ), hold );
		}

		PaintDressed( flash, frozen );

		if ( shieldPlate.IsValid() )
		{
			var facing = ShieldFacing;
			shieldPlate.WorldPosition = WorldPosition + new Vector3( facing.x, facing.y, 0f ) * 58f + Vector3.Up * 120f;
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
		body.LocalPosition = Vector3.Zero;
		body.LocalRotation = Rotation.Identity;

		dressed.Clear();
		terry = TerryLook.Attach( body, false, Kind == EnemyKind.Core ? TerryLook.CoreScale : TerryLook.BodyScale );

		if ( Kind == EnemyKind.Shield )
		{
			shieldPlate = Blocks.SpawnBox( GameObject, "Shield", WorldPosition + Vector3.Up * 120f, Rotation.Identity, new Vector3( 28f, 160f, 220f ), ShieldTint * 1.3f );
		}

		var tint = LiveTint;

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
		var head = TerryLook.Height( Kind == EnemyKind.Core ) + 24f;
		var half = Kind == EnemyKind.Core ? 110f : 48f;
		var center = WorldPosition + Vector3.Up * head - rot.Forward * 28f;
		var left = center - rot.Right * half;
		var right = center + rot.Right * half;

		hpBack.HeadTint = new Color( 0.07f, 0.09f, 0.11f );
		hpBack.TailTint = hpBack.HeadTint;
		hpBack.Apply();
		hpBack.SetPoints( new List<Vector3> { left, right } );

		if ( Health <= 0 || ratio <= 0f )
		{
			hpFill.Clear();
			return;
		}

		hpFill.HeadTint = tint;
		hpFill.TailTint = tint;
		hpFill.Apply();
		hpFill.SetPoints( new List<Vector3> { left, right - rot.Right * (half * 2f * (1f - ratio)) } );
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
