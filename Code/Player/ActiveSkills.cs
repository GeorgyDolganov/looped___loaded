namespace LoopedLoaded;

public sealed class ActiveSkills
{
	public const int SlotCount = 4;
	public static readonly string[] Actions = { "Skill1", "Skill2", "Skill3", "Skill4" };
	public static readonly string[] Keys = { "Z", "X", "C", "V" };

	readonly Slot[] slots = { new(), new(), new(), new() };

	public void Sync( RunLoadout loadout )
	{
		for ( var i = 0; i < SlotCount; i++ )
		{
			if ( slots[i].Card is null )
				continue;

			if ( loadout is null || loadout.TraitLevel( slots[i].Card ) <= 0 )
				slots[i] = new Slot();
		}

		if ( loadout is null )
			return;

		foreach ( var pair in loadout.Owned() )
		{
			var card = pair.Key;
			if ( card is null || !card.IsSkill )
				continue;

			var held = IndexOf( card );
			if ( held >= 0 )
			{
				slots[held].Card = card;
				continue;
			}

			var index = Free();
			if ( index < 0 )
				break;

			slots[index].Card = card;
		}
	}

	public void Tick( GameLoop loop )
	{
		var dt = RealTime.Delta;
		for ( var i = 0; i < SlotCount; i++ )
		{
			var slot = slots[i];
			if ( slot.Left > 0f )
				slot.Left = MathF.Max( 0f, slot.Left - dt );

			if ( slot.Card is null || !Input.Pressed( Actions[i] ) )
				continue;

			if ( slot.Left > 0.001f )
			{
				ArenaSounds.Deny();
				continue;
			}

			Fire( loop, slot );
		}
	}

	public TrinketDef CardAt( int index ) => index >= 0 && index < SlotCount ? slots[index].Card : null;

	public float Charge( int index )
	{
		if ( index < 0 || index >= SlotCount || slots[index].Card is null )
			return 0f;

		var slot = slots[index];
		if ( slot.For <= 0.01f || slot.Left <= 0f )
			return 1f;

		return Math.Clamp( 1f - slot.Left / slot.For, 0f, 1f );
	}

	public bool Ready( int index ) => CardAt( index ) is not null && slots[index].Left <= 0.001f;

	public int Stamp
	{
		get
		{
			var hash = 0;
			for ( var i = 0; i < SlotCount; i++ )
				hash = System.HashCode.Combine( hash, slots[i].Card?.Id, slots[i].Card is not null && slots[i].Left <= 0.001f );

			return hash;
		}
	}

	public void ReadyAll()
	{
		for ( var i = 0; i < SlotCount; i++ )
		{
			slots[i].Left = 0f;
			slots[i].For = 0f;
		}
	}

	public void Clear()
	{
		for ( var i = 0; i < SlotCount; i++ )
			slots[i] = new Slot();
	}

	public static int Owned( RunLoadout loadout )
	{
		if ( loadout is null )
			return 0;

		var count = 0;
		foreach ( var pair in loadout.Owned() )
		{
			if ( pair.Key is not null && pair.Key.IsSkill && pair.Value > 0 )
				count++;
		}

		return count;
	}

	void Fire( GameLoop loop, Slot slot )
	{
		var cooldown = 0f;
		switch ( slot.Card.Skill )
		{
			case ActiveSkill.Repulse:
				var cfg = GameSettings.Skills.Repulse ?? new RepulseStats();
				Repulse( loop, cfg );
				cooldown = cfg.Cooldown;
				break;
		}

		slot.For = MathF.Max( 0.01f, cooldown );
		slot.Left = slot.For;
	}

	static void Repulse( GameLoop loop, RepulseStats cfg )
	{
		if ( !loop.Runner.IsValid() )
			return;

		var origin = loop.Runner.Flat;
		var radius = MathF.Max( 1f, cfg.Radius );
		foreach ( var enemy in loop.Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive )
				continue;

			var away = enemy.Flat - origin;
			var dist = away.Length;
			if ( dist > radius )
				continue;

			var dir = dist > 1f ? away / dist : Vector2.Right;
			var falloff = MathX.Lerp( 1f, cfg.EdgeScale, dist / radius );
			enemy.Shove( dir * (cfg.Force * falloff) );
			enemy.Stun( cfg.StunTime );
		}

		var world = loop.Geometry.ToPlayWorld( origin );
		RepulseWave.Spawn( loop.Scene, world, radius );
		ArenaSounds.Repulse( world );
	}

	int IndexOf( TrinketDef card )
	{
		for ( var i = 0; i < SlotCount; i++ )
		{
			if ( slots[i].Card is not null && slots[i].Card.Id == card.Id )
				return i;
		}

		return -1;
	}

	int Free()
	{
		for ( var i = 0; i < SlotCount; i++ )
		{
			if ( slots[i].Card is null )
				return i;
		}

		return -1;
	}

	sealed class Slot
	{
		public TrinketDef Card;
		public float Left;
		public float For;
	}
}
