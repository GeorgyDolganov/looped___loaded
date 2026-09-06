namespace LoopedLoaded;

public sealed class RoundInventory : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float CatchRadius { get; set; } = 105f;
	[Property] public float CatchOffset { get; set; } = 82f;
	[Property] public float PickupRadius { get; set; } = 145f;

	public RunLoadout Loadout { get; } = new();
	public List<RoundSlot> Slots { get; } = new();
	public int SelectedIndex { get; private set; }
	public RoundSlot Selected => Slots.Count == 0 ? null : Slots[Math.Clamp( SelectedIndex, 0, Slots.Count - 1 )];

	public Vector2 CatchPoint => Runner.IsValid() && Aim.IsValid()
		? Runner.Flat + Aim.Direction * CatchOffset
		: Vector2.Zero;

	PolyLine catchRing;
	GameObject chamberedMarker;

	public void ResetLoadout()
	{
		foreach ( var slot in Slots )
			slot.ResetCombat();

		Slots.Clear();
		Loadout.Clear();
		GrantSlot();
		SelectedIndex = 0;
	}

	public RoundSlot GrantSlot()
	{
		if ( Slots.Count >= Progression.MaxSlots )
			return null;

		var slot = new RoundSlot
		{
			Index = Slots.Count,
			Tint = ShotColors.Player,
			Status = RoundStatus.Chambered
		};

		Slots.Add( slot );
		return slot;
	}

	public bool TrySelect( int index )
	{
		if ( index < 0 || index >= Slots.Count )
			return false;

		if ( Slots[index].Status != RoundStatus.Chambered )
			return false;

		SelectedIndex = index;
		return true;
	}

	public void SelectNextChambered( int direction = 1 )
	{
		if ( Slots.Count == 0 )
			return;

		for ( var step = 1; step <= Slots.Count; step++ )
		{
			var index = (SelectedIndex + direction * step + Slots.Count * 8) % Slots.Count;
			if ( Slots[index].Status == RoundStatus.Chambered )
			{
				SelectedIndex = index;
				return;
			}
		}
	}

	public void AfterFired( RoundSlot slot )
	{
		SelectNextChambered( 1 );

		if ( Selected is null || Selected.Status != RoundStatus.Chambered )
			SelectedIndex = slot.Index;
	}

	public bool InCatchZone( Vector2 flat, float radius, float catchBonus )
		=> (flat - CatchPoint).Length <= CatchRadius + catchBonus + radius;

	public float ArcTo( Vector2 flat )
	{
		if ( !Runner.IsValid() || !Loop.IsValid() )
			return 0f;

		var angle = flat.Length < 1f ? Runner.Angle : ArenaGeometry.ToAngle( flat );
		return Wrap( Runner.Angle - angle ) * Loop.Geometry.TrackRadius;
	}

	static float Wrap( float radians )
	{
		radians %= MathF.Tau;
		return radians < 0f ? radians + MathF.Tau : radians;
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

		chamberedMarker = Blocks.SpawnSphere( GameObject, "Chambered", Vector3.Zero, 20f, ShotColors.Player );
	}

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && (Loop.InCity || Loop.InMenu) )
		{
			if ( catchRing.IsValid() )
				catchRing.Clear();
			if ( chamberedMarker.IsValid() )
				chamberedMarker.Enabled = false;
			return;
		}

		if ( !Arena.IsValid() || !Runner.IsValid() || !Aim.IsValid() )
			return;

		var slot = Selected;
		var ready = slot is not null && slot.Status == RoundStatus.Chambered;
		var tint = slot is not null && ready ? slot.Tint : new Color( 0.35f, 0.45f, 0.55f );

		if ( catchRing.IsValid() )
		{
			catchRing.HeadTint = tint;
			catchRing.TailTint = tint;
			catchRing.Apply();
			catchRing.SetPoints( BuildRing( slot ) );
		}

		if ( chamberedMarker.IsValid() )
		{
			chamberedMarker.Enabled = ready;
			chamberedMarker.WorldPosition = Arena.Geometry.ToPlayWorld( Runner.Flat ) + Vector3.Up * 140f;

			var renderer = chamberedMarker.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() && slot is not null )
				renderer.Tint = slot.Tint;
		}
	}

	List<Vector3> BuildRing( RoundSlot slot )
	{
		const int segments = 24;
		var bonus = Loadout.CatchBonus;
		var radius = CatchRadius + bonus;
		var center = CatchPoint;
		var points = new List<Vector3>( segments + 1 );

		for ( var i = 0; i <= segments; i++ )
		{
			var angle = MathF.Tau * i / segments;
			var offset = ArenaGeometry.FromAngle( angle ) * radius;
			points.Add( Arena.Geometry.ToWorld( center + offset, 12f ) );
		}

		return points;
	}
}
