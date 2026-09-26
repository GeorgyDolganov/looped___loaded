namespace LoopedLoaded;

public sealed class BuildState
{
	public bool MirrorFull = true;
	public bool Collapsed;
	public GunFlag Mask;
	public int Count = 1;
	public int Full = 1;
	public float Cone;
	public int Damage = 1;
	public int Pierce;
	public int Bounces;
	public float Reload;
	public float BoreWait;
	public float Speed = 1f;
	public float Radius = 13f;
	public float Splash;
	public int SplashDamage;
	public float RangeCut;
	public float RangePad;
	public float MeatRange;
	public int MeatBonus;
	public float KickForce;
	public float KickRange;
	public float StunTime;
	public float StunRange;
	public float Cycle;
	public int Burst = 1;
	public float WalkStep;
	public int BeamHit;
	public float BeamTick = 1f;
	public int BeamTicks;
	public float BeamWidth;
	public float BeamArc;
	public float BeamKiln = 1f;
	public int BeamRank;
	public float StickTime;
	public float Dodge;
	public float Gap;
	public float SpinSpeed;
	public float Energy;
	public float BeamPad;
	public float BeamPerSecond;
	public float BeamMaxHold;
	public float BeamRange;
	public float Falloff;

	public BuildState( int bonus )
	{
		var traits = GameSettings.Traits;
		Bounces = traits.MaxBouncesBase;
		Reload = traits.ReloadBase;
		Damage = Math.Max( 1, traits.BaseDamage + bonus );
		Radius = traits.ProjectileRadius;
		Energy = traits.EnergyBase;
		Cycle = traits.DrumCycle;
		BeamWidth = traits.LashWidth;
		BeamPad = traits.LashPad;
		BeamPerSecond = traits.LashPerSecond;
		BeamMaxHold = traits.LashMaxHold;
		BeamRange = traits.LashRange;
	}

	public bool Has( GunFlag flag ) => (Mask & flag) != 0;

	public float Get( GunStat stat ) => stat switch
	{
		GunStat.Count => Count,
		GunStat.Full => Full,
		GunStat.Cone => Cone,
		GunStat.Damage => Damage,
		GunStat.Pierce => Pierce,
		GunStat.Bounces => Bounces,
		GunStat.Reload => Reload,
		GunStat.BoreWait => BoreWait,
		GunStat.Speed => Speed,
		GunStat.Radius => Radius,
		GunStat.Splash => Splash,
		GunStat.SplashDamage => SplashDamage,
		GunStat.RangeCut => RangeCut,
		GunStat.RangePad => RangePad,
		GunStat.MeatRange => MeatRange,
		GunStat.MeatBonus => MeatBonus,
		GunStat.KickForce => KickForce,
		GunStat.KickRange => KickRange,
		GunStat.StunTime => StunTime,
		GunStat.StunRange => StunRange,
		GunStat.Cycle => Cycle,
		GunStat.Burst => Burst,
		GunStat.WalkStep => WalkStep,
		GunStat.BeamHit => BeamHit,
		GunStat.BeamTick => BeamTick,
		GunStat.BeamTicks => BeamTicks,
		GunStat.BeamWidth => BeamWidth,
		GunStat.BeamArc => BeamArc,
		GunStat.BeamKiln => BeamKiln,
		GunStat.BeamRank => BeamRank,
		GunStat.StickTime => StickTime,
		GunStat.Dodge => Dodge,
		GunStat.Gap => Gap,
		GunStat.SpinSpeed => SpinSpeed,
		_ => Energy
	};

	public void Set( GunStat stat, float value )
	{
		switch ( stat )
		{
			case GunStat.Count: Count = (int)value; break;
			case GunStat.Full: Full = (int)value; break;
			case GunStat.Cone: Cone = value; break;
			case GunStat.Damage: Damage = (int)value; break;
			case GunStat.Pierce: Pierce = (int)value; break;
			case GunStat.Bounces: Bounces = (int)value; break;
			case GunStat.Reload: Reload = value; break;
			case GunStat.BoreWait: BoreWait = value; break;
			case GunStat.Speed: Speed = value; break;
			case GunStat.Radius: Radius = value; break;
			case GunStat.Splash: Splash = value; break;
			case GunStat.SplashDamage: SplashDamage = (int)value; break;
			case GunStat.RangeCut: RangeCut = value; break;
			case GunStat.RangePad: RangePad = value; break;
			case GunStat.MeatRange: MeatRange = value; break;
			case GunStat.MeatBonus: MeatBonus = (int)value; break;
			case GunStat.KickForce: KickForce = value; break;
			case GunStat.KickRange: KickRange = value; break;
			case GunStat.StunTime: StunTime = value; break;
			case GunStat.StunRange: StunRange = value; break;
			case GunStat.Cycle: Cycle = value; break;
			case GunStat.Burst: Burst = (int)value; break;
			case GunStat.WalkStep: WalkStep = value; break;
			case GunStat.BeamHit: BeamHit = (int)value; break;
			case GunStat.BeamTick: BeamTick = value; break;
			case GunStat.BeamTicks: BeamTicks = (int)value; break;
			case GunStat.BeamWidth: BeamWidth = value; break;
			case GunStat.BeamArc: BeamArc = value; break;
			case GunStat.BeamKiln: BeamKiln = value; break;
			case GunStat.BeamRank: BeamRank = (int)value; break;
			case GunStat.StickTime: StickTime = value; break;
			case GunStat.Dodge: Dodge = value; break;
			case GunStat.Gap: Gap = value; break;
			case GunStat.SpinSpeed: SpinSpeed = value; break;
			default: Energy = value; break;
		}
	}

	public void Apply( TrinketMod mod, int level )
	{
		if ( mod is null )
			return;

		if ( mod.UseWhen )
		{
			var current = Get( mod.WhenStat );
			if ( mod.HasWhenMin && current < mod.WhenMin )
				return;

			if ( mod.HasWhenMax && current > mod.WhenMax )
				return;
		}

		var amount = mod.Amount( level, Math.Max( 0, Full - Count ) );
		if ( mod.Stat == GunStat.Count )
		{
			Count = (int)Operate( Count, mod.Op, amount, true );
			if ( MirrorFull )
				Full = Count;
			else if ( mod.AlsoFull )
				Full = (int)Operate( Full, mod.Op, amount, true );
			return;
		}

		var integer = IsInteger( mod.Stat );
		Set( mod.Stat, Operate( Get( mod.Stat ), mod.Op, amount, integer ) );
	}

	public GunRecipe ToRecipe()
	{
		var beam = Has( GunFlag.Beam );
		var auto = Has( GunFlag.Auto ) && !beam;
		var nail = Has( GunFlag.Nail ) && !Has( GunFlag.NoNail );
		return new GunRecipe
		{
			Beam = beam,
			Auto = auto,
			DoublePump = Has( GunFlag.DoublePump ) && !auto && !beam,
			Count = Count,
			Cone = Cone,
			Damage = Math.Max( 1, Damage ),
			Pierce = Pierce,
			Bounces = Bounces,
			Energy = Energy,
			SpeedScale = Speed,
			SpinSpeed = SpinSpeed,
			Radius = Radius,
			Splash = Splash,
			SplashDamage = SplashDamage,
			FriendlySplash = Has( GunFlag.FriendlySplash ) && !Has( GunFlag.NoFriendlySplash ),
			PerPelletSplash = Has( GunFlag.PerPelletSplash ),
			PointAim = Has( GunFlag.PointAim ),
			IgnoreArmor = Has( GunFlag.IgnoreArmor ),
			RampPierce = Has( GunFlag.RampPierce ),
			Nail = nail,
			StickTime = nail ? StickTime : 0f,
			RangeCut = MathF.Max( 0f, RangeCut ),
			RangePad = RangePad,
			Falloff = Falloff,
			MeatRange = MeatRange,
			MeatBonus = MeatBonus,
			KickForce = KickForce,
			KickRange = KickRange,
			StunTime = StunTime,
			StunRange = StunRange,
			Cycle = Cycle,
			Burst = Burst,
			WalkStep = WalkStep,
			Sight = Has( GunFlag.Sight ),
			Bite = Has( GunFlag.Bite ),
			CommitBurst = Has( GunFlag.CommitBurst ),
			Reload = MathF.Max( GameSettings.Traits.ReloadMin, Reload ),
			BoreWait = BoreWait,
			BeamPad = BeamPad,
			BeamPerSecond = BeamPerSecond,
			BeamMaxHold = BeamMaxHold,
			BeamHit = BeamHit,
			BeamTick = BeamTick,
			BeamTicks = BeamTicks,
			BeamRange = BeamRange,
			BeamWidth = BeamWidth,
			BeamRank = BeamRank,
			BeamSear = Has( GunFlag.BeamSear ),
			BeamKiln = BeamKiln,
			BeamArc = BeamArc,
			BeamFork = Has( GunFlag.BeamFork ),
			BeamShunt = Has( GunFlag.BeamShunt ),
			BeamLinger = Has( GunFlag.BeamLinger ),
			Dodge = Dodge,
			Gap = Gap
		};
	}

	public static bool IsInteger( GunStat stat ) => stat is GunStat.Count or GunStat.Full or GunStat.Damage or GunStat.Pierce or GunStat.Bounces or GunStat.SplashDamage or GunStat.MeatBonus or GunStat.Burst or GunStat.BeamHit or GunStat.BeamTicks or GunStat.BeamRank;

	public static float Operate( float current, ModOp op, float amount, bool integer )
	{
		if ( integer )
		{
			var now = (int)current;
			var step = (int)amount;
			return op switch
			{
				ModOp.Mul => (int)(now * amount),
				ModOp.Set => step,
				ModOp.Min => Math.Min( now, step ),
				ModOp.Max => Math.Max( now, step ),
				_ => now + step
			};
		}

		return op switch
		{
			ModOp.Mul => current * amount,
			ModOp.Set => amount,
			ModOp.Min => MathF.Min( current, amount ),
			ModOp.Max => MathF.Max( current, amount ),
			_ => current + amount
		};
	}
}

enum TrinketStepKind
{
	Mod,
	PinEarly,
	PinLate,
	Slug,
	Flags,
	Freeze,
	SplashDamage
}

struct TrinketStep : IComparable<TrinketStep>
{
	public int Order;
	public int Sort;
	public int Index;
	public TrinketStepKind Kind;
	public TrinketMod Mod;
	public TrinketDef Card;
	public int Level;

	public int CompareTo( TrinketStep other )
	{
		var order = Order.CompareTo( other.Order );
		if ( order != 0 )
			return order;

		order = Sort.CompareTo( other.Sort );
		if ( order != 0 )
			return order;

		return Index.CompareTo( other.Index );
	}
}

public static class TrinketBuild
{
	public static GunRecipe Compile( RunLoadout loadout )
	{
		var state = new BuildState( loadout?.BonusDamage ?? 0 );
		var steps = loadout?.Steps;
		if ( steps is null )
			return state.ToRecipe();

		steps.Clear();
		var index = 0;
		foreach ( var card in Trinkets.Every )
			AddPassive( steps, card, ref index );

		if ( loadout is not null )
		{
			foreach ( var pair in loadout.Owned() )
				AddOwned( steps, pair.Key, pair.Value, ref index );
		}

		steps.Add( Builtin( 150, int.MaxValue, ref index, TrinketStepKind.Freeze ) );
		steps.Add( Builtin( 1250, int.MaxValue, ref index, TrinketStepKind.SplashDamage ) );
		steps.Sort();
		foreach ( var step in steps )
			Run( state, step );

		return state.ToRecipe();
	}

	static void AddPassive( List<TrinketStep> steps, TrinketDef card, ref int index )
	{
		if ( card?.Mods is null )
			return;

		foreach ( var mod in card.Mods )
		{
			if ( mod is null || !mod.Passive || mod.ByHook )
				continue;

			steps.Add( new TrinketStep
			{
				Order = mod.Order,
				Sort = card.Sort,
				Index = index++,
				Kind = TrinketStepKind.Mod,
				Mod = mod,
				Card = card,
				Level = 1
			} );
		}
	}

	static void AddOwned( List<TrinketStep> steps, TrinketDef card, int level, ref int index )
	{
		if ( card is null || level <= 0 )
			return;

		if ( card.Hook == TrinketHook.Pin )
		{
			steps.Add( Hook( 110, card, level, ref index, TrinketStepKind.PinEarly ) );
			steps.Add( Hook( 310, card, level, ref index, TrinketStepKind.PinLate ) );
		}

		if ( card.Hook == TrinketHook.Slug )
			steps.Add( Hook( 300, card, level, ref index, TrinketStepKind.Slug ) );

		if ( card.Mods is not null )
		{
			foreach ( var mod in card.Mods )
			{
				if ( mod is null || mod.Passive || mod.ByHook )
					continue;

				steps.Add( new TrinketStep
				{
					Order = mod.Order,
					Sort = card.Sort,
					Index = index++,
					Kind = TrinketStepKind.Mod,
					Mod = mod,
					Card = card,
					Level = level
				} );
			}
		}

		steps.Add( new TrinketStep
		{
			Order = 8000,
			Sort = card.Sort,
			Index = index++,
			Kind = TrinketStepKind.Flags,
			Card = card,
			Level = level
		} );
	}

	static TrinketStep Hook( int order, TrinketDef card, int level, ref int index, TrinketStepKind kind ) => new()
	{
		Order = order,
		Sort = card.Sort,
		Index = index++,
		Kind = kind,
		Card = card,
		Level = level
	};

	static TrinketStep Builtin( int order, int sort, ref int index, TrinketStepKind kind ) => new()
	{
		Order = order,
		Sort = sort,
		Index = index++,
		Kind = kind
	};

	static void Run( BuildState state, TrinketStep step )
	{
		switch ( step.Kind )
		{
			case TrinketStepKind.Mod:
				state.Apply( step.Mod, step.Level );
				break;
			case TrinketStepKind.PinEarly:
				TrinketHooks.PinEarly( state, step.Card, step.Level );
				break;
			case TrinketStepKind.PinLate:
				TrinketHooks.PinLate( state, step.Card, step.Level );
				break;
			case TrinketStepKind.Slug:
				TrinketHooks.Slug( state );
				break;
			case TrinketStepKind.Flags:
				if ( step.Card?.Flags is null )
					break;

				foreach ( var flag in step.Card.Flags )
					state.Mask |= flag;
				break;
			case TrinketStepKind.Freeze:
				state.MirrorFull = false;
				break;
			case TrinketStepKind.SplashDamage:
				state.SplashDamage = state.Splash > 1f ? 1 : 0;
				break;
		}
	}
}
