namespace LoopedLoaded;

public static class TrinketParity
{
	[ConCmd( "trinket_parity" )]
	public static void Command()
	{
		Trinkets.Refresh();
		foreach ( var id in Pool )
		{
			if ( Trinkets.Find( id ) is null )
			{
				Log.Warning( $"trinket parity missing {id}" );
				return;
			}
		}

		var fails = 0;
		var shown = 0;
		fails += Run( new Dictionary<string, int>(), 0, ref shown );
		fails += Run( Levels( ("CHOKE", 1) ), 0, ref shown );
		fails += Run( Levels( ("FAN", 1), ("CHOKE", 1), ("BUCK", 1) ), 0, ref shown );
		fails += Run( Levels( ("SPLIT", 4), ("SLUG", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1), ("SLUG", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1), ("SPLIT", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1), ("SHUCK", 1) ), 0, ref shown );
		fails += Run( Levels( ("LASH", 1), ("BORE", 1) ), 0, ref shown );
		fails += Run( Levels( ("LASH", 1), ("SEAR", 1), ("KILN", 1), ("FORK", 1), ("LINGER", 1), ("CELL", 1) ), 0, ref shown );
		fails += Run( Levels( ("LASH", 1), ("SNAP", 3) ), 0, ref shown );
		fails += Run( Levels( ("JACK", 1), ("SNAP", 1) ), 0, ref shown );
		fails += Run( Levels( ("WARHEAD", 1), ("MIRV", 1), ("BLOOM", 1), ("LANCE", 1) ), 0, ref shown );
		fails += Run( Levels( ("WARHEAD", 1), ("SPOT", 1), ("JACK", 1), ("SCORCH", 1) ), 0, ref shown );
		fails += Run( Levels( ("DRUM", 1), ("BELT", 1), ("EJECT", 1), ("DOUBLE", 1) ), 0, ref shown );
		fails += Run( Levels( ("SLUG", 1), ("SPLIT", 4), ("SLAM", 1) ), 0, ref shown );
		fails += Run( Levels( ("BUCK", 1), ("SHUCK", 1), ("SLAM", 1) ), 0, ref shown );
		fails += Run( Levels( ("MEAT", 2), ("BUCK", 3) ), 0, ref shown );
		fails += Run( Levels( ("DODGE", 3) ), 0, ref shown );
		fails += Run( Levels( ("KEEL", 1), ("BORE", 3), ("DEEP", 1), ("DRAW", 1), ("RACK", 1) ), 0, ref shown );
		fails += Run( Levels( ("LASH", 3), ("SEAR", 1), ("KILN", 1), ("CELL", 1), ("VENT", 1), ("COOL", 1) ), 0, ref shown );
		fails += Run( Levels( ("DRUM", 3), ("BELT", 1), ("FEED", 1), ("SPOOL", 1), ("SIGHT", 1) ), 0, ref shown );
		fails += Run( new Dictionary<string, int>(), 4, ref shown );

		var all = new Dictionary<string, int>();
		foreach ( var id in Pool )
			all[id] = Trinkets.Find( id ).Cap;
		fails += Run( all, 0, ref shown );
		fails += Run( all, 7, ref shown );

		var rng = new System.Random( 260926 );
		for ( var i = 0; i < 400; i++ )
		{
			var count = rng.Next( 0, 13 );
			var pick = new Dictionary<string, int>();
			while ( pick.Count < count )
			{
				var id = Pool[rng.Next( Pool.Length )];
				pick[id] = rng.Next( 1, Trinkets.Find( id ).Cap + 1 );
			}

			fails += Run( pick, rng.Next( 0, 4 ), ref shown );
		}

		fails += Blocked( new HashSet<string>(), ref shown );
		fails += Blocked( Set( "BUCK" ), ref shown );
		fails += Blocked( Set( "BUCK", "SHUCK" ), ref shown );
		fails += Blocked( Set( "WARHEAD", "MIRV" ), ref shown );
		fails += Blocked( Set( "WARHEAD", "LANCE" ), ref shown );
		fails += Blocked( Set( "BORE", "KEEL" ), ref shown );
		fails += Blocked( Set( "BORE", "DEEP" ), ref shown );
		fails += Blocked( Set( "DRUM", "BELT" ), ref shown );
		fails += Blocked( Set( "DRUM", "SPOOL" ), ref shown );
		fails += Blocked( Set( "LASH", "SEAR" ), ref shown );
		fails += Blocked( Set( "LASH", "ARC" ), ref shown );
		fails += Blocked( Set( "BORE", "LASH" ), ref shown );
		fails += Blocked( Set( "LASH", "VENT" ), ref shown );
		fails += Blocked( Set( "DRUM", "FEED" ), ref shown );
		fails += Blocked( Set( "WARHEAD", "JACK" ), ref shown );
		fails += Blocked( Set( "BORE", "RACK" ), ref shown );
		for ( var i = 0; i < 200; i++ )
		{
			var owned = new HashSet<string>();
			var count = rng.Next( 0, 11 );
			while ( owned.Count < count )
				owned.Add( Pool[rng.Next( Pool.Length )] );
			fails += Blocked( owned, ref shown );
		}

		foreach ( var id in Off )
		{
			if ( !Trinkets.Blocked( Trinkets.Find( id ), new RunLoadout() ) )
			{
				fails++;
				if ( shown < 12 )
				{
					Log.Warning( $"trinket parity {id} should stay out of the pool" );
					shown++;
				}
			}
		}

		if ( fails == 0 )
			Log.Info( "trinket parity ok" );
		else
			Log.Warning( $"trinket parity {fails} mismatches" );
	}

	static readonly string[] Pool =
	{
		"SPLIT", "FAN", "CHOKE", "MEAT", "RICO", "DOUBLE", "KICK", "STUN", "SLUG",
		"BUCK", "BORE", "DRUM", "WARHEAD", "LASH", "PIN", "RUSH", "DODGE", "SNAP",
		"MIRV", "BLOOM", "SCORCH", "LANCE", "CRATER", "SPOT",
		"DEEP", "AWL", "RAM", "KEEL",
		"BELT", "WALK", "SPOOL", "SIGHT", "BITE",
		"SEAR", "KILN", "ARC", "FORK", "SHUNT", "LINGER", "CELL",
		"JACK", "SLAP", "RACK", "DRAW", "FEED", "EJECT", "VENT", "COOL", "SHUCK", "SLAM"
	};

	static readonly string[] Off = { "LINK", "PUMP", "LOAD", "GAPE", "HEAP", "WASTE", "BREACH", "MASS", "TRACE" };

	static readonly Tier4 SplitPellets = new( 1f, 2f, 4f, 7f );
	static readonly Tier4 SplitReload = new( 0f, 0.15f, 0.25f, 0.5f );
	static readonly Tier4 BuckPellets = new( 2f, 3f, 5f );
	static readonly Tier4 BuckCone = new( 10f, 16f, 24f );
	static readonly Tier4 BuckRange = new( 0.83f, 0.94f, 1f );
	static readonly Tier4 BorePierce = new( 1f, 1f, 2f );
	static readonly Tier4 DrumBurst = new( 3f, 4f, 6f );
	static readonly Tier4 WarheadRadius = new( 90f, 126f, 176f );
	static readonly Tier4 WarheadSpeed = new( 0.78f, 0.68f, 0.58f );
	static readonly Tier4 LashTick = new( 0.6f, 0.4f, 0.2f );
	static readonly Tier4 LashTicks = new( 4f, 6f, 8f );
	static readonly Tier4 PinNails = new( 2f, 3f, 5f );
	static readonly Tier4 PinBounce = new( 1f, 2f, 3f );
	static readonly Tier4 PinCone = new( 8f, 10f, 12f );
	static readonly Tier4 RushSpeed = new( 1.2f, 1.4f, 1.65f );
	static readonly Tier4 DodgeChance = new( 0.1f, 0.2f, 0.32f );
	static readonly Tier4 SnapReload = new( 0.8f, 0.64f, 0.5f );

	const float FanCone = 14f;
	const float ChokeCone = 10f;
	const float ChokeFloor = 6f;
	const float MeatRange = 140f;
	const float MeatBonus = 1f;
	const float MeatCut = 1f;
	const int RicoBounces = 1;
	const float DoubleGap = 0.12f;
	const float DoubleReload = 0.35f;
	const float KickForce = 110f;
	const float KickRange = 180f;
	const float StunTime = 0.45f;
	const float StunRange = 160f;
	const float SlugRadius = 22f;
	const int SlugDamage = 2;
	const float SlugPad = 120f;
	const float BoreReload = 0.35f;
	const float DrumReload = 0.45f;
	const float MirvRadius = 0.55f;
	const float MirvSpeed = 0.8f;
	const float BloomRadius = 80f;
	const float BloomReload = 0.2f;
	const int ScorchDamage = 2;
	const float ScorchSpeed = 0.75f;
	const float ScorchReload = 0.12f;
	const int LanceDamage = 2;
	const float LanceRadius = 0.7f;
	const float LanceSpeed = 0.7f;
	const float LanceReload = 0.3f;
	const float CraterBody = 22f;
	const float CraterSplash = 56f;
	const float CraterSpeed = 0.65f;
	const float SpotSpeed = 0.85f;
	const float SpotSplash = 40f;
	const int SpotSplashDamage = 1;
	const int DeepPierce = 2;
	const float DeepReload = 0.25f;
	const float AwlSpeed = 0.85f;
	const float AwlReload = 0.12f;
	const float RamSpeed = 0.75f;
	const int KeelDamage = 5;
	const float KeelSpeed = 0.6f;
	const int BeltBurst = 3;
	const float BeltReload = 0.3f;
	const float WalkCone = 3f;
	const float WalkReload = 0.12f;
	const float SpoolCycle = 0.65f;
	const float SpoolReload = 0.15f;
	const float SightSpeed = 0.9f;
	const float BiteSpeed = 0.8f;
	const float LashReload = 0.3f;
	const int LashHit = 1;
	const float SearReload = 0.1f;
	const float KilnTick = 0.75f;
	const float KilnWidth = 0.75f;
	const float KilnReload = 0.06f;
	const float ArcRange = 220f;
	const float ArcTick = 1.15f;
	const float ForkReload = 0.08f;
	const float ShuntTick = 1.25f;
	const float ShuntWidth = 0.85f;
	const float LingerReload = 0.12f;
	const int CellTicks = 2;
	const float CellReload = 0.1f;
	const float JackReload = 0.8f;
	const float JackSplash = 0.8f;
	const float SlapReload = 0.85f;
	const float SlapSpeed = 0.85f;
	const float RackReload = 0.75f;
	const float RackSpeed = 0.85f;
	const float DrawReload = 0.85f;
	const int DrawPierce = 1;
	const float FeedReload = 0.75f;
	const float FeedCycle = 1.2f;
	const float EjectReload = 0.85f;
	const int EjectBurst = 1;
	const float VentReload = 0.8f;
	const float VentTick = 1.2f;
	const float CoolReload = 0.85f;
	const float CoolWidth = 0.8f;
	const float ShuckReload = 0.8f;
	const float ShuckCone = 8f;
	const float SlamReload = 0.85f;
	const int SlamPellets = 1;
	const float PinRadius = 6f;
	const float PinStick = 0.6f;
	const float RushReload = 0.12f;

	static int Run( Dictionary<string, int> levels, int bonus, ref int shown )
	{
		var live = Build( levels, bonus ).Recipe();
		var old = Legacy( levels, bonus );
		var bad = new List<string>();
		Flag( bad, "Beam", live.Beam, old.Beam );
		Flag( bad, "Auto", live.Auto, old.Auto );
		Flag( bad, "DoublePump", live.DoublePump, old.DoublePump );
		Whole( bad, "Count", live.Count, old.Count );
		Float( bad, "Cone", live.Cone, old.Cone );
		Whole( bad, "Damage", live.Damage, old.Damage );
		Whole( bad, "Pierce", live.Pierce, old.Pierce );
		Whole( bad, "Bounces", live.Bounces, old.Bounces );
		Float( bad, "Energy", live.Energy, old.Energy );
		Float( bad, "SpeedScale", live.SpeedScale, old.SpeedScale );
		Float( bad, "SpinSpeed", live.SpinSpeed, old.SpinSpeed );
		Float( bad, "Radius", live.Radius, old.Radius );
		Float( bad, "Splash", live.Splash, old.Splash );
		Whole( bad, "SplashDamage", live.SplashDamage, old.SplashDamage );
		Flag( bad, "FriendlySplash", live.FriendlySplash, old.FriendlySplash );
		Flag( bad, "PerPelletSplash", live.PerPelletSplash, old.PerPelletSplash );
		Flag( bad, "PointAim", live.PointAim, old.PointAim );
		Flag( bad, "IgnoreArmor", live.IgnoreArmor, old.IgnoreArmor );
		Flag( bad, "RampPierce", live.RampPierce, old.RampPierce );
		Flag( bad, "Nail", live.Nail, old.Nail );
		Float( bad, "StickTime", live.StickTime, old.StickTime );
		Float( bad, "RangeCut", live.RangeCut, old.RangeCut );
		Float( bad, "RangePad", live.RangePad, old.RangePad );
		Float( bad, "Falloff", live.Falloff, old.Falloff );
		Float( bad, "MeatRange", live.MeatRange, old.MeatRange );
		Whole( bad, "MeatBonus", live.MeatBonus, old.MeatBonus );
		Float( bad, "KickForce", live.KickForce, old.KickForce );
		Float( bad, "KickRange", live.KickRange, old.KickRange );
		Float( bad, "StunTime", live.StunTime, old.StunTime );
		Float( bad, "StunRange", live.StunRange, old.StunRange );
		Float( bad, "Cycle", live.Cycle, old.Cycle );
		Whole( bad, "Burst", live.Burst, old.Burst );
		Float( bad, "WalkStep", live.WalkStep, old.WalkStep );
		Flag( bad, "Sight", live.Sight, old.Sight );
		Flag( bad, "Bite", live.Bite, old.Bite );
		Flag( bad, "CommitBurst", live.CommitBurst, old.CommitBurst );
		Float( bad, "Reload", live.Reload, old.Reload );
		Float( bad, "BoreWait", live.BoreWait, old.BoreWait );
		Float( bad, "BeamPad", live.BeamPad, old.BeamPad );
		Float( bad, "BeamPerSecond", live.BeamPerSecond, old.BeamPerSecond );
		Float( bad, "BeamMaxHold", live.BeamMaxHold, old.BeamMaxHold );
		Whole( bad, "BeamHit", live.BeamHit, old.BeamHit );
		Float( bad, "BeamTick", live.BeamTick, old.BeamTick );
		Whole( bad, "BeamTicks", live.BeamTicks, old.BeamTicks );
		Float( bad, "BeamRange", live.BeamRange, old.BeamRange );
		Float( bad, "BeamWidth", live.BeamWidth, old.BeamWidth );
		Whole( bad, "BeamRank", live.BeamRank, old.BeamRank );
		Flag( bad, "BeamSear", live.BeamSear, old.BeamSear );
		Float( bad, "BeamKiln", live.BeamKiln, old.BeamKiln );
		Float( bad, "BeamArc", live.BeamArc, old.BeamArc );
		Flag( bad, "BeamFork", live.BeamFork, old.BeamFork );
		Flag( bad, "BeamShunt", live.BeamShunt, old.BeamShunt );
		Flag( bad, "BeamLinger", live.BeamLinger, old.BeamLinger );
		Float( bad, "Dodge", live.Dodge, old.Dodge );
		Float( bad, "Gap", live.Gap, old.Gap );

		if ( bad.Count == 0 )
			return 0;

		if ( shown < 12 )
		{
			Log.Warning( "trinket parity recipe " + string.Join( ", ", levels.Select( pair => $"{pair.Key}{pair.Value}" ) ) + $" bonus {bonus}" );
			foreach ( var line in bad )
				Log.Warning( "  " + line );
			shown++;
		}

		return bad.Count;
	}

	static int Blocked( HashSet<string> owned, ref int shown )
	{
		var levels = new Dictionary<string, int>();
		foreach ( var id in owned )
			levels[id] = 1;

		var loadout = Build( levels, 0 );
		var fails = 0;
		foreach ( var id in Pool )
		{
			var live = Trinkets.Blocked( Trinkets.Find( id ), loadout );
			var old = LegacyBlocked( id, owned );
			if ( live == old )
				continue;

			fails++;
			if ( shown < 12 )
			{
				Log.Warning( $"trinket parity blocked {id} live {live} legacy {old}" );
				shown++;
			}
		}

		return fails;
	}

	static RunLoadout Build( Dictionary<string, int> levels, int bonus )
	{
		var loadout = new RunLoadout { BonusDamage = bonus };
		foreach ( var pair in levels )
		{
			var card = Trinkets.Find( pair.Key );
			for ( var i = 0; i < pair.Value; i++ )
				loadout.Install( card );
		}

		return loadout;
	}

	static void Flag( List<string> bad, string name, bool live, bool old )
	{
		if ( live != old )
			bad.Add( $"{name} live {live} legacy {old}" );
	}

	static void Whole( List<string> bad, string name, int live, int old )
	{
		if ( live != old )
			bad.Add( $"{name} live {live} legacy {old}" );
	}

	static void Float( List<string> bad, string name, float live, float old )
	{
		if ( MathF.Abs( live - old ) > 0.001f )
			bad.Add( $"{name} live {live} legacy {old}" );
	}

	static int Lv( Dictionary<string, int> levels, string id )
	{
		for ( var i = 0; i < Off.Length; i++ )
		{
			if ( Off[i] == id )
				return 0;
		}

		return levels.TryGetValue( id, out var level ) ? level : 0;
	}

	static bool Has( Dictionary<string, int> levels, string id ) => Lv( levels, id ) > 0;

	static GunRecipe Legacy( Dictionary<string, int> levels, int bonus )
	{
		var t = GameSettings.Traits;
		var buck = Lv( levels, "BUCK" );
		var bore = Lv( levels, "BORE" );
		var drum = Lv( levels, "DRUM" );
		var warhead = Lv( levels, "WARHEAD" );
		var lash = Lv( levels, "LASH" );
		var pin = Lv( levels, "PIN" );
		var rush = Lv( levels, "RUSH" );
		var split = Lv( levels, "SPLIT" );
		var slug = Has( levels, "SLUG" );
		var meat = Lv( levels, "MEAT" );
		var count = 1;
		if ( split > 0 )
			count += Math.Max( 0, (int)SplitPellets.At( split ) );
		if ( buck > 0 )
			count += Math.Max( 0, (int)BuckPellets.At( buck ) - 1 );

		var full = count;
		if ( count <= 1 && pin > 0 )
			full = Math.Max( 1, (int)PinNails.At( pin ) );

		var cone = 0f;
		if ( Has( levels, "FAN" ) )
			cone += FanCone;
		if ( Has( levels, "CHOKE" ) )
			cone = MathF.Max( ChokeFloor, cone - ChokeCone );
		if ( buck > 0 )
			cone += BuckCone.At( buck );

		if ( slug )
		{
			count = 1;
			cone = 0f;
		}
		else if ( count <= 1 && pin > 0 )
		{
			count = full;
			cone = PinCone.At( pin );
		}

		if ( Has( levels, "SHUCK" ) )
			cone += ShuckCone;
		if ( Has( levels, "SLAM" ) )
		{
			count = Math.Max( 1, count - SlamPellets );
			full = Math.Max( 1, full - SlamPellets );
		}

		var bounces = t.MaxBouncesBase;
		if ( pin > 0 )
			bounces += (int)PinBounce.At( pin );
		if ( Has( levels, "RICO" ) )
			bounces += RicoBounces;
		if ( lash > 0 || Has( levels, "LANCE" ) || Has( levels, "CRATER" ) || Has( levels, "SPOT" ) || Has( levels, "KEEL" ) )
			bounces = 0;

		var pierce = bore <= 0 ? 0 : (int)BorePierce.At( bore );
		if ( Has( levels, "DEEP" ) )
			pierce += DeepPierce;
		if ( Has( levels, "DRAW" ) )
			pierce = Math.Max( 0, pierce - DrawPierce );

		var reload = t.ReloadBase;
		var boreWait = 0f;
		if ( bore > 0 )
		{
			boreWait = BoreReload * Progression.TraitMul( bore );
			reload += boreWait;
		}
		if ( drum > 0 )
			reload += DrumReload * Progression.TraitMul( drum );
		if ( rush > 0 )
			reload += RushReload * Progression.TraitMul( rush );
		if ( split > 0 )
			reload += SplitReload.At( split );
		if ( Has( levels, "DOUBLE" ) )
			reload += DoubleReload;
		if ( Has( levels, "BLOOM" ) )
			reload += BloomReload;
		if ( Has( levels, "SCORCH" ) )
			reload += ScorchReload;
		if ( Has( levels, "LANCE" ) )
			reload += LanceReload;
		if ( Has( levels, "DEEP" ) )
			reload += DeepReload;
		if ( Has( levels, "AWL" ) )
			reload += AwlReload;
		if ( Has( levels, "BELT" ) )
			reload += BeltReload;
		if ( Has( levels, "WALK" ) )
			reload += WalkReload;
		if ( Has( levels, "SPOOL" ) )
			reload += SpoolReload;

		if ( lash > 0 )
		{
			reload = LashReload;
			if ( Has( levels, "SEAR" ) )
				reload += SearReload;
			if ( Has( levels, "KILN" ) )
				reload += KilnReload;
			if ( Has( levels, "FORK" ) )
				reload += ForkReload;
			if ( Has( levels, "LINGER" ) )
				reload += LingerReload;
			if ( Has( levels, "CELL" ) )
				reload += CellReload;
		}

		if ( Has( levels, "JACK" ) )
			reload *= JackReload;
		if ( Has( levels, "SLAP" ) )
			reload *= SlapReload;
		if ( Has( levels, "RACK" ) )
			reload *= RackReload;
		if ( Has( levels, "DRAW" ) )
			reload *= DrawReload;
		if ( Has( levels, "FEED" ) )
			reload *= FeedReload;
		if ( Has( levels, "EJECT" ) )
			reload *= EjectReload;
		if ( Has( levels, "VENT" ) )
			reload *= VentReload;
		if ( Has( levels, "COOL" ) )
			reload *= CoolReload;
		if ( Has( levels, "SHUCK" ) )
			reload *= ShuckReload;
		if ( Has( levels, "SLAM" ) )
			reload *= SlamReload;

		var snap = Lv( levels, "SNAP" );
		if ( snap > 0 )
			reload *= SnapReload.At( snap );

		var speed = 1f;
		if ( warhead > 0 )
			speed *= WarheadSpeed.At( warhead );
		if ( Has( levels, "MIRV" ) )
			speed *= MirvSpeed;
		if ( Has( levels, "SCORCH" ) )
			speed *= ScorchSpeed;
		if ( Has( levels, "LANCE" ) )
			speed *= LanceSpeed;
		if ( Has( levels, "CRATER" ) )
			speed *= CraterSpeed;
		if ( Has( levels, "SPOT" ) )
			speed *= SpotSpeed;
		if ( rush > 0 )
			speed *= RushSpeed.At( rush );
		if ( Has( levels, "AWL" ) )
			speed *= AwlSpeed;
		if ( Has( levels, "RAM" ) )
			speed *= RamSpeed;
		if ( Has( levels, "KEEL" ) )
			speed *= KeelSpeed;
		if ( Has( levels, "SIGHT" ) )
			speed *= SightSpeed;
		if ( Has( levels, "BITE" ) )
			speed *= BiteSpeed;
		if ( Has( levels, "SLAP" ) )
			speed *= SlapSpeed;
		if ( Has( levels, "RACK" ) )
			speed *= RackSpeed;

		var rangeCut = 0f;
		var meatRange = 0f;
		var meatBonus = 0;
		if ( meat > 0 )
		{
			meatRange = MathF.Max( meatRange, MeatRange );
			meatBonus += (int)MeatBonus * meat;
			rangeCut += MeatCut * meat;
		}
		if ( buck > 0 )
			rangeCut += BuckRange.At( buck );

		var damage = Math.Max( 1, t.BaseDamage + bonus );
		if ( slug )
			damage += SlugDamage * Math.Max( 0, full - count );
		if ( Has( levels, "LANCE" ) )
			damage += LanceDamage;
		if ( Has( levels, "KEEL" ) )
			damage += KeelDamage;

		var radius = pin > 0 ? PinRadius : t.ProjectileRadius;
		if ( slug )
			radius = MathF.Max( radius, SlugRadius );
		if ( Has( levels, "CRATER" ) )
			radius = MathF.Max( radius, CraterBody );

		var splash = WarheadRadius.At( warhead );
		if ( Has( levels, "MIRV" ) )
			splash *= MirvRadius;
		if ( Has( levels, "BLOOM" ) )
			splash += BloomRadius;
		if ( Has( levels, "LANCE" ) )
			splash *= LanceRadius;
		if ( Has( levels, "CRATER" ) )
			splash += CraterSplash;
		if ( Has( levels, "SPOT" ) )
			splash += SpotSplash;
		if ( Has( levels, "JACK" ) )
			splash *= JackSplash;

		var splashDamage = splash > 1f ? 1 : 0;
		if ( Has( levels, "SCORCH" ) && splash > 1f )
			splashDamage = Math.Max( splashDamage, ScorchDamage );
		if ( Has( levels, "SPOT" ) && splash > 1f )
			splashDamage += SpotSplashDamage;

		var cycle = t.DrumCycle;
		if ( Has( levels, "SPOOL" ) )
			cycle *= SpoolCycle;
		if ( Has( levels, "FEED" ) )
			cycle *= FeedCycle;

		var burst = drum <= 0 ? 1 : Math.Max( 1, (int)DrumBurst.At( drum ) );
		if ( drum > 0 && Has( levels, "BELT" ) )
			burst += BeltBurst;
		if ( drum > 0 && Has( levels, "EJECT" ) )
			burst = Math.Max( 1, burst - EjectBurst );

		var beamTicks = 0;
		var beamTick = 1f;
		if ( lash > 0 )
		{
			beamTicks = Math.Max( 1, (int)LashTicks.At( lash ) );
			if ( Has( levels, "CELL" ) )
				beamTicks += CellTicks;
			beamTick = LashTick.At( lash );
			if ( Has( levels, "ARC" ) )
				beamTick *= ArcTick;
			if ( Has( levels, "SHUNT" ) )
				beamTick *= ShuntTick;
			if ( Has( levels, "VENT" ) )
				beamTick *= VentTick;
		}

		var width = t.LashWidth;
		if ( Has( levels, "KILN" ) )
			width *= KilnWidth;
		if ( Has( levels, "SHUNT" ) )
			width *= ShuntWidth;
		if ( Has( levels, "COOL" ) )
			width *= CoolWidth;

		var beam = lash > 0;
		var auto = drum > 0 && lash <= 0;
		return new GunRecipe
		{
			Beam = beam,
			Auto = auto,
			DoublePump = Has( levels, "DOUBLE" ) && drum <= 0 && lash <= 0,
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
			FriendlySplash = warhead > 0 && !Has( levels, "LANCE" ),
			PerPelletSplash = Has( levels, "MIRV" ),
			PointAim = Has( levels, "SPOT" ),
			IgnoreArmor = Has( levels, "AWL" ),
			RampPierce = Has( levels, "RAM" ),
			Nail = pin > 0 && !slug,
			StickTime = pin > 0 && !slug ? PinStick : 0f,
			RangeCut = MathF.Max( 0f, rangeCut ),
			RangePad = slug ? SlugPad : 0f,
			Falloff = 0f,
			MeatRange = meatRange,
			MeatBonus = meatBonus,
			KickForce = Has( levels, "KICK" ) ? KickForce : 0f,
			KickRange = KickRange,
			StunTime = Has( levels, "STUN" ) ? StunTime : 0f,
			StunRange = StunRange,
			Cycle = cycle,
			Burst = burst,
			WalkStep = Has( levels, "WALK" ) ? WalkCone : 0f,
			Sight = Has( levels, "SIGHT" ),
			Bite = Has( levels, "BITE" ),
			CommitBurst = false,
			Reload = MathF.Max( t.ReloadMin, reload ),
			BoreWait = boreWait,
			BeamPad = t.LashPad,
			BeamPerSecond = t.LashPerSecond,
			BeamMaxHold = t.LashMaxHold,
			BeamHit = lash > 0 ? Math.Max( 1, LashHit ) : 0,
			BeamTick = beamTick,
			BeamTicks = beamTicks,
			BeamRange = t.LashRange,
			BeamWidth = width,
			BeamRank = lash,
			BeamSear = Has( levels, "SEAR" ),
			BeamKiln = Has( levels, "KILN" ) ? KilnTick : 1f,
			BeamArc = Has( levels, "ARC" ) ? ArcRange : 0f,
			BeamFork = Has( levels, "FORK" ),
			BeamShunt = Has( levels, "SHUNT" ),
			BeamLinger = Has( levels, "LINGER" ),
			Dodge = Has( levels, "DODGE" ) ? DodgeChance.At( Lv( levels, "DODGE" ) ) : 0f,
			Gap = Has( levels, "DOUBLE" ) ? DoubleGap : 0f
		};
	}

	static bool LegacyBlocked( string id, HashSet<string> owned )
	{
		for ( var i = 0; i < Off.Length; i++ )
		{
			if ( Off[i] == id )
				return true;
		}

		bool Owns( params string[] group )
		{
			foreach ( var item in group )
			{
				if ( owned.Contains( item ) )
					return true;
			}

			return false;
		}

		if ( (id == "SHUCK" || id == "SLAM") && !owned.Contains( "BUCK" ) )
			return true;
		if ( (id is "MIRV" or "BLOOM" or "SCORCH" or "LANCE" or "CRATER" or "SPOT" or "JACK" or "SLAP") && !owned.Contains( "WARHEAD" ) )
			return true;
		if ( id == "LASH" && owned.Contains( "BORE" ) )
			return true;
		if ( id == "BORE" && owned.Contains( "LASH" ) )
			return true;
		if ( (id is "MIRV" or "BLOOM" or "SCORCH") && Owns( "LANCE", "CRATER" ) )
			return true;
		if ( (id is "LANCE" or "CRATER") && Owns( "MIRV", "BLOOM", "SCORCH" ) )
			return true;
		if ( (id is "DEEP" or "AWL" or "RAM" or "KEEL" or "RACK" or "DRAW") && !owned.Contains( "BORE" ) )
			return true;
		if ( (id is "DEEP" or "AWL" or "RAM") && owned.Contains( "KEEL" ) )
			return true;
		if ( id == "KEEL" && Owns( "DEEP", "AWL", "RAM" ) )
			return true;
		if ( (id is "BELT" or "WALK" or "SPOOL" or "SIGHT" or "BITE" or "FEED" or "EJECT") && !owned.Contains( "DRUM" ) )
			return true;
		if ( (id is "BELT" or "WALK") && Owns( "SPOOL", "SIGHT", "BITE" ) )
			return true;
		if ( (id is "SPOOL" or "SIGHT" or "BITE") && Owns( "BELT", "WALK" ) )
			return true;
		if ( (id is "SEAR" or "KILN" or "ARC" or "FORK" or "SHUNT" or "LINGER" or "CELL" or "VENT" or "COOL") && !owned.Contains( "LASH" ) )
			return true;
		if ( (id is "SEAR" or "KILN") && Owns( "ARC", "FORK" ) )
			return true;
		if ( (id is "ARC" or "FORK") && Owns( "SEAR", "KILN" ) )
			return true;
		if ( id == "SLAM" && !owned.Contains( "SHUCK" ) )
			return true;
		if ( id == "SLAP" && !owned.Contains( "JACK" ) )
			return true;
		if ( id == "DRAW" && !owned.Contains( "RACK" ) )
			return true;
		if ( id == "EJECT" && !owned.Contains( "FEED" ) )
			return true;
		if ( id == "COOL" && !owned.Contains( "VENT" ) )
			return true;
		return false;
	}

	static Dictionary<string, int> Levels( params (string Id, int Level)[] items )
	{
		var levels = new Dictionary<string, int>();
		foreach ( var item in items )
			levels[item.Id] = item.Level;
		return levels;
	}

	static HashSet<string> Set( params string[] ids ) => new( ids );

	readonly struct Tier4
	{
		public readonly float A;
		public readonly float B;
		public readonly float C;
		public readonly float D;

		public Tier4( float a, float b, float c, float d = 0f )
		{
			A = a;
			B = b;
			C = c;
			D = d;
		}

		public float At( int level )
		{
			if ( level >= 4 && D != 0f )
				return D;

			return Progression.Tier( level, A, B, C );
		}
	}
}
