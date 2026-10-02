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
		fails += Run( Levels( ("BUCK", 3), ("SLUG", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1) ), 0, ref shown );
		fails += Run( Levels( ("PIN", 1), ("SLUG", 1) ), 0, ref shown );
		fails += Run( Levels( ("ELECTRIFY", 1), ("BORE", 1) ), 0, ref shown );
		fails += Run( Levels( ("ELECTRIFY", 1), ("FOCUS", 1), ("LINGER", 1) ), 0, ref shown );
		fails += Run( Levels( ("ELECTRIFY", 1), ("RELOADER", 3) ), 0, ref shown );
		fails += Run( Levels( ("WARHEAD", 1), ("CASSETTE", 1), ("BLOOM", 1), ("IGNORANCE", 1) ), 0, ref shown );
		fails += Run( Levels( ("DRUM", 1) ), 0, ref shown );
		fails += Run( Levels( ("SLUG", 1), ("BUCK", 3) ), 0, ref shown );
		fails += Run( Levels( ("DODGE", 3) ), 0, ref shown );
		fails += Run( Levels( ("BORE", 3) ), 0, ref shown );
		fails += Run( Levels( ("ELECTRIFY", 3), ("FOCUS", 1) ), 0, ref shown );
		fails += Run( Levels( ("DRUM", 3) ), 0, ref shown );
		fails += Run( Levels( ("PINBALL", 3), ("RICO", 3) ), 0, ref shown );
		fails += Run( Levels( ("RICO", 2), ("BUCK", 3) ), 0, ref shown );
		fails += Run( Levels( ("PINBALL", 2) ), 0, ref shown );
		fails += Run( Levels( ("KICK", 1) ), 0, ref shown );
		fails += Run( Levels( ("KICK", 3) ), 0, ref shown );
		fails += Run( Levels( ("BULK", 1) ), 0, ref shown );
		fails += Run( Levels( ("BULK", 3) ), 0, ref shown );
		fails += Run( Levels( ("SLUG", 1), ("BULK", 3) ), 0, ref shown );
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
		fails += Blocked( Set( "BORE" ), ref shown );
		fails += Blocked( Set( "BUCK", "BORE" ), ref shown );
		fails += Blocked( Set( "WARHEAD", "CASSETTE" ), ref shown );
		fails += Blocked( Set( "IGNORANCE" ), ref shown );
		fails += Blocked( Set( "ELECTRIFY", "FOCUS" ), ref shown );
		fails += Blocked( Set( "ELECTRIFY", "ARC" ), ref shown );
		fails += Blocked( Set( "BORE", "ELECTRIFY" ), ref shown );
		fails += Blocked( Set( "DRUM", "ELECTRIFY" ), ref shown );
		fails += Blocked( Set( "DRUM", "FRENZY" ), ref shown );
		fails += Blocked( Set( "FETCH" ), ref shown );
		fails += Blocked( Set( "ELECTRIFY", "FETCH" ), ref shown );
		fails += Blocked( Set( "RETURN" ), ref shown );
		fails += Blocked( Set( "ELECTRIFY", "RETURN" ), ref shown );
		fails += Blocked( Set( "FETCH", "RETURN" ), ref shown );
		fails += Blocked( Set( "GHOST" ), ref shown );
		fails += Blocked( Set( "ELECTRIFY", "GHOST" ), ref shown );
		fails += Blocked( Set( "WARHEAD", "PINBALL" ), ref shown );
		for ( var i = 0; i < 200; i++ )
		{
			var owned = new HashSet<string>();
			var count = rng.Next( 0, 11 );
			while ( owned.Count < count )
				owned.Add( Pool[rng.Next( Pool.Length )] );
			fails += Blocked( owned, ref shown );
		}

		if ( fails == 0 )
			Log.Info( "trinket parity ok" );
		else
			Log.Warning( $"trinket parity {fails} mismatches" );
	}

	static readonly string[] Pool =
	{
		"RICO", "KICK", "BULK", "STUN", "SLUG",
		"BUCK", "BORE", "DRUM", "WARHEAD", "ELECTRIFY", "PIN", "RUSH", "DODGE", "RELOADER",
		"CASSETTE", "BLOOM", "IGNORANCE",
		"RAM",
		"FRENZY",
		"FOCUS", "ARC", "SHUNT", "LINGER",
		"PINBALL",
		"FETCH", "RETURN", "GHOST",
		"TURRET"
	};

	static readonly Tier4 BuckPellets = new( 2f, 3f, 5f );
	static readonly Tier4 BuckCone = new( 10f, 16f, 24f );
	static readonly Tier4 BuckRange = new( 0.83f, 0.94f, 1f );
	static readonly Tier4 BorePierce = new( 1f, 2f, 3f );
	static readonly Tier4 DrumBurst = new( 3f, 4f, 6f );
	static readonly Tier4 WarheadRadius = new( 90f, 126f, 176f );
	static readonly Tier4 WarheadSpeed = new( 0.78f, 0.68f, 0.58f );
	static readonly Tier4 ElectrifyTick = new( 0.6f, 0.4f, 0.2f );
	static readonly Tier4 ElectrifyTicks = new( 4f, 6f, 8f );
	static readonly Tier4 RushSpeed = new( 1.2f, 1.4f, 1.65f );
	static readonly Tier4 ReturnSpeed = new( 1.35f, 1.7f, 2.15f );
	static readonly Tier4 DodgeChance = new( 0.1f, 0.2f, 0.32f );
	static readonly Tier4 ReloaderReload = new( 0.8f, 0.64f, 0.5f );
	static readonly Tier4 RicoBounces = new( 1f, 3f, 5f );
	static readonly Tier4 RicoEnergy = new( 1.25f, 1.6f, 2f );
	static readonly Tier4 PinballDamage = new( 1f, 1f, 2f );
	static readonly Tier4 PinballSpeed = new( 0.1f, 0.2f, 0.3f );

	static readonly Tier4 KickForceTiers = new( 480f, 760f, 1100f );
	static readonly Tier4 BulkRadius = new( 2f, 4f, 7f );
	const float KickRange = 1600f;
	const float StunTime = 0.45f;
	const float SlugRadius = 22f;
	const int SlugDamage = 2;
	const int SlugBounce = 4;
	const int SlugPierce = 4;
	const float SlugPad = 120f;
	const float SlugRange = 0.4f;
	const float DrumReload = 0.45f;
	static readonly Tier4 CassetteExtra = new( 1f, 2f, 3f );
	const float BloomRadius = 80f;
	const float BloomReload = 0.2f;
	const float RamSpeed = 0.9f;
	const float BiteSpeed = 0.8f;
	const float ElectrifyReload = 0.3f;
	const int ElectrifyHit = 1;
	const float SearReload = 0.1f;
	const float ArcTick = 1.15f;
	const float ShuntTick = 1.25f;
	const float ShuntWidth = 0.85f;
	const float LingerReload = 0.12f;
	const float FetchReload = 0.35f;
	static int Run( Dictionary<string, int> levels, int bonus, ref int shown )
	{
		var live = Build( levels, bonus ).Recipe();
		var old = Legacy( levels, bonus );
		var bad = new List<string>();
		Flag( bad, "Beam", live.Beam, old.Beam );
		Flag( bad, "Auto", live.Auto, old.Auto );
		Whole( bad, "Count", live.Count, old.Count );
		Float( bad, "Cone", live.Cone, old.Cone );
		Whole( bad, "Damage", live.Damage, old.Damage );
		Whole( bad, "Pierce", live.Pierce, old.Pierce );
		Whole( bad, "Bounces", live.Bounces, old.Bounces );
		Whole( bad, "BounceDamage", live.BounceDamage, old.BounceDamage );
		Float( bad, "BounceSpeed", live.BounceSpeed, old.BounceSpeed );
		Float( bad, "Energy", live.Energy, old.Energy );
		Float( bad, "SpeedScale", live.SpeedScale, old.SpeedScale );
		Float( bad, "Radius", live.Radius, old.Radius );
		Float( bad, "Splash", live.Splash, old.Splash );
		Whole( bad, "SplashDamage", live.SplashDamage, old.SplashDamage );
		Whole( bad, "ExtraSplash", live.ExtraSplash, old.ExtraSplash );
		Flag( bad, "FriendlySplash", live.FriendlySplash, old.FriendlySplash );
		Flag( bad, "PerPelletSplash", live.PerPelletSplash, old.PerPelletSplash );
		Flag( bad, "RampPierce", live.RampPierce, old.RampPierce );
		Flag( bad, "Nail", live.Nail, old.Nail );
		Float( bad, "StickTime", live.StickTime, old.StickTime );
		Float( bad, "RangeCut", live.RangeCut, old.RangeCut );
		Float( bad, "RangePad", live.RangePad, old.RangePad );
		Float( bad, "RangeMul", live.RangeMul, old.RangeMul );
		Float( bad, "Falloff", live.Falloff, old.Falloff );
		Float( bad, "KickForce", live.KickForce, old.KickForce );
		Float( bad, "KickRange", live.KickRange, old.KickRange );
		Float( bad, "StunTime", live.StunTime, old.StunTime );
		Float( bad, "StunRange", live.StunRange, old.StunRange );
		Float( bad, "Cycle", live.Cycle, old.Cycle );
		Whole( bad, "Burst", live.Burst, old.Burst );
		Float( bad, "WalkStep", live.WalkStep, old.WalkStep );
		Flag( bad, "Bite", live.Bite, old.Bite );
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
		Float( bad, "BeamArc", live.BeamArc, old.BeamArc );
		Flag( bad, "BeamShunt", live.BeamShunt, old.BeamShunt );
		Flag( bad, "BeamLinger", live.BeamLinger, old.BeamLinger );
		Float( bad, "Dodge", live.Dodge, old.Dodge );
		Flag( bad, "Fetch", live.Fetch, old.Fetch );
		Float( bad, "Pickup", live.Pickup, old.Pickup );
		Flag( bad, "Ghost", live.Ghost, old.Ghost );

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
		=> levels.TryGetValue( id, out var level ) ? level : 0;

	static bool Has( Dictionary<string, int> levels, string id ) => Lv( levels, id ) > 0;

	static GunRecipe Legacy( Dictionary<string, int> levels, int bonus )
	{
		var t = GameSettings.Traits;
		var buck = Lv( levels, "BUCK" );
		var bore = Lv( levels, "BORE" );
		var drum = Lv( levels, "DRUM" );
		var warhead = Lv( levels, "WARHEAD" );
		var electrify = Lv( levels, "ELECTRIFY" );
		var rush = Lv( levels, "RUSH" );
		var slug = Has( levels, "SLUG" );
		var pinball = Lv( levels, "PINBALL" );
		var rico = Lv( levels, "RICO" );
		var cassette = Lv( levels, "CASSETTE" );
		var kick = Lv( levels, "KICK" );
		var count = 1;
		if ( buck > 0 )
			count += Math.Max( 0, (int)BuckPellets.At( buck ) - 1 );

		var full = count;

		var cone = 0f;
		if ( buck > 0 )
			cone += BuckCone.At( buck );

		if ( slug )
		{
			count = 1;
			cone = 0f;
		}

		var bounces = t.MaxBouncesBase;
		if ( rico > 0 )
			bounces += (int)RicoBounces.At( rico );
		if ( slug )
			bounces += SlugBounce * Math.Max( 0, full - count );
		if ( electrify > 0 )
			bounces -= electrify;
		if ( Has( levels, "FETCH" ) )
			bounces = 0;

		var pierce = bore <= 0 ? 0 : (int)BorePierce.At( bore );
		if ( slug )
			pierce += SlugPierce * Math.Max( 0, full - count );

		var reload = t.ReloadBase;
		if ( drum > 0 )
			reload += DrumReload * Progression.TraitMul( drum );
		if ( Has( levels, "BLOOM" ) )
			reload += BloomReload;
		if ( Has( levels, "FETCH" ) )
			reload += FetchReload;

		if ( electrify > 0 )
		{
			reload = ElectrifyReload;
			if ( Has( levels, "FOCUS" ) )
				reload += SearReload;
			if ( Has( levels, "LINGER" ) )
				reload += LingerReload;
		}

		var reloader = Lv( levels, "RELOADER" );
		if ( reloader > 0 )
			reload *= ReloaderReload.At( reloader );

		var speed = 1f;
		if ( warhead > 0 )
			speed *= WarheadSpeed.At( warhead );
		if ( rush > 0 )
			speed *= RushSpeed.At( rush );
		if ( Has( levels, "RAM" ) )
			speed *= RamSpeed;
		if ( Has( levels, "FRENZY" ) )
			speed *= BiteSpeed;

		var rangeCut = 0f;
		if ( buck > 0 )
			rangeCut += BuckRange.At( buck );

		var damage = Math.Max( 1, t.BaseDamage + bonus );
		if ( slug )
			damage += SlugDamage * Math.Max( 0, full - count );
		var radius = t.ProjectileRadius;
		if ( slug )
			radius = MathF.Max( radius, SlugRadius );
		var bulk = Lv( levels, "BULK" );
		if ( bulk > 0 )
			radius *= BulkRadius.At( bulk );

		var splash = WarheadRadius.At( warhead );
		if ( Has( levels, "BLOOM" ) )
			splash += BloomRadius;

		var splashDamage = splash > 1f ? 1 : 0;

		var cycle = t.DrumCycle;
		var burst = drum <= 0 ? 1 : Math.Max( 1, (int)DrumBurst.At( drum ) );

		var beamTicks = 0;
		var beamTick = 1f;
		if ( electrify > 0 )
		{
			beamTicks = Math.Max( 1, (int)ElectrifyTicks.At( electrify ) );
			beamTick = ElectrifyTick.At( electrify );
			if ( Has( levels, "ARC" ) )
				beamTick *= ArcTick;
			if ( Has( levels, "SHUNT" ) )
				beamTick *= ShuntTick;
		}

		var width = t.LashWidth;
		if ( Has( levels, "SHUNT" ) )
			width *= ShuntWidth;

		var beam = electrify > 0;
		var auto = drum > 0 && electrify <= 0;
		return new GunRecipe
		{
			Beam = beam,
			Auto = auto,
			Count = count,
			Cone = cone,
			Damage = damage,
			Pierce = pierce,
			Bounces = bounces,
			BounceDamage = (int)PinballDamage.At( pinball ),
			BounceSpeed = PinballSpeed.At( pinball ),
			Energy = rico > 0 ? t.EnergyBase * RicoEnergy.At( rico ) : t.EnergyBase,
			SpeedScale = speed,
			Radius = radius,
			Splash = splash,
			SplashDamage = splashDamage,
			ExtraSplash = cassette > 0 ? (int)CassetteExtra.At( cassette ) : 0,
			FriendlySplash = warhead > 0 && !Has( levels, "IGNORANCE" ),
			PerPelletSplash = false,
			RampPierce = Has( levels, "RAM" ),
			Nail = false,
			StickTime = 0f,
			RangeCut = MathF.Max( 0f, rangeCut ),
			RangePad = slug ? SlugPad : 0f,
			RangeMul = 1f + (slug ? SlugRange * Math.Max( 0, full - count ) : 0f),
			Falloff = 0f,
			KickForce = KickForceTiers.At( kick ),
			KickRange = KickRange,
			StunTime = Has( levels, "STUN" ) ? StunTime : 0f,
			StunRange = 0f,
			Cycle = cycle,
			Burst = burst,
			WalkStep = 0f,
			Bite = Has( levels, "FRENZY" ),
			Reload = MathF.Max( t.ReloadMin, reload ),
			BoreWait = 0f,
			BeamPad = t.LashPad,
			BeamPerSecond = t.LashPerSecond,
			BeamMaxHold = t.LashMaxHold,
			BeamHit = electrify > 0 ? Math.Max( 1, ElectrifyHit ) : 0,
			BeamTick = beamTick,
			BeamTicks = beamTicks,
			BeamRange = t.LashRange,
			BeamWidth = width,
			BeamRank = electrify,
			BeamSear = Has( levels, "FOCUS" ),
			BeamArc = Lv( levels, "ARC" ),
			BeamShunt = Has( levels, "SHUNT" ),
			BeamLinger = Has( levels, "LINGER" ),
			Dodge = Has( levels, "DODGE" ) ? DodgeChance.At( Lv( levels, "DODGE" ) ) : 0f,
			Pickup = Lv( levels, "RETURN" ) > 0 ? ReturnSpeed.At( Lv( levels, "RETURN" ) ) : 1f,
			Fetch = Has( levels, "FETCH" ),
			Ghost = Has( levels, "GHOST" )
		};
	}

	static bool LegacyBlocked( string id, HashSet<string> owned )
	{
		if ( id == "RAM" && !owned.Contains( "BORE" ) )
			return true;
		if ( (id is "CASSETTE" or "BLOOM" or "IGNORANCE") && !owned.Contains( "WARHEAD" ) )
			return true;
		if ( id == "ELECTRIFY" && (owned.Contains( "DRUM" ) || owned.Contains( "FETCH" ) || owned.Contains( "GHOST" )) )
			return true;
		if ( id == "DRUM" && owned.Contains( "ELECTRIFY" ) )
			return true;
		if ( id == "FETCH" && (owned.Contains( "ELECTRIFY" ) || owned.Contains( "RETURN" )) )
			return true;
		if ( id == "RETURN" && owned.Contains( "FETCH" ) )
			return true;
		if ( id == "GHOST" && owned.Contains( "ELECTRIFY" ) )
			return true;
		if ( id == "FRENZY" && !owned.Contains( "DRUM" ) )
			return true;
		if ( id == "SLUG" && !owned.Contains( "BUCK" ) )
			return true;
		if ( (id is "FOCUS" or "ARC" or "SHUNT" or "LINGER") && !owned.Contains( "ELECTRIFY" ) )
			return true;
		if ( id == "FOCUS" && owned.Contains( "ARC" ) )
			return true;
		if ( id == "ARC" && owned.Contains( "FOCUS" ) )
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
