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
			return magnet <= 0 ? 0f : 24f + magnet * 28f;
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
			Tint = slot.Tint,
			Damage = 1 + BonusDamage,
			PierceCharges = pierce,
			MaxBounces = 4 + bounce * 2,
			Energy = 5500f + bounce * 900f,
			MagnetRadius = magnet <= 0 ? 0f : 200f + magnet * 140f,
			MagnetPull = magnet <= 0 ? 0f : 1.4f + magnet * 0.9f,
			CatchBonus = magnet <= 0 ? 0f : 24f + magnet * 28f,
			FreezeDuration = freeze <= 0 ? 0f : 1.1f + freeze * 0.7f,
			FreezeScale = freeze <= 0 ? 1f : MathX.Lerp( 0.55f, 0.22f, (freeze - 1) / 2f )
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
	Playing,
	DecideLap,
	PickTrait,
	Dead,
	Extracted,
	City
}
