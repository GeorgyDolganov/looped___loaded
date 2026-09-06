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
	public readonly int[] Levels = new int[64];

	public int TraitLevel( RoundTrait trait )
	{
		var index = (int)trait;
		return index < 0 || index >= Levels.Length ? 0 : Levels[index];
	}

	public float CatchBonus
	{
		get
		{
			var magnet = TraitLevel( RoundTrait.Magnetic );
			return magnet <= 0 ? 0f : 24f * Progression.TraitMul( magnet );
		}
	}

	public float BackstopScale => Progression.Tier( TraitLevel( RoundTrait.Backstop ), 0.5f, 0.7f, 1f );
	public float SwipeRadius => Progression.Tier( TraitLevel( RoundTrait.Swipe ), 28f, 40f, 56f );
	public float LinkRadius => Progression.Tier( TraitLevel( RoundTrait.Link ), 24f, 34f, 47f );
	public float ReelSpeed => Progression.Tier( TraitLevel( RoundTrait.Reel ), 90f, 126f, 176f );
	public float RimMinAngle => Progression.Tier( TraitLevel( RoundTrait.Rim ), 25f, 18f, 10f );
	public int SnapPreview => TraitLevel( RoundTrait.Snap ) <= 0 ? 0 : TraitLevel( RoundTrait.Snap );
	public float SnapSpeed => TraitLevel( RoundTrait.Snap ) <= 0 ? 1f : Progression.Tier( TraitLevel( RoundTrait.Snap ), 1.08f, 1.12f, 1.16f );
	public int CueBounces => TraitLevel( RoundTrait.Cue ) >= 3 ? 2 : 1;
	public int BloodThreshold
	{
		get
		{
			var level = TraitLevel( RoundTrait.Blood );
			if ( level <= 0 )
				return 0;

			return level >= 3 ? 1 : 2;
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
		if ( index < 0 || index >= Levels.Length )
			return;

		Levels[index] = Math.Min( 3, Levels[index] + 1 );
	}

	public RoundFlight BuildFlight( RoundSlot slot )
	{
		var pierce = TraitLevel( RoundTrait.Pierce );
		var bounce = TraitLevel( RoundTrait.Bounce );
		var magnet = TraitLevel( RoundTrait.Magnetic );
		var freeze = TraitLevel( RoundTrait.Freeze );
		var heavy = TraitLevel( RoundTrait.Heavy );
		var skim = TraitLevel( RoundTrait.Skim );
		var stick = TraitLevel( RoundTrait.Stick );
		var stutter = TraitLevel( RoundTrait.Stutter );
		var incur = TraitLevel( RoundTrait.Incurve );
		var clock = TraitLevel( RoundTrait.Clockwise );
		var late = TraitLevel( RoundTrait.LateMag );
		var pinball = TraitLevel( RoundTrait.Pinball );
		var ribbon = TraitLevel( RoundTrait.Ribbon );
		var echo = TraitLevel( RoundTrait.Echo );

		var stickTime = Progression.Tier( stick, 0.10f, 0.14f, 0.20f );
		if ( stick > 0 && stutter > 0 )
			stickTime *= 0.7f;

		var curveIn = Progression.Tier( incur, 4f, 6f, 8f );
		var curveClock = Progression.Tier( clock, 4f, 6f, 8f );
		if ( incur > 0 && clock > 0 )
		{
			if ( clock > incur )
				curveIn = 0f;
			else
				curveClock = 0f;
		}

		var magRadius = magnet <= 0 ? 0f : 200f * Progression.TraitMul( magnet );
		var magPull = magnet <= 0 ? 0f : 1.4f * Progression.TraitMul( magnet );
		if ( late > 0 && magnet <= 0 )
		{
			magRadius = Progression.Tier( late, 140f, 196f, 274f );
			magPull = 1.4f;
		}

		return new RoundFlight
		{
			SlotIndex = slot.Index,
			Tint = ShotColors.Player,
			Damage = 1 + BonusDamage + (heavy <= 0 ? 0 : heavy >= 3 ? 2 : 1),
			PierceCharges = pierce <= 0 ? 0 : 1 << (pierce - 1),
			MaxBounces = 4 + Progression.TraitStack( bounce, 2f ),
			Energy = 5500f * (bounce <= 0 ? 1f : Progression.TraitMul( bounce )),
			MagnetRadius = magRadius,
			MagnetPull = magPull * (late <= 0 ? 1f : Progression.TraitMul( late )),
			CatchBonus = magnet <= 0 ? 0f : 24f * Progression.TraitMul( magnet ),
			FreezeDuration = freeze <= 0 ? 0f : 1.1f * Progression.TraitMul( freeze ),
			FreezeScale = freeze <= 0 ? 1f : MathF.Max( 0.18f, 0.55f / Progression.TraitMul( freeze ) ),
			SpeedScale = heavy <= 0 ? 1f : Progression.Tier( heavy, 0.82f, 0.72f, 0.63f ),
			AccelMul = TraitLevel( RoundTrait.Accel ) <= 0 ? 1f : Progression.Tier( TraitLevel( RoundTrait.Accel ), 1.12f, 1.17f, 1.24f ),
			AccelCap = 1.8f,
			ExplosiveRadius = Progression.Tier( TraitLevel( RoundTrait.Explosive ), 90f, 126f, 176f ),
			ElectricJumps = TraitLevel( RoundTrait.Electric ),
			BloodThreshold = BloodThreshold,
			HomingLead = Progression.Tier( TraitLevel( RoundTrait.Homing ), 0.20f, 0.28f, 0.40f ),
			SkimDegrees = Progression.Tier( skim, 18f, 25f, 35f ),
			SkimBoost = skim <= 0 ? 1f : Progression.Tier( skim, 1.08f, 1.12f, 1.16f ),
			KickExtra = Progression.Tier( TraitLevel( RoundTrait.Kick ), 8f, 11f, 16f ),
			KickSecond = TraitLevel( RoundTrait.Kick ) >= 3,
			StickTime = stickTime,
			StickSpeed = stick <= 0 ? 1f : Progression.Tier( stick, 1.00f, 1.10f, 1.24f ),
			CushionDegrees = Progression.Tier( TraitLevel( RoundTrait.Cushion ), 22f, 31f, 43f ),
			CueDegrees = TraitLevel( RoundTrait.Cue ) <= 0 ? 0f : Progression.Tier( TraitLevel( RoundTrait.Cue ), 12f, 8f, 4f ),
			CornerRadius = Progression.Tier( TraitLevel( RoundTrait.Corner ), 70f, 98f, 137f ),
			CornerPull = Progression.Tier( TraitLevel( RoundTrait.Corner ), 0.7f, 1.0f, 1.4f ),
			IncurveRate = MathX.DegreeToRadian( curveIn ),
			ClockwiseRate = MathX.DegreeToRadian( curveClock ),
			StutterTime = Progression.Tier( stutter, 0.08f, 0.11f, 0.16f ),
			BreachCharges = TraitLevel( RoundTrait.Breach ),
			BoomerangKeep = Progression.Tier( TraitLevel( RoundTrait.Boomerang ), 0.50f, 0.70f, 1.00f ),
			FuseRadius = Progression.Tier( TraitLevel( RoundTrait.Fuse ), 70f, 98f, 137f ),
			LateMag = late > 0,
			SecondWindKeep = Progression.Tier( TraitLevel( RoundTrait.SecondWind ), 0.40f, 0.56f, 0.78f ),
			ShredCharges = TraitLevel( RoundTrait.Shred ),
			HookPush = Progression.Tier( TraitLevel( RoundTrait.Hook ), 36f, 50f, 70f ),
			MarkTurn = MathX.DegreeToRadian( Progression.Tier( TraitLevel( RoundTrait.Mark ), 8f, 11f, 16f ) ),
			MarkBonus = TraitLevel( RoundTrait.Mark ) >= 3 ? 1 : 0,
			PinballKeep = pinball <= 0 ? 0.94f : Progression.Tier( pinball, 0.70f, 0.80f, 0.90f ),
			PinballFree = pinball >= 3,
			HasPinball = pinball > 0,
			RibbonWidth = Progression.Tier( ribbon, 18f, 25f, 35f ),
			RibbonSlow = ribbon <= 0 ? 0f : Progression.Tier( ribbon, 0.20f, 0.28f, 0.40f ),
			GrazePad = Progression.Tier( TraitLevel( RoundTrait.Graze ), 10f, 14f, 20f ),
			RehitCharges = TraitLevel( RoundTrait.Rehit ),
			StepDistance = Progression.Tier( TraitLevel( RoundTrait.Step ), 80f, 112f, 157f ),
			EchoKeep = Progression.Tier( echo, 0.40f, 0.56f, 0.78f ),
			EchoFreeze = echo >= 3,
			RedirectCone = MathX.DegreeToRadian( Progression.Tier( TraitLevel( RoundTrait.Redirect ), 40f, 56f, 78f ) ),
			RedirectRange = Progression.Tier( TraitLevel( RoundTrait.Redirect ), 260f, 364f, 510f ),
			HeavyPush = heavy <= 0 ? 0f : 42f * Progression.TraitMul( heavy )
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
	public float SpeedScale;
	public float AccelMul;
	public float AccelCap;
	public float ExplosiveRadius;
	public int ElectricJumps;
	public int BloodThreshold;
	public float HomingLead;
	public float SkimDegrees;
	public float SkimBoost;
	public float KickExtra;
	public bool KickSecond;
	public float StickTime;
	public float StickSpeed;
	public float CushionDegrees;
	public float CueDegrees;
	public float CornerRadius;
	public float CornerPull;
	public float IncurveRate;
	public float ClockwiseRate;
	public float StutterTime;
	public int BreachCharges;
	public float BoomerangKeep;
	public float FuseRadius;
	public bool LateMag;
	public float SecondWindKeep;
	public int ShredCharges;
	public float HookPush;
	public float MarkTurn;
	public int MarkBonus;
	public float PinballKeep;
	public bool PinballFree;
	public bool HasPinball;
	public float RibbonWidth;
	public float RibbonSlow;
	public float GrazePad;
	public int RehitCharges;
	public float StepDistance;
	public float EchoKeep;
	public bool EchoFreeze;
	public float RedirectCone;
	public float RedirectRange;
	public float HeavyPush;
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
