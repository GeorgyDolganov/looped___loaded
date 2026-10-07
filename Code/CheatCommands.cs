namespace LoopedLoaded;

public static class CheatCommands
{
	[ConCmd( "dodge_always" )]
	public static void DodgeAlways()
	{
		GameLoop.CheatDodge = !GameLoop.CheatDodge;
		Log.Info( $"dodge_always {(GameLoop.CheatDodge ? "on" : "off")}" );
	}

	[ConCmd( "wave" )]
	public static void Wave( int lap = 20 )
	{
		var loop = Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( !loop.IsValid() )
		{
			Log.Warning( "wave needs the arena scene running" );
			return;
		}

		loop.TestWave( lap );
		Log.Info( $"wave {loop.Lap}" );
	}

	[ConCmd( "biomass_give" )]
	public static void BiomassGive( int amount = 10 )
	{
		var loop = Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( !loop.IsValid() || !loop.City.IsValid() )
		{
			Log.Warning( "biomass_give needs the arena scene with an altar" );
			return;
		}

		if ( amount <= 0 )
		{
			Log.Warning( "biomass_give needs a positive amount" );
			return;
		}

		loop.City.Deposit( amount );
		loop.Autosave();
		Log.Info( $"biomass_give {amount}  warehouse {loop.City.Warehouse}" );
	}

	[ConCmd( "sacrifice_reset" )]
	public static void SacrificeReset()
	{
		var loop = Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( !loop.IsValid() || !loop.City.IsValid() )
		{
			Log.Warning( "sacrifice_reset needs the arena scene with an altar" );
			return;
		}

		loop.ForgetSacrificeStart();
		loop.Autosave();
		Log.Info( "sacrifice_reset" );
	}
}
