namespace LoopedLoaded;

public sealed class DroppedRound : Component
{
	const float ChaseSpeed = 220f;
	const float StopGap = 0.02f;

	[Property] public Color Tint { get; set; } = ShotColors.Player;

	public int SlotIndex { get; private set; }
	public Vector2 Flat { get; private set; }

	GameLoop loop;
	GameObject shell;
	PointLight glow;

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
		shell = Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 30f, ShotColors.Player );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Player * 4f;
		glow.Radius = 340f;
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
		var pulse = 0.6f + 0.4f * MathF.Sin( Time.Now * 7f );

		if ( shell.IsValid() )
			shell.WorldPosition = WorldPosition + Vector3.Up * (14f * pulse);

		if ( glow.IsValid() )
			glow.LightColor = ShotColors.Player * (2f + 4f * pulse);
	}
}
