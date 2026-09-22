namespace LoopedLoaded;

public sealed class PlayerAim : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public RoundInventory Inventory { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float MuzzleOffset { get; set; } = 82f;
	[Property] public float PreviewLength { get; set; } = 1500f;
	[Property] public float PreviewBounceLength { get; set; } = 340f;
	[Property] public float RoundRadius { get; set; } = 13f;

	public Vector2 Direction { get; private set; } = new Vector2( -1f, 0f );
	public Vector2 Cursor { get; private set; }
	public Vector2 Muzzle => Runner.IsValid() ? Runner.Flat + Direction * MuzzleOffset : Direction * MuzzleOffset;

	public Vector3 MuzzleWorld => Arena.IsValid() ? Arena.Geometry.ToPlayWorld( Muzzle ) : Vector3.Zero;

	GameObject reticle;
	readonly List<PolyLine> paths = new();
	readonly List<PolyLine> rings = new();

	protected override void OnStart()
	{
		reticle = Blocks.SpawnSphere( GameObject, "Reticle", Vector3.Zero, 22f, new Color( 0.4f, 1f, 1f ) );
	}

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && (Loop.InCity || Loop.InMenu) )
		{
			Hide( paths, 0 );
			Hide( rings, 0 );
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
		var ready = Inventory.IsValid() && Inventory.Ready;
		var tint = ready ? ShotColors.Player : new Color( 0.4f, 0.5f, 0.6f );

		if ( reticle.IsValid() )
		{
			var renderer = reticle.GetComponent<ModelRenderer>();
			if ( renderer.IsValid() )
				renderer.Tint = tint;
		}

		if ( !Inventory.IsValid() )
		{
			Hide( paths, 0 );
			Hide( rings, 0 );
			return;
		}

		var recipe = Inventory.Loadout.Recipe();
		var count = Math.Max( 1, recipe.Count );
		var reach = recipe.PointAim ? (Cursor - Muzzle).Length : 0f;
		var bounces = Math.Max( 0, recipe.Bounces );
		var body = recipe.Radius > 1f ? recipe.Radius : RoundRadius;
		var bolt = new Color( 0.55f, 0.78f, 1f );

		for ( var i = 0; i < count; i++ )
		{
			var heading = ShotSpread.Turn( Direction, ShotSpread.Yaw( i, count, recipe.Cone ) );
			List<Vector2> flat;
			if ( recipe.Beam )
			{
				var range = recipe.BeamRange;
				if ( recipe.PointAim )
					range = MathF.Min( range, reach );
				flat = new List<Vector2> { Muzzle, Clip( Muzzle, heading, range ) };
			}
			else if ( recipe.PointAim )
			{
				flat = new List<Vector2> { Muzzle, Clip( Muzzle, heading, reach ) };
			}
			else
			{
				flat = Arena.Geometry.PredictPath( Muzzle, heading, body, PreviewLength, PreviewBounceLength, bounces );
			}

			PaintPath( Take( paths, i, "Aim Path" ), flat, recipe.Beam ? bolt : tint, recipe.Beam ? 4f : 3f );
			if ( recipe.Splash > 1f && flat.Count > 0 )
				PaintRing( Take( rings, i, "Aim Splash" ), flat[^1], recipe.Splash, recipe.FriendlySplash );
		}

		Hide( paths, count );
		Hide( rings, recipe.Splash > 1f ? count : 0 );
	}

	Vector2 Clip( Vector2 origin, Vector2 dir, float range )
	{
		if ( range <= 1f || dir.Length <= 0.01f )
			return origin;

		var heading = dir.Normal;
		if ( Arena.Geometry.TraceRay( origin, heading, range, out var hit ) )
			return hit.Position;

		return origin + heading * range;
	}

	PolyLine Take( List<PolyLine> list, int index, string name )
	{
		while ( list.Count <= index )
		{
			var go = Scene.CreateObject();
			go.Name = name;
			go.Parent = GameObject;
			list.Add( go.AddComponent<PolyLine>() );
		}

		return list[index];
	}

	static void Hide( List<PolyLine> list, int from )
	{
		for ( var i = from; i < list.Count; i++ )
			list[i]?.Clear();
	}

	void PaintPath( PolyLine line, List<Vector2> flat, Color tint, float width )
	{
		if ( !line.IsValid() || flat is null || flat.Count < 2 )
		{
			line?.Clear();
			return;
		}

		var world = new List<Vector3>( flat.Count );
		foreach ( var point in flat )
			world.Add( Arena.Geometry.ToPlayWorld( point ) );

		line.HeadTint = tint;
		line.TailTint = tint * 0.35f;
		line.HeadWidth = width;
		line.TailWidth = width;
		line.Apply();
		line.SetPoints( world );
	}

	void PaintRing( PolyLine line, Vector2 flat, float radius, bool friendly )
	{
		if ( !line.IsValid() || radius <= 1f )
		{
			line?.Clear();
			return;
		}

		var tint = RoundCombat.RingTint( friendly );
		line.HeadTint = tint;
		line.TailTint = tint * 0.35f;
		line.HeadWidth = 3.5f;
		line.TailWidth = 3.5f;
		line.Apply();
		line.SetPoints( RoundCombat.Circle( Arena.Geometry, flat, radius ) );
	}
}
