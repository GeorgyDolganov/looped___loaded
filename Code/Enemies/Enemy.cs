namespace LoopedLoaded;

public enum EnemyKind
{
	Chaser,
	Shield,
	Shooter,
	Core,
	Splinter,
	Glimmer,
	Shardguard,
	Lens
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
	static readonly Color SplinterTint = new Color( 0.85f, 0.55f, 0.95f );
	static readonly Color GlimmerTint = new Color( 0.62f, 0.95f, 0.9f );
	static readonly Color ShardguardTint = new Color( 0.75f, 0.9f, 1f );
	static readonly Color LensTint = new Color( 0.55f, 0.85f, 0.95f );
	static readonly Color GlassCrackPlate = new Color( 0.94f, 0.98f, 1f );
	static readonly Color HurtTint = new Color( 1f, 0.9f, 0.6f );
	static readonly Color BarBack = new Color( 0.07f, 0.055f, 0.042f );
	static readonly Color BarHigh = new Color( 0.36f, 0.58f, 0.22f );
	static readonly Color BarMid = new Color( 0.86f, 0.64f, 0.2f );
	static readonly Color BarLow = new Color( 0.72f, 0.17f, 0.11f );

	const float BarInset = 3f;
	const int PipLimit = 10;
	const string ShieldModelPath = "models/sheild.vmdl";
	const float ShieldHeight = 220f;
	const float ShieldGap = 36f;

	GameObject body;
	GameObject shieldPlate;
	ModelRenderer shieldMesh;
	float shieldReach = 58f;
	float shieldLift = 120f;
	PolyLine outline;
	PolyLine hpBack;
	PolyLine hpFill;
	readonly List<PolyLine> hpPips = new();
	SkinnedModelRenderer hobo;
	EnemyKind builtKind;
	float headTop;
	float barWidth;
	Vector2 moveVelocity;
	float hitAt = -99f;
	readonly List<(ModelRenderer Renderer, Color Tint)> dressed = new();
	readonly List<Vector3> circle = new( 21 );
	readonly List<Vector3> span = new( 2 );
	float freezeUntil;
	float freezeScale = 1f;
	float shotAt = -99f;
	float telegraphUntil;
	float attackUntil;
	float attackReadyAt;
	int plateHits;
	Vector2 shootFlank;
	float flankUntil;
	Vector2 lastImpulse = Vector2.Right;
	readonly EnemyDrive drive = new();

	bool Melee => Kind is EnemyKind.Chaser or EnemyKind.Splinter or EnemyKind.Glimmer
		or EnemyKind.Shield or EnemyKind.Shardguard;

	public Color LiveTint => Kind switch
	{
		EnemyKind.Shield => ShieldTint,
		EnemyKind.Shooter => ShooterTint,
		EnemyKind.Core => CoreTint,
		EnemyKind.Splinter => SplinterTint,
		EnemyKind.Glimmer => GlimmerTint,
		EnemyKind.Shardguard => ShardguardTint,
		EnemyKind.Lens => LensTint,
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
		attackUntil = 0f;
		attackReadyAt = 0f;
		plateHits = 0;
		shootFlank = Vector2.Zero;
		flankUntil = 0f;
		Radius = kind switch
		{
			EnemyKind.Core => Arena.IsValid() ? Arena.Geometry.CoreRadius : 230f,
			EnemyKind.Lens => GameSettings.Boss.LensRadius,
			_ => GameSettings.Enemies.RadiusOf( kind )
		};

		if ( Arena.IsValid() && Kind != EnemyKind.Core && Kind != EnemyKind.Lens )
		{
			var pos = Flat;
			Arena.Geometry.Eject( ref pos, Radius, true, pos );
			Flat = pos;
		}

		var toPlayer = Loop.IsValid() && Loop.Runner.IsValid() ? Loop.Runner.Flat - Flat : -Flat;
		drive.Reset( toPlayer );
		moveVelocity = Vector2.Zero;
		lastImpulse = Vector2.Right;
		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
		RebuildVisuals();

		if ( body.IsValid() )
			body.Enabled = true;
	}

	public bool Marked => Time.Now < markUntil;

	float markUntil;

	public bool BlocksFrom( Vector2 incoming, RoundProjectile source, bool asBounce = false, Vector2 from = default )
	{
		if ( Kind == EnemyKind.Core )
		{
			if ( source is not null && source.Flight.IgnoreArmor )
				return false;

			return !asBounce && (source is null || source.Ricochets <= 0);
		}

		if ( Kind == EnemyKind.Lens )
			return BlocksLens( incoming, source, from );

		if ( Kind != EnemyKind.Shield && Kind != EnemyKind.Shardguard )
			return false;

		if ( source is not null && source.Flight.IgnoreArmor )
			return false;

		if ( !Loop.IsValid() || !Loop.Runner.IsValid() )
			return false;

		var facing = ShieldFacing;
		if ( ArenaGeometry.Dot( incoming, facing ) >= -0.22f )
			return false;

		if ( Kind != EnemyKind.Shardguard )
			return true;

		if ( plateHits <= 0 )
		{
			plateHits = 1;
			ArenaSounds.Crack( Arena.IsValid() ? Arena.Geometry.ToPlayWorld( Flat ) : WorldPosition );
			return true;
		}

		return false;
	}

	bool BlocksLens( Vector2 incoming, RoundProjectile source, Vector2 fromHint )
	{
		var lens = GetComponent<ArenaLens>();
		if ( lens is not null && !lens.Open )
			return true;

		var fromCenter = fromHint.Length > 1f
			? fromHint
			: source is not null && source.Flat.Length > 1f ? source.Flat : incoming;
		var radial = fromCenter.Length > 1f ? fromCenter.Normal : Vector2.Right;
		return MathF.Abs( ArenaGeometry.Dot( incoming.Normal, radial ) ) > GameSettings.Boss.LensBlock;
	}

	public void Mark( float duration )
	{
		if ( Locations.IsBoss( Kind ) || duration <= 0f )
			return;

		markUntil = MathF.Max( markUntil, Time.Now + duration );
	}

	public void Stun( float duration )
	{
		if ( !Alive || Locations.IsBoss( Kind ) || duration <= 0.01f )
			return;

		freezeUntil = MathF.Max( freezeUntil, Time.Now + duration );
		freezeScale = MathF.Min( freezeScale, 0.3f );
		shotAt = MathF.Max( shotAt, Time.Now + duration );
		telegraphUntil = 0f;
		attackUntil = MathF.Max( attackUntil, Time.Now + duration );
	}

	public void Shove( Vector2 delta )
	{
		if ( !Alive || Locations.IsBoss( Kind ) || delta.Length < 0.01f || !Arena.IsValid() )
			return;

		var pos = Flat;
		Arena.Geometry.MoveBody( ref pos, delta, Radius, true );
		var length = pos.Length;
		var inner = Arena.Geometry.CoreRadius + GameSettings.Enemies.ShoveInnerPad;
		var track = Arena.Geometry.TrackRadius - GameSettings.Enemies.TrackPad;
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

		if ( source is not null )
		{
			var away = Flat - source.Flat;
			if ( away.Length > 1f )
				lastImpulse = away.Normal;
		}
		else if ( Loop.IsValid() && Loop.Runner.IsValid() )
		{
			var away = Flat - Loop.Runner.Flat;
			if ( away.Length > 1f )
				lastImpulse = away.Normal;
		}

		if ( source is not null && source.Flight.FreezeDuration > 0f )
		{
			freezeUntil = MathF.Max( freezeUntil, Time.Now + source.Flight.FreezeDuration );
			freezeScale = MathF.Min( freezeScale, source.Flight.FreezeScale );
		}

		var world = Arena.Geometry.ToPlayWorld( Flat );
		ArenaSounds.Hit();

		if ( Health <= 0 )
		{
			Die();
			return;
		}

		ArenaSounds.Flesh( world );
		ImpactFlash.Spawn( Scene, world, HurtTint, 1.1f );
	}

	void Die()
	{
		Alive = false;

		GibChunk.Burst( Loop, Scene, hobo, WorldPosition, lastImpulse, LiveTint, Radius );

		if ( body.IsValid() )
			body.Enabled = false;

		outline?.Clear();
		ClearBar();
		shieldPlate?.Destroy();

		var world = Arena.Geometry.ToPlayWorld( Flat );
		ArenaSounds.Explode( world );
		ImpactFlash.Spawn( Scene, world, LiveTint, Locations.IsBoss( Kind ) ? 4.5f : 2.2f );

		if ( !Loop.IsValid() )
			return;

		Loop.Retire( this );

		if ( Locations.IsBoss( Kind ) )
			Loop.BeatBoss();
		else
			Loop.RegisterKill( Kind, Flat );
	}

	Vector2 LookFlat
	{
		get
		{
			if ( Kind == EnemyKind.Core || Kind == EnemyKind.Lens )
				return ArenaGeometry.FromAngle( Time.Now * 0.35f );

			if ( Kind == EnemyKind.Shield || Kind == EnemyKind.Shardguard )
				return ShieldFacing;

			if ( moveVelocity.Length > 12f )
				return drive.Look;

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

		if ( dressed.Count == 0 )
		{
			foreach ( var renderer in body.GetComponentsInChildren<ModelRenderer>( true ) )
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

			mesh.Renderer.Tint = Color.Lerp( mesh.Tint, overlay, blend );
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
		ThinkMelee();
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
		if ( attackUntil > 0f )
			attackUntil += dt;
		if ( attackReadyAt > 0f )
			attackReadyAt += dt;
		if ( flankUntil > 0f )
			flankUntil += dt;

		drive.Shift( dt );
	}

	float Pressure => Loop.IsValid() ? Loop.Threat : 1f;

	void Move()
	{
		if ( !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		var frozen = Time.Now < freezeUntil;
		var scale = frozen ? freezeScale : 1f;
		var cfg = GameSettings.Enemies;
		var inner = Arena.Geometry.CoreRadius + cfg.InnerPad;
		var track = Arena.Geometry.TrackRadius - cfg.TrackPad;

		switch ( Kind )
		{
			case EnemyKind.Core:
			case EnemyKind.Lens:
				break;
			case EnemyKind.Chaser:
			case EnemyKind.Splinter:
			{
				var stats = cfg.Of( Kind );
				var lead = Loop.Runner.Tangent * (stats.Lead + stats.LeadPressure * Pressure);
				Seek( Loop.Runner.Flat + lead, stats.SeekSpeed * Pressure * scale, inner, track );
				break;
			}
			case EnemyKind.Glimmer:
			{
				var stats = cfg.Of( Kind );
				var lead = Loop.Runner.Tangent * (stats.Lead + stats.LeadPressure * Pressure);
				Seek( Loop.Runner.Flat + lead, stats.SeekSpeed * Pressure * scale, inner, track );
				break;
			}
			case EnemyKind.Shield:
			case EnemyKind.Shardguard:
			{
				var stats = cfg.Of( Kind );
				Seek( Loop.Runner.Flat, stats.SeekSpeed * Pressure * scale, inner, Arena.Geometry.TrackRadius + cfg.ShieldTrackExtra );
				break;
			}
			case EnemyKind.Shooter:
				MoveShooter( scale, inner );
				break;
		}

		moveVelocity = drive.Velocity;
		WorldPosition = new Vector3( Flat.x, Flat.y, 0f );
	}

	void Seek( Vector2 goal, float speed, float minRadius, float maxRadius )
	{
		Flat = drive.Step( Arena.Geometry, Flat, goal, Radius, speed, minRadius, maxRadius, Loop.Enemies.Count );
	}

	void MoveShooter( float scale, float inner )
	{
		var geo = Arena.Geometry;
		var gun = GameSettings.Enemies.Shooter;
		var speed = gun.Speed * MathF.Max( gun.SpeedPressureFloor, Pressure ) * scale;
		var band = MathX.Lerp( inner + gun.BandInnerPad, geo.TrackInner - gun.BandOuterPad, gun.BandMix );

		if ( TrackFlank( geo, Loop.Runner.Flat ) )
		{
			Seek( shootFlank, speed, geo.CoreRadius + gun.FlankInnerPad, geo.TrackInner - gun.FlankOuterPad );
			return;
		}

		var ahead = ArenaGeometry.ToAngle( Flat ) - gun.Orbit * Pressure * scale;
		Seek( ArenaGeometry.FromAngle( ahead ) * band, speed, inner, geo.TrackInner - gun.OrbitOuterPad );
	}

	bool TrackFlank( ArenaGeometry geo, Vector2 player )
	{
		if ( !geo.SightBlocked( Flat, player, out var hit ) )
		{
			flankUntil = 0f;
			return false;
		}

		var held = flankUntil > 0f;
		var arrived = held && (shootFlank - Flat).Length < Radius + 20f;
		if ( held && !arrived && Time.Now < flankUntil )
			return true;

		geo.WallEnds( hit, Radius, out var a, out var b );

		if ( held && (arrived || drive.Squeezed) )
		{
			shootFlank = (a - shootFlank).Length >= (b - shootFlank).Length ? a : b;
			drive.FlipSide( Flat );
		}
		else
			shootFlank = OpenEnd( geo, player, a, b );

		flankUntil = Time.Now + GameSettings.Enemies.Shooter.FlankHold;
		return true;
	}

	Vector2 OpenEnd( ArenaGeometry geo, Vector2 player, Vector2 a, Vector2 b )
	{
		var near = (a - Flat).Length <= (b - Flat).Length ? a : b;
		var far = near == a ? b : a;
		var nearSees = !geo.SightBlocked( near, player, out _ );
		var farSees = !geo.SightBlocked( far, player, out _ );

		if ( nearSees != farSees )
			return nearSees ? near : far;

		return near;
	}

	void ThinkShoot()
	{
		if ( Kind != EnemyKind.Shooter || !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		var gun = GameSettings.Enemies.Shooter;
		var interval = MathF.Max( gun.IntervalFloor, gun.Interval / Pressure );
		var telegraph = MathF.Max( gun.TelegraphFloor, gun.Telegraph / MathF.Sqrt( Pressure ) );
		var clear = !Arena.Geometry.SightBlocked( Flat, LeadPoint(), out _ );

		if ( Time.Now < telegraphUntil )
			return;

		if ( Time.Now >= shotAt && telegraphUntil <= 0f )
		{
			if ( !clear )
				return;

			telegraphUntil = Time.Now + telegraph;
			return;
		}

		if ( telegraphUntil > 0f && Time.Now >= telegraphUntil )
		{
			telegraphUntil = 0f;
			if ( !clear )
			{
				shotAt = Time.Now;
				return;
			}

			shotAt = Time.Now + interval;
			EnemyShot.Fire( Loop, Flat, LeadDirection(), GameSettings.Enemies.Shooter.ShotSpeed * Pressure );
		}
	}

	void ThinkMelee()
	{
		if ( !Melee || !hobo.IsValid() || !Loop.IsValid() || !Loop.Runner.IsValid() )
			return;

		if ( Time.Now < freezeUntil || Time.Now < attackUntil || Time.Now < attackReadyAt )
			return;

		var reach = Radius + Loop.Runner.PlayerRadius + GameSettings.Enemies.MeleeReachPad;
		if ( (Loop.Runner.Flat - Flat).Length > reach )
			return;

		attackUntil = Time.Now + HoboLook.PlayAttack( hobo );
		attackReadyAt = attackUntil + 0.22f;
	}

	Vector2 LeadPoint()
	{
		var runner = Loop.Runner;
		var to = runner.Flat - Flat;
		var gun = GameSettings.Enemies.Shooter;
		var dist = MathF.Max( gun.LeadMinDistance, to.Length );
		var travel = dist / (gun.ShotSpeed * Pressure);
		var pace = runner.Speed;
		if ( runner.Slowing )
			pace *= runner.SlowSpeedScale;

		return runner.Flat + runner.Tangent * (pace * travel);
	}

	Vector2 LeadDirection()
	{
		var to = Loop.Runner.Flat - Flat;
		var lead = LeadPoint() - Flat;
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

		var cloaked = Kind == EnemyKind.Glimmer && !RoundNear();
		if ( body.IsValid() )
			body.Enabled = !cloaked;

		if ( cloaked )
		{
			outline?.Clear();
			ClearBar();
			if ( shieldPlate.IsValid() )
				shieldPlate.Enabled = false;
			return;
		}

		var look = LookFlat;
		if ( look.Length > 0.1f && body.IsValid() )
			body.WorldRotation = Ease( body.WorldRotation, Blocks.FlatFacing( look ), 13f );

		if ( hobo.IsValid() )
		{
			var pace = Loop.IsValid() && Loop.IsFrozen ? Vector2.Zero : moveVelocity;
			HoboLook.Drive( hobo, new Vector3( pace.x, pace.y, 0f ), new Vector3( look.x, look.y, 0f ), Time.Now < attackUntil );
		}

		PaintDressed( flash, frozen );

		if ( shieldPlate.IsValid() )
		{
			shieldPlate.Enabled = true;
			var plateRotation = Ease( shieldPlate.WorldRotation, Blocks.FlatFacing( ShieldFacing ), 10f );
			var facing = plateRotation.Forward;
			shieldPlate.WorldPosition = WorldPosition + facing.WithZ( 0f ) * shieldReach + Vector3.Up * shieldLift;
			shieldPlate.WorldRotation = plateRotation;

			if ( shieldMesh.IsValid() )
			{
				var baseTint = Kind == EnemyKind.Shardguard
					? Color.Lerp( Color.White, plateHits > 0 ? GlassCrackPlate : ShardguardTint, plateHits > 0 ? 0.35f : 0.45f )
					: Color.White;
				shieldMesh.Tint = Color.Lerp( baseTint, HurtTint, flash );
			}
		}

		if ( outline.IsValid() )
		{
			outline.HeadTint = tint;
			outline.TailTint = tint;
			outline.Apply();
			outline.SetPoints( CirclePoints() );
		}

		PaintHealth( flash );
	}

	void BuildShield()
	{
		shieldPlate = Scene.CreateObject();
		shieldPlate.Name = "Shield";
		shieldPlate.Parent = GameObject;

		var meshObject = Scene.CreateObject();
		meshObject.Name = "Shield Mesh";
		meshObject.Parent = shieldPlate;

		shieldMesh = meshObject.AddComponent<ModelRenderer>();
		var model = Model.Load( ShieldModelPath );
		shieldMesh.Model = model;
		shieldMesh.MaterialOverride = Material.Load( "materials/sheild/sheild.vmat" );
		shieldMesh.Tint = Color.White;
		shieldMesh.RenderType = ModelRenderer.ShadowRenderType.On;

		var bounds = model.IsValid() ? model.Bounds : default;
		var height = bounds.Size.z;
		var scale = height > 1f ? ShieldHeight / height : 1f;
		var yaw = Rotation.FromYaw( -90f );
		meshObject.LocalRotation = yaw;
		meshObject.LocalScale = Vector3.One * scale;
		meshObject.LocalPosition = -(yaw * bounds.Center) * scale;

		shieldReach = ShieldGap + bounds.Size.y * scale * 0.5f;
		shieldLift = height * scale * 0.5f + 8f;

		var facing = Blocks.FlatFacing( ShieldFacing );
		shieldPlate.WorldRotation = facing;
		shieldPlate.WorldPosition = WorldPosition + facing.Forward * shieldReach + Vector3.Up * shieldLift;
	}

	void RebuildVisuals()
	{
		if ( body.IsValid() && builtKind == Kind )
			return;

		body?.Destroy();
		shieldPlate?.Destroy();
		shieldMesh = null;
		outline?.GameObject?.Destroy();
		hpBack?.GameObject?.Destroy();
		hpFill?.GameObject?.Destroy();
		foreach ( var pip in hpPips )
			pip?.GameObject?.Destroy();

		hpPips.Clear();
		outline = null;
		hpBack = null;
		hpFill = null;
		builtKind = Kind;

		body = Scene.CreateObject();
		body.Name = "Enemy Body";
		body.Parent = GameObject;
		body.LocalPosition = Vector3.Zero;
		body.WorldRotation = Blocks.FlatFacing( LookFlat );

		dressed.Clear();
		var height = Kind == EnemyKind.Core
			? TerryLook.Height( true )
			: Kind == EnemyKind.Lens
				? TerryLook.CitizenHeight * 4.6f
				: TerryLook.Height( false );
		hobo = HoboLook.Attach( body, height );
		headTop = HoboLook.TopOf( height );

		if ( Kind == EnemyKind.Shield || Kind == EnemyKind.Shardguard )
			BuildShield();

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

		barWidth = Locations.IsBoss( Kind ) ? 26f : 16f;
		hpBack = MakeLine( "Hp Back", barWidth, BarBack );
		hpFill = MakeLine( "Hp Fill", barWidth - 6f, tint );
	}

	static Rotation Ease( Rotation from, Rotation to, float rate )
	{
		if ( Time.Delta <= 0.0001f )
			return to;

		return Rotation.Slerp( from, to, 1f - MathF.Exp( -rate * Time.Delta ) );
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
		line.HardCaps = true;
		line.Solid = true;
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
		var boss = Locations.IsBoss( Kind );
		var head = headTop + 24f;
		var half = boss ? 130f : 64f;
		var center = WorldPosition + Vector3.Up * head - rot.Forward * 28f;
		var left = center - rot.Right * half;
		var right = center + rot.Right * half;

		hpBack.HeadTint = BarBack;
		hpBack.TailTint = BarBack;
		hpBack.Apply();
		ShowSpan( hpBack, left, right );

		if ( Health <= 0 || ratio <= 0f )
		{
			hpFill.Clear();
			PaintPips( rot, left, right, 0 );
			return;
		}

		var front = rot.Forward * -14f;
		var edge = rot.Right * BarInset;
		var span = (half - BarInset) * 2f;
		hpFill.HeadTint = tint;
		hpFill.TailTint = tint;
		hpFill.Apply();
		ShowSpan( hpFill, left + front + edge, left + front + edge + rot.Right * (span * ratio) );

		PaintPips( rot, left + edge, right - edge, MaxHealth );
	}

	void PaintPips( Rotation rot, Vector3 left, Vector3 right, int steps )
	{
		var want = steps > 1 && steps <= PipLimit ? steps - 1 : 0;

		while ( hpPips.Count < want )
			hpPips.Add( MakeLine( "Hp Pip", MathF.Max( 2f, barWidth * 0.18f ), BarBack ) );

		if ( hpPips.Count == 0 )
			return;

		var front = rot.Forward * -22f;
		var up = rot.Up * ((barWidth - BarInset * 2f) * 0.5f);

		for ( var i = 0; i < hpPips.Count; i++ )
		{
			var pip = hpPips[i];
			if ( !pip.IsValid() )
				continue;

			if ( i >= want )
			{
				pip.Clear();
				continue;
			}

			var at = Vector3.Lerp( left, right, (i + 1f) / steps ) + front;
			pip.HeadTint = BarBack;
			pip.TailTint = BarBack;
			pip.Apply();
			ShowSpan( pip, at + up, at - up );
		}
	}

	void ShowSpan( PolyLine line, Vector3 from, Vector3 to )
	{
		if ( span.Count != 2 )
		{
			span.Clear();
			span.Add( from );
			span.Add( to );
		}
		else
		{
			span[0] = from;
			span[1] = to;
		}

		line.SetPoints( span );
	}

	void ClearBar()
	{
		hpBack?.Clear();
		hpFill?.Clear();
		foreach ( var pip in hpPips )
			pip?.Clear();
	}

	bool RoundNear()
	{
		if ( !Loop.IsValid() || !Loop.Inventory.IsValid() )
			return false;

		return Loop.Inventory.Reveals( Flat, GameSettings.Enemies.GlimmerReveal );
	}

	static Color HealthTint( float ratio )
	{
		if ( ratio > GameSettings.Boss.Phase2Health )
			return BarHigh;

		return ratio > GameSettings.Boss.Phase3Health ? BarMid : BarLow;
	}

	List<Vector3> CirclePoints()
	{
		const int segments = 20;
		if ( circle.Count != segments + 1 )
		{
			circle.Clear();
			for ( var n = 0; n <= segments; n++ )
				circle.Add( Vector3.Zero );
		}

		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			circle[i] = Arena.Geometry.ToWorld( Flat + ArenaGeometry.FromAngle( angle ) * Radius, 14f );
		}

		return circle;
	}
}
