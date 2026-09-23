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

	public static string Title( BuildingKind kind ) => GameSettings.Text.BuildingTitle( kind );
	public static string Payoff( BuildingKind kind ) => GameSettings.Text.BuildingPayoff( kind );
	public static string Promise( BuildingKind kind ) => GameSettings.Text.BuildingPromise( kind );
	public static string Blurb( BuildingKind kind ) => GameSettings.Text.BuildingBlurb( kind );

	public static Color Color( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => new Color( 0.95f, 0.32f, 0.38f ),
		BuildingKind.Anvil => new Color( 0.85f, 0.7f, 0.35f ),
		BuildingKind.Booster => new Color( 1f, 0.55f, 0.2f ),
		BuildingKind.Brake => new Color( 0.55f, 0.45f, 1f ),
		_ => new Color( 0.35f, 0.85f, 0.95f )
	};

	public static int[] Costs( BuildingKind kind, int copy = 0 )
	{
		var max = MaxLevel( kind );
		var first = FirstCost( kind );
		var costs = new int[max];
		for ( var i = 0; i < max; i++ )
			costs[i] = Progression.Cost( first, i );
		if ( costs.Length > 0 )
			costs[0] += Math.Max( 0, copy );
		return costs;
	}

	public static int FrameCost( BuildingKind kind, int copy )
	{
		var costs = Costs( kind, copy );
		return costs.Length == 0 ? 0 : costs[0];
	}

	public static int FirstCost( BuildingKind kind ) => GameSettings.City.Of( kind ).FirstCost;

	public static int MaxLevel( BuildingKind kind ) => GameSettings.City.Of( kind ).MaxLevel;

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
	public int Copy;
	public GameObject Root;
	public GameObject Body;

	public int NextCost
	{
		get
		{
			if ( !Occupied )
				return 0;

			var costs = Buildings.Costs( Kind, Copy );
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

public struct CityWall
{
	public Vector2 Center;
	public Vector2 AxisX;
	public Vector2 AxisY;
	public float Hx;
	public float Hy;
}
