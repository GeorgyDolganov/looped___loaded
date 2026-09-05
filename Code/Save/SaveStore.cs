namespace LoopedLoaded;

public static class SaveStore
{
	public const int Slots = 3;

	const string Folder = "saves";
	const string LastFile = "saves/last.txt";

	public static int LastSlot()
	{
		EnsureFolder();
		if ( !FileSystem.Data.FileExists( LastFile ) )
			return 0;

		return int.TryParse( FileSystem.Data.ReadAllText( LastFile ), out var slot )
			? Math.Clamp( slot, 0, Slots - 1 )
			: 0;
	}

	public static void SetLastSlot( int slot )
	{
		EnsureFolder();
		FileSystem.Data.WriteAllText( LastFile, Math.Clamp( slot, 0, Slots - 1 ).ToString() );
	}

	public static bool Exists( int slot ) => FileSystem.Data.FileExists( FilePath( slot ) );

	public static GameSave Read( int slot )
	{
		if ( !Exists( slot ) )
			return null;

		try
		{
			return FileSystem.Data.ReadJson<GameSave>( FilePath( slot ) );
		}
		catch
		{
			return null;
		}
	}

	public static bool Write( int slot, GameSave save )
	{
		if ( save is null )
			return false;

		try
		{
			EnsureFolder();
			save.Version = 1;
			save.SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			FileSystem.Data.WriteJson( FilePath( slot ), save );
			SetLastSlot( slot );
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool Delete( int slot )
	{
		try
		{
			var path = FilePath( slot );
			if ( FileSystem.Data.FileExists( path ) )
				FileSystem.Data.DeleteFile( path );

			return !FileSystem.Data.FileExists( path );
		}
		catch
		{
			return false;
		}
	}

	static string FilePath( int slot ) => $"{Folder}/slot{Math.Clamp( slot, 0, Slots - 1 ) + 1}.json";

	static void EnsureFolder()
	{
		if ( !FileSystem.Data.DirectoryExists( Folder ) )
			FileSystem.Data.CreateDirectory( Folder );
	}
}
