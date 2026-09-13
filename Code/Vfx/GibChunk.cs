namespace LoopedLoaded;

public sealed class GibChunk : Component
{
	static readonly Color Meat = new Color( 0.74f, 0.09f, 0.07f );
	static readonly Color Clot = new Color( 0.42f, 0.04f, 0.05f );

	static readonly (string Name, GibPart Kind)[] BodyParts =
	{
		("head", GibPart.Head),
		("neck", GibPart.Small),
		("spine_2", GibPart.Chest),
		("spine_1", GibPart.Chest),
		("spine", GibPart.Chest),
		("hips", GibPart.Hips),
		("shoulder_L", GibPart.Limb),
		("shoulder_R", GibPart.Limb),
		("upper_arm_L", GibPart.Limb),
		("upper_arm_R", GibPart.Limb),
		("arm_L", GibPart.Limb),
		("arm_R", GibPart.Limb),
		("forearm_L", GibPart.Limb),
		("forearm_R", GibPart.Limb),
		("hand_ik_L", GibPart.Small),
		("hand_ik_R", GibPart.Small),
		("fist_L", GibPart.Small),
		("fist_R", GibPart.Small),
		("hand_L", GibPart.Small),
		("hand_R", GibPart.Small),
		("thigh_L", GibPart.Limb),
		("thigh_R", GibPart.Limb),
		("shin_L", GibPart.Limb),
		("shin_R", GibPart.Limb),
		("calf_L", GibPart.Limb),
		("calf_R", GibPart.Limb),
		("foot_L", GibPart.Small),
		("foot_R", GibPart.Small)
	};

	enum GibPart
	{
		Head,
		Chest,
		Hips,
		Limb,
		Small,
		Drop,
		Shard
	}

	GameLoop loop;
	Vector3 velocity;
	Vector3 spin;
	Color tint;
	float life;
	float ground = 8f;
	ModelRenderer mesh;
	Material flesh;

	static Material hoboMat;

	static Material HoboMat
	{
		get
		{
			if ( hoboMat.IsValid() )
				return hoboMat;

			hoboMat = Material.Load( "materials/hobo/hobo.vmat" );
			return hoboMat.IsValid() ? hoboMat : Blocks.Flat;
		}
	}

	public static void Burst( GameLoop host, Scene scene, SkinnedModelRenderer skin, Vector3 origin, Vector2 impulse, Color mark, float radius, GameObject shield )
	{
		if ( !scene.IsValid() )
			return;

		var scale = Math.Clamp( radius / 64f, 0.85f, 3.6f );
		var meat = Color.Lerp( Meat, mark, 0.32f );

		if ( skin.IsValid() )
		{
			foreach ( var entry in BodyParts )
			{
				if ( !BoneWorld( skin, entry.Name, out var tx ) )
					continue;

				Throw( host, scene, tx.Position, tx.Rotation, impulse, origin, meat, entry.Kind, scale, true );
			}
		}

		var extras = scale > 1.6f ? 10 : 5;
		for ( var i = 0; i < extras; i++ )
		{
			var offset = ArenaGeometry.FromAngle( Game.Random.Float( 0f, MathF.Tau ) ) * Game.Random.Float( 12f, 38f * scale );
			var at = origin + new Vector3( offset.x, offset.y, Game.Random.Float( 24f, 90f * scale ) );
			var kind = i % 3 == 0 ? GibPart.Chest : GibPart.Limb;
			Throw( host, scene, at, Toss(), impulse, origin, meat, kind, scale, true );
		}

		var drops = scale > 1.6f ? 18 : 11;
		for ( var i = 0; i < drops; i++ )
		{
			var offset = ArenaGeometry.FromAngle( Game.Random.Float( 0f, MathF.Tau ) ) * Game.Random.Float( 6f, 28f * scale );
			var at = origin + new Vector3( offset.x, offset.y, Game.Random.Float( 18f, 70f * scale ) );
			Throw( host, scene, at, Toss(), impulse, origin, Color.Lerp( Meat, Clot, Game.Random.Float( 0f, 1f ) ), GibPart.Drop, scale, false );
		}

		if ( !shield.IsValid() )
			return;

		for ( var i = 0; i < 5; i++ )
			Throw( host, scene, shield.WorldPosition, shield.WorldRotation, impulse, origin, mark * 1.15f, GibPart.Shard, scale, false );
	}

	static void Throw( GameLoop host, Scene scene, Vector3 at, Rotation rotation, Vector2 impulse, Vector3 origin, Color tint, GibPart kind, float scale, bool flesh )
	{
		var go = scene.CreateObject();
		go.Name = "Gib";
		go.WorldPosition = at;
		go.WorldRotation = rotation;

		var chunk = go.AddComponent<GibChunk>();
		chunk.Arm( host, at, rotation, impulse, origin, tint, kind, scale, flesh );
		if ( host.IsValid() )
			host.Gibs.Add( chunk );
	}

	void Arm( GameLoop host, Vector3 at, Rotation rotation, Vector2 impulse, Vector3 origin, Color color, GibPart kind, float scale, bool flesh )
	{
		loop = host;
		tint = color;
		WorldPosition = at;
		WorldRotation = rotation;
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

		this.flesh = flesh ? HoboMat : Blocks.Flat;
		Build( kind, scale );
	}

	void Build( GibPart kind, float scale )
	{
		var size = kind switch
		{
			GibPart.Head => new Vector3( 19f, 19f, 19f ),
			GibPart.Chest => new Vector3( 23f, 15f, 19f ),
			GibPart.Hips => new Vector3( 20f, 14f, 12f ),
			GibPart.Limb => new Vector3( 24f, 7.5f, 7.5f ),
			GibPart.Small => new Vector3( 9f, 7f, 7f ),
			GibPart.Shard => new Vector3( 11f, 24f, 4f ),
			_ => new Vector3( 5f, 5f, 5f )
		} * scale;

		GameObject piece;
		if ( kind is GibPart.Head or GibPart.Drop )
			piece = Blocks.SpawnSphere( GameObject, "Mesh", WorldPosition, size.x, tint, false );
		else
			piece = Blocks.SpawnBox( GameObject, "Mesh", WorldPosition, Rotation.Identity, size, tint, false );

		piece.LocalPosition = Vector3.Zero;
		piece.LocalRotation = Rotation.Identity;
		mesh = piece.GetComponent<ModelRenderer>();
		if ( mesh.IsValid() )
		{
			mesh.MaterialOverride = flesh.IsValid() ? flesh : Blocks.Flat;
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
