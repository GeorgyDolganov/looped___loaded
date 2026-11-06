namespace LoopedLoaded;

public sealed class PolyLine : Component
{
	[Property] public Color HeadTint { get; set; } = Color.White;
	[Property] public Color TailTint { get; set; } = Color.White;
	[Property] public float HeadWidth { get; set; } = 9f;
	[Property] public float TailWidth { get; set; } = 1f;
	[Property] public bool HardCaps { get; set; }
	[Property] public bool Solid { get; set; }

	readonly List<Vector3> points = new();
	LineRenderer line;
	bool styled;
	Color styledHead;
	Color styledTail;
	float styledHeadWidth;
	float styledTailWidth;
	bool styledHard;
	bool styledSolid;

	public void Apply()
	{
		if ( !line.IsValid() )
			return;

		if ( styled
			&& styledHard == HardCaps
			&& styledSolid == Solid
			&& styledHeadWidth == HeadWidth
			&& styledTailWidth == TailWidth
			&& Same( styledHead, HeadTint )
			&& Same( styledTail, TailTint ) )
			return;

		styled = true;
		styledHard = HardCaps;
		styledSolid = Solid;
		styledHeadWidth = HeadWidth;
		styledTailWidth = TailWidth;
		styledHead = HeadTint;
		styledTail = TailTint;

		var cap = HardCaps ? SceneLineObject.CapStyle.None : SceneLineObject.CapStyle.Rounded;
		line.StartCap = cap;
		line.EndCap = cap;
		line.Opaque = Solid;
		line.CastShadows = false;

		line.Color = new Gradient(
			new Gradient.ColorFrame( 0f, TailTint ),
			new Gradient.ColorFrame( 1f, HeadTint ) );

		line.Width = new Curve(
			new Curve.Frame( 0f, TailWidth ),
			new Curve.Frame( 1f, HeadWidth ) );
	}

	static bool Same( Color a, Color b ) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

	public void SetPoints( List<Vector3> source )
	{
		EnsureLine();

		if ( source is null || source.Count < 2 )
		{
			line.Enabled = false;
			return;
		}

		points.Clear();
		points.AddRange( source );
		line.VectorPoints = points;
		line.Enabled = true;
	}

	public void Clear()
	{
		if ( line.IsValid() )
			line.Enabled = false;
	}

	void EnsureLine()
	{
		if ( line.IsValid() )
			return;

		line = GameObject.AddComponent<LineRenderer>();
		line.Lighting = false;
		line.Opaque = false;
		line.CastShadows = false;
		line.EndCap = SceneLineObject.CapStyle.Rounded;
		line.StartCap = SceneLineObject.CapStyle.Rounded;
		line.UseVectorPoints = true;
		line.VectorPoints = points;

		Apply();
	}

	protected override void OnEnabled() => EnsureLine();
}
