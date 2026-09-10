namespace LoopedLoaded;

public sealed class RingRunner : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float Speed { get; set; } = 330f;
	[Property] public float DashDistance { get; set; } = 430f;
	[Property] public float DashCooldown { get; set; } = 1.1f;
	[Property] public float DashDuration { get; set; } = 0.17f;
	[Property] public float SlowSpeedScale { get; set; } = 0.38f;
	[Property] public float SlowDrain { get; set; } = 0.55f;
	[Property] public float SlowRegen { get; set; } = 0.28f;
	[Property] public float PlayerRadius { get; set; } = 48f;

	public float Angle { get; private set; }
	public float TravelledArc { get; private set; }
	public int Lap => 1 + (int)(TravelledArc / (MathF.Tau * Radius));
	public float LapFraction => (TravelledArc / (MathF.Tau * Radius)) % 1f;
	public bool Dashing => dashElapsed < DashDuration;
	public float DashCharge => DashCooldown <= 0f ? 1f : MathF.Min( 1f, (Time.Now - lastDash) / DashCooldown );
	public bool SlowUnlocked { get; set; }
	public float SlowCharge { get; private set; } = 1f;
	public bool Slowing { get; private set; }

	public float Radius => Arena.IsValid() ? Arena.Geometry.TrackRadius : 1000f;
	public Vector2 Flat => ArenaGeometry.FromAngle( Angle ) * Radius;
	public Vector2 Tangent => new Vector2( MathF.Sin( Angle ), -MathF.Cos( Angle ) );

	float dashElapsed = 999f;
	float lastDash = -999f;
	float dashSpent;
	bool slowOverheat;
	SkinnedModelRenderer warlord;
	readonly List<(ModelRenderer Renderer, Color Tint)> meshes = new();

	public void ResetToStart( float startAngle )
	{
		Angle = startAngle;
		TravelledArc = 0f;
		dashElapsed = 999f;
		dashSpent = 0f;
		lastDash = -999f;
		SlowUnlocked = false;
		SlowCharge = 1f;
		Slowing = false;
		slowOverheat = false;
		DashCooldown = 1.1f;
		SlowDrain = 0.55f;
		Speed = 330f;
		ApplyTransform();
	}

	public void ApplyCity( CityStats stats )
	{
		DashCooldown = 1.1f * stats.DashCooldownScale;
		SlowUnlocked = stats.SlowUnlocked;
		SlowDrain = stats.SlowDrain;
	}

	public void ApplyPace( int lap )
	{
		Speed = 330f * Progression.Pace( lap );
	}

	public bool TryDash()
	{
		if ( DashCharge < 1f )
			return false;

		lastDash = Time.Now;
		dashElapsed = 0f;
		dashSpent = 0f;
		return true;
	}

	public void FinishCurrentLap()
	{
		var lapLength = MathF.Tau * Radius;
		if ( lapLength <= 0.001f )
			return;

		var into = TravelledArc % lapLength;
		if ( into < 0f )
			into += lapLength;

		TravelledArc += into < 0.001f ? lapLength : lapLength - into;
		Angle = Arena.IsValid() ? Arena.StartAngle : MathF.PI * 0.5f;
		dashElapsed = 999f;
		dashSpent = 0f;
		Slowing = false;
		ApplyTransform();
	}

	public void ShiftTime( float dt )
	{
		lastDash += dt;
	}

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && Loop.IsFrozen )
		{
			ApplyTransform();
			EnsureWarlord();
			DriveWarlord();
			PaintHurt();
			return;
		}

		var arc = Speed * Time.Delta;
		TickSlow();

		if ( Slowing )
			arc *= SlowSpeedScale;

		if ( Dashing )
		{
			dashElapsed = MathF.Min( DashDuration, dashElapsed + Time.Delta );
			var eased = 1f - MathF.Pow( 1f - dashElapsed / DashDuration, 3f );
			var target = DashDistance * eased;
			arc += target - dashSpent;
			dashSpent = target;
		}

		Advance( arc );
		ApplyTransform();
		EnsureWarlord();
		DriveWarlord();
		PaintHurt();
	}

	void EnsureWarlord()
	{
		foreach ( var child in GameObject.Children.ToArray() )
		{
			if ( child.Name == "Terry" || child.Name == "Terry Enemy" )
				child.Destroy();
		}

		if ( warlord.IsValid() )
		{
			var size = warlord.Model?.Bounds.Size ?? Vector3.Zero;
			if ( size.Length > 0.01f )
				return;

			warlord.GameObject.Destroy();
			warlord = null;
		}

		warlord = WarlordLook.Attach( GameObject );
	}

	void DriveWarlord()
	{
		if ( !warlord.IsValid() )
			return;

		var frozen = Loop.IsValid() && Loop.IsFrozen;
		var speed = 0f;
		if ( !frozen )
		{
			speed = Speed;
			if ( Slowing )
				speed *= SlowSpeedScale;
			if ( Dashing )
				speed += DashDistance / MathF.Max( 0.05f, DashDuration );
		}

		var vel = new Vector3( Tangent.x, Tangent.y, 0f ) * speed;
		var look = vel;
		var aim = GetComponent<PlayerAim>();
		if ( aim.IsValid() )
			look = new Vector3( aim.Direction.x, aim.Direction.y, 0f );

		WarlordLook.Drive( warlord, vel, look, 0 );
	}

	void PaintHurt()
	{
		var renderers = GameObject.GetComponentsInChildren<ModelRenderer>( true );
		if ( meshes.Count != renderers.Count() )
		{
			meshes.Clear();
			foreach ( var renderer in renderers )
				meshes.Add( (renderer, renderer.Tint) );
		}

		var hurt = Loop.IsValid() ? Loop.HurtAmount : 0f;
		var blink = Loop.IsValid() && Loop.Invulnerable && (Time.Now * 16f % 1f) < 0.5f;

		foreach ( var mesh in meshes )
		{
			if ( !mesh.Renderer.IsValid() )
				continue;

			if ( hurt > 0.01f )
				mesh.Renderer.Tint = Color.Lerp( mesh.Tint, new Color( 1f, 0.12f, 0.08f ), hurt );
			else if ( blink )
				mesh.Renderer.Tint = Color.Lerp( mesh.Tint, new Color( 1f, 0.35f, 0.28f ), 0.7f );
			else
				mesh.Renderer.Tint = mesh.Tint;
		}
	}

	void TickSlow()
	{
		Slowing = false;

		if ( !SlowUnlocked )
			return;

		var holding = Input.Down( "Attack2" );

		if ( slowOverheat )
		{
			SlowCharge = MathF.Min( 1f, SlowCharge + SlowRegen * Time.Delta );
			if ( SlowCharge >= 0.3f && !holding )
				slowOverheat = false;
			return;
		}

		if ( holding && SlowCharge > 0f )
		{
			Slowing = true;
			SlowCharge = MathF.Max( 0f, SlowCharge - SlowDrain * Time.Delta );

			if ( SlowCharge <= 0f )
				slowOverheat = true;

			return;
		}

		SlowCharge = MathF.Min( 1f, SlowCharge + SlowRegen * Time.Delta );
	}

	void Advance( float arc )
	{
		TravelledArc += arc;
		Angle -= arc / Radius;

		if ( Angle < -MathF.Tau )
			Angle += MathF.Tau;
	}

	void ApplyTransform()
	{
		var flat = Flat;
		WorldPosition = new Vector3( flat.x, flat.y, 0f );
	}
}
