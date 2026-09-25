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
	Linger,
	Cell,
	Jack,
	Slap,
	Rack,
	Draw,
	Feed,
	Eject,
	Vent,
	Cool,
	Shuck,
	Slam
}

public static class RoundTraits
{
	public static readonly RoundTrait[] All =
	{
		RoundTrait.Split, RoundTrait.Fan,
		RoundTrait.Choke, RoundTrait.Meat, RoundTrait.Rico,
		RoundTrait.Double, RoundTrait.Kick, RoundTrait.Stun,
		RoundTrait.Slug,
		RoundTrait.Buck, RoundTrait.Bore, RoundTrait.Drum, RoundTrait.Warhead, RoundTrait.Lash, RoundTrait.Pin,
		RoundTrait.Rush, RoundTrait.Dodge, RoundTrait.Snap,
		RoundTrait.Mirv, RoundTrait.Bloom, RoundTrait.Scorch, RoundTrait.Lance, RoundTrait.Crater, RoundTrait.Spot,
		RoundTrait.Deep, RoundTrait.Awl, RoundTrait.Ram, RoundTrait.Keel,
		RoundTrait.Belt, RoundTrait.Walk, RoundTrait.Spool, RoundTrait.Sight, RoundTrait.Bite, RoundTrait.Link,
		RoundTrait.Sear, RoundTrait.Kiln, RoundTrait.Arc, RoundTrait.Fork, RoundTrait.Shunt, RoundTrait.Linger,
		RoundTrait.Cell,
		RoundTrait.Jack, RoundTrait.Slap, RoundTrait.Rack, RoundTrait.Draw,
		RoundTrait.Feed, RoundTrait.Eject, RoundTrait.Vent, RoundTrait.Cool,
		RoundTrait.Shuck, RoundTrait.Slam
	};

	public static readonly RoundTrait[] Roots =
	{
		RoundTrait.Buck, RoundTrait.Bore, RoundTrait.Drum, RoundTrait.Warhead, RoundTrait.Lash, RoundTrait.Pin
	};

	public static readonly RoundTrait[] ShotgunBranch =
	{
		RoundTrait.Shuck, RoundTrait.Slam
	};

	public static readonly RoundTrait[] RocketBranch =
	{
		RoundTrait.Mirv, RoundTrait.Bloom, RoundTrait.Scorch, RoundTrait.Lance, RoundTrait.Crater, RoundTrait.Spot,
		RoundTrait.Jack, RoundTrait.Slap
	};

	public static readonly RoundTrait[] RailBranch =
	{
		RoundTrait.Deep, RoundTrait.Awl, RoundTrait.Ram, RoundTrait.Keel,
		RoundTrait.Rack, RoundTrait.Draw
	};

	public static readonly RoundTrait[] RifleBranch =
	{
		RoundTrait.Belt, RoundTrait.Walk, RoundTrait.Spool, RoundTrait.Sight, RoundTrait.Bite, RoundTrait.Link,
		RoundTrait.Feed, RoundTrait.Eject
	};

	public static readonly RoundTrait[] LaserBranch =
	{
		RoundTrait.Sear, RoundTrait.Kiln, RoundTrait.Arc, RoundTrait.Fork, RoundTrait.Shunt, RoundTrait.Linger,
		RoundTrait.Cell, RoundTrait.Vent, RoundTrait.Cool
	};

	public static RoundTrait[] BranchOf( RoundTrait trait ) => trait switch
	{
		RoundTrait.Buck => ShotgunBranch,
		RoundTrait.Bore => RailBranch,
		RoundTrait.Drum => RifleBranch,
		RoundTrait.Warhead => RocketBranch,
		RoundTrait.Lash => LaserBranch,
		_ => null
	};

	public static string UnlockList( RoundTrait trait )
	{
		var branch = BranchOf( trait );
		if ( branch is null )
			return "";

		var names = new List<string>();
		foreach ( var item in branch )
		{
			if ( Off( item ) )
				continue;

			names.Add( Title( item ) );
		}

		return names.Count == 0 ? "" : string.Join( ", ", names );
	}

	public static TraitPack Pack( RoundTrait trait ) => trait switch
	{
		RoundTrait.Split or RoundTrait.Fan or RoundTrait.Pump => TraitPack.Entry,
		RoundTrait.Load or RoundTrait.Choke or RoundTrait.Meat or RoundTrait.Rico => TraitPack.Junior,
		RoundTrait.Gape or RoundTrait.Double or RoundTrait.Kick or RoundTrait.Stun => TraitPack.Warrior,
		RoundTrait.Heap or RoundTrait.Waste or RoundTrait.Breach or RoundTrait.Slug => TraitPack.Abomination,
		RoundTrait.Buck or RoundTrait.Shuck or RoundTrait.Slam => TraitPack.Shotgun,
		RoundTrait.Bore or RoundTrait.Deep or RoundTrait.Awl or RoundTrait.Ram or RoundTrait.Mass or RoundTrait.Keel or RoundTrait.Trace or RoundTrait.Rack or RoundTrait.Draw => TraitPack.Rail,
		RoundTrait.Drum or RoundTrait.Rush or RoundTrait.Belt or RoundTrait.Walk or RoundTrait.Spool or RoundTrait.Sight or RoundTrait.Bite or RoundTrait.Link or RoundTrait.Feed or RoundTrait.Eject => TraitPack.Rifle,
		RoundTrait.Warhead or RoundTrait.Mirv or RoundTrait.Bloom or RoundTrait.Scorch or RoundTrait.Lance or RoundTrait.Crater or RoundTrait.Spot or RoundTrait.Jack or RoundTrait.Slap => TraitPack.Rocket,
		RoundTrait.Lash or RoundTrait.Sear or RoundTrait.Kiln or RoundTrait.Arc or RoundTrait.Fork or RoundTrait.Shunt or RoundTrait.Linger or RoundTrait.Cell or RoundTrait.Vent or RoundTrait.Cool => TraitPack.Laser,
		_ => TraitPack.Nailgun
	};

	public static bool Generic( RoundTrait trait ) => Pack( trait ) is TraitPack.Entry or TraitPack.Junior or TraitPack.Warrior or TraitPack.Abomination;

	public static bool OneShot( RoundTrait trait )
	{
		if ( trait is RoundTrait.Split or RoundTrait.Meat )
			return false;

		return NeedsBuck( trait ) || NeedsWarhead( trait ) || NeedsBore( trait ) || NeedsDrum( trait ) || NeedsLash( trait ) || Generic( trait );
	}

	public static bool NeedsBuck( RoundTrait trait ) => trait is RoundTrait.Shuck or RoundTrait.Slam;

	public static bool IsCluster( RoundTrait trait ) => trait is RoundTrait.Mirv or RoundTrait.Bloom or RoundTrait.Scorch;

	public static bool IsLance( RoundTrait trait ) => trait is RoundTrait.Lance or RoundTrait.Crater;

	public static bool NeedsWarhead( RoundTrait trait ) => IsCluster( trait ) || IsLance( trait ) || trait is RoundTrait.Spot or RoundTrait.Jack or RoundTrait.Slap;

	public static bool OwnsCluster( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Mirv ) || loadout.Has( RoundTrait.Bloom ) || loadout.Has( RoundTrait.Scorch ));

	public static bool OwnsLance( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Lance ) || loadout.Has( RoundTrait.Crater ));

	public static bool IsDeep( RoundTrait trait ) => trait is RoundTrait.Deep or RoundTrait.Awl or RoundTrait.Ram;

	public static bool IsMass( RoundTrait trait ) => trait == RoundTrait.Keel;

	public static bool NeedsBore( RoundTrait trait ) => IsDeep( trait ) || IsMass( trait ) || trait is RoundTrait.Rack or RoundTrait.Draw;

	public static bool OwnsDeep( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Deep ) || loadout.Has( RoundTrait.Awl ) || loadout.Has( RoundTrait.Ram ));

	public static bool OwnsMass( RunLoadout loadout ) => loadout is not null && loadout.Has( RoundTrait.Keel );

	public static bool IsSweep( RoundTrait trait ) => trait is RoundTrait.Belt or RoundTrait.Walk;

	public static bool IsTrack( RoundTrait trait ) => trait is RoundTrait.Spool or RoundTrait.Sight or RoundTrait.Bite;

	public static bool NeedsDrum( RoundTrait trait ) => IsSweep( trait ) || IsTrack( trait ) || trait is RoundTrait.Link or RoundTrait.Feed or RoundTrait.Eject;

	public static bool OwnsSweep( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Belt ) || loadout.Has( RoundTrait.Walk ));

	public static bool OwnsTrack( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Spool ) || loadout.Has( RoundTrait.Sight ) || loadout.Has( RoundTrait.Bite ));

	public static bool IsBrand( RoundTrait trait ) => trait is RoundTrait.Sear or RoundTrait.Kiln;

	public static bool IsArc( RoundTrait trait ) => trait is RoundTrait.Arc or RoundTrait.Fork;

	public static bool NeedsLash( RoundTrait trait ) => IsBrand( trait ) || IsArc( trait ) || trait is RoundTrait.Shunt or RoundTrait.Linger or RoundTrait.Cell or RoundTrait.Vent or RoundTrait.Cool;

	public static bool OwnsBrand( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Sear ) || loadout.Has( RoundTrait.Kiln ));

	public static bool OwnsArc( RunLoadout loadout ) => loadout is not null && (loadout.Has( RoundTrait.Arc ) || loadout.Has( RoundTrait.Fork ));

	public static bool TooEarly( RoundTrait trait, int lap )
		=> trait == RoundTrait.Snap && lap < GameSettings.Traits.SnapUnlockLap;

	public static bool Off( RoundTrait trait ) => trait is RoundTrait.Link or RoundTrait.Pump or RoundTrait.Load or RoundTrait.Gape or RoundTrait.Heap or RoundTrait.Waste or RoundTrait.Breach or RoundTrait.Mass or RoundTrait.Trace;

	public static bool Blocked( RoundTrait trait, RunLoadout loadout )
	{
		if ( Off( trait ) )
			return true;

		if ( loadout is null )
			return false;

		if ( NeedsBuck( trait ) && !loadout.Has( RoundTrait.Buck ) )
			return true;

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

		if ( trait == RoundTrait.Slam && !loadout.Has( RoundTrait.Shuck ) )
			return true;

		if ( trait == RoundTrait.Slap && !loadout.Has( RoundTrait.Jack ) )
			return true;

		if ( trait == RoundTrait.Draw && !loadout.Has( RoundTrait.Rack ) )
			return true;

		if ( trait == RoundTrait.Eject && !loadout.Has( RoundTrait.Feed ) )
			return true;

		if ( trait == RoundTrait.Cool && !loadout.Has( RoundTrait.Vent ) )
			return true;

		return false;
	}

	public static RoundTrait? Rival( RoundTrait trait ) => trait switch
	{
		RoundTrait.Lash => RoundTrait.Bore,
		RoundTrait.Bore => RoundTrait.Lash,
		_ => null
	};

	public static int MaxLevel( RoundTrait trait )
	{
		if ( trait == RoundTrait.Split )
			return 4;

		if ( trait == RoundTrait.Meat )
			return 2;

		return OneShot( trait ) ? 1 : GameSettings.Traits.MaxLevel;
	}

	public static string Code( RoundTrait trait ) => GameSettings.Text.TraitCode( trait );
	public static string Title( RoundTrait trait ) => GameSettings.Text.TraitTitle( trait );
	public static string Blurb( RoundTrait trait ) => GameSettings.Text.TraitBlurb( trait );

	public static Color Color( RoundTrait trait ) => Pack( trait ) switch
	{
		TraitPack.Rifle or TraitPack.Shotgun => new Color( 0.616f, 0.616f, 0.616f ),
		TraitPack.Nailgun or TraitPack.Junior => new Color( 0.118f, 1f, 0f ),
		TraitPack.Warrior or TraitPack.Laser or TraitPack.Rail or TraitPack.Rocket => new Color( 0f, 0.439f, 0.867f ),
		TraitPack.Abomination => new Color( 0.639f, 0.208f, 0.933f ),
		TraitPack.Entry => new Color( 0.95f, 0.95f, 0.95f ),
		_ => new Color( 0f, 0.439f, 0.867f )
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
		RoundTrait.Lash or RoundTrait.Sear or RoundTrait.Kiln or RoundTrait.Arc or RoundTrait.Fork or RoundTrait.Shunt or RoundTrait.Linger or RoundTrait.Cell => "ui/traits/electric.png",
		RoundTrait.Jack or RoundTrait.Slap or RoundTrait.Rack or RoundTrait.Draw or RoundTrait.Feed or RoundTrait.Eject or RoundTrait.Vent or RoundTrait.Cool or RoundTrait.Shuck or RoundTrait.Slam => "ui/traits/snap.png",
		RoundTrait.Spin => "ui/traits/clockwise.png",
		RoundTrait.Rush => "ui/traits/step.png",
		RoundTrait.Dodge => "ui/traits/graze.png",
		RoundTrait.Snap => "ui/traits/snap.png",
		_ => "ui/traits/stick.png"
	};
}
