namespace LoopedLoaded;

public sealed class ArenaBuilder : Component
{
	[Property] public string Code { get; set; } = "GLASS";
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
	public static readonly Color GlassFloorTint = new Color( 0.042f, 0.052f, 0.07f );
	public static readonly Color GlassTrackTint = new Color( 0.16f, 0.20f, 0.26f );
	public static readonly Color GlassEdgeTint = new Color( 0.72f, 0.88f, 0.96f );
	public static readonly Color GlassBoundaryTint = new Color( 0.70f, 0.82f, 0.92f );
	public static readonly Color GlassCoreTint = new Color( 0.38f, 0.52f, 0.64f );
	public static readonly Color GlassPanelTint = new Color( 0.78f, 0.92f, 0.98f );
	public static readonly Color GlassCrackTint = new Color( 0.94f, 0.98f, 1f );
	public static readonly Color GlassShardTint = new Color( 0.88f, 0.96f, 1f );
	const float SpinnerScale = 0.5f;
	static readonly Color GlassPane = new Color( 0.56f, 0.67f, 0.62f );
	static readonly Color GlassPaneBright = new Color( 0.77f, 0.87f, 0.86f );

	public RunLocation Location { get; private set; } = RunLocation.Glass;
	public int GlassBroken => Geometry.GlassBroken;
	public bool GlassBoard => Location == RunLocation.Glass;

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
	readonly Dictionary<int, GameObject> spinnerRotors = new();
	readonly List<int> spinnerIds = new();
	readonly Dictionary<int, GameObject> authoredVisuals = new();
	readonly Dictionary<int, GameObject> shardVisuals = new();
	readonly List<(ModelRenderer Renderer, Color Tint, float Pulse)> finishMarks = new();
	readonly List<int> expiredShards = new();
	readonly List<GlassBit> glassBits = new();

	protected override void OnAwake() => BindScene();

	protected override void OnStart() => BindScene();

	public void BindScene()
	{
		geometry ??= new ArenaGeometry();
		geometry.TrackRadius = TrackRadius;
		geometry.TrackWidth = TrackWidth;
		geometry.BoundaryRadius = BoundaryRadius;
		geometry.CoreRadius = CoreRadius;
		geometry.GlassRules = Location == RunLocation.Glass;

		authoredVisuals.Clear();
		var walls = new List<WallSegment>();
		foreach ( var marker in Descendants<ArenaWall>() )
		{
			walls.Add( marker.ToSegment() );
			if ( marker.Kind == WallKind.Panel )
				authoredVisuals[walls.Count - 1] = marker.GameObject;

			FindChild( marker.GameObject, "Glass" )?.Destroy();
		}

		geometry.ApplyAuthored( walls );

		finishRoot = FindChild( GameObject, "Finish" );
		panelRoot = FindChild( GameObject, "Panels" );
		CollectFinish();
	}

	public void ClearGeneratedLayout()
	{
		if ( geometry is null )
			BindScene();

		Geometry.ClearBossWalls();
		Geometry.ClearGeneratedPanels();
		ClearRuntimePanels();
		ClearShards();
	}

	public void RollLayout( int lap, int seed )
	{
		if ( geometry is null )
			BindScene();

		Geometry.GlassRules = Location == RunLocation.Glass;
		Geometry.ClearBossWalls();
		Geometry.GeneratePanels( lap, seed, SpinWallLength() );
		RebuildPanels();
		RebuildSpinners();
		ClearShards();
	}

	public void ApplyLocation( RunLocation location )
	{
		Location = location;
		Code = Locations.Code( location );
		if ( geometry is null )
			BindScene();

		Geometry.GlassRules = location == RunLocation.Glass;
		DressLocation();
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

	protected override void OnUpdate()
	{
		PulseFinish();
		TickShards();
		TickGlass();
		TickSpinners();
	}

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

	void DressLocation()
	{
		var glass = GlassBoard;
		RecolorGroup( "Floor", glass ? GlassFloorTint : FloorTint );
		RecolorGroup( "Track", glass ? GlassTrackTint : TrackTint );
		RecolorGroup( "Edges", glass ? GlassEdgeTint : TrackEdgeTint );
		RecolorNamed( "Walls", "Core", glass ? GlassCoreTint : CoreTint );
		RecolorNamed( "Walls", "Boundary", glass ? GlassBoundaryTint : BoundaryTint );
	}

	void RecolorGroup( string name, Color tint )
	{
		var root = FindChild( GameObject, name );
		if ( !root.IsValid() )
			return;

		foreach ( var renderer in root.GetComponentsInChildren<ModelRenderer>( true ) )
		{
			if ( renderer.IsValid() )
				renderer.Tint = tint;
		}
	}

	void RecolorNamed( string group, string token, Color tint )
	{
		var root = FindChild( GameObject, group );
		if ( !root.IsValid() )
			return;

		foreach ( var child in root.Children )
		{
			if ( !child.Name.Contains( token ) )
				continue;

			foreach ( var renderer in child.GetComponentsInChildren<ModelRenderer>( true ) )
			{
				if ( renderer.IsValid() && renderer.GameObject.Name != "Glass" )
					renderer.Tint = tint;
			}
		}
	}

	void TickShards()
	{
		if ( geometry is null )
			return;

		Geometry.CollectExpiredShards( Time.Now, expiredShards );
		foreach ( var index in expiredShards )
		{
			if ( !shardVisuals.TryGetValue( index, out var go ) )
				continue;

			shardVisuals.Remove( index );
			if ( go.IsValid() )
				go.Destroy();
		}
	}

	void ClearShards()
	{
		foreach ( var go in shardVisuals.Values )
		{
			if ( go.IsValid() )
				go.Destroy();
		}

		shardVisuals.Clear();

		foreach ( var bit in glassBits )
		{
			if ( bit.Body.IsValid() )
				bit.Body.Destroy();
		}

		glassBits.Clear();
	}

	void MarkCracked( int index )
	{
		if ( !panelVisuals.TryGetValue( index, out var go ) || !go.IsValid() )
			return;

		SeatGlass( go, true );
	}

	void RemovePanelVisual( int index )
	{
		if ( panelVisuals.Remove( index, out var go ) && go.IsValid() )
			go.Destroy();
	}

	GameObject ShardRoot()
	{
		var root = FindChild( GameObject, "Shards" );
		if ( root.IsValid() )
			return root;

		root = Scene.CreateObject();
		root.Name = "Shards";
		root.Parent = GameObject;
		return root;
	}

	void ClearRuntimePanels()
	{
		foreach ( var go in panelVisuals.Values )
		{
			if ( go.IsValid() )
				go.Destroy();
		}

		panelVisuals.Clear();
		spinnerRotors.Clear();

		if ( !panelRoot.IsValid() )
			return;

		foreach ( var child in panelRoot.Children.ToArray() )
		{
			if ( child.GetComponent<ArenaWall>().IsValid() )
				continue;

			child.Destroy();
		}
	}

	void RebuildPanels()
	{
		if ( !panelRoot.IsValid() )
		{
			panelRoot = FindChild( GameObject, "Panels" );
			if ( !panelRoot.IsValid() )
			{
				panelRoot = Scene.CreateObject();
				panelRoot.Name = "Panels";
				panelRoot.Parent = GameObject;
			}
		}

		ClearRuntimePanels();

		for ( var i = Geometry.AuthoredCount; i < Geometry.Walls.Count; i++ )
		{
			var wall = Geometry.Walls[i];
			if ( wall.Kind != WallKind.Panel )
				continue;

			panelVisuals[i] = SpawnWall( panelRoot, wall, i, false );
		}
	}

	float SpinWallLength()
	{
		var model = Blocks.SpinWall;
		return (model.IsValid() && model.Bounds.Size.x > 1f ? model.Bounds.Size.x : 830f) * SpinnerScale;
	}

	void RebuildSpinners()
	{
		Geometry.CollectSpinners( spinnerIds );
		if ( spinnerIds.Count == 0 )
			return;

		if ( !panelRoot.IsValid() )
		{
			panelRoot = FindChild( GameObject, "Panels" );
			if ( !panelRoot.IsValid() )
			{
				panelRoot = Scene.CreateObject();
				panelRoot.Name = "Panels";
				panelRoot.Parent = GameObject;
			}
		}

		foreach ( var index in spinnerIds )
		{
			if ( index < 0 || index >= Geometry.Walls.Count )
				continue;

			spinnerRotors[index] = SpawnSpinner( panelRoot, Geometry.Walls[index] );
		}
	}

	GameObject SpawnSpinner( GameObject parent, WallSegment wall )
	{
		var root = Scene.CreateObject();
		root.Name = "Spin Wall";
		root.Parent = parent;
		root.WorldPosition = new Vector3( wall.Center.x, wall.Center.y, 0f );
		root.WorldRotation = Rotation.Identity;
		root.LocalScale = Vector3.One * SpinnerScale;

		var stand = Scene.CreateObject();
		stand.Name = "Stand";
		stand.Parent = root;
		stand.LocalPosition = Vector3.Zero;
		stand.LocalRotation = Rotation.Identity;
		stand.LocalScale = Vector3.One;
		var standRenderer = stand.AddComponent<ModelRenderer>();
		standRenderer.Model = Blocks.SpinStand;
		standRenderer.MaterialOverride = Blocks.WallReflect;
		standRenderer.Tint = Color.White;

		var rotor = Scene.CreateObject();
		rotor.Name = "Rotor";
		rotor.Parent = root;
		rotor.LocalPosition = Vector3.Zero;
		rotor.LocalScale = Vector3.One;
		rotor.WorldRotation = Blocks.FlatFacing( wall.Direction );
		var renderer = rotor.AddComponent<ModelRenderer>();
		renderer.Model = Blocks.SpinWall;
		renderer.MaterialOverride = Blocks.WallReflect;
		renderer.Tint = Color.White;
		return rotor;
	}

	void TickSpinners()
	{
		if ( spinnerRotors.Count == 0 )
			return;

		loop ??= Scene.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( loop.IsValid() && loop.Paused )
			return;

		Geometry.AdvanceSpinners( Time.Delta );
		foreach ( var pair in spinnerRotors )
		{
			if ( !pair.Value.IsValid() || pair.Key < 0 || pair.Key >= Geometry.Walls.Count )
				continue;

			pair.Value.WorldRotation = Blocks.FlatFacing( Geometry.Walls[pair.Key].Direction );
		}
	}

	public void PushSpinner( int index, Vector2 hitPos, Vector2 incoming )
	{
		Geometry.PushSpinner( index, hitPos, incoming );
	}

	public void KickPanel( int index, Vector2 hitPos, Vector2 hitNormal, float extraDegrees = 0f, bool allowSecond = false )
	{
		Geometry.KickPanel( index, hitPos, hitNormal, extraDegrees, allowSecond );
		SyncPanel( index );
	}

	public GlassHit StrikeBoard( int index, Vector2 hitPos, Vector2 hitNormal, float extraDegrees = 0f, bool allowSecond = false )
	{
		if ( index < 0 || index >= Geometry.Walls.Count )
			return GlassHit.None;

		var wall = Geometry.Walls[index];
		if ( wall.Kind == WallKind.Shard )
			return GlassHit.None;

		var hit = Geometry.StrikePanel( index, hitPos, hitNormal, extraDegrees, allowSecond );
		if ( hit == GlassHit.Kick )
		{
			SyncPanel( index );
			return hit;
		}

		if ( hit == GlassHit.Crack )
		{
			SyncPanel( index );
			MarkCracked( index );
			var world = Geometry.ToPlayWorld( hitPos );
			ArenaSounds.Crack( world );
			ImpactFlash.Spawn( Scene, world, GlassCrackTint, 1.15f );
			return hit;
		}

		if ( hit == GlassHit.Shatter )
		{
			RemovePanelVisual( index );
			BurstGlass( wall );
			var world = Geometry.ToPlayWorld( hitPos );
			ArenaSounds.Shatter( world );
			ImpactFlash.Spawn( Scene, world, GlassShardTint, 1.8f );
			return hit;
		}

		return hit;
	}

	public void DropShard( Vector2 center, Vector2 along )
	{
		if ( along.Length < 0.01f )
			along = Vector2.Right;

		BurstGlass( new WallSegment( center - along.Normal * 40f, center + along.Normal * 40f, Vector2.Zero, WallKind.Panel ) );
	}

	void BurstGlass( WallSegment wall )
	{
		var root = ShardRoot();
		var along = wall.Direction;
		var normal = wall.Normal;
		if ( along.Length < 0.01f )
			along = Vector2.Right;
		if ( normal.Length < 0.01f )
			normal = new Vector2( -along.y, along.x );

		for ( var i = 0; i < 18; i++ )
		{
			var spot = wall.Center + along * (wall.Length * Game.Random.Float( -0.42f, 0.42f )) + normal * Game.Random.Float( -16f, 16f );
			var at = new Vector3( spot.x, spot.y, Game.Random.Float( 16f, 108f ) );
			var body = Scene.CreateObject();
			body.Name = "Glass Bit";
			body.Parent = root;
			body.WorldPosition = at;
			body.WorldRotation = Rotation.FromYaw( Game.Random.Float( 0f, 360f ) );

			var tint = Color.Lerp( GlassPane, GlassPaneBright, Game.Random.Float( 0.15f, 1f ) );
			var size = new Vector3( Game.Random.Float( 12f, 34f ), Game.Random.Float( 1.6f, 3.4f ), Game.Random.Float( 8f, 24f ) );
			var mesh = Blocks.SpawnBox( body, "Mesh", at, body.WorldRotation, size, tint, false );
			mesh.LocalPosition = Vector3.Zero;
			mesh.LocalRotation = Rotation.Identity;
			mesh.LocalScale = Blocks.Fit( Blocks.Box, size );

			glassBits.Add( new GlassBit
			{
				Body = body,
				Velocity = new Vector3( normal.x, normal.y, 0f ) * Game.Random.Float( -70f, 70f )
					+ new Vector3( along.x, along.y, 0f ) * Game.Random.Float( -36f, 36f )
					+ Vector3.Up * Game.Random.Float( 30f, 160f ),
				Spin = new Vector3( Game.Random.Float( -8f, 8f ), Game.Random.Float( -8f, 8f ), Game.Random.Float( -8f, 8f ) )
			} );
		}
	}

	void TickGlass()
	{
		if ( glassBits.Count == 0 )
			return;

		loop ??= Scene.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( loop.IsValid() && loop.Paused )
			return;

		var dt = Time.Delta;
		for ( var i = glassBits.Count - 1; i >= 0; i-- )
		{
			var bit = glassBits[i];
			if ( !bit.Body.IsValid() )
			{
				glassBits.RemoveAt( i );
				continue;
			}

			if ( bit.Rest )
				continue;

			bit.Velocity += Vector3.Down * 1400f * dt;
			var next = bit.Body.WorldPosition + bit.Velocity * dt;
			if ( next.z < 8f )
			{
				next.z = 8f;
				bit.Velocity = new Vector3( bit.Velocity.x * 0.4f, bit.Velocity.y * 0.4f, bit.Velocity.z < 0f ? bit.Velocity.z * -0.2f : bit.Velocity.z );
				bit.Spin *= 0.35f;
				if ( bit.Velocity.Length < 28f )
				{
					bit.Velocity = Vector3.Zero;
					bit.Spin = Vector3.Zero;
					bit.Rest = true;
				}
			}

			bit.Body.WorldPosition = next;
			if ( !bit.Rest && bit.Spin.Length > 0.05f )
				bit.Body.WorldRotation *= Rotation.FromAxis( bit.Spin.Normal, MathX.RadianToDegree( bit.Spin.Length * dt ) );
		}
	}

	sealed class GlassBit
	{
		public GameObject Body;
		public Vector3 Velocity;
		public Vector3 Spin;
		public bool Rest;
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

		if ( panelVisuals.ContainsKey( index ) )
			SeatGlass( go, Geometry.PanelCracked( index ) );
	}

	(float Thickness, float Height, Color Tint) WallSize( WallKind kind )
	{
		if ( kind == WallKind.Shard )
			return (8f, 88f, GlassShardTint);

		if ( GlassBoard )
		{
			return kind switch
			{
				WallKind.Boundary => (32f, 140f, GlassBoundaryTint),
				WallKind.Core => (36f, 170f, GlassCoreTint),
				_ => (22f, 108f, GlassPanelTint)
			};
		}

		return kind switch
		{
			WallKind.Boundary => (32f, 140f, BoundaryTint),
			WallKind.Core => (36f, 170f, CoreTint),
			_ => (26f, 115f, PanelTint)
		};
	}

	GameObject SpawnWall( GameObject parent, WallSegment wall, int index, bool authored )
	{
		var (thickness, height, tint) = WallSize( wall.Kind );
		var center = wall.Center;
		var go = Blocks.SpawnBox( parent, $"Wall {wall.Kind} {index}",
			new Vector3( center.x, center.y, height * 0.5f ),
			Blocks.FlatFacing( wall.Direction ),
			new Vector3( wall.Length + thickness, thickness, height ),
			tint );

		if ( wall.Kind != WallKind.Shard )
		{
			var box = go.GetComponent<ModelRenderer>();
			if ( box.IsValid() )
				box.Enabled = false;

			if ( !authored )
				SeatGlass( go, false );
		}

		if ( authored )
		{
			var marker = go.AddComponent<ArenaWall>();
			marker.Kind = wall.Kind;
		}

		return go;
	}

	void SeatGlass( GameObject root, bool damaged )
	{
		if ( !root.IsValid() )
			return;

		var model = Blocks.Wall;
		if ( !model.IsValid() )
			return;

		var box = root.GetComponent<ModelRenderer>();
		if ( box.IsValid() )
			box.Enabled = false;

		var visual = FindChild( root, "Glass" );
		if ( !visual.IsValid() )
		{
			visual = Scene.CreateObject();
			visual.Name = "Glass";
			visual.Parent = root;
		}

		var renderer = visual.GetComponent<ModelRenderer>() ?? visual.AddComponent<ModelRenderer>();
		renderer.Model = model;
		renderer.MaterialOverride = damaged ? Blocks.WallDamaged : Blocks.WallGlass;
		renderer.Tint = Color.White;
		renderer.RenderType = ModelRenderer.ShadowRenderType.On;

		var rotation = root.WorldRotation;
		var scale = root.WorldScale;
		var bounds = model.Bounds;
		var center = (bounds.Mins + bounds.Maxs) * 0.5f;
		var anchor = new Vector3( center.x, center.y, bounds.Mins.z );
		var origin = new Vector3( root.WorldPosition.x, root.WorldPosition.y, 0f ) - rotation * anchor;
		var local = rotation.Inverse * (origin - root.WorldPosition);
		var sx = MathF.Abs( scale.x ) > 0.001f ? scale.x : 1f;
		var sy = MathF.Abs( scale.y ) > 0.001f ? scale.y : 1f;
		var sz = MathF.Abs( scale.z ) > 0.001f ? scale.z : 1f;

		visual.LocalRotation = Rotation.Identity;
		visual.LocalScale = new Vector3( 1f / sx, 1f / sy, 1f / sz );
		visual.LocalPosition = new Vector3( local.x / sx, local.y / sy, local.z / sz );
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

	IEnumerable<T> Descendants<T>() where T : Component
	{
		foreach ( var component in Scene.GetAllComponents<T>() )
		{
			if ( !component.IsValid() )
				continue;

			var current = component.GameObject;
			while ( current.IsValid() )
			{
				if ( current == GameObject )
				{
					yield return component;
					break;
				}

				current = current.Parent;
			}
		}
	}
}
