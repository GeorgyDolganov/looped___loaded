namespace LoopedLoaded;

public enum BuildingKind
{
	Infirmary,
	Anvil,
	Booster,
	Brake,
	Showcase
}

public static class Buildings
{
	public static readonly BuildingKind[] All =
	{
		BuildingKind.Infirmary,
		BuildingKind.Anvil,
		BuildingKind.Booster,
		BuildingKind.Brake,
		BuildingKind.Showcase
	};

	public static string Title( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => "INFIRMARY",
		BuildingKind.Anvil => "ANVIL",
		BuildingKind.Booster => "BOOSTER",
		BuildingKind.Brake => "BRAKE",
		_ => "SHOWCASE"
	};

	public static string Payoff( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => "+HP",
		BuildingKind.Anvil => "+DMG",
		BuildingKind.Booster => "DASH",
		BuildingKind.Brake => "SLOW",
		_ => "+CARD"
	};

	public static string Promise( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => $"+{Progression.RankValue( 1 )} HP AT RANK 1",
		BuildingKind.Anvil => $"+{Progression.RankValue( 1 )} DAMAGE AT RANK 1",
		BuildingKind.Booster => "SHORTER DASH COOLDOWN",
		BuildingKind.Brake => "UNLOCKS THE SLOW METER",
		_ => "+1 UPGRADE CARD AFTER EACH LAP"
	};

	public static string Blurb( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => "Raises max hearts. Same neighbor adds a rank.",
		BuildingKind.Anvil => "Rounds hit harder. Same neighbor adds a rank.",
		BuildingKind.Booster => "Dash returns faster each working rank.",
		BuildingKind.Brake => "Unlocks slow. Higher rank drains slower.",
		_ => "One extra upgrade card after every lap."
	};

	public static Color Color( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => new Color( 0.95f, 0.32f, 0.38f ),
		BuildingKind.Anvil => new Color( 0.85f, 0.7f, 0.35f ),
		BuildingKind.Booster => new Color( 1f, 0.55f, 0.2f ),
		BuildingKind.Brake => new Color( 0.55f, 0.45f, 1f ),
		_ => new Color( 0.35f, 0.85f, 0.95f )
	};

	public static int[] Costs( BuildingKind kind )
	{
		var max = MaxLevel( kind );
		var first = FirstCost( kind );
		var costs = new int[max];
		for ( var i = 0; i < max; i++ )
			costs[i] = Progression.Cost( first, i );
		return costs;
	}

	public static int FirstCost( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => 3,
		BuildingKind.Anvil => 4,
		BuildingKind.Booster => 3,
		BuildingKind.Brake => 5,
		_ => 6
	};

	public static int MaxLevel( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => 3,
		BuildingKind.Anvil => 3,
		_ => 2
	};

	public static Vector2 Rotate( Vector2 point, int facing )
	{
		return (facing & 3) switch
		{
			1 => new Vector2( -point.y, point.x ),
			2 => new Vector2( -point.x, -point.y ),
			3 => new Vector2( point.y, -point.x ),
			_ => point
		};
	}

	public static Vector2[] Shape( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => Plus,
		BuildingKind.Anvil => Trapezoid,
		BuildingKind.Booster => Chevron,
		BuildingKind.Brake => Channel,
		_ => Diamond
	};

	static readonly Vector2[] Plus =
	{
		new Vector2( -0.14f, -0.42f ), new Vector2( 0.14f, -0.42f ),
		new Vector2( 0.14f, -0.14f ), new Vector2( 0.42f, -0.14f ),
		new Vector2( 0.42f, 0.14f ), new Vector2( 0.14f, 0.14f ),
		new Vector2( 0.14f, 0.42f ), new Vector2( -0.14f, 0.42f ),
		new Vector2( -0.14f, 0.14f ), new Vector2( -0.42f, 0.14f ),
		new Vector2( -0.42f, -0.14f ), new Vector2( -0.14f, -0.14f )
	};

	static readonly Vector2[] Trapezoid =
	{
		new Vector2( -0.44f, -0.38f ), new Vector2( 0.44f, -0.38f ),
		new Vector2( 0.22f, 0.40f ), new Vector2( -0.22f, 0.40f )
	};

	static readonly Vector2[] Chevron =
	{
		new Vector2( -0.36f, -0.40f ), new Vector2( 0.36f, -0.40f ),
		new Vector2( 0.36f, 0.02f ), new Vector2( 0.00f, 0.44f ),
		new Vector2( -0.36f, 0.02f )
	};

	static readonly Vector2[] Channel =
	{
		new Vector2( -0.40f, -0.40f ), new Vector2( -0.40f, 0.40f ),
		new Vector2( -0.16f, 0.40f ), new Vector2( -0.16f, -0.16f ),
		new Vector2( 0.16f, -0.16f ), new Vector2( 0.16f, 0.40f ),
		new Vector2( 0.40f, 0.40f ), new Vector2( 0.40f, -0.40f )
	};

	static readonly Vector2[] Diamond =
	{
		new Vector2( 0.00f, 0.44f ), new Vector2( 0.40f, 0.00f ),
		new Vector2( 0.00f, -0.44f ), new Vector2( -0.40f, 0.00f )
	};
}

public sealed class CityPlot
{
	public int X;
	public int Y;
	public bool Occupied;
	public BuildingKind Kind;
	public int Level;
	public int Hits;
	public int Facing;
	public GameObject Root;
	public GameObject Body;

	public int NextCost
	{
		get
		{
			if ( !Occupied )
				return 0;

			var costs = Buildings.Costs( Kind );
			return Level >= costs.Length ? 0 : costs[Level];
		}
	}

	public bool Maxed => Occupied && Level >= Buildings.MaxLevel( Kind );
	public bool Working => Occupied && Level > 0;
}

public struct CityStats
{
	public int BonusHealth;
	public int BonusDamage;
	public float DashCooldownScale;
	public int DashCharges;
	public bool SlowUnlocked;
	public float SlowDrain;
	public int OfferCount;
}

public struct CityHit
{
	public float Distance;
	public Vector2 Position;
	public Vector2 Normal;
	public CityPlot Plot;
}
