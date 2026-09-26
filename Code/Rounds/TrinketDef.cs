namespace LoopedLoaded;

public enum TraitPack
{
	Rifle,
	Shotgun,
	Nailgun,
	Laser,
	Rail,
	Rocket,
	Entry,
	Junior,
	Warrior,
	Abomination
}

public enum TrinketHook
{
	None,
	Slug,
	Pin
}

public enum GunStat
{
	Count,
	Full,
	Cone,
	Damage,
	Pierce,
	Bounces,
	Reload,
	BoreWait,
	Speed,
	Radius,
	Splash,
	SplashDamage,
	RangeCut,
	RangePad,
	MeatRange,
	MeatBonus,
	KickForce,
	KickRange,
	StunTime,
	StunRange,
	Cycle,
	Burst,
	WalkStep,
	BeamHit,
	BeamTick,
	BeamTicks,
	BeamWidth,
	BeamArc,
	BeamKiln,
	BeamRank,
	StickTime,
	Dodge,
	Gap,
	SpinSpeed,
	Energy
}

public enum ModOp
{
	Add,
	Mul,
	Set,
	Min,
	Max
}

public enum ModGrowth
{
	Flat,
	Tiers,
	Linear,
	TraitRatio,
	PerRemoved
}

public enum NoteWhen
{
	Always,
	First,
	Later
}

[Flags]
public enum GunFlag
{
	None = 0,
	Beam = 1 << 0,
	Auto = 1 << 1,
	DoublePump = 1 << 2,
	PointAim = 1 << 3,
	IgnoreArmor = 1 << 4,
	RampPierce = 1 << 5,
	Sight = 1 << 6,
	Bite = 1 << 7,
	CommitBurst = 1 << 8,
	PerPelletSplash = 1 << 9,
	FriendlySplash = 1 << 10,
	NoFriendlySplash = 1 << 11,
	Nail = 1 << 12,
	NoNail = 1 << 13,
	BeamSear = 1 << 14,
	BeamFork = 1 << 15,
	BeamShunt = 1 << 16,
	BeamLinger = 1 << 17
}

[AssetType( Name = "Trinket", Extension = "trinket", Category = "Looped Loaded" )]
public class TrinketDef : GameResource
{
	[Property] public string Id { get; set; }
	[Property] public string Title { get; set; }
	[Property] public string Blurb { get; set; }
	[Property] public string Icon { get; set; }
	[Property] public TraitPack Pack { get; set; }
	[Property] public int MaxLevel { get; set; }
	[Property] public bool InPool { get; set; } = true;
	[Property] public int Sort { get; set; }
	[Property] public int UnlockLap { get; set; }
	[Property] public int Price { get; set; }
	[Property] public int OwnedWeight { get; set; }
	[Property] public TrinketHook Hook { get; set; }
	[Property] public List<GunFlag> Flags { get; set; } = new();
	[Property] public List<TrinketDef> Requires { get; set; } = new();
	[Property] public List<TrinketDef> Excludes { get; set; } = new();
	[Property] public List<TrinketMod> Mods { get; set; } = new();
	[Property] public List<TrinketNote> Notes { get; set; } = new();

	public int Cap => MaxLevel > 0 ? MaxLevel : Math.Max( 1, GameSettings.Traits.MaxLevel );
	public string ShownTitle => string.IsNullOrWhiteSpace( Title ) ? Id : Title;
	public string ShownBlurb => Blurb ?? "";
	public string IconPath => string.IsNullOrWhiteSpace( Icon ) ? "ui/traits/stick.png" : Icon;
}

public class TrinketMod
{
	[Property] public GunStat Stat { get; set; }
	[Property] public ModOp Op { get; set; }
	[Property] public ModGrowth Growth { get; set; }
	[Property] public float Value { get; set; }
	[Property] public float Bias { get; set; }
	[Property] public TraitTiers Tiers { get; set; }
	[Property] public int Order { get; set; }
	[Property] public bool Hidden { get; set; }
	[Property] public bool Passive { get; set; }
	[Property] public bool ByHook { get; set; }
	[Property] public bool AlsoFull { get; set; }
	[Property] public bool Points { get; set; }
	[Property] public string Role { get; set; }
	[Property] public bool UseWhen { get; set; }
	[Property] public GunStat WhenStat { get; set; }
	[Property] public bool HasWhenMin { get; set; }
	[Property] public float WhenMin { get; set; }
	[Property] public bool HasWhenMax { get; set; }
	[Property] public float WhenMax { get; set; }

	public float Amount( int level, int removed )
	{
		var raw = Growth switch
		{
			ModGrowth.Linear => Value * level,
			ModGrowth.TraitRatio => Value * Progression.TraitMul( level ),
			ModGrowth.Tiers => Tiers is null ? 0f : Tiers.At( level ),
			ModGrowth.PerRemoved => Value * removed,
			_ => Value
		};
		return raw + Bias;
	}
}

public class TrinketNote
{
	[Property] public string Text { get; set; }
	[Property] public int Sign { get; set; }
	[Property] public bool Mixed { get; set; }
	[Property] public NoteWhen When { get; set; }
}
