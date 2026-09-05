namespace LoopedLoaded;

public sealed class RoundSlot
{
	public int Index;
	public Color Tint;
	public RoundStatus Status = RoundStatus.Chambered;
	public RoundProjectile Flying;
	public DroppedRound Lost;

	public int StatusHash => Index * 17 + (int)Status;

	public void ResetCombat()
	{
		Flying?.GameObject?.Destroy();
		Lost?.GameObject?.Destroy();
		Flying = null;
		Lost = null;
		Status = RoundStatus.Chambered;
	}
}

public sealed class RunLoadout
{
	public int BonusDamage;
	public readonly int[] Levels = new int[4];

	public int TraitLevel( RoundTrait trait ) => Levels[(int)trait];

	public int Hash
	{
		get
		{
			var hash = 0;
			foreach ( var level in Levels )
				hash = hash * 8 + level;
			return hash;
		}
	}

	public float CatchBonus
	{
		get
		{
			var magnet = TraitLevel( RoundTrait.Magnetic );
			return magnet <= 0 ? 0f : 24f * Progression.TraitMul( magnet );
		}
	}

	public void Clear()
	{
		for ( var i = 0; i < Levels.Length; i++ )
			Levels[i] = 0;

		BonusDamage = 0;
	}

	public void Install( RoundTrait trait )
	{
		var index = (int)trait;
		Levels[index] = Math.Min( 3, Levels[index] + 1 );
	}

	public RoundFlight BuildFlight( RoundSlot slot )
	{
		var pierce = TraitLevel( RoundTrait.Pierce );
		var bounce = TraitLevel( RoundTrait.Bounce );
		var magnet = TraitLevel( RoundTrait.Magnetic );
		var freeze = TraitLevel( RoundTrait.Freeze );

		return new RoundFlight
		{
			SlotIndex = slot.Index,
			Tint = ShotColors.Player,
			Damage = 1 + BonusDamage,
			PierceCharges = pierce <= 0 ? 0 : 1 << (pierce - 1),
			MaxBounces = 4 + Progression.TraitStack( bounce, 2f ),
			Energy = 5500f * ( bounce <= 0 ? 1f : Progression.TraitMul( bounce ) ),
			MagnetRadius = magnet <= 0 ? 0f : 200f * Progression.TraitMul( magnet ),
			MagnetPull = magnet <= 0 ? 0f : 1.4f * Progression.TraitMul( magnet ),
			CatchBonus = magnet <= 0 ? 0f : 24f * Progression.TraitMul( magnet ),
			FreezeDuration = freeze <= 0 ? 0f : 1.1f * Progression.TraitMul( freeze ),
			FreezeScale = freeze <= 0 ? 1f : MathF.Max( 0.18f, 0.55f / Progression.TraitMul( freeze ) ),
		};
	}
}

public struct RoundFlight
{
	public int SlotIndex;
	public Color Tint;
	public int Damage;
	public int PierceCharges;
	public int MaxBounces;
	public float Energy;
	public float MagnetRadius;
	public float MagnetPull;
	public float CatchBonus;
	public float FreezeDuration;
	public float FreezeScale;
}

public enum RunPhase
{
	Menu,
	Playing,
	DecideLap,
	PickTrait,
	Dead,
	Extracted,
	City
}
