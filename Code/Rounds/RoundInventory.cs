namespace LoopedLoaded;

public sealed class RoundInventory : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public float CatchRadius { get; set; } = 105f;
	[Property] public float CatchOffset { get; set; } = 60f;
	[Property] public float PickupRadius { get; set; } = 145f;
	[Property] public Color RoundTint { get; set; } = new Color( 1f, 0.72f, 0.22f );

	public RoundStatus Status { get; private set; } = RoundStatus.Chambered;
	public RoundProjectile Flying { get; private set; }
	public DroppedRound Lost { get; private set; }

	public Vector2 CatchPoint => Runner.IsValid() && Aim.IsValid()
		? Runner.Flat + Aim.Direction * CatchOffset
		: Vector2.Zero;

	public bool InCatchZone( Vector2 flat, float radius ) => (flat - CatchPoint).Length <= CatchRadius + radius;

	PolyLine catchRing;
	GameObject chamberedMarker;

	public void Chamber()
	{
		Flying?.GameObject?.Destroy();
		Lost?.GameObject?.Destroy();
		Flying = null;
		Lost = null;
		Status = RoundStatus.Chambered;
	}

	public void SetFlying( RoundProjectile projectile )
	{
		Flying = projectile;
		Lost = null;
		Status = RoundStatus.InFlight;
	}

	public void SetLost( DroppedRound dropped )
	{
		Flying = null;
		Lost = dropped;
		Status = RoundStatus.Dropped;
	}

	protected override void OnStart()
	{
		var ringObject = Scene.CreateObject();
		ringObject.Name = "Catch Ring";
		ringObject.Parent = GameObject;

		catchRing = ringObject.AddComponent<PolyLine>();
		catchRing.HeadWidth = 4f;
		catchRing.TailWidth = 4f;
		catchRing.Apply();

		chamberedMarker = Blocks.SpawnSphere( GameObject, "Chambered", Vector3.Zero, 20f, RoundTint );
	}

	protected override void OnUpdate()
	{
		if ( !Arena.IsValid() || !Runner.IsValid() || !Aim.IsValid() )
			return;

		var ready = Status == RoundStatus.Chambered;
		var tint = ready ? RoundTint : new Color( 0.35f, 0.45f, 0.55f );

		if ( catchRing.IsValid() )
		{
			catchRing.HeadTint = tint;
			catchRing.TailTint = tint;
			catchRing.Apply();
			catchRing.SetPoints( BuildRing() );
		}

		if ( chamberedMarker.IsValid() )
		{
			chamberedMarker.Enabled = ready;
			chamberedMarker.WorldPosition = Arena.Geometry.ToPlayWorld( Runner.Flat ) + Vector3.Up * 66f;
		}
	}

	List<Vector3> BuildRing()
	{
		const int segments = 24;
		var center = CatchPoint;
		var points = new List<Vector3>( segments + 1 );

		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			var offset = ArenaGeometry.FromAngle( angle ) * CatchRadius;
			points.Add( Arena.Geometry.ToWorld( center + offset, 12f ) );
		}

		return points;
	}
}
