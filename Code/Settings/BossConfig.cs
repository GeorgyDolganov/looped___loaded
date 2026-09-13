namespace LoopedLoaded;

[AssetType( Name = "Boss Config", Extension = "llboss", Category = "Looped Loaded" )]
public class BossConfig : GameResource
{
	[Property] public float Phase2Health { get; set; } = 0.66f;
	[Property] public float Phase3Health { get; set; } = 0.33f;
	[Property] public float LensRadius { get; set; } = 88f;
	[Property] public float LensBlock { get; set; } = 0.55f;
	[Property] public int LensBreakPhase1 { get; set; } = 2;
	[Property] public int LensBreakLater { get; set; } = 3;
	[Property] public CoreBossStats Core { get; set; } = new();
	[Property] public LensBossStats Lens { get; set; } = new();

	public int PhaseOf( float part )
	{
		if ( part > Phase2Health )
			return 1;
		if ( part > Phase3Health )
			return 2;
		return 3;
	}
}

public class CoreBossStats
{
	[Property] public float FirstShot { get; set; } = 0.9f;
	[Property] public float FirstPulse { get; set; } = 2.4f;
	[Property] public float Spin { get; set; } = 0.22f;
	[Property] public float SpinPerPhase { get; set; } = 0.12f;
	[Property] public float ShotFloor { get; set; } = 0.55f;
	[Property] public float ShotPhase1 { get; set; } = 2.1f;
	[Property] public float ShotPhase2 { get; set; } = 1.55f;
	[Property] public float ShotPhase3 { get; set; } = 1.15f;
	[Property] public float AimedSpeed { get; set; } = 520f;
	[Property] public float AimedOrigin { get; set; } = 40f;
	[Property] public float BurstSpeed { get; set; } = 480f;
	[Property] public float BurstOrigin { get; set; } = 50f;
	[Property] public int BurstPhase2 { get; set; } = 5;
	[Property] public int BurstPhase3 { get; set; } = 8;
	[Property] public float PulseSpeed { get; set; } = 620f;
	[Property] public float PulseReachPad { get; set; } = 22f;
	[Property] public float PulseEndPad { get; set; } = 40f;
	[Property] public float PulsePhase2 { get; set; } = 3.4f;
	[Property] public float PulsePhase3 { get; set; } = 2.6f;
	[Property] public int SpokesPhase1 { get; set; } = 0;
	[Property] public int SpokesPhase2 { get; set; } = 2;
	[Property] public int SpokesPhase3 { get; set; } = 4;
	[Property] public float SpokeInnerPad { get; set; } = 40f;
	[Property] public float SpokeLength { get; set; } = 280f;
	[Property] public int GuardHealth { get; set; } = 2;
	[Property] public float GuardPad { get; set; } = 220f;
	[Property] public float FreezeShotLock { get; set; } = 0.2f;
}

public class LensBossStats
{
	[Property] public float FirstShot { get; set; } = 0.85f;
	[Property] public float ShotFloor { get; set; } = 0.6f;
	[Property] public float ShotPhase1 { get; set; } = 2.0f;
	[Property] public float ShotPhase2 { get; set; } = 1.45f;
	[Property] public float ShotPhase3 { get; set; } = 1.1f;
	[Property] public float AimedSpeed { get; set; } = 500f;
	[Property] public float AimedOrigin { get; set; } = 36f;
	[Property] public float AimedSpin { get; set; } = 0.4f;
	[Property] public float BurstSpeed { get; set; } = 460f;
	[Property] public float BurstOrigin { get; set; } = 44f;
	[Property] public float BurstSpin { get; set; } = 0.35f;
	[Property] public int BurstPhase2 { get; set; } = 4;
	[Property] public int BurstPhase3 { get; set; } = 6;
	[Property] public float FreezeShotLock { get; set; } = 0.2f;
	[Property] public float LeadMinSpeed { get; set; } = 80f;
}
