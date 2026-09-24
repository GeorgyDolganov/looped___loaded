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
	[Property] public float MeatRangeCut { get; set; } = 1f;
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
	[Property] public float WasteRangeCut { get; set; } = 1f;
	[Property] public int BreachPierce { get; set; } = 1;
	[Property] public float SlugRadius { get; set; } = 22f;
	[Property] public int SlugDamage { get; set; } = 2;
	[Property] public float SlugFalloffPad { get; set; } = 120f;
	[Property] public TraitTiers BuckPellets { get; set; } = new() { Level1 = 2f, Level2 = 3f, Level3 = 5f };
	[Property] public TraitTiers BuckCone { get; set; } = new() { Level1 = 10f, Level2 = 16f, Level3 = 24f };
	[Property] public TraitTiers BuckRangeCut { get; set; } = new() { Level1 = 0.83f, Level2 = 0.94f, Level3 = 1f };
	[Property] public TraitTiers BorePierce { get; set; } = new() { Level1 = 1f, Level2 = 1f, Level3 = 2f };
	[Property] public float BoreReload { get; set; } = 0.55f;
	[Property] public TraitTiers DrumBurst { get; set; } = new() { Level1 = 3f, Level2 = 4f, Level3 = 6f };
	[Property] public float DrumCycle { get; set; } = 0.16f;
	[Property] public float DrumReload { get; set; } = 0.70f;
	[Property] public TraitTiers WarheadRadius { get; set; } = new() { Level1 = 90f, Level2 = 126f, Level3 = 176f };
	[Property] public TraitTiers WarheadSpeed { get; set; } = new() { Level1 = 0.78f, Level2 = 0.68f, Level3 = 0.58f };
	[Property] public float MirvRadiusScale { get; set; } = 0.55f;
	[Property] public float MirvSpeed { get; set; } = 0.80f;
	[Property] public float BloomRadius { get; set; } = 80f;
	[Property] public float BloomReload { get; set; } = 0.30f;
	[Property] public int ScorchDamage { get; set; } = 2;
	[Property] public float ScorchSpeed { get; set; } = 0.75f;
	[Property] public float ScorchReload { get; set; } = 0.20f;
	[Property] public int LanceDamage { get; set; } = 2;
	[Property] public float LanceRadiusScale { get; set; } = 0.70f;
	[Property] public float LanceSpeed { get; set; } = 0.70f;
	[Property] public float LanceReload { get; set; } = 0.45f;
	[Property] public float CraterBody { get; set; } = 22f;
	[Property] public float CraterSplash { get; set; } = 56f;
	[Property] public float CraterSpeed { get; set; } = 0.65f;
	[Property] public float SpotSpeed { get; set; } = 0.85f;
	[Property] public int DeepPierce { get; set; } = 2;
	[Property] public float DeepReload { get; set; } = 0.40f;
	[Property] public float AwlSpeed { get; set; } = 0.85f;
	[Property] public float AwlReload { get; set; } = 0.20f;
	[Property] public float RamSpeed { get; set; } = 0.75f;
	[Property] public int MassDamage { get; set; } = 3;
	[Property] public float MassSpeed { get; set; } = 0.70f;
	[Property] public float MassReload { get; set; } = 0.45f;
	[Property] public int KeelDamage { get; set; } = 2;
	[Property] public float KeelSpeed { get; set; } = 0.80f;
	[Property] public float TraceSpeed { get; set; } = 1.60f;
	[Property] public float TraceReload { get; set; } = 0.25f;
	[Property] public int BeltBurst { get; set; } = 3;
	[Property] public float BeltReload { get; set; } = 0.45f;
	[Property] public float WalkCone { get; set; } = 3f;
	[Property] public float WalkReload { get; set; } = 0.20f;
	[Property] public float SpoolCycle { get; set; } = 0.65f;
	[Property] public float SpoolReload { get; set; } = 0.25f;
	[Property] public float SightSpeed { get; set; } = 0.90f;
	[Property] public float BiteSpeed { get; set; } = 0.80f;
	[Property] public float LinkCycle { get; set; } = 1.10f;
	[Property] public float LinkReload { get; set; } = 0.20f;
	[Property] public float LashPad { get; set; } = 0.40f;
	[Property] public float LashPerSecond { get; set; } = 0.50f;
	[Property] public float LashMaxHold { get; set; } = 1.1f;
	[Property] public int LashHit { get; set; } = 1;
	[Property] public TraitTiers LashTick { get; set; } = new() { Level1 = 1f, Level2 = 0.8f, Level3 = 0.5f };
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
	[Property] public TraitTiers DodgeChance { get; set; } = new() { Level1 = 0.10f, Level2 = 0.20f, Level3 = 0.32f };
	[Property] public int SnapUnlockLap { get; set; } = 3;
	[Property] public int SnapPrice { get; set; } = 4;
	[Property] public TraitTiers SnapReload { get; set; } = new() { Level1 = 0.80f, Level2 = 0.64f, Level3 = 0.50f };

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
