namespace LoopedLoaded;

public sealed class CityInject : Component
{
	public const string ModelPath = "models/syrenge.vmdl";

	const float TargetLength = 156f;
	const float FallbackLife = 1.21f;
	const string SequenceName = "inject";

	public CityBoard Board { get; set; }

	float life = FallbackLife;
	float age;
	bool armed;

	public static void Spawn( CityBoard board, GameObject parent, CityPlot plot )
	{
		if ( !board.IsValid() || plot is null )
			return;

		var go = board.Scene.CreateObject();
		go.Name = "Syringe";
		if ( parent.IsValid() )
			go.Parent = parent;

		var inject = go.AddComponent<CityInject>();
		inject.Board = board;
		inject.Place( plot );
	}

	public void Place( CityPlot plot )
	{
		var model = Model.Load( ModelPath );
		var mesh = PickMesh( plot );
		var point = mesh.IsValid()
			? TopPoint( mesh )
			: plot.Root.IsValid()
				? plot.Root.WorldPosition + Vector3.Up * 80f
				: WorldPosition;

		var yaw = plot.Facing * 90f + Game.Random.Float( -25f, 25f );
		var tilt = Game.Random.Float( 10f, 30f );
		var lean = Rotation.FromYaw( yaw ) * new Vector3( 0f, 1f, 0f );
		var into = (Rotation.FromAxis( lean, tilt ) * Vector3.Down).Normal;

		Needle( model, out var needle, out var tip );
		var rotation = Rotation.FromAxis( into, yaw ) * FromTo( needle, into );
		var size = model.Bounds.Size;
		var length = MathF.Max( size.x, MathF.Max( size.y, size.z ) );
		var scale = length > 0.001f ? TargetLength / length : 1f;

		WorldRotation = rotation;
		WorldScale = new Vector3( scale, scale, scale );
		WorldPosition = point - rotation * new Vector3( tip.x * scale, tip.y * scale, tip.z * scale );

		var skin = GameObject.GetComponent<SkinnedModelRenderer>() ?? GameObject.AddComponent<SkinnedModelRenderer>();
		skin.Model = model;
		skin.UseAnimGraph = false;
		skin.RenderType = ModelRenderer.ShadowRenderType.On;
		Arm( skin );
	}

	protected override void OnStart()
	{
		var skin = GameObject.GetComponent<SkinnedModelRenderer>();
		if ( skin.IsValid() )
			Arm( skin );
	}

	protected override void OnUpdate()
	{
		if ( !Board.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		if ( Board.Loop.IsValid() && Board.Loop.Paused )
			return;

		age += Time.Delta;
		if ( age >= life )
			GameObject.Destroy();
	}

	void Arm( SkinnedModelRenderer skin )
	{
		if ( !skin.IsValid() || !skin.Model.IsValid() )
			return;

		if ( !armed )
		{
			skin.UseAnimGraph = false;
			skin.Sequence.Name = SequenceName;
			skin.Sequence.Looping = false;
			armed = true;
			age = 0f;
		}

		var duration = skin.Sequence.Duration;
		if ( duration > 0.05f )
			life = duration;
	}

	static ModelRenderer PickMesh( CityPlot plot )
	{
		if ( plot.Body is null || !plot.Body.IsValid() )
			return null;

		var found = new List<ModelRenderer>();
		foreach ( var renderer in plot.Body.GetComponentsInChildren<ModelRenderer>( true ) )
		{
			if ( !renderer.IsValid() || !renderer.Model.IsValid() )
				continue;

			var name = renderer.GameObject.Name;
			if ( name is "Heart" or "Muscle" or "Adrenal" or "Lung L" or "Lung R" or "Liver" )
				found.Add( renderer );
		}

		if ( found.Count == 0 )
			return null;

		return found[Game.Random.Int( 0, found.Count - 1 )];
	}

	static Vector3 TopPoint( ModelRenderer renderer )
	{
		var go = renderer.GameObject;
		var local = renderer.Model.Bounds;
		var rotation = go.WorldRotation;
		var ax = rotation * new Vector3( 1f, 0f, 0f );
		var ay = rotation * new Vector3( 0f, 1f, 0f );
		var az = rotation * new Vector3( 0f, 0f, 1f );
		var zx = MathF.Abs( ax.z );
		var zy = MathF.Abs( ay.z );
		var zz = MathF.Abs( az.z );
		var u = Game.Random.Float( 0.18f, 0.82f );
		var v = Game.Random.Float( 0.18f, 0.82f );
		var x = Lerp( local.Mins.x, local.Maxs.x, u );
		var y = Lerp( local.Mins.y, local.Maxs.y, v );
		var z = Lerp( local.Mins.z, local.Maxs.z, u );

		if ( zz >= zx && zz >= zy )
			z = az.z >= 0f ? local.Maxs.z : local.Mins.z;
		else if ( zy >= zx )
		{
			y = ay.z >= 0f ? local.Maxs.y : local.Mins.y;
			z = Lerp( local.Mins.z, local.Maxs.z, v );
		}
		else
		{
			x = ax.z >= 0f ? local.Maxs.x : local.Mins.x;
			y = Lerp( local.Mins.y, local.Maxs.y, v );
			z = Lerp( local.Mins.z, local.Maxs.z, u );
		}

		var scale = go.WorldScale;
		var scaled = new Vector3( x * scale.x, y * scale.y, z * scale.z );
		return go.WorldPosition + rotation * scaled;
	}

	static void Needle( Model model, out Vector3 direction, out Vector3 tip )
	{
		var bounds = model.Bounds;
		var size = bounds.Size;
		var axis = 0;
		var length = size.x;
		if ( size.y > length )
		{
			axis = 1;
			length = size.y;
		}

		if ( size.z > length )
			axis = 2;

		var min = axis == 0 ? bounds.Mins.x : axis == 1 ? bounds.Mins.y : bounds.Mins.z;
		var max = axis == 0 ? bounds.Maxs.x : axis == 1 ? bounds.Maxs.y : bounds.Maxs.z;
		var towardMin = MathF.Abs( min ) <= MathF.Abs( max );
		var unit = axis == 0 ? new Vector3( 1f, 0f, 0f ) : axis == 1 ? new Vector3( 0f, 1f, 0f ) : new Vector3( 0f, 0f, 1f );
		direction = towardMin ? -unit : unit;
		var tipValue = towardMin ? min : max;
		tip = axis == 0
			? new Vector3( tipValue, 0f, 0f )
			: axis == 1
				? new Vector3( 0f, tipValue, 0f )
				: new Vector3( 0f, 0f, tipValue );
	}

	static Rotation FromTo( Vector3 from, Vector3 to )
	{
		from = from.Normal;
		to = to.Normal;
		var dot = Math.Clamp( Vector3.Dot( from, to ), -1f, 1f );
		if ( dot > 0.9999f )
			return Rotation.Identity;

		var axis = Cross( from, to );
		if ( dot < -0.9999f || axis.Length < 0.0001f )
			axis = MathF.Abs( from.z ) < 0.9f ? Cross( from, Vector3.Up ) : Cross( from, new Vector3( 0f, 1f, 0f ) );

		var angle = MathX.RadianToDegree( MathF.Acos( dot ) );
		return Rotation.FromAxis( axis.Normal, angle );
	}

	static Vector3 Cross( Vector3 a, Vector3 b ) => new Vector3(
		a.y * b.z - a.z * b.y,
		a.z * b.x - a.x * b.z,
		a.x * b.y - a.y * b.x );

	static float Lerp( float min, float max, float t ) => min + (max - min) * t;
}
