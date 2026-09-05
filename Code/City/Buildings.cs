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

	public static string Blurb( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => "+1 max HP per level.",
		BuildingKind.Anvil => "+1 round damage per level.",
		BuildingKind.Booster => "Shorter dash cooldown.",
		BuildingKind.Brake => "Unlocks slow. Level 2 lasts longer.",
		_ => "+1 upgrade card in the run."
	};

	public static Color Color( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => new Color( 0.95f, 0.32f, 0.38f ),
		BuildingKind.Anvil => new Color( 0.85f, 0.7f, 0.35f ),
		BuildingKind.Booster => new Color( 1f, 0.55f, 0.2f ),
		BuildingKind.Brake => new Color( 0.55f, 0.45f, 1f ),
		_ => new Color( 0.35f, 0.85f, 0.95f )
	};

	public static int[] Costs( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => new[] { 3, 6, 10 },
		BuildingKind.Anvil => new[] { 4, 8, 12 },
		BuildingKind.Booster => new[] { 3, 7 },
		BuildingKind.Brake => new[] { 5, 9 },
		_ => new[] { 6, 12 }
	};

	public static int MaxLevel( BuildingKind kind ) => Costs( kind ).Length;
}

public sealed class CityPlot
{
	public int X;
	public int Y;
	public bool Occupied;
	public BuildingKind Kind;
	public int Level;
	public int Hits;
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
