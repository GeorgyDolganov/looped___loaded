namespace LoopedLoaded;

public sealed class GameLoop : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public RoundInventory Inventory { get; set; }
	[Property] public float LostRoundMinArc { get; set; } = 460f;
	[Property] public float NoticeDuration { get; set; } = 1.6f;
	[Property] public int MaxHealth { get; set; } = 3;

	public CityBoard City { get; set; }
	public bool InCity => Phase == RunPhase.City;
	public ArenaGeometry Geometry => Arena.Geometry;
	public List<Enemy> Enemies { get; } = new();
	public List<EnemyShot> Shots { get; } = new();

	public RunPhase Phase { get; private set; } = RunPhase.Playing;
	public bool IsFrozen => Phase != RunPhase.Playing;

	public string Notice { get; private set; } = "AIM. FIRE. CATCH IT BACK.";
	public float NoticeAge => Time.Now - noticeAt;
	public bool NoticeVisible => NoticeAge < NoticeDuration;

	public int Lap { get; private set; } = 1;
	public float LapFraction => Phase == RunPhase.DecideLap ? 1f : Runner.IsValid() ? Runner.LapFraction : 0f;
	public int Stash => Inventory.IsValid() ? Inventory.Slots.Count : 0;
	public int HeartMax => MaxHealth + (City.IsValid() ? City.Stats().BonusHealth : 0);
	public int Health { get; private set; }
	public float HurtAmount => Math.Clamp( 1f - (Time.Now - lastHurtAt) / 0.55f, 0f, 1f );
	public bool Invulnerable => Time.Now < invulnUntil;
	public int Kills { get; private set; }
	public int ShotsFired { get; private set; }
	public int Catches { get; private set; }
	public int Losses { get; private set; }
	public int ExtractedRounds { get; private set; }
	public int BurnedRounds { get; private set; }
	public int BestExtract { get; private set; }
	public float RunTime => Time.Now - runStartedAt;

	public RoundTrait OfferA { get; private set; }
	public RoundTrait OfferB { get; private set; }
	public RoundTrait OfferC { get; private set; }
	public bool HasThirdOffer { get; private set; }

	float noticeAt = -99f;
	float runStartedAt;
	float invulnUntil;
	float lastHurtAt = -99f;

	public void Announce( string text )
	{
		Notice = text;
		noticeAt = Time.Now;
	}

	public void Restart()
	{
		if ( Runner.IsValid() )
			Runner.GameObject.Enabled = true;

		City?.ClearShots();
		ClearCombat();
		Inventory.ResetLoadout();
		Runner.ResetToStart( MathF.PI * 0.5f );

		Phase = RunPhase.Playing;
		Health = HeartMax;
		if ( City.IsValid() )
		{
			var stats = City.Stats();
			Runner.ApplyCity( stats );
			Inventory.Loadout.BonusDamage = stats.BonusDamage;
			City.SetVisible( false );
		}
		Kills = 0;
		ShotsFired = 0;
		Catches = 0;
		Losses = 0;
		Lap = 1;
		ExtractedRounds = 0;
		BurnedRounds = 0;
		invulnUntil = 0f;
		lastHurtAt = -99f;
		runStartedAt = Time.Now;

		SpawnWave( 1 );
		Announce( "ONE LAP. ONE ROUND. CASH OUT OR GO AGAIN." );
	}

	public void RegisterKill()
	{
		Kills++;
		Announce( "TARGET DOWN" );
	}

	public void CatchRound( RoundProjectile projectile )
	{
		var slot = SlotOf( projectile.SlotIndex );
		if ( slot is null )
			return;

		var world = Geometry.ToPlayWorld( projectile.Flat );
		var chained = projectile.TargetsHit;

		slot.ResetCombat();
		slot.Status = RoundStatus.Chambered;
		Inventory.TrySelect( slot.Index );
		Catches++;

		Sound.Play( "sounds/impacts/melee/impact-melee-metal.sound", world );
		ImpactFlash.Spawn( Scene, world, slot.Tint, 1.4f );

		Announce( chained > 0 ? $"ROUND {slot.Index + 1} CHAMBERED  +{chained}" : $"ROUND {slot.Index + 1} CHAMBERED" );
	}

	public void LoseRound( RoundProjectile projectile )
	{
		var slot = SlotOf( projectile.SlotIndex );
		if ( slot is null )
			return;

		var resting = SnapToTrack( projectile.Flat );
		projectile.GameObject.Destroy();

		var go = Scene.CreateObject();
		go.Name = $"Dropped Round {slot.Index + 1}";

		var dropped = go.AddComponent<DroppedRound>();
		dropped.Tint = slot.Tint;
		dropped.Place( Geometry, resting, slot.Index );

		slot.Flying = null;
		slot.Lost = dropped;
		slot.Status = RoundStatus.Dropped;
		Losses++;

		Sound.Play( "sounds/kenney/ui/ui.navigate.deny.sound" );
		Announce( $"ROUND {slot.Index + 1} LOST" );
	}

	protected override void OnStart()
	{
		runStartedAt = Time.Now;
		Mouse.Visibility = MouseVisibility.Visible;
		Mouse.CursorType = "crosshair";
	}

	protected override void OnUpdate()
	{
		if ( !Arena.IsValid() || !Runner.IsValid() || !Inventory.IsValid() )
			return;

		if ( Input.Pressed( "Reload" ) )
		{
			if ( Phase == RunPhase.City || Phase == RunPhase.Playing || Phase == RunPhase.DecideLap || Phase == RunPhase.PickTrait )
				Restart();
			else
				EnterCity( false );
			return;
		}

		if ( Phase == RunPhase.City )
		{
			if ( Input.Pressed( "Jump" ) )
			{
				Restart();
				return;
			}

			City?.Tick();
			return;
		}

		if ( Phase == RunPhase.DecideLap )
		{
			if ( Input.Pressed( "Slot1" ) ) Extract();
			if ( Input.Pressed( "Slot2" ) ) ContinueRun();
			return;
		}

		if ( Phase == RunPhase.PickTrait )
		{
			if ( Input.Pressed( "Slot1" ) ) InstallOffer( OfferA );
			if ( Input.Pressed( "Slot2" ) ) InstallOffer( OfferB );
			if ( HasThirdOffer && Input.Pressed( "Slot3" ) ) InstallOffer( OfferC );
			return;
		}

		if ( Phase != RunPhase.Playing )
			return;

		HandleSelect();

		if ( Input.Pressed( "Jump" ) && Runner.TryDash() )
			Sound.Play( "sounds/footsteps/footstep-concrete-jump.sound", Runner.WorldPosition );

		if ( Input.Pressed( "Attack1" ) )
			Fire();

		CheckPickup();
		CheckHits();
		CheckLap();
	}

	void HandleSelect()
	{
		for ( var i = 0; i < 9; i++ )
		{
			if ( Input.Pressed( $"Slot{i + 1}" ) )
				Inventory.TrySelect( i );
		}

		if ( Input.Pressed( "SlotPrev" ) )
			Inventory.SelectNextChambered( -1 );

		if ( Input.Pressed( "SlotNext" ) )
			Inventory.SelectNextChambered( 1 );

		var wheel = Input.MouseWheel;
		if ( wheel.y > 0.1f )
			Inventory.SelectNextChambered( -1 );
		else if ( wheel.y < -0.1f )
			Inventory.SelectNextChambered( 1 );
	}

	void Fire()
	{
		var slot = Inventory.Selected;

		if ( slot is null || slot.Status != RoundStatus.Chambered )
		{
			Sound.Play( "sounds/kenney/ui/ui.button.deny.sound" );
			Announce( ChamberDeny() );
			return;
		}

		var go = Scene.CreateObject();
		go.Name = $"Round {slot.Index + 1}";

		var projectile = go.AddComponent<RoundProjectile>();
		projectile.Launch( this, Inventory.Loadout.BuildFlight( slot ), Aim.Muzzle, Aim.Direction );

		slot.Flying = projectile;
		slot.Lost = null;
		slot.Status = RoundStatus.InFlight;
		Inventory.AfterFired( slot );
		ShotsFired++;

		Sound.Play( "sounds/effects/explosion/explosion_small.sound", Aim.MuzzleWorld );
		ImpactFlash.Spawn( Scene, Aim.MuzzleWorld, slot.Tint, 0.8f );
	}

	string ChamberDeny()
	{
		if ( Inventory.Slots.Any( s => s.Status == RoundStatus.Chambered ) )
			return "SELECT A CHAMBERED ROUND";

		if ( Inventory.Slots.Any( s => s.Status == RoundStatus.InFlight ) )
			return "ROUNDS STILL IN FLIGHT";

		return "ROUNDS LOST ON THE RING";
	}

	void CheckPickup()
	{
		foreach ( var slot in Inventory.Slots )
		{
			if ( slot.Status != RoundStatus.Dropped || !slot.Lost.IsValid() )
				continue;

			if ( (Runner.Flat - slot.Lost.Flat).Length > Inventory.PickupRadius )
				continue;

			var world = Geometry.ToPlayWorld( slot.Lost.Flat );
			slot.ResetCombat();
			slot.Status = RoundStatus.Chambered;
			Inventory.TrySelect( slot.Index );

			Sound.Play( "sounds/kenney/ui/ui.favourite.sound", world );
			ImpactFlash.Spawn( Scene, world, slot.Tint, 1.2f );
			Announce( $"ROUND {slot.Index + 1} RECOVERED" );
		}
	}

	void CheckHits()
	{
		if ( Time.Now < invulnUntil || Runner.Dashing )
			return;

		var reach = Runner.PlayerRadius;

		foreach ( var enemy in Enemies )
		{
			if ( !enemy.IsValid() || !enemy.Alive )
				continue;

			if ( (enemy.Flat - Runner.Flat).Length <= enemy.Radius + reach )
			{
				Hurt();
				return;
			}
		}

		foreach ( var shot in Shots.ToArray() )
		{
			if ( !shot.IsValid() )
				continue;

			if ( (shot.Flat - Runner.Flat).Length <= shot.Radius + reach )
			{
				shot.GameObject.Destroy();
				Hurt();
				return;
			}
		}
	}

	void Hurt()
	{
		Health--;
		lastHurtAt = Time.Now;
		invulnUntil = Time.Now + 1.05f;

		var world = Geometry.ToPlayWorld( Runner.Flat );
		Sound.Play( "sounds/impacts/melee/impact-melee-flesh.sound", Runner.WorldPosition );
		Sound.Play( "sounds/kenney/ui/ui.navigate.deny.sound" );
		ImpactFlash.Spawn( Scene, world, new Color( 1f, 0.12f, 0.08f ), 3.4f );
		ImpactFlash.Spawn( Scene, world + Vector3.Up * 40f, Color.White, 1.8f );

		if ( Health > 0 )
		{
			Announce( $"-1  ·  {Health} LEFT" );
			return;
		}

		Phase = RunPhase.Dead;
		BurnedRounds = Stash;
		Announce( "RUN OVER" );
	}

	void CheckLap()
	{
		if ( !Runner.IsValid() || Runner.Lap <= Lap )
			return;

		Phase = RunPhase.DecideLap;
		Sound.Play( "sounds/kenney/ui/ui.popup.message.open.sound" );
		Announce( $"LAP {Lap} CLEAR" );
	}

	void Extract()
	{
		ExtractedRounds = Stash;
		if ( ExtractedRounds > BestExtract )
			BestExtract = ExtractedRounds;

		EnterCity( true );
	}

	void EnterCity( bool deposit )
	{
		if ( deposit && City.IsValid() )
			City.Deposit( Stash );

		foreach ( var slot in Inventory.Slots )
			slot.ResetCombat();

		ClearCombat();
		Phase = RunPhase.City;
		City?.EnsureBuilt();
		City?.SetVisible( true );

		if ( Runner.IsValid() )
			Runner.GameObject.Enabled = false;
		Sound.Play( "sounds/kenney/ui/ui.upvote.sound" );
		Announce( deposit ? $"CITY  ·  +{ExtractedRounds} WAREHOUSE" : "CITY" );
	}

	void ContinueRun()
	{
		Lap = Runner.Lap;
		var granted = Inventory.GrantSlot();
		if ( granted is not null )
			Inventory.TrySelect( granted.Index );
		BeginTraitPick();
		Sound.Play( "sounds/kenney/ui/ui.favourite.sound" );
		Announce( $"+1 ROUND  ·  STASH {Stash}" );
	}

	void BeginTraitPick()
	{
		var pool = RoundTraits.All.ToList();
		var count = City.IsValid() ? City.Stats().OfferCount : 2;
		count = Math.Clamp( count, 2, 3 );

		OfferA = TakeTrait( pool );
		OfferB = TakeTrait( pool );
		HasThirdOffer = count >= 3;
		if ( HasThirdOffer )
			OfferC = TakeTrait( pool );

		Phase = RunPhase.PickTrait;
	}

	static RoundTrait TakeTrait( List<RoundTrait> pool )
	{
		var pick = pool[Game.Random.Int( 0, pool.Count - 1 )];
		pool.Remove( pick );
		return pick;
	}

	void InstallOffer( RoundTrait trait )
	{
		Inventory.Loadout.Install( trait );
		SpawnWave( Lap );
		Phase = RunPhase.Playing;

		Sound.Play( "sounds/kenney/ui/ui.button.press.sound" );
		Announce( $"{RoundTraits.Title( trait )} LV{Inventory.Loadout.TraitLevel( trait )}  ·  ALL ROUNDS" );
	}

	void SpawnWave( int lap )
	{
		ClearEnemies();

		var inner = Geometry.CoreRadius + 190f;
		var hunt = MathX.Lerp( inner, Geometry.TrackInner - 110f, 0.32f );
		var mid = MathX.Lerp( inner, Geometry.TrackInner - 140f, 0.45f );
		var outer = Geometry.TrackInner - 90f;
		var offset = Runner.Angle + MathF.PI;

		void Add( EnemyKind kind, float angle, float radius, int hp )
		{
			var go = Scene.CreateObject();
			go.Name = kind.ToString();

			var enemy = go.AddComponent<Enemy>();
			enemy.Arena = Arena;
			enemy.Loop = this;
			enemy.Setup( kind, ArenaGeometry.FromAngle( angle ) * radius, hp );
			Enemies.Add( enemy );
		}

		switch ( lap )
		{
			case 1:
				Add( EnemyKind.Chaser, offset, hunt, 1 );
				break;
			case 2:
				Add( EnemyKind.Chaser, offset - 0.7f, hunt, 1 );
				Add( EnemyKind.Chaser, offset + 0.7f, hunt + 40f, 1 );
				break;
			case 3:
				Add( EnemyKind.Shield, offset, mid, 2 );
				Add( EnemyKind.Chaser, offset + 1.6f, hunt, 2 );
				Add( EnemyKind.Chaser, offset - 1.4f, inner, 2 );
				break;
			case 4:
				Add( EnemyKind.Shield, offset - 0.5f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 1.8f, mid, 2 );
				Add( EnemyKind.Chaser, offset + 2.8f, hunt, 2 );
				break;
			case 5:
				Add( EnemyKind.Chaser, offset - 1.1f, hunt, 2 );
				Add( EnemyKind.Chaser, offset + 0.4f, hunt, 2 );
				Add( EnemyKind.Shooter, offset + 2.2f, mid, 2 );
				Add( EnemyKind.Chaser, offset + 3.4f, inner, 2 );
				break;
			case 6:
				Add( EnemyKind.Chaser, offset, hunt, 2 );
				Add( EnemyKind.Shield, offset + 2.1f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 4.0f, mid, 2 );
				Add( EnemyKind.Shooter, offset - 2.2f, mid, 2 );
				break;
			case 7:
				Add( EnemyKind.Chaser, offset - 0.8f, hunt, 2 );
				Add( EnemyKind.Chaser, offset + 0.8f, outer - 40f, 2 );
				Add( EnemyKind.Shield, offset + 2.4f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 4.2f, mid, 2 );
				break;
			default:
				Add( EnemyKind.Chaser, offset, hunt, 2 );
				Add( EnemyKind.Chaser, offset + 3.1f, hunt, 2 );
				Add( EnemyKind.Shield, offset + 1.5f, mid, 2 );
				Add( EnemyKind.Shield, offset + 3.6f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 2.5f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 5.0f, mid, 2 );
				break;
		}
	}

	void ClearCombat()
	{
		ClearEnemies();

		foreach ( var shot in Shots.ToArray() )
		{
			if ( shot.IsValid() )
				shot.GameObject.Destroy();
		}

		Shots.Clear();
	}

	void ClearEnemies()
	{
		foreach ( var enemy in Enemies )
		{
			if ( enemy.IsValid() )
				enemy.GameObject.Destroy();
		}

		Enemies.Clear();
	}

	RoundSlot SlotOf( int index )
	{
		if ( index < 0 || index >= Inventory.Slots.Count )
			return null;

		return Inventory.Slots[index];
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
