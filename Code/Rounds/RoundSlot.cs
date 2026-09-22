namespace LoopedLoaded;

public sealed class RunLoadout
{
	public int BonusDamage;
	public readonly int[] Levels = new int[40];

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
		var buck = TraitLevel( RoundTrait.Buck );
		var bore = TraitLevel( RoundTrait.Bore );
		var drum = TraitLevel( RoundTrait.Drum );
		var warhead = TraitLevel( RoundTrait.Warhead );
		var lash = TraitLevel( RoundTrait.Lash );
		var pin = TraitLevel( RoundTrait.Pin );
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
		if ( buck > 0 )
			count += Math.Max( 0, (int)t.BuckPellets.At( buck ) - 1 );

		var cone = 0f;
		if ( Has( RoundTrait.Fan ) )
			cone += t.FanCone;
		if ( Has( RoundTrait.Gape ) )
			cone += t.GapeCone;
		if ( Has( RoundTrait.Choke ) )
			cone = MathF.Max( t.ChokeFloor, cone - t.ChokeCone );
		if ( buck > 0 )
			cone += t.BuckCone.At( buck );

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

		if ( Has( RoundTrait.Mass ) )
		{
			count = 1;
			cone = 0f;
		}

		var bounces = t.MaxBouncesBase;
		if ( pin > 0 )
			bounces += (int)t.PinBounce.At( pin );
		if ( Has( RoundTrait.Rico ) )
			bounces += t.RicoBounces;
		if ( lash > 0 )
			bounces = 0;
		if ( Has( RoundTrait.Lance ) || Has( RoundTrait.Crater ) || Has( RoundTrait.Spot ) || Has( RoundTrait.Keel ) )
			bounces = 0;

		var pierce = bore <= 0 ? 0 : (int)t.BorePierce.At( bore );
		if ( Has( RoundTrait.Deep ) )
			pierce += t.DeepPierce;
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
		if ( Has( RoundTrait.Bloom ) )
			reload += t.BloomReload;
		if ( Has( RoundTrait.Scorch ) )
			reload += t.ScorchReload;
		if ( Has( RoundTrait.Lance ) )
			reload += t.LanceReload;
		if ( Has( RoundTrait.Deep ) )
			reload += t.DeepReload;
		if ( Has( RoundTrait.Awl ) )
			reload += t.AwlReload;
		if ( Has( RoundTrait.Mass ) )
			reload += t.MassReload;
		if ( Has( RoundTrait.Trace ) )
			reload += t.TraceReload;

		var speed = 1f;
		if ( warhead > 0 )
			speed *= t.WarheadSpeed.At( warhead );
		if ( Has( RoundTrait.Mirv ) )
			speed *= t.MirvSpeed;
		if ( Has( RoundTrait.Scorch ) )
			speed *= t.ScorchSpeed;
		if ( Has( RoundTrait.Lance ) )
			speed *= t.LanceSpeed;
		if ( Has( RoundTrait.Crater ) )
			speed *= t.CraterSpeed;
		if ( Has( RoundTrait.Spot ) )
			speed *= t.SpotSpeed;
		if ( rush > 0 && t.RushSpeed is not null )
			speed *= t.RushSpeed.At( rush );
		if ( Has( RoundTrait.Awl ) )
			speed *= t.AwlSpeed;
		if ( Has( RoundTrait.Ram ) )
			speed *= t.RamSpeed;
		if ( Has( RoundTrait.Mass ) )
			speed *= t.MassSpeed;
		if ( Has( RoundTrait.Keel ) )
			speed *= t.KeelSpeed;
		if ( Has( RoundTrait.Trace ) )
			speed *= t.TraceSpeed;

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

		if ( buck > 0 )
		{
			var cut = t.BuckFalloff.At( buck );
			falloff = falloff > 1f ? MathF.Min( falloff, cut ) : cut;
		}

		if ( slug && falloff > 1f )
			falloff += t.SlugFalloffPad;

		var damage = Math.Max( 1, t.BaseDamage + BonusDamage );
		if ( slug )
			damage += t.SlugDamage;
		if ( Has( RoundTrait.Lance ) )
			damage += t.LanceDamage;
		if ( Has( RoundTrait.Mass ) )
			damage += t.MassDamage;
		if ( Has( RoundTrait.Keel ) )
			damage += t.KeelDamage;

		var radius = pin > 0 ? t.PinRadius : 13f;
		if ( slug )
			radius = MathF.Max( radius, t.SlugRadius );
		if ( Has( RoundTrait.Crater ) )
			radius = MathF.Max( radius, t.CraterBody );

		var splash = t.WarheadRadius.At( warhead );
		if ( Has( RoundTrait.Mirv ) )
			splash *= t.MirvRadiusScale;
		if ( Has( RoundTrait.Bloom ) )
			splash += t.BloomRadius;
		if ( Has( RoundTrait.Lance ) )
			splash *= t.LanceRadiusScale;
		if ( Has( RoundTrait.Crater ) )
			splash += t.CraterSplash;

		var splashDamage = splash > 1f ? 1 : 0;
		if ( Has( RoundTrait.Scorch ) && splash > 1f )
			splashDamage = Math.Max( splashDamage, t.ScorchDamage );

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
			SpinSpeed = 0f,
			Radius = radius,
			Splash = splash,
			SplashDamage = splashDamage,
			FriendlySplash = warhead > 0 && !Has( RoundTrait.Lance ),
			PerPelletSplash = Has( RoundTrait.Mirv ),
			PointAim = Has( RoundTrait.Spot ),
			IgnoreArmor = Has( RoundTrait.Awl ),
			RampPierce = Has( RoundTrait.Ram ),
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
			BeamHit = lash > 0 ? Math.Max( 1, t.LashHit ) : 0,
			BeamTick = lash > 0 && t.LashTick is not null ? t.LashTick.At( lash ) : 0.70f,
			BeamRange = t.LashRange,
			BeamWidth = t.LashWidth,
			BeamRank = lash
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
	public int SplashDamage;
	public bool FriendlySplash;
	public bool PerPelletSplash;
	public bool PointAim;
	public bool IgnoreArmor;
	public bool RampPierce;
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
	public int BeamHit;
	public float BeamTick;
	public float BeamRange;
	public float BeamWidth;
	public int BeamRank;
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
