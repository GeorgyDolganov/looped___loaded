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

	protected override void OnStart()
	{
		shell = Blocks.SpawnSphere( GameObject, "Shell", WorldPosition, 30f, ShotColors.Player );

		glow = GameObject.AddComponent<PointLight>();
		glow.LightColor = ShotColors.Player * 4f;
		glow.Radius = 340f;
	}

	protected override void OnUpdate()
	{
		var pulse = 0.6f + 0.4f * MathF.Sin( Time.Now * 7f );

		if ( loop.IsValid() && !loop.IsFrozen && loop.Inventory.IsValid() && loop.Geometry is not null )
		{
			var speed = loop.Inventory.Loadout.ReelSpeed;
			if ( speed > 1f )
			{
				var radius = loop.Geometry.TrackRadius;
				var angle = ArenaGeometry.ToAngle( Flat );
				angle -= speed / MathF.Max( 1f, radius ) * Time.Delta;
				Flat = ArenaGeometry.FromAngle( angle ) * radius;
				WorldPosition = loop.Geometry.ToPlayWorld( Flat );
			}
		}

		if ( shell.IsValid() )
			shell.WorldPosition = WorldPosition + Vector3.Up * (14f * pulse);

		if ( glow.IsValid() )
			glow.LightColor = ShotColors.Player * (2f + 4f * pulse);
	}
}
