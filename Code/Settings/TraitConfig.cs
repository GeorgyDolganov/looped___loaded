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
	[Property] public TraitPackStats Fuse { get; set; } = new() { Price = 2, Weight = 5 };
	[Property] public TraitPackStats Shell { get; set; } = new() { Price = 3, Weight = 3 };
	[Property] public TraitPackStats Payload { get; set; } = new() { Price = 5, Weight = 2 };
	[Property] public TraitPackStats Silo { get; set; } = new() { Price = 8, Weight = 1 };
	[Property] public TraitPackStats Entry { get; set; } = new() { Price = 2, Weight = 6 };
	[Property] public TraitPackStats Junior { get; set; } = new() { Price = 3, Weight = 4 };
	[Property] public TraitPackStats Warrior { get; set; } = new() { Price = 5, Weight = 2 };
	[Property] public TraitPackStats Abomination { get; set; } = new() { Price = 8, Weight = 1 };
	[Property] public int SplitPellets { get; set; } = 1;
	[Property] public float FanCone { get; set; } = 14f;
	[Property] public int PumpPellets { get; set; } = 1;
	[Property] public float PumpReload { get; set; } = 0.25f;
	[Property] public int LoadPellets { get; set; } = 2;
	[Property] public float LoadReload { get; set; } = 0.15f;
	[Property] public float ChokeCone { get; set; } = 10f;
	[Property] public float ChokeFloor { get; set; } = 6f;
	[Property] public float MeatRange { get; set; } = 140f;
	[Property] public int MeatBonus { get; set; } = 1;
	[Property] public float MeatFalloff { get; set; } = 280f;
	[Property] public int RicoBounces { get; set; } = 1;
	[Property] public float GapeCone { get; set; } = 14f;
	[Property] public float DoubleGap { get; set; } = 0.12f;
	[Property] public float DoubleReload { get; set; } = 0.55f;
	[Property] public float KickForce { get; set; } = 110f;
	[Property] public float KickRange { get; set; } = 180f;
	[Property] public float StunTime { get; set; } = 0.45f;
	[Property] public float StunRange { get; set; } = 160f;
	[Property] public int HeapPellets { get; set; } = 3;
	[Property] public float HeapReload { get; set; } = 0.40f;
	[Property] public float WasteRange { get; set; } = 80f;
	[Property] public int WasteBonus { get; set; } = 1;
	[Property] public float WasteFalloff { get; set; } = 150f;
	[Property] public int BreachPierce { get; set; } = 1;
	[Property] public float SlugRadius { get; set; } = 22f;
	[Property] public int SlugDamage { get; set; } = 2;
	[Property] public float SlugFalloffPad { get; set; } = 120f;
	[Property] public TraitTiers BuckPellets { get; set; } = new() { Level1 = 2f, Level2 = 3f, Level3 = 5f };
	[Property] public TraitTiers BuckCone { get; set; } = new() { Level1 = 10f, Level2 = 16f, Level3 = 24f };
	[Property] public TraitTiers BuckFalloff { get; set; } = new() { Level1 = 260f, Level2 = 220f, Level3 = 180f };
	[Property] public TraitTiers BorePierce { get; set; } = new() { Level1 = 1f, Level2 = 1f, Level3 = 2f };
	[Property] public float BoreReload { get; set; } = 0.55f;
	[Property] public TraitTiers DrumBurst { get; set; } = new() { Level1 = 3f, Level2 = 4f, Level3 = 6f };
	[Property] public float DrumCycle { get; set; } = 0.16f;
	[Property] public float DrumReload { get; set; } = 0.70f;
	[Property] public TraitTiers WarheadRadius { get; set; } = new() { Level1 = 48f, Level2 = 70f, Level3 = 96f };
	[Property] public TraitTiers WarheadSpeed { get; set; } = new() { Level1 = 0.78f, Level2 = 0.68f, Level3 = 0.58f };
	[Property] public int SplashDamageBase { get; set; } = 1;
	[Property] public float FuseSplash { get; set; } = 36f;
	[Property] public float FuseSpeed { get; set; } = 0.90f;
	[Property] public float FuseReload { get; set; } = 0.10f;
	[Property] public float FatRadius { get; set; } = 22f;
	[Property] public float FatSpeed { get; set; } = 0.90f;
	[Property] public float EmberSplash { get; set; } = 18f;
	[Property] public float EmberFalloff { get; set; } = 280f;
	[Property] public float BlastSplash { get; set; } = 32f;
	[Property] public float BlastReload { get; set; } = 0.18f;
	[Property] public int CrackDamage { get; set; } = 1;
	[Property] public float MineSplash { get; set; } = 22f;
	[Property] public int ClusterPellets { get; set; } = 2;
	[Property] public float ClusterCone { get; set; } = 14f;
	[Property] public float ClusterSplashMul { get; set; } = 0.62f;
	[Property] public float ClusterReload { get; set; } = 0.22f;
	[Property] public float NapalmTime { get; set; } = 0.55f;
	[Property] public float ShoveForce { get; set; } = 150f;
	[Property] public float CoreSplash { get; set; } = 24f;
	[Property] public int CoreDamage { get; set; } = 1;
	[Property] public float CoreReload { get; set; } = 0.40f;
	[Property] public float CoreSpeed { get; set; } = 0.85f;
	[Property] public float SafeMul { get; set; } = 0.72f;
	[Property] public float NukeSplash { get; set; } = 70f;
	[Property] public int NukeDamage { get; set; } = 1;
	[Property] public float NukeReload { get; set; } = 0.50f;
	[Property] public float NukeSpeed { get; set; } = 0.58f;
	[Property] public float NukeRadius { get; set; } = 26f;
	[Property] public float LashPad { get; set; } = 0.40f;
	[Property] public float LashPerSecond { get; set; } = 0.50f;
	[Property] public float LashMaxHold { get; set; } = 1.1f;
	[Property] public int LashHit { get; set; } = 1;
	[Property] public TraitTiers LashTick { get; set; } = new() { Level1 = 0.70f, Level2 = 0.55f, Level3 = 0.42f };
	[Property] public float LashRange { get; set; } = 1600f;
	[Property] public float LashWidth { get; set; } = 8f;
	[Property] public TraitTiers PinNails { get; set; } = new() { Level1 = 2f, Level2 = 3f, Level3 = 5f };
	[Property] public TraitTiers PinBounce { get; set; } = new() { Level1 = 1f, Level2 = 2f, Level3 = 3f };
	[Property] public TraitTiers PinCone { get; set; } = new() { Level1 = 8f, Level2 = 10f, Level3 = 12f };
	[Property] public float PinRadius { get; set; } = 6f;
	[Property] public float PinStick { get; set; } = 0.6f;
	[Property] public float SpinBase { get; set; } = 220f;
	[Property] public TraitTiers SpinBoost { get; set; } = new() { Level1 = 140f, Level2 = 260f, Level3 = 420f };
	[Property] public float SpinReload { get; set; } = 0.12f;
	[Property] public TraitTiers RushSpeed { get; set; } = new() { Level1 = 1.20f, Level2 = 1.40f, Level3 = 1.65f };
	[Property] public float RushReload { get; set; } = 0.18f;

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
		TraitPack.Abomination => Abomination ??= new() { Price = 8, Weight = 1 },
		TraitPack.Fuse => Fuse ??= new() { Price = 2, Weight = 5 },
		TraitPack.Shell => Shell ??= new() { Price = 3, Weight = 3 },
		TraitPack.Payload => Payload ??= new() { Price = 5, Weight = 2 },
		TraitPack.Silo => Silo ??= new() { Price = 8, Weight = 1 },
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
