namespace LoopedLoaded;

public sealed class BonePickup : Component
{
	public static readonly Color Tint = new Color( 0.93f, 0.84f, 0.62f );

	public int Value { get; private set; }
	public Vector2 Flat { get; private set; }

	const float CollectRadius = 58f;

	GameLoop loop;
	Vector2 velocity;
	float hop;
	float hopVel;
	float spin;
	float spinRate;
	float scatterUntil;
	float speed;
	bool taken;

	public static void Spill( GameLoop host, Vector2 origin, int scrap )
	{
		if ( !host.IsValid() || scrap <= 0 )
			return;

		var room = FxBudget.BoneRoom( host.Bones.Count );
		var count = Math.Min( Math.Clamp( scrap, 1, 7 ), room );
		if ( count <= 0 )
		{
			host.KeepScrap( scrap );
			return;
		}

		var share = scrap / count;
		var rest = scrap % count;

		for ( var i = 0; i < count; i++ )
		{
			var go = host.Scene.CreateObject();
			go.Name = "Bone";

			var bone = go.AddComponent<BonePickup>();
			bone.Arm( host, origin, share + (i < rest ? 1 : 0), i, count );
			host.Bones.Add( bone );
			FxBudget.NoteBone();
		}
	}

	void Arm( GameLoop host, Vector2 origin, int value, int index, int count )
	{
		loop = host;
		Value = Math.Max( 1, value );
		var angle = MathF.Tau * index / Math.Max( 1, count ) + Game.Random.Float( -0.4f, 0.4f );
		var burst = Game.Random.Float( 240f, 430f );
		velocity = ArenaGeometry.FromAngle( angle ) * burst;
		speed = burst;
		Flat = origin;
		hop = Game.Random.Float( 16f, 34f );
		hopVel = Game.Random.Float( 220f, 360f );
		spinRate = Game.Random.Float( -11f, 11f );
		scatterUntil = Time.Now + Game.Random.Float( 0.16f, 0.28f );
		WorldPosition = host.Geometry.ToPlayWorld( origin ) + Vector3.Up * hop;
		Build();
	}

	public void ShiftTime( float dt )
	{
		scatterUntil += dt;
	}

	public int Harvest()
	{
		if ( taken )
			return 0;

		taken = true;
		var value = Value;
		GameObject.Destroy();
		return value;
	}

	void Build()
	{
		var shaft = Blocks.SpawnBox( GameObject, "Shaft", WorldPosition, Rotation.Identity, new Vector3( 26f, 7f, 7f ), Tint, false );
		shaft.LocalPosition = Vector3.Zero;
		shaft.LocalRotation = Rotation.Identity;

		var headA = Blocks.SpawnBox( GameObject, "Head A", WorldPosition, Rotation.Identity, new Vector3( 11f, 12f, 9f ), Tint, false );
		headA.LocalPosition = Vector3.Forward * 11f;
		headA.LocalRotation = Rotation.Identity;

		var headB = Blocks.SpawnBox( GameObject, "Head B", WorldPosition, Rotation.Identity, new Vector3( 11f, 12f, 9f ), Tint, false );
		headB.LocalPosition = Vector3.Backward * 11f;
		headB.LocalRotation = Rotation.Identity;
	}

	protected override void OnUpdate()
	{
		if ( taken )
			return;

		if ( !loop.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		if ( loop.IsFrozen )
			return;

		var dt = Time.Delta;
		hopVel -= 980f * dt;
		hop += hopVel * dt;
		if ( hop < 0f )
		{
			hop = 0f;
			if ( hopVel < 0f )
				hopVel *= -0.35f;
			if ( MathF.Abs( hopVel ) < 40f )
				hopVel = 0f;
		}

		if ( loop.Runner.IsValid() )
		{
			var to = loop.Runner.Flat - Flat;
			var dist = to.Length;
			var reach = Time.Now < scatterUntil ? CollectRadius : CollectRadius + speed * dt;
			if ( dist <= reach )
			{
				Catch();
				return;
			}

			if ( Time.Now < scatterUntil )
				velocity *= MathF.Exp( -3.4f * dt );
			else
			{
				hop = MathX.Lerp( hop, 18f, 1f - MathF.Exp( -8f * dt ) );
				speed = MathF.Min( 1650f, speed + 1450f * dt );
				if ( dist > 0.01f )
					velocity = to / dist * speed;
			}
		}
		else if ( Time.Now < scatterUntil )
			velocity *= MathF.Exp( -3.4f * dt );

		Flat += velocity * dt;
		spin += spinRate * dt;

		var height = loop.Geometry is not null ? loop.Geometry.PlayHeight : 40f;
		var look = velocity.Length > 8f ? velocity : Vector2.Right;
		WorldPosition = new Vector3( Flat.x, Flat.y, height + hop + 10f );
		WorldRotation = Blocks.FlatFacing( look ) * Rotation.FromRoll( spin * 42f );
	}

	void Catch()
	{
		if ( taken )
			return;

		var world = WorldPosition;
		var value = Harvest();
		if ( value > 0 )
			loop.CollectBone( value, world );
	}

	protected override void OnDestroy()
	{
		if ( loop.IsValid() )
			loop.Bones.Remove( this );
	}
}
