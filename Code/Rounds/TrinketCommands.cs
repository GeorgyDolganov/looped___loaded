namespace LoopedLoaded;

public static class TrinketCommands
{
	[ConCmd( "trinket_list" )]
	public static void List()
	{
		Trinkets.Refresh();
		var loop = FindLoop();
		var loadout = loop.IsValid() && loop.Inventory.IsValid() ? loop.Inventory.Loadout : null;
		foreach ( var card in Trinkets.Every )
		{
			var level = loadout?.TraitLevel( card ) ?? 0;
			var pool = card.InPool ? "" : " (not in pool)";
			Log.Info( $"{card.Id,-8} {level}/{card.Cap}  {card.Pack}{pool}" );
		}
	}

	[ConCmd( "trinket_give" )]
	public static void Give( string id, int levels = 1 )
	{
		var loadout = ActiveLoadout();
		var card = Resolve( id );
		if ( loadout is null || card is null )
			return;

		if ( !loadout.Has( card ) && Trinkets.Blocked( card, loadout ) )
			Log.Warning( $"trinket_give {card.Id} is normally blocked by the current loadout" );

		for ( var i = 0; i < Math.Max( 1, levels ); i++ )
			loadout.Install( card );

		Log.Info( $"trinket_give {card.Id} {loadout.TraitLevel( card )}/{card.Cap}" );
	}

	[ConCmd( "trinket_take" )]
	public static void Take( string id, int levels = 1 )
	{
		var loadout = ActiveLoadout();
		var card = Resolve( id );
		if ( loadout is null || card is null )
			return;

		for ( var i = 0; i < Math.Max( 1, levels ); i++ )
			loadout.Remove( card );

		Log.Info( $"trinket_take {card.Id} {loadout.TraitLevel( card )}/{card.Cap}" );
	}

	[ConCmd( "trinket_clear" )]
	public static void Clear()
	{
		var loadout = ActiveLoadout();
		if ( loadout is null )
			return;

		var bonus = loadout.BonusDamage;
		loadout.Clear();
		loadout.BonusDamage = bonus;
		Log.Info( "trinket_clear done" );
	}

	static GameLoop FindLoop() => Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();

	static RunLoadout ActiveLoadout()
	{
		var loop = FindLoop();
		if ( !loop.IsValid() || !loop.Inventory.IsValid() )
		{
			Log.Warning( "trinket commands need a GameLoop with an inventory" );
			return null;
		}

		if ( loop.InMenu || loop.InCity )
		{
			Log.Warning( "trinket commands only work during a run, the loadout resets when a run starts" );
			return null;
		}

		return loop.Inventory.Loadout;
	}

	static TrinketDef Resolve( string id )
	{
		Trinkets.Refresh();
		var key = id?.Trim() ?? "";
		var card = Trinkets.Find( key.ToUpperInvariant() )
			?? Trinkets.Every.FirstOrDefault( item => string.Equals( item.ShownTitle, key, StringComparison.OrdinalIgnoreCase ) );
		if ( card is null )
			Log.Warning( $"unknown trinket '{id}', use trinket_list to see ids" );
		return card;
	}
}
