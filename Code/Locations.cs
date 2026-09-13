namespace LoopedLoaded;

public enum RunLocation
{
	Yard,
	Glass
}

public static class Locations
{
	public static readonly RunLocation[] Route =
	{
		RunLocation.Yard,
		RunLocation.Glass
	};

	public static int Index( RunLocation location )
	{
		for ( var i = 0; i < Route.Length; i++ )
		{
			if ( Route[i] == location )
				return i;
		}

		return 0;
	}

	public static bool IsLast( RunLocation location ) => Index( location ) >= Route.Length - 1;

	public static RunLocation Next( RunLocation location )
	{
		var index = Index( location ) + 1;
		return index >= Route.Length ? location : Route[index];
	}

	public static string Code( RunLocation location ) => location switch
	{
		RunLocation.Glass => "GLASS",
		_ => "YARD"
	};

	public static string Title( RunLocation location ) => location switch
	{
		RunLocation.Glass => "Glassworks",
		_ => "The Yard"
	};

	public static string Rule( RunLocation location ) => location switch
	{
		RunLocation.Glass => "PANELS SHATTER",
		_ => "PANELS KICK"
	};

	public static string Boss( RunLocation location ) => location switch
	{
		RunLocation.Glass => "LENS",
		_ => "CORE"
	};

	public static string FightCall( RunLocation location ) => location switch
	{
		RunLocation.Glass => "LENS FIGHT",
		_ => "CORE FIGHT"
	};

	public static string FightHint( RunLocation location ) => location switch
	{
		RunLocation.Glass => "THE LENS  ·  BREAK GLASS THEN HIT THE SIDE",
		_ => "THE CORE  ·  RICOCHET TO BREAK IT"
	};

	public static string BossBlurb( RunLocation location, bool last )
	{
		if ( location == RunLocation.Glass )
			return last
				? "Shatter the glass, then hit the side. Win: stash ×2, then city."
				: "Shatter the glass, then hit the side. Keep the stash.";

		return last
			? "Ricochet the nucleus. Win: stash ×2, then city."
			: "Ricochet the nucleus. Keep the stash. Extract or ride the next ring.";
	}

	public static string LineBlurb( int bestLine )
	{
		if ( bestLine < 0 )
			return "";

		var cleared = bestLine >= Route.Length ? Route[^1] : Route[bestLine];
		if ( IsLast( cleared ) )
			return $"{Code( cleared )} CLEARED";

		return $"{Code( cleared )} CLEARED  ·  {Code( Next( cleared ) )} OPEN";
	}

	public static bool IsBoss( EnemyKind kind ) => kind is EnemyKind.Core or EnemyKind.Lens;
}
