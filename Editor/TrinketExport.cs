namespace LoopedLoaded;

public static class TrinketExport
{
	[Menu( "Editor", "Looped Loaded/Export TRINKETS.md" )]
	public static void Export()
	{
		Trinkets.Refresh();
		var path = System.IO.Path.Combine( Project.Current.RootDirectory.FullName, "TRINKETS.md" );
		System.IO.File.WriteAllText( path, Trinkets.Markdown() );
		Log.Info( $"Wrote {path}" );
	}
}
