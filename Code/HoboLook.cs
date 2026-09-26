namespace LoopedLoaded;

public static class HoboLook
{
	public const string ModelPath = "models/hobo.vmdl";
	public const string ShooterModelPath = "models/hoboshoot.vmdl";
	public const float Size = 1.7f;

	const string OverlayName = "Hobo Overlay";
	const string AttackSequence = "attack";
	const string ShootSequence = "shoot";
	const string UpperRoot = "spine_1";

	static readonly string[] UpperBones =
	{
		"spine", "spine_1", "spine_2", "neck", "head",
		"shoulder_L", "shoulder_R",
		"hand_ik_L", "hand_ik_R",
		"fist_L", "fist_R",
		"elbow_pole_L", "elbow_pole_R"
	};

	public static SkinnedModelRenderer Attach( GameObject parent, float height, string path = ModelPath )
	{
		var model = Model.Load( path );
		var scale = ScaleOf( height );
		var lift = -model.Bounds.Mins.z * scale;

		return MakeSkin( parent, "Hobo", model, scale, lift, false );
	}

	public static float ScaleOf( float height )
	{
		var size = Model.Load( ModelPath ).Bounds.Size;
		var longest = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		return longest > 0.001f ? height * Size / longest : 1f;
	}

	public static float TopOf( float height, string path = ModelPath ) => Model.Load( path ).Bounds.Size.z * ScaleOf( height );

	static SkinnedModelRenderer MakeSkin( GameObject parent, string name, Model model, float scale, float lift, bool hide )
	{
		var go = parent.Scene.CreateObject();
		go.Name = name;
		go.Parent = parent;
		go.LocalRotation = Rotation.Identity;
		go.LocalScale = Vector3.One * scale;
		go.LocalPosition = Vector3.Up * lift;

		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;
		renderer.UseAnimGraph = false;
		renderer.CreateBoneObjects = false;
		renderer.Sequence.Name = "idle";
		renderer.Sequence.Looping = true;
		if ( hide )
			Hide( renderer );
		return renderer;
	}

	static void Hide( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() || !skin.SceneObject.IsValid() )
			return;

		skin.SceneObject.RenderingEnabled = false;
	}

	static SkinnedModelRenderer OverlayOf( SkinnedModelRenderer skin )
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

	public static float PlayAttack( SkinnedModelRenderer skin ) => PlayOverlay( skin, AttackSequence, 16f / 24f );

	public static float PlayShoot( SkinnedModelRenderer skin ) => PlayOverlay( skin, ShootSequence, 8f / 24f );

	static float PlayOverlay( SkinnedModelRenderer skin, string sequence, float fallback )
	{
		var overlay = EnsureOverlay( skin );
		if ( !overlay.IsValid() )
			return 0f;

		overlay.UseAnimGraph = false;
		overlay.Sequence.Name = sequence;
		overlay.Sequence.Looping = false;
		Hide( overlay );
		var duration = overlay.Sequence.Duration;
		return duration > 0.05f ? duration : fallback;
	}

	public static void Drive( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look, bool attacking )
	{
		if ( !skin.IsValid() )
			return;

		skin.UseAnimGraph = false;
		var name = velocity.Length <= 40f ? "idle" : "run";
		if ( skin.Sequence.Name != name )
		{
			skin.Sequence.Name = name;
			skin.Sequence.Looping = true;
		}

		if ( attacking )
			CopyUpper( skin, EnsureOverlay( skin ) );
		else
			DropOverlay( skin );

		WarlordLook.Face( skin, velocity, look );
	}

	static SkinnedModelRenderer EnsureOverlay( SkinnedModelRenderer skin )
	{
		var found = OverlayOf( skin );
		if ( found.IsValid() )
			return found;

		if ( !skin.IsValid() || !skin.GameObject.IsValid() || !skin.GameObject.Parent.IsValid() || !skin.Model.IsValid() )
			return null;

		var scale = skin.GameObject.LocalScale.x;
		var lift = skin.GameObject.LocalPosition.z;
		return MakeSkin( skin.GameObject.Parent, OverlayName, skin.Model, scale, lift, true );
	}

	static void DropOverlay( SkinnedModelRenderer skin )
	{
		var overlay = OverlayOf( skin );
		if ( overlay.IsValid() )
			overlay.GameObject.Destroy();
	}

	static void CopyUpper( SkinnedModelRenderer body, SkinnedModelRenderer overlay )
	{
		if ( !body.IsValid() || !overlay.IsValid() )
			return;

		Hide( overlay );
		overlay.WorldTransform = body.WorldTransform;

		if ( overlay.Sequence.Name == ShootSequence )
		{
			CopyTree( body, overlay, overlay.Model?.Bones.GetBone( UpperRoot ) );
			return;
		}

		foreach ( var name in UpperBones )
		{
			var bone = overlay.Model?.Bones.GetBone( name );
			if ( bone is null )
				continue;

			CopyBone( body, overlay, bone );
		}
	}

	static void CopyTree( SkinnedModelRenderer body, SkinnedModelRenderer overlay, BoneCollection.Bone bone )
	{
		if ( bone is null )
			return;

		CopyBone( body, overlay, bone );
		foreach ( var child in bone.Children )
			CopyTree( body, overlay, child );
	}

	static void CopyBone( SkinnedModelRenderer body, SkinnedModelRenderer overlay, BoneCollection.Bone bone )
	{
		if ( !overlay.TryGetBoneTransformAnimation( bone, out var world ) )
			return;

		body.SetBoneTransform( bone, body.WorldTransform.ToLocal( world ) );
	}
}
