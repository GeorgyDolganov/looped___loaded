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

	public static string Code( RoundTrait trait ) => GameSettings.Text.TraitCode( trait );
	public static string Title( RoundTrait trait ) => GameSettings.Text.TraitTitle( trait );
	public static string Blurb( RoundTrait trait ) => GameSettings.Text.TraitBlurb( trait );

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

	public static string Rarity( RoundTrait trait ) => GameSettings.Text.RarityOf( Pack( trait ) );

	public static int Weight( RoundTrait trait ) => GameSettings.Traits.PackWeight( Pack( trait ) );

	public static string Icon( RoundTrait trait ) => $"ui/traits/{trait.ToString().ToLowerInvariant()}.png";
}
