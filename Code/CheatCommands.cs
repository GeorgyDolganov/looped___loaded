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
}
