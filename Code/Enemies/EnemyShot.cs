namespace LoopedLoaded;

public sealed class EnemyShot : Component
{
	[Property] public float Speed { get; set; } = 640f;
	[Property] public float Radius { get; set; } = 18f;
	[Property] public float Lifetime { get; set; } = 3.2f;

	public GameLoop Loop { get; private set; }
	public Vector2 Flat { get; private set; }
	public Vector2 Direction { get; private set; }

	const int TrailPoints = 10;

	readonly List<Vector3> trail = new();
	ArenaGeometry geometry;
	GameObject bolt;
	PolyLine trailLine;
	float born;

	public static void Fire( GameLoop loop, Vector2 origin, Vector2 direction, float speed = 0f )
	{
		if ( direction.Length < 0.01f )
			return;

		var go = loop.Scene.CreateObject();
		go.Name = "Enemy Shot";

		var shot = go.AddComponent<EnemyShot>();
		shot.Loop = loop;
		shot.geometry = loop.Geometry;
		shot.Flat = origin;
		shot.Direction = direction.Normal;
		shot.born = Time.Now;
		shot.Speed = speed > 1f ? speed : 640f;
		shot.WorldPosition = loop.Geometry.ToPlayWorld( origin );
		shot.BuildVisuals();

		loop.Shots.Add( shot );

		Sound.Play( "sounds/impacts/bullets/impact-bullet-generic.sound", shot.WorldPosition );
	}

	public void ShiftTime( float dt )
	{
		born += dt;
	}

	protected override void OnStart() => BuildVisuals();

	void BuildVisuals()
	{
		if ( bolt.IsValid() )
			return;

		bolt = Blocks.SpawnBox( GameObject, "Bolt", WorldPosition, Blocks.FlatFacing( Direction ),
			new Vector3( 42f, 16f, 16f ), ShotColors.Enemy, false );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Enemy * 7f;
		glow.Radius = 280f;

		var trailObject = Scene.CreateObject();
		trailObject.Name = "Trail";
		trailObject.Parent = GameObject;

		trailLine = trailObject.AddComponent<PolyLine>();
		trailLine.HeadTint = ShotColors.Enemy;
		trailLine.TailTint = ShotColors.Enemy * 0.05f;
		trailLine.HeadWidth = 18f;
		trailLine.TailWidth = 2f;
		trailLine.Apply();
	}

	protected override void OnUpdate()
	{
		if ( !Loop.IsValid() )
		{
			DestroyShot();
			return;
		}

		if ( Loop.IsFrozen )
			return;

		if ( Time.Now - born >= Lifetime )
		{
			DestroyShot();
			return;
		}

		var step = Speed * Time.Delta;

		if ( geometry.TraceRay( Flat, Direction, step + Radius, out var hit ) )
		{
			ImpactFlash.Spawn( Scene, geometry.ToPlayWorld( hit.Position ), ShotColors.Enemy, 0.5f );
			DestroyShot();
			return;
		}

		Flat += Direction * step;
		WorldPosition = geometry.ToPlayWorld( Flat );

		if ( bolt.IsValid() )
		{
			bolt.WorldPosition = WorldPosition;
			bolt.WorldRotation = Blocks.FlatFacing( Direction );
		}

		PushTrail( WorldPosition );
	}

	protected override void OnDestroy()
	{
		if ( Loop.IsValid() )
			Loop.Shots.Remove( this );
	}

	void PushTrail( Vector3 point )
	{
		trail.Add( point );

		while ( trail.Count > TrailPoints )
			trail.RemoveAt( 0 );

		trailLine?.SetPoints( trail );
	}

	void DestroyShot() => GameObject.Destroy();
}
