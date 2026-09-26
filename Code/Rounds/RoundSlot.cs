namespace LoopedLoaded;

public sealed class RunLoadout
{
	public int BonusDamage;
	readonly Dictionary<TrinketDef, int> levels = new();
	internal readonly List<TrinketStep> Steps = new();

	public int TraitLevel( TrinketDef card )
	{
		if ( card is null )
			return 0;

		return levels.TryGetValue( card, out var level ) ? level : 0;
	}

	public bool Has( TrinketDef card ) => TraitLevel( card ) > 0;

	public IEnumerable<KeyValuePair<TrinketDef, int>> Owned()
	{
		foreach ( var pair in levels )
		{
			if ( pair.Value > 0 && pair.Key is not null )
				yield return pair;
		}
	}

	public void Clear()
	{
		levels.Clear();
		BonusDamage = 0;
	}

	public void Install( TrinketDef card )
	{
		if ( card is null )
			return;

		levels[card] = Math.Min( card.Cap, TraitLevel( card ) + 1 );
	}

	public RunLoadout Clone()
	{
		var copy = new RunLoadout { BonusDamage = BonusDamage };
		foreach ( var pair in levels )
			copy.levels[pair.Key] = pair.Value;
		return copy;
	}

	public GunRecipe Peek( TrinketDef card )
	{
		if ( card is null )
			return Recipe();

		var old = TraitLevel( card );
		levels[card] = Math.Min( card.Cap, old + 1 );
		var recipe = Recipe();
		if ( old <= 0 )
			levels.Remove( card );
		else
			levels[card] = old;
		return recipe;
	}

	public GunRecipe Recipe() => TrinketBuild.Compile( this );
}

public struct GunRecipe
{
	public bool Beam;
	public bool Auto;
	public bool DoublePump;
	public int Count;
	public float Cone;
	public int Damage;
	public int Pierce;
	public int Bounces;
	public float Energy;
	public float SpeedScale;
	public float SpinSpeed;
	public float Radius;
	public float Splash;
	public int SplashDamage;
	public bool FriendlySplash;
	public bool PerPelletSplash;
	public bool PointAim;
	public bool IgnoreArmor;
	public bool RampPierce;
	public bool Nail;
	public float StickTime;
	public float RangeCut;
	public float RangePad;
	public float Falloff;
	public float MeatRange;
	public int MeatBonus;
	public float KickForce;
	public float KickRange;
	public float StunTime;
	public float StunRange;
	public float Cycle;
	public int Burst;
	public float WalkStep;
	public bool Sight;
	public bool Bite;
	public bool CommitBurst;
	public float Reload;
	public float BoreWait;
	public float BeamPad;
	public float BeamPerSecond;
	public float BeamMaxHold;
	public int BeamHit;
	public float BeamTick;
	public int BeamTicks;
	public float BeamRange;
	public float BeamWidth;
	public int BeamRank;
	public bool BeamSear;
	public float BeamKiln;
	public float BeamArc;
	public bool BeamFork;
	public bool BeamShunt;
	public bool BeamLinger;
	public float Dodge;
	public float Gap;
}

public struct RoundFlight
{
	public Color Tint;
	public int Damage;
	public int PierceCharges;
	public int MaxBounces;
	public float Energy;
	public float SpeedScale;
	public float SpinSpeed;
	public float ExplosiveRadius;
	public int SplashDamage;
	public bool FriendlySplash;
	public bool PointAim;
	public bool IgnoreArmor;
	public bool RampPierce;
	public bool Bite;
	public int BurstId;
	public int VolleyIndex;
	public Vector2 Mark;
	public float Falloff;
	public float MeatRange;
	public int MeatBonus;
	public float KickForce;
	public float KickRange;
	public float StunTime;
	public float StunRange;
	public float StickTime;
	public bool Nail;
	public float FreezeDuration;
	public float FreezeScale;
	public ShotVolley Volley;
}

public sealed class ShotVolley
{
	public int Alive;
	public bool Splashed;
	public bool PerPellet;
	public bool Closed;
	public bool Hold;
	public Vector2 LastFlat;
	readonly HashSet<Enemy> crowd = new();

	public bool TrySplash()
	{
		if ( PerPellet )
			return true;

		if ( Splashed )
			return false;

		Splashed = true;
		return true;
	}

	public bool TryCrowd( Enemy enemy )
	{
		if ( enemy is null || !enemy.IsValid() )
			return false;

		return crowd.Add( enemy );
	}
}

public static class ShotRange
{
	public static void Apply( ref GunRecipe recipe, GameLoop loop )
	{
		if ( recipe.RangeCut <= 0.0001f || recipe.Falloff > 1f )
			return;

		var range = recipe.Energy - recipe.RangeCut * Span( loop );
		range = MathF.Max( range, Floor( loop ) );
		range += MathF.Max( 0f, recipe.RangePad );
		recipe.Falloff = range;
		recipe.Energy = MathF.Min( recipe.Energy, range );
	}

	public static float Span( GameLoop loop )
	{
		var radius = loop.IsValid() && loop.Geometry is not null ? loop.Geometry.BoundaryRadius : 1170f;
		return radius * 4f;
	}

	public static float Floor( GameLoop loop )
	{
		if ( !loop.IsValid() || !loop.Runner.IsValid() )
			return 770f;

		var from = loop.Runner.Flat;
		var nearest = float.MaxValue;
		var found = false;
		foreach ( var enemy in loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive || !Locations.IsBoss( enemy.Kind ) )
				continue;

			found = true;
			var gap = (enemy.Flat - from).Length - enemy.Radius;
			if ( gap < nearest )
				nearest = gap;
		}

		if ( found )
			return MathF.Max( 0f, nearest );

		var core = loop.Geometry is not null ? loop.Geometry.CoreRadius : 230f;
		return MathF.Max( 0f, from.Length - core );
	}
}

public enum RunPhase
{
	Menu,
	Playing,
	DecideLap,
	DecideRing,
	PickTrait,
	Dead,
	Extracted,
	Won,
	City
}
