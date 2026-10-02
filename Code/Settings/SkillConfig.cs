namespace LoopedLoaded;

[AssetType( Name = "Skill Config", Extension = "omrskill", Category = "Looped Loaded" )]
public class SkillConfig : GameResource
{
	[Property] public RepulseStats Repulse { get; set; } = new();
	[Property] public BackdashStats Backdash { get; set; } = new();
}

public class RepulseStats
{
	[Property] public TraitTiers Cooldown { get; set; } = new() { Level1 = 8f, Level2 = 6f, Level3 = 4.5f };
	[Property] public TraitTiers Radius { get; set; } = new() { Level1 = 466.67f, Level2 = 583f, Level3 = 700f };
	[Property] public TraitTiers Force { get; set; } = new() { Level1 = 900f, Level2 = 1200f, Level3 = 1550f };
	[Property] public float EdgeScale { get; set; } = 0.45f;
	[Property] public TraitTiers StunTime { get; set; } = new() { Level1 = 0.35f, Level2 = 0.45f, Level3 = 0.6f };

	public float CooldownAt( int level ) => Rank( Cooldown, level, 8f );
	public float RadiusAt( int level ) => Rank( Radius, level, 466.67f );
	public float ForceAt( int level ) => Rank( Force, level, 900f );
	public float StunAt( int level ) => Rank( StunTime, level, 0.35f );

	static float Rank( TraitTiers tiers, int level, float fallback )
	{
		if ( tiers is null )
			return fallback;

		return tiers.At( Math.Clamp( level, 1, 3 ) );
	}
}

public class BackdashStats
{
	[Property] public float Cooldown { get; set; } = 3f;
}
