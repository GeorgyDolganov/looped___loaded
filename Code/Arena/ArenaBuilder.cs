namespace LoopedLoaded;

public sealed class ArenaBuilder : Component
{
	[Property] public float TrackRadius { get; set; } = 1000f;
	[Property] public float TrackWidth { get; set; } = 190f;
	[Property] public float BoundaryRadius { get; set; } = 1170f;
	[Property] public float CoreRadius { get; set; } = 230f;

	public static readonly Color FloorTint = new Color( 0.035f, 0.042f, 0.055f );
	public static readonly Color TrackTint = new Color( 0.13f, 0.15f, 0.19f );
	public static readonly Color TrackEdgeTint = new Color( 0.34f, 0.42f, 0.52f );
	public static readonly Color BoundaryTint = new Color( 0.62f, 0.70f, 0.82f );
	public static readonly Color CoreTint = new Color( 0.42f, 0.24f, 0.30f );
	public static readonly Color PanelTint = new Color( 0.30f, 0.78f, 0.86f );

	public ArenaGeometry Geometry
	{
		get
		{
			if ( geometry is null )
			{
				geometry = new ArenaGeometry
				{
					TrackRadius = TrackRadius,
					TrackWidth = TrackWidth,
					BoundaryRadius = BoundaryRadius,
					CoreRadius = CoreRadius
				};

				geometry.Rebuild();
			}

			return geometry;
		}
	}

	ArenaGeometry geometry;
	GameObject visuals;
	GameObject panelRoot;

	protected override void OnStart() => EnsureVisuals();

	public void RollLayout( int lap, int seed )
	{
		EnsureVisuals();
		Geometry.ClearBossWalls();
		Geometry.GeneratePanels( lap, seed );
		RebuildPanels();
	}

	public void EnsureVisuals()
	{
		if ( visuals.IsValid() )
			return;

		visuals = Scene.CreateObject();
		visuals.Name = "Arena Visuals";
		visuals.Parent = GameObject;

		BuildFloor();
		BuildTrack();
		BuildRingWalls();
	}

	void BuildFloor()
	{
		var span = BoundaryRadius * 2.8f;
		Blocks.SpawnBox( visuals, "Floor", new Vector3( 0, 0, -30f ), Rotation.Identity, new Vector3( span, span, 60f ), FloorTint );
	}

	void BuildTrack()
	{
		const int segments = 64;
		var step = MathF.Tau / segments;
		var arc = MathF.Tau * TrackRadius / segments;

		for ( var i = 0; i < segments; i++ )
		{
			var angle = step * i;
			var outward = ArenaGeometry.FromAngle( angle );
			var tangent = new Vector2( -outward.y, outward.x );
			var shade = i % 2 == 0 ? 1f : 0.82f;

			Blocks.SpawnBox( visuals, $"Track {i}",
				new Vector3( outward.x * TrackRadius, outward.y * TrackRadius, 3f ),
				Blocks.FlatFacing( tangent ),
				new Vector3( arc * 1.02f, TrackWidth, 6f ),
				TrackTint * shade, false );
		}

		BuildTrackEdge( Geometry.TrackInner, "Inner" );
		BuildTrackEdge( Geometry.TrackOuter, "Outer" );
	}

	void BuildTrackEdge( float radius, string label )
	{
		const int segments = 72;
		var step = MathF.Tau / segments;
		var arc = MathF.Tau * radius / segments;

		for ( var i = 0; i < segments; i++ )
		{
			var angle = step * i;
			var outward = ArenaGeometry.FromAngle( angle );
			var tangent = new Vector2( -outward.y, outward.x );

			Blocks.SpawnBox( visuals, $"Edge {label} {i}",
				new Vector3( outward.x * radius, outward.y * radius, 8f ),
				Blocks.FlatFacing( tangent ),
				new Vector3( arc * 1.02f, 9f, 10f ),
				TrackEdgeTint, false );
		}
	}

	void BuildRingWalls()
	{
		for ( var i = 0; i < Geometry.Walls.Count; i++ )
		{
			var wall = Geometry.Walls[i];
			if ( wall.Kind != WallKind.Boundary && wall.Kind != WallKind.Core )
				continue;

			SpawnWall( visuals, wall, i );
		}
	}

	void RebuildPanels()
	{
		panelRoot?.Destroy();
		panelRoot = Scene.CreateObject();
		panelRoot.Name = "Panels";
		panelRoot.Parent = visuals;

		for ( var i = 0; i < Geometry.Walls.Count; i++ )
		{
			var wall = Geometry.Walls[i];
			if ( wall.Kind != WallKind.Panel )
				continue;

			SpawnWall( panelRoot, wall, i );
		}
	}

	void SpawnWall( GameObject parent, WallSegment wall, int index )
	{
		var (thickness, height, tint) = wall.Kind switch
		{
			WallKind.Boundary => (32f, 140f, BoundaryTint),
			WallKind.Core => (36f, 170f, CoreTint),
			_ => (26f, 115f, PanelTint)
		};

		var center = wall.Center;
		Blocks.SpawnBox( parent, $"Wall {wall.Kind} {index}",
			new Vector3( center.x, center.y, height * 0.5f ),
			Blocks.FlatFacing( wall.Direction ),
			new Vector3( wall.Length + thickness, thickness, height ),
			tint );
	}
}
