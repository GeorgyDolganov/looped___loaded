namespace LoopedLoaded;

[AssetType( Name = "Run Config", Extension = "omrrun", Category = "Looped Loaded" )]
public class RunConfig : GameResource
{
	[Property] public int BossOfferLap { get; set; } = 10;
	[Property] public int WinBiomass { get; set; } = 150;
	[Property] public float AscendWinRatio { get; set; } = 1.5f;
	[Property] public float AscendHealthRatio { get; set; } = 1.3f;
	[Property] public float AscendRewardRatio { get; set; } = 1.2f;
	[Property] public int FinalStashMul { get; set; } = 2;
	[Property] public int RingHeal { get; set; } = 1;
	[Property] public float IFrames { get; set; } = 1.05f;
	[Property] public float HurtFlash { get; set; } = 0.55f;
}
