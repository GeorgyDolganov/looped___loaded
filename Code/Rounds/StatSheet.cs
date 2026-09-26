namespace LoopedLoaded;

public readonly struct StatLine
{
	public string Text { get; }
	public int Sign { get; }
	public bool Mixed { get; }

	public StatLine( string text, int sign, bool mixed = false )
	{
		Text = text;
		Sign = sign;
		Mixed = mixed;
	}

	public string Class => Mixed ? "mixed" : Sign < 0 ? "down" : Sign > 0 ? "up" : "note";
}

public readonly struct SheetRow
{
	public string Label { get; }
	public string Now { get; }
	public string Next { get; }
	public int Sign { get; }
	public bool Mixed { get; }

	public SheetRow( string label, string now, string next = "", int sign = 0, bool mixed = false )
	{
		Label = label;
		Now = now;
		Next = next;
		Sign = sign;
		Mixed = mixed;
	}

	public string NextClass => Mixed ? "mixed" : Sign < 0 ? "down" : "up";
}

public static class StatSheet
{
	public static List<StatLine> Effects( TrinketDef card, int rank )
	{
		var lines = new List<StatLine>();
		if ( card is null )
			return lines;

		rank = Math.Max( 1, rank );
		if ( card.Mods is not null )
		{
			foreach ( var mod in card.Mods )
			{
				if ( mod is null || mod.Hidden )
					continue;

				if ( Describe( mod, rank ) is { } line )
					lines.Add( line );
			}
		}

		if ( card.Notes is not null )
		{
			foreach ( var note in card.Notes )
			{
				if ( note is null || string.IsNullOrWhiteSpace( note.Text ) )
					continue;

				if ( note.When == NoteWhen.First && rank > 1 )
					continue;

				if ( note.When == NoteWhen.Later && rank <= 1 )
					continue;

				lines.Add( new StatLine( note.Text, note.Sign, note.Mixed ) );
			}
		}

		if ( rank <= 1 && card.Flags is not null )
		{
			foreach ( var flag in card.Flags )
			{
				var text = FlagText( flag );
				if ( text is null )
					continue;

				lines.Add( new StatLine( text, FlagSign( flag ) ) );
			}
		}

		if ( rank <= 1 )
		{
			var names = new List<string>();
			foreach ( var ex in Trinkets.ExcludesOf( card ) )
				names.Add( ex.Id );

			if ( names.Count > 0 )
				lines.Add( new StatLine( "Locks out " + string.Join( ", ", names ), -1 ) );
		}

		return lines;
	}

	static StatLine? Describe( TrinketMod mod, int rank )
	{
		var meta = MetaOf( mod.Stat );
		if ( mod.ByHook && mod.Op == ModOp.Set )
		{
			var amount = mod.Amount( rank, 0 );
			if ( rank <= 1 )
			{
				var number = NearlyInt( amount ) ? ((int)MathF.Round( amount )).ToString() : Fmt( amount );
				var label = meta.Plural is not null && MathF.Abs( amount - 1f ) > 0.001f ? meta.Plural : meta.Label;
				var text = string.IsNullOrEmpty( meta.Suffix ) ? $"{number} {label}" : $"{meta.Label} {number}{meta.Suffix}";
				return new StatLine( text, 1 );
			}

			return DescribeDelta( mod.Stat, meta, amount - mod.Amount( rank - 1, 0 ) );
		}

		if ( mod.Growth == ModGrowth.PerRemoved )
		{
			if ( rank > 1 )
				return null;

			return new StatLine( $"+{Fmt( mod.Value )} {meta.Label} per removed projectile", 1 );
		}

		if ( mod.Op == ModOp.Mul )
			return DescribeMul( mod, meta, rank );

		if ( mod.Op == ModOp.Max )
		{
			if ( rank > 1 )
				return null;

			return new StatLine( $"{meta.Label} min {Fmt( mod.Amount( 1, 0 ) )}{meta.Suffix}", 0, true );
		}

		if ( mod.Op == ModOp.Min )
		{
			if ( rank > 1 )
				return null;

			return new StatLine( $"{meta.Label} max {Fmt( mod.Amount( 1, 0 ) )}{meta.Suffix}", 0, true );
		}

		if ( mod.Op == ModOp.Set && mod.Growth == ModGrowth.Flat )
			return DescribeSet( mod, meta, rank );

		var now = mod.Amount( rank, 0 );
		var prev = rank <= 1 ? 0f : mod.Amount( rank - 1, 0 );
		return DescribeDelta( mod.Stat, meta, now - prev );
	}

	static StatLine? DescribeMul( TrinketMod mod, StatMeta meta, int rank )
	{
		var now = mod.Amount( rank, 0 );
		var prev = rank <= 1 ? 1f : mod.Amount( rank - 1, 0 );
		if ( mod.Points )
		{
			var cut = prev - now;
			if ( MathF.Abs( cut ) < 0.001f )
				return null;

			var helpful = meta.HigherIsGood == false;
			var sign = (cut > 0) == helpful ? 1 : -1;
			if ( meta.Mixed || meta.HigherIsGood is null )
				sign = 0;

			var lead = cut > 0 ? "-" : "+";
			return new StatLine( $"{lead}{PctPoints( MathF.Abs( cut ) )} {meta.Label}", sign, meta.Mixed );
		}

		var factor = now / MathF.Max( 0.01f, prev );
		if ( MathF.Abs( factor - 1f ) < 0.001f )
			return null;

		var better = meta.HigherIsGood == false ? factor < 1f : factor > 1f;
		var mulSign = meta.HigherIsGood is null || meta.Mixed ? 0 : better ? 1 : -1;
		return new StatLine( $"{PctDelta( factor )} {meta.Label}", mulSign, meta.Mixed || meta.HigherIsGood is null );
	}

	static StatLine? DescribeSet( TrinketMod mod, StatMeta meta, int rank )
	{
		if ( rank > 1 )
			return null;

		var amount = mod.Amount( 1, 0 );
		var delta = amount - BaseOf( mod.Stat );
		if ( MathF.Abs( delta ) < 0.001f )
			return null;

		var sign = 0;
		var mixed = meta.Mixed || meta.HigherIsGood is null;
		if ( !mixed && meta.HigherIsGood is bool good )
			sign = (delta > 0) == good ? 1 : -1;

		var number = NearlyInt( amount ) ? ((int)MathF.Round( amount )).ToString() : Fmt( amount );
		var text = string.IsNullOrEmpty( meta.Suffix ) ? $"{meta.Label} {number}" : $"{meta.Label} {number}{meta.Suffix}";
		return new StatLine( text, sign, mixed );
	}

	static StatLine? DescribeDelta( GunStat stat, StatMeta meta, float delta )
	{
		if ( MathF.Abs( delta ) < 0.001f )
			return null;

		if ( stat == GunStat.RangeCut )
			return delta > 0
				? new StatLine( $"-{PctPoints( delta )} Range", -1 )
				: new StatLine( $"+{PctPoints( -delta )} Range", 1 );

		if ( stat == GunStat.Dodge )
			return new StatLine( $"+{PctPoints( delta )} Dodge", delta >= 0 ? 1 : -1 );

		var abs = MathF.Abs( delta );
		var shown = NearlyInt( abs ) ? ((int)MathF.Round( abs )).ToString() : Fmt( abs );
		var label = meta.Plural is not null && MathF.Abs( abs - 1f ) > 0.001f ? meta.Plural : meta.Label;
		var lead = delta > 0 ? "+" : "-";
		var text = string.IsNullOrEmpty( meta.Suffix ) ? $"{lead}{shown} {label}" : $"{lead}{shown}{meta.Suffix} {label}";
		var mixed = meta.Mixed || meta.HigherIsGood is null;
		var sign = 0;
		if ( !mixed && meta.HigherIsGood is bool good )
			sign = (delta > 0) == good ? 1 : -1;

		return new StatLine( text, sign, mixed );
	}

	readonly record struct StatMeta( string Label, string Plural, string Suffix, bool? HigherIsGood, bool Mixed );

	static StatMeta MetaOf( GunStat stat ) => stat switch
	{
		GunStat.Count => new( "Projectile", "Projectiles", "", true, false ),
		GunStat.Cone => new( "Spread", null, "°", null, true ),
		GunStat.Damage => new( "Damage", null, "", true, false ),
		GunStat.Pierce => new( "Pierce", null, "", true, false ),
		GunStat.Bounces => new( "Bounce", "Bounces", "", true, false ),
		GunStat.Reload => new( "Reload", null, "s", false, false ),
		GunStat.Speed => new( "Projectile Speed", null, "", true, false ),
		GunStat.Radius => new( "Body radius", null, "", true, false ),
		GunStat.Splash => new( "Splash Radius", null, "", true, false ),
		GunStat.SplashDamage => new( "Splash Damage", null, "", true, false ),
		GunStat.RangeCut => new( "Range", null, "", false, false ),
		GunStat.RangePad => new( "Range", null, "", true, false ),
		GunStat.MeatRange => new( "Damage within", null, "", true, false ),
		GunStat.MeatBonus => new( "Damage", null, "", true, false ),
		GunStat.KickForce => new( "Knockback", null, "", true, false ),
		GunStat.KickRange => new( "Knockback range", null, "", true, false ),
		GunStat.StunTime => new( "Stun", null, "s", true, false ),
		GunStat.StunRange => new( "Stun range", null, "", true, false ),
		GunStat.Cycle => new( "Cycle", null, "s", false, false ),
		GunStat.Burst => new( "Burst", null, "", true, false ),
		GunStat.WalkStep => new( "Spread per later volley", null, "°", null, true ),
		GunStat.BeamHit => new( "Bolt Damage", null, "", true, false ),
		GunStat.BeamTick => new( "Bolt Tick", null, "s", false, false ),
		GunStat.BeamTicks => new( "Tick", "Ticks", "", true, false ),
		GunStat.BeamWidth => new( "Beam Width", null, "", true, false ),
		GunStat.BeamArc => new( "Jump", null, "", true, false ),
		GunStat.BeamKiln => new( "Latch tick", null, "", false, false ),
		GunStat.StickTime => new( "Stick", null, "s", true, false ),
		GunStat.Dodge => new( "Dodge", null, "", true, false ),
		GunStat.Gap => new( "Gap", null, "s", null, false ),
		GunStat.SpinSpeed => new( "Spin", null, "", true, false ),
		_ => new( stat.ToString(), null, "", null, true )
	};

	static float BaseOf( GunStat stat )
	{
		var traits = GameSettings.Traits;
		return stat switch
		{
			GunStat.Reload => traits.ReloadBase,
			GunStat.Bounces => traits.MaxBouncesBase,
			GunStat.Speed => 1f,
			GunStat.BeamTick => 1f,
			GunStat.BeamKiln => 1f,
			GunStat.BeamWidth => traits.LashWidth,
			GunStat.Radius => traits.ProjectileRadius,
			GunStat.Cycle => traits.DrumCycle,
			GunStat.Burst => 1f,
			GunStat.Count => 1f,
			GunStat.Damage => traits.BaseDamage,
			_ => 0f
		};
	}

	static string FlagText( GunFlag flag ) => flag switch
	{
		GunFlag.Beam => "Hold to fire lightning",
		GunFlag.Auto => "Hold to fire a burst",
		GunFlag.DoublePump => "Two volleys per mag",
		GunFlag.PointAim => "Aim at a point",
		GunFlag.IgnoreArmor => "Ignores armor",
		GunFlag.RampPierce => "+1 damage per body already pierced",
		GunFlag.Sight => "Later volleys use half spread",
		GunFlag.Bite => "+1 damage on a body this burst already hit",
		GunFlag.CommitBurst => "Burst finishes on release",
		GunFlag.PerPelletSplash => "Splash per pellet",
		GunFlag.FriendlySplash => "You take splash damage",
		GunFlag.NoFriendlySplash => "Friendly splash off",
		GunFlag.Nail => "Nails that stick",
		GunFlag.BeamSear => "+1 damage while the beam stays",
		GunFlag.BeamFork => "Side bolts hit",
		GunFlag.BeamShunt => "Ignores shields",
		GunFlag.BeamLinger => "Remaining ticks finish",
		_ => null
	};

	static int FlagSign( GunFlag flag ) => flag switch
	{
		GunFlag.FriendlySplash => -1,
		_ => 1
	};

	static bool NearlyInt( float value ) => MathF.Abs( value - MathF.Round( value ) ) < 0.001f;

	public static List<SheetRow> Weapon( GameLoop loop, TrinketDef hover )
	{
		if ( loop is null || !loop.Inventory.IsValid() )
			return [];

		var loadout = loop.Inventory.Loadout;
		var now = loadout.Recipe();
		var next = hover is not null ? loadout.Peek( hover ) : now;
		ShotRange.Apply( ref now, loop );
		ShotRange.Apply( ref next, loop );
		var preview = hover is not null;
		var rows = new List<SheetRow>();

		AddInt( rows, "Damage", now.Damage, next.Damage, preview, true, true );
		AddInt( rows, "Projectiles", now.Count, next.Count, preview, true, true );
		AddFloat( rows, "Spread", now.Cone, next.Cone, preview, null, "°" );
		AddInt( rows, "Pierce", now.Pierce, next.Pierce, preview, true, false );
		if ( now.IgnoreArmor || next.IgnoreArmor )
			AddText( rows, "Armor", now.IgnoreArmor ? "Ignored" : "Holds", next.IgnoreArmor ? "Ignored" : "Holds", preview );
		if ( now.RampPierce || next.RampPierce )
			AddText( rows, "Line", now.RampPierce ? "+1" : "Flat", next.RampPierce ? "+1" : "Flat", preview );
		AddInt( rows, "Bounces", now.Bounces, next.Bounces, preview, true, false );
		AddFloat( rows, "Reload", now.Reload, next.Reload, preview, false, "s", true );
		AddPct( rows, "Proj. Speed", now.SpeedScale, next.SpeedScale, preview, true );
		AddRange( rows, now.Falloff, next.Falloff, preview );
		AddFloat( rows, "Damage within", now.MeatRange, next.MeatRange, preview, true );
		AddInt( rows, "Meat Damage", now.MeatBonus, next.MeatBonus, preview, true, false );
		AddFloat( rows, "Splash", now.Splash, next.Splash, preview, true );
		AddInt( rows, "Splash Dmg", now.SplashDamage, next.SplashDamage, preview, true, false );
		if ( now.PerPelletSplash || next.PerPelletSplash )
			AddText( rows, "Per Pellet", Flag( now.PerPelletSplash ), Flag( next.PerPelletSplash ), preview );
		if ( now.PointAim || next.PointAim )
			AddText( rows, "Aim", now.PointAim ? "Point" : "Direction", next.PointAim ? "Point" : "Direction", preview );
		if ( now.Auto || next.Auto || now.Burst > 1 || next.Burst > 1 )
		{
			AddInt( rows, "Burst", now.Burst, next.Burst, preview, true, true );
			AddFloat( rows, "Cycle", now.Cycle, next.Cycle, preview, false, "s", true );
		}
		if ( now.WalkStep > 0f || next.WalkStep > 0f )
			AddFloat( rows, "Walk", now.WalkStep, next.WalkStep, preview, null, "°" );
		if ( now.Sight || next.Sight )
			AddText( rows, "Sight", now.Sight ? "Half" : "Full", next.Sight ? "Half" : "Full", preview, true );
		if ( now.Bite || next.Bite )
			AddText( rows, "Bite", now.Bite ? "+1" : "Flat", next.Bite ? "+1" : "Flat", preview );
		if ( now.CommitBurst || next.CommitBurst )
			AddText( rows, "Queue", now.CommitBurst ? "Finish" : "Hold", next.CommitBurst ? "Finish" : "Hold", preview );
		if ( now.Beam || next.Beam )
		{
			AddInt( rows, "Bolt Dmg", now.BeamHit, next.BeamHit, preview, true, true );
			AddInt( rows, "Ticks", now.BeamTicks, next.BeamTicks, preview, true, true );
			AddFloat( rows, "Bolt Tick", now.BeamTick, next.BeamTick, preview, false, "s", true );
		}
		AddFloat( rows, "Knockback", now.KickForce, next.KickForce, preview, true );
		AddFloat( rows, "Stun", now.StunTime, next.StunTime, preview, true, "s" );
		AddText( rows, "Friendly Splash", Flag( now.FriendlySplash ), Flag( next.FriendlySplash ), preview );

		return rows;
	}

	public static List<SheetRow> Hero( GameLoop loop )
	{
		if ( loop is null )
			return [];

		var rows = new List<SheetRow>();
		var runner = loop.Runner;
		var loadout = loop.Inventory.IsValid() ? loop.Inventory.Loadout : null;
		rows.Add( new SheetRow( "HP", $"{loop.Health} / {loop.HeartMax}" ) );
		rows.Add( new SheetRow( "Speed", runner.IsValid() ? Fmt( runner.Speed ) : "-" ) );
		rows.Add( new SheetRow( "Dash CD", runner.IsValid() ? $"{Fmt( runner.DashCooldown )}s" : "-" ) );
		rows.Add( new SheetRow( "Dash Dist", runner.IsValid() ? Fmt( runner.DashDistance ) : "-" ) );
		rows.Add( new SheetRow( "Slow", runner.IsValid() && runner.SlowUnlocked ? "Yes" : "No" ) );
		rows.Add( new SheetRow( "Dodge", Pct( loop.DodgeChance ) ) );
		rows.Add( new SheetRow( "Bonus Dmg", (loadout?.BonusDamage ?? 0).ToString() ) );
		return rows;
	}

	static void AddInt( List<SheetRow> rows, string label, int now, int next, bool preview, bool higherIsGood, bool always )
	{
		if ( !always && now == 0 && next == 0 )
			return;

		rows.Add( Row( label, now.ToString(), next.ToString(), now, next, preview, higherIsGood ) );
	}

	static void AddFloat( List<SheetRow> rows, string label, float now, float next, bool preview, bool? higherIsGood, string suffix = "", bool always = false )
	{
		if ( !always && now <= 0.001f && next <= 0.001f )
			return;

		rows.Add( Row( label, Fmt( now ) + suffix, Fmt( next ) + suffix, now, next, preview, higherIsGood ) );
	}

	static void AddRange( List<SheetRow> rows, float now, float next, bool preview )
	{
		if ( now <= 0.001f && next <= 0.001f )
			return;

		var nowText = now <= 0.001f ? "Full" : Fmt( now );
		var nextText = next <= 0.001f ? "Full" : Fmt( next );
		if ( !preview || Almost( now, next ) )
		{
			rows.Add( new SheetRow( "Range", nowText ) );
			return;
		}

		var nowOpen = now <= 0.001f;
		var nextOpen = next <= 0.001f;
		var better = nextOpen || (!nowOpen && next > now);
		rows.Add( new SheetRow( "Range", nowText, nextText, better ? 1 : -1 ) );
	}

	static void AddPct( List<SheetRow> rows, string label, float now, float next, bool preview, bool higherIsGood )
	{
		rows.Add( Row( label, Pct( now ), Pct( next ), now, next, preview, higherIsGood ) );
	}

	static void AddText( List<SheetRow> rows, string label, string now, string next, bool preview, bool mixed = false )
	{
		if ( now == "No" && next == "No" )
			return;

		if ( !preview || now == next )
		{
			rows.Add( new SheetRow( label, now ) );
			return;
		}

		rows.Add( new SheetRow( label, now, next, next == "Yes" ? 1 : 0, mixed ) );
	}

	static SheetRow Row( string label, string nowText, string nextText, float now, float next, bool preview, bool? higherIsGood )
	{
		if ( !preview || Almost( now, next ) )
			return new SheetRow( label, nowText );

		if ( higherIsGood is not { } good )
			return new SheetRow( label, nowText, nextText, 0, true );

		var better = next > now;
		var sign = better == good ? 1 : -1;
		return new SheetRow( label, nowText, nextText, sign );
	}

	static string Flag( bool on ) => on ? "Yes" : "No";


	static string PctDelta( float scale )
	{
		var pct = (int)MathF.Round( (scale - 1f) * 100f );
		return pct >= 0 ? $"+{pct}%" : $"{pct}%";
	}

	static string Pct( float scale ) => $"{(int)MathF.Round( scale * 100f )}%";

	static string PctPoints( float fraction ) => $"{(int)MathF.Round( fraction * 100f )}%";

	static string Fmt( float value )
	{
		if ( MathF.Abs( value - MathF.Round( value ) ) < 0.05f )
			return ((int)MathF.Round( value )).ToString();

		return value.ToString( "0.##" );
	}

	static bool Almost( float a, float b ) => MathF.Abs( a - b ) < 0.01f;
}
