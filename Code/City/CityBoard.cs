namespace LoopedLoaded;

public sealed class CityBoard : Component
{
	[Property] public GameLoop Loop { get; set; }
	[Property] public int Size { get; set; } = 5;
	[Property] public float CellSize { get; set; } = 180f;
	[Property] public float PlayHeight { get; set; } = 40f;
	[Property] public Vector3 Anchor { get; set; } = new Vector3( 5000f, 0f, 0f );

	public int Warehouse { get; private set; }
	public BuildingKind Selected { get; private set; } = BuildingKind.Infirmary;
	public int PlacementFacing { get; private set; }
	public CityMode Mode { get; private set; } = CityMode.Build;
	public bool Building => Mode == CityMode.Build;
	public CityPlot Hovered { get; private set; }
	public Vector2 Cursor { get; private set; }
	public Vector3 Center => Anchor;
	public float Span => Size * CellSize;
	public Vector3 ShooterStand => Anchor + new Vector3( 0f, -Span * 0.5f - 260f, 0f );

	readonly List<CityPlot> plots = new();
	readonly List<GameObject> tiles = new();
	GameObject root;
	GameObject hover;
	GameObject shooter;
	GameObject cursor;
	PolyLine aim;
	PolyLine ghost;
	bool built;

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

			var plot = plots[row.Y * Size + row.X];
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

	public void EnsureBuilt()
	{
		if ( built )
			return;

		root = Scene.CreateObject();
		root.Name = "City";
		root.Parent = GameObject;

		Blocks.SpawnBox( root, "Ground", Anchor + new Vector3( 0f, -180f, -18f ), Rotation.Identity,
			new Vector3( Span * 1.45f, Span * 1.9f, 20f ), new Color( 0.07f, 0.09f, 0.12f ), false );

		for ( var y = 0; y < Size; y++ )
		{
			for ( var x = 0; x < Size; x++ )
			{
				var plot = new CityPlot { X = x, Y = y };
				plots.Add( plot );

				var pos = CellWorld( x, y );
				var shade = (x + y) % 2 == 0 ? 1f : 0.78f;
				var tile = Blocks.SpawnBox( root, $"Tile {x},{y}", pos + Vector3.Up * 2f, Rotation.Identity,
					new Vector3( CellSize * 0.92f, CellSize * 0.92f, 6f ),
					new Color( 0.12f, 0.15f, 0.2f ) * shade, false );
				tiles.Add( tile );

				plot.Root = Scene.CreateObject();
				plot.Root.Name = $"Plot {x},{y}";
				plot.Root.Parent = root;
				plot.Root.WorldPosition = pos;
			}
		}

		Blocks.SpawnBox( root, "Pad", ShooterStand + Vector3.Up * -8f, Rotation.Identity,
			new Vector3( 220f, 140f, 12f ), new Color( 0.18f, 0.22f, 0.28f ), false );

		shooter = Scene.CreateObject();
		shooter.Name = "City Shooter";
		shooter.Parent = root;
		shooter.WorldPosition = ShooterStand;

		Blocks.SpawnBox( shooter, "Torso", ShooterStand + Vector3.Up * 40f, Rotation.Identity, new Vector3( 46f, 46f, 80f ), new Color( 0.82f, 0.94f, 1f ) );
		Blocks.SpawnBox( shooter, "Barrel", ShooterStand + new Vector3( 54f, 0f, 46f ), Rotation.Identity, new Vector3( 76f, 16f, 16f ), new Color( 0.22f, 0.3f, 0.4f ) );

		hover = Blocks.SpawnBox( root, "Hover", CellWorld( 0, 0 ) + Vector3.Up * 8f, Rotation.Identity,
			new Vector3( CellSize * 0.96f, CellSize * 0.96f, 8f ), new Color( 1f, 0.85f, 0.35f ), false );

		cursor = Blocks.SpawnSphere( root, "Cursor", ShooterStand + Vector3.Up * PlayHeight, 26f, ShotColors.Player, false );

		var aimObject = Scene.CreateObject();
		aimObject.Name = "City Aim";
		aimObject.Parent = root;
		aim = aimObject.AddComponent<PolyLine>();
		aim.HeadTint = new Color( 1f, 0.9f, 0.4f );
		aim.TailTint = new Color( 1f, 0.55f, 0.18f );
		aim.HeadWidth = 3f;
		aim.TailWidth = 8f;
		aim.Apply();

		var ghostObject = Scene.CreateObject();
		ghostObject.Name = "Ghost";
		ghostObject.Parent = root;
		ghost = ghostObject.AddComponent<PolyLine>();
		ghost.HeadWidth = 6f;
		ghost.TailWidth = 6f;
		ghost.Apply();

		built = true;
		SetVisible( false );
	}

	public void SetVisible( bool visible )
	{
		EnsureBuilt();
		if ( !visible )
			ClearShots();
		if ( root.IsValid() )
			root.Enabled = visible;
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
			if ( Input.Pressed( "Attack1" ) )
				TryPlace();
			return;
		}

		if ( Input.Pressed( "Attack1" ) )
			TryFire();
	}

	void SetMode( CityMode mode )
	{
		if ( Mode == mode )
			return;

		Mode = mode;
		Sound.Play( "sounds/kenney/ui/ui.button.press.sound" );
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
		Sound.Play( "sounds/kenney/ui/ui.navigate.forward.sound" );
	}

	public void Miss( Vector2 at )
	{
		ImpactFlash.Spawn( Scene, new Vector3( at.x, at.y, PlayHeight ), new Color( 0.4f, 0.45f, 0.5f ), 0.6f );
		Sound.Play( "sounds/impacts/bullets/impact-bullet-dirt.sound", new Vector3( at.x, at.y, PlayHeight ) );
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
		Sound.Play( "sounds/impacts/melee/impact-melee-metal.sound", world );
		ImpactFlash.Spawn( Scene, world, Buildings.Color( plot.Kind ), 1.1f );

		if ( plot.Hits >= plot.NextCost )
		{
			plot.Hits = 0;
			plot.Level++;
			Sound.Play( "sounds/kenney/ui/ui.favourite.sound", world );
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

	public bool Trace( Vector2 origin, Vector2 direction, float maxDistance, out CityHit hit )
	{
		hit = default;
		var closest = maxDistance;
		var found = false;

		foreach ( var plot in plots )
		{
			if ( !plot.Occupied )
				continue;

			var poly = WorldPolygon( plot.Kind, plot.Facing, plot.X, plot.Y );
			for ( var i = 0; i < poly.Length; i++ )
			{
				var a = poly[i];
				var b = poly[(i + 1) % poly.Length];
				var span = b - a;
				var denominator = ArenaGeometry.Cross( direction, span );
				if ( MathF.Abs( denominator ) < 0.0000001f )
					continue;

				var offset = a - origin;
				var travel = ArenaGeometry.Cross( offset, span ) / denominator;
				var along = ArenaGeometry.Cross( offset, direction ) / denominator;

				if ( travel <= 0.02f || travel >= closest )
					continue;

				if ( along < 0f || along > 1f )
					continue;

				var normal = new Vector2( -span.y, span.x ).Normal;
				if ( ArenaGeometry.Dot( normal, direction ) > 0f )
					normal = -normal;

				closest = travel;
				found = true;
				hit = new CityHit
				{
					Distance = travel,
					Position = origin + direction * travel,
					Normal = normal,
					Plot = plot
				};
			}
		}

		return found;
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
		var origin = Anchor - new Vector3( Span * 0.5f, Span * 0.5f, 0f );
		return origin + new Vector3( (x + 0.5f) * CellSize, (y + 0.5f) * CellSize, 0f );
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
		var origin = Anchor - new Vector3( Span * 0.5f, Span * 0.5f, 0f );
		var local = flat - new Vector2( origin.x, origin.y );
		var x = (int)MathF.Floor( local.x / CellSize );
		var y = (int)MathF.Floor( local.y / CellSize );

		if ( x < 0 || y < 0 || x >= Size || y >= Size )
			return false;

		plot = plots[y * Size + x];
		return true;
	}

	void TryPlace()
	{
		if ( Hovered is null )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
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
		Sound.Play( "sounds/kenney/ui/ui.navigate.forward.sound" );
		Loop?.Announce( $"{Buildings.Title( Selected )} FRAME" );
		Loop?.Autosave();
	}

	void TryRemove()
	{
		if ( Hovered is null || !Hovered.Occupied || Hovered.Working )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			return;
		}

		var title = Buildings.Title( Hovered.Kind );
		Hovered.Occupied = false;
		Hovered.Level = 0;
		Hovered.Hits = 0;
		Hovered.Facing = 0;
		RefreshPlot( Hovered );
		Sound.Play( "sounds/kenney/ui/ui.navigate.deny.sound" );
		Loop?.Announce( $"{title} REMOVED" );
		Loop?.Autosave();
	}

	void TryFire()
	{
		if ( Warehouse <= 0 )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			Loop?.Announce( "WAREHOUSE EMPTY" );
			return;
		}

		Warehouse--;

		var origin = ShooterStand + Vector3.Up * PlayHeight;
		var to = Cursor - new Vector2( ShooterStand.x, ShooterStand.y );
		if ( to.Length < 8f )
		{
			Warehouse++;
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			return;
		}

		var go = Scene.CreateObject();
		go.Name = "City Round";
		if ( root.IsValid() )
			go.Parent = root;

		var shot = go.AddComponent<CityShot>();
		shot.Board = this;
		shot.Tint = ShotColors.Player;
		shot.Launch( origin, to );

		Sound.Play( "sounds/effects/explosion/explosion_small.sound", ShooterStand );
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
		if ( !ghost.IsValid() )
			return;

		if ( !Building || Hovered is null || Hovered.Occupied )
		{
			ghost.Clear();
			return;
		}

		var tint = Buildings.Color( Selected );
		ghost.HeadTint = tint;
		ghost.TailTint = tint;
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
		var thick = plot.Working ? 22f : 14f;
		var poly = WorldPolygon( plot.Kind, plot.Facing, plot.X, plot.Y );
		var center = CellWorld( plot.X, plot.Y );

		plot.Body = Scene.CreateObject();
		plot.Body.Name = "Building";
		plot.Body.Parent = plot.Root;
		plot.Body.WorldPosition = center;

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

	void SpawnAccent( CityPlot plot, Color tint, float height )
	{
		var center = CellWorld( plot.X, plot.Y );
		var top = center + Vector3.Up * (height + 18f);
		var facing = Buildings.Rotate( Vector2.Up, plot.Facing );

		switch ( plot.Kind )
		{
			case BuildingKind.Infirmary:
				Blocks.SpawnSphere( plot.Body, "Lamp", top, plot.Working ? 36f : 22f, tint * 1.3f );
				break;
			case BuildingKind.Anvil:
				Blocks.SpawnBox( plot.Body, "Horn",
					center + new Vector3( facing.x, facing.y, 0f ) * 28f + Vector3.Up * (height * 0.7f),
					Blocks.FlatFacing( facing ),
					new Vector3( 50f, 28f, 22f ), tint * 0.8f );
				break;
			case BuildingKind.Booster:
				Blocks.SpawnBox( plot.Body, "Nose",
					center + new Vector3( facing.x, facing.y, 0f ) * 40f + Vector3.Up * (height * 0.55f),
					Blocks.FlatFacing( facing ),
					new Vector3( 36f, 36f, 36f ), tint * 1.2f );
				break;
			case BuildingKind.Brake:
				Blocks.SpawnBox( plot.Body, "Gate",
					center - new Vector3( facing.x, facing.y, 0f ) * 18f + Vector3.Up * (height * 0.5f),
					Blocks.FlatFacing( facing ),
					new Vector3( 70f, 18f, height * 0.7f ), tint * 0.75f );
				break;
			default:
				Blocks.SpawnSphere( plot.Body, "Gem", top, plot.Working ? 32f : 20f, tint * 1.4f );
				break;
		}
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
