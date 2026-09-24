using Sandbox.UI;

namespace LoopedLoaded;

public sealed class HudFace : ScenePanel
{
	public GameLoop Loop { get; set; }

	Scene scene;
	CameraComponent camera;
	GameObject body;
	SkinnedModelRenderer portrait;
	bool ready;
	Vector3 headLocal;
	float shownYaw;
	float kick;
	float nod;
	float deadMix;
	float flinchSide = 1f;
	int seenHealth = -1;
	int seenMag = -1;

	public override void Tick()
	{
		base.Tick();

		if ( !Loop.IsValid() )
			return;

		Ensure();
		if ( !ready )
			return;

		var dt = MathF.Min( Time.Delta, 0.05f );
		Step( dt );
		Pose();
		scene.GameTick( dt );
	}

	public override void OnDeleted()
	{
		scene?.Destroy();
		scene = null;
		camera = null;
		body = null;
		portrait = null;
		ready = false;
		base.OnDeleted();
	}

	void Ensure()
	{
		if ( ready || scene is not null )
			return;

		var model = Model.Load( WarlordLook.ModelPath );
		if ( model is null )
			return;

		scene = new Scene();
		scene.Name = "Hud Face";
		scene.WantsSystemScene = false;

		var camObject = scene.CreateObject();
		camObject.Name = "Face Camera";
		camera = camObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = true;
		camera.BackgroundColor = new Color( 0.047f, 0.027f, 0.012f );
		camera.EnablePostProcessing = false;
		camera.FovAxis = CameraComponent.Axis.Horizontal;
		camera.FieldOfView = 24f;
		camera.ZNear = 0.05f;
		camera.ZFar = 4000f;

		body = scene.CreateObject();
		body.Name = "Warlord";
		portrait = body.AddComponent<SkinnedModelRenderer>();
		portrait.Model = model;
		portrait.UseAnimGraph = false;
		portrait.Sequence.Name = "rest";
		portrait.Sequence.Looping = true;

		RenderScene = scene;
		RenderOnce = false;
		scene.GameTick( 0.016 );
		Frame();
		ready = true;
	}

	void Frame()
	{
		var bounds = portrait.Model.Bounds;
		var height = MathF.Max( bounds.Size.z, 1f );
		var headOk = TryBone( "Head", out var head ) && head.z > bounds.Center.z;
		var neckOk = TryBone( "Neck", out var neck );
		var neckSpan = (head - neck).Length;

		Vector3 aim;
		float span;
		if ( headOk && neckOk && neckSpan > height * 0.01f )
		{
			aim = Vector3.Lerp( neck, head, 0.72f );
			span = MathF.Max( neckSpan * 3.6f, height * 0.2f );
		}
		else if ( headOk )
		{
			aim = head;
			span = height * 0.28f;
		}
		else
		{
			aim = bounds.Center + Vector3.Up * height * 0.34f;
			span = height * 0.32f;
		}

		headLocal = aim;

		var aspect = 241f / 202f;
		var halfH = MathX.DegreeToRadian( camera.FieldOfView ) * 0.5f;
		var vFov = 2f * MathF.Atan( MathF.Tan( halfH ) / aspect );
		var dist = MathF.Max( (span * 0.5f) / MathF.Tan( vFov * 0.5f ), 0.25f );
		var look = aim + Vector3.Up * span * 0.16f;
		var cam = look + Vector3.Forward * dist;

		camera.WorldPosition = cam;
		camera.WorldRotation = Rotation.LookAt( look - cam, Vector3.Up );
		camera.ZNear = MathF.Max( 0.01f, dist * 0.02f );
		camera.ZFar = dist * 40f;

		AddLight( "Key", aim + new Vector3( dist * 0.72f, -dist * 0.34f, dist * 0.26f ), dist * 14f, new Color( 3.2f, 2.7f, 2.2f ) );
		AddLight( "Fill", aim + new Vector3( dist * 0.35f, dist * 0.62f, dist * 0.08f ), dist * 16f, new Color( 0.7f, 0.78f, 0.95f ) );
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
		if ( !portrait.IsValid() )
			return false;

		if ( !portrait.TryGetBoneTransform( name, out var tx ) )
			return false;

		position = tx.Position;
		return true;
	}

	void Step( float dt )
	{
		var dead = Loop.Health <= 0 || Loop.Phase == RunPhase.Dead;
		portrait.PlaybackRate = dead ? 0.2f : 1f;

		var hp = Loop.Health;
		if ( seenHealth >= 0 && hp < seenHealth )
		{
			shownYaw = Loop.PainYaw;
			kick = 1f;
			flinchSide = MathF.Sign( Loop.PainYaw );
			if ( flinchSide == 0f )
				flinchSide = Game.Random.Float( 0f, 1f ) < 0.5f ? -1f : 1f;
		}
		else
		{
			shownYaw = Damp( shownYaw, 0f, 1.5f, dt );
		}

		seenHealth = hp;
		kick = MathF.Max( 0f, kick - dt * 2.8f );

		var mag = Loop.Inventory.IsValid() ? Loop.Inventory.MagLoaded : seenMag;
		if ( seenMag >= 0 && mag < seenMag )
			nod = 1f;

		seenMag = mag;
		nod = MathF.Max( 0f, nod - dt * 5.5f );
		deadMix = Damp( deadMix, dead ? 1f : 0f, 5f, dt );
	}

	void Pose()
	{
		var max = Math.Max( Loop.HeartMax, 1 );
		var frac = Math.Clamp( Loop.Health / (float)max, 0f, 1f );
		var missing = 1f - frac;
		var idleYaw = MathF.Sin( Time.Now * 0.55f ) * (2.5f + missing * 3f);
		var idlePitch = MathF.Sin( Time.Now * 0.9f ) * 1.2f;
		var tremble = MathF.Sin( Time.Now * (9f + missing * 14f) ) * missing * 5.5f;

		var yaw = (shownYaw + idleYaw) * (1f - deadMix);
		var pitch = MathX.Lerp( idlePitch + missing * 18f - kick * 24f + nod * 8f, 58f, deadMix );
		var roll = MathX.Lerp( tremble - flinchSide * kick * 10f, flinchSide * 34f, deadMix );
		var rot = Rotation.FromYaw( yaw ) * Rotation.FromPitch( pitch ) * Rotation.FromRoll( roll );

		body.WorldRotation = rot;
		body.WorldPosition = headLocal - rot * headLocal;

		var flash = MathF.Max( Loop.HurtAmount, missing * 0.72f );
		var tint = Color.Lerp( Color.White, new Color( 1f, 0.2f, 0.14f ), Math.Clamp( flash, 0f, 1f ) );
		portrait.Tint = Color.Lerp( tint, new Color( 0.42f, 0.09f, 0.07f ), deadMix );
	}

	static float Damp( float current, float target, float speed, float dt )
	{
		return MathX.Lerp( current, target, 1f - MathF.Exp( -speed * dt ) );
	}
}
