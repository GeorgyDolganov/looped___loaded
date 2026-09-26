namespace LoopedLoaded;

public static class TrinketHooks
{
	public static void PinEarly( BuildState state, TrinketDef card, int level )
	{
		if ( state.Count > 1 )
			return;

		state.Full = Math.Max( 1, (int)Read( card, "nails", level ) );
	}

	public static void PinLate( BuildState state, TrinketDef card, int level )
	{
		if ( state.Collapsed || state.Count > 1 )
			return;

		state.Count = Math.Max( 1, state.Full );
		state.Cone = Read( card, "cone", level );
	}

	public static void Slug( BuildState state )
	{
		state.Count = 1;
		state.Cone = 0f;
		state.Collapsed = true;
	}

	static float Read( TrinketDef card, string role, int level )
	{
		if ( card?.Mods is null )
			return 0f;

		foreach ( var mod in card.Mods )
		{
			if ( mod is not null && mod.Role == role )
				return mod.Amount( level, 0 );
		}

		return 0f;
	}
}
