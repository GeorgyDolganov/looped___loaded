namespace LoopedLoaded;

public sealed class TurretDrone : Component
{
	public const string TrinketId = "TURRET";

	const float Orbit = 92f;
	const float Spin = 2.2f;
	const float Lift = 90f;
	const float Bob = 7f;
	const float Range = 900f;
	const float Reach = 26f;
	const float TracerTime = 0.12f;
	static readonly Color Tint = new Color( 1f, 0.72f, 0.22f );
	static readonly Color BarrelTint = new Color( 0.45f, 0.3f, 0.1f );

	public GameLoop Loop { get; set; }
	public int Level { get; set; } = 1;

	PolyLine tracer;
	readonly List<Vector3> span = new( 2 );
	Enemy focus;
	Vector2 aim = Vector2.Right;
	Vector3 tracerEnd;
	float angle;
	float charge;
	float tracerLeft;

	public static float Interval( int level ) => level switch
	{
		>= 3 => 1f,
		2 => 1.5f,
		_ => 2f
	};

	public static TurretDrone Spawn( GameLoop loop, GameObject parent )
	{
		var go = loop.Scene.CreateObject();
		go.Name = "Turret";
		go.Parent = parent;
		var drone = go.AddComponent<TurretDrone>();
		drone.Loop = loop;
		return drone;
	}

	Vector3 Muzzle => WorldPosition + WorldRotation.Forward * Reach;

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Body", WorldPosition, 28f, Tint );

		var barrel = Blocks.SpawnBox( GameObject, "Barrel", WorldPosition, WorldRotation, new Vector3( 24f, 7f, 7f ), BarrelTint, false );
		barrel.LocalPosition = Vector3.Forward * 16f;
		barrel.LocalRotation = Rotation.Identity;

		var line = Scene.CreateObject();
		line.Name = "Tracer";
		line.Parent = GameObject;
		tracer = line.AddComponent<PolyLine>();
		tracer.HeadWidth = 2.5f;
		tracer.TailWidth = 5f;
		tracer.Apply();
	}

	protected override void OnUpdate()
	{
		if ( !Loop.IsValid() || !Loop.Runner.IsValid() || !Loop.Arena.IsValid() )
			return;

		var dt = Loop.Halted ? 0f : RealTime.Delta;
		angle = (angle + Spin * dt) % MathF.Tau;
		var flat = Loop.Runner.Flat + ArenaGeometry.FromAngle( angle ) * Orbit;
		WorldPosition = Loop.Geometry.ToPlayWorld( flat ) + Vector3.Up * (Lift + MathF.Sin( angle * 2f ) * Bob);

		if ( focus.IsValid() && focus.Alive )
		{
			var to = focus.Flat - flat;
			if ( to.Length > 1f )
				aim = to.Normal;
		}

		WorldRotation = Ease( WorldRotation, Blocks.FlatFacing( aim ), 12f, dt );

		if ( !Loop.IsFrozen )
			Think( flat, dt );

		PaintTracer( dt );
	}

	void Think( Vector2 flat, float dt )
	{
		var interval = Interval( Level );
		charge = MathF.Min( interval, charge + dt );
		if ( charge < interval )
			return;

		var target = Pick( flat );
		if ( target is null )
			return;

		charge = 0f;
		Fire( flat, target );
	}

	Enemy Pick( Vector2 flat )
	{
		Enemy best = null;
		var bestGap = Range;
		foreach ( var enemy in Loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || enemy.Kind == EnemyKind.Core || Hidden( enemy ) )
				continue;

			var gap = (enemy.Flat - flat).Length - enemy.Radius;
			if ( gap >= bestGap || Loop.Geometry.SightBlocked( flat, enemy.Flat, out _ ) )
				continue;

			best = enemy;
			bestGap = gap;
		}

		return best;
	}

	bool Hidden( Enemy enemy )
		=> enemy.Kind == EnemyKind.Glimmer
			&& (!Loop.Inventory.IsValid() || !Loop.Inventory.Reveals( enemy.Flat, GameSettings.Enemies.GlimmerReveal ));

	void Fire( Vector2 flat, Enemy target )
	{
		focus = target;
		var to = target.Flat - flat;
		if ( to.Length > 1f )
			aim = to.Normal;

		WorldRotation = Blocks.FlatFacing( aim );
		var contact = target.Flat - aim * target.Radius;
		tracerEnd = Loop.Geometry.ToPlayWorld( contact ) + Vector3.Up * 50f;
		tracerLeft = TracerTime;
		ImpactFlash.Spawn( Scene, Muzzle, Tint, 0.35f );

		if ( target.BlocksFrom( aim, null, false, contact ) )
		{
			if ( Locations.IsBoss( target.Kind ) )
				Loop.NoteArmor();

			ArenaSounds.Ricochet( tracerEnd );
			return;
		}

		target.Damage( 1, null );
	}

	void PaintTracer( float dt )
	{
		if ( !tracer.IsValid() )
			return;

		tracerLeft = MathF.Max( 0f, tracerLeft - dt );
		if ( tracerLeft <= 0f )
		{
			tracer.Clear();
			return;
		}

		var fade = tracerLeft / TracerTime;
		tracer.HeadTint = Tint.WithAlpha( fade );
		tracer.TailTint = Color.White.WithAlpha( fade );
		tracer.Apply();

		span.Clear();
		span.Add( Muzzle );
		span.Add( tracerEnd );
		tracer.SetPoints( span );
	}

	static Rotation Ease( Rotation from, Rotation to, float rate, float dt )
	{
		if ( dt <= 0.0001f )
			return from;

		return Rotation.Slerp( from, to, 1f - MathF.Exp( -rate * dt ) );
	}
}
