namespace LoopedLoaded;

static class FxBudget
{
	public static int GibAliveCap => GraphicsProfile.GibAliveCap;
	public static int GibFrameCap => GraphicsProfile.GibFrameCap;
	public static int FlashCap => GraphicsProfile.FlashCap;
	public static int BoneAliveCap => GraphicsProfile.BoneAliveCap;
	public static int BoneFrameCap => GraphicsProfile.BoneFrameCap;

	public static int Frame { get; private set; }
	public static int GibsThisFrame { get; private set; }
	public static int BonesThisFrame { get; private set; }

	static float seen = float.NaN;
	static int flashes;
	static bool exploded;
	static bool blasted;

	public static void Touch()
	{
		if ( seen == Time.Now )
			return;

		seen = Time.Now;
		Frame++;
		GibsThisFrame = 0;
		BonesThisFrame = 0;
		flashes = 0;
		exploded = false;
		blasted = false;
	}

	public static void NoteGib()
	{
		Touch();
		GibsThisFrame++;
	}

	public static int GibRoom( int alive )
	{
		Touch();
		return Math.Max( 0, Math.Min( GibAliveCap - alive, GibFrameCap - GibsThisFrame ) );
	}

	public static void NoteBone()
	{
		Touch();
		BonesThisFrame++;
	}

	public static int BoneRoom( int alive )
	{
		Touch();
		return Math.Max( 0, Math.Min( BoneAliveCap - alive, BoneFrameCap - BonesThisFrame ) );
	}

	public static bool AllowFlash()
	{
		Touch();
		if ( flashes >= FlashCap )
			return false;

		flashes++;
		return true;
	}

	public static bool AllowExplode()
	{
		Touch();
		if ( exploded )
			return false;

		exploded = true;
		return true;
	}

	public static bool AllowBlast()
	{
		Touch();
		if ( blasted )
			return false;

		blasted = true;
		return true;
	}
}
