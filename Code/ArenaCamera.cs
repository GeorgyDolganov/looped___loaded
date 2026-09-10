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
	[Property] public float CityFrame { get; set; } = 1.7f;
	[Property] public float FrameMargin { get; set; } = 1.24f;
	[Property] public float FollowBias { get; set; } = 0.14f;
	[Property] public float FollowSmoothing { get; set; } = 6f;

	static readonly Color Backdrop = new Color( 0.008f, 0.011f, 0.018f );
	static readonly Color HurtTint = new Color( 0.22f, 0.02f, 0.03f );

	CameraComponent camera;
	Vector3 focus;
	float pull;
	bool framingCity;

	protected override void OnAwake()
	{
		camera = GetComponent<CameraComponent>() ?? GameObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = true;
		camera.ZNear = 10f;
		camera.ZFar = 20000f;
	}

	protected override void OnUpdate()
	{
		if ( !camera.IsValid() )
			return;

		camera.BackgroundColor = Backdrop;

		var city = Loop.IsValid() && Loop.InCity && City.IsValid();
		if ( city )
		{
			Frame( City.Center + new Vector3( 0f, -City.Span * 0.18f, 0f ), PullFor( City.Span * CityFrame ), true );
			return;
		}

		if ( !Arena.IsValid() )
			return;

		var target = Vector3.Zero;
		if ( Runner.IsValid() )
		{
			var flat = Runner.Flat * FollowBias;
			target = new Vector3( flat.x, flat.y, 0f );
		}

		Frame( target, Distance, false );
		ShakeIfHurt();
	}

	void Frame( Vector3 target, float distance, bool city )
	{
		var blend = MathF.Min( 1f, Time.Delta * FollowSmoothing );
		focus = framingCity == city
			? focus.LerpTo( target, blend )
			: target;
		pull = pull <= 1f ? distance : pull.LerpTo( distance, blend );
		framingCity = city;

		var rotation = Rotation.From( Pitch, Yaw, 0f );
		camera.WorldRotation = rotation;
		camera.WorldPosition = focus - rotation.Forward * pull;
	}

	float PullFor( float height )
	{
		var fov = MathX.DegreeToRadian( Math.Clamp( camera.FieldOfView, 1f, 179f ) );
		var aspect = Screen.Width / Math.Max( 1f, Screen.Height );
		if ( camera.FovAxis == CameraComponent.Axis.Horizontal )
			fov = 2f * MathF.Atan( MathF.Tan( fov * 0.5f ) / aspect );

		return height * 0.5f / MathF.Max( 0.0001f, MathF.Tan( fov * 0.5f ) );
	}

	void ShakeIfHurt()
	{
		if ( !Loop.IsValid() )
			return;

		var hurt = Loop.HurtAmount;
		if ( hurt <= 0.01f )
			return;

		var amp = hurt * 52f;
		var t = Time.Now * 54f;
		var rot = camera.WorldRotation;
		camera.WorldPosition += rot.Right * MathF.Sin( t ) * amp
			+ rot.Up * MathF.Cos( t * 1.37f ) * amp * 0.72f;
		camera.BackgroundColor = Color.Lerp( Backdrop, HurtTint, hurt );
	}
}
