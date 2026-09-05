namespace LoopedLoaded;

public sealed class RingRunner : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public GameLoop Loop { get; set; }
	[Property] public float Speed { get; set; } = 330f;
	[Property] public float DashDistance { get; set; } = 430f;
	[Property] public float DashDuration { get; set; } = 0.17f;
	[Property] public float DashCooldown { get; set; } = 1.1f;
	[Property] public float SlowSpeedScale { get; set; } = 0.38f;
	[Property] public float SlowDrain { get; set; } = 0.55f;
	[Property] public float SlowRegen { get; set; } = 0.28f;
	[Property] public float PlayerRadius { get; set; } = 34f;

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
		ApplyTransform();
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

	protected override void OnUpdate()
	{
		if ( Loop.IsValid() && Loop.IsFrozen )
		{
			ApplyTransform();
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
