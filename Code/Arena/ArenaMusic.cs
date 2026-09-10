namespace LoopedLoaded;

public static class ArenaMusic
{
	const string CombatPath = "sounds/music/combat.ogg";
	const string MenuPath = "sounds/music/menu.ogg";

	static MusicPlayer player;
	static string current;

	public static void Tick( GameLoop loop )
	{
		if ( loop is null || !loop.IsValid() )
			return;

		var key = Wanted( loop );
		var path = key == "combat" ? CombatPath : MenuPath;
		if ( !FileExists( path ) )
			path = FileExists( CombatPath ) ? CombatPath : MenuPath;
		if ( !FileExists( path ) )
			return;

		if ( current != path )
			Start( path );

		if ( player is null )
			return;

		player.Paused = loop.Paused;
		player.Repeat = true;
		player.ListenLocal = true;
		player.Volume = VolumeFor( loop, key );
	}

	public static void Stop()
	{
		if ( player is null )
			return;

		player.Stop();
		player.Dispose();
		player = null;
		current = null;
	}

	static void Start( string path )
	{
		Stop();
		player = MusicPlayer.Play( FileSystem.Mounted, path );
		if ( player is null )
			return;

		player.Repeat = true;
		player.ListenLocal = true;
		current = path;
	}

	static string Wanted( GameLoop loop )
	{
		if ( loop.Phase == RunPhase.Playing )
			return "combat";

		return "menu";
	}

	static float VolumeFor( GameLoop loop, string key )
	{
		if ( loop.Paused )
			return 0.12f;
		if ( key == "combat" )
			return loop.InBossFight ? 0.48f : 0.38f;
		if ( loop.Phase == RunPhase.City )
			return 0.28f;
		return 0.32f;
	}

	static bool FileExists( string path )
	{
		var fs = FileSystem.Mounted;
		return fs is not null && fs.FileExists( path );
	}
}
