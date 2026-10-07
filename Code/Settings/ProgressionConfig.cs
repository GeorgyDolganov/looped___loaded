namespace LoopedLoaded;

[AssetType( Name = "Progression Config", Extension = "omrprog", Category = "Looped Loaded" )]
public class ProgressionConfig : GameResource
{
	[Property] public float ThreatRatio { get; set; } = 1.04f;
	[Property] public float SwarmRatio { get; set; } = 1.17f;
	[Property] public float PowerRatio { get; set; } = 1.6f;
	[Property] public float CostRatio { get; set; } = 2.5f;
	[Property] public float TraitRatio { get; set; } = 1.4f;
	[Property] public float PaceRatio { get; set; } = 1.018f;
	[Property] public float PaceCap { get; set; } = 1.4f;
	[Property] public float DashRatio { get; set; } = 1.16f;
	[Property] public float DashScaleFloor { get; set; } = 0.40f;
	[Property] public int MaxSlots { get; set; } = 9;
	[Property] public int BossBaseHealth { get; set; } = 18;
	[Property] public float ExtraBodiesScale { get; set; } = 1f;
	[Property] public int ExtraBodiesOffset { get; set; } = 0;
	[Property] public int ExtraBodiesMax { get; set; } = 14;
	[Property] public int WaveCopies { get; set; } = 2;
	[Property] public float KillScrapScale { get; set; } = 0.5f;
	[Property] public float WaveHealthScale { get; set; } = 0.75f;
	[Property] public float SlowDrainBase { get; set; } = 0.55f;
	[Property] public float SlowDrainFloor { get; set; } = 0.22f;
}
