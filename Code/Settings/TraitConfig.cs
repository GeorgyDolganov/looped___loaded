namespace LoopedLoaded;

[AssetType( Name = "Trait Config", Extension = "omrtrait", Category = "Looped Loaded" )]
public class TraitConfig : GameResource
{
	[Property] public int MaxLevel { get; set; } = 3;
	[Property] public int RefreshPrice { get; set; } = 4;
	[Property] public int RefreshStep { get; set; } = 2;
	[Property] public int LockPrice { get; set; } = 5;
	[Property] public int BaseDamage { get; set; } = 1;
	[Property] public int MaxBouncesBase { get; set; } = 1;
	[Property] public float EnergyBase { get; set; } = 5500f;
	[Property] public float ReloadBase { get; set; } = 0.70f;
	[Property] public float ReloadMin { get; set; } = 0.20f;
	[Property] public float ProjectileRadius { get; set; } = 13f;
	[Property] public float DrumCycle { get; set; } = 0.16f;
	[Property] public float LashPad { get; set; } = 0.40f;
	[Property] public float LashPerSecond { get; set; } = 0.50f;
	[Property] public float LashMaxHold { get; set; } = 1.1f;
	[Property] public float LashRange { get; set; } = 1600f;
	[Property] public float LashWidth { get; set; } = 8f;
	[Property] public RarityTable Rarities { get; set; } = new();

	public int RarityPrice( Rarity rarity ) => (Rarities ??= new()).Of( rarity ).Price;
	public int RarityWeight( Rarity rarity ) => (Rarities ??= new()).Of( rarity ).Weight;
	public int RarityLap( Rarity rarity ) => Math.Max( 1, (Rarities ??= new()).Of( rarity ).UnlockLap );
	public bool RarityOpen( Rarity rarity, int lap ) => lap >= RarityLap( rarity );
}

public class TraitTiers
{
	[Property] public float Level1 { get; set; }
	[Property] public float Level2 { get; set; }
	[Property] public float Level3 { get; set; }
	[Property] public float Level4 { get; set; }

	public float At( int level )
	{
		if ( level >= 4 && Level4 != 0f )
			return Level4;

		return Progression.Tier( level, Level1, Level2, Level3 );
	}
}

public class RarityStat
{
	[Property] public int Price { get; set; } = 6;
	[Property] public int Weight { get; set; } = 1;
	[Property] public int UnlockLap { get; set; } = 1;
}

public class RarityTable
{
	[Property] public RarityStat Common { get; set; } = new() { Price = 12, Weight = 6, UnlockLap = 1 };
	[Property] public RarityStat Uncommon { get; set; } = new() { Price = 35, Weight = 4, UnlockLap = 3 };
	[Property] public RarityStat Rare { get; set; } = new() { Price = 100, Weight = 2, UnlockLap = 5 };
	[Property] public RarityStat Epic { get; set; } = new() { Price = 180, Weight = 1, UnlockLap = 7 };

	public RarityStat Of( Rarity rarity ) => rarity switch
	{
		Rarity.Uncommon => Uncommon ??= new() { Price = 35, Weight = 4, UnlockLap = 3 },
		Rarity.Rare => Rare ??= new() { Price = 100, Weight = 2, UnlockLap = 5 },
		Rarity.Epic => Epic ??= new() { Price = 180, Weight = 1, UnlockLap = 7 },
		_ => Common ??= new() { Price = 12, Weight = 6, UnlockLap = 1 }
	};
}
