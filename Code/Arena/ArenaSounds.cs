namespace LoopedLoaded;

public static class ArenaSounds
{
	const int Studio = 44100;
	const int Rate = 22050;
	const int Bank = 5;

	static readonly Dictionary<string, SoundEvent> events = new();
	static int built;

	static readonly int[] AdpcmIndex =
	{
		-1, -1, -1, -1, 2, 4, 6, 8,
		-1, -1, -1, -1, 2, 4, 6, 8
	};

	static readonly int[] AdpcmStep =
	{
		7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
		19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
		50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
		130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
		337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
		876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
		2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
		5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
		15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
	};

	public static void Warm() => Ensure();

	public static void Fire( Vector3? at = null ) => Play( "fire", at, true );
	public static void Plasma( Vector3? at = null ) => Play( "plasma", at );
	public static void Ricochet( Vector3? at = null ) => Play( "ricochet", at );
	public static void Flesh( Vector3? at = null ) => Play( "flesh", at );
	public static void Metal( Vector3? at = null ) => Play( "metal", at );
	public static void Explode( Vector3? at = null ) => Play( "explode", at );
	public static void Pickup( Vector3? at = null ) => Play( "pickup", at );
	public static void Change() => Play( "change", null );
	public static void Deny() => Play( "deny", null );
	public static void Jump( Vector3? at = null ) => Play( "jump", at );
	public static void Pain( Vector3? at = null ) => Play( "pain", at );
	public static void Death( Vector3? at = null ) => Play( "death", at );
	public static void Armor( Vector3? at = null ) => Play( "armor", at );
	public static void MenuMove() => Play( "menu_move", null );
	public static void MenuOk() => Play( "menu_ok", null );
	public static void MenuBack() => Play( "menu_back", null );
	public static void MenuOpen() => Play( "menu_open", null );
	public static void Fight() => Play( "fight", null );
	public static void Tele() => Play( "tele", null );
	public static void Warn() => Play( "warn", null );
	public static void Hit( Vector3? at = null ) => Play( "hit", at );
	public static void Miss( Vector3? at = null ) => Play( "miss", at );
	public static void Lose() => Play( "lose", null );
	public static void Crack( Vector3? at = null ) => Play( "crack", at );

	static void Play( string key, Vector3? at, bool forceUi = false )
	{
		Ensure();
		if ( !events.TryGetValue( key, out var ev ) || ev is null )
			return;

		var ui = forceUi || ev.UI || !at.HasValue;
		var handle = Sound.Play( ev, at ?? Vector3.Zero, 0f );
		handle.OcclusionEnabled = false;
		handle.ReverbEnabled = false;
		handle.AirAbsorption = false;
		handle.DistanceAttenuation = false;
		handle.Distance = 20000f;
		handle.SpacialBlend = ui ? 0f : 0.2f;
		if ( key == "fire" )
			handle.Volume = 1.35f;
	}

	static void Ensure()
	{
		if ( built == Bank && events.Count > 0 )
			return;

		events.Clear();
		built = Bank;

		Put( "fire", true, 1f, 0.05f, 3, ClipFire );
		Put( "plasma", false, 0.74f, 0.08f, 2, ClipPlasma );
		Put( "ricochet", false, 0.82f, 0.1f, 3, ClipRicochet );
		Put( "flesh", false, 0.9f, 0.07f, 2, ClipFlesh );
		Put( "metal", false, 0.86f, 0.08f, 2, ClipMetal );
		Put( "explode", false, 1f, 0.05f, 3, ClipExplode );
		Put( "pickup", false, 0.82f, 0.04f, 2, ClipPickup );
		Put( "change", true, 0.7f, 0.03f, 1, _ => ClipChange() );
		Put( "deny", true, 0.7f, 0.03f, 1, _ => ClipDeny() );
		Put( "jump", false, 0.86f, 0.07f, 2, ClipJump );
		Put( "pain", false, 0.92f, 0.06f, 2, ClipPain );
		Put( "death", false, 1f, 0.04f, 1, _ => ClipDeath() );
		Put( "armor", false, 0.88f, 0.05f, 1, _ => ClipArmor() );
		Put( "menu_move", true, 0.52f, 0.03f, 1, _ => ClipMenuMove() );
		Put( "menu_ok", true, 0.62f, 0.02f, 1, _ => ClipMenuOk() );
		Put( "menu_back", true, 0.58f, 0.02f, 1, _ => ClipMenuBack() );
		Put( "menu_open", true, 0.7f, 0.02f, 1, _ => ClipMenuOpen() );
		Put( "fight", true, 1f, 0.02f, 1, _ => ClipFight() );
		Put( "tele", false, 0.88f, 0.03f, 1, _ => ClipTele() );
		Put( "warn", true, 0.86f, 0.03f, 1, _ => ClipWarn() );
		Put( "hit", true, 0.64f, 0.06f, 2, ClipHit );
		Put( "miss", false, 0.62f, 0.08f, 1, _ => ClipMiss() );
		Put( "lose", true, 0.78f, 0.03f, 1, _ => ClipLose() );
		Put( "crack", false, 0.78f, 0.1f, 2, ClipCrack );
	}

	static void Put( string key, bool ui, float volume, float jitter, int variants, Func<int, float[]> synth )
	{
		if ( TryLoadClips( key, variants, out var baked ) )
		{
			events[key] = MakeRaw( key, ui, volume, jitter, baked );
			return;
		}

		if ( ResourceLibrary.TryGet<SoundEvent>( $"sounds/arena/{key}.sound", out var packed ) && packed is not null )
		{
			events[key] = packed;
			return;
		}

		var clips = new float[variants][];
		for ( var i = 0; i < variants; i++ )
			clips[i] = synth( i );

		events[key] = Make( key, ui, volume, jitter, clips );
	}

	static SoundEvent Make( string name, bool ui, float volume, float jitter, params float[][] clips )
	{
		var files = new List<SoundFile>( clips.Length );
		for ( var i = 0; i < clips.Length; i++ )
			files.Add( ToFile( $"ll{Bank}_{name}_{i}", clips[i] ) );

		return BuildEvent( ui, volume, jitter, files );
	}

	static SoundEvent MakeRaw( string name, bool ui, float volume, float jitter, float[][] clips )
	{
		var files = new List<SoundFile>( clips.Length );
		for ( var i = 0; i < clips.Length; i++ )
			files.Add( ToFileRaw( $"ll{Bank}_{name}_{i}", clips[i] ) );

		return BuildEvent( ui, volume, jitter, files );
	}

	static SoundEvent BuildEvent( bool ui, float volume, float jitter, List<SoundFile> files )
	{
		return new SoundEvent
		{
			Sounds = files,
			UI = ui,
			Volume = new RangedFloat( volume ),
			Pitch = jitter > 0.001f ? new RangedFloat( 1f - jitter, 1f + jitter ) : new RangedFloat( 1f ),
			OcclusionEnabled = false,
			ReverbEnabled = false,
			AirAbsorption = false,
			DistanceAttenuation = false,
			Distance = 20000f
		};
	}

	static bool TryLoadClips( string key, int count, out float[][] clips )
	{
		clips = null;
		var fs = FileSystem.Mounted;
		if ( fs is null )
			return false;

		var loaded = new float[count][];
		for ( var i = 0; i < count; i++ )
		{
			var path = $"sounds/arena/{key}_{i}.wav";
			if ( !fs.FileExists( path ) )
				return false;

			var bytes = fs.ReadAllBytes( path );
			if ( bytes.Length < 44 || !TryDecodePcm16( bytes, out loaded[i], out var rate ) || rate != Rate )
				return false;
		}

		clips = loaded;
		return true;
	}

	static bool TryDecodePcm16( ReadOnlySpan<byte> file, out float[] samples, out int rate )
	{
		samples = null;
		rate = 0;
		if ( file.Length < 44 )
			return false;
		if ( file[0] != (byte)'R' || file[1] != (byte)'I' || file[2] != (byte)'F' || file[3] != (byte)'F' )
			return false;

		var pos = 12;
		short channels = 0;
		short bits = 0;
		byte[] data = null;
		while ( pos + 8 <= file.Length )
		{
			var size = BitConverter.ToInt32( file.Slice( pos + 4, 4 ) );
			if ( size < 0 )
				return false;

			var body = pos + 8;
			if ( body + size > file.Length )
				return false;

			if ( file[pos] == (byte)'f' && file[pos + 1] == (byte)'m' && file[pos + 2] == (byte)'t' )
			{
				if ( size < 16 )
					return false;
				if ( BitConverter.ToInt16( file.Slice( body, 2 ) ) != 1 )
					return false;
				channels = BitConverter.ToInt16( file.Slice( body + 2, 2 ) );
				rate = BitConverter.ToInt32( file.Slice( body + 4, 4 ) );
				bits = BitConverter.ToInt16( file.Slice( body + 14, 2 ) );
			}
			else if ( file[pos] == (byte)'d' && file[pos + 1] == (byte)'a' && file[pos + 2] == (byte)'t' && file[pos + 3] == (byte)'a' )
			{
				data = file.Slice( body, size ).ToArray();
			}

			pos = body + ((size + 1) & ~1);
		}

		if ( data is null || channels != 1 || bits != 16 || rate <= 0 )
			return false;

		var n = data.Length / 2;
		samples = new float[n];
		for ( var i = 0; i < n; i++ )
			samples[i] = BitConverter.ToInt16( data, i * 2 ) / 32768f;

		return true;
	}

	static SoundFile ToFileRaw( string name, float[] samples )
	{
		var bytes = new byte[samples.Length * 2];
		for ( var i = 0; i < samples.Length; i++ )
		{
			var s = (short)Math.Clamp( (int)(samples[i] * 32767f), -32768, 32767 );
			bytes[i * 2] = (byte)s;
			bytes[i * 2 + 1] = (byte)(s >> 8);
		}

		return SoundFile.FromPcm( name, bytes, new SoundFile.PcmOptions
		{
			Channels = 1,
			Rate = Rate,
			Bits = 16
		} );
	}

	static SoundFile ToFile( string name, float[] studio )
	{
		var samples = Pipe( studio );
		var bytes = new byte[samples.Length * 2];
		for ( var i = 0; i < samples.Length; i++ )
		{
			var s = (short)Math.Clamp( (int)(samples[i] * 32767f), -32768, 32767 );
			bytes[i * 2] = (byte)s;
			bytes[i * 2 + 1] = (byte)(s >> 8);
		}

		return SoundFile.FromPcm( name, bytes, new SoundFile.PcmOptions
		{
			Channels = 1,
			Rate = Rate,
			Bits = 16
		} );
	}

	static float[] Pipe( float[] studio )
	{
		HighPass( studio, Studio, 90f );
		Presence( studio, Studio );
		Squash( studio );

		var pcm44 = new short[studio.Length];
		for ( var i = 0; i < studio.Length; i++ )
			pcm44[i] = (short)Math.Clamp( (int)(studio[i] * 32767f), -32768, 32767 );

		var pcm22 = ResampleSfx( pcm44, Studio, Rate );
		AdpcmRoundtrip( pcm22 );
		PaintClip( pcm22 );

		var dest = new float[pcm22.Length];
		for ( var i = 0; i < pcm22.Length; i++ )
			dest[i] = pcm22[i] / 32768f;

		Fade( dest, Rate, 0.002f, 0.008f );
		return dest;
	}

	static void HighPass( float[] buf, int rate, float hz )
	{
		var dt = 1f / rate;
		var rc = 1f / (MathF.Tau * hz);
		var a = rc / (rc + dt);
		float prev = 0f;
		float hp = 0f;
		for ( var i = 0; i < buf.Length; i++ )
		{
			var x = buf[i];
			hp = a * (hp + x - prev);
			prev = x;
			buf[i] = hp;
		}
	}

	static void Presence( float[] buf, int rate )
	{
		var cLo = 1f - MathF.Exp( -MathF.Tau * 1600f / rate );
		var cHi = 1f - MathF.Exp( -MathF.Tau * 4200f / rate );
		float lpLo = 0f;
		float lpHi = 0f;
		for ( var i = 0; i < buf.Length; i++ )
		{
			var x = buf[i];
			lpLo += (x - lpLo) * cLo;
			lpHi += (x - lpHi) * cHi;
			buf[i] = x + (lpHi - lpLo) * 0.42f;
		}
	}

	static void Squash( float[] buf )
	{
		var env = 0f;
		for ( var i = 0; i < buf.Length; i++ )
		{
			var x = buf[i];
			var a = MathF.Abs( x );
			env = a > env ? a : env * 0.9991f;
			if ( env > 0.38f )
				x *= 0.38f / env;
			buf[i] = Math.Clamp( x * 1.14f, -1f, 1f );
		}
	}

	static short[] ResampleSfx( short[] data, int inrate, int outrate )
	{
		var stepscale = (float)inrate / outrate;
		var outcount = Math.Max( 8, (int)(data.Length / stepscale) );
		var fracstep = (int)(stepscale * 256f);
		var samplefrac = 0;
		var dest = new short[outcount];
		for ( var i = 0; i < outcount; i++ )
		{
			var src = samplefrac >> 8;
			samplefrac += fracstep;
			if ( src >= data.Length )
				src = data.Length - 1;
			dest[i] = data[src];
		}

		return dest;
	}

	static void AdpcmRoundtrip( short[] samples )
	{
		if ( samples.Length == 0 )
			return;

		var pred = (int)samples[0];
		var index = 0;
		for ( var i = 0; i < samples.Length; i++ )
		{
			var step = AdpcmStep[index];
			var diff = samples[i] - pred;
			var delta = 0;
			if ( diff < 0 )
			{
				delta = 8;
				diff = -diff;
			}

			var vpdiff = step >> 3;
			if ( diff >= step )
			{
				delta |= 4;
				diff -= step;
				vpdiff += step;
			}

			if ( diff >= (step >> 1) )
			{
				delta |= 2;
				diff -= step >> 1;
				vpdiff += step >> 1;
			}

			if ( diff >= (step >> 2) )
			{
				delta |= 1;
				vpdiff += step >> 2;
			}

			pred += (delta & 8) != 0 ? -vpdiff : vpdiff;
			if ( pred > 32767 )
				pred = 32767;
			else if ( pred < -32768 )
				pred = -32768;

			index += AdpcmIndex[delta];
			if ( index < 0 )
				index = 0;
			else if ( index > 88 )
				index = 88;

			samples[i] = (short)pred;
		}
	}

	static void PaintClip( short[] samples )
	{
		for ( var i = 0; i < samples.Length; i++ )
		{
			var v = (int)samples[i];
			if ( v > 32767 )
				v = 32767;
			else if ( v < -32768 )
				v = -32768;
			samples[i] = (short)v;
		}
	}

	static void Fade( float[] buf, int rate, float attack, float release )
	{
		var a = Math.Clamp( (int)(rate * attack), 1, buf.Length );
		var r = Math.Clamp( (int)(rate * release), 1, buf.Length );
		for ( var i = 0; i < a; i++ )
			buf[i] *= i / (float)a;
		for ( var i = 0; i < r; i++ )
			buf[buf.Length - 1 - i] *= i / (float)r;
	}

	static int Len( float seconds ) => Math.Max( 16, (int)(seconds * Studio) );
	static int Ms( float ms ) => Math.Max( 0, (int)(Studio * ms * 0.001f) );

	static void Stamp( float[] dest, int i, float v )
	{
		if ( (uint)i < (uint)dest.Length )
			dest[i] += v;
	}

	static float Env( float t, float attack, float decay )
	{
		if ( t <= 0f )
			return 0f;
		if ( t < attack )
			return t / MathF.Max( attack, 0.0001f );
		return MathF.Exp( -(t - attack) * decay );
	}

	static float Sine( float cycles ) => MathF.Sin( cycles * MathF.Tau );
	static float Square( float cycles ) => cycles - MathF.Floor( cycles ) < 0.5f ? 1f : -1f;
	static float Saw( float cycles )
	{
		var f = cycles - MathF.Floor( cycles );
		return f * 2f - 1f;
	}

	static float Noise( Random rng ) => (float)(rng.NextDouble() * 2.0 - 1.0);

	static float Fold( float x )
	{
		while ( x > 1f || x < -1f )
		{
			if ( x > 1f )
				x = 2f - x;
			if ( x < -1f )
				x = -2f - x;
		}

		return x;
	}

	static float[] ClipFire( int v )
	{
		var rng = new Random( 210 + v );
		var n = Len( 0.34f );
		var buf = new float[n];
		var thump = 118f + v * 16f;
		float lp = 0f;
		float mid = 0f;
		var dBody = Ms( 5f );
		var dScrape = Ms( 11f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.14f;
			mid += (noise - mid) * 0.38f;
			var hp = noise - mid;
			Stamp( buf, i, hp * Env( t, 0.001f, 52f ) * 1.55f );
			Stamp( buf, i + dBody, (mid - lp) * Env( t, 0.002f, 16f ) * 1.15f
				+ Square( thump * t ) * Env( t, 0.002f, 13f ) * 0.58f
				+ Sine( thump * 0.5f * t ) * Env( t, 0.003f, 11f ) * 0.72f );
			Stamp( buf, i + dScrape, Saw( (160f + v * 22f) * t ) * Env( t, 0.005f, 11f ) * 0.34f );
		}

		return buf;
	}

	static float[] ClipPlasma( int v )
	{
		var rng = new Random( 330 + v );
		var n = Len( 0.22f );
		var buf = new float[n];
		float lp = 0f;
		var hum = 92f + v * 14f;
		var dCoil = Ms( 4f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.28f;
			var arc = rng.NextDouble() < 0.22f * MathF.Exp( -t * 18f ) ? noise : noise * 0.12f;
			Stamp( buf, i, (arc - lp) * Env( t, 0.002f, 18f ) * 0.9f + lp * Env( t, 0.004f, 12f ) * 0.35f );
			Stamp( buf, i + dCoil, Square( hum * t ) * Env( t, 0.003f, 14f ) * 0.38f );
		}

		return buf;
	}

	static float[] ClipRicochet( int v )
	{
		var rng = new Random( 510 + v );
		var n = Len( 0.16f );
		var buf = new float[n];
		var f = 980f + v * 160f;
		var dSpark = Ms( 3f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var plate = Sine( f * t ) * Env( t, 0.001f, 36f )
				+ Sine( f * 1.63f * t ) * Env( t, 0.001f, 44f ) * 0.38f
				+ Sine( f * 2.47f * t ) * Env( t, 0.001f, 58f ) * 0.18f
				+ Saw( f * 0.37f * t ) * Env( t, 0.0015f, 28f ) * 0.22f;
			Stamp( buf, i, plate * 0.7f );
			if ( t < 0.012f )
				Stamp( buf, i + dSpark, Noise( rng ) * (1f - t / 0.012f) * 0.7f );
		}

		return buf;
	}

	static float[] ClipFlesh( int v )
	{
		var rng = new Random( 610 + v );
		var n = Len( 0.28f );
		var buf = new float[n];
		float lp = 0f;
		float mid = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.1f;
			mid += (noise - mid) * 0.3f;
			var wet = rng.NextDouble() < 0.04f * MathF.Exp( -t * 10f ) ? noise * 0.45f : 0f;
			buf[i] = lp * Env( t, 0.003f, 11f ) * 1.25f
				+ (mid - lp) * Env( t, 0.002f, 22f ) * 0.85f
				+ wet
				+ Sine( MathF.Max( 40f, 70f - t * 80f ) * t ) * Env( t, 0.004f, 16f ) * 0.4f;
		}

		return buf;
	}

	static float[] ClipMetal( int v )
	{
		var rng = new Random( 70 + v );
		var n = Len( 0.26f );
		var buf = new float[n];
		var f = 420f + v * 70f;
		var dPlate = Ms( 4f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			if ( t < 0.014f )
				Stamp( buf, i, Noise( rng ) * (1f - t / 0.014f) * 0.85f );
			Stamp( buf, i + dPlate, Sine( f * t ) * Env( t, 0.0015f, 16f )
				+ Sine( f * 1.37f * t ) * Env( t, 0.0015f, 21f ) * 0.55f
				+ Sine( f * 2.11f * t ) * Env( t, 0.0012f, 28f ) * 0.28f
				+ Square( f * 0.5f * t ) * Env( t, 0.002f, 18f ) * 0.18f );
		}

		return buf;
	}

	static float[] ClipExplode( int v )
	{
		var rng = new Random( 410 + v );
		var n = Len( 0.7f );
		var buf = new float[n];
		float lp = 0f;
		var dBoom = Ms( 6f );
		var dDebris = Ms( 18f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * (0.28f * MathF.Exp( -t * 2.6f ) + 0.035f);
			Stamp( buf, i, lp * Env( t, 0.005f, 4.2f ) * 1.2f );
			Stamp( buf, i + dBoom, Sine( (36f + v * 4f) * t ) * Env( t, 0.008f, 5.8f ) * 1.05f );
			if ( rng.NextDouble() < 0.045f * MathF.Exp( -t * 5.5f ) )
				Stamp( buf, i + dDebris, noise * 0.7f );
		}

		return buf;
	}

	static float[] ClipPickup( int v )
	{
		var rng = new Random( 22 + v );
		var n = Len( 0.24f );
		var buf = new float[n];
		float lp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.2f;
			Stamp( buf, i, (noise - lp) * Env( t, 0.01f, 14f ) * 0.35f );
			if ( t < 0.03f )
				Stamp( buf, i, noise * Env( t, 0.001f, 70f ) * 0.7f + Square( 86f * t ) * Env( t, 0.001f, 50f ) * 0.3f );
			var t2 = t - 0.05f;
			if ( t2 > 0f && t2 < 0.05f )
				Stamp( buf, i, Noise( rng ) * Env( t2, 0.001f, 55f ) * 0.55f + Sine( (140f + v * 18f) * t2 ) * Env( t2, 0.002f, 22f ) * 0.45f );
		}

		return buf;
	}

	static float[] ClipChange()
	{
		var rng = new Random( 91 );
		var n = Len( 0.18f );
		var buf = new float[n];
		var dLatch = Ms( 8f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			if ( t < 0.07f )
				Stamp( buf, i, noise * Env( t, 0.008f, 28f ) * 0.45f + Saw( 48f * t ) * Env( t, 0.006f, 22f ) * 0.25f );
			var t2 = t - 0.07f;
			if ( t2 > 0f )
				Stamp( buf, i + dLatch, noise * Env( t2, 0.001f, 60f ) * 0.7f + Square( 64f * t2 ) * Env( t2, 0.001f, 36f ) * 0.28f );
		}

		return buf;
	}

	static float[] ClipDeny()
	{
		var rng = new Random( 13 );
		var n = Len( 0.18f );
		var buf = new float[n];
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var click = t < 0.025f ? Noise( rng ) * Env( t, 0.001f, 80f ) * 0.65f : 0f;
			buf[i] = click + Square( 48f * t ) * Env( t, 0.006f, 10f ) * 0.42f + Saw( 36f * t ) * Env( t, 0.008f, 9f ) * 0.2f;
		}

		return buf;
	}

	static float[] ClipJump( int v )
	{
		var rng = new Random( 810 + v );
		var n = Len( 0.28f );
		var buf = new float[n];
		float lp = 0f;
		var dPlate = Ms( 8f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * (0.08f + 0.22f * MathF.Exp( -t * 8f ));
			Stamp( buf, i, (noise - lp) * Env( t, 0.006f, 13f ) * 0.8f
				+ Sine( MathF.Max( 34f, 78f - t * 160f ) * t ) * Env( t, 0.003f, 14f ) * 0.95f );
			if ( t < 0.04f )
				Stamp( buf, i + dPlate, lp * Env( t, 0.002f, 30f ) * 0.7f );
		}

		return buf;
	}

	static float[] ClipPain( int v )
	{
		var rng = new Random( 710 + v );
		var n = Len( 0.34f );
		var buf = new float[n];
		float lp = 0f;
		float bp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.14f;
			bp += (lp - bp) * 0.07f;
			var crush = 0.6f + 0.4f * Square( 22f * t );
			buf[i] = ((lp - bp) * crush * 1.5f + lp * 0.45f) * Env( t, 0.005f, 8f )
				+ Square( (90f + v * 10f) * (1f - t * 0.5f) * t ) * Env( t, 0.006f, 10f ) * 0.12f;
		}

		return buf;
	}

	static float[] ClipDeath()
	{
		var rng = new Random( 77 );
		var n = Len( 0.62f );
		var buf = new float[n];
		float lp = 0f;
		var dFall = Ms( 30f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.11f;
			var drop = MathF.Max( 28f, 96f - t * 140f );
			Stamp( buf, i, lp * Env( t, 0.012f, 4.6f ) * 1.1f
				+ Sine( drop * t ) * Env( t, 0.014f, 4.2f ) * 0.7f
				+ Saw( 40f * t ) * Env( t, 0.02f, 6f ) * 0.18f );
			if ( rng.NextDouble() < 0.03f * MathF.Exp( -t * 4f ) )
				Stamp( buf, i + dFall, noise * 0.5f );
		}

		return buf;
	}

	static float[] ClipArmor()
	{
		var rng = new Random( 5 );
		var n = Len( 0.28f );
		var buf = new float[n];
		var dRing = Ms( 5f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			if ( t < 0.016f )
				Stamp( buf, i, Noise( rng ) * (1f - t / 0.016f) * 0.8f );
			Stamp( buf, i + dRing, Sine( 310f * t ) * Env( t, 0.002f, 14f )
				+ Sine( 470f * t ) * Env( t, 0.002f, 18f ) * 0.55f
				+ Sine( 730f * t ) * Env( t, 0.0015f, 24f ) * 0.28f
				+ Square( 55f * t ) * Env( t, 0.003f, 16f ) * 0.2f );
		}

		return buf;
	}

	static float[] ClipMenuMove()
	{
		var rng = new Random( 3 );
		var n = Len( 0.06f );
		var buf = new float[n];
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			buf[i] = Noise( rng ) * Env( t, 0.001f, 70f ) * 0.55f
				+ Square( 72f * t ) * Env( t, 0.001f, 80f ) * 0.18f;
		}

		return buf;
	}

	static float[] ClipMenuOk()
	{
		var rng = new Random( 4 );
		var n = Len( 0.12f );
		var buf = new float[n];
		var dB = Ms( 6f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			if ( t < 0.04f )
				Stamp( buf, i, Noise( rng ) * Env( t, 0.001f, 55f ) * 0.55f + Square( 58f * t ) * Env( t, 0.002f, 40f ) * 0.22f );
			var t2 = t - 0.04f;
			if ( t2 > 0f )
				Stamp( buf, i + dB, Noise( rng ) * Env( t2, 0.001f, 48f ) * 0.45f + Sine( 120f * t2 ) * Env( t2, 0.002f, 22f ) * 0.28f );
		}

		return buf;
	}

	static float[] ClipMenuBack()
	{
		var rng = new Random( 6 );
		var n = Len( 0.14f );
		var buf = new float[n];
		float lp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.18f;
			buf[i] = Noise( rng ) * Env( t, 0.002f, 40f ) * 0.4f
				+ (noise - lp) * Env( t, 0.01f, 12f ) * 0.28f
				+ Square( MathF.Max( 32f, 70f - t * 220f ) * t ) * Env( t, 0.004f, 14f ) * 0.22f;
		}

		return buf;
	}

	static float[] ClipMenuOpen()
	{
		var rng = new Random( 7 );
		var n = Len( 0.26f );
		var buf = new float[n];
		float lp = 0f;
		var dHiss = Ms( 12f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.12f;
			if ( t < 0.06f )
				Stamp( buf, i, noise * Env( t, 0.002f, 28f ) * 0.7f + Square( 42f * t ) * Env( t, 0.003f, 18f ) * 0.3f );
			Stamp( buf, i + dHiss, (noise - lp) * Env( t, 0.02f, 9f ) * 0.4f );
		}

		return buf;
	}

	static float[] ClipFight()
	{
		var rng = new Random( 8 );
		var n = Len( 0.58f );
		var buf = new float[n];
		float lp = 0f;
		var dSlam = Ms( 8f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.14f;
			Stamp( buf, i, Fold( Square( 55f * t ) * 1.4f + Square( 82f * t ) * 1.1f ) * Env( t, 0.016f, 5.2f ) * 0.72f
				+ (noise - lp) * Env( t, 0.02f, 7f ) * 0.28f );
			if ( t < 0.08f )
				Stamp( buf, i + dSlam, lp * Env( t, 0.004f, 16f ) * 1.1f + Sine( 38f * t ) * Env( t, 0.006f, 12f ) );
		}

		return buf;
	}

	static float[] ClipTele()
	{
		var rng = new Random( 12 );
		var n = Len( 0.42f );
		var buf = new float[n];
		float lp = 0f;
		var dSlam = Ms( 8f );
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			var swell = t <= 0.2f ? MathF.Pow( t / 0.2f, 1.4f ) : Env( t - 0.2f, 0.004f, 11f );
			lp += (noise - lp) * (0.06f + swell * 0.2f);
			Stamp( buf, i, (noise - lp) * swell * 0.7f + lp * swell * 0.35f );
			if ( t > 0.18f && t < 0.28f )
				Stamp( buf, i + dSlam, Noise( rng ) * Env( t - 0.18f, 0.002f, 28f ) * 0.85f + Sine( 48f * (t - 0.18f) ) * Env( t - 0.18f, 0.003f, 16f ) * 0.5f );
		}

		return buf;
	}

	static float[] ClipWarn()
	{
		var rng = new Random( 15 );
		var n = Len( 0.5f );
		var buf = new float[n];
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var slot = t % 0.16f;
			var f = slot < 0.08f ? 210f : 145f;
			var local = slot < 0.08f ? slot : slot - 0.08f;
			buf[i] = (Fold( Square( f * t ) * 1.6f ) * 0.55f + Square( f * 0.5f * t ) * 0.2f + Noise( rng ) * 0.12f)
				* Env( local, 0.005f, 16f );
		}

		return buf;
	}

	static float[] ClipHit( int v )
	{
		var rng = new Random( 40 + v );
		var n = Len( 0.07f );
		var buf = new float[n];
		var f = 620f + v * 80f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			buf[i] = Noise( rng ) * Env( t, 0.001f, 70f ) * 0.55f
				+ Sine( f * t ) * Env( t, 0.001f, 48f ) * 0.4f
				+ Sine( f * 1.67f * t ) * Env( t, 0.001f, 60f ) * 0.18f;
		}

		return buf;
	}

	static float[] ClipMiss()
	{
		var rng = new Random( 44 );
		var n = Len( 0.14f );
		var buf = new float[n];
		float lp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.22f;
			buf[i] = lp * Env( t, 0.003f, 16f ) * 0.85f + (noise - lp) * Env( t, 0.002f, 28f ) * 0.25f;
		}

		return buf;
	}

	static float[] ClipLose()
	{
		var rng = new Random( 18 );
		var n = Len( 0.28f );
		var buf = new float[n];
		float lp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.15f;
			var f = MathF.Max( 28f, 90f - t * 220f );
			buf[i] = Saw( f * t ) * Env( t, 0.008f, 8f ) * 0.4f
				+ Square( f * 0.5f * t ) * Env( t, 0.01f, 7f ) * 0.22f
				+ (noise - lp) * Env( t, 0.012f, 9f ) * 0.28f;
		}

		return buf;
	}

	static float[] ClipCrack( int v )
	{
		var rng = new Random( 930 + v );
		var n = Len( 0.16f );
		var buf = new float[n];
		float lp = 0f;
		for ( var i = 0; i < n; i++ )
		{
			var t = i / (float)Studio;
			var noise = Noise( rng );
			lp += (noise - lp) * 0.35f;
			var snap = rng.NextDouble() < 0.28f * MathF.Exp( -t * 14f ) ? noise : noise * 0.08f;
			buf[i] = (snap - lp * 0.3f) * Env( t, 0.001f, 20f )
				+ Square( (110f + v * 16f) * t ) * Env( t, 0.002f, 22f ) * 0.22f;
		}

		return buf;
	}
}
