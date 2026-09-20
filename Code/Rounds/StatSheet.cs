namespace LoopedLoaded;

public readonly struct StatLine
{
	public string Text { get; }
	public int Sign { get; }

	public StatLine( string text, int sign )
	{
		Text = text;
		Sign = sign;
	}

	public string Class => Sign < 0 ? "down" : Sign > 0 ? "up" : "note";
}

public readonly struct SheetRow
{
	public string Label { get; }
	public string Now { get; }
	public string Next { get; }
	public int Sign { get; }

	public SheetRow( string label, string now, string next = "", int sign = 0 )
	{
		Label = label;
		Now = now;
		Next = next;
		Sign = sign;
	}

	public string NextClass => Sign < 0 ? "down" : "up";
}

public static class StatSheet
{
	public static List<StatLine> Effects( RoundTrait trait, int rank )
	{
		var t = GameSettings.Traits;
		var lines = new List<StatLine>();
		rank = Math.Max( 1, rank );

		void Up( string text ) => lines.Add( new StatLine( text, 1 ) );
		void Down( string text ) => lines.Add( new StatLine( text, -1 ) );
		void Note( string text ) => lines.Add( new StatLine( text, 0 ) );

		switch ( trait )
		{
			case RoundTrait.Buck:
			{
				var pellets = rank <= 1
					? Math.Max( 0, (int)t.BuckPellets.At( 1 ) - 1 )
					: (int)Gain( t.BuckPellets, rank );
				var cone = Gain( t.BuckCone, rank );
				if ( pellets > 0 )
					Up( pellets == 1 ? "+1 Projectile" : $"+{pellets} Projectiles" );
				if ( cone > 0.001f )
					Down( $"+{Fmt( cone )}° Spread" );
				var cut = t.BuckFalloff.At( rank );
				var prev = rank <= 1 ? 0f : t.BuckFalloff.At( rank - 1 );
				if ( rank <= 1 )
					Down( $"0 Damage after {Fmt( cut )}" );
				else if ( cut < prev - 0.001f )
					Down( $"{Fmt( cut - prev )} Falloff" );
				else if ( cut > prev + 0.001f )
					Up( $"+{Fmt( cut - prev )} Falloff" );
				break;
			}
			case RoundTrait.Split:
				Up( $"+{t.SplitPellets} Projectile" );
				break;
			case RoundTrait.Fan:
				Down( $"+{Fmt( t.FanCone )}° Spread" );
				break;
			case RoundTrait.Pump:
				Up( $"+{t.PumpPellets} Projectile" );
				Down( $"+{Fmt( t.PumpReload )}s Reload" );
				break;
			case RoundTrait.Load:
				Up( $"+{t.LoadPellets} Projectiles" );
				Down( $"+{Fmt( t.LoadReload )}s Reload" );
				break;
			case RoundTrait.Choke:
				Up( $"-{Fmt( t.ChokeCone )}° Spread" );
				Note( $"Spread min {Fmt( t.ChokeFloor )}°" );
				break;
			case RoundTrait.Meat:
				Up( $"+{t.MeatBonus} Damage" );
				Note( $"Range < {Fmt( t.MeatRange )}" );
				Down( $"0 Damage after {Fmt( t.MeatFalloff )}" );
				break;
			case RoundTrait.Rico:
				Up( $"+{t.RicoBounces} Bounce" );
				break;
			case RoundTrait.Gape:
				Down( $"+{Fmt( t.GapeCone )}° Spread" );
				break;
			case RoundTrait.Double:
				Note( "Two volleys per mag" );
				Note( $"Gap {Fmt( t.DoubleGap )}s" );
				Down( $"+{Fmt( t.DoubleReload )}s Reload" );
				break;
			case RoundTrait.Kick:
				Up( $"+{Fmt( t.KickForce )} Knockback" );
				Note( $"Range {Fmt( t.KickRange )}" );
				break;
			case RoundTrait.Stun:
				Up( $"+{Fmt( t.StunTime )}s Stun" );
				Note( $"Range {Fmt( t.StunRange )}" );
				break;
			case RoundTrait.Heap:
				Up( $"+{t.HeapPellets} Projectiles" );
				Down( $"+{Fmt( t.HeapReload )}s Reload" );
				break;
			case RoundTrait.Waste:
				Up( $"+{t.WasteBonus} Damage" );
				Note( $"Range < {Fmt( t.WasteRange )}" );
				Down( $"0 Damage after {Fmt( t.WasteFalloff )}" );
				break;
			case RoundTrait.Breach:
				Up( $"+{t.BreachPierce} Pierce" );
				break;
			case RoundTrait.Slug:
				Up( $"+{t.SlugDamage} Damage" );
				Note( $"Radius {Fmt( t.SlugRadius )}" );
				Up( $"+{Fmt( t.SlugFalloffPad )} Falloff Start" );
				break;
			case RoundTrait.Bore:
			{
				var pierce = Gain( t.BorePierce, rank );
				if ( pierce > 0f )
					Up( $"+{(int)pierce} Pierce" );
				Down( $"+{Fmt( ReloadGain( t.BoreReload, rank ) )}s Reload" );
				if ( rank <= 1 )
					Down( "Locks out LASH" );
				break;
			}
			case RoundTrait.Drum:
			{
				var burst = Gain( t.DrumBurst, rank );
				if ( burst > 0f )
					Up( $"+{(int)burst} Burst" );
				if ( rank <= 1 )
					Note( $"Cycle {Fmt( t.DrumCycle )}s" );
				Down( $"+{Fmt( ReloadGain( t.DrumReload, rank ) )}s Reload" );
				break;
			}
			case RoundTrait.Warhead:
			{
				var radius = Gain( t.WarheadRadius, rank );
				if ( radius > 0f )
					Up( $"+{Fmt( radius )} Splash Radius" );
				Down( $"{PctDelta( SpeedAt( t.WarheadSpeed, rank ) )} Projectile Speed" );
				if ( rank <= 1 )
					Down( "You take splash damage" );
				break;
			}
			case RoundTrait.Fuse:
				Up( $"+{Fmt( t.FuseSplash )} Splash Radius" );
				Down( $"{PctMul( t.FuseSpeed )} Projectile Speed" );
				Down( $"+{Fmt( t.FuseReload )}s Reload" );
				Down( "You take splash damage" );
				break;
			case RoundTrait.Fat:
				Up( $"Body {Fmt( t.FatRadius )}" );
				Down( $"{PctMul( t.FatSpeed )} Projectile Speed" );
				break;
			case RoundTrait.Ember:
				Up( $"+{Fmt( t.EmberSplash )} Splash Radius" );
				Down( $"0 Damage after {Fmt( t.EmberFalloff )}" );
				break;
			case RoundTrait.Blast:
				Up( $"+{Fmt( t.BlastSplash )} Splash Radius" );
				Down( $"+{Fmt( t.BlastReload )}s Reload" );
				Down( "You take splash damage" );
				break;
			case RoundTrait.Crack:
				Up( $"+{t.CrackDamage} Splash Damage" );
				break;
			case RoundTrait.Mine:
				Up( $"+{Fmt( t.MineSplash )} Splash Radius" );
				Note( "Explodes on first wall" );
				Down( "No bounce" );
				break;
			case RoundTrait.Cluster:
				Up( $"+{t.ClusterPellets} Projectiles" );
				Down( $"+{Fmt( t.ClusterCone )}° Spread" );
				Down( $"{PctMul( t.ClusterSplashMul )} Splash Radius" );
				Note( "Each rocket splashes" );
				Down( $"+{Fmt( t.ClusterReload )}s Reload" );
				break;
			case RoundTrait.Napalm:
				Up( $"+{Fmt( t.NapalmTime )}s Burn" );
				Note( "Burn on splash victims" );
				break;
			case RoundTrait.Shove:
				Up( $"+{Fmt( t.ShoveForce )} Blast Knockback" );
				break;
			case RoundTrait.Core:
				Up( $"+{Fmt( t.CoreSplash )} Splash Radius" );
				Up( $"+{t.CoreDamage} Splash Damage" );
				Down( $"{PctMul( t.CoreSpeed )} Projectile Speed" );
				Down( $"+{Fmt( t.CoreReload )}s Reload" );
				break;
			case RoundTrait.Safe:
				Up( "You ignore splash" );
				Down( $"{PctMul( t.SafeMul )} Splash Radius" );
				break;
			case RoundTrait.Nuke:
				Up( $"+{Fmt( t.NukeSplash )} Splash Radius" );
				Up( $"+{t.NukeDamage} Splash Damage" );
				Down( $"{PctMul( t.NukeSpeed )} Projectile Speed" );
				Down( $"+{Fmt( t.NukeReload )}s Reload" );
				Down( "You take splash damage" );
				break;
			case RoundTrait.Lash:
			{
				if ( rank <= 1 )
				{
					Note( "Hold to fire lightning" );
					Up( $"+{Math.Max( 1, t.LashHit )} Damage" );
					Note( $"Tick {Fmt( t.LashTick.At( 1 ) )}s" );
					Note( $"Max hold {Fmt( t.LashMaxHold )}s" );
					Down( "Locks out BORE" );
				}
				else
				{
					Note( "Lightning forks" );
					var tick = t.LashTick.At( rank ) - t.LashTick.At( rank - 1 );
					if ( tick < -0.001f )
						Up( $"{Fmt( tick )}s Bolt Tick" );
					else if ( tick > 0.001f )
						Down( $"+{Fmt( tick )}s Bolt Tick" );
				}
				break;
			}
			case RoundTrait.Pin:
			{
				var nails = Gain( t.PinNails, rank );
				var bounce = Gain( t.PinBounce, rank );
				var cone = Gain( t.PinCone, rank );
				if ( nails > 0f )
					Up( $"+{(int)nails} Projectile" );
				if ( bounce > 0f )
					Up( $"+{(int)bounce} Bounce" );
				if ( cone > 0.001f )
					Down( $"+{Fmt( cone )}° Spread" );
				if ( rank <= 1 )
					Note( $"Stick {Fmt( t.PinStick )}s" );
				break;
			}
			case RoundTrait.Spin:
			{
				var spin = Gain( t.SpinBoost, rank );
				if ( spin > 0f )
					Up( $"+{Fmt( spin )} Sweep Speed" );
				Down( $"+{Fmt( ReloadGain( t.SpinReload, rank ) )}s Reload" );
				break;
			}
			case RoundTrait.Rush:
				Up( $"{PctDelta( SpeedAt( t.RushSpeed, rank ) )} Projectile Speed" );
				Down( $"+{Fmt( ReloadGain( t.RushReload, rank ) )}s Reload" );
				break;
		}

		return lines;
	}

	public static List<SheetRow> Weapon( GameLoop loop, RoundTrait? hover )
	{
		if ( loop is null || !loop.Inventory.IsValid() )
			return [];

		var loadout = loop.Inventory.Loadout;
		var now = loadout.Recipe();
		var next = hover is { } trait ? loadout.Peek( trait ) : now;
		var preview = hover.HasValue;
		var mag = loop.Inventory.MagCap;
		var rows = new List<SheetRow>();

		AddText( rows, "Mode", Mode( now ), Mode( next ), preview );
		AddInt( rows, "Damage", now.Damage, next.Damage, preview, true, true );
		AddInt( rows, "Projectiles", now.Count, next.Count, preview, true, true );
		AddFloat( rows, "Spread", now.Cone, next.Cone, preview, false, "°" );
		AddInt( rows, "Pierce", now.Pierce, next.Pierce, preview, true, false );
		AddInt( rows, "Bounces", now.Bounces, next.Bounces, preview, true, false );
		AddFloat( rows, "Reload", now.Reload, next.Reload, preview, false, "s", true );
		AddPct( rows, "Proj. Speed", now.SpeedScale, next.SpeedScale, preview, true );
		AddFloat( rows, "Sweep", now.SpinSpeed, next.SpinSpeed, preview, true, "", true );
		AddInt( rows, "Mag", mag, mag, preview, true, true );
		AddFloat( rows, "Falloff", now.Falloff, next.Falloff, preview, true );
		AddFloat( rows, "Meat Range", now.MeatRange, next.MeatRange, preview, true );
		AddInt( rows, "Meat Damage", now.MeatBonus, next.MeatBonus, preview, true, false );
		AddFloat( rows, "Splash", now.Splash, next.Splash, preview, true );
		AddInt( rows, "Splash Dmg", now.BlastDamage, next.BlastDamage, preview, true, false );
		AddFloat( rows, "Blast Knock", now.BlastShove, next.BlastShove, preview, true );
		if ( now.Auto || next.Auto || now.Burst > 1 || next.Burst > 1 )
		{
			AddInt( rows, "Burst", now.Burst, next.Burst, preview, true, true );
			AddFloat( rows, "Cycle", now.Cycle, next.Cycle, preview, false, "s", true );
		}
		if ( now.Beam || next.Beam )
		{
			AddInt( rows, "Bolt Dmg", now.BeamHit, next.BeamHit, preview, true, true );
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
		var hud = GameSettings.Text.Hud;
		rows.Add( new SheetRow( "HP", $"{loop.Health} / {loop.HeartMax}" ) );
		rows.Add( new SheetRow( "Speed", runner.IsValid() ? Fmt( runner.Speed ) : "-" ) );
		rows.Add( new SheetRow( "Dash CD", runner.IsValid() ? $"{Fmt( runner.DashCooldown )}s" : "-" ) );
		rows.Add( new SheetRow( "Dash Dist", runner.IsValid() ? Fmt( runner.DashDistance ) : "-" ) );
		rows.Add( new SheetRow( "Slow", runner.IsValid() && runner.SlowUnlocked ? "Yes" : "No" ) );
		rows.Add( new SheetRow( hud.Scrap, loop.Scrap.ToString() ) );
		rows.Add( new SheetRow( hud.Stash, loop.Stash.ToString() ) );
		rows.Add( new SheetRow( "Bonus Dmg", (loadout?.BonusDamage ?? 0).ToString() ) );
		rows.Add( new SheetRow( "Blood", loop.BloodShields.ToString() ) );
		return rows;
	}

	static void AddInt( List<SheetRow> rows, string label, int now, int next, bool preview, bool higherIsGood, bool always )
	{
		if ( !always && now == 0 && next == 0 )
			return;

		rows.Add( Row( label, now.ToString(), next.ToString(), now, next, preview, higherIsGood ) );
	}

	static void AddFloat( List<SheetRow> rows, string label, float now, float next, bool preview, bool higherIsGood, string suffix = "", bool always = false )
	{
		if ( !always && now <= 0.001f && next <= 0.001f )
			return;

		rows.Add( Row( label, Fmt( now ) + suffix, Fmt( next ) + suffix, now, next, preview, higherIsGood ) );
	}

	static void AddPct( List<SheetRow> rows, string label, float now, float next, bool preview, bool higherIsGood )
	{
		rows.Add( Row( label, Pct( now ), Pct( next ), now, next, preview, higherIsGood ) );
	}

	static void AddText( List<SheetRow> rows, string label, string now, string next, bool preview )
	{
		if ( now == "No" && next == "No" )
			return;

		if ( !preview || now == next )
		{
			rows.Add( new SheetRow( label, now ) );
			return;
		}

		rows.Add( new SheetRow( label, now, next, next == "Yes" ? 1 : 0 ) );
	}

	static SheetRow Row( string label, string nowText, string nextText, float now, float next, bool preview, bool higherIsGood )
	{
		if ( !preview || Almost( now, next ) )
			return new SheetRow( label, nowText );

		var better = next > now;
		var sign = better == higherIsGood ? 1 : -1;
		return new SheetRow( label, nowText, nextText, sign );
	}

	static float Gain( TraitTiers tiers, int rank )
	{
		var now = rank <= 1 ? 0f : tiers.At( rank - 1 );
		return tiers.At( rank ) - now;
	}

	static float SpeedAt( TraitTiers tiers, int rank )
	{
		var now = rank <= 1 ? 1f : tiers.At( rank - 1 );
		return tiers.At( rank ) / MathF.Max( 0.01f, now );
	}

	static float ReloadGain( float add, int rank )
	{
		var before = rank <= 1 ? 0f : add * Progression.TraitMul( rank - 1 );
		return add * Progression.TraitMul( rank ) - before;
	}

	static string Mode( GunRecipe recipe )
	{
		if ( recipe.Beam )
			return "Lightning";
		if ( recipe.Splash > 1f )
			return "Rocket";
		if ( recipe.Auto )
			return "Auto";
		if ( recipe.DoublePump )
			return "Double";
		return "Semi";
	}

	static string Flag( bool on ) => on ? "Yes" : "No";


	static string PctDelta( float scale )
	{
		var pct = (int)MathF.Round( (scale - 1f) * 100f );
		return pct >= 0 ? $"+{pct}%" : $"{pct}%";
	}

	static string PctMul( float scale ) => PctDelta( scale );

	static string Pct( float scale ) => $"{(int)MathF.Round( scale * 100f )}%";

	static string Fmt( float value )
	{
		if ( MathF.Abs( value - MathF.Round( value ) ) < 0.05f )
			return ((int)MathF.Round( value )).ToString();

		return value.ToString( "0.##" );
	}

	static bool Almost( float a, float b ) => MathF.Abs( a - b ) < 0.01f;
}
