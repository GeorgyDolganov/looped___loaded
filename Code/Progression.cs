namespace LoopedLoaded;

public static class Progression
{
	public const float ThreatRatio = 1.22f;
	public const float PowerRatio = 1.6f;
	public const float CostRatio = 2f;
	public const float TraitRatio = 1.4f;
	public const float PaceRatio = 1.035f;
	public const float DashRatio = 1.16f;
	public const float RoundRatio = 1.25f;
	public const int MaxSlots = 9;
	public const int BossBaseHealth = 12;

	public static float Threat( int lap ) => MathF.Pow( ThreatRatio, Math.Max( 0, lap - 1 ) );

	public static float Pace( int lap ) => MathF.Min( 1.4f, MathF.Pow( PaceRatio, Math.Max( 0, lap - 1 ) ) );

	public static int RankValue( int rank )
	{
		if ( rank <= 0 )
			return 0;

		return Whole( MathF.Pow( PowerRatio, rank - 1 ) );
	}

	public static int EnemyHealth( int template, int lap )
		=> Math.Max( 1, Whole( template * Threat( lap ) ) );

	public static int ExtraBodies( int lap )
		=> Math.Clamp( Whole( Threat( lap ) ) - 2, 0, 4 );

	public static int RoundsGranted( int arrivingLap )
		=> Math.Clamp( Whole( MathF.Pow( RoundRatio, Math.Max( 0, arrivingLap - 2 ) ) ), 1, 4 );

	public static int BossHealth( int lap )
		=> Math.Max( BossBaseHealth, Whole( BossBaseHealth * Threat( lap ) ) );

	public static int Cost( int first, int level )
		=> Math.Max( 1, Whole( first * MathF.Pow( CostRatio, Math.Max( 0, level ) ) ) );

	public static float DashScale( int boost )
		=> MathF.Max( 0.42f, 1f / MathF.Pow( DashRatio, Math.Max( 0, boost ) ) );

	public static float SlowDrain( int brake )
	{
		if ( brake <= 0 )
			return 0.55f;

		return MathF.Max( 0.22f, 0.55f / MathF.Pow( ThreatRatio, brake - 1 ) );
	}

	public static int TraitStack( int level, float seed )
	{
		if ( level <= 0 )
			return 0;

		return Whole( seed * MathF.Pow( TraitRatio, level - 1 ) );
	}

	public static float TraitMul( int level )
		=> level <= 0 ? 1f : MathF.Pow( TraitRatio, level - 1 );

	public static int Whole( float value )
		=> Math.Max( 1, (int)MathF.Round( value ) );
}
