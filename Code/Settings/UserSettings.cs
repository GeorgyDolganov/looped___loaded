namespace LoopedLoaded;

public sealed class UserOptions
{
	public int Version { get; set; } = 2;
	public float Music { get; set; } = 1f;
	public float Sfx { get; set; } = 0.5f;
	public float Shake { get; set; } = 1f;
	public int Graphics { get; set; } = (int)GraphicsPreset.High;
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
	public static GraphicsPreset Graphics => (GraphicsPreset)Math.Clamp( Current.Graphics, 0, 2 );

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
		var existed = false;

		try
		{
			existed = FileSystem.Data.FileExists( File );
			if ( existed )
				data = FileSystem.Data.ReadJson<UserOptions>( File ) ?? new UserOptions();
		}
		catch
		{
			data = new UserOptions();
		}

		data.Music = Clamp( data.Music );
		data.Sfx = Clamp( data.Sfx );
		data.Shake = Clamp( data.Shake );
		if ( data.Version < 2 )
		{
			data.Graphics = (int)GraphicsPreset.High;
			data.Version = 2;
			if ( existed )
				Save();
		}

		data.Graphics = Math.Clamp( data.Graphics, 0, 2 );
		GraphicsProfile.Use( (GraphicsPreset)data.Graphics );
	}

	public static float Value( SettingRow row ) => row switch
	{
		SettingRow.Music => Current.Music,
		SettingRow.Sfx => Current.Sfx,
		SettingRow.Graphics => Current.Graphics,
		_ => Current.Shake
	};

	public static int Percent( SettingRow row ) => (int)MathF.Round( Value( row ) * 100f );

	public static int Steps( SettingRow row ) => (int)MathF.Round( Value( row ) * 10f );

	public static bool Nudge( SettingRow row, int delta )
	{
		if ( row == SettingRow.Graphics )
			return CycleGraphics( delta );

		return Set( row, Value( row ) + delta * Step );
	}

	public static bool CycleGraphics( int delta )
	{
		var count = 3;
		var next = (Current.Graphics + delta) % count;
		if ( next < 0 )
			next += count;

		if ( next == Current.Graphics )
			return false;

		Current.Graphics = next;
		GraphicsProfile.Use( (GraphicsPreset)next );
		Save();
		return true;
	}

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
