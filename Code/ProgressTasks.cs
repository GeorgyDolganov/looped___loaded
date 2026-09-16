namespace LoopedLoaded;

public enum ProgressGoal
{
	FireShot,
	FinishLap,
	Extract,
	PlaceFrame,
	WorkOrgan,
	BuyTrait,
	BeatBoss,
	RideNextRing
}

public sealed class ProgressStep
{
	public string Id { get; init; }
	public ProgressGoal Goal { get; init; }
	public RunLocation Location { get; init; }

	public string Title => GameSettings.Text.ProgressTitle( Id );
	public string Blurb => GameSettings.Text.ProgressBlurb( Id );
}

public sealed class ProgressTrack
{
	static readonly ProgressStep[] Route =
	{
		new() { Id = "catch", Goal = ProgressGoal.FireShot },
		new() { Id = "lap", Goal = ProgressGoal.FinishLap },
		new() { Id = "extract", Goal = ProgressGoal.Extract },
		new() { Id = "grow", Goal = ProgressGoal.PlaceFrame },
		new() { Id = "inject", Goal = ProgressGoal.WorkOrgan },
		new() { Id = "chapel", Goal = ProgressGoal.BuyTrait },
		new() { Id = "lens", Goal = ProgressGoal.BeatBoss, Location = RunLocation.Glass },
		new() { Id = "ring", Goal = ProgressGoal.RideNextRing },
		new() { Id = "core", Goal = ProgressGoal.BeatBoss, Location = RunLocation.Yard }
	};

	readonly HashSet<string> done = new( StringComparer.OrdinalIgnoreCase );

	public static int Total => Route.Length;

	public ProgressTrack()
	{
		Refresh();
	}

	public ProgressStep Current { get; private set; }
	public IReadOnlyList<ProgressStep> CompletedSteps { get; private set; } = Array.Empty<ProgressStep>();
	public bool HasCurrent => Current is not null;
	public string Stamp => Current?.Id ?? "done";

	public List<string> Capture()
	{
		var list = new List<string>( done.Count );
		foreach ( var step in Route )
		{
			if ( done.Contains( step.Id ) )
				list.Add( step.Id );
		}

		return list;
	}

	public void Clear()
	{
		done.Clear();
		Refresh();
	}

	public void Apply( IReadOnlyList<string> saved, CityBoard city, int bestLine, int bestExtract )
	{
		done.Clear();
		if ( saved is not null )
		{
			foreach ( var id in saved )
			{
				if ( !string.IsNullOrWhiteSpace( id ) )
					done.Add( id );
			}
		}

		Backfill( city, bestLine, bestExtract );
		Refresh();
	}

	public bool Note( ProgressGoal goal, RunLocation location = default )
	{
		var changed = false;
		foreach ( var step in Route )
		{
			if ( done.Contains( step.Id ) || step.Goal != goal )
				continue;

			if ( goal == ProgressGoal.BeatBoss && step.Location != location )
				continue;

			done.Add( step.Id );
			changed = true;
		}

		if ( changed )
			Refresh();

		return changed;
	}

	void Backfill( CityBoard city, int bestLine, int bestExtract )
	{
		var warehouse = city.IsValid() ? city.Warehouse : 0;
		var occupied = city.IsValid() && city.HasOccupiedPlot();
		var working = city.IsValid() && city.HasWorkingPlot();
		var leftRing = bestExtract > 0 || bestLine >= 0 || warehouse > 0 || occupied;

		if ( leftRing )
		{
			done.Add( "catch" );
			done.Add( "lap" );
		}

		if ( warehouse > 0 || bestExtract > 0 )
			done.Add( "extract" );

		if ( occupied )
			done.Add( "grow" );

		if ( working )
			done.Add( "inject" );

		if ( bestLine >= Locations.Index( RunLocation.Glass ) )
			done.Add( "lens" );

		if ( bestLine >= Locations.Index( RunLocation.Yard ) )
		{
			done.Add( "ring" );
			done.Add( "core" );
		}
	}

	void Refresh()
	{
		Current = null;
		var completed = new List<ProgressStep>();
		foreach ( var step in Route )
		{
			if ( done.Contains( step.Id ) )
			{
				completed.Add( step );
				continue;
			}

			if ( Current is null )
				Current = step;
		}

		CompletedSteps = completed;
	}
}
