namespace LoopedLoaded;

public sealed class PlayerAim : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public RoundInventory Inventory { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float MuzzleOffset { get; set; } = 52f;
	[Property] public float PreviewLength { get; set; } = 1500f;
	[Property] public float PreviewBounceLength { get; set; } = 340f;
	[Property] public float RoundRadius { get; set; } = 13f;

	public Vector2 Direction { get; private set; } = new Vector2( -1f, 0f );
	public Vector2 Cursor { get; private set; }
	public Vector2 Muzzle => Runner.IsValid() ? Runner.Flat + Direction * MuzzleOffset : Direction * MuzzleOffset;
	public Vector3 MuzzleWorld => Arena.IsValid() ? Arena.Geometry.ToPlayWorld( Muzzle ) : Vector3.Zero;

	PolyLine preview;
	GameObject reticle;

	protected override void OnStart()
	{
		var previewObject = Scene.CreateObject();
		previewObject.Name = "Trajectory Preview";
		previewObject.Parent = GameObject;

		preview = previewObject.AddComponent<PolyLine>();
		preview.HeadTint = ShotColors.Player;
		preview.TailTint = ShotColors.Player * 0.35f;
		preview.HeadWidth = 3f;
		preview.TailWidth = 7f;
		preview.Apply();

		reticle = Blocks.SpawnSphere( GameObject, "Reticle", Vector3.Zero, 22f, new Color( 0.4f, 1f, 1f ) );
	}

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && (Loop.InCity || Loop.InMenu) )
		{
			preview?.Clear();
			if ( reticle.IsValid() )
				reticle.Enabled = false;
			return;
		}

		if ( reticle.IsValid() )
			reticle.Enabled = true;

		if ( !Runner.IsValid() || !Arena.IsValid() )
			return;

		UpdateCursor();

		var toCursor = Cursor - Runner.Flat;
		if ( toCursor.Length > 1f )
			Direction = toCursor.Normal;

		WorldRotation = Blocks.FlatFacing( Direction );

		if ( reticle.IsValid() )
			reticle.WorldPosition = Arena.Geometry.ToPlayWorld( Cursor );

		if ( Loop.IsValid() && Loop.IsFrozen )
			return;

		UpdatePreview();
	}

	void UpdateCursor()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		var ray = camera.ScreenPixelToRay( Mouse.Position );
		var height = Arena.Geometry.PlayHeight;
		var slope = ray.Forward.z;

		if ( MathF.Abs( slope ) < 0.0001f )
			return;

		var travel = (height - ray.Position.z) / slope;
		if ( travel <= 0f )
			return;

		var point = ray.Position + ray.Forward * travel;
		Cursor = new Vector2( point.x, point.y );
	}

	void UpdatePreview()
	{
		var slot = Inventory.IsValid() ? Inventory.Selected : null;
		var ready = slot is not null && slot.Status == RoundStatus.Chambered;
		var tint = ready ? ShotColors.Player : new Color( 0.4f, 0.5f, 0.6f );

		if ( preview.IsValid() )
		{
			preview.HeadTint = tint;
			preview.TailTint = Color.Lerp( tint, Color.White, 0.35f );
			preview.Apply();
		}

		if ( reticle.IsValid() )
		{
			var renderer = reticle.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				renderer.Tint = tint;
		}

		if ( !ready )
		{
			preview?.Clear();
			return;
		}

		var flat = Arena.Geometry.PredictPath( Muzzle, Direction, RoundRadius, PreviewLength, PreviewBounceLength );
		var world = new List<Vector3>( flat.Count );

		foreach ( var point in flat )
			world.Add( Arena.Geometry.ToPlayWorld( point ) );

		preview?.SetPoints( world );
	}
}
