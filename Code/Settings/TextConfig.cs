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
	[Property] public TalkCopy Talk { get; set; } = new();

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
		Talk ??= new();
		Talk.Ensure();
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

	public string RarityOf( TraitPack pack )
	{
		var rarity = (Traits ??= new()).Rarity ??= new();
		var label = pack switch
		{
			TraitPack.Entry => rarity.Common,
			TraitPack.Junior => rarity.Uncommon,
			TraitPack.Warrior => rarity.Rare,
			TraitPack.Abomination => rarity.Epic,
			TraitPack.Rifle or TraitPack.Shotgun => rarity.Common,
			TraitPack.Nailgun => rarity.Uncommon,
			_ => rarity.Rare
		};
		return label ?? "";
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
	public string BuildingPromise( BuildingKind kind ) => F( Building( kind ).Promise, PromiseAmount( kind ) );
	public string BuildingBlurb( BuildingKind kind ) => Building( kind ).Blurb ?? "";

	static int PromiseAmount( BuildingKind kind )
	{
		if ( kind != BuildingKind.Booster )
			return Progression.RankValue( 1 );

		var scale = Progression.DashScale( 1 );
		return (int)MathF.Round( (1f - scale) * 100f );
	}

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

	public IReadOnlyList<string> TalkLines( string id )
	{
		var talk = (Talk ??= new());
		talk.Ensure();
		var lines = id switch
		{
			"intro" => talk.Intro,
			"bones" => talk.Bones,
			"skip" => talk.SkipLap,
			"lap" => talk.Lap,
			"boss" => talk.Boss,
			"chapel" => talk.Chapel,
			"lens" => talk.Lens,
			"core" => talk.Core,
			"ring" => talk.Ring,
			"yard" => talk.Yard,
			"dead" => talk.Dead,
			"altar" => talk.Altar,
			"frame" => talk.Frame,
			"organ" => talk.Organ,
			"won" => talk.Won,
			_ => null
		};

		return lines ?? (IReadOnlyList<string>)Array.Empty<string>();
	}

	public string CityModeLabel( CityMode mode )
	{
		City ??= new();
		return mode == CityMode.Build ? City.ModeBuild ?? "" : City.ModeShoot ?? "";
	}

	public static string Shown( string value )
	{
		if ( string.IsNullOrEmpty( value ) )
			return value;

		return value.Replace( "CITY", "ALTAR" ).Replace( "City", "Altar" ).Replace( "city", "altar" );
	}

	static string Or( string value, string fallback ) => string.IsNullOrWhiteSpace( value ) ? fallback : value;
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
	[Property] public string Common { get; set; }
	[Property] public string Uncommon { get; set; }
	[Property] public string Rare { get; set; }
	[Property] public string Epic { get; set; }
	[Property] public string Entry { get; set; }
}

public class TraitsCopy
{
	[Property] public RarityCopy Rarity { get; set; } = new();
}

public class BuildingsCopy
{
	[Property] public BuildingCopy Infirmary { get; set; } = new();
	[Property] public BuildingCopy Anvil { get; set; } = new();
	[Property] public BuildingCopy Booster { get; set; } = new();
	[Property] public BuildingCopy Brake { get; set; } = new();
	[Property] public BuildingCopy Showcase { get; set; } = new();
}

public class ProgressStepCopy
{
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
}

public class ProgressCopy
{
	[Property] public ProgressStepCopy Catch { get; set; } = new();
	[Property] public ProgressStepCopy Lap { get; set; } = new();
	[Property] public ProgressStepCopy Extract { get; set; } = new();
	[Property] public ProgressStepCopy Grow { get; set; } = new();
	[Property] public ProgressStepCopy Inject { get; set; } = new();
	[Property] public ProgressStepCopy Chapel { get; set; } = new();
	[Property] public ProgressStepCopy Lens { get; set; } = new();
	[Property] public ProgressStepCopy Ring { get; set; } = new();
	[Property] public ProgressStepCopy Core { get; set; } = new();
	[Property] public ProgressStepCopy Done { get; set; } = new();
}

public class PlacesCopy
{
	[Property] public string LineCleared { get; set; }
	[Property] public string LineOpen { get; set; }
	[Property] public PlaceCopy Yard { get; set; } = new();
	[Property] public PlaceCopy Glass { get; set; } = new();
}

public class HudCopy
{
	[Property] public string Plain { get; set; }
	[Property] public string Scrap { get; set; }
	[Property] public string Stash { get; set; }
	[Property] public string Biomass { get; set; }
	[Property] public string Best { get; set; }
	[Property] public string Kills { get; set; }
	[Property] public string Caught { get; set; }
	[Property] public string Dash { get; set; }
	[Property] public string Slow { get; set; }
	[Property] public string HurtStamp { get; set; }
	[Property] public string DodgeStamp { get; set; }
	[Property] public string BlockStamp { get; set; } = "Blocked.";
	[Property] public string Task { get; set; }
	[Property] public string FeedCount { get; set; }
	[Property] public string Objective { get; set; }
	[Property] public string Trinkets { get; set; }
	[Property] public string TrinketsNone { get; set; }
	[Property] public string Health { get; set; }
	[Property] public string Ammo { get; set; }
	[Property] public string DashKey { get; set; }
	[Property] public string DashCoach { get; set; }
	[Property] public string SlowKey { get; set; }
	[Property] public string Fed { get; set; }
	[Property] public string Threat { get; set; }
	[Property] public string FedValue { get; set; }
}

public class PauseCopy
{
	[Property] public string Button { get; set; }
	[Property] public string Kicker { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
	[Property] public string Resume { get; set; }
	[Property] public string ResumeBlurb { get; set; }
	[Property] public string Menu { get; set; }
	[Property] public string MenuBlurb { get; set; }
	[Property] public string Quit { get; set; }
	[Property] public string QuitBlurb { get; set; }
	[Property] public string Key1 { get; set; }
	[Property] public string Key2 { get; set; }
	[Property] public string Key3 { get; set; }
}

public class MenuCopy
{
	[Property] public string Kicker { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
	[Property] public string SlotLine { get; set; }
	[Property] public string SlotNew { get; set; }
	[Property] public string Continue { get; set; }
	[Property] public string Play { get; set; }
	[Property] public string ContinueBlurb { get; set; }
	[Property] public string PlayBlurb { get; set; }
	[Property] public string City { get; set; }
	[Property] public string CityBlurb { get; set; }
	[Property] public string Saves { get; set; }
	[Property] public string SavesBlurb { get; set; }
	[Property] public string Collection { get; set; }
	[Property] public string Key1 { get; set; }
	[Property] public string Key2 { get; set; }
	[Property] public string Key3 { get; set; }
	[Property] public string Tagline { get; set; }
	[Property] public string Settings { get; set; }
	[Property] public string Quit { get; set; }
	[Property] public string ActiveSave { get; set; }
	[Property] public string SaveSlot { get; set; }
	[Property] public string NavigateHint { get; set; }
	[Property] public string SelectHint { get; set; }
	[Property] public string Objective { get; set; }
	[Property] public string ObjectiveDone { get; set; }
	[Property] public string StatBiomass { get; set; }
	[Property] public string StatBest { get; set; }
	[Property] public string StatOrgans { get; set; }
}

public class OptionsCopy
{
	[Property] public string Kicker { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Tagline { get; set; }
	[Property] public string Graphics { get; set; }
	[Property] public string GraphicsLow { get; set; }
	[Property] public string GraphicsMedium { get; set; }
	[Property] public string GraphicsHigh { get; set; }
	[Property] public string Music { get; set; }
	[Property] public string Sfx { get; set; }
	[Property] public string Shake { get; set; }
	[Property] public string Value { get; set; }
	[Property] public string Off { get; set; }
	[Property] public string Back { get; set; }
	[Property] public string AdjustHint { get; set; }
	[Property] public string BackHint { get; set; }
}

public class SavesCopy
{
	[Property] public string Kicker { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
	[Property] public string Slot { get; set; }
	[Property] public string Empty { get; set; }
	[Property] public string WithLine { get; set; }
	[Property] public string WithBuildings { get; set; }
	[Property] public string Load { get; set; }
	[Property] public string LoadHas { get; set; }
	[Property] public string LoadEmpty { get; set; }
	[Property] public string Delete { get; set; }
	[Property] public string DeleteBlurb { get; set; }
	[Property] public string Key1 { get; set; }
	[Property] public string Key2 { get; set; }
	[Property] public string Key3 { get; set; }
	[Property] public string KeySpace { get; set; }
	[Property] public string KeyR { get; set; }
}

public class DecideCopy
{
	[Property] public string LapClear { get; set; }
	[Property] public string LapBlurb { get; set; }
	[Property] public string Extract { get; set; }
	[Property] public string ExtractTake { get; set; }
	[Property] public string ExtractBankOne { get; set; }
	[Property] public string ExtractBankMany { get; set; }
	[Property] public string Boss { get; set; }
	[Property] public string OneMore { get; set; }
	[Property] public string NextAmmo { get; set; }
	[Property] public string NextBiomass { get; set; }
	[Property] public string OneMoreBlurb { get; set; }
	[Property] public string Fight { get; set; }
	[Property] public string RingClear { get; set; }
	[Property] public string RingBlurb { get; set; }
	[Property] public string NextRing { get; set; }
	[Property] public string NextRingBlurb { get; set; }
	[Property] public string Dead { get; set; }
	[Property] public string DeadBurnedOne { get; set; }
	[Property] public string DeadBurnedMany { get; set; }
	[Property] public string DeadReturnedOne { get; set; }
	[Property] public string DeadReturnedMany { get; set; }
	[Property] public string DeadCityBlurb { get; set; }
	[Property] public string Exit { get; set; }
	[Property] public string Extracted { get; set; }
	[Property] public string ExtractedRoundsLabel { get; set; }
	[Property] public string ExtractedBiomassLabel { get; set; }
	[Property] public string ExtractedOne { get; set; }
	[Property] public string ExtractedMany { get; set; }
	[Property] public string ExtractedCityBlurb { get; set; }
	[Property] public string ShowcaseKicker { get; set; }
	[Property] public string ShowcaseCount { get; set; }
	[Property] public string ShowcaseContinue { get; set; }
	[Property] public string WonKicker { get; set; }
	[Property] public string WonKickerAscend { get; set; }
	[Property] public string Won { get; set; }
	[Property] public string WonBlurb { get; set; }
	[Property] public string WonAscend { get; set; }
	[Property] public string WonAscendBlurb { get; set; }
	[Property] public string WonAscendHealth { get; set; }
	[Property] public string WonAscendReward { get; set; }
	[Property] public string WonAscendFeed { get; set; }
	[Property] public string WonKeep { get; set; }
	[Property] public string WonKeepBlurb { get; set; }
	[Property] public string City { get; set; }
	[Property] public string Key1 { get; set; }
	[Property] public string Key2 { get; set; }
	[Property] public string Key3 { get; set; }
	[Property] public string KeyR { get; set; }
}

public class ShopCopy
{
	[Property] public string Title { get; set; }
	[Property] public string StarterTitle { get; set; }
	[Property] public string Free { get; set; }
	[Property] public string Blurb { get; set; }
	[Property] public string BuyAll { get; set; }
	[Property] public string BuyAllBlurb { get; set; }
	[Property] public string Cost { get; set; }
	[Property] public string Refresh { get; set; }
	[Property] public string Go { get; set; }
	[Property] public string GoBlurb { get; set; }
	[Property] public string Bought { get; set; }
	[Property] public string Locked { get; set; }
	[Property] public string OfferBlurb { get; set; }
	[Property] public string OfferRank { get; set; }
	[Property] public string Weapon { get; set; }
	[Property] public string Active { get; set; }
	[Property] public string Unlocks { get; set; }
	[Property] public string Key1 { get; set; }
	[Property] public string Key2 { get; set; }
	[Property] public string Key3 { get; set; }
	[Property] public string Key4 { get; set; }
	[Property] public string Key5 { get; set; }
	[Property] public string Key6 { get; set; }
	[Property] public string Key7 { get; set; }
	[Property] public string Key8 { get; set; }
	[Property] public string Key9 { get; set; }
	[Property] public string KeyE { get; set; }
	[Property] public string KeySpace { get; set; }

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
	[Property] public string Paused { get; set; }
	[Property] public string Saves { get; set; }
	[Property] public string Collection { get; set; }
	[Property] public string Menu { get; set; }
	[Property] public string CityBuild { get; set; }
	[Property] public string CityShoot { get; set; }
	[Property] public string DecideLapBoss { get; set; }
	[Property] public string DecideLap { get; set; }
	[Property] public string DecideRingNext { get; set; }
	[Property] public string DecideRing { get; set; }
	[Property] public string Shop { get; set; }
	[Property] public string Dead { get; set; }
	[Property] public string Extracted { get; set; }
	[Property] public string Won { get; set; }
	[Property] public string PlayGlassSkip { get; set; }
	[Property] public string PlayGlass { get; set; }
	[Property] public string PlaySkip { get; set; }
	[Property] public string Play { get; set; }
}

public class AnnounceCopy
{
	[Property] public string AimFire { get; set; }
	[Property] public string SlotPicked { get; set; }
	[Property] public string SlotDeleted { get; set; }
	[Property] public string StartRun { get; set; }
	[Property] public string TargetDown { get; set; }
	[Property] public string ScrapGain { get; set; }
	[Property] public string RoundChamberedHits { get; set; }
	[Property] public string RoundChambered { get; set; }
	[Property] public string RoundStatus { get; set; }
	[Property] public string RoundLost { get; set; }
	[Property] public string SelectChambered { get; set; }
	[Property] public string StillInFlight { get; set; }
	[Property] public string LostOnRing { get; set; }
	[Property] public string Recovered { get; set; }
	[Property] public string Linked { get; set; }
	[Property] public string HealthLeft { get; set; }
	[Property] public string RunOver { get; set; }
	[Property] public string RingClear { get; set; }
	[Property] public string FinalClear { get; set; }
	[Property] public string RingClearNext { get; set; }
	[Property] public string FinalBank { get; set; }
	[Property] public string ArenaSkip { get; set; }
	[Property] public string LapClear { get; set; }
	[Property] public string CityDeposit { get; set; }
	[Property] public string Won { get; set; }
	[Property] public string Ascended { get; set; }
	[Property] public string City { get; set; }
	[Property] public string ContinueRounds { get; set; }
	[Property] public string StashMax { get; set; }
	[Property] public string BossContinue { get; set; }
	[Property] public string NeedScrap { get; set; }
	[Property] public string ShopAdded { get; set; }
	[Property] public string ShopRefresh { get; set; }
	[Property] public string TraitBought { get; set; }
	[Property] public string Armed { get; set; }
	[Property] public string PlaceRule { get; set; }
	[Property] public string CorePhase { get; set; }
}

public class CityCopy
{
	[Property] public string ModeBuild { get; set; }
	[Property] public string ModeShoot { get; set; }
	[Property] public string WarehouseHud { get; set; }
	[Property] public string ShootFire { get; set; }
	[Property] public string UpgradeReflects { get; set; }
	[Property] public string Frame { get; set; }
	[Property] public string LevelReflects { get; set; }
	[Property] public string Level { get; set; }
	[Property] public string TitleStageHits { get; set; }
	[Property] public string WheelRotate { get; set; }
	[Property] public string PlaceRotate { get; set; }
	[Property] public string FrameRemove { get; set; }
	[Property] public string LevelMaxReflects { get; set; }
	[Property] public string LevelStageHits { get; set; }
	[Property] public string PlacedFrame { get; set; }
	[Property] public string Removed { get; set; }
	[Property] public string WarehouseEmpty { get; set; }
	[Property] public string DashCap { get; set; }
	[Property] public string SlowCap { get; set; }
	[Property] public string CardCap { get; set; }
	[Property] public string FrameHits { get; set; }
	[Property] public string ClickToCraft { get; set; }
	[Property] public string SwitchToInject { get; set; }
	[Property] public string ClickToInject { get; set; }
	[Property] public string HpTag { get; set; }
	[Property] public string DmgTag { get; set; }
	[Property] public string Now { get; set; }
	[Property] public string Then { get; set; }
	[Property] public string Unlocks { get; set; }
	[Property] public string CardTag { get; set; }
	[Property] public string NoBonuses { get; set; }
	[Property] public string BonusHp { get; set; }
	[Property] public string BonusDmg { get; set; }
	[Property] public string BonusDash { get; set; }
	[Property] public string BonusSlow { get; set; }
	[Property] public string BonusCards { get; set; }
}

public class TalkCopy
{
	[Property] public string Name { get; set; }
	[Property] public string Advance { get; set; }
	[Property] public string Skip { get; set; }
	[Property] public List<string> Intro { get; set; } = new();
	[Property] public List<string> Bones { get; set; } = new();
	[Property] public List<string> SkipLap { get; set; } = new();
	[Property] public List<string> Lap { get; set; } = new();
	[Property] public List<string> Boss { get; set; } = new();
	[Property] public List<string> Chapel { get; set; } = new();
	[Property] public List<string> Lens { get; set; } = new();
	[Property] public List<string> Core { get; set; } = new();
	[Property] public List<string> Ring { get; set; } = new();
	[Property] public List<string> Yard { get; set; } = new();
	[Property] public List<string> Dead { get; set; } = new();
	[Property] public List<string> Altar { get; set; } = new();
	[Property] public List<string> Frame { get; set; } = new();
	[Property] public List<string> Organ { get; set; } = new();
	[Property] public List<string> Won { get; set; } = new();

	public void Ensure()
	{
		Intro ??= new();
		Bones ??= new();
		SkipLap ??= new();
		Lap ??= new();
		Boss ??= new();
		Chapel ??= new();
		Lens ??= new();
		Core ??= new();
		Ring ??= new();
		Yard ??= new();
		Dead ??= new();
		Altar ??= new();
		Frame ??= new();
		Organ ??= new();
		Won ??= new();
	}
}
