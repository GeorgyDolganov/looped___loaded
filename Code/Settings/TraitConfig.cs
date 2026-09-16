namespace LoopedLoaded;

[AssetType( Name = "Trait Config", Extension = "omrtrait", Category = "Looped Loaded" )]
public class TraitConfig : GameResource
{
	[Property] public int MaxLevel { get; set; } = 3;
	[Property] public int BaseDamage { get; set; } = 1;
	[Property] public int MaxBouncesBase { get; set; } = 1;
	[Property] public float EnergyBase { get; set; } = 5500f;
	[Property] public float ReloadBase { get; set; } = 0.70f;
	[Property] public float ReloadMin { get; set; } = 0.20f;
	[Property] public TraitPackStats Rifle { get; set; } = new() { Price = 2, Weight = 5 };
	[Property] public TraitPackStats Shotgun { get; set; } = new() { Price = 2, Weight = 5 };
	[Property] public TraitPackStats Nailgun { get; set; } = new() { Price = 2, Weight = 4 };
	[Property] public TraitPackStats Laser { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Rail { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Rocket { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitTiers BuckPellets { get; set; } = new() { Level1 = 5f, Level2 = 7f, Level3 = 9f };
	[Property] public TraitTiers BuckCone { get; set; } = new() { Level1 = 18f, Level2 = 24f, Level3 = 32f };
	[Property] public TraitTiers BuckFalloff { get; set; } = new() { Level1 = 340f, Level2 = 280f, Level3 = 220f };
	[Property] public TraitTiers BorePierce { get; set; } = new() { Level1 = 1f, Level2 = 2f, Level3 = 4f };
	[Property] public float BoreReload { get; set; } = 0.35f;
	[Property] public TraitTiers DrumBurst { get; set; } = new() { Level1 = 6f, Level2 = 8f, Level3 = 10f };
	[Property] public float DrumCycle { get; set; } = 0.09f;
	[Property] public float DrumReload { get; set; } = 0.45f;
	[Property] public TraitTiers WarheadRadius { get; set; } = new() { Level1 = 90f, Level2 = 126f, Level3 = 176f };
	[Property] public TraitTiers WarheadSpeed { get; set; } = new() { Level1 = 0.70f, Level2 = 0.60f, Level3 = 0.52f };
	[Property] public float LashPad { get; set; } = 0.40f;
	[Property] public float LashPerSecond { get; set; } = 0.50f;
	[Property] public float LashMaxHold { get; set; } = 1.6f;
	[Property] public float LashTick { get; set; } = 0.12f;
	[Property] public float LashRange { get; set; } = 1600f;
	[Property] public float LashWidth { get; set; } = 14f;
	[Property] public TraitTiers PinNails { get; set; } = new() { Level1 = 3f, Level2 = 5f, Level3 = 7f };
	[Property] public TraitTiers PinBounce { get; set; } = new() { Level1 = 2f, Level2 = 3f, Level3 = 4f };
	[Property] public TraitTiers PinCone { get; set; } = new() { Level1 = 8f, Level2 = 10f, Level3 = 12f };
	[Property] public float PinRadius { get; set; } = 6f;
	[Property] public float PinStick { get; set; } = 0.6f;

	public TraitPackStats PackOf( TraitPack pack ) => pack switch
	{
		TraitPack.Rifle => Rifle ??= new() { Price = 2, Weight = 5 },
		TraitPack.Shotgun => Shotgun ??= new() { Price = 2, Weight = 5 },
		TraitPack.Nailgun => Nailgun ??= new() { Price = 2, Weight = 4 },
		TraitPack.Laser => Laser ??= new() { Price = 3, Weight = 3 },
		TraitPack.Rail => Rail ??= new() { Price = 3, Weight = 3 },
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

	public float At( int level ) => Progression.Tier( level, Level1, Level2, Level3 );
}

public class TraitPackStats
{
	[Property] public int Price { get; set; } = 4;
	[Property] public int Weight { get; set; } = 1;
}
