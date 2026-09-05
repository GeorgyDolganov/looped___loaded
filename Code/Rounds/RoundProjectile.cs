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
	public Color Tint => Flight.Tint;
	public int SlotIndex => Flight.SlotIndex;

	const float StepLength = 18f;
	const int TrailPoints = 24;

	readonly List<Vector3> trail = new();
	readonly HashSet<Enemy> struck = new();
	ArenaGeometry geometry;
	PolyLine trailLine;
	int pierceLeft;
	bool leftCatchZone;

	public void Launch( GameLoop loop, RoundFlight flight, Vector2 origin, Vector2 direction )
	{
		Loop = loop;
		Flight = flight;
		geometry = loop.Geometry;
		Flat = origin;
		Direction = direction.Normal;
		Speed = BaseSpeed;
		BouncesLeft = flight.MaxBounces;
		EnergyLeft = flight.Energy;
		pierceLeft = flight.PierceCharges;
		Ricochets = 0;
		TargetsHit = 0;
		leftCatchZone = false;
		struck.Clear();
		trail.Clear();
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 26f, Tint );

		var glow = GameObject.AddComponent<PointLight>();
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
		if ( geometry is null || !Loop.IsValid() || Loop.IsFrozen )
			return;

		var toTravel = Speed * Time.Delta;

		while ( toTravel > 0.001f )
		{
			var step = MathF.Min( StepLength, toTravel );
			toTravel -= step;

			if ( !Step( step ) )
				return;
		}

		SteerHome();
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

		if ( Loop.Inventory.IsValid() )
		{
			var inside = Loop.Inventory.InCatchZone( Flat, Radius, Flight.CatchBonus );

			if ( !leftCatchZone )
			{
				if ( !inside )
					leftCatchZone = true;
			}
			else if ( inside )
			{
				Loop.CatchRound( this );
				return false;
			}
		}

		return true;
	}

	void SteerHome()
	{
		if ( Flight.MagnetRadius <= 0f || !Armed || !Loop.Inventory.IsValid() )
			return;

		var toCatch = Loop.Inventory.CatchPoint - Flat;
		var distance = toCatch.Length;

		if ( distance < 1f || distance > Flight.MagnetRadius )
			return;

		var weight = 1f - distance / Flight.MagnetRadius;
		var turn = 1f - MathF.Exp( -Flight.MagnetPull * weight * Time.Delta );
		Direction = (Direction + toCatch.Normal * turn).Normal;
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
			if ( !target.IsValid() || !target.Alive || struck.Contains( target ) )
				continue;

			var offset = Flat - target.Flat;
			var reach = target.Radius + Radius;

			if ( offset.Length > reach )
				continue;

			struck.Add( target );

			var normal = offset.Length < 0.01f ? -Direction : offset.Normal;
			PushTrail( geometry.ToPlayWorld( Flat ) );

			if ( target.BlocksFrom( Direction ) )
			{
				Flat = target.Flat + normal * (reach + 1f);
				Direction = ArenaGeometry.Reflect( Direction, normal ).Normal;
				BouncesLeft--;

				var world = geometry.ToPlayWorld( Flat );
				Sound.Play( "sounds/impacts/bullets/impact-bullet-metal.sound", world );
				ImpactFlash.Spawn( Scene, world, new Color( 0.75f, 0.85f, 1f ), 0.9f );

				if ( BouncesLeft < 0 )
				{
					Loop.LoseRound( this );
					return true;
				}

				continue;
			}

			target.Damage( Flight.Damage > 0 ? Flight.Damage : 1, this );
			TargetsHit++;

			if ( pierceLeft > 0 )
			{
				pierceLeft--;
				Flat = target.Flat + Direction * (reach + 4f);
				continue;
			}

			Flat = target.Flat + normal * (reach + 1f);
			Direction = ArenaGeometry.Reflect( Direction, normal ).Normal;
			Speed *= 0.94f;
			BouncesLeft--;

			if ( BouncesLeft < 0 )
			{
				Loop.LoseRound( this );
				return true;
			}
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
