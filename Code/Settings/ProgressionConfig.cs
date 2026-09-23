namespace LoopedLoaded;

[AssetType( Name = "Progression Config", Extension = "omrprog", Category = "Looped Loaded" )]
public class ProgressionConfig : GameResource
{
	[Property] public float ThreatRatio { get; set; } = 1.08f;
	[Property] public float SwarmRatio { get; set; } = 1.4f;
	[Property] public float PowerRatio { get; set; } = 1.6f;
	[Property] public float CostRatio { get; set; } = 2f;
	[Property] public float TraitRatio { get; set; } = 1.4f;
	[Property] public float PaceRatio { get; set; } = 1.035f;
	[Property] public float PaceCap { get; set; } = 1.4f;
	[Property] public float DashRatio { get; set; } = 1.16f;
	[Property] public float DashScaleFloor { get; set; } = 0.42f;
	[Property] public float RoundRatio { get; set; } = 1.25f;
	[Property] public int MaxSlots { get; set; } = 9;
	[Property] public int BossBaseHealth { get; set; } = 18;
	[Property] public float ExtraBodiesScale { get; set; } = 2f;
	[Property] public int ExtraBodiesOffset { get; set; } = 0;
	[Property] public int ExtraBodiesMax { get; set; } = 14;
	[Property] public int WaveCopies { get; set; } = 2;
	[Property] public float KillScrapScale { get; set; } = 0.5f;
	[Property] public int RoundsGrantedMin { get; set; } = 1;
	[Property] public int RoundsGrantedMax { get; set; } = 4;
	[Property] public int SoftRuns { get; set; } = 3;
	[Property] public float SlowDrainBase { get; set; } = 0.55f;
	[Property] public float SlowDrainFloor { get; set; } = 0.22f;
}
