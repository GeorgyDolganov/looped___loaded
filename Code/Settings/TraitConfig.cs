namespace LoopedLoaded;

[AssetType( Name = "Trait Config", Extension = "omrtrait", Category = "Looped Loaded" )]
public class TraitConfig : GameResource
{
	[Property] public int MaxLevel { get; set; } = 3;
	[Property] public int RefreshPrice { get; set; } = 2;
	[Property] public int RefreshStep { get; set; } = 1;
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
	[Property] public TraitPackStats Rifle { get; set; } = new() { Price = 2, Weight = 5 };
	[Property] public TraitPackStats Shotgun { get; set; } = new() { Price = 2, Weight = 5 };
	[Property] public TraitPackStats Nailgun { get; set; } = new() { Price = 2, Weight = 4 };
	[Property] public TraitPackStats Laser { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Rail { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Rocket { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Entry { get; set; } = new() { Price = 2, Weight = 6 };
	[Property] public TraitPackStats Junior { get; set; } = new() { Price = 3, Weight = 4 };
	[Property] public TraitPackStats Warrior { get; set; } = new() { Price = 5, Weight = 2 };
	[Property] public TraitPackStats Abomination { get; set; } = new() { Price = 8, Weight = 1, Single = true };

	public TraitPackStats PackOf( TraitPack pack ) => pack switch
	{
		TraitPack.Rifle => Rifle ??= new() { Price = 2, Weight = 5 },
		TraitPack.Shotgun => Shotgun ??= new() { Price = 2, Weight = 5 },
		TraitPack.Nailgun => Nailgun ??= new() { Price = 2, Weight = 4 },
		TraitPack.Laser => Laser ??= new() { Price = 3, Weight = 3 },
		TraitPack.Rail => Rail ??= new() { Price = 3, Weight = 3 },
		TraitPack.Entry => Entry ??= new() { Price = 2, Weight = 6 },
		TraitPack.Junior => Junior ??= new() { Price = 3, Weight = 4 },
		TraitPack.Warrior => Warrior ??= new() { Price = 5, Weight = 2 },
		TraitPack.Abomination => Abomination ??= new() { Price = 8, Weight = 1, Single = true },
		_ => Rocket ??= new() { Price = 3, Weight = 3 }
	};

	public int PackPrice( TraitPack pack ) => PackOf( pack ).Price;
	public int PackWeight( TraitPack pack ) => PackOf( pack ).Weight;
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

public class TraitPackStats
{
	[Property] public int Price { get; set; } = 4;
	[Property] public int Weight { get; set; } = 1;
	[Property] public bool Single { get; set; }
}
