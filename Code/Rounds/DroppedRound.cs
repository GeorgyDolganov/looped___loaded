namespace LoopedLoaded;

public sealed class DroppedRound : Component
{
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

	protected override void OnUpdate()
	{
		Chase();

		var pulse = 0.6f + 0.4f * MathF.Sin( Time.Now * 7f );

		if ( shell.IsValid() )
			shell.WorldPosition = WorldPosition + Vector3.Up * (14f * pulse);

		if ( glow.IsValid() )
			glow.LightColor = ShotColors.Player * (2f + 4f * pulse);
	}

	void Chase()
	{
		if ( !loop.IsValid() || loop.IsFrozen || !loop.Runner.IsValid() || loop.Geometry is null )
			return;

		var track = loop.Geometry.TrackRadius;
		if ( track < 1f )
			return;

		var ang = MathF.Atan2( Flat.y, Flat.x );
		var gap = loop.Runner.Angle - ang;
		gap %= MathF.Tau;
		if ( gap < 0f )
			gap += MathF.Tau;

		if ( gap <= 0.02f )
			return;

		var step = 220f / track * Time.Delta;
		SetFlat( ArenaGeometry.FromAngle( ang + MathF.Min( step, gap ) ) * track );
	}
}
