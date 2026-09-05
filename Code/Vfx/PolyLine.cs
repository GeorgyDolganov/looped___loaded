namespace LoopedLoaded;

public sealed class PolyLine : Component
{
	[Property] public Color HeadTint { get; set; } = Color.White;
	[Property] public Color TailTint { get; set; } = Color.White;
	[Property] public float HeadWidth { get; set; } = 9f;
	[Property] public float TailWidth { get; set; } = 1f;

	readonly List<GameObject> nodes = new();
	LineRenderer line;

	public void Apply()
	{
		if ( !line.IsValid() )
			return;

		line.Color = new Gradient(
			new Gradient.ColorFrame( 0f, TailTint ),
			new Gradient.ColorFrame( 1f, HeadTint ) );

		line.Width = new Curve(
			new Curve.Frame( 0f, TailWidth ),
			new Curve.Frame( 1f, HeadWidth ) );
	}

	public void SetPoints( List<Vector3> points )
	{
		EnsureLine();

		if ( points is null || points.Count < 2 )
		{
			line.Enabled = false;
			return;
		}

		while ( nodes.Count < points.Count )
		{
			var node = Scene.CreateObject();
			node.Name = $"Node {nodes.Count}";
			node.Parent = GameObject;
			nodes.Add( node );
		}

		while ( nodes.Count > points.Count )
		{
			var last = nodes[^1];
			nodes.RemoveAt( nodes.Count - 1 );
			last.Destroy();
		}

		for ( var i = 0; i < points.Count; i++ )
			nodes[i].WorldPosition = points[i];

		line.Points = nodes;
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
		line.EndCap = SceneLineObject.CapStyle.Rounded;
		line.StartCap = SceneLineObject.CapStyle.Rounded;
		line.Points = nodes;

		Apply();
	}

	protected override void OnEnabled() => EnsureLine();
}
