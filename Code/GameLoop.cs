namespace LoopedLoaded;

public sealed class GameLoop : Component
{
	[Property] public ArenaBuilder Arena { get; set; }
	[Property] public RingRunner Runner { get; set; }
	[Property] public PlayerAim Aim { get; set; }
	[Property] public RoundInventory Inventory { get; set; }
	[Property] public CityBoard City { get; set; }
	[Property] public float LostRoundMinArc { get; set; } = 460f;
	[Property] public float NoticeDuration { get; set; } = 1.6f;
	[Property] public int MaxHealth { get; set; } = 3;
	public RunPhase Phase { get; private set; } = RunPhase.Menu;
	public bool InCity => Phase == RunPhase.City;
	public bool InMenu => Phase == RunPhase.Menu;
	public bool WantsUiCursor => InMenu || Paused || Phase == RunPhase.DecideLap || Phase == RunPhase.PickTrait || Phase == RunPhase.Dead || Phase == RunPhase.Extracted;
	public bool CanPause => !InMenu && !Paused && (Phase == RunPhase.Playing || Phase == RunPhase.City || Phase == RunPhase.DecideLap || Phase == RunPhase.PickTrait);
	public bool BlocksShot => Time.Now < uiClickUntil;
	public ArenaGeometry Geometry => Arena.Geometry;
	public List<Enemy> Enemies { get; } = new();
	public List<EnemyShot> Shots { get; } = new();

	public MenuPage MenuView { get; private set; } = MenuPage.Title;
	public int ActiveSlot { get; private set; }
	public int SaveCursor { get; private set; }
	readonly GameSave[] slotCache = new GameSave[SaveStore.Slots];
	bool savesLoaded;
	public bool Paused { get; private set; }
	public bool IsFrozen => Paused || Phase != RunPhase.Playing;

	public string Notice { get; private set; } = "AIM. FIRE. CATCH IT BACK.";
	public float NoticeAge => Time.Now - noticeAt;
	public bool NoticeVisible => NoticeAge < NoticeDuration;

	public int Lap { get; private set; } = 1;
	int layoutSeed;
	public float LapFraction => Phase == RunPhase.DecideLap ? 1f : Runner.IsValid() ? Runner.LapFraction : 0f;
	public int Stash => Inventory.IsValid() ? Inventory.Slots.Count : 0;
	public int HeartMax => MaxHealth + (City.IsValid() ? City.Stats().BonusHealth : 0);
	public int Health { get; private set; }
	public float HurtAmount => Math.Clamp( 1f - (Time.Now - lastHurtAt) / 0.55f, 0f, 1f );
	public bool Invulnerable => Time.Now < invulnUntil;
	public int Kills { get; private set; }
	public int Scrap { get; private set; }
	public int ShotsFired { get; private set; }
	public int Catches { get; private set; }
	public int Losses { get; private set; }
	public int BloodShields { get; private set; }
	public float SnapUntil { get; private set; }
	public float SnapBoost => Time.Now < SnapUntil && Inventory.IsValid() ? Inventory.Loadout.SnapSpeed : 1f;
	public int ExtractedRounds { get; private set; }
	public int BurnedRounds { get; private set; }
	public int BestExtract { get; private set; }
	public float RunTime => Time.Now - runStartedAt;

	public RoundTrait OfferA { get; private set; }
	public RoundTrait OfferB { get; private set; }
	public RoundTrait OfferC { get; private set; }
	public bool HasThirdOffer { get; private set; }
	public bool OfferABought { get; private set; }
	public bool OfferBBought { get; private set; }
	public bool OfferCBought { get; private set; }
	public int ShopBuyAllCost => Phase == RunPhase.PickTrait ? RemainingOfferCost() : 0;
	public bool ShopHasBundle => Phase == RunPhase.PickTrait && OpenOfferCount() >= 2;
	public bool ShopCanBuyAll => ShopHasBundle && Scrap >= ShopBuyAllCost && ShopBuyAllCost > 0;

	public float Threat => Progression.Threat( Lap );
	public bool InBossFight { get; private set; }
	public bool HasBossOffer => !InBossFight && Lap >= 5;
	public bool CanSkipLap
	{
		get
		{
			if ( Phase != RunPhase.Playing || InBossFight || Paused || !Runner.IsValid() )
				return false;

			foreach ( var enemy in Enemies )
			{
				if ( enemy.IsValid() && enemy.Alive )
					return false;
			}

			return true;
		}
	}
	public int BossHealth
	{
		get
		{
			foreach ( var enemy in Enemies )
			{
				if ( enemy.IsValid() && enemy.Alive && enemy.Kind == EnemyKind.Core )
					return enemy.Health;
			}

			return 0;
		}
	}
	public int BossMaxHealth
	{
		get
		{
			foreach ( var enemy in Enemies )
			{
				if ( enemy.IsValid() && enemy.Alive && enemy.Kind == EnemyKind.Core )
					return enemy.MaxHealth;
			}

			return Progression.BossHealth( Lap );
		}
	}

	float noticeAt = -99f;
	float runStartedAt;
	float invulnUntil;
	float lastHurtAt = -99f;
	float armorNoticeAt = -99f;
	float pauseStartedAt;
	float uiClickUntil;
	bool pendingBoss;
	bool bossWon;
	bool skipHinted;

	public string SlotBlurb( int index )
	{
		var save = SlotInfo( index );
		if ( save is null || !save.HasProgress )
			return "EMPTY";

		return $"WH {save.Warehouse}  ·  BEST {save.BestExtract}  ·  {save.BuildingCount} BUILDINGS";
	}

	public bool HasActiveSave => SlotInfo( ActiveSlot ) is not null && SlotInfo( ActiveSlot ).HasProgress;

	public bool SlotExists( int index ) => SaveStore.Exists( index );

	public GameSave SlotInfo( int index )
	{
		if ( index < 0 || index >= slotCache.Length )
			return null;

		return slotCache[index];
	}

	public void HighlightSave( int index )
	{
		SaveCursor = Math.Clamp( index, 0, SaveStore.Slots - 1 );
		ArenaSounds.MenuMove();
	}

	public void RestoreSaves()
	{
		if ( savesLoaded )
			return;

		savesLoaded = true;
		ActiveSlot = SaveStore.LastSlot();
		SaveCursor = ActiveSlot;
		RefreshSaves();
		ApplySave( SlotInfo( ActiveSlot ) );
	}

	public void Autosave()
	{
		if ( !City.IsValid() )
			return;

		var save = City.Capture( BestExtract );
		if ( !save.HasProgress && !SaveStore.Exists( ActiveSlot ) )
			return;

		if ( SaveStore.Write( ActiveSlot, save ) )
			slotCache[ActiveSlot] = save;
	}

	public void OpenSaves()
	{
		Autosave();
		RefreshSaves();
		SaveCursor = ActiveSlot;
		MenuView = MenuPage.Saves;
		ArenaSounds.MenuOk();
	}

	public void CloseSaves()
	{
		MenuView = MenuPage.Title;
		ArenaSounds.MenuOk();
	}

	public void UseSave( int index )
	{
		index = Math.Clamp( index, 0, SaveStore.Slots - 1 );
		Autosave();
		ActiveSlot = index;
		SaveStore.SetLastSlot( ActiveSlot );
		ApplySave( SaveStore.Read( ActiveSlot ) );
		RefreshSaves();
		MenuView = MenuPage.Title;
		ArenaSounds.MenuOk();
		Announce( $"SLOT {ActiveSlot + 1}" );
	}

	public void DeleteSave( int index )
	{
		index = Math.Clamp( index, 0, SaveStore.Slots - 1 );
		if ( !SaveStore.Exists( index ) )
		{
			ArenaSounds.Deny();
			return;
		}

		if ( !SaveStore.Delete( index ) )
		{
			ArenaSounds.Deny();
			return;
		}
		if ( index == ActiveSlot )
		{
			BestExtract = 0;
			City?.Wipe();
		}

		RefreshSaves();
		ArenaSounds.MenuBack();
		Announce( $"SLOT {index + 1} DELETED" );
	}

	void ApplySave( GameSave save )
	{
		if ( save is null || !save.HasProgress )
		{
			BestExtract = 0;
			City?.Wipe();
			return;
		}

		BestExtract = save.BestExtract;
		City?.Apply( save );
	}

	void RefreshSaves()
	{
		for ( var i = 0; i < SaveStore.Slots; i++ )
			slotCache[i] = SaveStore.Read( i );
	}

	public void Announce( string text )
	{
		Notice = text;
		noticeAt = Time.Now;
	}

	public void ShowMenu()
	{
		City?.ClearShots();
		ClearCombat();

		if ( Inventory.IsValid() )
		{
			foreach ( var slot in Inventory.Slots )
				slot.ResetCombat();
		}

		if ( Runner.IsValid() )
		{
			Runner.GameObject.Enabled = true;
			Runner.ResetToStart( Arena.IsValid() ? Arena.StartAngle : MathF.PI * 0.5f );
		}

		City?.SetVisible( false );
		Phase = RunPhase.Menu;
		pendingBoss = false;
		bossWon = false;
		InBossFight = false;
		lastHurtAt = -99f;
		invulnUntil = 0f;
		ClearPause();
		MenuView = MenuPage.Title;
		Autosave();
		if ( Arena.IsValid() )
			Arena.ClearGeneratedLayout();
		ArenaSounds.MenuOpen();
	}

	public void OpenCityFromMenu()
	{
		EnterCity( false );
	}

	public void QuitGame()
	{
		Autosave();
		Game.Close();
	}

	static bool PressedEscape()
	{
		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			return true;
		}

		return Input.Pressed( "Menu" );
	}

	void TickMenu()
	{
		Mouse.CursorType = "pointer";

		if ( MenuView == MenuPage.Saves )
		{
			TickSaves();
			return;
		}

		if ( Input.Pressed( "Jump" ) || Input.Pressed( "Slot1" ) )
		{
			Restart();
			return;
		}

		if ( Input.Pressed( "Slot2" ) )
		{
			OpenCityFromMenu();
			return;
		}

		if ( Input.Pressed( "Slot3" ) )
		{
			OpenSaves();
			return;
		}

		if ( PressedEscape() )
			QuitGame();
	}

	void TickSaves()
	{
		for ( var i = 0; i < SaveStore.Slots; i++ )
		{
			if ( !Input.Pressed( $"Slot{i + 1}" ) )
				continue;

			SaveCursor = i;
			ArenaSounds.MenuMove();
		}

		if ( Input.Pressed( "Jump" ) )
		{
			UseSave( SaveCursor );
			return;
		}

		if ( Input.Pressed( "Reload" ) )
		{
			DeleteSave( SaveCursor );
			return;
		}

		if ( PressedEscape() )
			CloseSaves();
	}

	public void NoteUiClick() => uiClickUntil = Time.Now + 0.15f;

	public void TogglePause()
	{
		if ( Paused )
			Resume();
		else
			Pause();
	}

	public void Pause()
	{
		if ( Paused || InMenu )
			return;

		Paused = true;
		pauseStartedAt = Time.Now;
		Mouse.CursorType = "pointer";
		ArenaSounds.MenuOpen();
	}

	public void Resume()
	{
		if ( !Paused )
			return;

		ShiftClocks( Time.Now - pauseStartedAt );
		ClearPause();
		Mouse.CursorType = "crosshair";
		ArenaSounds.MenuOk();
	}

	void ClearPause()
	{
		Paused = false;
		pauseStartedAt = 0f;
	}

	void TickPause()
	{
		Mouse.CursorType = "pointer";

		if ( PressedEscape() || Input.Pressed( "Jump" ) || Input.Pressed( "Slot1" ) )
		{
			Resume();
			return;
		}

		if ( Input.Pressed( "Slot2" ) )
		{
			ShowMenu();
			return;
		}

		if ( Input.Pressed( "Slot3" ) )
			QuitGame();
	}

	void ShiftClocks( float dt )
	{
		if ( dt <= 0f )
			return;

		noticeAt += dt;
		runStartedAt += dt;
		lastHurtAt += dt;
		armorNoticeAt += dt;
		if ( invulnUntil > 0f )
			invulnUntil += dt;

		Runner?.ShiftTime( dt );

		foreach ( var enemy in Enemies )
		{
			if ( !enemy.IsValid() )
				continue;

			enemy.ShiftTime( dt );
			enemy.GetComponent<ArenaBoss>()?.ShiftTime( dt );
		}

		foreach ( var shot in Shots )
		{
			if ( shot.IsValid() )
				shot.ShiftTime( dt );
		}
	}

	public void Restart()
	{
		Autosave();

		City?.ClearShots();
		ClearCombat();

		if ( Inventory.IsValid() )
			Inventory.ResetLoadout();

		if ( Runner.IsValid() )
		{
			Runner.GameObject.Enabled = true;
			Runner.ResetToStart( Arena.IsValid() ? Arena.StartAngle : MathF.PI * 0.5f );
			Runner.ApplyPace( 1 );
		}

		Phase = RunPhase.Playing;
		Health = HeartMax;
		if ( City.IsValid() )
		{
			var stats = City.Stats();
			if ( Runner.IsValid() )
			{
				Runner.ApplyCity( stats );
				Runner.ApplyPace( 1 );
			}

			if ( Inventory.IsValid() )
				Inventory.Loadout.BonusDamage = stats.BonusDamage;

			City.SetVisible( false );
		}
		Kills = 0;
		Scrap = 0;
		ShotsFired = 0;
		Catches = 0;
		Losses = 0;
		BloodShields = 0;
		SnapUntil = 0f;
		Lap = 1;
		layoutSeed = Game.Random.Int( 1, int.MaxValue - 1 );
		ExtractedRounds = 0;
		BurnedRounds = 0;
		invulnUntil = 0f;
		lastHurtAt = -99f;
		pendingBoss = false;
		InBossFight = false;
		bossWon = false;
		skipHinted = false;
		ClearPause();
		if ( Arena.IsValid() )
		{
			Geometry.CoreSolid = true;
			Geometry.ClearBossWalls();
		}
		runStartedAt = Time.Now;

		SpawnWave( 1 );
		Mouse.CursorType = "crosshair";
		ArenaSounds.Fight();
		Announce( "ONE LAP. ONE ROUND. CASH OUT OR GO AGAIN." );
	}

	public void RegisterKill( EnemyKind kind )
	{
		Kills++;
		var gain = Progression.KillScrap( kind, Lap );
		if ( gain > 0 )
			Scrap += gain;

		Announce( gain > 0 ? $"+{gain} SCRAP  ·  {Scrap}" : "TARGET DOWN" );
	}

	public void CatchRound( RoundProjectile projectile )
	{
		var slot = SlotOf( projectile.SlotIndex );
		if ( slot is null )
			return;

		var world = Geometry.ToPlayWorld( projectile.Flat );
		var chained = projectile.TargetsHit;
		var threshold = Inventory.Loadout.BloodThreshold;
		var shielded = false;
		if ( threshold > 0 && projectile.Kills >= threshold )
		{
			BloodShields++;
			shielded = true;
		}

		if ( Inventory.Loadout.SnapPreview > 0 )
			SnapUntil = Time.Now + 0.45f;

		slot.ResetCombat();
		slot.Status = RoundStatus.Chambered;
		Inventory.TrySelect( slot.Index );
		Catches++;

		ArenaSounds.Pickup( world );
		ImpactFlash.Spawn( Scene, world, slot.Tint, 1.4f );

		if ( shielded )
			Announce( $"ROUND {slot.Index + 1} CHAMBERED  ·  SHIELD" );
		else
			Announce( chained > 0 ? $"ROUND {slot.Index + 1} CHAMBERED  +{chained}" : $"ROUND {slot.Index + 1} CHAMBERED" );
	}

	public void RecoverDropped( RoundSlot slot, string reason )
	{
		if ( slot is null || slot.Status != RoundStatus.Dropped || !slot.Lost.IsValid() )
			return;

		var world = Geometry.ToPlayWorld( slot.Lost.Flat );
		slot.ResetCombat();
		slot.Status = RoundStatus.Chambered;
		Inventory.TrySelect( slot.Index );
		ArenaSounds.Pickup( world );
		ImpactFlash.Spawn( Scene, world, slot.Tint, 1.1f );
		Announce( $"ROUND {slot.Index + 1} {reason}" );
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
		dropped.Place( this, resting, slot.Index );

		slot.Flying = null;
		slot.Lost = dropped;
		slot.Status = RoundStatus.Dropped;
		Losses++;

		ArenaSounds.Lose();
		Announce( $"ROUND {slot.Index + 1} LOST" );
	}

	protected override void OnStart()
	{
		runStartedAt = Time.Now;
		Mouse.Visibility = MouseVisibility.Visible;
		Mouse.CursorType = "crosshair";
		ArenaMusic.Tick( this );
	}

	protected override void OnDestroy()
	{
		ArenaMusic.Stop();
	}

	protected override void OnUpdate()
	{
		ArenaMusic.Tick( this );

		if ( !Arena.IsValid() || !Runner.IsValid() || !Inventory.IsValid() )
			return;

		if ( Phase == RunPhase.Menu )
		{
			TickMenu();
			return;
		}

		if ( Paused )
		{
			TickPause();
			return;
		}

		if ( PressedEscape() )
		{
			if ( Phase == RunPhase.Playing || Phase == RunPhase.City || Phase == RunPhase.DecideLap || Phase == RunPhase.PickTrait )
				Pause();
			else
				ShowMenu();
			return;
		}

		if ( Input.Pressed( "Reload" ) )
		{
			if ( Phase == RunPhase.City || Phase == RunPhase.Playing || Phase == RunPhase.DecideLap || Phase == RunPhase.PickTrait )
				Restart();
			else
				EnterCity( false );
			return;
		}

		if ( bossWon )
		{
			FinishBossWin();
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
			Mouse.CursorType = "pointer";
			if ( Input.Pressed( "Slot1" ) )
			{
				Extract();
				return;
			}

			if ( Input.Pressed( "Slot2" ) )
			{
				ContinueRun();
				return;
			}

			if ( HasBossOffer && Input.Pressed( "Slot3" ) )
				ContinueBoss();
			return;
		}

		if ( Phase == RunPhase.PickTrait )
		{
			Mouse.CursorType = "pointer";
			if ( Input.Pressed( "Slot1" ) )
			{
				TryBuyOffer( OfferA );
				return;
			}

			if ( Input.Pressed( "Slot2" ) )
			{
				TryBuyOffer( OfferB );
				return;
			}

			if ( HasThirdOffer && Input.Pressed( "Slot3" ) )
			{
				TryBuyOffer( OfferC );
				return;
			}

			if ( Input.Pressed( "Use" ) )
			{
				TryBuyAll();
				return;
			}

			if ( Input.Pressed( "Jump" ) )
				LeaveShop();
			return;
		}

		if ( Phase == RunPhase.Dead || Phase == RunPhase.Extracted )
		{
			Mouse.CursorType = "pointer";
			return;
		}

		HandleSelect();

		if ( Input.Pressed( "Jump" ) && Runner.TryDash() )
			ArenaSounds.Jump( Runner.WorldPosition );

		if ( Input.Pressed( "Attack1" ) && !BlocksShot )
			Fire();

		CheckPickup();
		CheckSwipe();
		CheckHits();
		CheckLap();
	}

	void HandleSelect()
	{
		var prev = Inventory.SelectedIndex;

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

		if ( Inventory.SelectedIndex != prev )
			ArenaSounds.Change();
	}

	void Fire()
	{
		var slot = Inventory.Selected;

		if ( slot is null || slot.Status != RoundStatus.Chambered )
		{
			ArenaSounds.Deny();
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

		ArenaSounds.Fire( Aim.MuzzleWorld );
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

	void CheckSwipe()
	{
		if ( !Runner.IsValid() || !Runner.Dashing || !Inventory.IsValid() )
			return;

		var reach = Inventory.Loadout.SwipeRadius;
		if ( reach <= 1f )
			return;

		foreach ( var slot in Inventory.Slots )
		{
			if ( slot.Status != RoundStatus.InFlight || !slot.Flying.IsValid() || !slot.Flying.Armed )
				continue;

			if ( (slot.Flying.Flat - Runner.Flat).Length > reach + slot.Flying.Radius )
				continue;

			CatchRound( slot.Flying );
		}
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

			ArenaSounds.Pickup( world );
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
		if ( BloodShields > 0 )
		{
			BloodShields--;
			lastHurtAt = Time.Now;
			invulnUntil = Time.Now + 1.05f;
			var blocked = Geometry.ToPlayWorld( Runner.Flat );
			ArenaSounds.Armor( Runner.WorldPosition );
			ImpactFlash.Spawn( Scene, blocked, new Color( 1f, 0.85f, 0.35f ), 2.4f );
			Announce( BloodShields > 0 ? $"SHIELD  ·  {BloodShields} LEFT" : "SHIELD BROKE" );
			return;
		}

		Health--;
		lastHurtAt = Time.Now;
		invulnUntil = Time.Now + 1.05f;

		var world = Geometry.ToPlayWorld( Runner.Flat );
		ImpactFlash.Spawn( Scene, world, new Color( 1f, 0.12f, 0.08f ), 3.4f );
		ImpactFlash.Spawn( Scene, world + Vector3.Up * 40f, Color.White, 1.8f );

		if ( Health > 0 )
		{
			ArenaSounds.Pain( Runner.WorldPosition );
			Announce( $"-1  ·  {Health} LEFT" );
			return;
		}

		ArenaSounds.Death( Runner.WorldPosition );
		InBossFight = false;
		Phase = RunPhase.Dead;
		BurnedRounds = Stash;
		Announce( "RUN OVER" );
	}

	public void TryHurt()
	{
		if ( Time.Now < invulnUntil || (Runner.IsValid() && Runner.Dashing) )
			return;

		Hurt();
	}

	public void NoteArmor()
	{
		if ( Time.Now < armorNoticeAt )
			return;

		armorNoticeAt = Time.Now + 0.85f;
		Announce( "ARMOR  ·  RICOCHET FIRST" );
	}

	public void BeatBoss()
	{
		if ( bossWon )
			return;

		bossWon = true;
		InBossFight = false;
		ArenaSounds.Pickup();
		Announce( "NO MORE ROUNDS  ·  ×2" );
	}

	void FinishBossWin()
	{
		bossWon = false;

		if ( Arena.IsValid() )
		{
			Geometry.CoreSolid = true;
			Geometry.ClearBossWalls();
		}

		var doubled = Math.Max( 1, Stash ) * 2;
		ExtractedRounds = doubled;
		if ( ExtractedRounds > BestExtract )
			BestExtract = ExtractedRounds;

		EnterCity( true, doubled );
		Announce( $"NO MORE ROUNDS  ·  ×2  ·  +{doubled}" );
	}

	void CheckLap()
	{
		if ( InBossFight )
			return;

		if ( CanSkipLap && !skipHinted )
		{
			skipHinted = true;
			Announce( "ARENA CLEAR  ·  E SKIP LAP" );
		}

		if ( Runner.IsValid() && Runner.Lap > Lap )
		{
			OpenLapClear();
			return;
		}

		if ( !CanSkipLap || !Input.Pressed( "Use" ) )
			return;

		Runner.FinishCurrentLap();
		OpenLapClear();
	}

	void OpenLapClear()
	{
		skipHinted = false;
		Phase = RunPhase.DecideLap;
		ArenaSounds.Tele();
		Announce( $"LAP {Lap} CLEAR" );
	}

	public void ChooseExtract()
	{
		if ( Paused || Phase != RunPhase.DecideLap )
			return;

		Extract();
	}

	public void ChooseContinue()
	{
		if ( Paused || Phase != RunPhase.DecideLap )
			return;

		ContinueRun();
	}

	public void ChooseBoss()
	{
		if ( Paused || Phase != RunPhase.DecideLap || !HasBossOffer )
			return;

		ContinueBoss();
	}

	public void ChooseUpgrade( RoundTrait trait )
	{
		if ( Paused || Phase != RunPhase.PickTrait )
			return;

		TryBuyOffer( trait );
	}

	public void ChooseBuyAll()
	{
		if ( Paused || Phase != RunPhase.PickTrait )
			return;

		TryBuyAll();
	}

	public void ChooseShopGo()
	{
		if ( Paused || Phase != RunPhase.PickTrait )
			return;

		LeaveShop();
	}

	public int PriceOf( RoundTrait trait )
	{
		if ( !Inventory.IsValid() )
			return Progression.TraitPrice( trait, 0 );

		return Progression.TraitPrice( trait, Inventory.Loadout.TraitLevel( trait ) );
	}

	public void ChooseCity()
	{
		if ( Paused || (Phase != RunPhase.Dead && Phase != RunPhase.Extracted) )
			return;

		EnterCity( false );
	}

	void Extract()
	{
		ExtractedRounds = Stash;
		if ( ExtractedRounds > BestExtract )
			BestExtract = ExtractedRounds;

		EnterCity( true );
	}

	void EnterCity( bool deposit, int rounds = -1 )
	{
		var packed = rounds >= 0 ? rounds : Stash;
		if ( deposit && City.IsValid() )
			City.Deposit( packed );

		if ( Inventory.IsValid() )
		{
			foreach ( var slot in Inventory.Slots )
				slot.ResetCombat();
		}

		ClearCombat();
		Phase = RunPhase.City;
		City?.EnsureBuilt();
		City?.SetVisible( true );

		if ( Runner.IsValid() )
			Runner.GameObject.Enabled = false;
		ArenaSounds.Tele();
		Mouse.CursorType = "crosshair";
		Announce( deposit ? $"CITY  ·  +{ExtractedRounds} WAREHOUSE" : "CITY" );
		Autosave();
	}

	void ContinueRun()
	{
		pendingBoss = false;
		Lap = Runner.Lap;
		Runner.ApplyPace( Lap );
		var added = GrantContinueRounds();
		BeginTraitPick();
		ArenaSounds.Pickup();
		Announce( added > 0
			? $"+{added} ROUND  ·  STASH {Stash}  ·  ×{Threat:0.00}"
			: $"STASH MAX  ·  ×{Threat:0.00}" );
	}

	void ContinueBoss()
	{
		pendingBoss = true;
		Lap = Runner.Lap;
		Runner.ApplyPace( Lap );
		var added = GrantContinueRounds();
		BeginTraitPick();
		ArenaSounds.Warn();
		Announce( $"CORE FIGHT  ·  +{added}  ·  STASH {Stash}" );
	}

	int GrantContinueRounds()
	{
		var want = Progression.RoundsGranted( Lap );
		var added = 0;

		for ( var i = 0; i < want; i++ )
		{
			if ( Inventory.Slots.Count >= Progression.MaxSlots )
				break;

			var granted = Inventory.GrantSlot();
			if ( granted is null )
				break;

			Inventory.TrySelect( granted.Index );
			added++;
		}

		return added;
	}

	void BeginTraitPick()
	{
		var loadout = Inventory.Loadout;
		var fresh = new List<RoundTrait>();
		var owned = new List<RoundTrait>();
		foreach ( var trait in RoundTraits.All )
		{
			var level = loadout.TraitLevel( trait );
			if ( level >= 3 )
				continue;

			if ( level > 0 )
				owned.Add( trait );
			else
				fresh.Add( trait );
		}

		var count = City.IsValid() ? City.Stats().OfferCount : 2;
		count = Math.Clamp( count, 2, 3 );

		OfferA = TakeTrait( fresh, owned, loadout, Scrap );
		OfferB = TakeTrait( fresh, owned, loadout, 0, OfferA );
		HasThirdOffer = count >= 3;
		if ( HasThirdOffer )
			OfferC = TakeTrait( fresh, owned, loadout, 0, OfferA, OfferB );

		OfferABought = false;
		OfferBBought = false;
		OfferCBought = false;
		Phase = RunPhase.PickTrait;
	}

	static RoundTrait TakeTrait( List<RoundTrait> fresh, List<RoundTrait> owned, RunLoadout loadout, int budget, params RoundTrait[] taken )
	{
		bool Used( RoundTrait trait )
		{
			foreach ( var skip in taken )
			{
				if ( skip == trait )
					return true;
			}

			return false;
		}

		var pool = new List<RoundTrait>();
		foreach ( var trait in fresh )
		{
			if ( !Used( trait ) )
				pool.Add( trait );
		}

		var hadFresh = pool.Count > 0;
		if ( pool.Count == 0 )
		{
			foreach ( var trait in owned )
			{
				if ( !Used( trait ) )
					pool.Add( trait );
			}
		}

		if ( pool.Count == 0 )
			return RoundTrait.Pierce;

		if ( budget > 0 )
		{
			var cheap = new List<RoundTrait>();
			foreach ( var trait in pool )
			{
				var level = loadout is null ? 0 : loadout.TraitLevel( trait );
				if ( Progression.TraitPrice( trait, level ) <= budget )
					cheap.Add( trait );
			}

			if ( cheap.Count > 0 )
				pool = cheap;
		}

		var pick = WeightedTrait( pool );
		if ( hadFresh )
			fresh.Remove( pick );
		else
			owned.Remove( pick );

		return pick;
	}

	static RoundTrait WeightedTrait( List<RoundTrait> pool )
	{
		var total = 0;
		foreach ( var trait in pool )
			total += RoundTraits.Weight( trait );

		if ( total <= 0 )
			return pool[0];

		var roll = Game.Random.Int( 0, total - 1 );
		var acc = 0;
		foreach ( var trait in pool )
		{
			acc += RoundTraits.Weight( trait );
			if ( roll < acc )
				return trait;
		}

		return pool[0];
	}

	void TryBuyOffer( RoundTrait trait )
	{
		if ( !MatchesOpenOffer( trait ) )
		{
			ArenaSounds.Deny();
			return;
		}

		var price = PriceOf( trait );
		if ( Scrap < price )
		{
			ArenaSounds.Deny();
			Announce( $"NEED {price - Scrap} SCRAP" );
			return;
		}

		Scrap -= price;
		Inventory.Loadout.Install( trait );
		MarkBought( trait );
		ArenaSounds.Pickup();
		Announce( $"{RoundTraits.Title( trait )} LV{Inventory.Loadout.TraitLevel( trait )}  ·  {Scrap} SCRAP" );

		if ( OpenOfferCount() == 0 )
			LeaveShop();
	}

	void TryBuyAll()
	{
		if ( !ShopCanBuyAll )
		{
			ArenaSounds.Deny();
			if ( OpenOfferCount() >= 2 )
				Announce( $"NEED {ShopBuyAllCost - Scrap} SCRAP" );
			return;
		}

		if ( !OfferABought )
			TryBuyOffer( OfferA );
		if ( Phase != RunPhase.PickTrait )
			return;

		if ( !OfferBBought )
			TryBuyOffer( OfferB );
		if ( Phase != RunPhase.PickTrait )
			return;

		if ( HasThirdOffer && !OfferCBought )
			TryBuyOffer( OfferC );
	}

	void LeaveShop()
	{
		if ( Phase != RunPhase.PickTrait )
			return;

		var fight = pendingBoss;
		pendingBoss = false;

		if ( fight )
			SpawnBoss();
		else
			SpawnWave( Lap );

		Phase = RunPhase.Playing;

		if ( fight )
			ArenaSounds.Fight();
		else
			ArenaSounds.Change();

		Announce( fight
			? "THE CORE  ·  RICOCHET TO BREAK IT"
			: $"ARMED  ·  {Scrap} SCRAP" );
	}

	bool MatchesOpenOffer( RoundTrait trait )
	{
		if ( !OfferABought && trait == OfferA )
			return true;
		if ( !OfferBBought && trait == OfferB )
			return true;
		if ( HasThirdOffer && !OfferCBought && trait == OfferC )
			return true;

		return false;
	}

	void MarkBought( RoundTrait trait )
	{
		if ( !OfferABought && trait == OfferA )
			OfferABought = true;
		else if ( !OfferBBought && trait == OfferB )
			OfferBBought = true;
		else if ( HasThirdOffer && !OfferCBought && trait == OfferC )
			OfferCBought = true;
	}

	int OpenOfferCount()
	{
		var n = 0;
		if ( !OfferABought ) n++;
		if ( !OfferBBought ) n++;
		if ( HasThirdOffer && !OfferCBought ) n++;
		return n;
	}

	int RemainingOfferCost()
	{
		var sum = 0;
		if ( !OfferABought ) sum += PriceOf( OfferA );
		if ( !OfferBBought ) sum += PriceOf( OfferB );
		if ( HasThirdOffer && !OfferCBought ) sum += PriceOf( OfferC );
		return sum;
	}

	void SpawnBoss()
	{
		ClearEnemies();
		Geometry.CoreSolid = false;
		Geometry.ClearBossWalls();
		InBossFight = true;
		RollArena( Lap );

		var go = Scene.CreateObject();
		go.Name = "Core";

		var enemy = go.AddComponent<Enemy>();
		enemy.Arena = Arena;
		enemy.Loop = this;
		enemy.Setup( EnemyKind.Core, Vector2.Zero, Progression.BossHealth( Lap ) );
		Enemies.Add( enemy );

		var boss = go.AddComponent<ArenaBoss>();
		boss.Arm( enemy );
	}

	void SpawnWave( int lap )
	{
		ClearEnemies();
		if ( Arena.IsValid() )
		{
			Geometry.CoreSolid = true;
			Geometry.ClearBossWalls();
		}

		RollArena( lap );

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
			enemy.Setup( kind, ArenaGeometry.FromAngle( angle ) * radius, Progression.EnemyHealth( hp, lap ) );
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

		var extra = Progression.ExtraBodies( lap );
		for ( var i = 0; i < extra; i++ )
			Add( EnemyKind.Chaser, offset + 0.85f * ( i + 3 ), hunt, 1 );
	}

	void RollArena( int lap )
	{
		if ( !Arena.IsValid() )
			return;

		Arena.RollLayout( lap, layoutSeed );
		NudgeLiveRounds();
	}

	void NudgeLiveRounds()
	{
		if ( !Inventory.IsValid() )
			return;

		foreach ( var slot in Inventory.Slots )
			slot.Flying?.NudgeOut();
	}

	void ClearCombat()
	{
		InBossFight = false;
		if ( Arena.IsValid() )
		{
			Geometry.CoreSolid = true;
			Geometry.ClearBossWalls();
		}

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
		var minArc = LostRoundMinArc;
		var rim = Inventory.IsValid() ? Inventory.Loadout.RimMinAngle : 0f;
		if ( rim > 0.01f )
			minArc = MathX.DegreeToRadian( rim ) * radius;

		var ahead = Wrap( Runner.Angle - angle ) * radius;
		if ( ahead < minArc )
			angle = Runner.Angle - minArc / radius;

		return ArenaGeometry.FromAngle( angle ) * radius;
	}

	static float Wrap( float radians )
	{
		radians %= MathF.Tau;
		return radians < 0f ? radians + MathF.Tau : radians;
	}
}
