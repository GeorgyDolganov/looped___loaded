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
			var traits = GameSettings.Traits;
			return magnet <= 0 ? 0f : traits.Magnetic.CatchBonus * Progression.TraitMul( magnet );
		}
	}

	public float BackstopScale => GameSettings.Traits.Backstop.At( TraitLevel( RoundTrait.Backstop ) );
	public float SwipeRadius => GameSettings.Traits.Swipe.At( TraitLevel( RoundTrait.Swipe ) );
	public float LinkRadius => GameSettings.Traits.Link.At( TraitLevel( RoundTrait.Link ) );
	public float ReelSpeed => GameSettings.Traits.Reel.At( TraitLevel( RoundTrait.Reel ) );
	public float RimMinAngle => GameSettings.Traits.Rim.At( TraitLevel( RoundTrait.Rim ) );
	public int SnapPreview => TraitLevel( RoundTrait.Snap ) <= 0 ? 0 : TraitLevel( RoundTrait.Snap );
	public float SnapSpeed => TraitLevel( RoundTrait.Snap ) <= 0 ? 1f : GameSettings.Traits.Snap.Speed.At( TraitLevel( RoundTrait.Snap ) );
	public int CueBounces
	{
		get
		{
			var cue = GameSettings.Traits.Cue;
			return TraitLevel( RoundTrait.Cue ) >= GameSettings.Traits.MaxLevel ? cue.BouncesAtMax : cue.Bounces;
		}
	}
	public int BloodThreshold => GameSettings.Traits.BloodThreshold( TraitLevel( RoundTrait.Blood ) );

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

	public RoundFlight BuildFlight( RoundSlot slot )
	{
		var t = GameSettings.Traits;
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
		var max = t.MaxLevel;

		var stickTime = t.Stick.Time.At( stick );
		if ( stick > 0 && stutter > 0 )
			stickTime *= t.StickStutterScale;

		var curveIn = t.Curve.At( incur );
		var curveClock = t.Curve.At( clock );
		if ( incur > 0 && clock > 0 )
		{
			if ( clock > incur )
				curveIn = 0f;
			else
				curveClock = 0f;
		}

		var magRadius = magnet <= 0 ? 0f : t.Magnetic.Radius * Progression.TraitMul( magnet );
		var magPull = magnet <= 0 ? 0f : t.Magnetic.Pull * Progression.TraitMul( magnet );
		if ( late > 0 && magnet <= 0 )
		{
			magRadius = t.LateMag.Radius.At( late );
			magPull = t.LateMag.Pull;
		}

		return new RoundFlight
		{
			SlotIndex = slot.Index,
			Tint = ShotColors.Player,
			Damage = t.BaseDamage + BonusDamage + (heavy <= 0 ? 0 : (int)t.Heavy.Damage.At( heavy )),
			PierceCharges = pierce <= 0 ? 0 : (int)t.PierceCharges.At( pierce ),
			MaxBounces = t.MaxBouncesBase + Progression.TraitStack( bounce, t.BounceStackSeed ),
			Energy = t.EnergyBase * (bounce <= 0 ? 1f : Progression.TraitMul( bounce )),
			MagnetRadius = magRadius,
			MagnetPull = magPull * (late <= 0 ? 1f : Progression.TraitMul( late )),
			CatchBonus = magnet <= 0 ? 0f : t.Magnetic.CatchBonus * Progression.TraitMul( magnet ),
			FreezeDuration = freeze <= 0 ? 0f : t.Freeze.Duration * Progression.TraitMul( freeze ),
			FreezeScale = freeze <= 0 ? 1f : MathF.Max( t.Freeze.ScaleFloor, t.Freeze.Scale / Progression.TraitMul( freeze ) ),
			SpeedScale = heavy <= 0 ? 1f : t.Heavy.Speed.At( heavy ),
			AccelMul = TraitLevel( RoundTrait.Accel ) <= 0 ? 1f : t.Accel.At( TraitLevel( RoundTrait.Accel ) ),
			AccelCap = t.AccelCap,
			ExplosiveRadius = t.Explosive.At( TraitLevel( RoundTrait.Explosive ) ),
			ElectricJumps = TraitLevel( RoundTrait.Electric ),
			BloodThreshold = BloodThreshold,
			HomingLead = t.Homing.At( TraitLevel( RoundTrait.Homing ) ),
			SkimDegrees = t.Skim.Degrees.At( skim ),
			SkimBoost = skim <= 0 ? 1f : t.Skim.Boost.At( skim ),
			KickExtra = t.Kick.Extra.At( TraitLevel( RoundTrait.Kick ) ),
			KickSecond = TraitLevel( RoundTrait.Kick ) >= max,
			StickTime = stickTime,
			StickSpeed = stick <= 0 ? 1f : t.Stick.Speed.At( stick ),
			CushionDegrees = t.Cushion.At( TraitLevel( RoundTrait.Cushion ) ),
			CueDegrees = TraitLevel( RoundTrait.Cue ) <= 0 ? 0f : t.Cue.Degrees.At( TraitLevel( RoundTrait.Cue ) ),
			CornerRadius = t.Corner.Radius.At( TraitLevel( RoundTrait.Corner ) ),
			CornerPull = t.Corner.Pull.At( TraitLevel( RoundTrait.Corner ) ),
			IncurveRate = MathX.DegreeToRadian( curveIn ),
			ClockwiseRate = MathX.DegreeToRadian( curveClock ),
			StutterTime = t.Stutter.At( stutter ),
			BreachCharges = TraitLevel( RoundTrait.Breach ),
			BoomerangKeep = t.Boomerang.At( TraitLevel( RoundTrait.Boomerang ) ),
			FuseRadius = t.Fuse.At( TraitLevel( RoundTrait.Fuse ) ),
			LateMag = late > 0,
			SecondWindKeep = t.SecondWind.At( TraitLevel( RoundTrait.SecondWind ) ),
			ShredCharges = TraitLevel( RoundTrait.Shred ),
			HookPush = t.Hook.At( TraitLevel( RoundTrait.Hook ) ),
			MarkTurn = MathX.DegreeToRadian( t.Mark.Degrees.At( TraitLevel( RoundTrait.Mark ) ) ),
			MarkBonus = TraitLevel( RoundTrait.Mark ) >= max ? t.Mark.BonusAtMax : 0,
			PinballKeep = pinball <= 0 ? t.PinballNaturalKeep : t.Pinball.Keep.At( pinball ),
			PinballFree = pinball >= max,
			HasPinball = pinball > 0,
			RibbonWidth = t.Ribbon.Width.At( ribbon ),
			RibbonSlow = ribbon <= 0 ? 0f : t.Ribbon.Slow.At( ribbon ),
			GrazePad = t.Graze.At( TraitLevel( RoundTrait.Graze ) ),
			RehitCharges = TraitLevel( RoundTrait.Rehit ),
			StepDistance = t.Step.At( TraitLevel( RoundTrait.Step ) ),
			EchoKeep = t.Echo.Keep.At( echo ),
			EchoFreeze = echo >= max,
			RedirectCone = MathX.DegreeToRadian( t.Redirect.Cone.At( TraitLevel( RoundTrait.Redirect ) ) ),
			RedirectRange = t.Redirect.Range.At( TraitLevel( RoundTrait.Redirect ) ),
			HeavyPush = heavy <= 0 ? 0f : t.Heavy.Push * Progression.TraitMul( heavy )
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
	DecideRing,
	PickTrait,
	Dead,
	Extracted,
	Won,
	City
}
