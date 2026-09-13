namespace LoopedLoaded;

public static class Progression
{
	static ProgressionConfig C => GameSettings.Progression;

	public static float ThreatRatio => C.ThreatRatio;
	public static float PowerRatio => C.PowerRatio;
	public static float CostRatio => C.CostRatio;
	public static float TraitRatio => C.TraitRatio;
	public static float PaceRatio => C.PaceRatio;
	public static float DashRatio => C.DashRatio;
	public static float RoundRatio => C.RoundRatio;
	public static int MaxSlots => C.MaxSlots;
	public static int BossBaseHealth => C.BossBaseHealth;

	public static float LocationMul( int location ) => MathF.Pow( ThreatRatio, Math.Max( 0, location ) );

	public static float Threat( int lap, int location = 0 )
		=> MathF.Pow( ThreatRatio, Math.Max( 0, lap - 1 ) ) * LocationMul( location );

	public static float Pace( int lap ) => MathF.Min( C.PaceCap, MathF.Pow( PaceRatio, Math.Max( 0, lap - 1 ) ) );

	public static int RankValue( int rank )
	{
		if ( rank <= 0 )
			return 0;

		return Whole( MathF.Pow( PowerRatio, rank - 1 ) );
	}

	public static int EnemyHealth( int template, int lap, int location = 0 )
		=> Math.Max( 1, Whole( template * Threat( lap, location ) ) );

	public static int ExtraBodies( int lap, int location = 0 )
		=> Math.Clamp( Whole( Threat( lap, location ) ) - C.ExtraBodiesOffset, 0, C.ExtraBodiesMax );

	public static int RoundsGranted( int arrivingLap )
		=> Math.Clamp( Whole( MathF.Pow( RoundRatio, Math.Max( 0, arrivingLap - 2 ) ) ), C.RoundsGrantedMin, C.RoundsGrantedMax );

	public static int BossHealth( int lap, int location = 0 )
		=> Math.Max( BossBaseHealth, Whole( BossBaseHealth * Threat( lap, location ) ) );

	public static int Cost( int first, int level )
		=> Math.Max( 1, Whole( first * MathF.Pow( CostRatio, Math.Max( 0, level ) ) ) );

	public static int PackPrice( TraitPack pack ) => GameSettings.Traits.PackPrice( pack );

	public static int TraitPrice( RoundTrait trait, int ownedLevel )
		=> Cost( PackPrice( RoundTraits.Pack( trait ) ), Math.Max( 0, ownedLevel ) );

	public static int KillScrap( EnemyKind kind, int lap, int location = 0 )
	{
		var seed = GameSettings.Enemies.ScrapOf( kind );
		if ( seed <= 0 )
			return 0;

		return Whole( seed * Threat( lap, location ) );
	}

	public static float DashScale( int boost )
		=> MathF.Max( C.DashScaleFloor, 1f / MathF.Pow( DashRatio, Math.Max( 0, boost ) ) );

	public static float SlowDrain( int brake )
	{
		if ( brake <= 0 )
			return C.SlowDrainBase;

		return MathF.Max( C.SlowDrainFloor, C.SlowDrainBase / MathF.Pow( ThreatRatio, brake - 1 ) );
	}

	public static int TraitStack( int level, float seed )
	{
		if ( level <= 0 )
			return 0;

		return Whole( seed * MathF.Pow( TraitRatio, level - 1 ) );
	}

	public static float TraitMul( int level )
		=> level <= 0 ? 1f : MathF.Pow( TraitRatio, level - 1 );

	public static float Tier( int level, float a, float b, float c )
	{
		if ( level <= 0 )
			return 0f;

		if ( level == 1 )
			return a;

		if ( level == 2 )
			return b;

		return c;
	}

	public static int Whole( float value )
		=> Math.Max( 1, (int)MathF.Round( value ) );
}
