namespace LoopedLoaded;

public sealed class DroppedRound : Component
{
	const float ChaseSpeed = 220f;
	const float RecallSpeed = 3600f;
	const float StopGap = 0.02f;
	const float BodyRadius = 13f;
	const float CursorLift = 40f;
	const float CursorGap = 92f;
	const float CursorSize = 72f;
	const string CursorPath = "ui/Arena UI/cursor_hand.png";

	[Property] public Color Tint { get; set; } = ShotColors.Player;

	public int SlotIndex { get; private set; }
	public Vector2 Flat { get; private set; }
	public bool Homing { get; private set; }

	GameLoop loop;
	GameObject shell;
	GameObject cursor;
	SpriteRenderer cursorHand;
	PointLight glow;
	Vector2 heading;
	bool shellReady;

	public void Place( GameLoop host, Vector2 flat, int slotIndex )
	{
		loop = host;
		SlotIndex = slotIndex;
		Flat = flat;
		WorldPosition = host.Geometry.ToPlayWorld( Flat );
	}

	public void SetFlat( Vector2 flat )
	{
		Flat = flat;
		if ( loop.IsValid() )
			WorldPosition = loop.Geometry.ToPlayWorld( Flat );
	}

	protected override void OnStart()
	{
		EnsureShell();

		glow = GraphicsApply.AddShotLight( GameObject, ShotColors.Player * 4f, 340f );
	}

	void EnsureShell()
	{
		if ( shellReady )
			return;

		if ( !RoundProjectile.TryAttach( GameObject, RoundProjectile.BodyDiameter( BodyRadius ), out shell ) )
			return;

		shellReady = true;
		FaceShell();
	}

	public void Recall( Vector2 player )
	{
		Homing = true;
		var to = player - Flat;
		if ( to.Length > 1f )
			heading = to.Normal;
	}

	public void StopRecall() => Homing = false;

	public bool FlyHome( Vector2 player, float playerRadius, float dt )
	{
		var to = player - Flat;
		var dist = to.Length;
		var reach = playerRadius + BodyRadius;
		if ( dist <= reach )
			return true;

		heading = to / dist;
		var gap = dist - reach;
		var step = RecallSpeed * MathF.Max( 0f, dt );
		if ( step >= gap )
			return true;

		SetFlat( Flat + heading * step );
		return false;
	}

	void FaceShell()
	{
		var dir = Homing && heading.Length > 0.01f
			? heading
			: Tangent();
		RoundProjectile.Face( shell, dir );
	}

	Vector2 Tangent()
	{
		var ang = MathF.Atan2( Flat.y, Flat.x );
		return new Vector2( -MathF.Sin( ang ), MathF.Cos( ang ) );
	}

	public float GapTo( float playerAngle )
	{
		var gap = playerAngle - MathF.Atan2( Flat.y, Flat.x );
		gap %= MathF.Tau;
		if ( gap < 0f )
			gap += MathF.Tau;
		return gap;
	}

	public bool Crosses( float gap, float playerArc, float dashArc, float track, float pace )
	{
		if ( track < 1f || dashArc <= 0.001f )
			return false;

		if ( gap <= StopGap )
			return playerArc > 0.001f;

		var closing = (playerArc + ChaseSpeed * pace * RealTime.Delta + dashArc) / track;
		return closing >= gap;
	}

	public void Roll( float playerAngle, float dashArc, float track, float pace )
	{
		if ( track < 1f )
			return;

		var gap = GapTo( playerAngle );
		if ( gap <= StopGap )
			return;

		var ang = MathF.Atan2( Flat.y, Flat.x );
		var step = (ChaseSpeed * pace * RealTime.Delta + MathF.Max( 0f, dashArc )) / track;
		SetFlat( ArenaGeometry.FromAngle( ang + MathF.Min( step, gap - StopGap ) ) * track );
	}

	public void ParkShort( float playerAngle, float track )
	{
		SetFlat( ArenaGeometry.FromAngle( playerAngle - StopGap ) * track );
	}

	protected override void OnUpdate()
	{
		EnsureShell();

		var pulse = 0.6f + 0.4f * MathF.Sin( Time.Now * 7f );

		if ( shell.IsValid() )
		{
			shell.LocalPosition = Vector3.Up * (14f * pulse);
			FaceShell();
		}

		if ( glow.IsValid() )
			glow.LightColor = ShotColors.Player * (2f + 4f * pulse);

		PaintCursor();
	}

	bool CanHint() => !Homing && loop.IsValid() && !loop.IsFrozen;

	Vector3 CursorSpot( float bob )
	{
		var aside = Vector3.Zero;
		var camera = Scene.Camera;
		if ( camera.IsValid() )
		{
			aside = -camera.WorldRotation.Up;
			aside.z = 0f;
			if ( aside.Length > 0.01f )
				aside = aside.Normal * CursorGap;
		}

		return aside + Vector3.Up * (CursorLift + bob);
	}

	void PaintCursor()
	{
		if ( !CanHint() )
		{
			if ( cursor.IsValid() )
				cursor.Enabled = false;
			return;
		}

		EnsureCursor();
		if ( !cursor.IsValid() || !cursorHand.IsValid() )
			return;

		cursor.Enabled = true;
		var beat = 0.5f + 0.5f * MathF.Sin( Time.Now * 4.5f );
		cursor.LocalPosition = CursorSpot( 8f * beat );
		cursor.LocalScale = Vector3.One * (0.84f + 0.28f * beat);
		cursorHand.Color = Color.White.WithAlpha( 0.72f + 0.28f * beat );
	}

	void EnsureCursor()
	{
		if ( cursor.IsValid() )
			return;

		var hand = Texture.Load( CursorPath );
		if ( !hand.IsValid() )
			return;

		cursor = Scene.CreateObject();
		cursor.Name = "Grab Cursor";
		cursor.Parent = GameObject;
		cursor.LocalPosition = CursorSpot( 0f );
		cursor.LocalRotation = Rotation.Identity;

		var go = Scene.CreateObject();
		go.Name = "Grab Hand";
		go.Parent = cursor;
		go.LocalPosition = Vector3.Zero;
		go.LocalRotation = Rotation.Identity;

		cursorHand = go.AddComponent<SpriteRenderer>();
		cursorHand.Sprite = Sprite.FromTexture( hand );
		cursorHand.Size = new Vector2( CursorSize, CursorSize );
		cursorHand.Billboard = SpriteRenderer.BillboardMode.Always;
		cursorHand.Opaque = false;
		cursorHand.Additive = false;
		cursorHand.Lighting = false;
		cursorHand.Shadows = false;
		cursorHand.Color = Color.White;
		cursorHand.TextureFilter = Sandbox.Rendering.FilterMode.Point;
	}
}
