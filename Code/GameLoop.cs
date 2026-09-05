namespace LoopedLoaded;

public sealed class GameLoop : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public RoundInventory Inventory { get; set; }
	[Property] public float LostRoundMinArc { get; set; } = 460f;
	[Property] public float NoticeDuration { get; set; } = 1.6f;

	public ArenaGeometry Geometry => Arena.Geometry;
	public List<DummyTarget> Targets { get; } = new();

	public string Notice { get; private set; } = "AIM. FIRE. CATCH IT BACK.";
	public float NoticeAge => Time.Now - noticeAt;
	public bool NoticeVisible => NoticeAge < NoticeDuration;

	public int Lap => Runner.IsValid() ? Runner.Lap : 1;
	public float LapFraction => Runner.IsValid() ? Runner.LapFraction : 0f;
	public int Kills { get; private set; }
	public int Shots { get; private set; }
	public int Catches { get; private set; }
	public int Losses { get; private set; }
	public float RunTime => Time.Now - runStartedAt;

	public float LostRoundArc
	{
		get
		{
			if ( Inventory.Status != RoundStatus.Dropped || !Inventory.Lost.IsValid() )
				return 0f;

			var angle = ArenaGeometry.ToAngle( Inventory.Lost.Flat );
			return Wrap( Runner.Angle - angle ) * Geometry.TrackRadius;
		}
	}

	float noticeAt = -99f;
	float runStartedAt;
	int lastLap = 1;

	public void Announce( string text )
	{
		Notice = text;
		noticeAt = Time.Now;
	}

	public void Restart()
	{
		Inventory.Chamber();
		Runner.ResetToStart( MathF.PI * 0.5f );

		foreach ( var target in Targets )
			target.PlaceRandom();

		Kills = 0;
		Shots = 0;
		Catches = 0;
		Losses = 0;
		lastLap = 1;
		runStartedAt = Time.Now;

		Announce( "RUN RESET" );
	}

	public void RegisterKill()
	{
		Kills++;
		Announce( "TARGET DOWN" );
	}

	public void CatchRound( RoundProjectile projectile )
	{
		var world = Geometry.ToPlayWorld( projectile.Flat );
		var chained = projectile.TargetsHit;

		Inventory.Chamber();
		Catches++;

		Sound.Play( "sounds/impacts/melee/impact-melee-metal.sound", world );
		ImpactFlash.Spawn( Scene, world, Inventory.RoundTint, 1.4f );

		Announce( chained > 0 ? $"ROUND CHAMBERED  +{chained}" : "ROUND CHAMBERED" );
	}

	public void LoseRound( RoundProjectile projectile )
	{
		var resting = SnapToTrack( projectile.Flat );
		projectile.GameObject.Destroy();

		var go = Scene.CreateObject();
		go.Name = "Dropped Round";

		var dropped = go.AddComponent<DroppedRound>();
		dropped.Tint = Inventory.RoundTint;
		dropped.Place( Geometry, resting );

		Inventory.SetLost( dropped );
		Losses++;

		Sound.Play( "sounds/kenney/ui/ui.navigate.deny.sound" );
		Announce( "ROUND LOST" );
	}

	protected override void OnStart()
	{
		runStartedAt = Time.Now;
		Mouse.Visibility = MouseVisibility.Visible;
		Mouse.CursorType = "crosshair";
		Announce( "ONE LAP. ONE ROUND. BRING IT BACK." );
	}

	protected override void OnUpdate()
	{
		if ( !Arena.IsValid() || !Runner.IsValid() || !Inventory.IsValid() )
			return;

		if ( Input.Pressed( "Reload" ) )
		{
			Restart();
			return;
		}

		if ( Input.Pressed( "Jump" ) && Runner.TryDash() )
			Sound.Play( "sounds/footsteps/footstep-concrete-jump.sound", Runner.WorldPosition );

		if ( Input.Pressed( "Attack1" ) )
			Fire();

		CheckPickup();
		CheckLap();
	}

	void Fire()
	{
		if ( Inventory.Status != RoundStatus.Chambered )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			Announce( Inventory.Status == RoundStatus.InFlight ? "ROUND STILL IN FLIGHT" : "ROUND LOST ON THE RING" );
			return;
		}

		var go = Scene.CreateObject();
		go.Name = "Round";

		var projectile = go.AddComponent<RoundProjectile>();
		projectile.Tint = Inventory.RoundTint;
		projectile.Launch( this, Aim.Muzzle, Aim.Direction );

		Inventory.SetFlying( projectile );
		Shots++;

		Sound.Play( "sounds/effects/explosion/explosion_small.sound", Aim.MuzzleWorld );
		ImpactFlash.Spawn( Scene, Aim.MuzzleWorld, Inventory.RoundTint, 0.8f );
	}

	void CheckPickup()
	{
		if ( Inventory.Status != RoundStatus.Dropped || !Inventory.Lost.IsValid() )
			return;

		if ( (Runner.Flat - Inventory.Lost.Flat).Length > Inventory.PickupRadius )
			return;

		var world = Geometry.ToPlayWorld( Inventory.Lost.Flat );

		Inventory.Chamber();

		Sound.Play( "sounds/kenney/ui/ui.favourite.sound", world );
		ImpactFlash.Spawn( Scene, world, Inventory.RoundTint, 1.2f );
		Announce( "ROUND RECOVERED" );
	}

	void CheckLap()
	{
		if ( Runner.Lap == lastLap )
			return;

		lastLap = Runner.Lap;
		Sound.Play( "sounds/kenney/ui/ui.popup.message.open.sound" );
		Announce( $"LAP {lastLap}" );
	}

	Vector2 SnapToTrack( Vector2 flat )
	{
		var radius = Geometry.TrackRadius;
		var angle = flat.Length < 1f ? Runner.Angle : ArenaGeometry.ToAngle( flat );
		var ahead = Wrap( Runner.Angle - angle ) * radius;

		if ( ahead < LostRoundMinArc )
			angle = Runner.Angle - LostRoundMinArc / radius;

		return ArenaGeometry.FromAngle( angle ) * radius;
	}

	static float Wrap( float radians )
	{
		radians %= MathF.Tau;
		return radians < 0f ? radians + MathF.Tau : radians;
	}
}
