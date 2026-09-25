namespace LoopedLoaded;

[AssetType( Name = "Enemy Config", Extension = "omrenemy", Category = "Looped Loaded" )]
public class EnemyConfig : GameResource
{
	[Property] public float InnerPad { get; set; } = 160f;
	[Property] public float TrackPad { get; set; } = 28f;
	[Property] public float ShieldTrackExtra { get; set; } = 8f;
	[Property] public float MeleeReachPad { get; set; } = 36f;
	[Property] public float GlimmerReveal { get; set; } = 160f;
	[Property] public float ShoveInnerPad { get; set; } = 160f;
	[Property] public EnemyKindStats Chaser { get; set; } = new() { Radius = 64f, Scrap = 4, SeekSpeed = 185f, Lead = 160f, LeadPressure = 50f };
	[Property] public EnemyKindStats Splinter { get; set; } = new() { Radius = 60f, Scrap = 6, SeekSpeed = 185f, Lead = 160f, LeadPressure = 50f };
	[Property] public EnemyKindStats Glimmer { get; set; } = new() { Radius = 56f, Scrap = 7, SeekSpeed = 155f, Lead = 120f, LeadPressure = 40f };
	[Property] public EnemyKindStats Shield { get; set; } = new() { Radius = 72f, Scrap = 7, SeekSpeed = 170f };
	[Property] public EnemyKindStats Shardguard { get; set; } = new() { Radius = 72f, Scrap = 8, SeekSpeed = 170f };
	[Property] public EnemyKindStats ShooterBody { get; set; } = new() { Radius = 64f, Scrap = 7 };
	[Property] public ShooterStats Shooter { get; set; } = new();
	[Property] public WaveLayout Wave { get; set; } = new();

	public EnemyKindStats Of( EnemyKind kind ) => kind switch
	{
		EnemyKind.Splinter => Splinter ??= new() { Radius = 60f, Scrap = 6, SeekSpeed = 185f, Lead = 160f, LeadPressure = 50f },
		EnemyKind.Glimmer => Glimmer ??= new() { Radius = 56f, Scrap = 7, SeekSpeed = 155f, Lead = 120f, LeadPressure = 40f },
		EnemyKind.Shield => Shield ??= new() { Radius = 72f, Scrap = 7, SeekSpeed = 170f },
		EnemyKind.Shardguard => Shardguard ??= new() { Radius = 72f, Scrap = 8, SeekSpeed = 170f },
		EnemyKind.Shooter => ShooterBody ??= new() { Radius = 64f, Scrap = 7 },
		_ => Chaser ??= new() { Radius = 64f, Scrap = 4, SeekSpeed = 185f, Lead = 160f, LeadPressure = 50f }
	};

	public float RadiusOf( EnemyKind kind ) => Of( kind ).Radius;

	public int ScrapOf( EnemyKind kind )
	{
		if ( kind is EnemyKind.Core or EnemyKind.Lens )
			return 0;

		return Of( kind ).Scrap;
	}
}

public class EnemyKindStats
{
	[Property] public float Radius { get; set; } = 64f;
	[Property] public int Scrap { get; set; } = 4;
	[Property] public float SeekSpeed { get; set; } = 185f;
	[Property] public float Lead { get; set; } = 0f;
	[Property] public float LeadPressure { get; set; } = 0f;
}

public class ShooterStats
{
	[Property] public float Speed { get; set; } = 175f;
	[Property] public float SpeedPressureFloor { get; set; } = 0.8f;
	[Property] public float BandInnerPad { get; set; } = 30f;
	[Property] public float BandOuterPad { get; set; } = 140f;
	[Property] public float BandMix { get; set; } = 0.42f;
	[Property] public float FlankInnerPad { get; set; } = 90f;
	[Property] public float FlankOuterPad { get; set; } = 30f;
	[Property] public float Orbit { get; set; } = 0.55f;
	[Property] public float OrbitOuterPad { get; set; } = 40f;
	[Property] public float FlankHold { get; set; } = 0.9f;
	[Property] public float Interval { get; set; } = 1.65f;
	[Property] public float IntervalFloor { get; set; } = 0.8f;
	[Property] public float Telegraph { get; set; } = 0.28f;
	[Property] public float TelegraphFloor { get; set; } = 0.14f;
	[Property] public float ShotSpeed { get; set; } = 560f;
	[Property] public float LeadMinDistance { get; set; } = 80f;
}

public class WaveLayout
{
	[Property] public float InnerPad { get; set; } = 190f;
	[Property] public float HuntOuterPad { get; set; } = 110f;
	[Property] public float HuntMix { get; set; } = 0.32f;
	[Property] public float MidOuterPad { get; set; } = 140f;
	[Property] public float MidMix { get; set; } = 0.45f;
	[Property] public float OuterPad { get; set; } = 90f;
	[Property] public float ExtraAngle { get; set; } = 0.85f;
	[Property] public float EdgeAngle { get; set; } = 0.2f;
}
