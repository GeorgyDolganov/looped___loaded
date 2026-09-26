namespace LoopedLoaded;

public sealed class RingRunner : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float Speed { get; set; } = 255f;
	[Property] public float DashDistance { get; set; } = 280f;
	[Property] public float DashCooldown { get; set; } = 1.1f;
	[Property] public float DashDuration { get; set; } = 0.17f;
	[Property] public float SlowSpeedScale { get; set; } = 0.38f;
	[Property] public float SlowDrain { get; set; } = 0.55f;
	[Property] public float SlowRegen { get; set; } = 0.28f;
	[Property] public float PlayerRadius { get; set; } = 48f;
	[Property] public float ClearSpeedScale { get; set; } = 3f;
	[Property] public float ClearSpeedRamp { get; set; } = 1.2f;

	public float Angle { get; private set; }
	public float TravelledArc { get; private set; }
	public int Lap => 1 + (int)(TravelledArc / (MathF.Tau * Radius));
	public float LapFraction => (TravelledArc / (MathF.Tau * Radius)) % 1f;
	public bool Dashing => dashElapsed < DashDuration;
	public float DashCharge => DashCooldown <= 0f ? 1f : MathF.Min( 1f, (RealTime.Now - lastDash) / DashCooldown );
	public bool SlowUnlocked { get; set; }
	public float SlowCharge { get; private set; } = 1f;
	public bool Slowing { get; private set; }
	public float SpeedScale => 1f + (ClearSpeedScale - 1f) * clearBoost * clearBoost * (3f - 2f * clearBoost);

	public float Radius => Arena.IsValid() ? Arena.Geometry.TrackRadius : 1000f;
	public Vector2 Flat => ArenaGeometry.FromAngle( Angle ) * Radius;
	public Vector2 Tangent => new Vector2( MathF.Sin( Angle ), -MathF.Cos( Angle ) );

	float dashElapsed = 999f;
	float lastDash = -999f;
	float dashSpent;
	bool slowOverheat;
	float clearBoost;
	SkinnedModelRenderer warlord;
	Vector3 warlordVelocity;
	Vector3 warlordLook;
	readonly List<(ModelRenderer Renderer, Color Tint)> meshes = new();

	float startSpeed;
	float startDashCooldown;
	float startSlowDrain;
	bool started;

	protected override void OnAwake()
	{
		CaptureStart();
	}

	void CaptureStart()
	{
		if ( started )
			return;

		started = true;
		startSpeed = Speed;
		startDashCooldown = DashCooldown;
		startSlowDrain = SlowDrain;
	}

	public void ResetToStart( float startAngle )
	{
		CaptureStart();
		Angle = startAngle;
		TravelledArc = 0f;
		dashElapsed = 999f;
		dashSpent = 0f;
		lastDash = -999f;
		SlowUnlocked = false;
		SlowCharge = 1f;
		Slowing = false;
		slowOverheat = false;
		clearBoost = 0f;
		DashCooldown = startDashCooldown;
		SlowDrain = startSlowDrain;
		Speed = startSpeed;
		ApplyTimeScale();
		ApplyTransform();
	}

	public void ResetLap( float startAngle )
	{
		Angle = startAngle;
		TravelledArc = 0f;
		dashElapsed = 999f;
		dashSpent = 0f;
		Slowing = false;
		ApplyTimeScale();
		ApplyTransform();
	}

	public void ApplyCity( CityStats stats )
	{
		CaptureStart();
		DashCooldown = startDashCooldown * stats.DashCooldownScale;
		SlowUnlocked = stats.SlowUnlocked;
		SlowDrain = stats.SlowDrain;
	}

	public void ApplyPace( int lap )
	{
		CaptureStart();
		Speed = startSpeed * Progression.Pace( lap );
	}

	public bool TryDash()
	{
		if ( DashCharge < 1f )
			return false;

		lastDash = RealTime.Now;
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
		ApplyTimeScale();
		ApplyTransform();
	}

	public void ShiftTime( float dt )
	{
		lastDash += dt;
	}

	protected override void OnDisabled()
	{
		Slowing = false;
		ApplyTimeScale();
	}

	protected override void OnDestroy()
	{
		Slowing = false;
		ApplyTimeScale();
	}

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && Loop.IsFrozen )
		{
			if ( !Loop.Paused )
				clearBoost = 0f;

			Slowing = false;
			ApplyTimeScale();
			ApplyTransform();
			EnsureWarlord();
			DriveWarlord();
			PaintHurt();
			return;
		}

		TickClearBoost();
		var play = RealTime.Delta;
		var arc = Speed * SpeedScale * play;
		TickSlow();
		ApplyTimeScale();

		var dashArc = 0f;
		if ( Dashing )
		{
			dashElapsed = MathF.Min( DashDuration, dashElapsed + play );
			var eased = 1f - MathF.Pow( 1f - dashElapsed / DashDuration, 3f );
			var target = DashDistance * eased;
			dashArc = target - dashSpent;
			arc += dashArc;
			dashSpent = target;
		}

		var angleBefore = Angle;
		Advance( arc );
		if ( Loop.IsValid() )
			Loop.Inventory?.AdvanceDropped( angleBefore, arc, dashArc );
		ApplyTransform();
		EnsureWarlord();
		DriveWarlord();
		PaintHurt();
	}

	protected override void OnPreRender()
	{
		if ( !warlord.IsValid() )
			return;

		WarlordLook.Face( warlord, warlordVelocity, warlordLook );
		WarlordLook.ApplyShoot( warlord );
	}

	void EnsureWarlord()
	{
		if ( !warlord.IsValid() )
		{
			foreach ( var child in GameObject.Children )
			{
				if ( child.Name != "Warlord" )
					continue;

				var skin = child.GetComponent<SkinnedModelRenderer>();
				if ( !skin.IsValid() )
					continue;

				warlord = skin;
				break;
			}
		}

		if ( warlord.IsValid() )
		{
			var size = warlord.Model?.Bounds.Size ?? Vector3.Zero;
			if ( size.Length <= 0.01f )
			{
				warlord.GameObject.Destroy();
				warlord = null;
				meshes.Clear();
			}
		}

		if ( warlord.IsValid() )
		{
			foreach ( var child in GameObject.Children.ToArray() )
			{
				if ( child.Name == "Warlord" && child != warlord.GameObject )
					child.Destroy();
			}

			return;
		}

		foreach ( var child in GameObject.Children.ToArray() )
		{
			if ( child.Name == "Terry" || child.Name == "Terry Enemy" || child.Name == "Warlord" )
				child.Destroy();
		}

		warlord = WarlordLook.Attach( GameObject );
		meshes.Clear();
	}

	void DriveWarlord()
	{
		if ( !warlord.IsValid() )
			return;

		var frozen = Loop.IsValid() && Loop.IsFrozen;
		var speed = 0f;
		if ( !frozen )
		{
			speed = Speed * SpeedScale;
			if ( Dashing )
				speed += DashDistance / MathF.Max( 0.05f, DashDuration );
		}

		var vel = new Vector3( Tangent.x, Tangent.y, 0f ) * speed;
		var look = vel;
		var aim = GetComponent<PlayerAim>();
		if ( aim.IsValid() )
			look = new Vector3( aim.Direction.x, aim.Direction.y, 0f );

		warlordVelocity = vel;
		warlordLook = look;
		WarlordLook.Drive( warlord, vel, look );
	}

	public void PlayShoot()
	{
		if ( warlord.IsValid() )
			WarlordLook.PlayShoot( warlord );
	}

	void PaintHurt()
	{
		if ( meshes.Count == 0 && warlord.IsValid() )
		{
			foreach ( var renderer in GameObject.GetComponentsInChildren<ModelRenderer>( true ) )
				meshes.Add( (renderer, renderer.Tint) );
		}

		var hurt = Loop.IsValid() ? Loop.HurtAmount : 0f;
		var blink = Loop.IsValid() && Loop.Invulnerable && (RealTime.Now * 16f % 1f) < 0.5f;

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

	void TickClearBoost()
	{
		var cleared = Loop.IsValid() && Loop.CanSkipLap;
		var step = ClearSpeedRamp <= 0f ? 1f : RealTime.Delta / ClearSpeedRamp;
		clearBoost = cleared ? MathF.Min( 1f, clearBoost + step ) : MathF.Max( 0f, clearBoost - step );
	}

	void TickSlow()
	{
		Slowing = false;

		if ( !SlowUnlocked )
			return;

		var holding = Input.Down( "Attack2" );
		var dt = RealTime.Delta;

		if ( slowOverheat )
		{
			SlowCharge = MathF.Min( 1f, SlowCharge + SlowRegen * dt );
			if ( SlowCharge >= 0.3f && !holding )
				slowOverheat = false;
			return;
		}

		if ( holding && SlowCharge > 0f )
		{
			Slowing = true;
			SlowCharge = MathF.Max( 0f, SlowCharge - SlowDrain * dt );

			if ( SlowCharge <= 0f )
				slowOverheat = true;

			return;
		}

		SlowCharge = MathF.Min( 1f, SlowCharge + SlowRegen * dt );
	}

	void ApplyTimeScale()
	{
		if ( !Scene.IsValid() )
			return;

		// Time.* slows enemies and their shots. The player side reads RealTime.
		Scene.TimeScale = Slowing ? SlowSpeedScale : 1f;
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
