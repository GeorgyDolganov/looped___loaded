namespace LoopedLoaded;

[AssetType( Name = "Run Config", Extension = "omrrun", Category = "Looped Loaded" )]
public class RunConfig : GameResource
{
	[Property] public int BossOfferLap { get; set; } = 10;
	[Property] public int WinBiomass { get; set; } = 100;
	[Property] public int FinalStashMul { get; set; } = 2;
	[Property] public int RingHeal { get; set; } = 1;
	[Property] public float IFrames { get; set; } = 1.05f;
	[Property] public float HurtFlash { get; set; } = 0.55f;
}
