namespace LoopedLoaded;

public sealed class RingRunner : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public float Speed { get; set; } = 330f;
	[Property] public float DashDistance { get; set; } = 430f;
	[Property] public float DashDuration { get; set; } = 0.17f;
	[Property] public float DashCooldown { get; set; } = 1.1f;

	public float Angle { get; private set; }
	public float TravelledArc { get; private set; }
	public int Lap => 1 + (int)(TravelledArc / (MathF.Tau * Radius));
	public float LapFraction => (TravelledArc / (MathF.Tau * Radius)) % 1f;
	public bool Dashing => dashElapsed < DashDuration;
	public float DashCharge => DashCooldown <= 0f ? 1f : MathF.Min( 1f, (Time.Now - lastDash) / DashCooldown );

	public float Radius => Arena.IsValid() ? Arena.Geometry.TrackRadius : 1000f;
	public Vector2 Flat => ArenaGeometry.FromAngle( Angle ) * Radius;
	public Vector2 Tangent => new Vector2( MathF.Sin( Angle ), -MathF.Cos( Angle ) );

	float dashElapsed = 999f;
	float lastDash = -999f;
	float dashSpent;

	public void ResetToStart( float startAngle )
	{
		Angle = startAngle;
		TravelledArc = 0f;
		dashElapsed = 999f;
		dashSpent = 0f;
		lastDash = -999f;
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
		var arc = Speed * Time.Delta;

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
