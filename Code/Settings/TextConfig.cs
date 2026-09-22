namespace LoopedLoaded;

[AssetType( Name = "Text Config", Extension = "omrtext", Category = "Looped Loaded" )]
public class TextConfig : GameResource
{
	[Property] public HudCopy Hud { get; set; } = new();
	[Property] public PauseCopy Pause { get; set; } = new();
	[Property] public MenuCopy Menu { get; set; } = new();
	[Property] public SavesCopy Saves { get; set; } = new();
	[Property] public OptionsCopy Options { get; set; } = new();
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
		Options ??= new();
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
			RoundTrait.Buck => pack.Buck ??= new(),
			RoundTrait.Bore => pack.Bore ??= new(),
			RoundTrait.Drum => pack.Drum ??= new(),
			RoundTrait.Warhead => pack.Warhead ??= new(),
			RoundTrait.Lash => pack.Lash ??= new(),
			RoundTrait.Pin => pack.Pin ??= new(),
			RoundTrait.Spin => pack.Spin ??= new(),
			RoundTrait.Rush => pack.Rush ??= new(),
			RoundTrait.Split => pack.Split ??= new(),
			RoundTrait.Fan => pack.Fan ??= new(),
			RoundTrait.Pump => pack.Pump ??= new(),
			RoundTrait.Load => pack.Load ??= new(),
			RoundTrait.Choke => pack.Choke ??= new(),
			RoundTrait.Meat => pack.Meat ??= new(),
			RoundTrait.Rico => pack.Rico ??= new(),
			RoundTrait.Gape => pack.Gape ??= new(),
			RoundTrait.Double => pack.Double ??= new(),
			RoundTrait.Kick => pack.Kick ??= new(),
			RoundTrait.Stun => pack.Stun ??= new(),
			RoundTrait.Heap => pack.Heap ??= new(),
			RoundTrait.Waste => pack.Waste ??= new(),
			RoundTrait.Breach => pack.Breach ??= new(),
			RoundTrait.Slug => pack.Slug ??= new(),
			RoundTrait.Mirv => pack.Mirv ??= new(),
			RoundTrait.Bloom => pack.Bloom ??= new(),
			RoundTrait.Scorch => pack.Scorch ??= new(),
			RoundTrait.Lance => pack.Lance ??= new(),
			RoundTrait.Crater => pack.Crater ??= new(),
			RoundTrait.Spot => pack.Spot ??= new(),
			_ => pack.Slug ??= new()
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
			TraitPack.Entry => Or( rarity.Entry, "ENTRY" ),
			TraitPack.Junior => Or( rarity.Junior, "JUNIOR" ),
			TraitPack.Warrior => Or( rarity.Warrior, "WARRIOR" ),
			TraitPack.Abomination => Or( rarity.Abomination, "ABOMINATION" ),
			TraitPack.Rifle or TraitPack.Shotgun => Or( rarity.Common, "COMMON" ),
			TraitPack.Nailgun => Or( rarity.Uncommon, "UNCOMMON" ),
			_ => Or( rarity.Rare, "RARE" )
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
	[Property] public string Entry { get; set; } = "ENTRY";
	[Property] public string Junior { get; set; } = "JUNIOR";
	[Property] public string Warrior { get; set; } = "WARRIOR";
	[Property] public string Abomination { get; set; } = "ABOMINATION";
}

public class TraitsCopy
{
	[Property] public RarityCopy Rarity { get; set; } = new();
	[Property] public TraitCopy Buck { get; set; } = new() { Code = "BUCK", Title = "BUCK", Blurb = "A couple of pellets. Full spread at rank 3. Shotgun DNA." };
	[Property] public TraitCopy Bore { get; set; } = new() { Code = "BORE", Title = "BORE", Blurb = "One punch-through. Rail at rank 3. Locks out LASH." };
	[Property] public TraitCopy Drum { get; set; } = new() { Code = "DRUM", Title = "DRUM", Blurb = "A short burst. Rifle dump at rank 3. Long reload." };
	[Property] public TraitCopy Warhead { get; set; } = new() { Code = "WARHEAD", Title = "WARHEAD", Blurb = "A small clap. Slow rocket. Hurts you." };
	[Property] public TraitCopy Lash { get; set; } = new() { Code = "LASH", Title = "LASH", Blurb = "Hold for lightning. Weak ticks. Locks out BORE." };
	[Property] public TraitCopy Pin { get; set; } = new() { Code = "PIN", Title = "PIN", Blurb = "A couple of nails. Full spray at rank 3. Stick and tick." };
	[Property] public TraitCopy Spin { get; set; } = new() { Code = "SPIN", Title = "SPIN", Blurb = "Shots sweep harder against the clock. Longer reload." };
	[Property] public TraitCopy Rush { get; set; } = new() { Code = "RUSH", Title = "RUSH", Blurb = "Shots fly faster. Longer reload." };
	[Property] public TraitCopy Split { get; set; } = new() { Code = "SPLIT", Title = "SPLIT", Blurb = "+1 pellet. Shot becomes 2." };
	[Property] public TraitCopy Fan { get; set; } = new() { Code = "FAN", Title = "FAN", Blurb = "Pellets spread 14°." };
	[Property] public TraitCopy Pump { get; set; } = new() { Code = "PUMP", Title = "PUMP", Blurb = "+1 pellet. Reload +0.25s." };
	[Property] public TraitCopy Load { get; set; } = new() { Code = "LOAD", Title = "LOAD", Blurb = "+2 pellets. Reload +0.15s." };
	[Property] public TraitCopy Choke { get; set; } = new() { Code = "CHOKE", Title = "CHOKE", Blurb = "Cone −10°. Floor 6°." };
	[Property] public TraitCopy Meat { get; set; } = new() { Code = "MEAT", Title = "MEAT", Blurb = "+1 dmg inside 140. Zero after 280." };
	[Property] public TraitCopy Rico { get; set; } = new() { Code = "RICO", Title = "RICO", Blurb = "Pellets bounce +1." };
	[Property] public TraitCopy Gape { get; set; } = new() { Code = "GAPE", Title = "GAPE", Blurb = "Cone +14°." };
	[Property] public TraitCopy Double { get; set; } = new() { Code = "DOUBLE", Title = "DOUBLE", Blurb = "Two fans, 0.12s apart. Reload +0.55s. One mag." };
	[Property] public TraitCopy Kick { get; set; } = new() { Code = "KICK", Title = "KICK", Blurb = "Shove 110 inside 180." };
	[Property] public TraitCopy Stun { get; set; } = new() { Code = "STUN", Title = "STUN", Blurb = "0.45s stagger inside 160. No bosses." };
	[Property] public TraitCopy Heap { get; set; } = new() { Code = "HEAP", Title = "HEAP", Blurb = "+3 pellets. Reload +0.40s." };
	[Property] public TraitCopy Waste { get; set; } = new() { Code = "WASTE", Title = "WASTE", Blurb = "+1 dmg inside 80. Zero after 150." };
	[Property] public TraitCopy Breach { get; set; } = new() { Code = "BREACH", Title = "BREACH", Blurb = "Pellets punch 1 body." };
	[Property] public TraitCopy Slug { get; set; } = new() { Code = "SLUG", Title = "SLUG", Blurb = "One fat slug. Radius 22. +2 dmg. No fan." };
	[Property] public TraitCopy Mirv { get; set; } = new() { Code = "MIRV", Title = "MIRV", Blurb = "Each pellet splashes. Radius ×0.55. Slower. Locks out LANCE." };
	[Property] public TraitCopy Bloom { get; set; } = new() { Code = "BLOOM", Title = "BLOOM", Blurb = "+40 splash radius. Reload +0.30s. Locks out LANCE." };
	[Property] public TraitCopy Scorch { get; set; } = new() { Code = "SCORCH", Title = "SCORCH", Blurb = "Splash damage 2. Slower. Reload +0.20s. Locks out LANCE." };
	[Property] public TraitCopy Lance { get; set; } = new() { Code = "LANCE", Title = "LANCE", Blurb = "No friendly splash. +2 direct hit. Smaller radius. No bounce. Locks out CLUSTER." };
	[Property] public TraitCopy Crater { get; set; } = new() { Code = "CRATER", Title = "CRATER", Blurb = "Fat body. +28 splash. No bounce. Slower. Locks out CLUSTER." };
	[Property] public TraitCopy Spot { get; set; } = new() { Code = "SPOT", Title = "SPOT", Blurb = "Shots fly to the cursor and burst there. No bounce. Slower." };
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
		Title = "FIRE THE GUN",
		Blurb = "LMB. Watch the reload. Kill something."
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
	[Property] public string Caught { get; set; } = "SHOTS";
	[Property] public string Dash { get; set; } = "DASH";
	[Property] public string Slow { get; set; } = "SLOW";
	[Property] public string HurtStamp { get; set; } = "-1";
	[Property] public string Task { get; set; } = "NEXT";
	[Property] public string FeedCount { get; set; } = "TOTAL BIOMASS {0} / {1}";
	[Property] public string Objective { get; set; } = "NEXT OBJECTIVE";
	[Property] public string Trinkets { get; set; } = "GUN";
	[Property] public string TrinketsNone { get; set; } = "PEA";
	[Property] public string Health { get; set; } = "HEALTH";
	[Property] public string Ammo { get; set; } = "RELOAD";
	[Property] public string DashKey { get; set; } = "SPACE";
	[Property] public string SlowKey { get; set; } = "RMB";
	[Property] public string Fed { get; set; } = "TOTAL BIOMASS";
	[Property] public string Threat { get; set; } = "THREAT {0}";
	[Property] public string FedValue { get; set; } = "{0} / {1}";
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
	[Property] public string Tagline { get; set; } = "Bring the brains to the EKKE.";
	[Property] public string Settings { get; set; } = "SETTINGS";
	[Property] public string Quit { get; set; } = "QUIT";
	[Property] public string ActiveSave { get; set; } = "ACTIVE SAVE — SLOT {0}";
	[Property] public string NavigateHint { get; set; } = "NAVIGATE";
	[Property] public string SelectHint { get; set; } = "ENTER SELECT";
	[Property] public string Objective { get; set; } = "NEXT OBJECTIVE";
	[Property] public string ObjectiveDone { get; set; } = "ALL OBJECTIVES DONE";
	[Property] public string StatBiomass { get; set; } = "BIOMASS";
	[Property] public string StatBest { get; set; } = "BEST";
	[Property] public string StatOrgans { get; set; } = "ORGANS";
}

public class OptionsCopy
{
	[Property] public string Kicker { get; set; } = "OPTIONS";
	[Property] public string Title { get; set; } = "SETTINGS";
	[Property] public string Tagline { get; set; } = "Saved the moment you change it.";
	[Property] public string Music { get; set; } = "MUSIC";
	[Property] public string Sfx { get; set; } = "SOUND";
	[Property] public string Shake { get; set; } = "SCREEN SHAKE";
	[Property] public string Value { get; set; } = "{0}%";
	[Property] public string Off { get; set; } = "OFF";
	[Property] public string Back { get; set; } = "BACK";
	[Property] public string AdjustHint { get; set; } = "ADJUST";
	[Property] public string BackHint { get; set; } = "ESC BACK";
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
	[Property] public string ExtractBankOne { get; set; } = "Bank {0} biomass. Return to the city.";
	[Property] public string ExtractBankMany { get; set; } = "Bank {0} biomass. Return to the city.";
	[Property] public string OneMore { get; set; } = "ONE MORE LAP";
	[Property] public string OneMoreBlurb { get; set; } = "Chapel. Another DNA card. The board gets meaner.";
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
	[Property] public string ShowcaseKicker { get; set; } = "DONE";
	[Property] public string ShowcaseCount { get; set; } = "{0} / {1}";
	[Property] public string ShowcaseContinue { get; set; } = "Back to the altar.";
	[Property] public string WonKicker { get; set; } = "THE FEEDING";
	[Property] public string Won { get; set; } = "EKKE IS FULL";
	[Property] public string WonBlurb { get; set; } = "{0} BIOMASS ON THE ALTAR. THE MOUTH IS QUIET.";
	[Property] public string WonPlay { get; set; } = "PLAY AGAIN";
	[Property] public string WonPlayBlurb { get; set; } = "A new body. Feed it again.";
	[Property] public string WonKey { get; set; } = "SPACE";
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
	[Property] public string Locked { get; set; } = "LOCKED";
	[Property] public string OfferBlurb { get; set; } = "{0}  LV{1}";
	[Property] public string Key1 { get; set; } = "1";
	[Property] public string Key2 { get; set; } = "2";
	[Property] public string Key3 { get; set; } = "3";
	[Property] public string Key4 { get; set; } = "4";
	[Property] public string Key5 { get; set; } = "5";
	[Property] public string Key6 { get; set; } = "6";
	[Property] public string Key7 { get; set; } = "7";
	[Property] public string Key8 { get; set; } = "8";
	[Property] public string Key9 { get; set; } = "9";
	[Property] public string KeyE { get; set; } = "E";
	[Property] public string KeySpace { get; set; } = "SPACE";

	public string KeyOf( int index ) => index switch
	{
		0 => Key1,
		1 => Key2,
		2 => Key3,
		3 => Key4,
		4 => Key5,
		5 => Key6,
		6 => Key7,
		7 => Key8,
		8 => Key9,
		_ => (index + 1).ToString()
	};
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
	[Property] public string Shop { get; set; } = "CLICK A CARD  ·  1-{0} BUY  ·  E ALL  ·  SPACE GO  ·  ESC PAUSE";
	[Property] public string Dead { get; set; } = "CLICK CITY  ·  R CITY  ·  ESC MENU";
	[Property] public string Extracted { get; set; } = "SPACE CONTINUE";
	[Property] public string Won { get; set; } = "CLICK  ·  SPACE PLAY AGAIN  ·  ESC MENU";
	[Property] public string BossGlass { get; set; } = "BREAK GLASS THEN HIT THE SIDE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string BossYard { get; set; } = "RICOCHET TO BREAK THE CORE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string PlayGlassSkip { get; set; } = "PANELS SHATTER  ·  E SKIP LAP  ·  LMB FIRE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string PlayGlass { get; set; } = "PANELS SHATTER  ·  LMB FIRE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string PlaySkip { get; set; } = "E SKIP LAP  ·  LMB FIRE  ·  SPACE DASH  ·  R RESTART  ·  ESC PAUSE";
	[Property] public string Play { get; set; } = "MOUSE AIM · LMB FIRE · SPACE DASH · R RESTART  ·  ESC PAUSE";
}

public class AnnounceCopy
{
	[Property] public string AimFire { get; set; } = "AIM. FIRE.";
	[Property] public string SlotPicked { get; set; } = "SLOT {0}";
	[Property] public string SlotDeleted { get; set; } = "SLOT {0} DELETED";
	[Property] public string StartRun { get; set; } = "ONE LAP. ONE GUN. CASH OUT OR GO AGAIN.";
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
	[Property] public string Won { get; set; } = "EKKE IS FULL";
	[Property] public string City { get; set; } = "CITY";
	[Property] public string ContinueRounds { get; set; } = "+{0} BIOMASS  ·  STASH {1}  ·  ×{2:0.00}";
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
	[Property] public string BonusCards { get; set; } = "{0} CARDS";
}
