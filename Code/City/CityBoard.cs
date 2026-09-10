namespace LoopedLoaded;

public sealed class CityBoard : Component
{
	[Property] public GameLoop Loop { get; set; }
	[Property] public int Size { get; set; } = 5;
	[Property] public float CellSize { get; set; } = 180f;
	[Property] public float PlayHeight { get; set; } = 40f;

	public int Warehouse { get; private set; }
	public BuildingKind Selected { get; private set; } = BuildingKind.Infirmary;
	public int PlacementFacing { get; private set; }
	public CityMode Mode { get; private set; } = CityMode.Build;
	public bool Building => Mode == CityMode.Build;
	public CityPlot Hovered { get; private set; }
	public Vector2 Cursor { get; private set; }
	public Vector3 Anchor => GameObject.WorldPosition;
	public Vector3 Center => Anchor;
	public float Span => Size * CellSize;
	public Vector3 ShooterStand => shooter.IsValid()
		? shooter.WorldPosition
		: Anchor + new Vector3( 0f, -Span * 0.5f - 260f, 0f );

	readonly List<CityPlot> plots = new();
	readonly List<GameObject> tiles = new();
	GameObject stage;
	GameObject hover;
	GameObject shooter;
	GameObject cursor;
	PolyLine aim;
	PolyLine ghost;
	GameObject ghostHeart;
	GameObject ghostMuscle;
	GameObject ghostAdrenal;
	GameObject ghostLungLeft;
	GameObject ghostLungRight;
	GameObject ghostLiver;
	bool bound;

	public void Deposit( int rounds )
	{
		Warehouse += Math.Max( 0, rounds );
	}

	public GameSave Capture( int bestExtract )
	{
		EnsureBuilt();
		var save = new GameSave
		{
			Warehouse = Warehouse,
			BestExtract = bestExtract
		};

		foreach ( var plot in plots )
		{
			if ( !plot.Occupied )
				continue;

			save.Plots.Add( new PlotSave
			{
				X = plot.X,
				Y = plot.Y,
				Occupied = true,
				Kind = (int)plot.Kind,
				Level = plot.Level,
				Hits = plot.Hits,
				Facing = plot.Facing
			} );
		}

		return save;
	}

	public void Apply( GameSave save )
	{
		EnsureBuilt();
		WipePlots();
		Warehouse = save is null ? 0 : Math.Max( 0, save.Warehouse );
		if ( save?.Plots is null )
			return;

		foreach ( var row in save.Plots )
		{
			if ( row is null || !row.Occupied || row.X < 0 || row.Y < 0 || row.X >= Size || row.Y >= Size )
				continue;

			if ( row.Kind < 0 || row.Kind >= Buildings.All.Length )
				continue;

			var plot = FindPlot( row.X, row.Y );
			if ( plot is null )
				continue;

			plot.Occupied = true;
			plot.Kind = (BuildingKind)row.Kind;
			plot.Level = Math.Clamp( row.Level, 0, Buildings.MaxLevel( plot.Kind ) );
			plot.Facing = row.Facing & 3;
			plot.Hits = plot.Maxed ? 0 : Math.Clamp( row.Hits, 0, Math.Max( 0, plot.NextCost ) );
			RefreshPlot( plot );
		}
	}

	public void Wipe()
	{
		EnsureBuilt();
		Warehouse = 0;
		WipePlots();
	}

	void WipePlots()
	{
		foreach ( var plot in plots )
		{
			plot.Occupied = false;
			plot.Kind = default;
			plot.Level = 0;
			plot.Hits = 0;
			plot.Facing = 0;
			RefreshPlot( plot );
		}
	}

	public CityStats Stats()
	{
		var health = 0;
		var damage = 0;
		var boost = 0;
		var brake = 0;
		var show = 0;

		foreach ( var plot in plots )
		{
			if ( !plot.Working )
				continue;

			var extra = HasSameNeighbor( plot ) ? 1 : 0;
			var power = plot.Level + extra;

			switch ( plot.Kind )
			{
				case BuildingKind.Infirmary: health += Progression.RankValue( power ); break;
				case BuildingKind.Anvil: damage += Progression.RankValue( power ); break;
				case BuildingKind.Booster: boost += power; break;
				case BuildingKind.Brake: brake += power; break;
				case BuildingKind.Showcase: show += power; break;
			}
		}

		return new CityStats
		{
			BonusHealth = health,
			BonusDamage = damage,
			DashCooldownScale = Progression.DashScale( boost ),
			DashCharges = 1,
			SlowUnlocked = brake > 0,
			SlowDrain = Progression.SlowDrain( brake ),
			OfferCount = Math.Clamp( 2 + show, 2, 3 )
		};
	}

	protected override void OnAwake() => BindScene();

	public void EnsureBuilt() => BindScene();

	public void BindScene()
	{
		if ( bound )
			return;

		stage = FindChild( GameObject, "Stage" ) ?? GameObject;
		CollectCells();

		shooter = FindByName( stage, "City Shooter" );
		EnsureOverlays();
		bound = true;
		SetVisible( false );
	}

	[Button( "Fill Default City" )]
	public void FillDefaultCity()
	{
		stage = FindChild( GameObject, "Stage" ) ?? ReplaceChild( GameObject, "Stage" );
		foreach ( var child in stage.Children.ToArray() )
		{
			if ( child.Name == "Runtime" )
				continue;

			child.Destroy();
		}

		plots.Clear();
		tiles.Clear();

		Blocks.SpawnBox( stage, "Ground", Anchor + new Vector3( 0f, -180f, -18f ), Rotation.Identity,
			new Vector3( Span * 1.45f, Span * 1.9f, 20f ), new Color( 0.07f, 0.09f, 0.12f ), false );

		for ( var y = 0; y < Size; y++ )
		{
			for ( var x = 0; x < Size; x++ )
			{
				var pos = GridWorld( x, y );
				var shade = (x + y) % 2 == 0 ? 1f : 0.78f;
				var tile = Blocks.SpawnBox( stage, $"Tile {x},{y}", pos + Vector3.Up * 2f, Rotation.Identity,
					new Vector3( CellSize * 0.92f, CellSize * 0.92f, 6f ),
					new Color( 0.12f, 0.15f, 0.2f ) * shade, false );
				var cell = tile.AddComponent<CityCell>();
				cell.X = x;
				cell.Y = y;
			}
		}

		var stand = Anchor + new Vector3( 0f, -Span * 0.5f - 260f, 0f );
		Blocks.SpawnBox( stage, "Pad", stand + Vector3.Up * -8f, Rotation.Identity,
			new Vector3( 220f, 140f, 12f ), new Color( 0.18f, 0.22f, 0.28f ), false );

		shooter = Scene.CreateObject();
		shooter.Name = "City Shooter";
		shooter.Parent = stage;
		shooter.WorldPosition = stand;

		Blocks.SpawnBox( shooter, "Torso", stand + Vector3.Up * 40f, Rotation.Identity, new Vector3( 46f, 46f, 80f ), new Color( 0.82f, 0.94f, 1f ) );
		Blocks.SpawnBox( shooter, "Barrel", stand + new Vector3( 54f, 0f, 46f ), Rotation.Identity, new Vector3( 76f, 16f, 16f ), new Color( 0.22f, 0.3f, 0.4f ) );

		CollectCells();
	}

	public void SetVisible( bool visible )
	{
		EnsureBuilt();
		if ( !visible )
			ClearShots();
		if ( stage.IsValid() )
			stage.Enabled = visible;
	}

	public void ClearShots()
	{
		foreach ( var shot in Scene.GetAllComponents<CityShot>().ToArray() )
		{
			if ( shot.IsValid() && shot.Board == this )
				shot.GameObject.Destroy();
		}
	}

	public void Tick()
	{
		EnsureBuilt();
		UpdateCursor();
		FaceShooter();
		PaintHover();
		PaintAim();
		PaintGhost();

		for ( var i = 0; i < Buildings.All.Length; i++ )
		{
			if ( !Input.Pressed( $"Slot{i + 1}" ) )
				continue;

			Selected = Buildings.All[i];
			if ( Mode != CityMode.Build )
				SetMode( CityMode.Build );
		}

		if ( Input.Pressed( "Use" ) || Input.Pressed( "Score" ) )
			SetMode( Mode == CityMode.Build ? CityMode.Shoot : CityMode.Build );

		if ( Building )
		{
			TickRotate();
			if ( Input.Pressed( "Attack1" ) && !(Loop.IsValid() && Loop.BlocksShot) )
				TryPlace();
			return;
		}

		if ( Input.Pressed( "Attack1" ) && !(Loop.IsValid() && Loop.BlocksShot) )
			TryFire();
	}

	void SetMode( CityMode mode )
	{
		if ( Mode == mode )
			return;

		Mode = mode;
		ArenaSounds.Change();
		Loop?.Announce( Building ? "BUILD" : "SHOOT" );
	}

	void TickRotate()
	{
		var wheel = Input.MouseWheel;
		if ( wheel.y > 0.1f )
			Turn( 1 );
		else if ( wheel.y < -0.1f )
			Turn( -1 );

		if ( Input.Pressed( "SlotPrev" ) || Input.Pressed( "Attack2" ) )
			Turn( 1 );

		if ( Input.Pressed( "SlotNext" ) )
			Turn( -1 );
	}

	void Turn( int delta )
	{
		PlacementFacing = (PlacementFacing + delta) & 3;
		ArenaSounds.MenuMove();
	}

	public void Miss( Vector2 at )
	{
		ImpactFlash.Spawn( Scene, new Vector3( at.x, at.y, PlayHeight ), new Color( 0.4f, 0.45f, 0.5f ), 0.6f );
		ArenaSounds.Miss( new Vector3( at.x, at.y, PlayHeight ) );
	}

	public void RegisterHit( CityPlot plot )
	{
		if ( plot is null || !plot.Occupied )
		{
			Miss( Cursor );
			return;
		}

		if ( plot.Maxed )
			return;

		plot.Hits++;
		var world = CellWorld( plot.X, plot.Y ) + Vector3.Up * 70f;
		ArenaSounds.Metal( world );
		ImpactFlash.Spawn( Scene, world, Buildings.Color( plot.Kind ), 1.1f );

		if ( plot.Hits >= plot.NextCost )
		{
			plot.Hits = 0;
			plot.Level++;
			ArenaSounds.Pickup( world );
			Loop?.Announce( plot.Working
				? $"{Buildings.Title( plot.Kind )} LV{plot.Level}  ·  REFLECTS"
				: $"{Buildings.Title( plot.Kind )} LV{plot.Level}" );
		}

		RefreshPlot( plot );
		Loop?.Autosave();
	}

	public bool OutOfBounds( Vector2 flat )
	{
		var min = Anchor - new Vector3( Span * 0.5f + 120f, Span * 0.5f + 420f, 0f );
		var max = Anchor + new Vector3( Span * 0.5f + 120f, Span * 0.5f + 80f, 0f );
		return flat.x < min.x || flat.y < min.y || flat.x > max.x || flat.y > max.y;
	}

	public bool Trace( Vector2 origin, Vector2 direction, float maxDistance, float radius, out CityHit hit )
	{
		hit = default;
		if ( direction.Length < 0.001f || maxDistance <= 0f )
			return false;

		direction = direction.Normal;
		var closest = maxDistance;
		var bestPen = float.MaxValue;
		var found = false;

		foreach ( var plot in plots )
		{
			if ( !plot.Occupied )
				continue;

			var poly = WorldPolygon( plot.Kind, plot.Facing, plot.X, plot.Y );
			var thick = PlotThickness( plot );
			for ( var i = 0; i < poly.Length; i++ )
			{
				if ( !TryWall( poly, i, thick, radius, out var wall ) )
					continue;

				if ( !ArenaGeometry.SweepBox( origin, direction, closest, wall.Center, wall.AxisX, wall.AxisY, wall.Hx, wall.Hy, out var travel, out var normal ) )
					continue;

				var pen = 0f;
				if ( travel <= 0.001f )
				{
					pen = WallPenetration( origin, wall );
					if ( found && hit.Distance <= 0.001f && pen >= bestPen )
						continue;

					bestPen = pen;
				}
				else if ( travel >= closest )
					continue;

				closest = travel;
				found = true;
				hit = new CityHit
				{
					Distance = travel,
					Position = travel <= 0.001f
						? origin + normal * (pen + 2f)
						: origin + direction * travel + normal * 2f,
					Normal = normal,
					Plot = plot
				};
			}
		}

		return found;
	}

	public void Separate( ref Vector2 flat, float radius )
	{
		for ( var pass = 0; pass < 8; pass++ )
		{
			var pushed = false;

			foreach ( var plot in plots )
			{
				if ( !plot.Occupied )
					continue;

				var poly = WorldPolygon( plot.Kind, plot.Facing, plot.X, plot.Y );
				var thick = PlotThickness( plot );
				for ( var i = 0; i < poly.Length; i++ )
				{
					if ( !TryWall( poly, i, thick, radius, out var wall ) )
						continue;

					var pen = WallPenetration( flat, wall );
					if ( pen <= 0f )
						continue;

					var to = flat - wall.Center;
					var lx = ArenaGeometry.Dot( to, wall.AxisX );
					var ly = ArenaGeometry.Dot( to, wall.AxisY );
					var px = wall.Hx - MathF.Abs( lx );
					var py = wall.Hy - MathF.Abs( ly );
					var normal = px < py
						? wall.AxisX * (lx >= 0f ? 1f : -1f)
						: wall.AxisY * (ly >= 0f ? 1f : -1f);
					flat += normal * (pen + 2f);
					pushed = true;
				}
			}

			if ( !pushed )
				return;
		}
	}

	public string HoverText()
	{
		if ( !Building )
		{
			if ( Hovered is null || !Hovered.Occupied )
				return "SHOOT  ·  LMB FIRE";

			if ( Hovered.Maxed )
				return $"{Buildings.Title( Hovered.Kind )} LV{Hovered.Level}  ·  REFLECTS";

			var hitsLeft = Hovered.NextCost - Hovered.Hits;
			var hitStage = Hovered.Working ? "UPGRADE · REFLECTS" : "FRAME";
			return $"{Buildings.Title( Hovered.Kind )}  ·  {hitStage}  ·  {hitsLeft} HITS";
		}

		if ( Hovered is null )
			return $"{Buildings.Title( Selected )}  ·  WHEEL ROTATE";

		if ( !Hovered.Occupied )
			return $"{Buildings.Title( Selected )}  ·  LMB PLACE  ·  WHEEL ROTATE";

		if ( !Hovered.Working )
			return $"{Buildings.Title( Hovered.Kind )} FRAME  ·  LMB REMOVE";

		if ( Hovered.Maxed )
			return $"{Buildings.Title( Hovered.Kind )} LV{Hovered.Level}  ·  MAX  ·  REFLECTS";

		var left = Hovered.NextCost - Hovered.Hits;
		var stage = Hovered.Working ? "UPGRADE · REFLECTS" : "FRAME";
		return $"{Buildings.Title( Hovered.Kind )} LV{Hovered.Level}  ·  {stage}  ·  {left} HITS";
	}

	Vector3 CellWorld( int x, int y )
	{
		foreach ( var plot in plots )
		{
			if ( plot.X == x && plot.Y == y && plot.Root.IsValid() )
			{
				var at = plot.Root.WorldPosition;
				return new Vector3( at.x, at.y, 0f );
			}
		}

		return GridWorld( x, y );
	}

	Vector3 GridWorld( int x, int y )
	{
		var origin = Anchor - new Vector3( Span * 0.5f, Span * 0.5f, 0f );
		return origin + new Vector3( (x + 0.5f) * CellSize, (y + 0.5f) * CellSize, 0f );
	}

	CityPlot FindPlot( int x, int y )
	{
		foreach ( var plot in plots )
		{
			if ( plot.X == x && plot.Y == y )
				return plot;
		}

		return null;
	}

	void CollectCells()
	{
		plots.Clear();
		tiles.Clear();

		var cells = GameObject.GetComponentsInChildren<CityCell>( true )
			.Where( cell => cell.IsValid() )
			.OrderBy( cell => cell.Y * Size + cell.X )
			.ToList();

		foreach ( var cell in cells )
		{
			plots.Add( new CityPlot
			{
				X = cell.X,
				Y = cell.Y,
				Root = cell.GameObject
			} );
			tiles.Add( cell.GameObject );
		}
	}

	void EnsureOverlays()
	{
		var runtime = FindChild( stage, "Runtime" );
		if ( !runtime.IsValid() )
		{
			runtime = Scene.CreateObject();
			runtime.Name = "Runtime";
			runtime.Parent = stage;
		}

		hover = FindByName( runtime, "Hover" );
		if ( !hover.IsValid() )
			hover = Blocks.SpawnBox( runtime, "Hover", GridWorld( 0, 0 ) + Vector3.Up * 8f, Rotation.Identity,
				new Vector3( CellSize * 0.96f, CellSize * 0.96f, 8f ), new Color( 1f, 0.85f, 0.35f ), false );

		cursor = FindByName( runtime, "Cursor" );
		if ( !cursor.IsValid() )
			cursor = Blocks.SpawnSphere( runtime, "Cursor", ShooterStand + Vector3.Up * PlayHeight, 26f, ShotColors.Player, false );

		var aimObject = FindByName( runtime, "City Aim" );
		if ( !aimObject.IsValid() )
		{
			aimObject = Scene.CreateObject();
			aimObject.Name = "City Aim";
			aimObject.Parent = runtime;
		}

		aim = aimObject.GetComponent<PolyLine>() ?? aimObject.AddComponent<PolyLine>();
		aim.HeadTint = new Color( 1f, 0.9f, 0.4f );
		aim.TailTint = new Color( 1f, 0.55f, 0.18f );
		aim.HeadWidth = 3f;
		aim.TailWidth = 8f;
		aim.Apply();

		var ghostObject = FindByName( runtime, "Ghost" );
		if ( !ghostObject.IsValid() )
		{
			ghostObject = Scene.CreateObject();
			ghostObject.Name = "Ghost";
			ghostObject.Parent = runtime;
		}

		ghost = ghostObject.GetComponent<PolyLine>() ?? ghostObject.AddComponent<PolyLine>();
		ghost.HeadWidth = 6f;
		ghost.TailWidth = 6f;
		ghost.Apply();

		EnsureGhostMuscle( runtime );
		EnsureGhostAdrenal( runtime );
		EnsureGhostLung( runtime );
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

	static GameObject FindByName( GameObject parent, string name )
	{
		if ( !parent.IsValid() )
			return null;

		if ( parent.Name == name )
			return parent;

		foreach ( var child in parent.Children )
		{
			var found = FindByName( child, name );
			if ( found.IsValid() )
				return found;
		}

		return null;
	}

	GameObject ReplaceChild( GameObject parent, string name )
	{
		FindChild( parent, name )?.Destroy();
		var go = Scene.CreateObject();
		go.Name = name;
		go.Parent = parent;
		return go;
	}

	void UpdateCursor()
	{
		Hovered = null;

		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var slope = ray.Forward.z;
		if ( MathF.Abs( slope ) < 0.0001f )
			return;

		var travel = (PlayHeight - ray.Position.z) / slope;
		if ( travel <= 0f )
			return;

		var point = ray.Position + ray.Forward * travel;
		Cursor = new Vector2( point.x, point.y );
		TryPlot( Cursor, out var plot );
		Hovered = plot;
	}

	bool TryPlot( Vector2 flat, out CityPlot plot )
	{
		plot = null;
		var best = float.MaxValue;

		foreach ( var candidate in plots )
		{
			if ( !candidate.Root.IsValid() )
				continue;

			var center = new Vector2( candidate.Root.WorldPosition.x, candidate.Root.WorldPosition.y );
			var delta = flat - center;
			if ( MathF.Abs( delta.x ) > CellSize * 0.5f || MathF.Abs( delta.y ) > CellSize * 0.5f )
				continue;

			var dist = delta.LengthSquared;
			if ( dist >= best )
				continue;

			best = dist;
			plot = candidate;
		}

		return plot is not null;
	}

	void TryPlace()
	{
		if ( Hovered is null )
		{
			ArenaSounds.Deny();
			return;
		}

		if ( Hovered.Occupied )
		{
			TryRemove();
			return;
		}

		Hovered.Occupied = true;
		Hovered.Kind = Selected;
		Hovered.Facing = PlacementFacing;
		Hovered.Level = 0;
		Hovered.Hits = 0;
		RefreshPlot( Hovered );
		ArenaSounds.MenuOk();
		Loop?.Announce( $"{Buildings.Title( Selected )} FRAME" );
		Loop?.Autosave();
	}

	void TryRemove()
	{
		if ( Hovered is null || !Hovered.Occupied || Hovered.Working )
		{
			ArenaSounds.Deny();
			return;
		}

		var title = Buildings.Title( Hovered.Kind );
		Hovered.Occupied = false;
		Hovered.Level = 0;
		Hovered.Hits = 0;
		Hovered.Facing = 0;
		RefreshPlot( Hovered );
		ArenaSounds.MenuBack();
		Loop?.Announce( $"{title} REMOVED" );
		Loop?.Autosave();
	}

	void TryFire()
	{
		if ( Warehouse <= 0 )
		{
			ArenaSounds.Deny();
			Loop?.Announce( "WAREHOUSE EMPTY" );
			return;
		}

		Warehouse--;

		var origin = ShooterStand + Vector3.Up * PlayHeight;
		var to = Cursor - new Vector2( ShooterStand.x, ShooterStand.y );
		if ( to.Length < 8f )
		{
			Warehouse++;
			ArenaSounds.Deny();
			return;
		}

		var go = Scene.CreateObject();
		go.Name = "City Round";
		if ( stage.IsValid() )
			go.Parent = stage;

		var shot = go.AddComponent<CityShot>();
		shot.Board = this;
		shot.Tint = ShotColors.Player;
		shot.Launch( origin, to );

		ArenaSounds.Fire( ShooterStand );
		Loop?.Autosave();
	}

	void FaceShooter()
	{
		if ( !shooter.IsValid() )
			return;

		var to = Cursor - new Vector2( ShooterStand.x, ShooterStand.y );
		if ( to.Length > 1f )
			shooter.WorldRotation = Blocks.FlatFacing( to );
	}

	void PaintAim()
	{
		var muzzle = ShooterStand + Vector3.Up * PlayHeight;
		var target = new Vector3( Cursor.x, Cursor.y, PlayHeight );

		if ( cursor.IsValid() )
		{
			cursor.Enabled = !Building;
			if ( !Building )
				cursor.WorldPosition = target;
		}

		if ( !aim.IsValid() )
			return;

		if ( Building )
		{
			aim.Clear();
			return;
		}

		var tint = ShotColors.Player;
		aim.HeadTint = tint;
		aim.TailTint = Color.Lerp( tint, Color.White, 0.35f );
		aim.Apply();
		aim.SetPoints( new List<Vector3> { muzzle, target } );
	}

	void PaintHover()
	{
		if ( !hover.IsValid() )
			return;

		if ( Hovered is null || !Building )
		{
			hover.Enabled = false;
			return;
		}

		hover.Enabled = true;
		hover.WorldPosition = CellWorld( Hovered.X, Hovered.Y ) + Vector3.Up * 8f;

		var renderer = hover.GetComponent<ModelRenderer>();
		if ( renderer.IsValid() )
		{
			if ( Hovered.Occupied && !Hovered.Working )
				renderer.Tint = new Color( 0.95f, 0.28f, 0.22f );
			else
				renderer.Tint = Hovered.Occupied ? Buildings.Color( Hovered.Kind ) : Buildings.Color( Selected );
		}
	}

	void PaintGhost()
	{
		if ( !Building || Hovered is null || Hovered.Occupied )
		{
			ghost?.Clear();
			HideGhostModels();
			return;
		}

		if ( Selected == BuildingKind.Infirmary )
		{
			ghost?.Clear();
			HideGhostModels();

			var runtime = ghost.IsValid() ? ghost.GameObject.Parent : FindChild( stage, "Runtime" );
			EnsureGhostHeart( runtime );
			if ( !ghostHeart.IsValid() )
				return;

			ghostHeart.Enabled = true;
			var tint = Buildings.Color( Selected );
			tint.a = 0.55f;
			PlaceHeart( ghostHeart, ghostHeart.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, tint );
			return;
		}

		if ( Selected == BuildingKind.Anvil )
		{
			ghost?.Clear();
			HideGhostModels();

			var runtime = ghost.IsValid() ? ghost.GameObject.Parent : FindChild( stage, "Runtime" );
			EnsureGhostMuscle( runtime );
			if ( !ghostMuscle.IsValid() )
				return;

			ghostMuscle.Enabled = true;
			var tint = Buildings.Color( Selected );
			tint.a = 0.55f;
			PlaceMuscle( ghostMuscle, ghostMuscle.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, tint );
			return;
		}

		if ( Selected == BuildingKind.Booster )
		{
			ghost?.Clear();
			HideGhostModels();

			var runtime = ghost.IsValid() ? ghost.GameObject.Parent : FindChild( stage, "Runtime" );
			EnsureGhostAdrenal( runtime );
			if ( !ghostAdrenal.IsValid() )
				return;

			ghostAdrenal.Enabled = true;
			var tint = Buildings.Color( Selected );
			tint.a = 0.55f;
			PlaceAdrenal( ghostAdrenal, ghostAdrenal.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, tint );
			return;
		}

		if ( Selected == BuildingKind.Brake )
		{
			ghost?.Clear();
			HideGhostModels();

			var runtime = ghost.IsValid() ? ghost.GameObject.Parent : FindChild( stage, "Runtime" );
			EnsureGhostLung( runtime );
			if ( !ghostLungLeft.IsValid() || !ghostLungRight.IsValid() )
				return;

			ghostLungLeft.Enabled = true;
			ghostLungRight.Enabled = true;
			var tint = Buildings.Color( Selected );
			tint.a = 0.55f;
			var color = tint;
			PlaceLung( ghostLungLeft, ghostLungLeft.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, color, -1 );
			PlaceLung( ghostLungRight, ghostLungRight.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, color, 1 );
			return;
		}

		if ( Selected == BuildingKind.Showcase )
		{
			ghost?.Clear();
			HideGhostModels();

			var runtime = ghost.IsValid() ? ghost.GameObject.Parent : FindChild( stage, "Runtime" );
			EnsureGhostLiver( runtime );
			if ( !ghostLiver.IsValid() )
				return;

			ghostLiver.Enabled = true;
			var tint = Buildings.Color( Selected );
			tint.a = 0.55f;
			PlaceLiver( ghostLiver, ghostLiver.GetComponent<ModelRenderer>(), Hovered.X, Hovered.Y, PlacementFacing, 34f, false, tint );
			return;
		}

		HideGhostModels();

		if ( !ghost.IsValid() )
			return;

		var outline = Buildings.Color( Selected );
		ghost.HeadTint = outline;
		ghost.TailTint = outline;
		ghost.Apply();
		ghost.SetPoints( OutlinePoints( Selected, PlacementFacing, Hovered.X, Hovered.Y, PlayHeight + 10f ) );
	}

	void RefreshPlot( CityPlot plot )
	{
		plot.Body?.Destroy();

		if ( !plot.Occupied )
			return;

		var tint = Buildings.Color( plot.Kind );
		if ( !plot.Working )
			tint *= 0.45f;

		var height = plot.Working ? 78f + plot.Level * 26f : 34f;
		var thick = PlotThickness( plot );
		var poly = WorldPolygon( plot.Kind, plot.Facing, plot.X, plot.Y );
		var center = CellWorld( plot.X, plot.Y );

		plot.Body = Scene.CreateObject();
		plot.Body.Name = "Building";
		plot.Body.Parent = plot.Root;
		plot.Body.WorldPosition = center;

		if ( plot.Kind == BuildingKind.Infirmary )
		{
			SpawnHeart( plot, tint, height );
		}
		else if ( plot.Kind == BuildingKind.Anvil )
		{
			SpawnMuscle( plot, tint, height );
		}
		else if ( plot.Kind == BuildingKind.Booster )
		{
			SpawnAdrenal( plot, tint, height );
		}
		else if ( plot.Kind == BuildingKind.Brake )
		{
			SpawnLung( plot, tint, height );
		}
		else if ( plot.Kind == BuildingKind.Showcase )
		{
			SpawnLiver( plot, tint, height );
		}
		else
		{
			for ( var i = 0; i < poly.Length; i++ )
			{
				var a = poly[i];
				var b = poly[(i + 1) % poly.Length];
				var mid = (a + b) * 0.5f;
				var span = b - a;
				var length = MathF.Max( 12f, span.Length );
				Blocks.SpawnBox( plot.Body, $"Edge {i}",
					new Vector3( mid.x, mid.y, height * 0.5f + 8f ),
					Blocks.FlatFacing( span ),
					new Vector3( length, thick, height ),
					tint );
			}

			SpawnAccent( plot, tint, height );
		}

		if ( plot.NextCost > 0 )
		{
			var fill = (float)plot.Hits / plot.NextCost;
			var barHeight = 10f + fill * 40f;
			Blocks.SpawnBox( plot.Body, "Progress",
				center + new Vector3( CellSize * 0.38f, 0f, 24f ),
				Rotation.Identity,
				new Vector3( 12f, 12f, barHeight ),
				Color.Lerp( new Color( 0.3f, 0.35f, 0.4f ), tint, fill ), false );
		}
	}

	void HideGhostModels()
	{
		if ( ghostHeart.IsValid() )
			ghostHeart.Enabled = false;
		if ( ghostMuscle.IsValid() )
			ghostMuscle.Enabled = false;
		if ( ghostAdrenal.IsValid() )
			ghostAdrenal.Enabled = false;
		if ( ghostLungLeft.IsValid() )
			ghostLungLeft.Enabled = false;
		if ( ghostLungRight.IsValid() )
			ghostLungRight.Enabled = false;
		if ( ghostLiver.IsValid() )
			ghostLiver.Enabled = false;
	}

	void EnsureGhostHeart( GameObject parent )
	{
		if ( ghostHeart.IsValid() )
			return;

		if ( !parent.IsValid() )
			return;

		ghostHeart = FindByName( parent, "Ghost Heart" );
		if ( !ghostHeart.IsValid() )
		{
			ghostHeart = Scene.CreateObject();
			ghostHeart.Name = "Ghost Heart";
			ghostHeart.Parent = parent;
		}

		var renderer = ghostHeart.GetComponent<ModelRenderer>() ?? ghostHeart.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/heart.vmdl" );
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		ghostHeart.Enabled = false;
	}

	void EnsureGhostMuscle( GameObject parent )
	{
		if ( ghostMuscle.IsValid() )
			return;

		if ( !parent.IsValid() )
			return;

		ghostMuscle = FindByName( parent, "Ghost Muscle" );
		if ( !ghostMuscle.IsValid() )
		{
			ghostMuscle = Scene.CreateObject();
			ghostMuscle.Name = "Ghost Muscle";
			ghostMuscle.Parent = parent;
		}

		var renderer = ghostMuscle.GetComponent<ModelRenderer>() ?? ghostMuscle.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/muscle.vmdl" );
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		ghostMuscle.Enabled = false;
	}

	void EnsureGhostAdrenal( GameObject parent )
	{
		if ( ghostAdrenal.IsValid() )
			return;

		if ( !parent.IsValid() )
			return;

		ghostAdrenal = FindByName( parent, "Ghost Adrenal" );
		if ( !ghostAdrenal.IsValid() )
		{
			ghostAdrenal = Scene.CreateObject();
			ghostAdrenal.Name = "Ghost Adrenal";
			ghostAdrenal.Parent = parent;
		}

		var renderer = ghostAdrenal.GetComponent<ModelRenderer>() ?? ghostAdrenal.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/adrenal.vmdl" );
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		ghostAdrenal.Enabled = false;
	}

	void EnsureGhostLung( GameObject parent )
	{
		if ( !parent.IsValid() )
			return;

		FindByName( parent, "Ghost Lung" )?.Destroy();
		ghostLungLeft = EnsureGhostLungObject( parent, ghostLungLeft, "Ghost Lung L" );
		ghostLungRight = EnsureGhostLungObject( parent, ghostLungRight, "Ghost Lung R" );
	}

	GameObject EnsureGhostLungObject( GameObject parent, GameObject ghost, string name )
	{
		if ( ghost.IsValid() )
			return ghost;

		ghost = FindByName( parent, name );
		if ( !ghost.IsValid() )
		{
			ghost = parent.Scene.CreateObject();
			ghost.Name = name;
			ghost.Parent = parent;
		}

		FindChild( ghost, "Mesh" )?.Destroy();
		var renderer = ghost.GetComponent<ModelRenderer>() ?? ghost.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/lung.vmdl" );
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		ghost.Enabled = false;
		return ghost;
	}

	void EnsureGhostLiver( GameObject parent )
	{
		if ( ghostLiver.IsValid() )
			return;

		if ( !parent.IsValid() )
			return;

		ghostLiver = FindByName( parent, "Ghost Liver" );
		if ( !ghostLiver.IsValid() )
		{
			ghostLiver = Scene.CreateObject();
			ghostLiver.Name = "Ghost Liver";
			ghostLiver.Parent = parent;
		}

		var renderer = ghostLiver.GetComponent<ModelRenderer>() ?? ghostLiver.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( "models/liver.vmdl" );
		renderer.RenderType = ModelRenderer.ShadowRenderType.Off;
		ghostLiver.Enabled = false;
	}

	void SpawnHeart( CityPlot plot, Color tint, float height )
	{
		var go = Scene.CreateObject();
		go.Name = "Heart";
		go.Parent = plot.Body;

		var renderer = go.AddComponent<ModelRenderer>();
		PlaceHeart( go, renderer, plot.X, plot.Y, plot.Facing, height, plot.Working, plot.Working ? Color.White : tint );
	}

	void PlaceHeart( GameObject go, ModelRenderer renderer, int x, int y, int facing, float height, bool working, Color tint )
	{
		var model = Model.Load( "models/heart.vmdl" );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var target = MathF.Max( height, CellSize * (working ? 0.62f : 0.42f) );
		var scale = (longest > 0.001f ? target / longest : 1f) * (0.85f / 1.5f);
		var center = CellWorld( x, y );
		var rotation = Rotation.FromYaw( facing * 90f ) * Rotation.FromPitch( -90f );

		go.WorldRotation = rotation;
		go.WorldPosition = center + Vector3.Up * (8f - RotatedMinZ( bounds, rotation ) * scale);

		if ( renderer.IsValid() )
		{
			renderer.Model = model;
			renderer.Tint = tint;
		}

		var pulse = go.GetComponent<HeartPulse>() ?? go.AddComponent<HeartPulse>();
		pulse.RestScale = scale;
		pulse.Strength = working ? 0.12f : 0.07f;
		pulse.Rate = working ? 1.2f : 0.9f;
		pulse.Seed = x * 97 + y * 13 + 1;
		pulse.Apply();
	}

	void SpawnMuscle( CityPlot plot, Color tint, float height )
	{
		var go = Scene.CreateObject();
		go.Name = "Muscle";
		go.Parent = plot.Body;

		var renderer = go.AddComponent<ModelRenderer>();
		PlaceMuscle( go, renderer, plot.X, plot.Y, plot.Facing, height, plot.Working, plot.Working ? Color.White : tint );
	}

	void PlaceMuscle( GameObject go, ModelRenderer renderer, int x, int y, int facing, float height, bool working, Color tint )
	{
		var model = Model.Load( "models/muscle.vmdl" );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var target = MathF.Max( height, CellSize * (working ? 0.62f : 0.42f) );
		var scale = (longest > 0.001f ? target / longest : 1f) * (0.85f / 1.5f);
		var center = CellWorld( x, y );
		var rotation = Rotation.FromYaw( facing * 90f ) * Rotation.FromPitch( -90f );

		go.WorldRotation = rotation;
		go.WorldScale = scale;
		go.WorldPosition = center + Vector3.Up * (8f - RotatedMinZ( bounds, rotation ) * scale);

		if ( renderer.IsValid() )
		{
			renderer.Model = model;
			renderer.Tint = tint;
		}
	}

	void SpawnAdrenal( CityPlot plot, Color tint, float height )
	{
		var go = Scene.CreateObject();
		go.Name = "Adrenal";
		go.Parent = plot.Body;

		var renderer = go.AddComponent<ModelRenderer>();
		PlaceAdrenal( go, renderer, plot.X, plot.Y, plot.Facing, height, plot.Working, plot.Working ? Color.White : tint );
	}

	void PlaceAdrenal( GameObject go, ModelRenderer renderer, int x, int y, int facing, float height, bool working, Color tint )
	{
		var model = Model.Load( "models/adrenal.vmdl" );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var target = MathF.Max( height, CellSize * (working ? 0.62f : 0.42f) );
		var scale = (longest > 0.001f ? target / longest : 1f) * (0.85f / 1.5f);
		var center = CellWorld( x, y );
		var rotation = Rotation.FromYaw( facing * 90f ) * Rotation.FromPitch( -90f );

		go.WorldRotation = rotation;
		go.WorldScale = scale;
		go.WorldPosition = center + Vector3.Up * (8f - RotatedMinZ( bounds, rotation ) * scale);

		if ( renderer.IsValid() )
		{
			renderer.Model = model;
			renderer.Tint = tint;
			renderer.Attributes.Set( "StripeSpeed", working ? 0.72f : 0.38f );
			renderer.Attributes.Set( "StripeWidth", working ? 0.04f : 0.055f );
			renderer.Attributes.Set( "StripeStrength", working ? 1.15f : 0.55f );
			renderer.Attributes.Set( "StripePhase", ( x * 0.37f + y * 0.19f ) % 1f );
		}
	}

	void SpawnLung( CityPlot plot, Color tint, float height )
	{
		var color = plot.Working ? Color.White : tint;
		SpawnOneLung( plot, "Lung L", color, height, -1 );
		SpawnOneLung( plot, "Lung R", color, height, 1 );
	}

	void SpawnOneLung( CityPlot plot, string name, Color tint, float height, int side )
	{
		var go = plot.Body.Scene.CreateObject();
		go.Name = name;
		go.Parent = plot.Body;

		var renderer = go.AddComponent<ModelRenderer>();
		PlaceLung( go, renderer, plot.X, plot.Y, plot.Facing, height, plot.Working, tint, side );
	}

	void PlaceLung( GameObject go, ModelRenderer renderer, int x, int y, int facing, float height, bool working, Color tint, int side )
	{
		var model = Model.Load( "models/lung.vmdl" );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var target = MathF.Max( height, CellSize * (working ? 0.62f : 0.42f) );
		var scale = (longest > 0.001f ? target / longest : 1f) * (0.85f / 1.5f);
		var center = CellWorld( x, y );
		var rotation = Rotation.From( -90f, 0f, facing * 90f + 90f );
		var along = Buildings.Rotate( Vector2.Right, facing );
		var sit = 8f - RotatedMinZ( bounds, rotation ) * scale;
		var offset = new Vector3( along.x, along.y, 0f ) * ( side * CellSize * 0.22f );

		go.WorldRotation = rotation;
		go.WorldPosition = center + offset + Vector3.Up * sit;

		if ( renderer.IsValid() )
		{
			renderer.Model = model;
			renderer.Tint = tint;
			renderer.MaterialOverride = null;
		}

		var breath = go.GetComponent<LungBreath>() ?? go.AddComponent<LungBreath>();
		breath.RestScale = scale;
		breath.Mirror = side < 0 ? LungMirror( rotation, along ) : Vector3.Zero;
		breath.Strength = working ? 0.1f : 0.055f;
		breath.Rate = working ? 0.3f : 0.22f;
		breath.Seed = x * 53 + y * 29 + 7 + side;
		breath.Apply();
	}

	static Vector3 LungMirror( Rotation rotation, Vector2 along )
	{
		var across = new Vector3( along.x, along.y, 0f );
		if ( across.Length < 0.01f )
			return new Vector3( 1f, 0f, 0f );

		across = across.Normal;
		var best = -1f;
		var mirror = new Vector3( 1f, 0f, 0f );
		TryAxis( rotation.Right, new Vector3( 1f, 0f, 0f ) );
		TryAxis( rotation.Up, new Vector3( 0f, 1f, 0f ) );
		TryAxis( rotation.Forward, new Vector3( 0f, 0f, 1f ) );
		return mirror;

		void TryAxis( Vector3 worldAxis, Vector3 candidate )
		{
			var xy = new Vector3( worldAxis.x, worldAxis.y, 0f );
			if ( xy.Length < 0.2f )
				return;

			var align = MathF.Abs( Vector3.Dot( xy.Normal, across ) );
			if ( align <= best )
				return;

			best = align;
			mirror = candidate;
		}
	}

	void SpawnLiver( CityPlot plot, Color tint, float height )
	{
		var go = Scene.CreateObject();
		go.Name = "Liver";
		go.Parent = plot.Body;

		var renderer = go.AddComponent<ModelRenderer>();
		PlaceLiver( go, renderer, plot.X, plot.Y, plot.Facing, height, plot.Working, plot.Working ? Color.White : tint );
	}

	void PlaceLiver( GameObject go, ModelRenderer renderer, int x, int y, int facing, float height, bool working, Color tint )
	{
		var model = Model.Load( "models/liver.vmdl" );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var target = MathF.Max( height, CellSize * (working ? 0.62f : 0.42f) );
		var scale = (longest > 0.001f ? target / longest : 1f) * (0.85f / 1.5f);
		var center = CellWorld( x, y );
		var rotation = Rotation.FromYaw( facing * 90f ) * Rotation.FromPitch( -90f );

		go.WorldRotation = rotation;
		go.WorldScale = scale;
		go.WorldPosition = center + Vector3.Up * (8f - RotatedMinZ( bounds, rotation ) * scale);

		if ( renderer.IsValid() )
		{
			renderer.Model = model;
			renderer.Tint = tint;
		}
	}

	static float RotatedMinZ( BBox bounds, Rotation rotation )
	{
		var minZ = float.MaxValue;
		for ( var ix = 0; ix < 2; ix++ )
		for ( var iy = 0; iy < 2; iy++ )
		for ( var iz = 0; iz < 2; iz++ )
		{
			var corner = new Vector3(
				ix == 0 ? bounds.Mins.x : bounds.Maxs.x,
				iy == 0 ? bounds.Mins.y : bounds.Maxs.y,
				iz == 0 ? bounds.Mins.z : bounds.Maxs.z );
			minZ = MathF.Min( minZ, (rotation * corner).z );
		}

		return minZ;
	}

	void SpawnAccent( CityPlot plot, Color tint, float height )
	{
		var center = CellWorld( plot.X, plot.Y );
		var top = center + Vector3.Up * (height + 18f);
		var facing = Buildings.Rotate( Vector2.Up, plot.Facing );

		switch ( plot.Kind )
		{
			case BuildingKind.Anvil:
				Blocks.SpawnBox( plot.Body, "Horn",
					center + new Vector3( facing.x, facing.y, 0f ) * 28f + Vector3.Up * (height * 0.7f),
					Blocks.FlatFacing( facing ),
					new Vector3( 50f, 28f, 22f ), tint * 0.8f );
				break;
			default:
				Blocks.SpawnSphere( plot.Body, "Gem", top, plot.Working ? 32f : 20f, tint * 1.4f );
				break;
		}
	}

	static float PlotThickness( CityPlot plot ) => plot.Working ? 22f : 14f;

	static float WallPenetration( Vector2 point, CityWall wall )
	{
		var to = point - wall.Center;
		var px = wall.Hx - MathF.Abs( ArenaGeometry.Dot( to, wall.AxisX ) );
		var py = wall.Hy - MathF.Abs( ArenaGeometry.Dot( to, wall.AxisY ) );
		return px > 0f && py > 0f ? MathF.Min( px, py ) : 0f;
	}

	static bool TryWall( Vector2[] poly, int index, float thick, float radius, out CityWall wall )
	{
		wall = default;
		var a = poly[index];
		var b = poly[(index + 1) % poly.Length];
		var span = b - a;
		var length = span.Length;
		if ( length < 1f )
			return false;

		var axisX = span / length;
		wall = new CityWall
		{
			Center = (a + b) * 0.5f,
			AxisX = axisX,
			AxisY = new Vector2( -axisX.y, axisX.x ),
			Hx = length * 0.5f + thick * 0.5f + radius,
			Hy = thick * 0.5f + radius
		};
		return true;
	}

	Vector2[] WorldPolygon( BuildingKind kind, int facing, int x, int y )
	{
		var center = CellWorld( x, y );
		var origin = new Vector2( center.x, center.y );
		var local = Buildings.Shape( kind );
		var scale = CellSize * 0.78f;
		var world = new Vector2[local.Length];

		for ( var i = 0; i < local.Length; i++ )
			world[i] = origin + Buildings.Rotate( local[i], facing ) * scale;

		return world;
	}

	List<Vector3> OutlinePoints( BuildingKind kind, int facing, int x, int y, float height )
	{
		var poly = WorldPolygon( kind, facing, x, y );
		var points = new List<Vector3>( poly.Length + 1 );
		foreach ( var p in poly )
			points.Add( new Vector3( p.x, p.y, height ) );
		points.Add( points[0] );
		return points;
	}

	bool HasSameNeighbor( CityPlot plot )
	{
		foreach ( var other in plots )
		{
			if ( other == plot || !other.Working || other.Kind != plot.Kind )
				continue;

			if ( Math.Abs( other.X - plot.X ) + Math.Abs( other.Y - plot.Y ) == 1 )
				return true;
		}

		return false;
	}
}

public enum CityMode
{
	Build,
	Shoot
}
