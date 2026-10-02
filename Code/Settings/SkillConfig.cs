namespace LoopedLoaded;

[AssetType( Name = "Skill Config", Extension = "omrskill", Category = "Looped Loaded" )]
public class SkillConfig : GameResource
{
	[Property] public RepulseStats Repulse { get; set; } = new();
}

public class RepulseStats
{
	[Property] public float Cooldown { get; set; } = 8f;
	[Property] public float Radius { get; set; } = 466.67f;
	[Property] public float Force { get; set; } = 900f;
	[Property] public float EdgeScale { get; set; } = 0.45f;
	[Property] public float StunTime { get; set; } = 0.35f;
}
