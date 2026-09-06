namespace LoopedLoaded;

public sealed class RoundEcho : Component
{
	const float Speed = 1400f;

	GameLoop loop;
	readonly List<Vector2> path = new();
	int damage;
	float freezeDuration;
	float freezeScale;
	int cursor;
	Vector2 flat;
	readonly HashSet<Enemy> struck = new();
	PolyLine trailLine;

	public static void Spawn( GameLoop loop, List<Vector2> crumbs, float keep, int damage, float freezeDuration, float freezeScale )
	{
		if ( loop is null || crumbs is null || crumbs.Count < 2 || keep <= 0.01f )
			return;

		var go = loop.Scene.CreateObject();
		go.Name = "Echo";

		var echo = go.AddComponent<RoundEcho>();
		echo.Arm( loop, crumbs, keep, damage, freezeDuration, freezeScale );
	}

	void Arm( GameLoop host, List<Vector2> crumbs, float keep, int amount, float freezeDur, float freezeSc )
	{
		loop = host;
		damage = Math.Max( 1, amount );
		freezeDuration = freezeDur;
		freezeScale = freezeSc;
		var limit = Math.Clamp( keep, 0.05f, 1f );
		var take = Math.Max( 2, (int)MathF.Ceiling( crumbs.Count * limit ) );
		for ( var i = 0; i < take && i < crumbs.Count; i++ )
			path.Add( crumbs[i] );

		cursor = 1;
		flat = path[0];
		WorldPosition = loop.Geometry.ToPlayWorld( flat );
	}

	protected override void OnStart()
	{
		Blocks.SpawnSphere( GameObject, "Ghost", WorldPosition, 18f, new Color( 0.55f, 0.9f, 1f ) * 0.7f, false );

		var trailObject = Scene.CreateObject();
		trailObject.Name = "Echo Trail";
		trailObject.Parent = GameObject;
		trailLine = trailObject.AddComponent<PolyLine>();
		trailLine.HeadTint = new Color( 0.55f, 0.9f, 1f );
		trailLine.TailTint = new Color( 0.55f, 0.9f, 1f ) * 0.1f;
		trailLine.HeadWidth = 8f;
		trailLine.TailWidth = 1f;
		trailLine.Apply();
	}

	protected override void OnUpdate()
	{
		if ( loop is null || loop.IsFrozen || path.Count < 2 )
		{
			GameObject.Destroy();
			return;
		}

		var remain = Speed * Time.Delta;
		while ( remain > 0.001f && cursor < path.Count )
		{
			var to = path[cursor] - flat;
			var dist = to.Length;
			if ( dist < 0.5f )
			{
				cursor++;
				continue;
			}

			var step = MathF.Min( remain, dist );
			flat += to.Normal * step;
			remain -= step;
			Strike();
		}

		WorldPosition = loop.Geometry.ToPlayWorld( flat );
		trailLine?.SetPoints( new List<Vector3> { loop.Geometry.ToPlayWorld( path[0] ), WorldPosition } );

		if ( cursor >= path.Count )
			GameObject.Destroy();
	}

	void Strike()
	{
		foreach ( var enemy in loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || struck.Contains( enemy ) )
				continue;

			if ( (flat - enemy.Flat).Length > enemy.Radius + 14f )
				continue;

			struck.Add( enemy );
			enemy.Damage( damage, freezeDuration, freezeScale );
		}
	}
}
