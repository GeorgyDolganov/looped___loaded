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
	[Property] public int FinalLap { get; set; } = 8;

	public ArenaGeometry Geometry => Arena.Geometry;
	public List<Enemy> Enemies { get; } = new();
	public List<EnemyShot> Shots { get; } = new();

	public RunPhase Phase { get; private set; } = RunPhase.Playing;
	public bool IsFrozen => Phase != RunPhase.Playing;

	public string Notice { get; private set; } = "AIM. FIRE. CATCH IT BACK.";
	public float NoticeAge => Time.Now - noticeAt;
	public bool NoticeVisible => NoticeAge < NoticeDuration;

	public int Lap => Runner.IsValid() ? Math.Clamp( Runner.Lap, 1, FinalLap ) : 1;
	public float LapFraction => Runner.IsValid() && Runner.Lap <= FinalLap ? Runner.LapFraction : 1f;
	public int Health { get; private set; }
	public int Kills { get; private set; }
	public int ShotsFired { get; private set; }
	public int Catches { get; private set; }
	public int Losses { get; private set; }
	public float RunTime => Time.Now - runStartedAt;

	public RoundTrait OfferA { get; private set; }
	public RoundTrait OfferB { get; private set; }
	public RoundTrait PendingTrait { get; private set; }

	float noticeAt = -99f;
	float runStartedAt;
	float invulnUntil;
	int lastLap = 1;

	public void Announce( string text )
	{
		Notice = text;
		noticeAt = Time.Now;
	}

	public void Restart()
	{
		ClearCombat();
		Inventory.ResetLoadout();
		Runner.ResetToStart( MathF.PI * 0.5f );

		Phase = RunPhase.Playing;
		Health = MaxHealth;
		Kills = 0;
		ShotsFired = 0;
		Catches = 0;
		Losses = 0;
		lastLap = 1;
		invulnUntil = 0f;
		runStartedAt = Time.Now;

		SpawnWave( 1 );
		Announce( "ONE LAP. ONE ROUND. BRING IT BACK." );
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
			Restart();
			return;
		}

		if ( Phase == RunPhase.PickTrait )
		{
			if ( Input.Pressed( "Slot1" ) ) PickTrait( OfferA );
			if ( Input.Pressed( "Slot2" ) ) PickTrait( OfferB );
			return;
		}

		if ( Phase == RunPhase.PickRound )
		{
			if ( Input.Pressed( "Slot1" ) ) AssignTrait( 0 );
			if ( Input.Pressed( "Slot2" ) ) AssignTrait( 1 );
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
		if ( Input.Pressed( "Slot1" ) )
			Inventory.TrySelect( 0 );

		if ( Input.Pressed( "Slot2" ) )
			Inventory.TrySelect( 1 );

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
		projectile.Launch( this, slot.BuildFlight(), Aim.Muzzle, Aim.Direction );

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
		invulnUntil = Time.Now + 1.05f;

		Sound.Play( "sounds/impacts/melee/impact-melee-flesh.sound", Runner.WorldPosition );
		ImpactFlash.Spawn( Scene, Geometry.ToPlayWorld( Runner.Flat ), new Color( 1f, 0.3f, 0.2f ), 1.6f );

		if ( Health > 0 )
		{
			Announce( "HIT" );
			return;
		}

		Phase = RunPhase.Dead;
		Announce( "RUN OVER" );
	}

	void CheckLap()
	{
		var lap = Runner.Lap;
		if ( lap == lastLap )
			return;

		lastLap = lap;

		if ( lap > FinalLap )
		{
			Win();
			return;
		}

		Sound.Play( "sounds/kenney/ui/ui.popup.message.open.sound" );
		Announce( $"LAP {lap}" );
		OnLapEntered( lap );
	}

	void OnLapEntered( int lap )
	{
		SpawnWave( lap );

		switch ( lap )
		{
			case 2:
				BeginTraitPick();
				break;
			case 3:
				GrantSecondRound();
				break;
			case 4:
				UnlockSlow();
				break;
			case 5:
			case 7:
				BeginTraitPick();
				break;
		}
	}

	void GrantSecondRound()
	{
		var slot = Inventory.GrantSlot();
		if ( slot is null )
			return;

		Inventory.TrySelect( slot.Index );
		Announce( "SECOND ROUND CHAMBERED" );
		Sound.Play( "sounds/kenney/ui/ui.favourite.sound" );
	}

	void UnlockSlow()
	{
		Runner.SlowUnlocked = true;
		Announce( "SLOW UNLOCKED  ·  RMB" );
		Sound.Play( "sounds/kenney/ui/ui.popup.message.open.sound" );
	}

	void BeginTraitPick()
	{
		var pool = RoundTraits.All.ToList();
		OfferA = pool[Game.Random.Int( 0, pool.Count - 1 )];
		pool.Remove( OfferA );
		OfferB = pool[Game.Random.Int( 0, pool.Count - 1 )];
		Phase = RunPhase.PickTrait;
		Announce( "INSTALL A TRAIT" );
	}

	void PickTrait( RoundTrait trait )
	{
		PendingTrait = trait;

		if ( Inventory.Slots.Count == 1 )
		{
			AssignTrait( 0 );
			return;
		}

		Phase = RunPhase.PickRound;
		Announce( $"ASSIGN {RoundTraits.Title( trait )}" );
	}

	void AssignTrait( int index )
	{
		if ( index < 0 || index >= Inventory.Slots.Count )
			return;

		var slot = Inventory.Slots[index];
		slot.Install( PendingTrait );
		Phase = RunPhase.Playing;

		Sound.Play( "sounds/kenney/ui/ui.button.press.sound" );
		Announce( $"{RoundTraits.Title( PendingTrait )} LV{slot.TraitLevel( PendingTrait )} · ROUND {slot.Index + 1}" );
	}

	void Win()
	{
		Phase = RunPhase.Won;
		Announce( "NO MORE ROUNDS" );
		Sound.Play( "sounds/kenney/ui/ui.upvote.sound" );
	}

	void SpawnWave( int lap )
	{
		ClearEnemies();

		var inner = Geometry.CoreRadius + 190f;
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
				Add( EnemyKind.Chaser, offset, inner, 1 );
				break;
			case 2:
				Add( EnemyKind.Chaser, offset - 0.7f, inner, 1 );
				Add( EnemyKind.Chaser, offset + 0.7f, inner + 40f, 1 );
				break;
			case 3:
				Add( EnemyKind.Shield, offset, mid, 2 );
				Add( EnemyKind.Chaser, offset + 1.6f, inner, 2 );
				break;
			case 4:
				Add( EnemyKind.Shield, offset - 0.5f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 1.8f, mid, 2 );
				break;
			case 5:
				Add( EnemyKind.Chaser, offset - 1.1f, inner, 2 );
				Add( EnemyKind.Chaser, offset + 0.4f, inner, 2 );
				Add( EnemyKind.Shooter, offset + 2.2f, mid, 2 );
				break;
			case 6:
				Add( EnemyKind.Chaser, offset, inner, 2 );
				Add( EnemyKind.Shield, offset + 2.1f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 4.0f, mid, 2 );
				break;
			case 7:
				Add( EnemyKind.Chaser, offset - 0.8f, inner, 2 );
				Add( EnemyKind.Chaser, offset + 0.8f, outer - 40f, 2 );
				Add( EnemyKind.Shield, offset + 2.4f, mid, 2 );
				Add( EnemyKind.Shooter, offset + 4.2f, mid, 2 );
				break;
			default:
				Add( EnemyKind.Chaser, offset, inner, 2 );
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
