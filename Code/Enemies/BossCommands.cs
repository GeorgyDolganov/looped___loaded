namespace LoopedLoaded;

public static class BossCommands
{
	[ConCmd( "boss" )]
	public static void Boss()
	{
		var loop = Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();
		if ( !loop.IsValid() )
		{
			Log.Warning( "boss needs the arena scene running" );
			return;
		}

		loop.TestBoss();
		Log.Info( "boss: lens" );
	}
}
