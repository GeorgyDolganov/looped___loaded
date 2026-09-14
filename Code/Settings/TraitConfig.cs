namespace LoopedLoaded;

[AssetType( Name = "Trait Config", Extension = "omrtrait", Category = "Looped Loaded" )]
public class TraitConfig : GameResource
{
	[Property] public int MaxLevel { get; set; } = 3;
	[Property] public int BaseDamage { get; set; } = 1;
	[Property] public int MaxBouncesBase { get; set; } = 4;
	[Property] public float BounceStackSeed { get; set; } = 2f;
	[Property] public float EnergyBase { get; set; } = 5500f;
	[Property] public float AccelCap { get; set; } = 1.8f;
	[Property] public float PinballNaturalKeep { get; set; } = 0.94f;
	[Property] public float StickStutterScale { get; set; } = 0.7f;
	[Property] public TraitPackStats Starter { get; set; } = new() { Price = 4, Weight = 6 };
	[Property] public TraitPackStats Geometry { get; set; } = new() { Price = 8, Weight = 4 };
	[Property] public TraitPackStats Return { get; set; } = new() { Price = 10, Weight = 3 };
	[Property] public TraitPackStats Chaos { get; set; } = new() { Price = 12, Weight = 2 };
	[Property] public TraitPackStats Body { get; set; } = new() { Price = 14, Weight = 2 };
	[Property] public TraitPackStats HomingPack { get; set; } = new() { Price = 16, Weight = 1 };
	[Property] public TraitPackStats Strike { get; set; } = new() { Price = 18, Weight = 1 };
	[Property] public TraitTiers PierceCharges { get; set; } = new() { Level1 = 1f, Level2 = 2f, Level3 = 4f };
	[Property] public MagneticTrait Magnetic { get; set; } = new();
	[Property] public FreezeTrait Freeze { get; set; } = new();
	[Property] public HeavyTrait Heavy { get; set; } = new();
	[Property] public TraitTiers Accel { get; set; } = new() { Level1 = 1.12f, Level2 = 1.17f, Level3 = 1.24f };
	[Property] public TraitTiers Explosive { get; set; } = new() { Level1 = 90f, Level2 = 126f, Level3 = 176f };
	[Property] public float ElectricRange { get; set; } = 220f;
	[Property] public BloodTrait Blood { get; set; } = new();
	[Property] public TraitTiers Homing { get; set; } = new() { Level1 = 0.20f, Level2 = 0.28f, Level3 = 0.40f };
	[Property] public SkimTrait Skim { get; set; } = new();
	[Property] public KickTrait Kick { get; set; } = new();
	[Property] public StickTrait Stick { get; set; } = new();
	[Property] public TraitTiers Cushion { get; set; } = new() { Level1 = 22f, Level2 = 31f, Level3 = 43f };
	[Property] public CueTrait Cue { get; set; } = new();
	[Property] public CornerTrait Corner { get; set; } = new();
	[Property] public TraitTiers Curve { get; set; } = new() { Level1 = 4f, Level2 = 6f, Level3 = 8f };
	[Property] public TraitTiers Stutter { get; set; } = new() { Level1 = 0.08f, Level2 = 0.11f, Level3 = 0.16f };
	[Property] public TraitTiers Boomerang { get; set; } = new() { Level1 = 0.50f, Level2 = 0.70f, Level3 = 1.00f };
	[Property] public TraitTiers Reel { get; set; } = new() { Level1 = 90f, Level2 = 126f, Level3 = 176f };
	[Property] public TraitTiers Swipe { get; set; } = new() { Level1 = 28f, Level2 = 40f, Level3 = 56f };
	[Property] public TraitTiers Backstop { get; set; } = new() { Level1 = 0.5f, Level2 = 0.7f, Level3 = 1f };
	[Property] public TraitTiers Link { get; set; } = new() { Level1 = 24f, Level2 = 34f, Level3 = 47f };
	[Property] public TraitTiers Fuse { get; set; } = new() { Level1 = 70f, Level2 = 98f, Level3 = 137f };
	[Property] public SnapTrait Snap { get; set; } = new();
	[Property] public LateMagTrait LateMag { get; set; } = new();
	[Property] public TraitTiers Rim { get; set; } = new() { Level1 = 25f, Level2 = 18f, Level3 = 10f };
	[Property] public TraitTiers SecondWind { get; set; } = new() { Level1 = 0.40f, Level2 = 0.56f, Level3 = 0.78f };
	[Property] public TraitTiers Hook { get; set; } = new() { Level1 = 36f, Level2 = 50f, Level3 = 70f };
	[Property] public MarkTrait Mark { get; set; } = new();
	[Property] public PinballTrait Pinball { get; set; } = new();
	[Property] public RibbonTrait Ribbon { get; set; } = new();
	[Property] public TraitTiers Graze { get; set; } = new() { Level1 = 10f, Level2 = 14f, Level3 = 20f };
	[Property] public TraitTiers Step { get; set; } = new() { Level1 = 80f, Level2 = 112f, Level3 = 157f };
	[Property] public EchoTrait Echo { get; set; } = new();
	[Property] public RedirectTrait Redirect { get; set; } = new();

	public TraitPackStats PackOf( TraitPack pack ) => pack switch
	{
		TraitPack.Starter => Starter ??= new() { Price = 4, Weight = 6 },
		TraitPack.Geometry => Geometry ??= new() { Price = 8, Weight = 4 },
		TraitPack.Return => Return ??= new() { Price = 10, Weight = 3 },
		TraitPack.Chaos => Chaos ??= new() { Price = 12, Weight = 2 },
		TraitPack.Body => Body ??= new() { Price = 14, Weight = 2 },
		TraitPack.Homing => HomingPack ??= new() { Price = 16, Weight = 1 },
		_ => Strike ??= new() { Price = 18, Weight = 1 }
	};

	public int PackPrice( TraitPack pack ) => PackOf( pack ).Price;
	public int PackWeight( TraitPack pack ) => PackOf( pack ).Weight;

	public int BloodThreshold( int level )
	{
		var blood = Blood ??= new();
		if ( level <= 0 )
			return 0;

		return level >= MaxLevel ? blood.KillsAtMax : blood.Kills;
	}
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

public class MagneticTrait
{
	[Property] public float Radius { get; set; } = 200f;
	[Property] public float Pull { get; set; } = 1.4f;
	[Property] public float CatchBonus { get; set; } = 24f;
}

public class FreezeTrait
{
	[Property] public float Duration { get; set; } = 1.1f;
	[Property] public float Scale { get; set; } = 0.55f;
	[Property] public float ScaleFloor { get; set; } = 0.18f;
}

public class HeavyTrait
{
	[Property] public TraitTiers Speed { get; set; } = new() { Level1 = 0.82f, Level2 = 0.72f, Level3 = 0.63f };
	[Property] public TraitTiers Damage { get; set; } = new() { Level1 = 1f, Level2 = 1f, Level3 = 2f };
	[Property] public float Push { get; set; } = 42f;
}

public class BloodTrait
{
	[Property] public int Kills { get; set; } = 2;
	[Property] public int KillsAtMax { get; set; } = 1;
}

public class SkimTrait
{
	[Property] public TraitTiers Degrees { get; set; } = new() { Level1 = 18f, Level2 = 25f, Level3 = 35f };
	[Property] public TraitTiers Boost { get; set; } = new() { Level1 = 1.08f, Level2 = 1.12f, Level3 = 1.16f };
}

public class KickTrait
{
	[Property] public TraitTiers Extra { get; set; } = new() { Level1 = 8f, Level2 = 11f, Level3 = 16f };
}

public class StickTrait
{
	[Property] public TraitTiers Time { get; set; } = new() { Level1 = 0.10f, Level2 = 0.14f, Level3 = 0.20f };
	[Property] public TraitTiers Speed { get; set; } = new() { Level1 = 1.00f, Level2 = 1.10f, Level3 = 1.24f };
}

public class CueTrait
{
	[Property] public TraitTiers Degrees { get; set; } = new() { Level1 = 12f, Level2 = 8f, Level3 = 4f };
	[Property] public int Bounces { get; set; } = 1;
	[Property] public int BouncesAtMax { get; set; } = 2;
}

public class CornerTrait
{
	[Property] public TraitTiers Radius { get; set; } = new() { Level1 = 70f, Level2 = 98f, Level3 = 137f };
	[Property] public TraitTiers Pull { get; set; } = new() { Level1 = 0.7f, Level2 = 1.0f, Level3 = 1.4f };
}

public class SnapTrait
{
	[Property] public TraitTiers Speed { get; set; } = new() { Level1 = 1.08f, Level2 = 1.12f, Level3 = 1.16f };
	[Property] public float Duration { get; set; } = 0.45f;
}

public class LateMagTrait
{
	[Property] public TraitTiers Radius { get; set; } = new() { Level1 = 140f, Level2 = 196f, Level3 = 274f };
	[Property] public float Pull { get; set; } = 1.4f;
}

public class MarkTrait
{
	[Property] public TraitTiers Degrees { get; set; } = new() { Level1 = 8f, Level2 = 11f, Level3 = 16f };
	[Property] public int BonusAtMax { get; set; } = 1;
}

public class PinballTrait
{
	[Property] public TraitTiers Keep { get; set; } = new() { Level1 = 0.70f, Level2 = 0.80f, Level3 = 0.90f };
}

public class RibbonTrait
{
	[Property] public TraitTiers Width { get; set; } = new() { Level1 = 18f, Level2 = 25f, Level3 = 35f };
	[Property] public TraitTiers Slow { get; set; } = new() { Level1 = 0.20f, Level2 = 0.28f, Level3 = 0.40f };
}

public class EchoTrait
{
	[Property] public TraitTiers Keep { get; set; } = new() { Level1 = 0.40f, Level2 = 0.56f, Level3 = 0.78f };
}

public class RedirectTrait
{
	[Property] public TraitTiers Cone { get; set; } = new() { Level1 = 40f, Level2 = 56f, Level3 = 78f };
	[Property] public TraitTiers Range { get; set; } = new() { Level1 = 260f, Level2 = 364f, Level3 = 510f };
}
