namespace LoopedLoaded;

public sealed class UserOptions
{
	public int Version { get; set; } = 1;
	public float Music { get; set; } = 1f;
	public float Sfx { get; set; } = 0.5f;
	public float Shake { get; set; } = 1f;
}

public static class UserSettings
{
	const string File = "options.json";
	const float Step = 0.1f;

	static UserOptions data = new();
	static bool loaded;

	public static float Music => Current.Music;
	public static float Sfx => Current.Sfx;
	public static float Shake => Current.Shake;

	static UserOptions Current
	{
		get
		{
			Load();
			return data;
		}
	}

	public static void Load()
	{
		if ( loaded )
			return;

		loaded = true;

		try
		{
			if ( FileSystem.Data.FileExists( File ) )
				data = FileSystem.Data.ReadJson<UserOptions>( File ) ?? new UserOptions();
		}
		catch
		{
			data = new UserOptions();
		}

		data.Music = Clamp( data.Music );
		data.Sfx = Clamp( data.Sfx );
		data.Shake = Clamp( data.Shake );
	}

	public static float Value( SettingRow row ) => row switch
	{
		SettingRow.Music => Current.Music,
		SettingRow.Sfx => Current.Sfx,
		_ => Current.Shake
	};

	public static int Percent( SettingRow row ) => (int)MathF.Round( Value( row ) * 100f );

	public static int Steps( SettingRow row ) => (int)MathF.Round( Value( row ) * 10f );

	public static bool Nudge( SettingRow row, int delta ) => Set( row, Value( row ) + delta * Step );

	public static bool Set( SettingRow row, float value )
	{
		var before = Value( row );
		var after = Clamp( value );
		if ( MathF.Abs( after - before ) < 0.001f )
			return false;

		switch ( row )
		{
			case SettingRow.Music:
				Current.Music = after;
				break;
			case SettingRow.Sfx:
				Current.Sfx = after;
				break;
			default:
				Current.Shake = after;
				break;
		}

		Save();
		return true;
	}

	public static void Save()
	{
		try
		{
			FileSystem.Data.WriteJson( File, Current );
		}
		catch
		{
		}
	}

	static float Clamp( float value ) => MathF.Round( Math.Clamp( value, 0f, 1f ) * 10f ) / 10f;
}
