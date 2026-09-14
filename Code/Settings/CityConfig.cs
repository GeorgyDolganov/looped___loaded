namespace LoopedLoaded;

[AssetType( Name = "City Config", Extension = "omrcity", Category = "Looped Loaded" )]
public class CityConfig : GameResource
{
	[Property] public int MinOffers { get; set; } = 2;
	[Property] public int MaxOffers { get; set; } = 3;
	[Property] public BuildingStats Infirmary { get; set; } = new() { FirstCost = 3, MaxLevel = 3 };
	[Property] public BuildingStats Anvil { get; set; } = new() { FirstCost = 4, MaxLevel = 3 };
	[Property] public BuildingStats Booster { get; set; } = new() { FirstCost = 3, MaxLevel = 2 };
	[Property] public BuildingStats Brake { get; set; } = new() { FirstCost = 5, MaxLevel = 2 };
	[Property] public BuildingStats Showcase { get; set; } = new() { FirstCost = 6, MaxLevel = 2 };

	public BuildingStats Of( BuildingKind kind ) => kind switch
	{
		BuildingKind.Infirmary => Infirmary ??= new() { FirstCost = 3, MaxLevel = 3 },
		BuildingKind.Anvil => Anvil ??= new() { FirstCost = 4, MaxLevel = 3 },
		BuildingKind.Booster => Booster ??= new() { FirstCost = 3, MaxLevel = 2 },
		BuildingKind.Brake => Brake ??= new() { FirstCost = 5, MaxLevel = 2 },
		_ => Showcase ??= new() { FirstCost = 6, MaxLevel = 2 }
	};
}

public class BuildingStats
{
	[Property] public int FirstCost { get; set; } = 3;
	[Property] public int MaxLevel { get; set; } = 3;
}
