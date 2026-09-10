namespace LoopedLoaded;

public sealed class CityShot : Component
{
	public CityBoard Board { get; set; }
	public Vector2 Direction { get; set; }
	public float Speed { get; set; } = 1600f;
	public float Radius { get; set; } = 12f;
	public Color Tint { get; set; } = ShotColors.Player;
	public int BouncesLeft { get; set; } = 8;
	public float Energy { get; set; } = 3800f;

	const float StepLength = 16f;

	Vector2 flat;
	float height;

	public void Launch( Vector3 origin, Vector2 direction )
	{
		flat = new Vector2( origin.x, origin.y );
		height = origin.z;
		Direction = direction.Length > 0.001f ? direction.Normal : Vector2.Up;
		WorldPosition = origin;
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Bolt", WorldPosition, 22f, ShotColors.Player );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Player * 5f;
		glow.Radius = 280f;
	}

	protected override void OnUpdate()
	{
		if ( !Board.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		if ( Board.Loop.IsValid() && Board.Loop.Paused )
			return;

		var toTravel = Speed * Time.Delta;

		while ( toTravel > 0.001f )
		{
			var step = MathF.Min( StepLength, toTravel );
			toTravel -= step;

			if ( !Advance( step ) )
				return;
		}

		WorldPosition = new Vector3( flat.x, flat.y, height );
	}

	bool Advance( float step )
	{
		if ( Board.Trace( flat, Direction, step + Radius, Radius, out var hit ) )
		{
			flat = hit.Position;
			Board.Separate( ref flat, Radius );
			var world = new Vector3( flat.x, flat.y, height );

			if ( hit.Plot is not null && hit.Plot.Working )
			{
				if ( ArenaGeometry.Dot( Direction, hit.Normal ) < 0f )
				{
					Board.RegisterHit( hit.Plot );
					Direction = ArenaGeometry.Reflect( Direction, hit.Normal ).Normal;
					BouncesLeft--;
					ArenaSounds.Ricochet( world );
					ImpactFlash.Spawn( Scene, world, Buildings.Color( hit.Plot.Kind ), 0.85f );
				}

				if ( BouncesLeft < 0 || Energy <= 0f )
				{
					GameObject.Destroy();
					return false;
				}

				return true;
			}

			Board.RegisterHit( hit.Plot );
			GameObject.Destroy();
			return false;
		}

		Energy -= step;
		flat += Direction * step;

		if ( Energy <= 0f || Board.OutOfBounds( flat ) )
		{
			Board.Miss( flat );
			GameObject.Destroy();
			return false;
		}

		return true;
	}
}
