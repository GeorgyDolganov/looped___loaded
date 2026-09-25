namespace LoopedLoaded;

public sealed class GameSave
{
	public int Version { get; set; } = 1;
	public long SavedAt { get; set; }
	public int Warehouse { get; set; }
	public int FedBiomass { get; set; }
	public int BestExtract { get; set; }
	public int BestLine { get; set; } = -1;
	public int Runs { get; set; }
	public int Ascend { get; set; }
	public List<string> Tasks { get; set; } = new();
	public List<PlotSave> Plots { get; set; } = new();

	public int BuildingCount
	{
		get
		{
			if ( Plots is null )
				return 0;

			var count = 0;
			foreach ( var plot in Plots )
			{
				if ( plot is not null && plot.Occupied )
					count++;
			}

			return count;
		}
	}

	public bool HasProgress => Warehouse > 0 || FedBiomass > 0 || BestExtract > 0 || BuildingCount > 0 || BestLine >= 0 || Runs > 0 || Ascend > 0 || (Tasks is not null && Tasks.Count > 0);
}

public sealed class PlotSave
{
	public int X { get; set; }
	public int Y { get; set; }
	public bool Occupied { get; set; }
	public int Kind { get; set; }
	public int Level { get; set; }
	public int Hits { get; set; }
	public int Facing { get; set; }
	public int Copy { get; set; }
}

public enum MenuPage
{
	Title,
	Saves,
	Settings
}

public enum SettingRow
{
	Graphics,
	Music,
	Sfx,
	Shake
}

public enum MenuChoice
{
	Continue,
	Upgrades,
	Saves,
	Settings,
	Quit
}
