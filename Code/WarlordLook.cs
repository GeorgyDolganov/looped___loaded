namespace LoopedLoaded;

public static class WarlordLook
{
	public const string ModelPath = "models/warlord.vmdl";
	public const string WeaponPath = "models/bfg.vmdl";

	const float WeaponWorld = 125f;
	const float WeaponGrip = 0f;
	const float WeaponSide = 40f;
	const float WeaponLift = 20f;
	const float WeaponYaw = 180f;
	const float WeaponPitch = 125f;
	const float WeaponRoll = -20f;

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
		renderer.UseAnimGraph = false;
		renderer.CreateBoneObjects = true;
		renderer.Sequence.Name = "idle";
		renderer.Sequence.Looping = true;
		return renderer;
	}

	public static GameObject AttachWeapon( SkinnedModelRenderer skin )
	{
		var go = skin.Scene.CreateObject();
		go.Name = "BFG";
		go.Parent = skin.GameObject.Parent ?? skin.GameObject;

		var renderer = go.AddComponent<ModelRenderer>();
		renderer.Model = Model.Load( WeaponPath );
		return go;
	}

	public static void Hold( GameObject weapon, SkinnedModelRenderer skin, Vector3 look )
	{
		if ( !weapon.IsValid() || !skin.IsValid() )
			return;

		if ( !HandWorld( skin, out var right ) )
		{
			Place( weapon, skin.WorldPosition + skin.WorldRotation.Forward * 80f + Vector3.Up * 140f, skin.WorldRotation );
			return;
		}

		var rot = right.Rotation * Rotation.From( WeaponPitch, WeaponYaw, WeaponRoll );
		Place( weapon, right.Position + rot.Forward * WeaponGrip + rot.Right * WeaponSide + rot.Up * WeaponLift, rot );
	}

	static bool HandWorld( SkinnedModelRenderer skin, out Transform tx )
	{
		tx = default;
		var bone = skin.Model?.Bones.GetBone( "hand_R" );
		if ( bone is not null && skin.TryGetBoneTransformAnimation( bone, out tx ) )
			return true;

		return skin.TryGetBoneTransform( "hand_R", out tx );
	}

	static void Place( GameObject weapon, Vector3 position, Rotation rotation )
	{
		var renderer = weapon.GetComponent<ModelRenderer>();
		var size = renderer.IsValid() ? renderer.Model?.Bounds.Size ?? Vector3.Zero : Vector3.Zero;
		var length = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		if ( length < 0.01f )
			length = 1f;

		weapon.WorldRotation = rotation;
		weapon.WorldPosition = position;
		weapon.WorldScale = Vector3.One * (WeaponWorld / length);
	}

	public static bool TryMuzzle( GameObject weapon, out Vector3 world )
	{
		world = default;
		if ( !weapon.IsValid() )
			return false;

		var renderer = weapon.GetComponent<ModelRenderer>();
		var size = renderer.IsValid() ? renderer.Model?.Bounds.Size ?? Vector3.Zero : Vector3.Zero;
		var length = MathF.Max( 0.2f, size.x * 0.5f ) * weapon.WorldScale.x;
		world = weapon.WorldPosition + weapon.WorldRotation.Forward * length;
		return true;
	}

	const float SideEnter = 60f;
	const float SideExit = 45f;
	const float BackEnter = 130f;
	const float BackExit = 115f;

	public static void Drive( SkinnedModelRenderer skin, Vector3 velocity, Vector3 look, int holdType )
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

		Face( skin, velocity, look );
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

	static string Locomotion( string current, Vector3 velocity, Vector3 look )
	{
		if ( velocity.Length <= 40f )
			return "idle";

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
			"run_s" => abs < BackExit ? (abs < SideExit ? "run" : "side") : "back",
			"run_e" or "run_w" => abs >= BackEnter ? "back" : (abs < SideExit ? "run" : "side"),
			_ => abs >= BackEnter ? "back" : (abs >= SideEnter ? "side" : "run")
		};

		if ( band == "back" )
			return "run_s";
		if ( band == "side" )
			return yaw > 0f ? "run_w" : "run_e";
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
