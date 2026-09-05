namespace LoopedLoaded;

public enum RoundTrait
{
	Pierce,
	Bounce,
	Magnetic,
	Freeze
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All =
	{
		RoundTrait.Pierce,
		RoundTrait.Bounce,
		RoundTrait.Magnetic,
		RoundTrait.Freeze
	};

	public static string Code( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "PRC",
		RoundTrait.Bounce => "RCH",
		RoundTrait.Magnetic => "MAG",
		_ => "FRZ"
	};

	public static string Title( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "PIERCE",
		RoundTrait.Bounce => "RICOCHET",
		RoundTrait.Magnetic => "MAGNET",
		_ => "FREEZE"
	};

	public static string Blurb( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => "Pass through enemies instead of bouncing off them.",
		RoundTrait.Bounce => "More wall bounces and a longer flight.",
		RoundTrait.Magnetic => "Steer home near you. Wider catch zone.",
		_ => "Slow whatever you hit. Stacks duration by level."
	};

	public static Color Color( RoundTrait trait ) => trait switch
	{
		RoundTrait.Pierce => new Color( 1f, 0.45f, 0.2f ),
		RoundTrait.Bounce => new Color( 0.55f, 0.85f, 1f ),
		RoundTrait.Magnetic => new Color( 0.7f, 0.4f, 1f ),
		_ => new Color( 0.35f, 0.95f, 1f )
	};
}
