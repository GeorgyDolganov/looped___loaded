namespace LoopedLoaded;

public enum TraitPack
{
	Rifle,
	Shotgun,
	Nailgun,
	Laser,
	Rail,
	Rocket
}

public enum RoundTrait
{
	Buck,
	Bore,
	Drum,
	Warhead,
	Lash,
	Pin
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All = Enum.GetValues<RoundTrait>();

	public static TraitPack Pack( RoundTrait trait ) => trait switch
	{
		RoundTrait.Buck => TraitPack.Shotgun,
		RoundTrait.Bore => TraitPack.Rail,
		RoundTrait.Drum => TraitPack.Rifle,
		RoundTrait.Warhead => TraitPack.Rocket,
		RoundTrait.Lash => TraitPack.Laser,
		_ => TraitPack.Nailgun
	};

	public static string Code( RoundTrait trait ) => GameSettings.Text.TraitCode( trait );
	public static string Title( RoundTrait trait ) => GameSettings.Text.TraitTitle( trait );
	public static string Blurb( RoundTrait trait ) => GameSettings.Text.TraitBlurb( trait );

	public static Color Color( RoundTrait trait ) => Pack( trait ) switch
	{
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
		RoundTrait.Buck => "ui/traits/bounce.png",
		RoundTrait.Bore => "ui/traits/pierce.png",
		RoundTrait.Drum => "ui/traits/accel.png",
		RoundTrait.Warhead => "ui/traits/explosive.png",
		RoundTrait.Lash => "ui/traits/electric.png",
		_ => "ui/traits/stick.png"
	};
}
