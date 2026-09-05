namespace LoopedLoaded;

public sealed class EnemyShot : Component
{
	[Property] public float Speed { get; set; } = 640f;
	[Property] public float Radius { get; set; } = 18f;
	[Property] public float Lifetime { get; set; } = 3.2f;

	public GameLoop Loop { get; private set; }
	public Vector2 Flat { get; private set; }
	public Vector2 Direction { get; private set; }

	ArenaGeometry geometry;
	Color tint;
	float born;

	public static void Fire( GameLoop loop, Vector2 origin, Vector2 direction, Color tint, float speed = 0f )
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
		shot.tint = tint;
		shot.born = Time.Now;
		shot.Speed = speed > 1f ? speed : 640f;
		shot.WorldPosition = loop.Geometry.ToPlayWorld( origin );

		loop.Shots.Add( shot );

		Sound.Play( "sounds/impacts/bullets/impact-bullet-generic.sound", shot.WorldPosition );
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Bolt", WorldPosition, 28f, tint );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = tint * 5f;
		glow.Radius = 260f;
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
			ImpactFlash.Spawn( Scene, geometry.ToPlayWorld( hit.Position ), tint, 0.5f );
			DestroyShot();
			return;
		}

		Flat += Direction * step;
		WorldPosition = geometry.ToPlayWorld( Flat );
	}

	protected override void OnDestroy()
	{
		if ( Loop.IsValid() )
			Loop.Shots.Remove( this );
	}

	void DestroyShot() => GameObject.Destroy();
}
