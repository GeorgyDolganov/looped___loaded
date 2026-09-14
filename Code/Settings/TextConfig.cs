namespace LoopedLoaded;

[AssetType( Name = "Text Config", Extension = "omrtext", Category = "Looped Loaded" )]
public class TextConfig : GameResource
{
	[Property] public HudCopy Hud { get; set; } = new();
	[Property] public PauseCopy Pause { get; set; } = new();
	[Property] public MenuCopy Menu { get; set; } = new();
	[Property] public SavesCopy Saves { get; set; } = new();
	[Property] public DecideCopy Decide { get; set; } = new();
	[Property] public ShopCopy Shop { get; set; } = new();
	[Property] public HelpCopy Help { get; set; } = new();
	[Property] public AnnounceCopy Announce { get; set; } = new();
	[Property] public CityCopy City { get; set; } = new();
	[Property] public TraitsCopy Traits { get; set; } = new();
	[Property] public BuildingsCopy Buildings { get; set; } = new();
	[Property] public PlacesCopy Places { get; set; } = new();
	[Property] public ProgressCopy Progress { get; set; } = new();

	public void Ensure()
	{
		Hud ??= new();
		Pause ??= new();
		Menu ??= new();
		Saves ??= new();
		Decide ??= new();
		Shop ??= new();
		Help ??= new();
		Announce ??= new();
		City ??= new();
		Traits ??= new();
		Traits.Rarity ??= new();
		Buildings ??= new();
		Places ??= new();
		Progress ??= new();
	}

	public string F( string template, params object[] args )
	{
		Ensure();

		if ( string.IsNullOrEmpty( template ) )
			return "";

		if ( args is null || args.Length == 0 )
			return template;

		try
		{
			return string.Format( System.Globalization.CultureInfo.InvariantCulture, template, args );
		}
		catch ( FormatException )
		{
			return template;
		}
	}

	public TraitCopy Trait( RoundTrait trait )
	{
		var pack = Traits ??= new();
		return trait switch
		{
			RoundTrait.Pierce => pack.Pierce ??= new(),
			RoundTrait.Bounce => pack.Bounce ??= new(),
			RoundTrait.Magnetic => pack.Magnetic ??= new(),
			RoundTrait.Freeze => pack.Freeze ??= new(),
			RoundTrait.Explosive => pack.Explosive ??= new(),
			RoundTrait.Heavy => pack.Heavy ??= new(),
			RoundTrait.Accel => pack.Accel ??= new(),
			RoundTrait.Electric => pack.Electric ??= new(),
			RoundTrait.Blood => pack.Blood ??= new(),
			RoundTrait.Homing => pack.Homing ??= new(),
			RoundTrait.Skim => pack.Skim ??= new(),
			RoundTrait.Kick => pack.Kick ??= new(),
			RoundTrait.Stick => pack.Stick ??= new(),
			RoundTrait.Cushion => pack.Cushion ??= new(),
			RoundTrait.Cue => pack.Cue ??= new(),
			RoundTrait.Corner => pack.Corner ??= new(),
			RoundTrait.Incurve => pack.Incurve ??= new(),
			RoundTrait.Clockwise => pack.Clockwise ??= new(),
			RoundTrait.Stutter => pack.Stutter ??= new(),
			RoundTrait.Breach => pack.Breach ??= new(),
			RoundTrait.Boomerang => pack.Boomerang ??= new(),
			RoundTrait.Reel => pack.Reel ??= new(),
			RoundTrait.Swipe => pack.Swipe ??= new(),
			RoundTrait.Backstop => pack.Backstop ??= new(),
			RoundTrait.Link => pack.Link ??= new(),
			RoundTrait.Fuse => pack.Fuse ??= new(),
			RoundTrait.Snap => pack.Snap ??= new(),
			RoundTrait.LateMag => pack.LateMag ??= new(),
			RoundTrait.Rim => pack.Rim ??= new(),
			RoundTrait.SecondWind => pack.SecondWind ??= new(),
			RoundTrait.Shred => pack.Shred ??= new(),
			RoundTrait.Hook => pack.Hook ??= new(),
			RoundTrait.Mark => pack.Mark ??= new(),
			RoundTrait.Pinball => pack.Pinball ??= new(),
			RoundTrait.Ribbon => pack.Ribbon ??= new(),
			RoundTrait.Graze => pack.Graze ??= new(),
			RoundTrait.Rehit => pack.Rehit ??= new(),
			RoundTrait.Step => pack.Step ??= new(),
			RoundTrait.Echo => pack.Echo ??= new(),
			_ => pack.Redirect ??= new()
		};
	}

	public string TraitCode( RoundTrait trait ) => Or( Trait( trait ).Code, trait.ToString().ToUpperInvariant() );
	public string TraitTitle( RoundTrait trait ) => Or( Trait( trait ).Title, trait.ToString().ToUpperInvariant() );
	public string TraitBlurb( RoundTrait trait ) => Or( Trait( trait ).Blurb, "" );

	public string RarityOf( TraitPack pack )
	{
		var rarity = (Traits ??= new()).Rarity ??= new();
		return pack switch
		{
			TraitPack.Starter => Or( rarity.Common, "COMMON" ),
			TraitPack.Geometry or TraitPack.Return => Or( rarity.Uncommon, "UNCOMMON" ),
			TraitPack.Chaos or TraitPack.Body => Or( rarity.Rare, "RARE" ),
			_ => Or( rarity.Epic, "EPIC" )
		};
	}

	public BuildingCopy Building( BuildingKind kind )
	{
		var pack = Buildings ??= new();
		return kind switch
		{
			BuildingKind.Infirmary => pack.Infirmary ??= new(),
			BuildingKind.Anvil => pack.Anvil ??= new(),
			BuildingKind.Booster => pack.Booster ??= new(),
			BuildingKind.Brake => pack.Brake ??= new(),
			_ => pack.Showcase ??= new()
		};
	}

	public string BuildingTitle( BuildingKind kind ) => Or( Building( kind ).Title, kind.ToString().ToUpperInvariant() );
	public string BuildingPayoff( BuildingKind kind ) => Or( Building( kind ).Payoff, "" );
	public string BuildingPromise( BuildingKind kind ) => F( Building( kind ).Promise, Progression.RankValue( 1 ) );
	public string BuildingBlurb( BuildingKind kind ) => Or( Building( kind ).Blurb, "" );

	public PlaceCopy Place( RunLocation location )
	{
		var pack = Places ??= new();
		return location == RunLocation.Glass ? pack.Glass ??= new() : pack.Yard ??= new();
	}

	public string PlaceCode( RunLocation location ) => Or( Place( location ).Code, location.ToString().ToUpperInvariant() );
	public string PlaceTitle( RunLocation location ) => Or( Place( location ).Title, location.ToString() );
	public string PlaceRule( RunLocation location ) => Or( Place( location ).Rule, "" );
	public string PlaceBoss( RunLocation location ) => Or( Place( location ).Boss, "" );
	public string PlaceFightCall( RunLocation location ) => Or( Place( location ).FightCall, "" );
	public string PlaceFightHint( RunLocation location ) => Or( Place( location ).FightHint, "" );
	public string PlaceArmor( RunLocation location ) => Or( Place( location ).Armor, "" );
	public string PlaceBossBlurb( RunLocation location, bool last ) => last
		? Or( Place( location ).BossLast, "" )
		: Or( Place( location ).BossKeep, "" );

	public ProgressStepCopy ProgressStep( string id )
	{
		var pack = Progress ??= new();
		return id switch
		{
			"catch" => pack.Catch ??= new(),
			"lap" => pack.Lap ??= new(),
			"extract" => pack.Extract ??= new(),
			"grow" => pack.Grow ??= new(),
			"inject" => pack.Inject ??= new(),
			"chapel" => pack.Chapel ??= new(),
			"lens" => pack.Lens ??= new(),
			"ring" => pack.Ring ??= new(),
			"core" => pack.Core ??= new(),
			_ => pack.Done ??= new()
		};
	}

	public string ProgressTitle( string id ) => Or( ProgressStep( id ).Title, id?.ToUpperInvariant() ?? "" );
	public string ProgressBlurb( string id ) => Or( ProgressStep( id ).Blurb, "" );

	public string CityModeLabel( CityMode mode )
	{
		City ??= new();
		return mode == CityMode.Build ? Or( City.ModeBuild, "BUILD" ) : Or( City.ModeShoot, "SHOOT" );
	}

	static string Or( string value, string fallback ) => string.IsNullOrWhiteSpace( value ) ? fallback : value;
}

public class TraitCopy
{
	[Property] public string Code { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
}

public class BuildingCopy
{
	[Property] public string Title { get; set; }
	[Property] public string Payoff { get; set; }
	[Property] public string Promise { get; set; }
	[Property] public string Blurb { get; set; }
}

public class PlaceCopy
{
	[Property] public string Code { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Rule { get; set; }
	[Property] public string Boss { get; set; }
	[Property] public string FightCall { get; set; }
	[Property] public string FightHint { get; set; }
	[Property] public string Armor { get; set; }
	[Property] public string BossKeep { get; set; }
	[Property] public string BossLast { get; set; }
}

public class RarityCopy
{
	[Property] public string Common { get; set; } = "COMMON";
	[Property] public string Uncommon { get; set; } = "UNCOMMON";
	[Property] public string Rare { get; set; } = "RARE";
	[Property] public string Epic { get; set; } = "EPIC";
}

public class TraitsCopy
{
	[Property] public RarityCopy Rarity { get; set; } = new();
	[Property] public TraitCopy Pierce { get; set; } = new() { Code = "PRC", Title = "PIERCE", Blurb = "Pass through enemies instead of bouncing off them." };
	[Property] public TraitCopy Bounce { get; set; } = new() { Code = "RCH", Title = "RICOCHET", Blurb = "More wall bounces and a longer flight." };
	[Property] public TraitCopy Magnetic { get; set; } = new() { Code = "MAG", Title = "MAGNET", Blurb = "Steer home near you. Wider catch zone." };
	[Property] public TraitCopy Freeze { get; set; } = new() { Code = "FRZ", Title = "FREEZE", Blurb = "Slow whatever you hit." };
	[Property] public TraitCopy Explosive { get; set; } = new() { Code = "XPL", Title = "EXPLOSIVE", Blurb = "Blast on a kill or the last bounce." };
	[Property] public TraitCopy Heavy { get; set; } = new() { Code = "HVY", Title = "HEAVY", Blurb = "Harder hits and shove. Slower flight." };
	[Property] public TraitCopy Accel { get; set; } = new() { Code = "ACL", Title = "ACCEL", Blurb = "Speed up after a bounce or a kill." };
	[Property] public TraitCopy Electric { get; set; } = new() { Code = "ELC", Title = "ELECTRIC", Blurb = "Jump lightning to nearby enemies. Course stays." };
	[Property] public TraitCopy Blood { get; set; } = new() { Code = "BLD", Title = "BLOOD", Blurb = "A kill streak on one shot grants a shield." };
	[Property] public TraitCopy Homing { get; set; } = new() { Code = "HOM", Title = "HOMING", Blurb = "After a kill or last bounce, turn home earlier." };
	[Property] public TraitCopy Skim { get; set; } = new() { Code = "SKM", Title = "SKIM", Blurb = "Glancing panel hits keep the bounce charge." };
	[Property] public TraitCopy Kick { get; set; } = new() { Code = "KCK", Title = "KICK", Blurb = "Player shots twist inner panels harder." };
	[Property] public TraitCopy Stick { get; set; } = new() { Code = "STK", Title = "STICK", Blurb = "Cling to a panel, then launch along it." };
	[Property] public TraitCopy Cushion { get; set; } = new() { Code = "CSH", Title = "CUSHION", Blurb = "The first bounce hugs the wall." };
	[Property] public TraitCopy Cue { get; set; } = new() { Code = "CUE", Title = "CUE", Blurb = "True-face banks. Preview matches the shot." };
	[Property] public TraitCopy Corner { get; set; } = new() { Code = "CNR", Title = "CORNER", Blurb = "Pull toward panel ends for bank shots." };
	[Property] public TraitCopy Incurve { get; set; } = new() { Code = "INC", Title = "INCURVE", Blurb = "Curve toward the core." };
	[Property] public TraitCopy Clockwise { get; set; } = new() { Code = "CLK", Title = "CLOCKWISE", Blurb = "Curve with your run around the ring." };
	[Property] public TraitCopy Stutter { get; set; } = new() { Code = "STT", Title = "STUTTER", Blurb = "Pause on each wall bounce." };
	[Property] public TraitCopy Breach { get; set; } = new() { Code = "BRH", Title = "BREACH", Blurb = "Punch through inner panels." };
	[Property] public TraitCopy Boomerang { get; set; } = new() { Code = "BMG", Title = "BOOMERANG", Blurb = "When spent, retrace your path home." };
	[Property] public TraitCopy Reel { get; set; } = new() { Code = "REL", Title = "REEL", Blurb = "Lost rounds crawl the ring toward you." };
	[Property] public TraitCopy Swipe { get; set; } = new() { Code = "SWP", Title = "SWIPE", Blurb = "Dash through your round to chamber it." };
	[Property] public TraitCopy Backstop { get; set; } = new() { Code = "BCK", Title = "BACKSTOP", Blurb = "Catch zone behind you too." };
	[Property] public TraitCopy Link { get; set; } = new() { Code = "LNK", Title = "LINK", Blurb = "A flying round picks up a dropped one." };
	[Property] public TraitCopy Fuse { get; set; } = new() { Code = "FUS", Title = "FUSE", Blurb = "Missed catch explodes where it drops." };
	[Property] public TraitCopy Snap { get; set; } = new() { Code = "SNP", Title = "SNAP", Blurb = "A catch primes a faster, longer-preview shot." };
	[Property] public TraitCopy LateMag { get; set; } = new() { Code = "LTM", Title = "LATE MAG", Blurb = "Magnet waits until the last bounce or a kill." };
	[Property] public TraitCopy Rim { get; set; } = new() { Code = "RIM", Title = "RIM", Blurb = "Drops always land ahead on the outer ring." };
	[Property] public TraitCopy SecondWind { get; set; } = new() { Code = "2ND", Title = "SECOND WIND", Blurb = "First stall kicks toward the ring instead." };
	[Property] public TraitCopy Shred { get; set; } = new() { Code = "SHD", Title = "SHRED", Blurb = "First shield hit counts as a rear shot." };
	[Property] public TraitCopy Hook { get; set; } = new() { Code = "HOK", Title = "HOOK", Blurb = "Hits drag the enemy along the shot." };
	[Property] public TraitCopy Mark { get; set; } = new() { Code = "MRK", Title = "MARK", Blurb = "Tagged foes pull the next round in." };
	[Property] public TraitCopy Pinball { get; set; } = new() { Code = "PNB", Title = "PINBALL", Blurb = "Bounce off bodies harder. Crowd becomes banks." };
	[Property] public TraitCopy Ribbon { get; set; } = new() { Code = "RBN", Title = "RIBBON", Blurb = "Your trail cuts and slows enemies." };
	[Property] public TraitCopy Graze { get; set; } = new() { Code = "GRZ", Title = "GRAZE", Blurb = "Near-misses still chip a hit." };
	[Property] public TraitCopy Rehit { get; set; } = new() { Code = "RHT", Title = "REHIT", Blurb = "A wall bounce lets you hit the same foe again." };
	[Property] public TraitCopy Step { get; set; } = new() { Code = "STP", Title = "STEP", Blurb = "A kill jumps the round forward." };
	[Property] public TraitCopy Echo { get; set; } = new() { Code = "ECO", Title = "ECHO", Blurb = "A ghost retraces the line and hits again." };
	[Property] public TraitCopy Redirect { get; set; } = new() { Code = "RDR", Title = "REDIRECT", Blurb = "A kill turns you toward the next body in cone." };
}

public class BuildingsCopy
{
	[Property] public BuildingCopy Infirmary { get; set; } = new()
	{
		Title = "INFIRMARY",
		Payoff = "+HP",
		Promise = "+{0} HP AT RANK 1",
		Blurb = "Raises max hearts. Same neighbor adds a rank."
	};
	[Property] public BuildingCopy Anvil { get; set; } = new()
	{
		Title = "ANVIL",
		Payoff = "+DMG",
		Promise = "+{0} DAMAGE AT RANK 1",
		Blurb = "Rounds hit harder. Same neighbor adds a rank."
	};
	[Property] public BuildingCopy Booster { get; set; } = new()
	{
		Title = "BOOSTER",
		Payoff = "DASH",
		Promise = "SHORTER DASH COOLDOWN",
		Blurb = "Dash returns faster each working rank."
	};
	[Property] public BuildingCopy Brake { get; set; } = new()
	{
		Title = "BRAKE",
		Payoff = "SLOW",
		Promise = "UNLOCKS THE SLOW METER",
		Blurb = "Unlocks slow. Higher rank drains slower."
	};
	[Property] public BuildingCopy Showcase { get; set; } = new()
	{
		Title = "SHOWCASE",
		Payoff = "+CARD",
		Promise = "+1 UPGRADE CARD AFTER EACH LAP",
		Blurb = "One extra upgrade card after every lap."
	};
}

public class ProgressStepCopy
{
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
}

public class ProgressCopy
{
	[Property] public ProgressStepCopy Catch { get; set; } = new()
	{
		Title = "CATCH IT BACK",
		Blurb = "Fire a round. Catch it before it dies."
	};
	[Property] public ProgressStepCopy Lap { get; set; } = new()
	{
		Title = "FINISH A LAP",
		Blurb = "Reach the north mark. Extract or risk another."
	};
	[Property] public ProgressStepCopy Extract { get; set; } = new()
	{
		Title = "BANK THE BIOMASS",
		Blurb = "Extract stashed rounds to the altar."
	};
	[Property] public ProgressStepCopy Grow { get; set; } = new()
	{
		Title = "GROW A FRAME",
		Blurb = "Place an organ frame on the altar grid."
	};
	[Property] public ProgressStepCopy Inject { get; set; } = new()
	{
		Title = "INJECT IT",
		Blurb = "Shoot the frame until it works."
	};
	[Property] public ProgressStepCopy Chapel { get; set; } = new()
	{
		Title = "BUY A TRINKET",
		Blurb = "Spend bones in the chapel."
	};
	[Property] public ProgressStepCopy Lens { get; set; } = new()
	{
		Title = "BREAK THE LENS",
		Blurb = "After lap 5, fight LENS."
	};
	[Property] public ProgressStepCopy Ring { get; set; } = new()
	{
		Title = "RIDE THE NEXT RING",
		Blurb = "Keep the stash. Sit on YARD."
	};
	[Property] public ProgressStepCopy Core { get; set; } = new()
	{
		Title = "BREAK THE CORE",
		Blurb = "Ricochet the nucleus."
	};
	[Property] public ProgressStepCopy Done { get; set; } = new()
	{
		Title = "",
		Blurb = ""
	};
}

public class PlacesCopy
{
	[Property] public string LineCleared { get; set; } = "{0} CLEARED";
	[Property] public string LineOpen { get; set; } = "{0} CLEARED  ·  {1} OPEN";
	[Property] public PlaceCopy Yard { get; set; } = new()
	{
		Code = "YARD",
		Title = "The Yard",
		Rule = "PANELS KICK",
		Boss = "CORE",
		FightCall = "CORE FIGHT",
		FightHint = "THE CORE  ·  RICOCHET TO BREAK IT",
		Armor = "ARMOR  ·  RICOCHET FIRST",
		BossKeep = "Ricochet the nucleus. Keep the stash. Extract or ride the next ring.",
		BossLast = "Ricochet the nucleus. Win: stash ×2, then city."
	};
	[Property] public PlaceCopy Glass { get; set; } = new()
	{
		Code = "GLASS",
		Title = "Glassworks",
		Rule = "PANELS SHATTER",
		Boss = "LENS",
		FightCall = "LENS FIGHT",
		FightHint = "THE LENS  ·  BREAK GLASS THEN HIT THE SIDE",
		Armor = "ARMOR  ·  BREAK GLASS OR HIT THE SIDE",
		BossKeep = "Shatter the glass, then hit the side. Keep the stash. Extract or ride the next ring.",
		BossLast = "Shatter the glass, then hit the side. Win: stash ×2, then city."
	};
}

public class HudCopy
{
	[Property] public string Plain { get; set; } = "PLAIN";
	[Property] public string Shield { get; set; } = "SH {0}";
	[Property] public string Scrap { get; set; } = "SCRAP";
	[Property] public string Stash { get; set; } = "STASH";
	[Property] public string Best { get; set; } = "BEST";
	[Property] public string Kills { get; set; } = "KILLS";
	[Property] public string Caught { get; set; } = "CAUGHT";
	[Property] public string Dash { get; set; } = "DASH";
	[Property] public string Slow { get; set; } = "SLOW";
	[Property] public string HurtStamp { get; set; } = "-1";
	[Property] public string Task { get; set; } = "NEXT";
}

public class PauseCopy
{
	[Property] public string Button { get; set; } = "PAUSE";
	[Property] public string Kicker { get; set; } = "PAUSED";
	[Property] public string Title { get; set; } = "ONE ROUND TRIP";
	[Property] public string Blurb { get; set; } = "The ring holds. Trajectories wait.";
	[Property] public string Resume { get; set; } = "RESUME";
	[Property] public string ResumeBlurb { get; set; } = "Back to the lap. Nothing moved.";
	[Property] public string Menu { get; set; } = "MENU";
	[Property] public string MenuBlurb { get; set; } = "Leave the run. City stays.";
	[Property] public string Quit { get; set; } = "QUIT";
	[Property] public string QuitBlurb { get; set; } = "Close the game.";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
}

public class MenuCopy
{
	[Property] public string Kicker { get; set; } = "ONE MORE LAP";
	[Property] public string Title { get; set; } = "Looped & Loaded";
	[Property] public string Blurb { get; set; } = "Aim. Fire. Catch it back. Cash out to the city — or beat the core and ride the next ring.";
	[Property] public string SlotLine { get; set; } = "SLOT {0}  ·  {1}";
	[Property] public string SlotNew { get; set; } = "NEW";
	[Property] public string Continue { get; set; } = "CONTINUE";
	[Property] public string Play { get; set; } = "PLAY";
	[Property] public string ContinueBlurb { get; set; } = "Run with this city's warehouse and buildings.";
	[Property] public string PlayBlurb { get; set; } = "Start a run with one round. Extract or go again.";
	[Property] public string City { get; set; } = "CITY";
	[Property] public string CityBlurb { get; set; } = "Build frames and spend warehouse ammo.";
	[Property] public string Saves { get; set; } = "SAVES";
	[Property] public string SavesBlurb { get; set; } = "Autosave slots. Load or delete a city.";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
}

public class SavesCopy
{
	[Property] public string Kicker { get; set; } = "AUTOSAVE";
	[Property] public string Title { get; set; } = "SAVES";
	[Property] public string Blurb { get; set; } = "City writes itself. Pick a slot, then load or delete.";
	[Property] public string Slot { get; set; } = "SLOT {0}";
	[Property] public string Empty { get; set; } = "EMPTY";
	[Property] public string WithLine { get; set; } = "WH {0}  ·  BEST {1}  ·  {2}";
	[Property] public string WithBuildings { get; set; } = "WH {0}  ·  BEST {1}  ·  {2} BUILDINGS";
	[Property] public string Load { get; set; } = "LOAD SLOT {0}";
	[Property] public string LoadHas { get; set; } = "Use this city.";
	[Property] public string LoadEmpty { get; set; } = "Empty slot. Fresh city.";
	[Property] public string Delete { get; set; } = "DELETE SLOT {0}";
	[Property] public string DeleteBlurb { get; set; } = "Wipe this city. Cannot undo.";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
	[Property] public string KeySpace { get; set; } = "SPACE";
	[Property] public string KeyR { get; set; } = "R";
}

public class DecideCopy
{
	[Property] public string LapClear { get; set; } = "LAP {0} CLEAR";
	[Property] public string LapBlurb { get; set; } = "STASH {0}  ·  SCRAP {1}  ·  TAKE IT HOME OR RISK ANOTHER";
	[Property] public string Extract { get; set; } = "EXTRACT";
	[Property] public string ExtractBankOne { get; set; } = "Bank {0} round. Return to the city.";
	[Property] public string ExtractBankMany { get; set; } = "Bank {0} rounds. Return to the city.";
	[Property] public string OneMore { get; set; } = "ONE MORE LAP";
	[Property] public string OneMoreBlurb { get; set; } = "Stay on the ring. It gets harder.";
	[Property] public string Fight { get; set; } = "FIGHT THE {0}";
	[Property] public string RingClear { get; set; } = "RING CLEAR";
	[Property] public string RingBlurb { get; set; } = "STASH {0}  ·  NEXT {1}  ·  {2}";
	[Property] public string NextRing { get; set; } = "NEXT RING";
	[Property] public string NextRingBlurb { get; set; } = "{0}  ·  {1}  ·  keep stash and upgrades.";
	[Property] public string Dead { get; set; } = "RUN OVER";
	[Property] public string DeadBurnedOne { get; set; } = "{0} ROUND BURNED";
	[Property] public string DeadBurnedMany { get; set; } = "{0} ROUNDS BURNED";
	[Property] public string DeadCityBlurb { get; set; } = "Leave the ring. Build with what you already banked.";
	[Property] public string Extracted { get; set; } = "EXTRACTED";
	[Property] public string ExtractedOne { get; set; } = "{0} ROUND BANKED  ·  BEST {1}";
	[Property] public string ExtractedMany { get; set; } = "{0} ROUNDS BANKED  ·  BEST {1}";
	[Property] public string ExtractedCityBlurb { get; set; } = "Return to construction.";
	[Property] public string City { get; set; } = "CITY";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
	[Property] public string KeyR { get; set; } = "R";
}

public class ShopCopy
{
	[Property] public string Title { get; set; } = "ARMORY";
	[Property] public string Blurb { get; set; } = "SCRAP {0}  ·  ALL ROUNDS  ·  BUY ANY, THEN GO";
	[Property] public string BuyAll { get; set; } = "BUY ALL";
	[Property] public string BuyAllBlurb { get; set; } = "Take every leftover card for {0} scrap.";
	[Property] public string Cost { get; set; } = "{0} SCRAP";
	[Property] public string Go { get; set; } = "GO";
	[Property] public string GoBlurb { get; set; } = "Next lap. Keep leftover scrap.";
	[Property] public string Bought { get; set; } = "BOUGHT";
	[Property] public string OfferBlurb { get; set; } = "{0}  LV{1}";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
	[Property] public string KeyE { get; set; } = "E";
	[Property] public string KeySpace { get; set; } = "SPACE";
}

public class HelpCopy
{
	[Property] public string Paused { get; set; } = "1 RESUME  ·  2 MENU  ·  3 QUIT";
	[Property] public string Saves { get; set; } = "1-3 SELECT  ·  SPACE LOAD  ·  R DELETE  ·  ESC BACK";
	[Property] public string Menu { get; set; } = "1 PLAY  ·  2 CITY  ·  3 SAVES  ·  ESC QUIT";
	[Property] public string CityBuild { get; set; } = "LMB PLACE/REMOVE  ·  WHEEL ROTATE  ·  1-5 TYPE  ·  E SHOOT  ·  SPACE/R RUN  ·  ESC PAUSE";
	[Property] public string CityShoot { get; set; } = "LMB FIRE  ·  E BUILD  ·  1-5 TYPE  ·  SPACE/R RUN  ·  ESC PAUSE";
	[Property] public string DecideLapBoss { get; set; } = "CLICK  ·  1 CITY  ·  2 STAY  ·  3 FIGHT THE {0}  ·  ESC PAUSE";
	[Property] public string DecideLap { get; set; } = "CLICK  ·  1 CITY  ·  2 STAY  ·  ESC PAUSE";
	[Property] public string DecideRingNext { get; set; } = "CLICK  ·  1 CITY  ·  2 NEXT RING  ·  ESC PAUSE";
	[Property] public string DecideRing { get; set; } = "CLICK  ·  1 CITY  ·  ESC PAUSE";
	[Property] public string ShopThree { get; set; } = "CLICK A CARD  ·  1 / 2 / 3 BUY  ·  E ALL  ·  SPACE GO  ·  ESC PAUSE";
	[Property] public string ShopTwo { get; set; } = "CLICK A CARD  ·  1 / 2 BUY  ·  E ALL  ·  SPACE GO  ·  ESC PAUSE";
	[Property] public string Dead { get; set; } = "CLICK CITY  ·  R CITY  ·  ESC MENU";
	[Property] public string BossGlass { get; set; } = "BREAK GLASS THEN HIT THE SIDE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string BossYard { get; set; } = "RICOCHET TO BREAK THE CORE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string PlayGlassSkip { get; set; } = "PANELS SHATTER  ·  E SKIP LAP  ·  LMB FIRE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string PlayGlass { get; set; } = "PANELS SHATTER  ·  LMB FIRE · 1-9 ROUNDS · SPACE DASH · R RESTART  ·  ESC PAUSE";
	[Property] public string PlaySkip { get; set; } = "E SKIP LAP  ·  LMB FIRE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string Play { get; set; } = "MOUSE AIM · LMB FIRE · 1-9 ROUNDS · SPACE DASH · R RESTART  ·  ESC PAUSE";
}

public class AnnounceCopy
{
	[Property] public string AimFire { get; set; } = "AIM. FIRE. CATCH IT BACK.";
	[Property] public string SlotPicked { get; set; } = "SLOT {0}";
	[Property] public string SlotDeleted { get; set; } = "SLOT {0} DELETED";
	[Property] public string StartRun { get; set; } = "ONE LAP. ONE ROUND. CASH OUT OR GO AGAIN.";
	[Property] public string TargetDown { get; set; } = "TARGET DOWN";
	[Property] public string ScrapGain { get; set; } = "+{0} SCRAP  ·  {1}";
	[Property] public string RoundChamberedShield { get; set; } = "ROUND {0} CHAMBERED  ·  SHIELD";
	[Property] public string RoundChamberedHits { get; set; } = "ROUND {0} CHAMBERED  +{1}";
	[Property] public string RoundChambered { get; set; } = "ROUND {0} CHAMBERED";
	[Property] public string RoundStatus { get; set; } = "ROUND {0} {1}";
	[Property] public string RoundLost { get; set; } = "ROUND {0} LOST";
	[Property] public string SelectChambered { get; set; } = "SELECT A CHAMBERED ROUND";
	[Property] public string StillInFlight { get; set; } = "ROUNDS STILL IN FLIGHT";
	[Property] public string LostOnRing { get; set; } = "ROUNDS LOST ON THE RING";
	[Property] public string Recovered { get; set; } = "RECOVERED";
	[Property] public string Linked { get; set; } = "LINKED";
	[Property] public string ShieldLeft { get; set; } = "SHIELD  ·  {0} LEFT";
	[Property] public string ShieldBroke { get; set; } = "SHIELD BROKE";
	[Property] public string HealthLeft { get; set; } = "-1  ·  {0} LEFT";
	[Property] public string RunOver { get; set; } = "RUN OVER";
	[Property] public string RingClear { get; set; } = "RING CLEAR";
	[Property] public string FinalClear { get; set; } = "NO MORE ROUNDS  ·  ×2";
	[Property] public string RingClearNext { get; set; } = "RING CLEAR  ·  {0}  ·  {1}";
	[Property] public string FinalBank { get; set; } = "NO MORE ROUNDS  ·  ×2  ·  +{0}";
	[Property] public string ArenaSkip { get; set; } = "ARENA CLEAR  ·  E SKIP LAP";
	[Property] public string LapClear { get; set; } = "LAP {0} CLEAR";
	[Property] public string CityDeposit { get; set; } = "CITY  ·  +{0} WAREHOUSE";
	[Property] public string City { get; set; } = "CITY";
	[Property] public string ContinueRounds { get; set; } = "+{0} ROUND  ·  STASH {1}  ·  ×{2:0.00}";
	[Property] public string StashMax { get; set; } = "STASH MAX  ·  ×{0:0.00}";
	[Property] public string BossContinue { get; set; } = "{0}  ·  +{1}  ·  STASH {2}";
	[Property] public string NeedScrap { get; set; } = "NEED {0} SCRAP";
	[Property] public string TraitBought { get; set; } = "{0} LV{1}  ·  {2} SCRAP";
	[Property] public string Armed { get; set; } = "ARMED  ·  {0} SCRAP";
	[Property] public string PlaceRule { get; set; } = "{0}  ·  {1}";
	[Property] public string CorePhase { get; set; } = "CORE PHASE {0}";
	[Property] public string LensPhase { get; set; } = "LENS PHASE {0}  ·  NEW GLASS";
	[Property] public string LensOpen { get; set; } = "LENS OPEN  ·  HIT THE SIDE";
}

public class CityCopy
{
	[Property] public string ModeBuild { get; set; } = "BUILD";
	[Property] public string ModeShoot { get; set; } = "SHOOT";
	[Property] public string WarehouseHud { get; set; } = "WH {0}";
	[Property] public string ShootFire { get; set; } = "SHOOT  ·  LMB FIRE";
	[Property] public string UpgradeReflects { get; set; } = "UPGRADE · REFLECTS";
	[Property] public string Frame { get; set; } = "FRAME";
	[Property] public string LevelReflects { get; set; } = "{0} LV{1}  ·  REFLECTS";
	[Property] public string Level { get; set; } = "{0} LV{1}";
	[Property] public string TitleStageHits { get; set; } = "{0}  ·  {1}  ·  {2} HITS";
	[Property] public string WheelRotate { get; set; } = "{0}  ·  WHEEL ROTATE";
	[Property] public string PlaceRotate { get; set; } = "{0}  ·  LMB PLACE  ·  WHEEL ROTATE";
	[Property] public string FrameRemove { get; set; } = "{0} FRAME  ·  LMB REMOVE";
	[Property] public string LevelMaxReflects { get; set; } = "{0} LV{1}  ·  MAX  ·  REFLECTS";
	[Property] public string LevelStageHits { get; set; } = "{0} LV{1}  ·  {2}  ·  {3} HITS";
	[Property] public string PlacedFrame { get; set; } = "{0} FRAME";
	[Property] public string Removed { get; set; } = "{0} REMOVED";
	[Property] public string WarehouseEmpty { get; set; } = "WAREHOUSE EMPTY";
	[Property] public string FrameHits { get; set; } = "FRAME  ·  {0} HITS TO WORK";
	[Property] public string HpTag { get; set; } = "+1 HP";
	[Property] public string DmgTag { get; set; } = "+1 DMG";
	[Property] public string Now { get; set; } = "NOW";
	[Property] public string Then { get; set; } = "THEN";
	[Property] public string Unlocks { get; set; } = "UNLOCKS";
	[Property] public string CardTag { get; set; } = "+1 CARD";
	[Property] public string NoBonuses { get; set; } = "NO BONUSES YET";
	[Property] public string BonusHp { get; set; } = "HP+{0}";
	[Property] public string BonusDmg { get; set; } = "DMG+{0}";
	[Property] public string BonusDash { get; set; } = "DASH";
	[Property] public string BonusSlow { get; set; } = "SLOW";
	[Property] public string BonusCards { get; set; } = "3 CARDS";
}
