namespace LoopedLoaded;

public sealed class RunLoadout
{
	public int BonusDamage;
	public readonly int[] Levels = new int[16];

	public int TraitLevel( RoundTrait trait )
	{
		var index = (int)trait;
		return index < 0 || index >= Levels.Length ? 0 : Levels[index];
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
		if ( index < 0 || index >= Levels.Length )
			return;

		Levels[index] = Math.Min( GameSettings.Traits.MaxLevel, Levels[index] + 1 );
	}

	public GunRecipe Recipe()
	{
		var t = GameSettings.Traits;
		var buck = TraitLevel( RoundTrait.Buck );
		var bore = TraitLevel( RoundTrait.Bore );
		var drum = TraitLevel( RoundTrait.Drum );
		var warhead = TraitLevel( RoundTrait.Warhead );
		var lash = TraitLevel( RoundTrait.Lash );
		var pin = TraitLevel( RoundTrait.Pin );

		var count = 1;
		var cone = 0f;
		if ( buck > 0 )
		{
			count = Math.Max( 1, (int)t.BuckPellets.At( buck ) );
			cone = t.BuckCone.At( buck );
		}
		else if ( pin > 0 )
		{
			count = Math.Max( 1, (int)t.PinNails.At( pin ) );
			cone = t.PinCone.At( pin );
		}

		var bounces = t.MaxBouncesBase;
		if ( pin > 0 )
			bounces += (int)t.PinBounce.At( pin );
		if ( lash > 0 )
			bounces = 0;

		var reload = t.ReloadBase;
		var boreWait = 0f;
		if ( bore > 0 )
		{
			boreWait = t.BoreReload * Progression.TraitMul( bore );
			reload += boreWait;
		}
		if ( drum > 0 )
			reload += t.DrumReload * Progression.TraitMul( drum );

		return new GunRecipe
		{
			Beam = lash > 0,
			Auto = drum > 0 && lash <= 0,
			Count = count,
			Cone = cone,
			Damage = Math.Max( 1, t.BaseDamage + BonusDamage ),
			Pierce = bore <= 0 ? 0 : (int)t.BorePierce.At( bore ),
			Bounces = bounces,
			Energy = t.EnergyBase,
			SpeedScale = warhead <= 0 ? 1f : t.WarheadSpeed.At( warhead ),
			Radius = pin > 0 ? t.PinRadius : 13f,
			Splash = t.WarheadRadius.At( warhead ),
			FriendlySplash = warhead > 0,
			Nail = pin > 0,
			StickTime = pin > 0 ? t.PinStick : 0f,
			Falloff = buck > 0 ? t.BuckFalloff.At( buck ) : 0f,
			Cycle = t.DrumCycle,
			Burst = drum <= 0 ? 1 : Math.Max( 1, (int)t.DrumBurst.At( drum ) ),
			Reload = MathF.Max( t.ReloadMin, reload ),
			BoreWait = boreWait,
			BeamPad = t.LashPad,
			BeamPerSecond = t.LashPerSecond,
			BeamMaxHold = t.LashMaxHold,
			BeamTick = t.LashTick,
			BeamRange = t.LashRange,
			BeamWidth = t.LashWidth
		};
	}
}

public struct GunRecipe
{
	public bool Beam;
	public bool Auto;
	public int Count;
	public float Cone;
	public int Damage;
	public int Pierce;
	public int Bounces;
	public float Energy;
	public float SpeedScale;
	public float Radius;
	public float Splash;
	public bool FriendlySplash;
	public bool Nail;
	public float StickTime;
	public float Falloff;
	public float Cycle;
	public int Burst;
	public float Reload;
	public float BoreWait;
	public float BeamPad;
	public float BeamPerSecond;
	public float BeamMaxHold;
	public float BeamTick;
	public float BeamRange;
	public float BeamWidth;
}

public struct RoundFlight
{
	public Color Tint;
	public int Damage;
	public int PierceCharges;
	public int MaxBounces;
	public float Energy;
	public float SpeedScale;
	public float ExplosiveRadius;
	public bool FriendlySplash;
	public float Falloff;
	public float StickTime;
	public bool Nail;
	public float FreezeDuration;
	public float FreezeScale;
}

public enum RunPhase
{
	Menu,
	Playing,
	DecideLap,
	DecideRing,
	PickTrait,
	Dead,
	Extracted,
	Won,
	City
}
