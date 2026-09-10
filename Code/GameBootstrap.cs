namespace LoopedLoaded;

public sealed class GameBootstrap : Component
{
	[Property] public GameLoop Loop { get; set; }
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public CityBoard City { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	protected override void OnStart()
	{
		EnsureLighting();
		var loop = ResolveLoop();
		if ( !loop.IsValid() )
			return;

		Wire( loop );
		EnsureHud( loop );
		EnsureCamera( loop );
		ArenaSounds.Warm();
		loop.RestoreSaves();
		loop.ShowMenu();
	}

	GameLoop ResolveLoop()
	{
		if ( Loop.IsValid() )
			return Loop;

		Loop = GetComponent<GameLoop>() ?? Scene.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( Loop.IsValid() )
			return Loop;

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
			var player = Scene.CreateObject();
			player.Name = "Player";
			loop.Runner = player.AddComponent<RingRunner>();
			player.AddComponent<PlayerAim>();
			player.AddComponent<RoundInventory>();
		}

		if ( !loop.Aim.IsValid() )
			loop.Aim = loop.Runner.GetComponent<PlayerAim>();

		if ( !loop.Inventory.IsValid() )
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

		var go = Scene.CreateObject();
		go.Name = "HUD";
		go.AddComponent<ScreenPanel>();
		hud = go.AddComponent<ArenaHud>();
		hud.Loop = loop;
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

		var bloom = go.GetComponent<Bloom>() ?? go.AddComponent<Bloom>();
		bloom.Strength = 1.22f;
		bloom.Threshold = 0.78f;
		bloom.Tint = new Color( 1f, 0.88f, 0.72f );

		var tone = go.GetComponent<Tonemapping>() ?? go.AddComponent<Tonemapping>();
		tone.Mode = Tonemapping.TonemappingMode.ACES;
		tone.AutoExposureEnabled = true;
		tone.ExposureCompensation = 0.22f;
		tone.MinimumExposure = 0.9f;
		tone.MaximumExposure = 1.7f;
		tone.Rate = 1.4f;

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
			light.LightColor = new Color( 0.55f, 0.62f, 0.78f );
			light.SkyColor = new Color( 0.04f, 0.05f, 0.08f );
			light.Shadows = true;
		}

		if ( Scene.GetAllComponents<AmbientLight>().Any() )
			return;

		var ambientObject = Scene.CreateObject();
		ambientObject.Name = "Ambient";
		var ambient = ambientObject.AddComponent<AmbientLight>();
		ambient.Color = new Color( 0.06f, 0.08f, 0.12f );
	}
}
