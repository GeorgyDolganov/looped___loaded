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
	public bool InCity => Phase == RunPhase.City || Phase == RunPhase.Won;
	public bool InMenu => Phase == RunPhase.Menu;
	public RunLocation Location { get; private set; } = RunLocation.Glass;
	public int LocationIndex => Locations.Index( Location );
	public bool HasNextRing => !Locations.IsLast( Location );
	public string LocationCode => Locations.Code( Location );
	public string LocationRule => Locations.Rule( Location );
	public string BossName => Locations.Boss( Location );
	public string NextRingCode => Locations.Code( Locations.Next( Location ) );
	public string NextRingRule => Locations.Rule( Locations.Next( Location ) );
	public bool WantsUiCursor => InMenu || Paused || Phase == RunPhase.DecideLap || Phase == RunPhase.DecideRing || Phase == RunPhase.PickTrait || Phase == RunPhase.Dead || Phase == RunPhase.Extracted || Phase == RunPhase.Won;
	public bool CanPause => !InMenu && !Paused && (Phase == RunPhase.Playing || Phase == RunPhase.City || Phase == RunPhase.DecideLap || Phase == RunPhase.DecideRing || Phase == RunPhase.PickTrait);
	public bool BlocksShot => Time.Now < uiClickUntil;
	public ArenaGeometry Geometry => Arena.Geometry;
	public List<Enemy> Enemies { get; } = new();
	public List<EnemyShot> Shots { get; } = new();
	public List<BonePickup> Bones { get; } = new();
	public List<GibChunk> Gibs { get; } = new();

	public MenuPage MenuView { get; private set; } = MenuPage.Title;
	public MenuChoice MenuFocus { get; private set; } = MenuChoice.Continue;
	public SettingRow SettingCursor { get; private set; } = SettingRow.Music;
	static readonly MenuChoice[] MenuOrder = { MenuChoice.Continue, MenuChoice.Upgrades, MenuChoice.Saves, MenuChoice.Settings, MenuChoice.Quit };
	public static readonly SettingRow[] SettingOrder = { SettingRow.Music, SettingRow.Sfx, SettingRow.Shake };
	public int ActiveSlot { get; private set; }
	public int SaveCursor { get; private set; }
	readonly GameSave[] slotCache = new GameSave[SaveStore.Slots];
	bool savesLoaded;
	public bool Paused { get; private set; }
	public bool IsFrozen => Paused || Phase != RunPhase.Playing;

	TextConfig T => GameSettings.Text;
	public string Notice { get; private set; } = "AIM. FIRE.";
	public float NoticeAge => Time.Now - noticeAt;
	public bool NoticeVisible => NoticeAge < NoticeDuration;

	public int Lap { get; private set; } = 1;
	int layoutSeed;
	public float LapFraction => Phase == RunPhase.DecideLap ? 1f : Runner.IsValid() ? Runner.LapFraction : 0f;
	public int Stash { get; private set; }
	public int HeartMax => MaxHealth + (City.IsValid() ? City.Stats().BonusHealth : 0);
	public int Health { get; private set; }
	public float HurtAmount => Math.Clamp( 1f - (Time.Now - lastHurtAt) / GameSettings.Run.HurtFlash, 0f, 1f );
	public bool Invulnerable => Time.Now < invulnUntil;
	public int Kills { get; private set; }
	public int Scrap { get; private set; }
	public int ShotsFired { get; private set; }
	public int Catches { get; private set; }
	public int Losses { get; private set; }
	public int BloodShields { get; private set; }
	public int ExtractedRounds { get; private set; }
	public int BurnedRounds { get; private set; }
	public int BestExtract { get; private set; }
	public int FedBiomass { get; private set; }
	public int WinNeed => Math.Max( 1, GameSettings.Run.WinBiomass );
	public float RunTime => Time.Now - runStartedAt;

	public IReadOnlyList<ShopOffer> Offers => offers;
	public int OfferStamp
	{
		get
		{
			var hash = offers.Count;
			foreach ( var offer in offers )
				hash = System.HashCode.Combine( hash, (int)offer.Trait, offer.Bought, TraitLocked( offer.Trait ) );

			return hash;
		}
	}
	public int ShopBuyAllCost => Phase == RunPhase.PickTrait ? RemainingOfferCost() : 0;
	public bool ShopHasBundle => Phase == RunPhase.PickTrait && OpenOfferCount() >= 2;
	public bool ShopCanBuyAll => ShopHasBundle && Scrap >= ShopBuyAllCost && ShopBuyAllCost > 0;

	public float Threat => Progression.Threat( Lap, LocationIndex );
	public bool InBossFight { get; private set; }
	public bool HasBossOffer => !InBossFight && Lap >= GameSettings.Run.BossOfferLap;
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
				if ( enemy.IsValid() && enemy.Alive && Locations.IsBoss( enemy.Kind ) )
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
				if ( enemy.IsValid() && enemy.Alive && Locations.IsBoss( enemy.Kind ) )
					return enemy.MaxHealth;
			}

			return Progression.BossHealth( Lap, LocationIndex );
		}
	}

	float noticeAt = -99f;
	float runStartedAt;
	float invulnUntil;
	float lastHurtAt = -99f;
	float armorNoticeAt = -99f;
	float boneSoundAt = -99f;
	float scrapNoticeAt = -99f;
	float pauseStartedAt;
	float uiClickUntil;
	bool pendingBoss;
	int featureTurn;
	readonly List<ShopOffer> offers = new();
	static readonly string[] OfferSlots = { "Slot1", "Slot2", "Slot3", "Slot4", "Slot5", "Slot6", "Slot7", "Slot8", "Slot9" };
	bool bossWon;
	bool skipHinted;
	public int BestLine { get; private set; } = -1;
	readonly ProgressTrack progress = new();
	public bool HasTask => progress.HasCurrent;
	public string TaskTitle => progress.Current?.Title ?? "";
	public string TaskBlurb => progress.Current?.Blurb ?? "";
	public string TaskStamp => progress.Stamp;
	public IReadOnlyList<ProgressStep> CompletedTasks => progress.CompletedSteps;
	public int TaskDone => progress.CompletedSteps.Count;
	public int TaskTotal => ProgressTrack.Total;

	public string SlotBlurb( int index )
	{
		var save = SlotInfo( index );
		if ( save is null || !save.HasProgress )
			return T.Saves.Empty;

		var line = Locations.LineBlurb( save.BestLine );
		return line.Length > 0
			? T.F( T.Saves.WithLine, save.Warehouse, save.BestExtract, line )
			: T.F( T.Saves.WithBuildings, save.Warehouse, save.BestExtract, save.BuildingCount );
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

		var save = City.Capture( BestExtract, BestLine );
		save.FedBiomass = FedBiomass;
		save.Tasks = progress.Capture();
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
		Announce( T.F( T.Announce.SlotPicked, ActiveSlot + 1 ) );
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
			WipeCampaign();

		RefreshSaves();
		ArenaSounds.MenuBack();
		Announce( T.F( T.Announce.SlotDeleted, index + 1 ) );
	}

	void ApplySave( GameSave save )
	{
		if ( save is null || !save.HasProgress )
		{
			WipeCampaign();
			return;
		}

		BestExtract = save.BestExtract;
		BestLine = save.BestLine;
		City?.Apply( save );
		FedBiomass = Math.Max( save.FedBiomass, City.IsValid() ? City.Warehouse : save.Warehouse );
		progress.Apply( save.Tasks, City, BestLine, BestExtract );
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

	public void NoteProgress( ProgressGoal goal, RunLocation location = default )
	{
		progress.Note( goal, location );
	}

	public void ShowMenu()
	{
		City?.ClearShots();
		ClearCombat();

		if ( Inventory.IsValid() )
			Inventory.ChamberAll();

		if ( Runner.IsValid() )
		{
			Runner.GameObject.Enabled = true;
			Runner.ResetToStart( Arena.IsValid() ? Arena.StartAngle : MathF.PI * 0.5f );
		}

		City?.SetVisible( false );
		Location = Locations.Start;
		if ( Arena.IsValid() )
			Arena.ApplyLocation( Location );
		Phase = RunPhase.Menu;
		pendingBoss = false;
		bossWon = false;
		InBossFight = false;
		lastHurtAt = -99f;
		invulnUntil = 0f;
		ClearPause();
		MenuView = MenuPage.Title;
		MenuFocus = MenuChoice.Continue;
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

	public int OrganCount => City.IsValid() ? City.OccupiedPlots : 0;

	public void FocusMenu( MenuChoice item )
	{
		if ( MenuFocus == item )
			return;

		MenuFocus = item;
		ArenaSounds.MenuMove();
	}

	public void ActivateMenu( MenuChoice item )
	{
		MenuFocus = item;

		switch ( item )
		{
			case MenuChoice.Continue:
				Restart();
				return;
			case MenuChoice.Upgrades:
				OpenCityFromMenu();
				return;
			case MenuChoice.Saves:
				OpenSaves();
				return;
			case MenuChoice.Settings:
				OpenSettings();
				return;
			default:
				QuitGame();
				return;
		}
	}

	public void OpenSettings()
	{
		UserSettings.Load();
		SettingCursor = SettingRow.Music;
		MenuView = MenuPage.Settings;
		ArenaSounds.MenuOk();
	}

	public void CloseSettings()
	{
		MenuView = MenuPage.Title;
		MenuFocus = MenuChoice.Settings;
		ArenaSounds.MenuBack();
	}

	public void HighlightSetting( SettingRow row )
	{
		if ( SettingCursor == row )
			return;

		SettingCursor = row;
		ArenaSounds.MenuMove();
	}

	public void NudgeSetting( SettingRow row, int delta )
	{
		SettingCursor = row;

		if ( UserSettings.Nudge( row, delta ) )
			ArenaSounds.MenuMove();
		else
			ArenaSounds.Deny();
	}

	public bool SetSetting( SettingRow row, int steps )
	{
		SettingCursor = row;

		if ( !UserSettings.Set( row, steps * 0.1f ) )
			return false;

		ArenaSounds.MenuOk();
		return true;
	}

	void StepSettings( int delta )
	{
		var rows = SettingOrder;
		var index = Array.IndexOf( rows, SettingCursor );
		if ( index < 0 )
			index = 0;

		index = (index + delta + rows.Length) % rows.Length;
		HighlightSetting( rows[index] );
	}

	void TickSettings()
	{
		if ( Input.Pressed( "MenuUp" ) || Input.Pressed( "Forward" ) )
		{
			StepSettings( -1 );
			return;
		}

		if ( Input.Pressed( "MenuDown" ) || Input.Pressed( "Backward" ) )
		{
			StepSettings( 1 );
			return;
		}

		if ( Input.Pressed( "MenuLeft" ) || Input.Pressed( "Left" ) )
		{
			NudgeSetting( SettingCursor, -1 );
			return;
		}

		if ( Input.Pressed( "MenuRight" ) || Input.Pressed( "Right" ) )
		{
			NudgeSetting( SettingCursor, 1 );
			return;
		}

		if ( PressedEscape() || Input.Pressed( "MenuSelect" ) || Input.Pressed( "Jump" ) )
			CloseSettings();
	}

	void StepMenu( int delta )
	{
		var index = Array.IndexOf( MenuOrder, MenuFocus );
		if ( index < 0 )
			index = 0;

		index = (index + delta + MenuOrder.Length) % MenuOrder.Length;
		FocusMenu( MenuOrder[index] );
	}

	void TickMenu()
	{
		Mouse.CursorType = "pointer";

		if ( MenuView == MenuPage.Saves )
		{
			TickSaves();
			return;
		}

		if ( MenuView == MenuPage.Settings )
		{
			TickSettings();
			return;
		}

		if ( Input.Pressed( "MenuUp" ) || Input.Pressed( "Forward" ) )
		{
			StepMenu( -1 );
			return;
		}

		if ( Input.Pressed( "MenuDown" ) || Input.Pressed( "Backward" ) )
		{
			StepMenu( 1 );
			return;
		}

		if ( Input.Pressed( "MenuSelect" ) || Input.Pressed( "Jump" ) )
		{
			ActivateMenu( MenuFocus );
			return;
		}

		if ( Input.Pressed( "Slot1" ) )
		{
			ActivateMenu( MenuChoice.Continue );
			return;
		}

		if ( Input.Pressed( "Slot2" ) )
		{
			ActivateMenu( MenuChoice.Upgrades );
			return;
		}

		if ( Input.Pressed( "Slot3" ) )
		{
			ActivateMenu( MenuChoice.Saves );
			return;
		}

		if ( Input.Pressed( "Slot4" ) )
		{
			ActivateMenu( MenuChoice.Settings );
			return;
		}

		if ( Input.Pressed( "Slot5" ) )
		{
			ActivateMenu( MenuChoice.Quit );
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
			enemy.GetComponent<ArenaLens>()?.ShiftTime( dt );
		}

		foreach ( var shot in Shots )
		{
			if ( shot.IsValid() )
				shot.ShiftTime( dt );
		}

		foreach ( var bone in Bones )
		{
			if ( bone.IsValid() )
				bone.ShiftTime( dt );
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
			{
				Inventory.ResetLoadout();
				Inventory.Loadout.BonusDamage = stats.BonusDamage;
			}

			City.SetVisible( false );
		}
		Kills = 0;
		Scrap = 0;
		featureTurn = 0;
		ShotsFired = 0;
		Catches = 0;
		Losses = 0;
		BloodShields = 0;
		Stash = 1;
		Lap = 1;
		Location = Locations.Start;
		if ( Arena.IsValid() )
			Arena.ApplyLocation( Location );
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
		Announce( T.Announce.StartRun );
	}

	public void RegisterKill( EnemyKind kind, Vector2 origin )
	{
		Kills++;
		var gain = Progression.KillScrap( kind, Lap, LocationIndex );
		if ( gain > 0 )
			BonePickup.Spill( this, origin, gain );

		Announce( T.Announce.TargetDown );
	}

	public void CollectBone( int value, Vector3 world )
	{
		if ( value <= 0 )
			return;

		Scrap += value;
		if ( Time.Now >= boneSoundAt )
		{
			boneSoundAt = Time.Now + 0.05f;
			ArenaSounds.Pickup( world );
		}

		ImpactFlash.Spawn( Scene, world, BonePickup.Tint, 0.42f );
		if ( Time.Now >= scrapNoticeAt )
		{
			scrapNoticeAt = Time.Now + 0.4f;
			Announce( T.F( T.Announce.ScrapGain, value, Scrap ) );
		}
	}

	public void NoteShot()
	{
		ShotsFired++;
		NoteProgress( ProgressGoal.FireShot );
	}

	public void NoteCatch()
	{
		Catches++;
	}

	public Vector2 SnapToTrack( Vector2 from )
	{
		if ( !Arena.IsValid() )
			return from;

		var track = Geometry.TrackRadius;
		var ang = from.LengthSquared > 1f ? MathF.Atan2( from.y, from.x ) : Runner.IsValid() ? Runner.Angle : 0f;
		if ( Runner.IsValid() )
		{
			var minAng = LostRoundMinArc / MathF.Max( 1f, track );
			var delta = ang - Runner.Angle;
			while ( delta < 0f )
				delta += MathF.Tau;
			while ( delta >= MathF.Tau )
				delta -= MathF.Tau;
			if ( delta < minAng )
				ang = Runner.Angle + minAng;
		}

		return ArenaGeometry.FromAngle( ang ) * track;
	}

	protected override void OnStart()
	{
		runStartedAt = Time.Now;
		UserSettings.Load();
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
			if ( Phase == RunPhase.Playing || Phase == RunPhase.City || Phase == RunPhase.DecideLap || Phase == RunPhase.DecideRing || Phase == RunPhase.PickTrait )
				Pause();
			else
				ShowMenu();
			return;
		}

		if ( Input.Pressed( "Reload" ) )
		{
			if ( Phase == RunPhase.Won )
			{
				PlayAgain();
				return;
			}

			if ( Phase == RunPhase.Extracted )
				return;

			if ( Phase == RunPhase.City || Phase == RunPhase.Playing || Phase == RunPhase.DecideLap || Phase == RunPhase.DecideRing || Phase == RunPhase.PickTrait )
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

		if ( Phase == RunPhase.DecideRing )
		{
			Mouse.CursorType = "pointer";
			if ( Input.Pressed( "Slot1" ) )
			{
				Extract();
				return;
			}

			if ( HasNextRing && Input.Pressed( "Slot2" ) )
				EnterNextRing();
			return;
		}

		if ( Phase == RunPhase.PickTrait )
		{
			Mouse.CursorType = "pointer";
			var slotCount = Math.Min( offers.Count, OfferSlots.Length );
			for ( var i = 0; i < slotCount; i++ )
			{
				if ( Input.Pressed( OfferSlots[i] ) )
				{
					TryBuyOfferAt( i );
					return;
				}
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

		if ( Phase == RunPhase.Extracted )
		{
			Mouse.CursorType = "pointer";
			if ( Input.Pressed( "Jump" ) )
				ChooseCity();
			return;
		}

		if ( Phase == RunPhase.Won )
		{
			Mouse.CursorType = "pointer";
			if ( Input.Pressed( "Jump" )
				|| Input.Pressed( "Use" )
				|| Input.Pressed( "Attack1" )
				|| Input.Pressed( "Slot1" ) )
				PlayAgain();
			return;
		}

		if ( Phase == RunPhase.Dead )
		{
			Mouse.CursorType = "pointer";
			return;
		}

		HandleFire();
		Inventory?.TryPickup();

		if ( Input.Pressed( "Jump" ) && Runner.TryDash() )
			ArenaSounds.Jump( Runner.WorldPosition );

		CheckHits();
		CheckLap();
	}

	void HandleFire()
	{
		Inventory?.TickGun();
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
			invulnUntil = Time.Now + GameSettings.Run.IFrames;
			var blocked = Geometry.ToPlayWorld( Runner.Flat );
			ArenaSounds.Armor( Runner.WorldPosition );
			ImpactFlash.Spawn( Scene, blocked, new Color( 1f, 0.85f, 0.35f ), 2.4f );
			Announce( BloodShields > 0 ? T.F( T.Announce.ShieldLeft, BloodShields ) : T.Announce.ShieldBroke );
			return;
		}

		Health--;
		lastHurtAt = Time.Now;
		invulnUntil = Time.Now + GameSettings.Run.IFrames;

		var world = Geometry.ToPlayWorld( Runner.Flat );
		ImpactFlash.Spawn( Scene, world, new Color( 1f, 0.12f, 0.08f ), 3.4f );
		ImpactFlash.Spawn( Scene, world + Vector3.Up * 40f, Color.White, 1.8f );

		if ( Health > 0 )
		{
			ArenaSounds.Pain( Runner.WorldPosition );
			Announce( T.F( T.Announce.HealthLeft, Health ) );
			return;
		}

		ArenaSounds.Death( Runner.WorldPosition );
		VacuumBones();
		Inventory?.ChamberAll();
		InBossFight = false;
		Phase = RunPhase.Dead;
		BurnedRounds = Stash;
		Announce( T.Announce.RunOver );
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
		Announce( T.PlaceArmor( Location ) );
	}

	public void BeatBoss()
	{
		if ( bossWon )
			return;

		bossWon = true;
		InBossFight = false;
		ArenaSounds.Pickup();
		Announce( HasNextRing ? T.Announce.RingClear : T.Announce.FinalClear );
	}

	void FinishBossWin()
	{
		bossWon = false;

		if ( Arena.IsValid() )
		{
			Geometry.CoreSolid = true;
			Geometry.ClearBossWalls();
		}

		Inventory?.ChamberAll();

		if ( LocationIndex > BestLine )
			BestLine = LocationIndex;

		NoteProgress( ProgressGoal.BeatBoss, Location );

		if ( HasNextRing )
		{
			Phase = RunPhase.DecideRing;
			ArenaSounds.Tele();
			Announce( T.F( T.Announce.RingClearNext, NextRingCode, NextRingRule ) );
			Autosave();
			return;
		}

		var doubled = Math.Max( 1, Stash ) * GameSettings.Run.FinalStashMul;
		ExtractedRounds = doubled;
		if ( ExtractedRounds > BestExtract )
			BestExtract = ExtractedRounds;

		EnterCity( true, doubled );
		Announce( T.F( T.Announce.FinalBank, doubled ) );
	}

	void CheckLap()
	{
		if ( InBossFight )
			return;

		if ( CanSkipLap && !skipHinted )
		{
			skipHinted = true;
			Announce( T.Announce.ArenaSkip );
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
		VacuumBones();
		Inventory?.ChamberAll();
		skipHinted = false;
		Phase = RunPhase.DecideLap;
		ArenaSounds.Tele();
		Announce( T.F( T.Announce.LapClear, Lap ) );
		NoteProgress( ProgressGoal.FinishLap );
	}

	public void ChooseExtract()
	{
		if ( Paused || (Phase != RunPhase.DecideLap && Phase != RunPhase.DecideRing) )
			return;

		Extract();
	}

	public void ChooseNextRing()
	{
		if ( Paused || Phase != RunPhase.DecideRing || !HasNextRing )
			return;

		EnterNextRing();
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

	public void ChooseUpgradeAt( int index )
	{
		if ( Paused || Phase != RunPhase.PickTrait )
			return;

		TryBuyOfferAt( index );
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
			return Progression.TraitPrice( trait, 0, Lap, LocationIndex );

		return Progression.TraitPrice( trait, Inventory.Loadout.TraitLevel( trait ), Lap, LocationIndex );
	}

	public bool TraitLocked( RoundTrait trait )
		=> Inventory.IsValid() && RoundTraits.Blocked( trait, Inventory.Loadout );

	public void ChooseCity()
	{
		if ( Paused || (Phase != RunPhase.Dead && Phase != RunPhase.Extracted) )
			return;

		EnterCity( false );
	}

	public void PlayAgain()
	{
		if ( Paused || Phase != RunPhase.Won )
			return;

		NoteUiClick();
		WipeCampaign();
		Restart();
	}

	void WipeCampaign()
	{
		BestExtract = 0;
		BestLine = -1;
		FedBiomass = 0;
		ExtractedRounds = 0;
		progress.Clear();
		City?.Wipe();
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
		if ( !deposit && Phase == RunPhase.Extracted )
		{
			Phase = RunPhase.City;
			Mouse.CursorType = "crosshair";
			return;
		}

		var packed = rounds >= 0 ? rounds : Stash;
		var crossed = false;
		if ( deposit && City.IsValid() )
		{
			var before = FedBiomass;
			City.Deposit( packed );
			FedBiomass += Math.Max( 0, packed );
			crossed = before < WinNeed && FedBiomass >= WinNeed;
			NoteProgress( ProgressGoal.Extract );
		}

		if ( Inventory.IsValid() )
			Inventory.ChamberAll();

		ClearCombat();
		City?.EnsureBuilt();
		City?.SetVisible( true );

		if ( Runner.IsValid() )
			Runner.GameObject.Enabled = false;
		ArenaSounds.Tele();
		if ( crossed )
		{
			Phase = RunPhase.Won;
			Mouse.CursorType = "pointer";
			Announce( T.Announce.Won );
			Autosave();
			return;
		}

		if ( deposit )
		{
			Phase = RunPhase.Extracted;
			Mouse.CursorType = "pointer";
		}
		else
		{
			Phase = RunPhase.City;
			Mouse.CursorType = "crosshair";
		}

		Announce( deposit ? T.F( T.Announce.CityDeposit, ExtractedRounds ) : T.Announce.City );
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
			? T.F( T.Announce.ContinueRounds, added, Stash, Threat )
			: T.F( T.Announce.StashMax, Threat ) );
	}

	void ContinueBoss()
	{
		pendingBoss = true;
		Lap = Runner.Lap;
		Runner.ApplyPace( Lap );
		var added = GrantContinueRounds();
		BeginTraitPick();
		ArenaSounds.Warn();
		Announce( T.F( T.Announce.BossContinue, Locations.FightCall( Location ), added, Stash ) );
	}

	int GrantContinueRounds()
	{
		var want = Progression.RoundsGranted( Lap );
		Inventory?.GrowMag( want );
		var room = Progression.MaxSlots - Stash;
		if ( room <= 0 )
			return 0;

		var added = Math.Min( want, room );
		Stash += added;
		return added;
	}

	void BeginTraitPick()
	{
		var loadout = Inventory.Loadout;
		var fresh = new List<RoundTrait>();
		var owned = new List<RoundTrait>();
		foreach ( var trait in RoundTraits.All )
		{
			if ( RoundTraits.Blocked( trait, loadout ) )
				continue;

			var level = loadout.TraitLevel( trait );
			if ( level >= RoundTraits.MaxLevel( trait ) )
				continue;

			if ( level > 0 )
				owned.Add( trait );
			else
				fresh.Add( trait );
		}

		var count = City.IsValid() ? City.Stats().OfferCount : GameSettings.City.MinOffers;
		count = Math.Clamp( count, GameSettings.City.MinOffers, GameSettings.City.MaxOffers );

		offers.Clear();
		var taken = new List<RoundTrait>();
		for ( var i = 0; i < count; i++ )
		{
			if ( !TryTakeTrait( fresh, owned, loadout, Scrap, taken, out var trait ) )
				break;

			taken.Add( trait );
			offers.Add( new ShopOffer { Trait = trait } );
		}

		FeatureOffer( loadout );
		Phase = RunPhase.PickTrait;
	}

	void FeatureOffer( RunLoadout loadout )
	{
		if ( offers.Count == 0 || loadout is null )
			return;

		var lineup = new List<RoundTrait>();
		if ( loadout.Has( RoundTrait.Warhead ) )
			CollectBranch( lineup, RoundTraits.RocketBranch, loadout );
		if ( loadout.Has( RoundTrait.Bore ) )
			CollectBranch( lineup, RoundTraits.RailBranch, loadout );
		if ( loadout.Has( RoundTrait.Drum ) )
			CollectBranch( lineup, RoundTraits.RifleBranch, loadout );

		if ( lineup.Count == 0 )
		{
			foreach ( var trait in RoundTraits.Roots )
			{
				if ( loadout.TraitLevel( trait ) >= RoundTraits.MaxLevel( trait ) )
					continue;

				lineup.Add( trait );
			}
		}

		if ( lineup.Count == 0 )
			return;

		var pick = lineup[featureTurn % lineup.Count];
		featureTurn++;
		foreach ( var offer in offers )
		{
			if ( offer.Trait == pick )
				return;
		}

		offers[offers.Count - 1] = new ShopOffer { Trait = pick };
	}

	static void CollectBranch( List<RoundTrait> lineup, RoundTrait[] branch, RunLoadout loadout )
	{
		foreach ( var trait in branch )
		{
			if ( RoundTraits.Blocked( trait, loadout ) )
				continue;

			if ( loadout.TraitLevel( trait ) >= RoundTraits.MaxLevel( trait ) )
				continue;

			lineup.Add( trait );
		}
	}

	bool TryTakeTrait( List<RoundTrait> fresh, List<RoundTrait> owned, RunLoadout loadout, int budget, List<RoundTrait> taken, out RoundTrait pick )
	{
		bool Used( RoundTrait trait )
		{
			foreach ( var skip in taken )
			{
				if ( skip == trait )
					return true;

				if ( RoundTraits.Pack( skip ) == TraitPack.Abomination && RoundTraits.Pack( trait ) == TraitPack.Abomination )
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
		{
			pick = default;
			return false;
		}

		if ( budget > 0 )
		{
			var cheap = new List<RoundTrait>();
			foreach ( var trait in pool )
			{
				var level = loadout is null ? 0 : loadout.TraitLevel( trait );
				if ( Progression.TraitPrice( trait, level, Lap, LocationIndex ) <= budget )
					cheap.Add( trait );
			}

			if ( cheap.Count > 0 )
				pool = cheap;
		}

		pick = WeightedTrait( pool );
		if ( hadFresh )
			fresh.Remove( pick );
		else
			owned.Remove( pick );

		return true;
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
		for ( var i = 0; i < offers.Count; i++ )
		{
			if ( !offers[i].Bought && offers[i].Trait == trait )
			{
				TryBuyOfferAt( i );
				return;
			}
		}

		ArenaSounds.Deny();
	}

	void TryBuyOfferAt( int index )
	{
		if ( index < 0 || index >= offers.Count || offers[index].Bought )
		{
			ArenaSounds.Deny();
			return;
		}

		var trait = offers[index].Trait;
		if ( TraitLocked( trait ) )
		{
			ArenaSounds.Deny();
			return;
		}

		var price = PriceOf( trait );
		if ( Scrap < price )
		{
			ArenaSounds.Deny();
			Announce( T.F( T.Announce.NeedScrap, price - Scrap ) );
			return;
		}

		Scrap -= price;
		Inventory.Loadout.Install( trait );
		offers[index].Bought = true;
		ArenaSounds.Pickup();
		Announce( T.F( T.Announce.TraitBought, RoundTraits.Title( trait ), Inventory.Loadout.TraitLevel( trait ), Scrap ) );
		NoteProgress( ProgressGoal.BuyTrait );

		if ( OpenOfferCount() == 0 )
			LeaveShop();
	}

	void TryBuyAll()
	{
		if ( !ShopCanBuyAll )
		{
			ArenaSounds.Deny();
			if ( OpenOfferCount() >= 2 )
				Announce( T.F( T.Announce.NeedScrap, ShopBuyAllCost - Scrap ) );
			return;
		}

		for ( var i = 0; i < offers.Count; i++ )
		{
			if ( Phase != RunPhase.PickTrait )
				return;

			if ( offers[i].Bought || TraitLocked( offers[i].Trait ) )
				continue;

			TryBuyOfferAt( i );
		}
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
			? Locations.FightHint( Location )
			: T.F( T.Announce.Armed, Scrap ) );
	}

	int OpenOfferCount()
	{
		var n = 0;
		foreach ( var offer in offers )
		{
			if ( !offer.Bought && !TraitLocked( offer.Trait ) )
				n++;
		}

		return n;
	}

	int RemainingOfferCost()
	{
		var sum = 0;
		var loadout = Inventory.IsValid() ? Inventory.Loadout : null;
		var hasLash = loadout is not null && loadout.Has( RoundTrait.Lash );
		var hasBore = loadout is not null && loadout.Has( RoundTrait.Bore );
		var cluster = RoundTraits.OwnsCluster( loadout );
		var lance = RoundTraits.OwnsLance( loadout );
		var deep = RoundTraits.OwnsDeep( loadout );
		var mass = RoundTraits.OwnsMass( loadout );
		var hasDrum = loadout is not null && loadout.Has( RoundTrait.Drum );
		var sweep = RoundTraits.OwnsSweep( loadout );
		var track = RoundTraits.OwnsTrack( loadout );
		foreach ( var offer in offers )
		{
			if ( offer.Bought )
				continue;

			if ( offer.Trait == RoundTrait.Lash && hasBore )
				continue;

			if ( offer.Trait == RoundTrait.Bore && hasLash )
				continue;

			if ( RoundTraits.IsCluster( offer.Trait ) && lance )
				continue;

			if ( RoundTraits.IsLance( offer.Trait ) && cluster )
				continue;

			if ( RoundTraits.NeedsBore( offer.Trait ) && !hasBore )
				continue;

			if ( RoundTraits.IsDeep( offer.Trait ) && mass )
				continue;

			if ( RoundTraits.IsMass( offer.Trait ) && deep )
				continue;

			if ( RoundTraits.NeedsDrum( offer.Trait ) && !hasDrum )
				continue;

			if ( RoundTraits.IsSweep( offer.Trait ) && track )
				continue;

			if ( RoundTraits.IsTrack( offer.Trait ) && sweep )
				continue;

			sum += PriceOf( offer.Trait );
			if ( offer.Trait == RoundTrait.Lash )
				hasLash = true;
			if ( offer.Trait == RoundTrait.Bore )
				hasBore = true;
			if ( RoundTraits.IsCluster( offer.Trait ) )
				cluster = true;
			if ( RoundTraits.IsLance( offer.Trait ) )
				lance = true;
			if ( RoundTraits.IsDeep( offer.Trait ) )
				deep = true;
			if ( RoundTraits.IsMass( offer.Trait ) )
				mass = true;
			if ( offer.Trait == RoundTrait.Drum )
				hasDrum = true;
			if ( RoundTraits.IsSweep( offer.Trait ) )
				sweep = true;
			if ( RoundTraits.IsTrack( offer.Trait ) )
				track = true;
		}

		return sum;
	}

	void SpawnBoss()
	{
		ClearEnemies();
		Geometry.CoreSolid = false;
		Geometry.ClearBossWalls();
		InBossFight = true;
		RollArena( Lap );

		if ( Location == RunLocation.Glass )
		{
			var lensObject = Scene.CreateObject();
			lensObject.Name = "Lens";

			var lensEnemy = lensObject.AddComponent<Enemy>();
			lensEnemy.Arena = Arena;
			lensEnemy.Loop = this;
			lensEnemy.Setup( EnemyKind.Lens, Vector2.Zero, Progression.BossHealth( Lap, LocationIndex ) );
			Enemies.Add( lensEnemy );

			var lens = lensObject.AddComponent<ArenaLens>();
			lens.Arm( lensEnemy );
			return;
		}

		var go = Scene.CreateObject();
		go.Name = "Core";

		var enemy = go.AddComponent<Enemy>();
		enemy.Arena = Arena;
		enemy.Loop = this;
		enemy.Setup( EnemyKind.Core, Vector2.Zero, Progression.BossHealth( Lap, LocationIndex ) );
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

		var wave = GameSettings.Enemies.Wave;
		var inner = Geometry.CoreRadius + wave.InnerPad;
		var hunt = MathX.Lerp( inner, Geometry.TrackInner - wave.HuntOuterPad, wave.HuntMix );
		var mid = MathX.Lerp( inner, Geometry.TrackInner - wave.MidOuterPad, wave.MidMix );
		var outer = Geometry.TrackInner - wave.OuterPad;
		var offset = Runner.Angle + MathF.PI;

		void Add( EnemyKind kind, float angle, float radius, int hp )
		{
			var copies = Progression.WaveCopies;
			for ( var n = 0; n < copies; n++ )
			{
				var go = Scene.CreateObject();
				go.Name = kind.ToString();

				var enemy = go.AddComponent<Enemy>();
				enemy.Arena = Arena;
				enemy.Loop = this;
				enemy.Setup( kind, ArenaGeometry.FromAngle( angle + n * wave.ExtraAngle ) * radius, Progression.EnemyHealth( hp, lap, LocationIndex ) );
				Enemies.Add( enemy );
			}
		}

		if ( Location == RunLocation.Glass )
		{
			SpawnGlassWave( lap, offset, hunt, mid, inner, outer, Add );
			return;
		}

		var hp = WaveBodyHp( lap );
		switch ( lap )
		{
			case 1:
				Add( EnemyKind.Chaser, offset, hunt, hp );
				break;
			case 2:
				Add( EnemyKind.Chaser, offset - 0.7f, hunt, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset + 0.7f, hunt + 40f, hp );
				break;
			case 3:
				Add( EnemyKind.Shield, offset, mid, hp );
				Add( EnemyKind.Chaser, offset + 1.6f, hunt, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset - 1.4f, inner, hp );
				break;
			case 4:
				Add( EnemyKind.Shield, offset - 0.5f, mid, hp );
				Add( EnemyKind.Shooter, offset + 1.8f, mid, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset + 2.8f, hunt, hp );
				break;
			case 5:
				Add( EnemyKind.Chaser, offset - 1.1f, hunt, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset + 0.4f, hunt, hp );
				Add( EnemyKind.Shooter, offset + 2.2f, mid, hp );
				Add( EnemyKind.Chaser, offset + 3.4f, inner, hp );
				break;
			case 6:
				Add( WaveCloak( EnemyKind.Chaser ), offset, hunt, hp );
				Add( EnemyKind.Shield, offset + 2.1f, mid, hp );
				Add( EnemyKind.Shooter, offset + 4.0f, mid, hp );
				Add( EnemyKind.Shooter, offset - 2.2f, mid, hp );
				break;
			case 7:
				Add( EnemyKind.Chaser, offset - 0.8f, hunt, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset + 0.8f, outer - 40f, hp );
				Add( EnemyKind.Shield, offset + 2.4f, mid, hp );
				Add( EnemyKind.Shooter, offset + 4.2f, mid, hp );
				break;
			default:
				Add( EnemyKind.Chaser, offset, hunt, hp );
				Add( WaveCloak( EnemyKind.Chaser ), offset + 3.1f, hunt, hp );
				Add( EnemyKind.Shield, offset + 1.5f, mid, hp );
				Add( EnemyKind.Shield, offset + 3.6f, mid, hp );
				Add( EnemyKind.Shooter, offset + 2.5f, mid, hp );
				Add( EnemyKind.Shooter, offset + 5.0f, mid, hp );
				break;
		}

		AddWaveExtras( lap, offset, hunt, mid, inner, Add, EnemyKind.Chaser );
	}

	void SpawnGlassWave( int lap, float offset, float hunt, float mid, float inner, float outer, Action<EnemyKind, float, float, int> add )
	{
		var hp = WaveBodyHp( lap );
		switch ( lap )
		{
			case 1:
				add( EnemyKind.Splinter, offset, hunt, hp );
				break;
			case 2:
				add( EnemyKind.Splinter, offset - 0.6f, hunt, hp );
				add( WaveCloak( EnemyKind.Splinter ), offset + 1.4f, mid, hp );
				break;
			case 3:
				add( EnemyKind.Shardguard, offset, mid, hp );
				add( EnemyKind.Splinter, offset + 1.7f, hunt, hp );
				add( WaveCloak( EnemyKind.Splinter ), offset - 1.5f, inner, hp );
				break;
			case 4:
				add( EnemyKind.Shardguard, offset - 0.4f, mid, hp );
				add( WaveCloak( EnemyKind.Splinter ), offset + 1.9f, hunt, hp );
				add( EnemyKind.Shooter, offset + 3.2f, mid, hp );
				break;
			case 5:
				add( EnemyKind.Splinter, offset - 1.0f, hunt, hp );
				add( WaveCloak( EnemyKind.Splinter ), offset + 0.6f, hunt, hp );
				add( EnemyKind.Shardguard, offset + 2.3f, mid, hp );
				add( EnemyKind.Chaser, offset + 3.6f, inner, hp );
				break;
			default:
				add( EnemyKind.Splinter, offset, hunt, hp );
				add( EnemyKind.Splinter, offset + 3.0f, hunt, hp );
				add( WaveCloak( EnemyKind.Splinter ), offset + 1.4f, mid, hp );
				add( EnemyKind.Shardguard, offset + 3.8f, mid, hp );
				add( EnemyKind.Shooter, offset + 2.4f, mid, hp );
				break;
		}

		AddWaveExtras( lap, offset, hunt, mid, inner, add, EnemyKind.Splinter );
	}

	void AddWaveExtras( int lap, float offset, float hunt, float mid, float inner, Action<EnemyKind, float, float, int> add, EnemyKind kind )
	{
		var extra = Progression.ExtraBodies( lap, LocationIndex );
		for ( var i = 0; i < extra; i++ )
		{
			var angle = offset + MathF.Tau * ( i + 0.37f ) / extra;
			var ring = ( i % 3 ) switch
			{
				0 => hunt,
				1 => mid,
				_ => inner
			};
			add( WaveCloak( kind ), angle, ring, WaveExtraHp( lap ) );
		}
	}

	static int WaveBodyHp( int lap )
	{
		if ( lap <= 1 )
			return 1;
		if ( lap == 2 )
			return 3;
		if ( lap <= 5 )
			return 4;
		return 5;
	}

	static int WaveExtraHp( int lap ) => lap <= 1 ? 1 : 3;

	EnemyKind WaveCloak( EnemyKind fallback ) => Locations.IsLast( Location ) ? EnemyKind.Glimmer : fallback;

	void EnterNextRing()
	{
		if ( !HasNextRing )
			return;

		Inventory?.ChamberAll();
		NoteProgress( ProgressGoal.RideNextRing );
		if ( Health < HeartMax )
			Health += GameSettings.Run.RingHeal;

		Location = Locations.Next( Location );
		Lap = 1;
		skipHinted = false;
		pendingBoss = false;
		InBossFight = false;

		if ( Arena.IsValid() )
			Arena.ApplyLocation( Location );

		if ( Runner.IsValid() )
		{
			Runner.GameObject.Enabled = true;
			Runner.ResetLap( Arena.IsValid() ? Arena.StartAngle : MathF.PI * 0.5f );
			Runner.ApplyPace( 1 );
		}

		ClearCombat();
		SpawnWave( 1 );
		Phase = RunPhase.Playing;
		Mouse.CursorType = "crosshair";
		ArenaSounds.Tele();
		Announce( T.F( T.Announce.PlaceRule, LocationCode, LocationRule ) );
		Autosave();
	}

	public void RerollBoard()
	{
		RollArena( Lap );
	}

	void RollArena( int lap )
	{
		if ( !Arena.IsValid() )
			return;

		Arena.RollLayout( lap, unchecked( layoutSeed + LocationIndex * 104729 ) );
		NudgeLiveRounds();
	}

	void NudgeLiveRounds()
	{
		if ( !Inventory.IsValid() )
			return;

		foreach ( var shot in Inventory.Live )
			shot?.NudgeOut();

		Inventory.NudgeDropped();
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
		VacuumBones();
		ClearGibs();

		Inventory?.ClearShots();

		foreach ( var shot in Shots.ToArray() )
		{
			if ( shot.IsValid() )
				shot.GameObject.Destroy();
		}

		Shots.Clear();
	}

	void VacuumBones()
	{
		var gained = 0;
		foreach ( var bone in Bones.ToArray() )
		{
			if ( bone.IsValid() )
				gained += bone.Harvest();
		}

		Bones.Clear();
		if ( gained > 0 )
			Scrap += gained;
	}

	void ClearGibs()
	{
		foreach ( var gib in Gibs.ToArray() )
		{
			if ( gib.IsValid() )
				gib.GameObject.Destroy();
		}

		Gibs.Clear();
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
}

public sealed class ShopOffer
{
	public RoundTrait Trait { get; set; }
	public bool Bought { get; set; }
}

