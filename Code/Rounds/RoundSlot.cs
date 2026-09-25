namespace LoopedLoaded;

public sealed class RunLoadout
{
	public int BonusDamage;
	public readonly int[] Levels = new int[64];

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

	public float ReloadScale()
	{
		var snap = TraitLevel( RoundTrait.Snap );
		if ( snap <= 0 || GameSettings.Traits.SnapReload is null )
			return 1f;

		return GameSettings.Traits.SnapReload.At( snap );
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

		if ( Has( RoundTrait.Shuck ) )
			cone += t.ShuckCone;
		if ( Has( RoundTrait.Slam ) )
			count = Math.Max( 1, count - t.SlamPellets );

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
		if ( Has( RoundTrait.Draw ) )
			pierce = Math.Max( 0, pierce - t.DrawPierce );

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
		if ( Has( RoundTrait.Belt ) )
			reload += t.BeltReload;
		if ( Has( RoundTrait.Walk ) )
			reload += t.WalkReload;
		if ( Has( RoundTrait.Spool ) )
			reload += t.SpoolReload;
		if ( Has( RoundTrait.Link ) )
			reload += t.LinkReload;

		if ( lash > 0 )
		{
			reload = t.LashReload;
			if ( Has( RoundTrait.Sear ) )
				reload += t.SearReload;
			if ( Has( RoundTrait.Kiln ) )
				reload += t.KilnReload;
			if ( Has( RoundTrait.Fork ) )
				reload += t.ForkReload;
			if ( Has( RoundTrait.Linger ) )
				reload += t.LingerReload;
			if ( Has( RoundTrait.Cell ) )
				reload += t.CellReload;
		}

		if ( Has( RoundTrait.Jack ) )
			reload *= t.JackReload;
		if ( Has( RoundTrait.Slap ) )
			reload *= t.SlapReload;
		if ( Has( RoundTrait.Rack ) )
			reload *= t.RackReload;
		if ( Has( RoundTrait.Draw ) )
			reload *= t.DrawReload;
		if ( Has( RoundTrait.Feed ) )
			reload *= t.FeedReload;
		if ( Has( RoundTrait.Eject ) )
			reload *= t.EjectReload;
		if ( Has( RoundTrait.Vent ) )
			reload *= t.VentReload;
		if ( Has( RoundTrait.Cool ) )
			reload *= t.CoolReload;
		if ( Has( RoundTrait.Shuck ) )
			reload *= t.ShuckReload;
		if ( Has( RoundTrait.Slam ) )
			reload *= t.SlamReload;

		reload *= ReloadScale();

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
		if ( Has( RoundTrait.Sight ) )
			speed *= t.SightSpeed;
		if ( Has( RoundTrait.Bite ) )
			speed *= t.BiteSpeed;
		if ( Has( RoundTrait.Slap ) )
			speed *= t.SlapSpeed;
		if ( Has( RoundTrait.Rack ) )
			speed *= t.RackSpeed;

		var rangeCut = 0f;
		var meatRange = 0f;
		var meatBonus = 0;
		if ( Has( RoundTrait.Meat ) )
		{
			meatRange = MathF.Max( meatRange, t.MeatRange );
			meatBonus += t.MeatBonus;
			rangeCut += t.MeatRangeCut;
		}

		if ( Has( RoundTrait.Waste ) )
		{
			meatRange = MathF.Max( meatRange, t.WasteRange );
			meatBonus += t.WasteBonus;
			rangeCut += t.WasteRangeCut;
		}

		if ( buck > 0 )
			rangeCut += t.BuckRangeCut.At( buck );

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
		if ( Has( RoundTrait.Jack ) )
			splash *= t.JackSplash;

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
			RangeCut = MathF.Max( 0f, rangeCut ),
			RangePad = slug ? t.SlugFalloffPad : 0f,
			Falloff = 0f,
			MeatRange = meatRange,
			MeatBonus = meatBonus,
			KickForce = Has( RoundTrait.Kick ) ? t.KickForce : 0f,
			KickRange = t.KickRange,
			StunTime = Has( RoundTrait.Stun ) ? t.StunTime : 0f,
			StunRange = t.StunRange,
			Cycle = CycleOf( t ),
			Burst = BurstOf( t, drum ),
			WalkStep = Has( RoundTrait.Walk ) ? t.WalkCone : 0f,
			Sight = Has( RoundTrait.Sight ),
			Bite = Has( RoundTrait.Bite ),
			CommitBurst = Has( RoundTrait.Link ),
			Reload = MathF.Max( t.ReloadMin, reload ),
			BoreWait = boreWait,
			BeamPad = t.LashPad,
			BeamPerSecond = t.LashPerSecond,
			BeamMaxHold = t.LashMaxHold,
			BeamHit = lash > 0 ? Math.Max( 1, t.LashHit ) : 0,
			BeamTick = BeamTickOf( t, lash ),
			BeamTicks = BeamTicksOf( t, lash ),
			BeamRange = t.LashRange,
			BeamWidth = BeamWidthOf( t ),
			BeamRank = lash,
			BeamSear = Has( RoundTrait.Sear ),
			BeamKiln = Has( RoundTrait.Kiln ) ? t.KilnTick : 1f,
			BeamArc = Has( RoundTrait.Arc ) ? t.ArcRange : 0f,
			BeamFork = Has( RoundTrait.Fork ),
			BeamShunt = Has( RoundTrait.Shunt ),
			BeamLinger = Has( RoundTrait.Linger )
		};
	}

	float CycleOf( TraitConfig t )
	{
		var cycle = t.DrumCycle;
		if ( Has( RoundTrait.Spool ) )
			cycle *= t.SpoolCycle;
		if ( Has( RoundTrait.Link ) )
			cycle *= t.LinkCycle;
		if ( Has( RoundTrait.Feed ) )
			cycle *= t.FeedCycle;
		return cycle;
	}

	int BurstOf( TraitConfig t, int drum )
	{
		var burst = drum <= 0 ? 1 : Math.Max( 1, (int)t.DrumBurst.At( drum ) );
		if ( drum > 0 && Has( RoundTrait.Belt ) )
			burst += t.BeltBurst;
		if ( drum > 0 && Has( RoundTrait.Eject ) )
			burst = Math.Max( 1, burst - t.EjectBurst );
		return burst;
	}

	int BeamTicksOf( TraitConfig t, int lash )
	{
		if ( lash <= 0 || t.LashTicks is null )
			return 0;

		var ticks = Math.Max( 1, (int)t.LashTicks.At( lash ) );
		if ( Has( RoundTrait.Cell ) )
			ticks += t.CellTicks;
		return ticks;
	}

	float BeamTickOf( TraitConfig t, int lash )
	{
		if ( lash <= 0 || t.LashTick is null )
			return 1f;

		var tick = t.LashTick.At( lash );
		if ( Has( RoundTrait.Arc ) )
			tick *= t.ArcTick;
		if ( Has( RoundTrait.Shunt ) )
			tick *= t.ShuntTick;
		if ( Has( RoundTrait.Vent ) )
			tick *= t.VentTick;
		return tick;
	}

	float BeamWidthOf( TraitConfig t )
	{
		var width = t.LashWidth;
		if ( Has( RoundTrait.Kiln ) )
			width *= t.KilnWidth;
		if ( Has( RoundTrait.Shunt ) )
			width *= t.ShuntWidth;
		if ( Has( RoundTrait.Cool ) )
			width *= t.CoolWidth;
		return width;
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
