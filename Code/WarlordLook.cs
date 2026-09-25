namespace LoopedLoaded;

public static class WarlordLook
{
	public const string ModelPath = "models/warlord.vmdl";
	const string OverlayName = "Warlord Overlay";

	static readonly string[] UpperBones =
	{
		"Spine1", "Spine2", "Neck", "Head",
		"Shoulder_L", "UpperArm_L", "LowerArm_L", "Hand_L",
		"Shoulder_R", "UpperArm_R", "LowerArm_R", "Hand_R",
		"Gun"
	};

	static float shootUntil = -1f;

	public static SkinnedModelRenderer Attach( GameObject parent )
	{
		var go = parent.Scene.CreateObject();
		go.Name = "Warlord";
		go.Parent = parent;

		var model = Model.Load( ModelPath );
		var bounds = model.Bounds;
		var size = bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var scale = longest > 0.001f ? TerryLook.Height( false ) / longest : 1f;

		go.LocalRotation = Rotation.Identity;
		go.LocalScale = Vector3.One * scale;
		go.LocalPosition = Vector3.Up * ( -bounds.Mins.z * scale );

		return Make( go, model, "rest", true );
	}

	static SkinnedModelRenderer Make( GameObject go, Model model, string sequence, bool looping )
	{
		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;
		renderer.UseAnimGraph = false;
		renderer.CreateBoneObjects = false;
		renderer.Sequence.Name = sequence;
		renderer.Sequence.Looping = looping;
		return renderer;
	}

	static void Hide( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() || !skin.SceneObject.IsValid() )
			return;

		skin.SceneObject.RenderingEnabled = false;
	}

	static SkinnedModelRenderer FindOverlay( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() || !skin.GameObject.IsValid() || !skin.GameObject.Parent.IsValid() )
			return null;

		foreach ( var child in skin.GameObject.Parent.Children )
		{
			if ( child.Name != OverlayName )
				continue;

			return child.GetComponent<SkinnedModelRenderer>();
		}

		return null;
	}

	static SkinnedModelRenderer OverlayOf( SkinnedModelRenderer skin )
	{
		var overlay = FindOverlay( skin );
		if ( overlay.IsValid() )
			return overlay;

		if ( !skin.IsValid() || !skin.GameObject.IsValid() || !skin.GameObject.Parent.IsValid() )
			return null;

		var go = skin.GameObject.Parent.Scene.CreateObject();
		go.Name = OverlayName;
		go.Parent = skin.GameObject.Parent;
		go.WorldTransform = skin.WorldTransform;
		overlay = Make( go, skin.Model, "rest", false );
		Hide( overlay );
		return overlay;
	}

	static void CopyUpper( SkinnedModelRenderer body, SkinnedModelRenderer overlay )
	{
		if ( !body.IsValid() || !overlay.IsValid() )
			return;

		Hide( overlay );
		overlay.WorldTransform = body.WorldTransform;

		foreach ( var name in UpperBones )
		{
			var bone = overlay.Model?.Bones.GetBone( name );
			if ( bone is null )
				continue;

			if ( !overlay.TryGetBoneTransformAnimation( bone, out var world ) )
				continue;

			body.SetBoneTransform( bone, body.WorldTransform.ToLocal( world ) );
		}
	}

	const float SideEnter = 60f;
	const float SideExit = 45f;
	const float BackEnter = 130f;
	const float BackExit = 115f;

	public static void Drive( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look )
	{
		if ( !skin.IsValid() )
			return;

		skin.UseAnimGraph = false;
		var name = Locomotion( skin.Sequence.Name, velocity, look );
		if ( skin.Sequence.Name != name )
		{
			skin.Sequence.Name = name;
			skin.Sequence.Looping = true;
		}

		var overlay = FindOverlay( skin );
		if ( overlay.IsValid() )
			Hide( overlay );

		Face( skin, velocity, look );
		ApplyShoot( skin );
	}

	public static void PlayShoot( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() )
			return;

		var overlay = OverlayOf( skin );
		if ( !overlay.IsValid() )
			return;

		overlay.UseAnimGraph = false;
		overlay.Sequence.Name = "shoot";
		overlay.Sequence.Looping = false;
		overlay.Sequence.Time = 0f;
		Hide( overlay );
		var duration = overlay.Sequence.Duration;
		shootUntil = Time.Now + (duration > 0.05f ? duration : 13f / 25f);
	}

	public static void Face( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look )
	{
		if ( !skin.IsValid() )
			return;

		var face = look.Length > 0.01f ? look : velocity;
		face.z = 0f;
		if ( face.Length > 0.01f )
			skin.WorldRotation = Rotation.LookAt( face.Normal, Vector3.Up );

		if ( velocity.Length > 40f )
			Aim( skin, face, look );
	}

	public static void ApplyShoot( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() )
			return;

		var overlay = FindOverlay( skin );
		if ( overlay.IsValid() )
			Hide( overlay );

		if ( shootUntil < 0f || Time.Now >= shootUntil )
			return;

		CopyUpper( skin, overlay );
	}

	static string Locomotion( string current, Vector3 velocity, Vector3 look )
	{
		if ( velocity.Length <= 40f )
			return "rest";

		look.z = 0f;
		velocity.z = 0f;
		if ( look.Length < 0.01f )
			return "run";

		var from = look.Normal;
		var to = velocity.Normal;
		var yaw = MathX.RadianToDegree( MathF.Atan2( from.x * to.y - from.y * to.x, Vector3.Dot( from, to ) ) );
		var abs = MathF.Abs( yaw );

		var band = current switch
		{
			"run_backwards" => abs < BackExit ? (abs < SideExit ? "run" : "side") : "back",
			"run_sideways" => abs >= BackEnter ? "back" : (abs < SideExit ? "run" : "side"),
			_ => abs >= BackEnter ? "back" : (abs >= SideEnter ? "side" : "run")
		};

		if ( band == "back" )
			return "run_backwards";
		if ( band == "side" )
			return "run_sideways";
		return "run";
	}

	static void Aim( SkinnedModelRenderer skin, Vector3 body, Vector3 look )
	{
		body.z = 0f;
		look.z = 0f;
		if ( body.Length < 0.01f || look.Length < 0.01f )
			return;

		var from = body.Normal;
		var to = look.Normal;
		var yaw = MathX.RadianToDegree( MathF.Atan2( from.x * to.y - from.y * to.x, Vector3.Dot( from, to ) ) );
		yaw = Math.Clamp( yaw, -85f, 85f );
		if ( MathF.Abs( yaw ) < 0.25f )
			return;

		Twist( skin, "spine_2", yaw );
		Twist( skin, "Spine2", yaw );
	}

	static void Twist( SkinnedModelRenderer skin, string name, float degrees )
	{
		var bone = skin.Model?.Bones.GetBone( name );
		if ( bone is null )
			return;

		if ( !skin.TryGetBoneTransformAnimation( bone, out var world ) )
			return;

		var yawed = world.WithRotation( Rotation.FromAxis( Vector3.Up, degrees ) * world.Rotation );
		skin.SetBoneTransform( bone, skin.WorldTransform.ToLocal( yawed ) );
	}
}
