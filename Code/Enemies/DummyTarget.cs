namespace LoopedLoaded;

public sealed class DummyTarget : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public int MaxHealth { get; set; } = 3;
	[Property] public float Radius { get; set; } = 58f;
	[Property] public float RespawnDelay { get; set; } = 2.4f;

	public bool Alive { get; private set; } = true;
	public int Health { get; private set; }
	public Vector2 Flat { get; private set; }

	static readonly Color LiveTint = new Color( 0.95f, 0.28f, 0.22f );
	static readonly Color HurtTint = new Color( 1f, 0.9f, 0.6f );

	GameObject body;
	PolyLine outline;
	float hitAt = -99f;
	float diedAt;

	public void Place( Vector2 flat )
	{
		Flat = flat;
		Health = MaxHealth;
		Alive = true;

		if ( Arena.IsValid() )
			WorldPosition = new Vector3( Flat.x, Flat.y, 0f );

		if ( body.IsValid() )
			body.Enabled = true;
	}

	public void PlaceRandom()
	{
		var angle = Game.Random.Float( 0f, MathF.Tau );
		var radius = Game.Random.Float( Arena.Geometry.CoreRadius + 160f, Arena.Geometry.TrackInner - 140f );
		Place( ArenaGeometry.FromAngle( angle ) * radius );
	}

	public void Damage( int amount )
	{
		if ( !Alive )
			return;

		Health -= amount;
		hitAt = Time.Now;

		var world = Arena.Geometry.ToPlayWorld( Flat );
		Sound.Play( "sounds/impacts/bullets/impact-bullet-flesh.sound", world );
		ImpactFlash.Spawn( Scene, world, HurtTint, 1.1f );

		if ( Health > 0 )
			return;

		Alive = false;
		diedAt = Time.Now;

		if ( body.IsValid() )
			body.Enabled = false;

		Sound.Play( "sounds/effects/explosion/explosion_small.sound", world );
		ImpactFlash.Spawn( Scene, world, LiveTint, 2.2f );

		if ( Loop.IsValid() )
			Loop.RegisterKill();
	}

	protected override void OnStart()
	{
		body = Scene.CreateObject();
		body.Name = "Dummy Body";
		body.Parent = GameObject;

		Blocks.SpawnBox( body, "Torso", WorldPosition + Vector3.Up * 62f, Rotation.Identity, new Vector3( 74f, 74f, 124f ), LiveTint );
		Blocks.SpawnBox( body, "Head", WorldPosition + Vector3.Up * 148f, Rotation.Identity, new Vector3( 44f, 44f, 44f ), LiveTint * 1.4f );

		var outlineObject = Scene.CreateObject();
		outlineObject.Name = "Hit Circle";
		outlineObject.Parent = GameObject;

		outline = outlineObject.AddComponent<PolyLine>();
		outline.HeadWidth = 3f;
		outline.TailWidth = 3f;
		outline.HeadTint = LiveTint;
		outline.TailTint = LiveTint;
		outline.Apply();
	}

	protected override void OnUpdate()
	{
		if ( !Arena.IsValid() )
			return;

		if ( !Alive )
		{
			outline?.Clear();

			if ( Time.Now - diedAt >= RespawnDelay )
				PlaceRandom();

			return;
		}

		var flash = MathF.Max( 0f, 1f - (Time.Now - hitAt) * 6f );
		var tint = Color.Lerp( LiveTint, HurtTint, flash );

		foreach ( var renderer in body.GetComponentsInChildren<ModelRenderer>() )
			renderer.Tint = tint;

		if ( outline.IsValid() )
		{
			outline.HeadTint = tint;
			outline.TailTint = tint;
			outline.Apply();
			outline.SetPoints( BuildCircle() );
		}
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
