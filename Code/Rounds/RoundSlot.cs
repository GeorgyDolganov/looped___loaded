namespace LoopedLoaded;

public sealed class RunLoadout
{
	public int BonusDamage;
	public readonly int[] Levels = new int[32];

	public int TraitLevel( RoundTrait trait )
	{
		var index = (int)trait;
		return index < 0 || index >= Levels.Length ? 0 : Levels[index];
	}

	public bool Has( RoundTrait trait ) => TraitLevel( trait ) > 0;

	public void Clear()
	{
		for ( var i = 0; i < Levels.Length; i++ )
			Levels[i] = 0;

		BonusDamage = 0;
	}

	public void Install( RoundTrait trait )
	{
		var index = (int)trait;
		if ( index < 0 || index >= Levels.Length )
			return;

		Levels[index] = Math.Min( RoundTraits.MaxLevel( trait ), Levels[index] + 1 );
	}

	public GunRecipe Peek( RoundTrait trait )
	{
		var index = (int)trait;
		if ( index < 0 || index >= Levels.Length )
			return Recipe();

		var old = Levels[index];
		Levels[index] = Math.Min( RoundTraits.MaxLevel( trait ), old + 1 );
		var recipe = Recipe();
		Levels[index] = old;
		return recipe;
	}

	public GunRecipe Recipe()
	{
		var t = GameSettings.Traits;
		var bore = TraitLevel( RoundTrait.Bore );
		var drum = TraitLevel( RoundTrait.Drum );
		var warhead = TraitLevel( RoundTrait.Warhead );
		var lash = TraitLevel( RoundTrait.Lash );
		var pin = TraitLevel( RoundTrait.Pin );
		var spin = TraitLevel( RoundTrait.Spin );
		var rush = TraitLevel( RoundTrait.Rush );
		var slug = Has( RoundTrait.Slug );

		var count = 1;
		if ( Has( RoundTrait.Split ) )
			count += t.SplitPellets;
		if ( Has( RoundTrait.Pump ) )
			count += t.PumpPellets;
		if ( Has( RoundTrait.Load ) )
			count += t.LoadPellets;
		if ( Has( RoundTrait.Heap ) )
			count += t.HeapPellets;

		var cone = 0f;
		if ( Has( RoundTrait.Fan ) )
			cone += t.FanCone;
		if ( Has( RoundTrait.Gape ) )
			cone += t.GapeCone;
		if ( Has( RoundTrait.Choke ) )
			cone = MathF.Max( t.ChokeFloor, cone - t.ChokeCone );

		if ( slug )
		{
			count = 1;
			cone = 0f;
		}
		else if ( count <= 1 && pin > 0 )
		{
			count = Math.Max( 1, (int)t.PinNails.At( pin ) );
			cone = t.PinCone.At( pin );
		}

		var bounces = t.MaxBouncesBase;
		if ( pin > 0 )
			bounces += (int)t.PinBounce.At( pin );
		if ( Has( RoundTrait.Rico ) )
			bounces += t.RicoBounces;
		if ( lash > 0 )
			bounces = 0;

		var pierce = bore <= 0 ? 0 : (int)t.BorePierce.At( bore );
		if ( Has( RoundTrait.Breach ) )
			pierce += t.BreachPierce;

		var reload = t.ReloadBase;
		var boreWait = 0f;
		if ( bore > 0 )
		{
			boreWait = t.BoreReload * Progression.TraitMul( bore );
			reload += boreWait;
		}
		if ( drum > 0 )
			reload += t.DrumReload * Progression.TraitMul( drum );
		if ( spin > 0 )
			reload += t.SpinReload * Progression.TraitMul( spin );
		if ( rush > 0 )
			reload += t.RushReload * Progression.TraitMul( rush );
		if ( Has( RoundTrait.Pump ) )
			reload += t.PumpReload;
		if ( Has( RoundTrait.Load ) )
			reload += t.LoadReload;
		if ( Has( RoundTrait.Heap ) )
			reload += t.HeapReload;
		if ( Has( RoundTrait.Double ) )
			reload += t.DoubleReload;

		var speed = 1f;
		if ( warhead > 0 )
			speed *= t.WarheadSpeed.At( warhead );
		if ( rush > 0 && t.RushSpeed is not null )
			speed *= t.RushSpeed.At( rush );

		var sweep = t.SpinBase;
		if ( spin > 0 && t.SpinBoost is not null )
			sweep += t.SpinBoost.At( spin );

		var falloff = 0f;
		var meatRange = 0f;
		var meatBonus = 0;
		if ( Has( RoundTrait.Meat ) )
		{
			meatRange = MathF.Max( meatRange, t.MeatRange );
			meatBonus += t.MeatBonus;
			falloff = falloff > 1f ? MathF.Min( falloff, t.MeatFalloff ) : t.MeatFalloff;
		}

		if ( Has( RoundTrait.Waste ) )
		{
			meatRange = MathF.Max( meatRange, t.WasteRange );
			meatBonus += t.WasteBonus;
			falloff = falloff > 1f ? MathF.Min( falloff, t.WasteFalloff ) : t.WasteFalloff;
		}

		if ( slug && falloff > 1f )
			falloff += t.SlugFalloffPad;

		var damage = Math.Max( 1, t.BaseDamage + BonusDamage );
		if ( slug )
			damage += t.SlugDamage;

		var radius = pin > 0 ? t.PinRadius : 13f;
		if ( slug )
			radius = MathF.Max( radius, t.SlugRadius );

		return new GunRecipe
		{
			Beam = lash > 0,
			Auto = drum > 0 && lash <= 0,
			DoublePump = Has( RoundTrait.Double ) && drum <= 0 && lash <= 0,
			Count = count,
			Cone = cone,
			Damage = damage,
			Pierce = pierce,
			Bounces = bounces,
			Energy = t.EnergyBase,
			SpeedScale = speed,
			SpinSpeed = sweep,
			Radius = radius,
			Splash = t.WarheadRadius.At( warhead ),
			FriendlySplash = warhead > 0,
			Nail = pin > 0 && !slug,
			StickTime = pin > 0 && !slug ? t.PinStick : 0f,
			Falloff = falloff,
			MeatRange = meatRange,
			MeatBonus = meatBonus,
			KickForce = Has( RoundTrait.Kick ) ? t.KickForce : 0f,
			KickRange = t.KickRange,
			StunTime = Has( RoundTrait.Stun ) ? t.StunTime : 0f,
			StunRange = t.StunRange,
			Cycle = t.DrumCycle,
			Burst = drum <= 0 ? 1 : Math.Max( 1, (int)t.DrumBurst.At( drum ) ),
			Reload = MathF.Max( t.ReloadMin, reload ),
			BoreWait = boreWait,
			BeamPad = t.LashPad,
			BeamPerSecond = t.LashPerSecond,
			BeamMaxHold = t.LashMaxHold,
			BeamTick = lash > 0 && t.LashTick is not null ? t.LashTick.At( lash ) : 0.28f,
			BeamRange = t.LashRange,
			BeamWidth = t.LashWidth
		};
	}
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
	public bool FriendlySplash;
	public bool Nail;
	public float StickTime;
	public float Falloff;
	public float MeatRange;
	public int MeatBonus;
	public float KickForce;
	public float KickRange;
	public float StunTime;
	public float StunRange;
	public float Cycle;
	public int Burst;
	public float Reload;
	public float BoreWait;
	public float BeamPad;
	public float BeamPerSecond;
	public float BeamMaxHold;
	public float BeamTick;
	public float BeamRange;
	public float BeamWidth;
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
	public bool FriendlySplash;
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
	public bool Closed;
	public bool Hold;
	public Vector2 LastFlat;
	readonly HashSet<Enemy> crowd = new();

	public bool TrySplash()
	{
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
