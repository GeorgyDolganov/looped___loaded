namespace LoopedLoaded;

public enum TraitPack
{
	Starter,
	Chaos,
	Body,
	Homing,
	Geometry,
	Return,
	Strike
}

public enum RoundTrait
{
	Pierce,
	Bounce,
	Magnetic,
	Freeze,
	Explosive,
	Heavy,
	Accel,
	Electric,
	Blood,
	Homing,
	Skim,
	Kick,
	Stick,
	Cushion,
	Cue,
	Corner,
	Incurve,
	Clockwise,
	Stutter,
	Breach,
	Boomerang,
	Reel,
	Swipe,
	Backstop,
	Link,
	Fuse,
	Snap,
	LateMag,
	Rim,
	SecondWind,
	Shred,
	Hook,
	Mark,
	Pinball,
	Ribbon,
	Graze,
	Rehit,
	Step,
	Echo,
	Redirect
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All = Enum.GetValues<RoundTrait>();

	public static readonly RoundTrait[] Starter =
	{
		RoundTrait.Pierce,
		RoundTrait.Bounce,
		RoundTrait.Magnetic,
		RoundTrait.Freeze
	};

	public static TraitPack Pack( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce or RoundTrait.Bounce or RoundTrait.Magnetic or RoundTrait.Freeze => TraitPack.Starter,
		RoundTrait.Explosive or RoundTrait.Electric or RoundTrait.Accel => TraitPack.Chaos,
		RoundTrait.Heavy or RoundTrait.Blood => TraitPack.Body,
		RoundTrait.Homing => TraitPack.Homing,
		RoundTrait.Skim or RoundTrait.Kick or RoundTrait.Stick or RoundTrait.Cushion or RoundTrait.Cue
			or RoundTrait.Corner or RoundTrait.Incurve or RoundTrait.Clockwise or RoundTrait.Stutter or RoundTrait.Breach => TraitPack.Geometry,
		RoundTrait.Boomerang or RoundTrait.Reel or RoundTrait.Swipe or RoundTrait.Backstop or RoundTrait.Link
			or RoundTrait.Fuse or RoundTrait.Snap or RoundTrait.LateMag or RoundTrait.Rim or RoundTrait.SecondWind => TraitPack.Return,
		_ => TraitPack.Strike
	};

	public static string Code( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "PRC",
		RoundTrait.Bounce => "RCH",
		RoundTrait.Magnetic => "MAG",
		RoundTrait.Freeze => "FRZ",
		RoundTrait.Explosive => "XPL",
		RoundTrait.Heavy => "HVY",
		RoundTrait.Accel => "ACL",
		RoundTrait.Electric => "ELC",
		RoundTrait.Blood => "BLD",
		RoundTrait.Homing => "HOM",
		RoundTrait.Skim => "SKM",
		RoundTrait.Kick => "KCK",
		RoundTrait.Stick => "STK",
		RoundTrait.Cushion => "CSH",
		RoundTrait.Cue => "CUE",
		RoundTrait.Corner => "CNR",
		RoundTrait.Incurve => "INC",
		RoundTrait.Clockwise => "CLK",
		RoundTrait.Stutter => "STT",
		RoundTrait.Breach => "BRH",
		RoundTrait.Boomerang => "BMG",
		RoundTrait.Reel => "REL",
		RoundTrait.Swipe => "SWP",
		RoundTrait.Backstop => "BCK",
		RoundTrait.Link => "LNK",
		RoundTrait.Fuse => "FUS",
		RoundTrait.Snap => "SNP",
		RoundTrait.LateMag => "LTM",
		RoundTrait.Rim => "RIM",
		RoundTrait.SecondWind => "2ND",
		RoundTrait.Shred => "SHD",
		RoundTrait.Hook => "HOK",
		RoundTrait.Mark => "MRK",
		RoundTrait.Pinball => "PNB",
		RoundTrait.Ribbon => "RBN",
		RoundTrait.Graze => "GRZ",
		RoundTrait.Rehit => "RHT",
		RoundTrait.Step => "STP",
		RoundTrait.Echo => "ECO",
		_ => "RDR"
	};

	public static string Title( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "PIERCE",
		RoundTrait.Bounce => "RICOCHET",
		RoundTrait.Magnetic => "MAGNET",
		RoundTrait.Freeze => "FREEZE",
		RoundTrait.Explosive => "EXPLOSIVE",
		RoundTrait.Heavy => "HEAVY",
		RoundTrait.Accel => "ACCEL",
		RoundTrait.Electric => "ELECTRIC",
		RoundTrait.Blood => "BLOOD",
		RoundTrait.Homing => "HOMING",
		RoundTrait.Skim => "SKIM",
		RoundTrait.Kick => "KICK",
		RoundTrait.Stick => "STICK",
		RoundTrait.Cushion => "CUSHION",
		RoundTrait.Cue => "CUE",
		RoundTrait.Corner => "CORNER",
		RoundTrait.Incurve => "INCURVE",
		RoundTrait.Clockwise => "CLOCKWISE",
		RoundTrait.Stutter => "STUTTER",
		RoundTrait.Breach => "BREACH",
		RoundTrait.Boomerang => "BOOMERANG",
		RoundTrait.Reel => "REEL",
		RoundTrait.Swipe => "SWIPE",
		RoundTrait.Backstop => "BACKSTOP",
		RoundTrait.Link => "LINK",
		RoundTrait.Fuse => "FUSE",
		RoundTrait.Snap => "SNAP",
		RoundTrait.LateMag => "LATE MAG",
		RoundTrait.Rim => "RIM",
		RoundTrait.SecondWind => "SECOND WIND",
		RoundTrait.Shred => "SHRED",
		RoundTrait.Hook => "HOOK",
		RoundTrait.Mark => "MARK",
		RoundTrait.Pinball => "PINBALL",
		RoundTrait.Ribbon => "RIBBON",
		RoundTrait.Graze => "GRAZE",
		RoundTrait.Rehit => "REHIT",
		RoundTrait.Step => "STEP",
		RoundTrait.Echo => "ECHO",
		_ => "REDIRECT"
	};

	public static string Blurb( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "Pass through enemies instead of bouncing off them.",
		RoundTrait.Bounce => "More wall bounces and a longer flight.",
		RoundTrait.Magnetic => "Steer home near you. Wider catch zone.",
		RoundTrait.Freeze => "Slow whatever you hit.",
		RoundTrait.Explosive => "Blast on a kill or the last bounce.",
		RoundTrait.Heavy => "Harder hits and shove. Slower flight.",
		RoundTrait.Accel => "Speed up after a bounce or a kill.",
		RoundTrait.Electric => "Jump lightning to nearby enemies. Course stays.",
		RoundTrait.Blood => "A kill streak on one shot grants a shield.",
		RoundTrait.Homing => "After a kill or last bounce, turn home earlier.",
		RoundTrait.Skim => "Glancing panel hits keep the bounce charge.",
		RoundTrait.Kick => "Player shots twist inner panels harder.",
		RoundTrait.Stick => "Cling to a panel, then launch along it.",
		RoundTrait.Cushion => "The first bounce hugs the wall.",
		RoundTrait.Cue => "True-face banks. Preview matches the shot.",
		RoundTrait.Corner => "Pull toward panel ends for bank shots.",
		RoundTrait.Incurve => "Curve toward the core.",
		RoundTrait.Clockwise => "Curve with your run around the ring.",
		RoundTrait.Stutter => "Pause on each wall bounce.",
		RoundTrait.Breach => "Punch through inner panels.",
		RoundTrait.Boomerang => "When spent, retrace your path home.",
		RoundTrait.Reel => "Lost rounds crawl the ring toward you.",
		RoundTrait.Swipe => "Dash through your round to chamber it.",
		RoundTrait.Backstop => "Catch zone behind you too.",
		RoundTrait.Link => "A flying round picks up a dropped one.",
		RoundTrait.Fuse => "Missed catch explodes where it drops.",
		RoundTrait.Snap => "A catch primes a faster, longer-preview shot.",
		RoundTrait.LateMag => "Magnet waits until the last bounce or a kill.",
		RoundTrait.Rim => "Drops always land ahead on the outer ring.",
		RoundTrait.SecondWind => "First stall kicks toward the ring instead.",
		RoundTrait.Shred => "First shield hit counts as a rear shot.",
		RoundTrait.Hook => "Hits drag the enemy along the shot.",
		RoundTrait.Mark => "Tagged foes pull the next round in.",
		RoundTrait.Pinball => "Bounce off bodies harder. Crowd becomes banks.",
		RoundTrait.Ribbon => "Your trail cuts and slows enemies.",
		RoundTrait.Graze => "Near-misses still chip a hit.",
		RoundTrait.Rehit => "A wall bounce lets you hit the same foe again.",
		RoundTrait.Step => "A kill jumps the round forward.",
		RoundTrait.Echo => "A ghost retraces the line and hits again.",
		_ => "A kill turns you toward the next body in cone."
	};

	public static Color Color( RoundTrait trait ) => Pack( trait ) switch
	{
		TraitPack.Starter => new Color( 0.55f, 0.85f, 1f ),
		TraitPack.Chaos => new Color( 1f, 0.45f, 0.2f ),
		TraitPack.Body => new Color( 0.85f, 0.7f, 0.35f ),
		TraitPack.Homing => new Color( 0.7f, 0.4f, 1f ),
		TraitPack.Geometry => new Color( 0.35f, 0.95f, 0.85f ),
		TraitPack.Return => new Color( 0.45f, 0.85f, 1f ),
		_ => new Color( 1f, 0.35f, 0.42f )
	};
}
