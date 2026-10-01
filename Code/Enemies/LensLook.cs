namespace LoopedLoaded;

public static class LensLook
{
	public const string ModelPath = "models/lens.vmdl";
	public const string IdleSequence = "idle";
	public static readonly Rotation FaceYaw = Rotation.FromYaw( 180f );

	public static SkinnedModelRenderer Attach( GameObject parent, float height )
	{
		var model = Model.Load( ModelPath );
		var tall = model.IsValid() ? model.Bounds.Size.z : 0f;
		var scale = tall > 0.001f ? height / tall : 1f;
		var lift = model.IsValid() ? -model.Bounds.Mins.z * scale : 0f;

		var go = parent.Scene.CreateObject();
		go.Name = "Lens";
		go.Parent = parent;
		go.LocalRotation = Rotation.Identity;
		go.LocalScale = Vector3.One * scale;
		go.LocalPosition = Vector3.Up * lift;

		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;
		renderer.UseAnimGraph = false;
		renderer.CreateBoneObjects = false;
		renderer.Sequence.Name = IdleSequence;
		renderer.Sequence.Looping = true;
		return renderer;
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
}
