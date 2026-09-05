namespace LoopedLoaded;

public sealed class GameSave
{
	public int Version { get; set; } = 1;
	public long SavedAt { get; set; }
	public int Warehouse { get; set; }
	public int BestExtract { get; set; }
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

	public bool HasProgress => Warehouse > 0 || BestExtract > 0 || BuildingCount > 0;
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
}

public enum MenuPage
{
	Title,
	Saves
}
