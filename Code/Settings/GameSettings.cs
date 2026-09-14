namespace LoopedLoaded;

public static class GameSettings
{
	public static ProgressionConfig Progression => Load( ref progression, "settings/progression.omrprog" );
	public static RunConfig Run => Load( ref run, "settings/run.omrrun" );
	public static EnemyConfig Enemies => Load( ref enemies, "settings/enemy.omrenemy" );
	public static TraitConfig Traits => Load( ref traits, "settings/traits.omrtrait" );
	public static CityConfig City => Load( ref city, "settings/city.omrcity" );
	public static BossConfig Boss => Load( ref boss, "settings/boss.omrboss" );
	public static TextConfig Text
	{
		get
		{
			var loaded = Load( ref text, "settings/text.omrtext" );
			loaded.Ensure();
			return loaded;
		}
	}

	static ProgressionConfig progression;
	static RunConfig run;
	static EnemyConfig enemies;
	static TraitConfig traits;
	static CityConfig city;
	static BossConfig boss;
	static TextConfig text;

	static T Load<T>( ref T fallback, string path ) where T : GameResource, new()
	{
		if ( ResourceLibrary.TryGet<T>( path, out var resource ) && resource is not null )
			return resource;

		return fallback ??= new T();
	}
}
