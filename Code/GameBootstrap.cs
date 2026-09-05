namespace LoopedLoaded;

public sealed class GameBootstrap : Component
{
	[Property] public bool BuildLighting { get; set; } = true;

	static readonly Color PlayerTint = new Color( 0.82f, 0.94f, 1f );
	static readonly Color WeaponTint = new Color( 0.22f, 0.30f, 0.40f );

	protected override void OnStart()
	{
		if ( BuildLighting )
			EnsureLighting();

		var camera = EnsureCamera();
		var arena = BuildArena();
		var player = BuildPlayer( arena );

		var loop = GameObject.AddComponent<GameLoop>();
		loop.Arena = arena;
		loop.Runner = player.GetComponent<RingRunner>();
		loop.Aim = player.GetComponent<PlayerAim>();
		loop.Inventory = player.GetComponent<RoundInventory>();

		var city = GameObject.AddComponent<CityBoard>();
		city.Loop = loop;
		loop.City = city;

		loop.Runner.Loop = loop;
		loop.Aim.Loop = loop;
		loop.Aim.Inventory = loop.Inventory;
		loop.Inventory.Loop = loop;

		var rig = camera.AddComponent<ArenaCamera>();
		rig.Arena = arena;
		rig.Runner = loop.Runner;
		rig.Loop = loop;
		rig.City = city;

		BuildHud( loop );
		loop.Restart();
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

	GameObject EnsureCamera()
	{
		var cameras = Scene.GetAllComponents<CameraComponent>().ToList();
		var main = cameras.FirstOrDefault( c => c.IsMainCamera ) ?? cameras.FirstOrDefault();

		if ( !main.IsValid() )
		{
			var go = Scene.CreateObject();
			go.Name = "Arena Camera";

			main = go.AddComponent<CameraComponent>();
			main.ClearFlags = ClearFlags.All;

			go.AddComponent<Bloom>();
			go.AddComponent<Tonemapping>();
		}

		main.IsMainCamera = true;

		foreach ( var extra in cameras )
		{
			if ( extra == main )
				continue;

			extra.IsMainCamera = false;
			extra.Enabled = false;
		}

		return main.GameObject;
	}

	ArenaBuilder BuildArena()
	{
		var go = Scene.CreateObject();
		go.Name = "Arena";

		var arena = go.AddComponent<ArenaBuilder>();
		arena.EnsureVisuals();

		return arena;
	}

	GameObject BuildPlayer( ArenaBuilder arena )
	{
		var go = Scene.CreateObject();
		go.Name = "Player";

		Blocks.SpawnBox( go, "Torso", new Vector3( 0f, 0f, 40f ), Rotation.Identity, new Vector3( 46f, 46f, 80f ), PlayerTint );
		Blocks.SpawnBox( go, "Barrel", new Vector3( 54f, 0f, 46f ), Rotation.Identity, new Vector3( 76f, 16f, 16f ), WeaponTint );
		Blocks.SpawnBox( go, "Shoulder", new Vector3( 8f, 0f, 46f ), Rotation.Identity, new Vector3( 34f, 40f, 26f ), WeaponTint );

		var runner = go.AddComponent<RingRunner>();
		runner.Arena = arena;

		var aim = go.AddComponent<PlayerAim>();
		aim.Arena = arena;
		aim.Runner = runner;

		var inventory = go.AddComponent<RoundInventory>();
		inventory.Arena = arena;
		inventory.Runner = runner;
		inventory.Aim = aim;

		return go;
	}

	void BuildHud( GameLoop loop )
	{
		var go = Scene.CreateObject();
		go.Name = "HUD";

		go.AddComponent<ScreenPanel>();

		var hud = go.AddComponent<ArenaHud>();
		hud.Loop = loop;
	}
}
