namespace LoopedLoaded;

public sealed class ArenaWall : Component
{
	[Property] public WallKind Kind { get; set; } = WallKind.Boundary;
	[Property] public bool FlipFacing { get; set; }

	public WallSegment ToSegment()
	{
		var center = new Vector2( WorldPosition.x, WorldPosition.y );
		var forward = WorldRotation.Forward;
		var dir = new Vector2( forward.x, forward.y );
		if ( dir.Length < 0.001f )
			dir = Vector2.Right;
		dir = dir.Normal;

		var bounds = Blocks.Box.Bounds.Size;
		var length = MathF.Abs( WorldScale.x ) * (bounds.x > 0.001f ? bounds.x : 50f);
		var thick = MathF.Abs( WorldScale.y ) * (bounds.y > 0.001f ? bounds.y : 50f);
		var span = MathF.Max( 1f, length - thick );
		var a = center - dir * span * 0.5f;
		var b = center + dir * span * 0.5f;

		var normal = new Vector2( -dir.y, dir.x );
		var facing = normal;

		if ( Kind == WallKind.Core )
			facing = ArenaGeometry.Dot( normal, center ) >= 0f ? normal : -normal;
		else if ( Kind == WallKind.Boundary )
			facing = ArenaGeometry.Dot( normal, -center ) >= 0f ? normal : -normal;

		if ( FlipFacing )
			facing = -facing;

		return new WallSegment( a, b, facing, Kind );
	}
}
