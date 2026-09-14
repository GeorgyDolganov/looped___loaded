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
		RunLocation.Glass,
		RunLocation.Yard
	};

	public static RunLocation Start => Route[0];

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

	public static string Code( RunLocation location ) => GameSettings.Text.PlaceCode( location );
	public static string Title( RunLocation location ) => GameSettings.Text.PlaceTitle( location );
	public static string Rule( RunLocation location ) => GameSettings.Text.PlaceRule( location );
	public static string Boss( RunLocation location ) => GameSettings.Text.PlaceBoss( location );
	public static string FightCall( RunLocation location ) => GameSettings.Text.PlaceFightCall( location );
	public static string FightHint( RunLocation location ) => GameSettings.Text.PlaceFightHint( location );
	public static string BossBlurb( RunLocation location, bool last ) => GameSettings.Text.PlaceBossBlurb( location, last );

	public static string LineBlurb( int bestLine )
	{
		if ( bestLine < 0 )
			return "";

		var t = GameSettings.Text;
		var cleared = bestLine >= Route.Length ? Route[^1] : Route[bestLine];
		if ( IsLast( cleared ) )
			return t.F( t.Places.LineCleared, Code( cleared ) );

		return t.F( t.Places.LineOpen, Code( cleared ), Code( Next( cleared ) ) );
	}

	public static bool IsBoss( EnemyKind kind ) => kind is EnemyKind.Core or EnemyKind.Lens;
}
