using Sandbox.UI;

namespace LoopedLoaded;

public sealed class EkkePortrait : ScenePanel
{
	public GameLoop Loop { get; set; }

	Scene scene;
	CameraComponent camera;
	GameObject rig;
	SkinnedModelRenderer portrait;
	bool ready;

	public override void Tick()
	{
		base.Tick();

		if ( !Loop.IsValid() )
			return;

		Ensure();
		if ( !ready )
			return;

		RenderScene = scene;
		var dt = MathF.Min( Time.Delta, 0.05f );
		scene.GameTick( dt );
		if ( !Loop.TalkReady )
			EkkeLook.Pulse();
		EkkeLook.Drive( portrait );
	}

	public override void OnDeleted()
	{
		EkkeLook.Release( portrait );
		scene?.Destroy();
		scene = null;
		camera = null;
		rig = null;
		portrait = null;
		ready = false;
		base.OnDeleted();
	}

	void Ensure()
	{
		if ( ready || scene is not null )
			return;

		var model = Model.Load( EkkeLook.ModelPath );
		if ( model is null || !model.IsValid() )
			return;

		scene = new Scene();
		scene.Name = "Ekke Portrait";
		scene.WantsSystemScene = false;

		var camObject = scene.CreateObject();
		camObject.Name = "Ekke Camera";
		camera = camObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = true;
		camera.BackgroundColor = new Color( 0.082f, 0.071f, 0.067f );
		camera.EnablePostProcessing = false;
		camera.FovAxis = CameraComponent.Axis.Horizontal;
		camera.FieldOfView = 28f;
		camera.ZNear = 0.05f;
		camera.ZFar = 80f;

		rig = scene.CreateObject();
		rig.Name = "Ekke";

		var body = scene.CreateObject();
		body.Name = "Ekke Model";
		body.Parent = rig;
		portrait = body.AddComponent<SkinnedModelRenderer>();
		portrait.Model = model;
		portrait.UseAnimGraph = false;
		portrait.CreateBoneObjects = false;
		EkkeLook.Bind( portrait );

		RenderScene = scene;
		RenderOnce = false;
		scene.GameTick( 0.016f );
		Frame();
		ready = true;
	}

	void Frame()
	{
		var bounds = portrait.Model.Bounds;
		var height = MathF.Max( bounds.Size.z, 1f );
		var aim = bounds.Center + Vector3.Up * height * 0.22f;
		if ( TryBone( "Head", out var head ) )
			aim = head;

		var face = Vector3.Backward;
		if ( TryBone( "Face_Front", out var plate ) && TryBone( "Head", out var neck ) )
		{
			var outward = plate - neck;
			outward.z = 0f;
			if ( outward.Length > 0.02f )
				face = outward.Normal;
		}

		var gaze = Rotation.FromYaw( -22f );
		rig.WorldRotation = gaze;
		rig.WorldPosition = aim - gaze * aim;

		var span = height * 0.62f;
		var half = MathX.DegreeToRadian( camera.FieldOfView ) * 0.5f;
		var dist = MathF.Max( (span * 0.5f) / MathF.Tan( half ), 0.4f );
		var look = aim + Vector3.Up * span * 0.04f;
		camera.WorldPosition = look + face * dist;
		camera.WorldRotation = Rotation.LookAt( look - camera.WorldPosition, Vector3.Up );
		camera.ZNear = MathF.Max( 0.02f, dist * 0.02f );
		camera.ZFar = dist * 12f;

		AddLight( "Key", look + face * dist * 0.4f + Vector3.Right * dist * 0.35f + Vector3.Up * dist * 0.2f, dist * 8f, new Color( 2.4f, 2.2f, 2f ) );
		AddLight( "Fill", look - face * dist * 0.2f + Vector3.Left * dist * 0.4f, dist * 10f, new Color( 0.45f, 0.5f, 0.62f ) );
	}

	void AddLight( string name, Vector3 position, float radius, Color color )
	{
		var go = scene.CreateObject();
		go.Name = name;
		go.WorldPosition = position;
		var light = go.AddComponent<PointLight>();
		light.LightColor = color;
		light.Radius = radius;
		light.Shadows = false;
	}

	bool TryBone( string name, out Vector3 position )
	{
		position = default;
		if ( !portrait.IsValid() || !portrait.TryGetBoneTransform( name, out var tx ) )
			return false;

		position = tx.Position;
		return true;
	}
}
