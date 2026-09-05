namespace LoopedLoaded;

public sealed class ArenaCamera : Component
{
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

	protected override void OnAwake()
	{
		camera = GetComponent<CameraComponent>() ?? GameObject.AddComponent<CameraComponent>();
		camera.IsMainCamera = true;
	}

	protected override void OnUpdate()
	{
		if ( !camera.IsValid() || !Arena.IsValid() )
			return;

		camera.Orthographic = true;
		camera.OrthographicHeight = Arena.Geometry.BoundaryRadius * 2f * FrameMargin;
		camera.ZNear = 10f;
		camera.ZFar = 20000f;
		camera.BackgroundColor = Backdrop;

		var target = Vector3.Zero;

		if ( Runner.IsValid() )
		{
			var flat = Runner.Flat * FollowBias;
			target = new Vector3( flat.x, flat.y, 0f );
		}

		focus = focus.LerpTo( target, MathF.Min( 1f, Time.Delta * FollowSmoothing ) );

		var rotation = Rotation.From( Pitch, Yaw, 0f );
		camera.WorldRotation = rotation;
		camera.WorldPosition = focus - rotation.Forward * Distance;
	}
}
