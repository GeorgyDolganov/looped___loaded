namespace LoopedLoaded;

public sealed class GameBootstrap : Component
{
	[Property] public GameLoop Loop { get; set; }
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public CityBoard City { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	int trimPasses;

	protected override void OnStart()
	{
		TrimDuplicates();
		EnsureLighting();
		var loop = ResolveLoop();
		if ( !loop.IsValid() )
			return;

		Wire( loop );
		EnsureHud( loop );
		EnsureCamera( loop );
		TrimDuplicates();
		ArenaSounds.Warm();
		loop.RestoreSaves();
		loop.ShowMenu();
		UserSettings.Load();
		GraphicsApply.Push( Scene );
	}

	protected override void OnUpdate()
	{
		if ( trimPasses >= 8 )
			return;

		trimPasses++;
		TrimDuplicates();
	}

	GameLoop ResolveLoop()
	{
		if ( Loop.IsValid() )
			return Loop;

		Loop = GetComponent<GameLoop>() ?? Scene.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( Loop.IsValid() )
			return Loop;

		if ( HasComponentNamed( GameObject, "GameLoop" ) )
			return null;

		Loop = GameObject.AddComponent<GameLoop>();
		return Loop;
	}

	void Wire( GameLoop loop )
	{
		if ( !loop.Arena.IsValid() )
			loop.Arena = Arena.IsValid() ? Arena : Scene.GetAllComponents<ArenaBuilder>().FirstOrDefault();

		if ( !loop.City.IsValid() )
			loop.City = City.IsValid() ? City : Scene.GetAllComponents<CityBoard>().FirstOrDefault();

		if ( !loop.Runner.IsValid() )
			loop.Runner = Runner.IsValid() ? Runner : Scene.GetAllComponents<RingRunner>().FirstOrDefault();

		if ( !loop.Runner.IsValid() )
		{
			var player = FindNamed( "Player" );
			if ( player.IsValid() )
				loop.Runner = player.GetComponent<RingRunner>();

			if ( !loop.Runner.IsValid() && !HasComponentNamed( player, "RingRunner" ) )
			{
				if ( !player.IsValid() )
				{
					player = Scene.CreateObject();
					player.Name = "Player";
				}

				loop.Runner = player.AddComponent<RingRunner>();
				if ( !HasComponentNamed( player, "PlayerAim" ) )
					player.AddComponent<PlayerAim>();
				if ( !HasComponentNamed( player, "RoundInventory" ) )
					player.AddComponent<RoundInventory>();
			}
		}

		if ( !loop.Aim.IsValid() && loop.Runner.IsValid() )
			loop.Aim = loop.Runner.GetComponent<PlayerAim>();

		if ( !loop.Inventory.IsValid() && loop.Runner.IsValid() )
			loop.Inventory = loop.Runner.GetComponent<RoundInventory>();

		if ( loop.Runner.IsValid() )
		{
			if ( !loop.Runner.Arena.IsValid() )
				loop.Runner.Arena = loop.Arena;
			if ( !loop.Runner.Loop.IsValid() )
				loop.Runner.Loop = loop;
		}

		if ( loop.Aim.IsValid() )
		{
			if ( !loop.Aim.Arena.IsValid() )
				loop.Aim.Arena = loop.Arena;
			if ( !loop.Aim.Runner.IsValid() )
				loop.Aim.Runner = loop.Runner;
			if ( !loop.Aim.Inventory.IsValid() )
				loop.Aim.Inventory = loop.Inventory;
			if ( !loop.Aim.Loop.IsValid() )
				loop.Aim.Loop = loop;
		}

		if ( loop.Inventory.IsValid() )
		{
			if ( !loop.Inventory.Arena.IsValid() )
				loop.Inventory.Arena = loop.Arena;
			if ( !loop.Inventory.Runner.IsValid() )
				loop.Inventory.Runner = loop.Runner;
			if ( !loop.Inventory.Aim.IsValid() )
				loop.Inventory.Aim = loop.Aim;
			if ( !loop.Inventory.Loop.IsValid() )
				loop.Inventory.Loop = loop;
		}

		if ( loop.City.IsValid() && !loop.City.Loop.IsValid() )
			loop.City.Loop = loop;
	}

	void EnsureHud( GameLoop loop )
	{
		var hud = Scene.GetAllComponents<ArenaHud>().FirstOrDefault();
		if ( hud.IsValid() )
		{
			if ( !hud.Loop.IsValid() )
				hud.Loop = loop;
			return;
		}

		var go = FindNamed( "HUD" );
		if ( go.IsValid() && HasComponentNamed( go, "ArenaHud" ) )
			return;

		if ( !go.IsValid() )
		{
			go = Scene.CreateObject();
			go.Name = "HUD";
		}

		if ( !go.GetComponent<ScreenPanel>().IsValid() )
			go.AddComponent<ScreenPanel>();

		hud = go.AddComponent<ArenaHud>();
		hud.Loop = loop;
	}

	void TrimDuplicates()
	{
		KeepNamed( "Player" );
		KeepNamed( "HUD" );

		var player = FindNamed( "Player" );
		if ( player.IsValid() )
		{
			KeepComponent( player, "RingRunner" );
			KeepChild( player, "Warlord" );
		}

		var hud = FindNamed( "HUD" );
		if ( hud.IsValid() )
			KeepComponent( hud, "ArenaHud" );
	}

	void KeepNamed( string name )
	{
		GameObject keep = null;
		var drop = new List<GameObject>();
		foreach ( var go in Scene.GetAllObjects( false ) )
		{
			if ( !go.IsValid() || go.Name != name )
				continue;

			if ( !keep.IsValid() )
			{
				keep = go;
				continue;
			}

			drop.Add( go );
		}

		foreach ( var go in drop )
		{
			if ( go.IsValid() )
				go.Destroy();
		}
	}

	void KeepComponent( GameObject go, string typeName )
	{
		Component keep = null;
		var drop = new List<Component>();
		foreach ( var component in go.Components.GetAll<Component>( FindMode.EnabledInSelfAndDescendants ) )
		{
			if ( !component.IsValid() || component.GameObject != go || component.GetType().Name != typeName )
				continue;

			if ( !keep.IsValid() )
			{
				keep = component;
				continue;
			}

			drop.Add( component );
		}

		foreach ( var component in drop )
		{
			if ( component.IsValid() )
				component.Destroy();
		}
	}

	void KeepChild( GameObject parent, string name )
	{
		GameObject keep = null;
		var drop = new List<GameObject>();
		foreach ( var child in parent.Children )
		{
			if ( !child.IsValid() || child.Name != name )
				continue;

			if ( !keep.IsValid() )
			{
				keep = child;
				continue;
			}

			drop.Add( child );
		}

		foreach ( var child in drop )
		{
			if ( child.IsValid() )
				child.Destroy();
		}
	}

	GameObject FindNamed( string name )
	{
		foreach ( var go in Scene.GetAllObjects( false ) )
		{
			if ( go.IsValid() && go.Name == name )
				return go;
		}

		return null;
	}

	bool HasComponentNamed( GameObject go, string typeName )
	{
		if ( !go.IsValid() )
			return false;

		foreach ( var component in go.Components.GetAll<Component>( FindMode.EnabledInSelfAndDescendants ) )
		{
			if ( component.IsValid() && component.GameObject == go && component.GetType().Name == typeName )
				return true;
		}

		return false;
	}

	void EnsureCamera( GameLoop loop )
	{
		var cameras = Scene.GetAllComponents<CameraComponent>().ToList();
		var main = Camera.IsValid() ? Camera : cameras.FirstOrDefault( c => c.IsMainCamera ) ?? cameras.FirstOrDefault();

		if ( !main.IsValid() )
		{
			var go = Scene.CreateObject();
			go.Name = "Arena Camera";
			main = go.AddComponent<CameraComponent>();
			main.ClearFlags = ClearFlags.All;
		}

		main.IsMainCamera = true;
		main.EnablePostProcessing = true;
		foreach ( var extra in cameras )
		{
			if ( extra == main )
				continue;

			extra.IsMainCamera = false;
			extra.Enabled = false;
		}

		EnsurePostProcess( main, loop );

		var rig = main.GetComponent<ArenaCamera>() ?? main.AddComponent<ArenaCamera>();
		if ( !rig.Loop.IsValid() )
			rig.Loop = loop;
		if ( !rig.Arena.IsValid() )
			rig.Arena = loop.Arena;
		if ( !rig.Runner.IsValid() )
			rig.Runner = loop.Runner;
		if ( !rig.City.IsValid() )
			rig.City = loop.City;
	}

	void EnsurePostProcess( CameraComponent camera, GameLoop loop )
	{
		var go = camera.GameObject;

		if ( !go.GetComponent<Bloom>().IsValid() )
		{
			var bloom = go.AddComponent<Bloom>();
			bloom.Strength = 0.55f;
			bloom.Threshold = 1.05f;
			bloom.Tint = new Color( 1f, 0.96f, 0.9f );
		}

		if ( !go.GetComponent<Tonemapping>().IsValid() )
		{
			var tone = go.AddComponent<Tonemapping>();
			tone.Mode = Tonemapping.TonemappingMode.AgX;
			tone.AutoExposureEnabled = true;
			tone.ExposureCompensation = 0.06f;
			tone.MinimumExposure = 1f;
			tone.MaximumExposure = 1.35f;
			tone.Rate = 1.2f;
		}

		var look = go.GetComponent<QuakeArenaLook>() ?? go.AddComponent<QuakeArenaLook>();
		if ( !look.Loop.IsValid() )
			look.Loop = loop;
	}

	void EnsureLighting()
	{
		if ( !Scene.GetAllComponents<DirectionalLight>().Any() )
		{
			var sun = Scene.CreateObject();
			sun.Name = "Sun";
			sun.WorldRotation = Rotation.From( 72f, 55f, 0f );

			var light = sun.AddComponent<DirectionalLight>();
			light.LightColor = new Color( 0.78f, 0.76f, 0.72f );
			light.SkyColor = new Color( 0.06f, 0.06f, 0.07f );
			light.Shadows = true;
		}

		if ( Scene.GetAllComponents<AmbientLight>().Any() )
			return;

		var ambientObject = Scene.CreateObject();
		ambientObject.Name = "Ambient";
		var ambient = ambientObject.AddComponent<AmbientLight>();
		ambient.Color = new Color( 0.08f, 0.08f, 0.085f );
	}
}
