namespace LoopedLoaded;

public static class TerryLook
{
	public const string ModelPath = "models/citizen/citizen.vmdl";
	public const float BodyScale = 3.2f;
	public const float CoreScale = 7.5f;
	public const float CitizenHeight = 72f;

	public static float Height( bool core ) => CitizenHeight * (core ? CoreScale : BodyScale);

	public static SkinnedModelRenderer Attach( GameObject parent, bool localClothes, float scale = 1f )
	{
		var go = parent.Scene.CreateObject();
		go.Name = localClothes ? "Terry" : "Terry Enemy";
		go.Parent = parent;
		go.LocalPosition = Vector3.Zero;
		go.LocalRotation = Rotation.Identity;
		go.LocalScale = Vector3.One * MathF.Max( 0.01f, scale );

		var skin = go.AddComponent<SkinnedModelRenderer>();
		skin.Model = Model.Load( ModelPath );
		skin.UseAnimGraph = true;
		skin.Set( "b_grounded", true );

		var dresser = go.AddComponent<Dresser>();
		dresser.BodyTarget = skin;
		dresser.ApplyHeightScale = localClothes;
		dresser.Clothing = new();

		if ( localClothes )
		{
			dresser.Source = Dresser.ClothingSource.LocalUser;
			_ = dresser.Apply();
		}
		else
		{
			dresser.Source = Dresser.ClothingSource.Manual;
			dresser.Randomize();
		}

		return skin;
	}

	public static void Drive( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look, int holdType )
	{
		if ( !skin.IsValid() )
			return;

		var rotation = skin.WorldRotation;
		var forward = rotation.Forward.Dot( velocity );
		var sideward = rotation.Right.Dot( velocity );
		var speed = velocity.Length;

		skin.Set( "b_grounded", true );
		skin.Set( "move_direction", MathX.RadianToDegree( MathF.Atan2( sideward, forward ) ) );
		skin.Set( "move_speed", speed );
		skin.Set( "move_groundspeed", speed );
		skin.Set( "move_x", forward );
		skin.Set( "move_y", sideward );
		skin.Set( "move_z", 0f );
		skin.Set( "holdtype", holdType );
		skin.Set( "holdtype_handedness", holdType > 0 ? 1 : 0 );

		if ( look.Length > 0.01f )
			skin.SetLookDirection( "aim_eyes", look );
	}
}
