namespace LoopedLoaded;

public sealed class EkkeTalk
{
	public static readonly string[] All =
	{
		"intro", "bones", "skip", "lap", "boss", "chapel",
		"lens", "core", "ring", "yard", "dead", "altar",
		"frame", "organ", "won", "dash"
	};

	readonly HashSet<string> seen = new( StringComparer.OrdinalIgnoreCase );
	readonly Queue<string[]> queue = new();

	string[] lines = Array.Empty<string>();
	int index;
	int shown;
	int glyphs;
	float revealed;
	float hold;
	float mouth;
	float gate;
	bool open;

	public bool Active => open;
	public float Mouth => mouth;
	public int Cursor => index * 10000 + shown;
	public bool LineDone => open && index < lines.Length && shown >= lines[index].Length;

	public string Shown
	{
		get
		{
			if ( !open || index < 0 || index >= lines.Length )
				return "";

			var text = lines[index];
			var count = Math.Clamp( shown, 0, text.Length );
			return text[..count];
		}
	}

	public bool Remember( string id )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return false;

		return seen.Add( id );
	}

	public bool Once( string id, IReadOnlyList<string> source, bool force = false )
	{
		if ( string.IsNullOrWhiteSpace( id ) || source is null )
			return false;

		var packed = new List<string>();
		foreach ( var line in source )
		{
			if ( !string.IsNullOrWhiteSpace( line ) )
				packed.Add( line );
		}

		if ( packed.Count == 0 )
			return false;

		var fresh = seen.Add( id );
		if ( !fresh && !force )
			return false;

		var starting = !open;
		queue.Enqueue( packed.ToArray() );
		if ( !starting )
			return true;

		OpenNext();
		gate = RealTime.Now + 0.16f;
		return true;
	}

	public void Tick( float dt )
	{
		dt = MathF.Min( MathF.Max( dt, 0f ), 0.1f );
		mouth = MathF.Max( 0f, mouth - dt * 9f );
		if ( !open || index < 0 || index >= lines.Length )
			return;

		if ( hold > 0f )
		{
			hold -= dt;
			return;
		}

		var text = lines[index];
		if ( shown >= text.Length )
			return;

		var rate = MathF.Max( 1f, GameSettings.Run.TalkCharsPerSecond );
		revealed += dt * rate;

		while ( shown < text.Length && shown + 1 <= revealed )
		{
			var ch = text[shown];
			shown++;
			if ( char.IsLetterOrDigit( ch ) )
			{
				glyphs++;
				if ( (glyphs & 1) == 0 )
				{
					ArenaSounds.Talk();
					mouth = 1f;
					EkkeLook.Pulse();
				}
			}

			if ( ch is '.' or ',' or '!' or '?' )
			{
				revealed = shown;
				hold = MathF.Max( 0f, GameSettings.Run.TalkStopPause );
				return;
			}
		}
	}

	public bool Advance()
	{
		if ( !open || RealTime.Now < gate )
			return false;

		gate = RealTime.Now + 0.12f;

		if ( index < lines.Length && shown < lines[index].Length )
		{
			shown = lines[index].Length;
			revealed = shown;
			hold = 0f;
			return false;
		}

		index++;
		if ( index < lines.Length )
		{
			BeginLine();
			return false;
		}

		open = false;
		return !OpenNext();
	}

	public bool Skip()
	{
		if ( !open && queue.Count == 0 )
			return false;

		Dismiss();
		return true;
	}

	public List<string> Capture()
	{
		var list = new List<string>();
		foreach ( var id in All )
		{
			if ( seen.Contains( id ) )
				list.Add( id );
		}

		return list;
	}

	public void Apply( IReadOnlyList<string> saved )
	{
		seen.Clear();
		Dismiss();
		if ( saved is null )
			return;

		foreach ( var id in saved )
		{
			if ( !string.IsNullOrWhiteSpace( id ) )
				seen.Add( id );
		}
	}

	public void ClearSeen()
	{
		seen.Clear();
	}

	public void Clear()
	{
		seen.Clear();
		Dismiss();
	}

	public void SeedAll()
	{
		Dismiss();
		foreach ( var id in All )
			seen.Add( id );
	}

	bool OpenNext()
	{
		if ( queue.Count == 0 )
		{
			open = false;
			return false;
		}

		lines = queue.Dequeue();
		index = 0;
		open = true;
		BeginLine();
		return true;
	}

	void BeginLine()
	{
		shown = 0;
		revealed = 0f;
		hold = 0f;
		glyphs = 0;
	}

	void Dismiss()
	{
		queue.Clear();
		lines = Array.Empty<string>();
		index = 0;
		open = false;
		shown = 0;
		revealed = 0f;
		hold = 0f;
		glyphs = 0;
		mouth = 0f;
	}
}

public static class EkkeCommands
{
	[ConCmd( "ekke_reset" )]
	public static void Reset()
	{
		var loop = FindLoop();
		if ( !loop.IsValid() )
		{
			Log.Warning( "ekke_reset needs a running game" );
			return;
		}

		loop.ResetTalk();
	}

	[ConCmd( "ekke_say" )]
	public static void Say( string id )
	{
		var loop = FindLoop();
		if ( !loop.IsValid() )
		{
			Log.Warning( "ekke_say needs a running game" );
			return;
		}

		loop.PreviewTalk( id );
	}

	static GameLoop FindLoop() => Game.ActiveScene?.GetAllComponents<GameLoop>().FirstOrDefault();
}
