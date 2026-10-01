namespace LoopedLoaded;

public static class EkkeLook
{
	public const string ModelPath = "models/ekke.vmdl";
	public const string IdleSequence = "idle";
	public const string TalkSequence = "talk";

	const string OverlayName = "Ekke Overlay";
	const string HeadBone = "Head";
	const string NeckBone = "Neck";

	static readonly string[] LockedBones =
	{
		"Face_Front", "Face_Front_Eyes",
		"Face_Left", "Face_Left_Eyes",
		"Face_Back", "Face_Back_Eyes",
		"Face_Right", "Face_Right_Eyes",
		"Corner_FrontLeft", "Corner_BackLeft", "Corner_BackRight", "Corner_FrontRight",
	};

	static SkinnedModelRenderer shown;
	static float talkUntil;
	static bool talking;

	public static void Bind( SkinnedModelRenderer renderer )
	{
		shown = renderer;
		PlayIdle( renderer );
	}

	public static void Release( SkinnedModelRenderer renderer )
	{
		if ( shown != renderer )
			return;

		if ( renderer.IsValid() && renderer.SceneModel.IsValid() )
			renderer.SceneModel.ClearBoneOverrides();

		DropOverlay( renderer );
		shown = null;
		talking = false;
		talkUntil = 0f;
	}

	public static void Pulse()
	{
		if ( !shown.IsValid() )
			return;

		var overlay = EnsureOverlay( shown );
		if ( !overlay.IsValid() )
			return;

		overlay.UseAnimGraph = false;
		var start = overlay.Sequence.Name != TalkSequence || !overlay.Sequence.Looping;
		overlay.Sequence.Name = TalkSequence;
		if ( overlay.Sequence.Name != TalkSequence )
			return;

		overlay.Sequence.Looping = true;
		if ( start )
			overlay.Sequence.Time = 0f;
		Hide( overlay );
		var duration = overlay.Sequence.Duration;
		talkUntil = Time.Now + (duration > 0.05f ? duration : 0.5f);
	}

	public static void Drive( SkinnedModelRenderer skin )
	{
		PlayIdle( skin );
		if ( !skin.IsValid() )
			return;

		var overlay = OverlayOf( skin );
		var speaking = Time.Now < talkUntil || (talking && overlay.IsValid() && !HeadHome( overlay ) && Time.Now < talkUntil + 0.6f);
		if ( !speaking )
		{
			if ( !talking )
				return;

			talking = false;
			if ( skin.SceneModel.IsValid() )
				skin.SceneModel.ClearBoneOverrides();
			DropOverlay( skin );
			return;
		}

		talking = true;
		ApplyNod( skin, EnsureOverlay( skin ) );
	}

	public static void PlayIdle( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() )
			return;

		skin.UseAnimGraph = false;
		if ( skin.Sequence.Name == IdleSequence )
			return;

		skin.Sequence.Name = IdleSequence;
		skin.Sequence.Looping = true;
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
		var go = skin.GameObject.Parent.Scene.CreateObject();
		go.Name = OverlayName;
		go.Parent = skin.GameObject.Parent;
		go.LocalRotation = Rotation.Identity;
		go.LocalScale = Vector3.One * scale;
		go.LocalPosition = Vector3.Up * lift;

		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = skin.Model;
		renderer.UseAnimGraph = false;
		renderer.CreateBoneObjects = false;
		renderer.Sequence.Name = TalkSequence;
		renderer.Sequence.Looping = true;
		Hide( renderer );
		return renderer;
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

	static void DropOverlay( SkinnedModelRenderer skin )
	{
		var overlay = OverlayOf( skin );
		if ( overlay.IsValid() )
			overlay.GameObject.Destroy();
	}

	static void Hide( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() || !skin.SceneObject.IsValid() )
			return;

		skin.SceneObject.RenderingEnabled = false;
	}

	static bool HeadHome( SkinnedModelRenderer overlay )
	{
		if ( !overlay.IsValid() )
			return true;

		var neck = overlay.Model?.Bones.GetBone( NeckBone );
		var head = overlay.Model?.Bones.GetBone( HeadBone );
		if ( neck is null || head is null )
			return true;

		if ( !overlay.TryGetBoneTransformAnimation( neck, out var neckWorld ) )
			return true;

		if ( !overlay.TryGetBoneTransformAnimation( head, out var headWorld ) )
			return true;

		var up = neckWorld.ToLocal( headWorld ).Rotation * Vector3.Up;
		return Vector3.Dot( up, Vector3.Up ) > 0.999f;
	}

	static void ApplyNod( SkinnedModelRenderer body, SkinnedModelRenderer overlay )
	{
		if ( !body.IsValid() || !overlay.IsValid() )
			return;

		Hide( overlay );
		overlay.WorldTransform = body.WorldTransform;
		if ( overlay.Sequence.Name != TalkSequence )
			return;

		var head = body.Model?.Bones.GetBone( HeadBone );
		var overlayNeck = overlay.Model?.Bones.GetBone( NeckBone );
		var overlayHead = overlay.Model?.Bones.GetBone( HeadBone );
		if ( head is null || overlayNeck is null || overlayHead is null )
			return;

		if ( !body.TryGetBoneTransformAnimation( head, out var headWorld ) )
			return;

		if ( !overlay.TryGetBoneTransformAnimation( overlayNeck, out var overlayNeckWorld ) )
			return;

		if ( !overlay.TryGetBoneTransformAnimation( overlayHead, out var overlayHeadWorld ) )
			return;

		var delta = overlayNeckWorld.ToLocal( overlayHeadWorld ).Rotation;
		var nodded = headWorld.WithRotation( headWorld.Rotation * delta );
		body.SetBoneTransform( head, body.WorldTransform.ToLocal( nodded ) );

		foreach ( var name in LockedBones )
		{
			var bone = body.Model?.Bones.GetBone( name );
			if ( bone is null )
				continue;

			if ( !body.TryGetBoneTransformAnimation( bone, out var boneWorld ) )
				continue;

			var local = headWorld.ToLocal( boneWorld );
			var world = nodded.ToWorld( local );
			body.SetBoneTransform( bone, body.WorldTransform.ToLocal( world ) );
		}
	}
}
