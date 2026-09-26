namespace LoopedLoaded;

public static class Trinkets
{
	public static IReadOnlyList<TrinketDef> All => Ensure().pool;
	public static IReadOnlyList<TrinketDef> Every => Ensure().every;

	public static TrinketDef Find( string id )
	{
		if ( string.IsNullOrWhiteSpace( id ) )
			return null;

		Ensure().byId.TryGetValue( id, out var card );
		return card;
	}

	public static bool Generic( TraitPack pack ) => pack is TraitPack.Entry or TraitPack.Junior or TraitPack.Warrior or TraitPack.Abomination;

	public static bool ShowRank( TrinketDef card ) => card is not null && (Generic( card.Pack ) || card.Cap > 1);

	public static bool IsWeapon( TrinketDef card )
	{
		if ( card is null || Live( card.Requires ).Count > 0 )
			return false;

		foreach ( var dep in Dependents( card ) )
		{
			if ( dep.InPool )
				return true;
		}

		return false;
	}

	public static IReadOnlyList<TrinketDef> Dependents( TrinketDef card )
	{
		if ( card is null )
			return Array.Empty<TrinketDef>();

		return Ensure().dependents.TryGetValue( card, out var list ) ? list : Array.Empty<TrinketDef>();
	}

	public static IReadOnlyList<TrinketDef> ExcludesOf( TrinketDef card )
	{
		if ( card is null )
			return Array.Empty<TrinketDef>();

		return Ensure().excludes.TryGetValue( card, out var list ) ? list : Array.Empty<TrinketDef>();
	}

	public static string Unlocks( TrinketDef card )
	{
		var names = new List<string>();
		foreach ( var dep in Dependents( card ) )
		{
			if ( !dep.InPool )
				continue;

			names.Add( dep.ShownTitle );
		}

		return names.Count == 0 ? "" : string.Join( ", ", names );
	}

	public static bool TooEarly( TrinketDef card, int lap ) => card is not null && card.UnlockLap > 0 && lap < card.UnlockLap;

	public static int Weight( TrinketDef card, RunLoadout loadout )
	{
		if ( card is null )
			return 0;

		var weight = Math.Max( 0, GameSettings.Traits.PackWeight( card.Pack ) );
		if ( card.OwnedWeight > 0 && loadout is not null && loadout.TraitLevel( card ) > 0 )
			return Math.Max( weight, card.OwnedWeight );

		return weight;
	}

	public static bool Blocked( TrinketDef card, RunLoadout loadout )
	{
		if ( card is null || !card.InPool )
			return true;

		if ( loadout is null )
			return false;

		foreach ( var req in Live( card.Requires ) )
		{
			if ( !loadout.Has( req ) )
				return true;
		}

		foreach ( var ex in ExcludesOf( card ) )
		{
			if ( loadout.Has( ex ) )
				return true;
		}

		return false;
	}

	public static Color PackColor( TrinketDef card )
	{
		if ( card is null )
			return new Color( 0f, 0.439f, 0.867f );

		return card.Pack switch
		{
			TraitPack.Rifle or TraitPack.Shotgun => new Color( 0.616f, 0.616f, 0.616f ),
			TraitPack.Nailgun or TraitPack.Junior => new Color( 0.118f, 1f, 0f ),
			TraitPack.Warrior or TraitPack.Laser or TraitPack.Rail or TraitPack.Rocket => new Color( 0f, 0.439f, 0.867f ),
			TraitPack.Abomination => new Color( 0.639f, 0.208f, 0.933f ),
			TraitPack.Entry => new Color( 0.95f, 0.95f, 0.95f ),
			_ => new Color( 0f, 0.439f, 0.867f )
		};
	}

	public static string Markdown()
	{
		var index = Ensure();
		var text = GameSettings.Text;
		var traits = GameSettings.Traits;
		var lines = new List<string>
		{
			"# Trinkets",
			"",
			"## Globals",
			"",
			"| Field | Value |",
			"| --- | --- |",
			$"| MaxLevel | {traits.MaxLevel} |",
			$"| BaseDamage | {traits.BaseDamage} |",
			$"| MaxBouncesBase | {traits.MaxBouncesBase} |",
			$"| EnergyBase | {Fmt( traits.EnergyBase )} |",
			$"| ReloadBase | {Fmt( traits.ReloadBase )} |",
			$"| ReloadMin | {Fmt( traits.ReloadMin )} |",
			$"| ProjectileRadius | {Fmt( traits.ProjectileRadius )} |",
			$"| DrumCycle | {Fmt( traits.DrumCycle )} |",
			$"| LashWidth | {Fmt( traits.LashWidth )} |",
			"",
			"## Packs",
			"",
			"| Pack | Rarity | Price | Weight | Single |",
			"| --- | --- | --- | --- | --- |"
		};

		foreach ( var pack in Enum.GetValues<TraitPack>() )
		{
			var stats = traits.PackOf( pack );
			lines.Add( $"| {pack} | {text.RarityOf( pack )} | {stats.Price} | {stats.Weight} | {(stats.Single ? "yes" : "")} |" );
		}

		TraitPack? section = null;
		foreach ( var card in index.every )
		{
			if ( section != card.Pack )
			{
				section = card.Pack;
				lines.Add( "" );
				lines.Add( $"## {card.Pack}" );
			}

			lines.Add( "" );
			lines.Add( $"### {card.ShownTitle}" );
			lines.Add( "" );
			lines.Add( "| Field | Value |" );
			lines.Add( "| --- | --- |" );
			lines.Add( $"| Id | {card.Id} |" );
			lines.Add( $"| Pack | {card.Pack} |" );
			lines.Add( $"| InPool | {(card.InPool ? "yes" : "no")} |" );
			lines.Add( $"| MaxLevel | {card.Cap} |" );
			if ( card.Price > 0 )
				lines.Add( $"| Price | {card.Price} |" );
			if ( card.OwnedWeight > 0 )
				lines.Add( $"| OwnedWeight | {card.OwnedWeight} |" );
			if ( card.UnlockLap > 0 )
				lines.Add( $"| UnlockLap | {card.UnlockLap} |" );
			if ( card.Hook != TrinketHook.None )
				lines.Add( $"| Hook | {card.Hook} |" );
			if ( !string.IsNullOrWhiteSpace( card.Blurb ) )
				lines.Add( $"| Blurb | {Escape( card.Blurb )} |" );
			var requires = Live( card.Requires );
			if ( requires.Count > 0 )
				lines.Add( $"| Requires | {string.Join( ", ", requires.Select( item => item.Id ) )} |" );
			var excludes = ExcludesOf( card );
			if ( excludes.Count > 0 )
				lines.Add( $"| Excludes | {string.Join( ", ", excludes.Select( item => item.Id ) )} |" );
			if ( card.Flags is not null )
			{
				foreach ( var flag in card.Flags )
					lines.Add( $"| Flag | {flag} |" );
			}
			if ( card.Mods is null )
				continue;

			foreach ( var mod in card.Mods )
			{
				if ( mod is null )
					continue;

				lines.Add( $"| {mod.Stat} {mod.Op} | {ModText( mod )} |" );
			}
		}

		lines.Add( "" );
		return string.Join( "\n", lines );
	}

	public static void Refresh() => ready = null;

	static Index ready;

	static Index Ensure()
	{
		if ( ready is not null && ready.every.Count > 0 )
			return ready;

		var found = new List<TrinketDef>();
		foreach ( var card in ResourceLibrary.GetAll<TrinketDef>() )
		{
			if ( card is not null )
				found.Add( card );
		}

		found.Sort( ( a, b ) =>
		{
			var order = a.Sort.CompareTo( b.Sort );
			return order != 0 ? order : string.Compare( a.Id, b.Id, StringComparison.Ordinal );
		} );

		var index = new Index();
		foreach ( var card in found )
		{
			if ( string.IsNullOrWhiteSpace( card.Id ) )
			{
				Log.Warning( $"Trinket is missing an Id ({card.ResourcePath})" );
				continue;
			}

			if ( !index.byId.TryAdd( card.Id, card ) )
			{
				Log.Warning( $"Duplicate trinket id {card.Id}" );
				continue;
			}

			index.every.Add( card );
			if ( card.InPool )
				index.pool.Add( card );

			WarnMissing( card, card.Requires, "requirement" );
			WarnMissing( card, card.Excludes, "exclude" );
			if ( card.Hook == TrinketHook.Pin && !HasRole( card, "nails" ) )
				Log.Warning( $"Trinket {card.Id} Pin hook is missing a nails mod" );
			if ( card.Hook == TrinketHook.Pin && !HasRole( card, "cone" ) )
				Log.Warning( $"Trinket {card.Id} Pin hook is missing a cone mod" );
		}

		foreach ( var card in index.every )
		{
			foreach ( var req in Live( card.Requires ) )
			{
				if ( !index.dependents.TryGetValue( req, out var list ) )
				{
					list = new List<TrinketDef>();
					index.dependents[req] = list;
				}

				list.Add( card );
			}

			foreach ( var ex in Live( card.Excludes ) )
			{
				Link( index.excludeSets, card, ex );
				Link( index.excludeSets, ex, card );
			}
		}

		foreach ( var pair in index.dependents )
			pair.Value.Sort( ( a, b ) =>
			{
				var order = a.Sort.CompareTo( b.Sort );
				return order != 0 ? order : string.Compare( a.Id, b.Id, StringComparison.Ordinal );
			} );

		foreach ( var pair in index.excludeSets )
		{
			var list = pair.Value.ToList();
			list.Sort( ( a, b ) =>
			{
				var order = a.Sort.CompareTo( b.Sort );
				return order != 0 ? order : string.Compare( a.Id, b.Id, StringComparison.Ordinal );
			} );
			index.excludes[pair.Key] = list;
		}

		ready = index;
		return index;
	}

	static void Link( Dictionary<TrinketDef, HashSet<TrinketDef>> map, TrinketDef card, TrinketDef other )
	{
		if ( card is null || other is null || card == other )
			return;

		if ( !map.TryGetValue( card, out var set ) )
		{
			set = new HashSet<TrinketDef>();
			map[card] = set;
		}

		set.Add( other );
	}

	static void WarnMissing( TrinketDef card, List<TrinketDef> list, string label )
	{
		if ( list is null )
			return;

		foreach ( var item in list )
		{
			if ( item is null )
				Log.Warning( $"Trinket {card.Id} has a missing {label}" );
		}
	}

	static bool HasRole( TrinketDef card, string role )
	{
		if ( card.Mods is null )
			return false;

		foreach ( var mod in card.Mods )
		{
			if ( mod is not null && mod.Role == role )
				return true;
		}

		return false;
	}

	internal static List<TrinketDef> Live( List<TrinketDef> list )
	{
		var live = new List<TrinketDef>();
		if ( list is null )
			return live;

		foreach ( var item in list )
		{
			if ( item is not null )
				live.Add( item );
		}

		return live;
	}

	static string ModText( TrinketMod mod )
	{
		var body = mod.Growth switch
		{
			ModGrowth.Tiers => TierText( mod.Tiers ),
			ModGrowth.Linear => $"{Fmt( mod.Value )} x level",
			ModGrowth.TraitRatio => $"{Fmt( mod.Value )} x rank",
			ModGrowth.PerRemoved => $"{Fmt( mod.Value )} per removed",
			_ => Fmt( mod.Value )
		};
		if ( MathF.Abs( mod.Bias ) > 0.0001f )
			body += $" bias {Fmt( mod.Bias )}";
		if ( mod.ByHook )
			body += " hook";
		if ( mod.Passive )
			body += " passive";
		body += $" @{mod.Order}";
		return body;
	}

	static string TierText( TraitTiers tiers )
	{
		if ( tiers is null )
			return "0";

		var text = $"{Fmt( tiers.Level1 )} / {Fmt( tiers.Level2 )} / {Fmt( tiers.Level3 )}";
		if ( MathF.Abs( tiers.Level4 ) > 0.0001f )
			text += $" / {Fmt( tiers.Level4 )}";
		return text;
	}

	static string Fmt( float value )
	{
		if ( MathF.Abs( value - MathF.Round( value ) ) < 0.001f )
			return ((int)MathF.Round( value )).ToString();

		return value.ToString( "0.##", System.Globalization.CultureInfo.InvariantCulture );
	}

	static string Escape( string value ) => value.Replace( "|", "\\|" ).Replace( "\n", " " );

	sealed class Index
	{
		public readonly Dictionary<string, TrinketDef> byId = new();
		public readonly List<TrinketDef> every = new();
		public readonly List<TrinketDef> pool = new();
		public readonly Dictionary<TrinketDef, List<TrinketDef>> dependents = new();
		public readonly Dictionary<TrinketDef, HashSet<TrinketDef>> excludeSets = new();
		public readonly Dictionary<TrinketDef, List<TrinketDef>> excludes = new();
	}
}
