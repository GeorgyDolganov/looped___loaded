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

public enum RoundTrait
{
	Buck,
	Bore,
	Drum,
	Warhead,
	Lash,
	Pin,
	Spin,
	Rush,
	Split,
	Fan,
	Pump,
	Load,
	Choke,
	Meat,
	Rico,
	Gape,
	Double,
	Kick,
	Stun,
	Heap,
	Waste,
	Breach,
	Slug
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All =
	{
		RoundTrait.Split, RoundTrait.Fan, RoundTrait.Pump,
		RoundTrait.Load, RoundTrait.Choke, RoundTrait.Meat, RoundTrait.Rico,
		RoundTrait.Gape, RoundTrait.Double, RoundTrait.Kick, RoundTrait.Stun,
		RoundTrait.Heap, RoundTrait.Waste, RoundTrait.Breach, RoundTrait.Slug,
		RoundTrait.Bore, RoundTrait.Drum, RoundTrait.Warhead, RoundTrait.Lash, RoundTrait.Pin,
		RoundTrait.Spin, RoundTrait.Rush
	};

	public static TraitPack Pack( RoundTrait trait ) => trait switch
	{
		RoundTrait.Split or RoundTrait.Fan or RoundTrait.Pump => TraitPack.Entry,
		RoundTrait.Load or RoundTrait.Choke or RoundTrait.Meat or RoundTrait.Rico => TraitPack.Junior,
		RoundTrait.Gape or RoundTrait.Double or RoundTrait.Kick or RoundTrait.Stun => TraitPack.Warrior,
		RoundTrait.Heap or RoundTrait.Waste or RoundTrait.Breach or RoundTrait.Slug => TraitPack.Abomination,
		RoundTrait.Bore => TraitPack.Rail,
		RoundTrait.Drum or RoundTrait.Rush => TraitPack.Rifle,
		RoundTrait.Warhead => TraitPack.Rocket,
		RoundTrait.Lash => TraitPack.Laser,
		_ => TraitPack.Nailgun
	};

	public static bool OneShot( RoundTrait trait ) => Pack( trait ) is TraitPack.Entry or TraitPack.Junior or TraitPack.Warrior or TraitPack.Abomination;

	public static int MaxLevel( RoundTrait trait ) => OneShot( trait ) ? 1 : GameSettings.Traits.MaxLevel;

	public static string Code( RoundTrait trait ) => GameSettings.Text.TraitCode( trait );
	public static string Title( RoundTrait trait ) => GameSettings.Text.TraitTitle( trait );
	public static string Blurb( RoundTrait trait ) => GameSettings.Text.TraitBlurb( trait );

	public static Color Color( RoundTrait trait ) => Pack( trait ) switch
	{
		TraitPack.Entry => new Color( 1f, 0.74f, 0.42f ),
		TraitPack.Junior => new Color( 1f, 0.55f, 0.22f ),
		TraitPack.Warrior => new Color( 0.95f, 0.28f, 0.16f ),
		TraitPack.Abomination => new Color( 0.62f, 0.95f, 0.28f ),
		TraitPack.Rifle => new Color( 0.55f, 0.85f, 1f ),
		TraitPack.Shotgun => new Color( 1f, 0.62f, 0.28f ),
		TraitPack.Nailgun => new Color( 0.95f, 0.82f, 0.35f ),
		TraitPack.Laser => new Color( 0.95f, 0.35f, 0.72f ),
		TraitPack.Rail => new Color( 0.4f, 0.75f, 1f ),
		_ => new Color( 1f, 0.38f, 0.28f )
	};

	public static string Rarity( RoundTrait trait ) => GameSettings.Text.RarityOf( Pack( trait ) );

	public static int Weight( RoundTrait trait ) => GameSettings.Traits.PackWeight( Pack( trait ) );

	public static string Icon( RoundTrait trait ) => trait switch
	{
		RoundTrait.Split => "ui/traits/bounce.png",
		RoundTrait.Fan => "ui/traits/swipe.png",
		RoundTrait.Pump => "ui/traits/latemag.png",
		RoundTrait.Load => "ui/traits/cue.png",
		RoundTrait.Choke => "ui/traits/incurve.png",
		RoundTrait.Meat => "ui/traits/heavy.png",
		RoundTrait.Rico => "ui/traits/pinball.png",
		RoundTrait.Gape => "ui/traits/redirect.png",
		RoundTrait.Double => "ui/traits/echo.png",
		RoundTrait.Kick => "ui/traits/kick.png",
		RoundTrait.Stun => "ui/traits/freeze.png",
		RoundTrait.Heap => "ui/traits/shred.png",
		RoundTrait.Waste => "ui/traits/rim.png",
		RoundTrait.Breach => "ui/traits/hook.png",
		RoundTrait.Slug => "ui/traits/heavy.png",
		RoundTrait.Bore => "ui/traits/pierce.png",
		RoundTrait.Drum => "ui/traits/accel.png",
		RoundTrait.Warhead => "ui/traits/explosive.png",
		RoundTrait.Lash => "ui/traits/electric.png",
		RoundTrait.Spin => "ui/traits/clockwise.png",
		RoundTrait.Rush => "ui/traits/step.png",
		_ => "ui/traits/stick.png"
	};
}
