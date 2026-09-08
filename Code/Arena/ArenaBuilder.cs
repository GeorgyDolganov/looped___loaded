namespace LoopedLoaded;

public sealed class ArenaBuilder : Component
{
	[Property] public string Code { get; set; } = "YARD";
	[Property] public float TrackRadius { get; set; } = 1000f;
	[Property] public float TrackWidth { get; set; } = 190f;
	[Property] public float BoundaryRadius { get; set; } = 1170f;
	[Property] public float CoreRadius { get; set; } = 230f;
	[Property] public float StartAngle { get; set; } = MathF.PI * 0.5f;

	public static readonly Color FloorTint = new Color( 0.035f, 0.042f, 0.055f );
	public static readonly Color TrackTint = new Color( 0.13f, 0.15f, 0.19f );
	public static readonly Color TrackEdgeTint = new Color( 0.34f, 0.42f, 0.52f );
	public static readonly Color BoundaryTint = new Color( 0.62f, 0.70f, 0.82f );
	public static readonly Color CoreTint = new Color( 0.42f, 0.24f, 0.30f );
	public static readonly Color PanelTint = new Color( 0.30f, 0.78f, 0.86f );
	public static readonly Color FinishGold = new Color( 1f, 0.84f, 0.38f );
	public static readonly Color FinishInk = new Color( 0.05f, 0.06f, 0.08f );

	public ArenaGeometry Geometry
	{
		get
		{
			if ( geometry is null )
				BindScene();

			return geometry;
		}
	}

	ArenaGeometry geometry;
	GameObject panelRoot;
	GameObject finishRoot;
	PointLight finishLight;
	PolyLine finishBeam;
	GameObject beamInner;
	GameObject beamOuter;
	GameLoop loop;
	readonly Dictionary<int, GameObject> panelVisuals = new();
	readonly Dictionary<int, GameObject> authoredVisuals = new();
	readonly List<(ModelRenderer Renderer, Color Tint, float Pulse)> finishMarks = new();

	protected override void OnAwake() => BindScene();

	public void BindScene()
	{
		geometry ??= new ArenaGeometry();
		geometry.TrackRadius = TrackRadius;
		geometry.TrackWidth = TrackWidth;
		geometry.BoundaryRadius = BoundaryRadius;
		geometry.CoreRadius = CoreRadius;

		authoredVisuals.Clear();
		var walls = new List<WallSegment>();
		foreach ( var marker in GameObject.GetComponentsInChildren<ArenaWall>( true ) )
		{
			if ( !marker.IsValid() || IsUnder( marker.GameObject, "Panels" ) )
				continue;

			walls.Add( marker.ToSegment() );
			if ( marker.Kind == WallKind.Panel )
				authoredVisuals[walls.Count - 1] = marker.GameObject;
		}

		geometry.ApplyAuthored( walls );

		finishRoot = FindChild( GameObject, "Finish" );
		panelRoot = FindChild( GameObject, "Panels" );
		if ( !panelRoot.IsValid() )
		{
			panelRoot = Scene.CreateObject();
			panelRoot.Name = "Panels";
			panelRoot.Parent = GameObject;
		}

		CollectFinish();
	}

	public void RollLayout( int lap, int seed )
	{
		if ( geometry is null )
			BindScene();

		Geometry.ClearBossWalls();
		Geometry.GeneratePanels( lap, seed );
		RebuildPanels();
	}

	[Button( "Fill Default Layout" )]
	public void FillDefaultLayout()
	{
		ReplaceGroup( "Floor" );
		ReplaceGroup( "Track" );
		ReplaceGroup( "Edges" );
		ReplaceGroup( "Walls" );
		ReplaceGroup( "Finish" );
		if ( !FindChild( GameObject, "Panels" ).IsValid() )
			ReplaceGroup( "Panels" );

		BuildFloor();
		BuildTrack();
		BuildFinish();

		geometry ??= new ArenaGeometry
		{
			TrackRadius = TrackRadius,
			TrackWidth = TrackWidth,
			BoundaryRadius = BoundaryRadius,
			CoreRadius = CoreRadius
		};
		geometry.ApplyAuthored( null );
		BuildRingWalls();
		BindScene();
	}

	protected override void OnUpdate() => PulseFinish();

	void CollectFinish()
	{
		finishMarks.Clear();
		finishLight = null;
		finishBeam = null;
		beamInner = null;
		beamOuter = null;

		if ( !finishRoot.IsValid() )
			return;

		foreach ( var mark in finishRoot.GetComponentsInChildren<FinishPulse>( true ) )
		{
			var renderer = mark.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				finishMarks.Add( (renderer, renderer.Tint, mark.Pulse) );
		}

		finishLight = finishRoot.GetComponentsInChildren<PointLight>( true ).FirstOrDefault();
		finishBeam = finishRoot.GetComponentsInChildren<PolyLine>( true ).FirstOrDefault();
		beamInner = FindChild( finishRoot, "Beam Inner" );
		beamOuter = FindChild( finishRoot, "Beam Outer" );
		ApplyFinishBeam();
	}

	void ApplyFinishBeam()
	{
		if ( !finishBeam.IsValid() )
			return;

		Vector3 a;
		Vector3 b;
		if ( beamInner.IsValid() && beamOuter.IsValid() )
		{
			a = beamInner.WorldPosition;
			b = beamOuter.WorldPosition;
		}
		else
		{
			var outward = ArenaGeometry.FromAngle( StartAngle );
			var inner = Geometry.TrackInner - 8f;
			var outer = Geometry.TrackOuter + 8f;
			a = new Vector3( outward.x * inner, outward.y * inner, 28f );
			b = new Vector3( outward.x * outer, outward.y * outer, 28f );
		}

		finishBeam.SetPoints( new List<Vector3> { a, b } );
	}

	void BuildFloor()
	{
		var root = FindChild( GameObject, "Floor" ) ?? ReplaceGroup( "Floor" );
		var span = BoundaryRadius * 2.8f;
		Blocks.SpawnBox( root, "Slab", new Vector3( 0, 0, -30f ), Rotation.Identity, new Vector3( span, span, 60f ), FloorTint );
	}

	void BuildTrack()
	{
		var root = FindChild( GameObject, "Track" ) ?? ReplaceGroup( "Track" );
		const int segments = 64;
		var step = MathF.Tau / segments;
		var arc = MathF.Tau * TrackRadius / segments;

		for ( var i = 0; i < segments; i++ )
		{
			var angle = step * i;
			var outward = ArenaGeometry.FromAngle( angle );
			var tangent = new Vector2( -outward.y, outward.x );
			var shade = i % 2 == 0 ? 1f : 0.82f;

			Blocks.SpawnBox( root, $"Track {i}",
				new Vector3( outward.x * TrackRadius, outward.y * TrackRadius, 3f ),
				Blocks.FlatFacing( tangent ),
				new Vector3( arc * 1.02f, TrackWidth, 6f ),
				TrackTint * shade, false );
		}

		BuildTrackEdge( Geometry.TrackInner, "Inner" );
		BuildTrackEdge( Geometry.TrackOuter, "Outer" );
	}

	void BuildFinish()
	{
		finishRoot = FindChild( GameObject, "Finish" ) ?? ReplaceGroup( "Finish" );

		var angle = StartAngle;
		var outward = ArenaGeometry.FromAngle( angle );
		var along = new Vector2( -outward.y, outward.x );
		var rot = Blocks.FlatFacing( along );
		var mid = new Vector3( outward.x * TrackRadius, outward.y * TrackRadius, 0f );

		const int cols = 8;
		const int rows = 3;
		var cellAlong = 18f;
		var cellAcross = (TrackWidth + 12f) / cols;

		for ( var row = 0; row < rows; row++ )
		{
			var shift = (row - (rows - 1) * 0.5f) * cellAlong;
			for ( var col = 0; col < cols; col++ )
			{
				var across = (col + 0.5f) * cellAcross - (TrackWidth + 12f) * 0.5f;
				var gold = (row + col) % 2 == 0;
				var pos = mid
					+ new Vector3( along.x, along.y, 0f ) * shift
					+ new Vector3( outward.x, outward.y, 0f ) * across
					+ Vector3.Up * 10f;
				PlaceFinish( $"Cell {row},{col}", pos, rot, new Vector3( cellAlong * 1.05f, cellAcross * 1.05f, 8f ),
					gold ? FinishGold : FinishInk, gold ? 0.7f : 0.12f );
			}
		}

		PlaceFinish( "Post Inner",
			new Vector3( outward.x * Geometry.TrackInner, outward.y * Geometry.TrackInner, 110f ),
			Rotation.Identity, new Vector3( 26f, 26f, 220f ), FinishGold, 0.65f );
		PlaceFinish( "Post Outer",
			new Vector3( outward.x * Geometry.TrackOuter, outward.y * Geometry.TrackOuter, 110f ),
			Rotation.Identity, new Vector3( 26f, 26f, 220f ), FinishGold, 0.65f );
		PlaceFinish( "Bar",
			mid + Vector3.Up * 214f, rot, new Vector3( 22f, TrackWidth + 36f, 16f ), FinishGold, 0.8f );
		PlaceFinish( "Flag Inner",
			new Vector3( outward.x * Geometry.TrackInner, outward.y * Geometry.TrackInner, 232f ),
			rot, new Vector3( 8f, 52f, 36f ), FinishGold, 0.9f );
		PlaceFinish( "Flag Outer",
			new Vector3( outward.x * Geometry.TrackOuter, outward.y * Geometry.TrackOuter, 232f ),
			rot, new Vector3( 8f, 52f, 36f ), FinishGold, 0.9f );

		for ( var i = 0; i < 3; i++ )
		{
			var width = 72f + i * 38f;
			var at = angle + 0.30f - i * 0.09f;
			var o = ArenaGeometry.FromAngle( at );
			var t = new Vector2( -o.y, o.x );
			PlaceFinish( $"Approach {i}",
				new Vector3( o.x * TrackRadius, o.y * TrackRadius, 10f ),
				Blocks.FlatFacing( t ),
				new Vector3( 14f, width, 8f ), FinishGold, 0.55f + i * 0.12f );
		}

		var inner = Geometry.TrackInner - 8f;
		var outer = Geometry.TrackOuter + 8f;
		beamInner = Scene.CreateObject();
		beamInner.Name = "Beam Inner";
		beamInner.Parent = finishRoot;
		beamInner.WorldPosition = new Vector3( outward.x * inner, outward.y * inner, 28f );

		beamOuter = Scene.CreateObject();
		beamOuter.Name = "Beam Outer";
		beamOuter.Parent = finishRoot;
		beamOuter.WorldPosition = new Vector3( outward.x * outer, outward.y * outer, 28f );

		var beamObject = Scene.CreateObject();
		beamObject.Name = "Finish Beam";
		beamObject.Parent = finishRoot;
		finishBeam = beamObject.AddComponent<PolyLine>();
		finishBeam.HeadWidth = 16f;
		finishBeam.TailWidth = 16f;
		finishBeam.HeadTint = FinishGold;
		finishBeam.TailTint = FinishGold;
		finishBeam.Apply();
		ApplyFinishBeam();

		var lamp = Scene.CreateObject();
		lamp.Name = "Finish Light";
		lamp.Parent = finishRoot;
		lamp.WorldPosition = mid + Vector3.Up * 80f;
		finishLight = lamp.AddComponent<PointLight>();
		finishLight.LightColor = FinishGold * 3.5f;
		finishLight.Radius = 720f;
	}

	void PlaceFinish( string name, Vector3 position, Rotation rotation, Vector3 size, Color tint, float pulse )
	{
		var go = Blocks.SpawnBox( finishRoot, name, position, rotation, size, tint, false );
		var mark = go.AddComponent<FinishPulse>();
		mark.Pulse = pulse;
	}

	void PulseFinish()
	{
		if ( finishMarks.Count == 0 )
			return;

		loop ??= Scene.GetAllComponents<GameLoop>().FirstOrDefault();
		var near = 0f;
		if ( loop.IsValid() && loop.Phase == RunPhase.Playing && !loop.InBossFight )
			near = Math.Clamp( (loop.LapFraction - 0.7f) / 0.3f, 0f, 1f );

		var wave = 0.5f + 0.5f * MathF.Sin( Time.Now * (3.4f + near * 10f) );
		var flash = MathX.Lerp( 0.18f, 1f, wave ) * MathX.Lerp( 0.5f, 1f, near );

		foreach ( var mark in finishMarks )
		{
			if ( !mark.Renderer.IsValid() )
				continue;

			mark.Renderer.Tint = Color.Lerp( mark.Tint, Color.White, flash * mark.Pulse );
		}

		if ( finishLight.IsValid() )
			finishLight.LightColor = FinishGold * (1.4f + 6.5f * flash);

		if ( finishBeam.IsValid() )
		{
			var glow = Color.Lerp( FinishGold, Color.White, flash * 0.7f );
			finishBeam.HeadTint = glow;
			finishBeam.TailTint = glow;
			finishBeam.Apply();
		}
	}

	void BuildTrackEdge( float radius, string label )
	{
		var root = FindChild( GameObject, "Edges" ) ?? ReplaceGroup( "Edges" );
		const int segments = 72;
		var step = MathF.Tau / segments;
		var arc = MathF.Tau * radius / segments;

		for ( var i = 0; i < segments; i++ )
		{
			var angle = step * i;
			var outward = ArenaGeometry.FromAngle( angle );
			var tangent = new Vector2( -outward.y, outward.x );

			Blocks.SpawnBox( root, $"Edge {label} {i}",
				new Vector3( outward.x * radius, outward.y * radius, 8f ),
				Blocks.FlatFacing( tangent ),
				new Vector3( arc * 1.02f, 9f, 10f ),
				TrackEdgeTint, false );
		}
	}

	void BuildRingWalls()
	{
		var root = FindChild( GameObject, "Walls" ) ?? ReplaceGroup( "Walls" );
		for ( var i = 0; i < Geometry.Walls.Count; i++ )
		{
			var wall = Geometry.Walls[i];
			if ( wall.Kind != WallKind.Boundary && wall.Kind != WallKind.Core )
				continue;

			SpawnWall( root, wall, i, true );
		}
	}

	void RebuildPanels()
	{
		if ( !panelRoot.IsValid() )
		{
			panelRoot = Scene.CreateObject();
			panelRoot.Name = "Panels";
			panelRoot.Parent = GameObject;
		}

		foreach ( var child in panelRoot.Children.ToArray() )
			child.Destroy();

		panelVisuals.Clear();

		for ( var i = Geometry.AuthoredCount; i < Geometry.Walls.Count; i++ )
		{
			var wall = Geometry.Walls[i];
			if ( wall.Kind != WallKind.Panel )
				continue;

			panelVisuals[i] = SpawnWall( panelRoot, wall, i, false );
		}
	}

	public void KickPanel( int index, Vector2 hitPos, Vector2 hitNormal, float extraDegrees = 0f, bool allowSecond = false )
	{
		Geometry.KickPanel( index, hitPos, hitNormal, extraDegrees, allowSecond );
		SyncPanel( index );
	}

	void SyncPanel( int index )
	{
		if ( !panelVisuals.TryGetValue( index, out var go ) )
			authoredVisuals.TryGetValue( index, out go );

		if ( !go.IsValid() || index < 0 || index >= Geometry.Walls.Count )
			return;

		var wall = Geometry.Walls[index];
		if ( wall.Kind != WallKind.Panel )
			return;

		var (thickness, height, _) = WallSize( wall.Kind );
		var size = new Vector3( wall.Length + thickness, thickness, height );
		var bounds = Blocks.Box.Bounds.Size;
		go.WorldPosition = new Vector3( wall.Center.x, wall.Center.y, height * 0.5f );
		go.WorldRotation = Blocks.FlatFacing( wall.Direction );
		go.WorldScale = new Vector3(
			bounds.x > 0.001f ? size.x / bounds.x : 1f,
			bounds.y > 0.001f ? size.y / bounds.y : 1f,
			bounds.z > 0.001f ? size.z / bounds.z : 1f );
	}

	static (float Thickness, float Height, Color Tint) WallSize( WallKind kind ) => kind switch
	{
		WallKind.Boundary => (32f, 140f, BoundaryTint),
		WallKind.Core => (36f, 170f, CoreTint),
		_ => (26f, 115f, PanelTint)
	};

	GameObject SpawnWall( GameObject parent, WallSegment wall, int index, bool authored )
	{
		var (thickness, height, tint) = WallSize( wall.Kind );
		var center = wall.Center;
		var go = Blocks.SpawnBox( parent, $"Wall {wall.Kind} {index}",
			new Vector3( center.x, center.y, height * 0.5f ),
			Blocks.FlatFacing( wall.Direction ),
			new Vector3( wall.Length + thickness, thickness, height ),
			tint );

		if ( authored )
		{
			var marker = go.AddComponent<ArenaWall>();
			marker.Kind = wall.Kind;
		}

		return go;
	}

	GameObject ReplaceGroup( string name )
	{
		var existing = FindChild( GameObject, name );
		existing?.Destroy();

		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = GameObject;
		return go;
	}

	static GameObject FindChild( GameObject parent, string name )
	{
		if ( !parent.IsValid() )
			return null;

		foreach ( var child in parent.Children )
		{
			if ( child.Name == name )
				return child;
		}

		return null;
	}

	static bool IsUnder( GameObject go, string name )
	{
		var current = go;
		while ( current.IsValid() )
		{
			if ( current.Name == name )
				return true;

			current = current.Parent;
		}

		return false;
	}
}
