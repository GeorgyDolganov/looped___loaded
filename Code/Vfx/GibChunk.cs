namespace LoopedLoaded;

public sealed class GibChunk : Component
{
	static readonly Color Meat = new Color( 0.74f, 0.09f, 0.07f );
	static readonly Color Clot = new Color( 0.42f, 0.04f, 0.05f );

	static readonly (string Name, GibPart Kind)[] BodyParts =
	{
		("head", GibPart.Head),
		("spine_2", GibPart.Chest),
		("hips", GibPart.Hips),
		("upper_arm_L", GibPart.Limb),
		("upper_arm_R", GibPart.Limb),
		("forearm_L", GibPart.Limb),
		("forearm_R", GibPart.Limb),
		("thigh_L", GibPart.Limb),
		("thigh_R", GibPart.Limb),
		("shin_L", GibPart.Limb),
		("shin_R", GibPart.Limb)
	};

	enum GibPart
	{
		Head,
		Chest,
		Hips,
		Limb,
		Small,
		Drop
	}

	const int CheapAfter = 12;
	const int CheapParts = 5;

	GameLoop loop;
	Vector3 velocity;
	Vector3 spin;
	Color tint;
	float life;
	float ground = 8f;
	ModelRenderer mesh;

	public static void Burst( GameLoop host, Scene scene, SkinnedModelRenderer skin, Vector3 origin, Vector2 impulse, Color mark, float radius )
	{
		if ( !scene.IsValid() )
			return;

		var alive = host.IsValid() ? host.Gibs.Count : 0;
		var room = FxBudget.GibRoom( alive );
		if ( room <= 0 )
			return;

		var cheap = FxBudget.GibsThisFrame >= CheapAfter;
		var scale = Math.Clamp( radius / 64f, 0.85f, 3.6f );
		var meat = Color.Lerp( Meat, mark, 0.32f );
		var parts = cheap ? CheapParts : BodyParts.Length;

		if ( skin.IsValid() )
		{
			var spawned = 0;
			foreach ( var entry in BodyParts )
			{
				if ( spawned >= parts || room <= 0 )
					break;

				if ( !BoneWorld( skin, entry.Name, out var tx ) )
					continue;

				if ( !Emit( host, scene, ref room, tx.Position, tx.Rotation, impulse, origin, meat, entry.Kind, scale ) )
					break;

				spawned++;
			}
		}

		if ( cheap || room <= 0 )
			return;

		var extras = scale > 1.6f ? 6 : 3;
		for ( var i = 0; i < extras && room > 0; i++ )
		{
			var offset = ArenaGeometry.FromAngle( Game.Random.Float( 0f, MathF.Tau ) ) * Game.Random.Float( 12f, 38f * scale );
			var at = origin + new Vector3( offset.x, offset.y, Game.Random.Float( 24f, 90f * scale ) );
			Emit( host, scene, ref room, at, Toss(), impulse, origin, meat, GibPart.Small, scale );
		}

		var drops = scale > 1.6f ? 18 : 11;
		for ( var i = 0; i < drops && room > 0; i++ )
		{
			var offset = ArenaGeometry.FromAngle( Game.Random.Float( 0f, MathF.Tau ) ) * Game.Random.Float( 6f, 28f * scale );
			var at = origin + new Vector3( offset.x, offset.y, Game.Random.Float( 18f, 70f * scale ) );
			var clot = Color.Lerp( Meat, Clot, Game.Random.Float( 0f, 1f ) );
			Emit( host, scene, ref room, at, Toss(), impulse, origin, clot, GibPart.Drop, scale );
		}
	}

	static bool Emit( GameLoop host, Scene scene, ref int room, Vector3 at, Rotation rotation, Vector2 impulse, Vector3 origin, Color tint, GibPart kind, float scale )
	{
		if ( room <= 0 )
			return false;

		Throw( host, scene, at, rotation, impulse, origin, tint, kind, scale );
		room--;
		FxBudget.NoteGib();
		return true;
	}

	static void Throw( GameLoop host, Scene scene, Vector3 at, Rotation rotation, Vector2 impulse, Vector3 origin, Color tint, GibPart kind, float scale )
	{
		var go = scene.CreateObject();
		go.Name = "Gib";
		go.WorldPosition = at;

		var chunk = go.AddComponent<GibChunk>();
		chunk.Arm( host, at, rotation, impulse, origin, tint, kind, scale );
		if ( host.IsValid() )
			host.Gibs.Add( chunk );
	}

	void Arm( GameLoop host, Vector3 at, Rotation rotation, Vector2 impulse, Vector3 origin, Color color, GibPart kind, float scale )
	{
		loop = host;
		tint = color;
		WorldPosition = at;
		WorldRotation = Rotation.Identity;
		ground = 8f;
		life = kind == GibPart.Drop ? Game.Random.Float( 0.55f, 0.95f ) : Game.Random.Float( 1.15f, 1.85f );
		if ( scale > 1.6f )
			life += 0.45f;

		var away = at - origin;
		var radial = away.Length > 4f
			? away.Normal
			: new Vector3( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ), 0.2f ).Normal;
		radial = (radial + new Vector3( Game.Random.Float( -0.85f, 0.85f ), Game.Random.Float( -0.85f, 0.85f ), Game.Random.Float( -0.25f, 0.55f ) )).Normal;
		var kick = impulse.Length > 0.01f
			? new Vector3( impulse.x, impulse.y, 0f ) * Game.Random.Float( 220f, 480f )
			: Vector3.Zero;
		var burst = kind == GibPart.Drop ? Game.Random.Float( 280f, 620f ) : Game.Random.Float( 180f, 420f );
		velocity = radial * burst * (0.65f + 0.35f * scale) + kick + Vector3.Up * Game.Random.Float( 240f, 560f ) * scale;
		spin = new Vector3(
			Game.Random.Float( -12f, 12f ),
			Game.Random.Float( -12f, 12f ),
			Game.Random.Float( -16f, 16f ) );

		Build( kind, scale );
		WorldRotation = rotation;
	}

	void Build( GibPart kind, float scale )
	{
		var size = kind switch
		{
			GibPart.Head => new Vector3( 18f, 18f, 18f ),
			GibPart.Chest => new Vector3( 22f, 10f, 14f ),
			GibPart.Hips => new Vector3( 18f, 11f, 9f ),
			GibPart.Limb => new Vector3( 28f, 6f, 6f ),
			GibPart.Small => new Vector3( 8f, 8f, 8f ),
			_ => new Vector3( 5f, 5f, 5f )
		} * scale;

		var ball = kind is GibPart.Head or GibPart.Drop or GibPart.Small;
		var piece = ball
			? Blocks.SpawnSphere( GameObject, "Mesh", WorldPosition, size.x, tint, false )
			: Blocks.SpawnBox( GameObject, "Mesh", WorldPosition, Rotation.Identity, size, tint, false );

		piece.LocalPosition = Vector3.Zero;
		piece.LocalRotation = Rotation.Identity;
		piece.LocalScale = Blocks.Fit( ball ? Blocks.Sphere : Blocks.Box, ball ? new Vector3( size.x, size.x, size.x ) : size );
		mesh = piece.GetComponent<ModelRenderer>();
		if ( mesh.IsValid() )
		{
			mesh.MaterialOverride = Blocks.Flat;
			mesh.Tint = tint;
		}
	}

	protected override void OnUpdate()
	{
		if ( loop.IsValid() && loop.Paused )
			return;

		var dt = Time.Delta;
		life -= dt;
		if ( life <= 0f )
		{
			GameObject.Destroy();
			return;
		}

		velocity += Vector3.Down * 1680f * dt;
		var next = WorldPosition + velocity * dt;
		if ( next.z < ground )
		{
			next.z = ground;
			if ( velocity.z < 0f )
				velocity = new Vector3( velocity.x * 0.62f, velocity.y * 0.62f, velocity.z * -0.34f );
			if ( MathF.Abs( velocity.z ) < 48f )
				velocity = new Vector3( velocity.x * 0.84f, velocity.y * 0.84f, 0f );
		}

		WorldPosition = next;
		WorldRotation *= Rotation.FromAxis(
			spin.Length > 0.01f ? spin.Normal : Vector3.Up,
			MathX.RadianToDegree( spin.Length * dt ) );

		var fade = life < 0.4f ? Math.Clamp( life / 0.4f, 0f, 1f ) : 1f;
		if ( mesh.IsValid() )
			mesh.Tint = tint.WithAlpha( fade );
	}

	protected override void OnDestroy()
	{
		if ( loop.IsValid() )
			loop.Gibs.Remove( this );
	}

	static Rotation Toss()
		=> Rotation.FromYaw( Game.Random.Float( 0f, 360f ) )
		* Rotation.FromPitch( Game.Random.Float( -80f, 80f ) )
		* Rotation.FromRoll( Game.Random.Float( -180f, 180f ) );

	static bool BoneWorld( SkinnedModelRenderer skin, string name, out Transform tx )
	{
		tx = default;
		if ( !skin.IsValid() )
			return false;

		var bone = skin.Model?.Bones.GetBone( name );
		if ( bone is not null && skin.TryGetBoneTransformAnimation( bone, out tx ) )
			return true;

		return skin.TryGetBoneTransform( name, out tx );
	}
}
