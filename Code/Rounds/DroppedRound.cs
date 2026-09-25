namespace LoopedLoaded;

public sealed class DroppedRound : Component
{
	const float ChaseSpeed = 220f;
	const float StopGap = 0.02f;
	const float BodyRadius = 13f;

	[Property] public Color Tint { get; set; } = ShotColors.Player;

	public int SlotIndex { get; private set; }
	public Vector2 Flat { get; private set; }

	GameLoop loop;
	GameObject shell;
	PointLight glow;
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

	void FaceShell()
	{
		var ang = MathF.Atan2( Flat.y, Flat.x );
		RoundProjectile.Face( shell, new Vector2( -MathF.Sin( ang ), MathF.Cos( ang ) ) );
	}

	public float GapTo( float playerAngle )
	{
		var gap = playerAngle - MathF.Atan2( Flat.y, Flat.x );
		gap %= MathF.Tau;
		if ( gap < 0f )
			gap += MathF.Tau;
		return gap;
	}

	public bool Crosses( float gap, float playerArc, float dashArc, float track )
	{
		if ( track < 1f || dashArc <= 0.001f )
			return false;

		if ( gap <= StopGap )
			return playerArc > 0.001f;

		var closing = (playerArc + ChaseSpeed * Time.Delta + dashArc) / track;
		return closing >= gap;
	}

	public void Roll( float playerAngle, float dashArc, float track )
	{
		if ( track < 1f )
			return;

		var gap = GapTo( playerAngle );
		if ( gap <= StopGap )
			return;

		var ang = MathF.Atan2( Flat.y, Flat.x );
		var step = (ChaseSpeed * Time.Delta + MathF.Max( 0f, dashArc )) / track;
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
	}
}
