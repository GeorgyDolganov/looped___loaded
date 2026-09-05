namespace LoopedLoaded;

public sealed class CityShot : Component
{
	public CityBoard Board { get; set; }
	public Vector2 Target { get; set; }
	public float Speed { get; set; } = 1600f;
	public Color Tint { get; set; } = new Color( 1f, 0.72f, 0.22f );

	Vector2 flat;
	float height;

	public void Launch( Vector3 origin )
	{
		flat = new Vector2( origin.x, origin.y );
		height = origin.z;
		WorldPosition = origin;
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Bolt", WorldPosition, 22f, Tint );

		var glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = Tint * 5f;
		glow.Radius = 280f;
	}

	protected override void OnUpdate()
	{
		if ( !Board.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		var to = Target - flat;
		var step = Speed * Time.Delta;

		if ( to.Length <= step + 8f )
		{
			Board.ResolveShot( Target );
			GameObject.Destroy();
			return;
		}

		flat += to.Normal * step;
		WorldPosition = new Vector3( flat.x, flat.y, height );
	}
}
