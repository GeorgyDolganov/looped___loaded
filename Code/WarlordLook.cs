namespace LoopedLoaded;

public static class WarlordLook
{
	public const string ModelPath = "models/warlord.vmdl";
	public const string AnimGraphPath = "models/citizen/citizen.vanmgrph";

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

		var renderer = go.AddComponent<SkinnedModelRenderer>();
		renderer.Model = model;
		renderer.UseAnimGraph = true;
		renderer.AnimationGraph = ResourceLibrary.Get<AnimationGraph>( AnimGraphPath );
		renderer.Set( "b_grounded", true );
		return renderer;
	}

	public static void Drive( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look, int holdType )
	{
		TerryLook.Drive( skin, velocity, look, holdType );
	}
}
