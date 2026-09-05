namespace LoopedLoaded;

public sealed class RoundProjectile : Component
{
	[Property] public float BaseSpeed { get; set; } = 950f;
	[Property] public float Radius { get; set; } = 13f;
	[Property] public int MaxBounces { get; set; } = 4;
	[Property] public float Energy { get; set; } = 5500f;
	[Property] public float ArmDelay { get; set; } = 0.14f;
	[Property] public Color Tint { get; set; } = new Color( 1f, 0.72f, 0.22f );

	public GameLoop Loop { get; private set; }
	public Vector2 Flat { get; private set; }
	public Vector2 Direction { get; private set; }
	public float Speed { get; private set; }
	public int BouncesLeft { get; private set; }
	public float EnergyLeft { get; private set; }
	public bool Armed => Time.Now - born >= ArmDelay;
	public int TargetsHit { get; private set; }
	public int Ricochets { get; private set; }

	const float StepLength = 18f;
	const int TrailPoints = 24;

	readonly List<Vector3> trail = new();
	ArenaGeometry geometry;
	PolyLine trailLine;
	PointLight glow;
	float born;

	public void Launch( GameLoop loop, Vector2 origin, Vector2 direction )
	{
		Loop = loop;
		geometry = loop.Geometry;
		Flat = origin;
		Direction = direction.Normal;
		Speed = BaseSpeed;
		BouncesLeft = MaxBounces;
		EnergyLeft = Energy;
		Ricochets = 0;
		TargetsHit = 0;
		born = Time.Now;

		trail.Clear();
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 26f, Tint );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = Tint * 6f;
		glow.Radius = 420f;

		var trailObject = Scene.CreateObject();
		trailObject.Name = "Trail";
		trailObject.Parent = GameObject;

		trailLine = trailObject.AddComponent<PolyLine>();
		trailLine.HeadTint = Tint;
		trailLine.TailTint = Tint * 0.08f;
		trailLine.HeadWidth = 12f;
		trailLine.TailWidth = 1f;
		trailLine.Apply();
	}

	protected override void OnUpdate()
	{
		if ( geometry is null || !Loop.IsValid() )
			return;

		var toTravel = Speed * Time.Delta;

		while ( toTravel > 0.001f )
		{
			var step = MathF.Min( StepLength, toTravel );
			toTravel -= step;

			if ( !Step( step ) )
				return;
		}

		WorldPosition = geometry.ToPlayWorld( Flat );
		PushTrail( WorldPosition );
	}

	bool Step( float step )
	{
		if ( geometry.TraceRay( Flat, Direction, step + Radius, out var hit ) )
		{
			EnergyLeft -= MathF.Max( 0f, hit.Distance - Radius );
			PushTrail( geometry.ToPlayWorld( hit.Position ) );

			Flat = hit.Position + hit.Normal * Radius;
			Direction = ArenaGeometry.Reflect( Direction, hit.Normal ).Normal;
			BouncesLeft--;
			Ricochets++;

			var world = geometry.ToPlayWorld( Flat );
			Sound.Play( "sounds/impacts/bullets/impact-bullet-metal.sound", world );
			ImpactFlash.Spawn( Scene, world, new Color( 0.55f, 0.9f, 1f ), 0.65f );
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
			Loop.LoseRound( this );
			return false;
		}

		if ( Armed && Loop.Inventory.IsValid() && Loop.Inventory.InCatchZone( Flat, Radius ) )
		{
			Loop.CatchRound( this );
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
		foreach ( var target in Loop.Targets )
		{
			if ( !target.IsValid() || !target.Alive )
				continue;

			var offset = Flat - target.Flat;
			var reach = target.Radius + Radius;

			if ( offset.Length > reach )
				continue;

			var normal = offset.Length < 0.01f ? -Direction : offset.Normal;

			PushTrail( geometry.ToPlayWorld( Flat ) );

			Flat = target.Flat + normal * (reach + 1f);
			Direction = ArenaGeometry.Reflect( Direction, normal ).Normal;
			Speed *= 0.94f;
			BouncesLeft--;
			TargetsHit++;

			target.Damage( 1 );

			if ( BouncesLeft < 0 )
			{
				Loop.LoseRound( this );
				return true;
			}

			return false;
		}

		return false;
	}

	void PushTrail( Vector3 point )
	{
		trail.Add( point );

		while ( trail.Count > TrailPoints )
			trail.RemoveAt( 0 );

		trailLine?.SetPoints( trail );
	}
}
