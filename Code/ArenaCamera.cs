namespace LoopedLoaded;

public sealed class ArenaCamera : Component
{
	[Property] public GameLoop Loop { get; set; }
	[Property] public CityBoard City { get; set; }
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public float Pitch { get; set; } = 68f;
	[Property] public float Yaw { get; set; } = 90f;
	[Property] public float Distance { get; set; } = 4000f;
	[Property] public float FrameMargin { get; set; } = 1.24f;
	[Property] public float FollowBias { get; set; } = 0.14f;
	[Property] public float FollowSmoothing { get; set; } = 6f;

	static readonly Color Backdrop = new Color( 0.008f, 0.011f, 0.018f );

	CameraComponent camera;
	Vector3 focus;
	bool framingCity;

	protected override void OnAwake()
	{
		camera = GetComponent<CameraComponent>() ?? GameObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = true;
	}

	protected override void OnUpdate()
	{
		if ( !camera.IsValid() )
			return;

		camera.Orthographic = true;
		camera.ZNear = 10f;
		camera.ZFar = 20000f;
		camera.BackgroundColor = Backdrop;

		if ( Loop.IsValid() && Loop.InCity && City.IsValid() )
		{
			camera.OrthographicHeight = City.Span * 1.9f;
			var cityFocus = City.Center + new Vector3( 0f, -City.Span * 0.18f, 0f );
			if ( !framingCity )
				focus = cityFocus;
			else
				focus = focus.LerpTo( cityFocus, MathF.Min( 1f, Time.Delta * FollowSmoothing ) );

			framingCity = true;
			var cityRotation = Rotation.From( Pitch, Yaw, 0f );
			camera.WorldRotation = cityRotation;
			camera.WorldPosition = focus - cityRotation.Forward * Distance;
			return;
		}

		if ( !Arena.IsValid() )
			return;

		camera.OrthographicHeight = Arena.Geometry.BoundaryRadius * 2f * FrameMargin;

		var target = Vector3.Zero;

		if ( Runner.IsValid() )
		{
			var flat = Runner.Flat * FollowBias;
			target = new Vector3( flat.x, flat.y, 0f );
		}

		if ( framingCity )
			focus = target;
		else
			focus = focus.LerpTo( target, MathF.Min( 1f, Time.Delta * FollowSmoothing ) );

		framingCity = false;

		var rotation = Rotation.From( Pitch, Yaw, 0f );
		camera.WorldRotation = rotation;
		camera.WorldPosition = focus - rotation.Forward * Distance;
		ShakeIfHurt();
	}

	void ShakeIfHurt()
	{
		if ( !camera.IsValid() || !Loop.IsValid() || Loop.InCity )
			return;

		var hurt = Loop.HurtAmount;
		if ( hurt <= 0.01f )
		{
			camera.BackgroundColor = Backdrop;
			return;
		}

		var amp = hurt * 52f;
		var t = Time.Now * 54f;
		camera.WorldPosition += camera.WorldRotation.Right * MathF.Sin( t ) * amp
			+ camera.WorldRotation.Up * MathF.Cos( t * 1.37f ) * amp * 0.72f;
		camera.BackgroundColor = Color.Lerp( Backdrop, new Color( 0.22f, 0.02f, 0.03f ), hurt );
	}
}
