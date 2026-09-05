namespace LoopedLoaded;

public readonly struct WallSegment
{
	public readonly Vector2 A;
	public readonly Vector2 B;
	public readonly Vector2 Facing;
	public readonly WallKind Kind;

	public WallSegment( Vector2 a, Vector2 b, Vector2 facing, WallKind kind )
	{
		A = a;
		B = b;
		Facing = facing;
		Kind = kind;
	}

	public Vector2 Delta => B - A;
	public Vector2 Center => (A + B) * 0.5f;
	public float Length => Delta.Length;
	public Vector2 Direction => Delta.Normal;
	public Vector2 Normal => new Vector2( -Direction.y, Direction.x );

	public float SignedDistance( Vector2 point ) => ArenaGeometry.Dot( point - A, Facing );
}

public enum WallKind
{
	Boundary,
	Core,
	Panel,
	Boss
}
