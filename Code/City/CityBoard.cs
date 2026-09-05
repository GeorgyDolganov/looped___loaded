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
	bool built;

	public void Deposit( int rounds )
	{
		Warehouse += Math.Max( 0, rounds );
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
				case BuildingKind.Infirmary: health += power; break;
				case BuildingKind.Anvil: damage += power; break;
				case BuildingKind.Booster: boost += power; break;
				case BuildingKind.Brake: brake += power; break;
				case BuildingKind.Showcase: show += power; break;
			}
		}

		return new CityStats
		{
			BonusHealth = health,
			BonusDamage = damage,
			DashCooldownScale = MathF.Max( 0.52f, 1f - boost * 0.18f ),
			DashCharges = 1,
			SlowUnlocked = brake > 0,
			SlowDrain = brake >= 2 ? 0.38f : 0.55f,
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

		cursor = Blocks.SpawnSphere( root, "Cursor", ShooterStand + Vector3.Up * PlayHeight, 26f, new Color( 1f, 0.85f, 0.35f ), false );

		var aimObject = Scene.CreateObject();
		aimObject.Name = "City Aim";
		aimObject.Parent = root;
		aim = aimObject.AddComponent<PolyLine>();
		aim.HeadTint = new Color( 1f, 0.9f, 0.4f );
		aim.TailTint = new Color( 1f, 0.55f, 0.18f );
		aim.HeadWidth = 3f;
		aim.TailWidth = 8f;
		aim.Apply();

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

		for ( var i = 0; i < Buildings.All.Length; i++ )
		{
			if ( Input.Pressed( $"Slot{i + 1}" ) )
				Selected = Buildings.All[i];
		}

		if ( Input.Pressed( "Use" ) )
			TryPlace();

		if ( Input.Pressed( "Attack1" ) )
			TryFire();
	}

	public void ResolveShot( Vector2 impact )
	{
		if ( !TryPlot( impact, out var plot ) )
		{
			ImpactFlash.Spawn( Scene, new Vector3( impact.x, impact.y, PlayHeight ), new Color( 0.4f, 0.45f, 0.5f ), 0.6f );
			Sound.Play( "sounds/impacts/bullets/impact-bullet-dirt.sound", new Vector3( impact.x, impact.y, PlayHeight ) );
			return;
		}

		if ( !plot.Occupied || plot.Maxed )
		{
			ImpactFlash.Spawn( Scene, CellWorld( plot.X, plot.Y ) + Vector3.Up * PlayHeight, new Color( 0.5f, 0.55f, 0.6f ), 0.7f );
			Sound.Play( "sounds/impacts/bullets/impact-bullet-concrete.sound", CellWorld( plot.X, plot.Y ) );
			return;
		}

		plot.Hits++;
		var world = CellWorld( plot.X, plot.Y ) + Vector3.Up * 70f;
		Sound.Play( "sounds/impacts/melee/impact-melee-metal.sound", world );
		ImpactFlash.Spawn( Scene, world, Buildings.Color( plot.Kind ), 1.1f );

		if ( plot.Hits >= plot.NextCost )
		{
			plot.Hits = 0;
			plot.Level++;
			Sound.Play( "sounds/kenney/ui/ui.favourite.sound", world );
			Loop?.Announce( $"{Buildings.Title( plot.Kind )} LV{plot.Level}" );
		}

		RefreshPlot( plot );
	}

	public string HoverText()
	{
		if ( Hovered is null )
			return Buildings.Title( Selected );

		if ( !Hovered.Occupied )
			return $"{Buildings.Title( Selected )}  ·  E PLACE";

		if ( Hovered.Maxed )
			return $"{Buildings.Title( Hovered.Kind )} LV{Hovered.Level}  ·  MAX";

		var left = Hovered.NextCost - Hovered.Hits;
		var stage = Hovered.Working ? "UPGRADE" : "FRAME";
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
		if ( Hovered is null || Hovered.Occupied )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			return;
		}

		Hovered.Occupied = true;
		Hovered.Kind = Selected;
		Hovered.Level = 0;
		Hovered.Hits = 0;
		RefreshPlot( Hovered );
		Sound.Play( "sounds/kenney/ui/ui.navigate.forward.sound" );
		Loop?.Announce( $"{Buildings.Title( Selected )} FRAME" );
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

		var go = Scene.CreateObject();
		go.Name = "City Round";
		if ( root.IsValid() )
			go.Parent = root;

		var shot = go.AddComponent<CityShot>();
		shot.Board = this;
		shot.Target = Cursor;
		shot.Tint = new Color( 1f, 0.72f, 0.22f );
		shot.Launch( ShooterStand + Vector3.Up * PlayHeight );

		Sound.Play( "sounds/effects/explosion/explosion_small.sound", ShooterStand );
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
			cursor.WorldPosition = target;

		if ( !aim.IsValid() )
			return;

		var tint = Buildings.Color( Selected );
		aim.HeadTint = tint;
		aim.TailTint = Color.Lerp( tint, Color.White, 0.35f );
		aim.Apply();
		aim.SetPoints( new List<Vector3> { muzzle, target } );
	}

	void PaintHover()
	{
		if ( !hover.IsValid() )
			return;

		if ( Hovered is null )
		{
			hover.Enabled = false;
			return;
		}

		hover.Enabled = true;
		hover.WorldPosition = CellWorld( Hovered.X, Hovered.Y ) + Vector3.Up * 8f;

		var renderer = hover.GetComponent<ModelRenderer>();
		if ( renderer.IsValid() )
			renderer.Tint = Hovered.Occupied ? Buildings.Color( Hovered.Kind ) : Buildings.Color( Selected );
	}

	void RefreshPlot( CityPlot plot )
	{
		plot.Body?.Destroy();

		if ( !plot.Occupied )
			return;

		var tint = Buildings.Color( plot.Kind );
		if ( !plot.Working )
			tint *= 0.45f;

		var height = plot.Working ? 70f + plot.Level * 28f : 36f;
		var width = plot.Working ? CellSize * 0.55f : CellSize * 0.42f;
		var pos = CellWorld( plot.X, plot.Y ) + Vector3.Up * (height * 0.5f + 8f);

		plot.Body = Blocks.SpawnBox( plot.Root, "Building", pos, Rotation.Identity, new Vector3( width, width, height ), tint );

		if ( plot.NextCost > 0 )
		{
			var fill = plot.NextCost <= 0 ? 1f : (float)plot.Hits / plot.NextCost;
			var barHeight = 10f + fill * 40f;
			Blocks.SpawnBox( plot.Body, "Progress", pos + new Vector3( width * 0.55f, 0f, -height * 0.2f ), Rotation.Identity,
				new Vector3( 12f, 12f, barHeight ), Color.Lerp( new Color( 0.3f, 0.35f, 0.4f ), tint, fill ), false );
		}
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
