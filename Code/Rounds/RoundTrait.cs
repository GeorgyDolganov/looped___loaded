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
	Slug,
	Mirv,
	Bloom,
	Scorch,
	Lance,
	Crater,
	Spot,
	Deep,
	Awl,
	Ram,
	Mass,
	Keel,
	Trace,
	Belt,
	Walk,
	Spool,
	Sight,
	Bite,
	Link,
	Dodge,
	Snap,
	Sear,
	Kiln,
	Arc,
	Fork,
	Shunt,
	Linger
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All =
	{
		RoundTrait.Split, RoundTrait.Fan, RoundTrait.Pump,
		RoundTrait.Load, RoundTrait.Choke, RoundTrait.Meat, RoundTrait.Rico,
		RoundTrait.Gape, RoundTrait.Double, RoundTrait.Kick, RoundTrait.Stun,
		RoundTrait.Heap, RoundTrait.Waste, RoundTrait.Breach, RoundTrait.Slug,
		RoundTrait.Buck, RoundTrait.Bore, RoundTrait.Drum, RoundTrait.Warhead, RoundTrait.Lash, RoundTrait.Pin,
		RoundTrait.Rush, RoundTrait.Dodge, RoundTrait.Snap,
		RoundTrait.Mirv, RoundTrait.Bloom, RoundTrait.Scorch, RoundTrait.Lance, RoundTrait.Crater, RoundTrait.Spot,
		RoundTrait.Deep, RoundTrait.Awl, RoundTrait.Ram, RoundTrait.Mass, RoundTrait.Keel, RoundTrait.Trace,
		RoundTrait.Belt, RoundTrait.Walk, RoundTrait.Spool, RoundTrait.Sight, RoundTrait.Bite, RoundTrait.Link,
		RoundTrait.Sear, RoundTrait.Kiln, RoundTrait.Arc, RoundTrait.Fork, RoundTrait.Shunt, RoundTrait.Linger
	};

	public static readonly RoundTrait[] Roots =
	{
		RoundTrait.Buck, RoundTrait.Bore, RoundTrait.Drum, RoundTrait.Warhead, RoundTrait.Lash, RoundTrait.Pin
	};

	public static readonly RoundTrait[] RocketBranch =
	{
		RoundTrait.Mirv, RoundTrait.Bloom, RoundTrait.Scorch, RoundTrait.Lance, RoundTrait.Crater, RoundTrait.Spot
	};

	public static readonly RoundTrait[] RailBranch =
	{
		RoundTrait.Deep, RoundTrait.Awl, RoundTrait.Ram, RoundTrait.Mass, RoundTrait.Keel, RoundTrait.Trace
	};

	public static readonly RoundTrait[] RifleBranch =
	{
		RoundTrait.Belt, RoundTrait.Walk, RoundTrait.Spool, RoundTrait.Sight, RoundTrait.Bite, RoundTrait.Link
	};

	public static readonly RoundTrait[] LaserBranch =
	{
		RoundTrait.Sear, RoundTrait.Kiln, RoundTrait.Arc, RoundTrait.Fork, RoundTrait.Shunt, RoundTrait.Linger
	};

	public static TraitPack Pack( RoundTrait trait ) => trait switch
	{
		RoundTrait.Split or RoundTrait.Fan or RoundTrait.Pump => TraitPack.Entry,
		RoundTrait.Load or RoundTrait.Choke or RoundTrait.Meat or RoundTrait.Rico => TraitPack.Junior,
		RoundTrait.Gape or RoundTrait.Double or RoundTrait.Kick or RoundTrait.Stun => TraitPack.Warrior,
		RoundTrait.Heap or RoundTrait.Waste or RoundTrait.Breach or RoundTrait.Slug => TraitPack.Abomination,
		RoundTrait.Buck => TraitPack.Shotgun,
		RoundTrait.Bore or RoundTrait.Deep or RoundTrait.Awl or RoundTrait.Ram or RoundTrait.Mass or RoundTrait.Keel or RoundTrait.Trace => TraitPack.Rail,
		RoundTrait.Drum or RoundTrait.Rush or RoundTrait.Belt or RoundTrait.Walk or RoundTrait.Spool or RoundTrait.Sight or RoundTrait.Bite or RoundTrait.Link => TraitPack.Rifle,
		RoundTrait.Warhead or RoundTrait.Mirv or RoundTrait.Bloom or RoundTrait.Scorch or RoundTrait.Lance or RoundTrait.Crater or RoundTrait.Spot => TraitPack.Rocket,
		RoundTrait.Lash or RoundTrait.Sear or RoundTrait.Kiln or RoundTrait.Arc or RoundTrait.Fork or RoundTrait.Shunt or RoundTrait.Linger => TraitPack.Laser,
		_ => TraitPack.Nailgun
	};

	public static bool OneShot( RoundTrait trait ) => NeedsWarhead( trait ) || NeedsBore( trait ) || NeedsDrum( trait ) || NeedsLash( trait ) || Pack( trait ) is TraitPack.Entry or TraitPack.Junior or TraitPack.Warrior or TraitPack.Abomination;

	public static bool IsCluster( RoundTrait trait ) => trait is RoundTrait.Mirv or RoundTrait.Bloom or RoundTrait.Scorch;

	public static bool IsLance( RoundTrait trait ) => trait is RoundTrait.Lance or RoundTrait.Crater;

	public static bool NeedsWarhead( RoundTrait trait ) => IsCluster( trait ) || IsLance( trait ) || trait == RoundTrait.Spot;

	public static bool OwnsCluster( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Mirv ) || loadout.Has( RoundTrait.Bloom ) || loadout.Has( RoundTrait.Scorch ));

	public static bool OwnsLance( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Lance ) || loadout.Has( RoundTrait.Crater ));

	public static bool IsDeep( RoundTrait trait ) => trait is RoundTrait.Deep or RoundTrait.Awl or RoundTrait.Ram;

	public static bool IsMass( RoundTrait trait ) => trait is RoundTrait.Mass or RoundTrait.Keel;

	public static bool NeedsBore( RoundTrait trait ) => IsDeep( trait ) || IsMass( trait ) || trait == RoundTrait.Trace;

	public static bool OwnsDeep( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Deep ) || loadout.Has( RoundTrait.Awl ) || loadout.Has( RoundTrait.Ram ));

	public static bool OwnsMass( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Mass ) || loadout.Has( RoundTrait.Keel ));

	public static bool IsSweep( RoundTrait trait ) => trait is RoundTrait.Belt or RoundTrait.Walk;

	public static bool IsTrack( RoundTrait trait ) => trait is RoundTrait.Spool or RoundTrait.Sight or RoundTrait.Bite;

	public static bool NeedsDrum( RoundTrait trait ) => IsSweep( trait ) || IsTrack( trait ) || trait == RoundTrait.Link;

	public static bool OwnsSweep( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Belt ) || loadout.Has( RoundTrait.Walk ));

	public static bool OwnsTrack( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Spool ) || loadout.Has( RoundTrait.Sight ) || loadout.Has( RoundTrait.Bite ));

	public static bool IsBrand( RoundTrait trait ) => trait is RoundTrait.Sear or RoundTrait.Kiln;

	public static bool IsArc( RoundTrait trait ) => trait is RoundTrait.Arc or RoundTrait.Fork;

	public static bool NeedsLash( RoundTrait trait ) => IsBrand( trait ) || IsArc( trait ) || trait is RoundTrait.Shunt or RoundTrait.Linger;

	public static bool OwnsBrand( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Sear ) || loadout.Has( RoundTrait.Kiln ));

	public static bool OwnsArc( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Arc ) || loadout.Has( RoundTrait.Fork ));

	public static bool TooEarly( RoundTrait trait, int lap )
		=> trait == RoundTrait.Snap && lap < GameSettings.Traits.SnapUnlockLap;

	public static bool Blocked( RoundTrait trait, RunLoadout loadout )
	{
		if ( loadout is null )
			return false;

		if ( NeedsWarhead( trait ) && !loadout.Has( RoundTrait.Warhead ) )
			return true;

		if ( Rival( trait ) is { } other && loadout.Has( other ) )
			return true;

		if ( IsCluster( trait ) && OwnsLance( loadout ) )
			return true;

		if ( IsLance( trait ) && OwnsCluster( loadout ) )
			return true;

		if ( NeedsBore( trait ) && !loadout.Has( RoundTrait.Bore ) )
			return true;

		if ( IsDeep( trait ) && OwnsMass( loadout ) )
			return true;

		if ( IsMass( trait ) && OwnsDeep( loadout ) )
			return true;

		if ( NeedsDrum( trait ) && !loadout.Has( RoundTrait.Drum ) )
			return true;

		if ( IsSweep( trait ) && OwnsTrack( loadout ) )
			return true;

		if ( IsTrack( trait ) && OwnsSweep( loadout ) )
			return true;

		if ( NeedsLash( trait ) && !loadout.Has( RoundTrait.Lash ) )
			return true;

		if ( IsBrand( trait ) && OwnsArc( loadout ) )
			return true;

		if ( IsArc( trait ) && OwnsBrand( loadout ) )
			return true;

		return false;
	}

	public static RoundTrait? Rival( RoundTrait trait ) => trait switch
	{
		RoundTrait.Lash => RoundTrait.Bore,
		RoundTrait.Bore => RoundTrait.Lash,
		_ => null
	};

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
		RoundTrait.Buck => "ui/traits/heavy.png",
		RoundTrait.Bore or RoundTrait.Deep or RoundTrait.Awl or RoundTrait.Ram or RoundTrait.Mass or RoundTrait.Keel or RoundTrait.Trace => "ui/traits/pierce.png",
		RoundTrait.Drum or RoundTrait.Belt or RoundTrait.Walk or RoundTrait.Spool or RoundTrait.Sight or RoundTrait.Bite or RoundTrait.Link => "ui/traits/accel.png",
		RoundTrait.Warhead or RoundTrait.Mirv or RoundTrait.Bloom or RoundTrait.Scorch or RoundTrait.Lance or RoundTrait.Crater or RoundTrait.Spot => "ui/traits/explosive.png",
		RoundTrait.Lash or RoundTrait.Sear or RoundTrait.Kiln or RoundTrait.Arc or RoundTrait.Fork or RoundTrait.Shunt or RoundTrait.Linger => "ui/traits/electric.png",
		RoundTrait.Spin => "ui/traits/clockwise.png",
		RoundTrait.Rush => "ui/traits/step.png",
		RoundTrait.Dodge => "ui/traits/graze.png",
		RoundTrait.Snap => "ui/traits/snap.png",
		_ => "ui/traits/stick.png"
	};
}
