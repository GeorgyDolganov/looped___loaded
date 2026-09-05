namespace LoopedLoaded;

public sealed class DroppedRound : Component
{
	[Property] public Color Tint { get; set; } = new Color( 1f, 0.72f, 0.22f );

	public int SlotIndex { get; private set; }
	public Vector2 Flat { get; private set; }

	GameObject shell;
	PointLight glow;

	public void Place( ArenaGeometry arena, Vector2 flat, int slotIndex )
	{
		SlotIndex = slotIndex;
		Flat = flat;
		WorldPosition = arena.ToPlayWorld( Flat );
	}

	protected override void OnStart()
	{
		shell = Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 30f, Tint );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = Tint * 4f;
		glow.Radius = 340f;
	}

	protected override void OnUpdate()
	{
		var pulse = 0.6f + 0.4f * MathF.Sin( Time.Now * 7f );

		if ( shell.IsValid() )
			shell.WorldPosition = WorldPosition + Vector3.Up * (14f * pulse);

		if ( glow.IsValid() )
			glow.LightColor = Tint * (2f + 4f * pulse);
	}
}
