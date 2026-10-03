# Game loop implementation tasks

Read [GAME_LOOP_PLAN.md](GAME_LOOP_PLAN.md) and preserve [WEAPON_WORKSHOP_CONTRACTS.md](WEAPON_WORKSHOP_CONTRACTS.md) as the shared architecture. Paths below are repository-relative. New paths are explicitly marked. Execute in dependency order; keep each change limited to its task and validate the behavior it changes. Task descriptions intentionally contain no implementation code.

## T01 — Repair shooting audio

**Depends:** None.

**Read:** `Assets/_Project/Unity/GunAudio.cs`, `Assets/_Project/Unity/AudioListenerBase.cs`, `Assets/_Project/Unity/Gun.cs`, `Assets/_Project/Unity/WeaponLoadout.cs`, `Assets/_Project/Unity/GameCompositionRoot.cs`, `Assets/_Project/Editor/MiniTopDownShooterSetupCommands.cs`, all five `Assets/_Project/Weapons/Gun_*.prefab`, `Assets/_Project/Audio/GameAudioMixer.mixer`, `Assets/Scenes/ArenaShowcase.unity`.

**Do:** Inspect/resave the arena in Unity as text. Bind shot audio to every equipped gun using one consistent prefab pattern and the existing SFX mixer; assign existing clips or suitable existing fallbacks for all five weapons. Remove stale/duplicate fixed-gun playback and add missing-reference validation.

**Verify:** Hear every weapon after switching, pause/resume, and restart; assert valid authored gun/clip/source/mixer bindings and one playback request per accepted shot.

## T02 — Establish guarded flow and activity contracts

**Depends:** None.

**Read:** `Assets/_Project/Application/GameState.cs`, `Assets/_Project/Application/GameStateManager.cs`, `Assets/_Project/Application/IGameStateController.cs`, `Assets/_Project/Application/Workshop/GameActivityPolicy.cs`, `Assets/_Project/Unity/GameStateController.cs`, `Assets/_Project/Unity/TimeController.cs`, `Assets/_Project/Tests/GameStateLifecycleTests.cs`, `Assets/_Project/Tests/Workshop/A5/WorkshopActivityPolicyAndStateTests.cs`.

**Do:** Add the small Application flow models described in the plan under new `Assets/_Project/Application/Flow/`. Define run outcomes/ownership and valid deploy/return commands; guard local hub/edit/range/briefing transitions, preserve enum values, separate switching from firing, and freeze loading/results consistently.

**Verify:** Cover allowed/rejected transitions, duplicate commands, terminal-result idempotence, and pause restoration for every activity.

## T03 — Own committed player data and runtime catalog wiring

**Depends:** T02.

**Read:** `Assets/_Project/Application/ProfileData.cs`, `Assets/_Project/Unity/SaveManager.cs`, `Assets/_Project/Unity/WeaponLoadout.cs`, `Assets/_Project/Unity/Workshop/WeaponBuildApplier.cs`, `Assets/_Project/Unity/Workshop/Persistence/SaveManagerWeaponBuildStore.cs`, `Assets/_Project/Unity/Workshop/Persistence/WeaponWorkshopSaveData.cs`, `Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset`, `Assets/_Project/Tests/SaveManagerTests.cs`, `Assets/_Project/Tests/Workshop/A6/WeaponWorkshopPersistenceTests.cs`.

**Do:** Implement the shared data-only `PlayerSession`; load committed builds once, preserve ordered/equipped weapon IDs, and register all five visual profiles explicitly. Reuse existing persistence ports and atomic saves; migrate old profiles safely and inject session/catalog dependencies into production consumers.

**Verify:** Old/corrupt saves recover; equipment survives relaunch; a player build resolves every platform/visual without editor lookup; pending committed saves remain available across scene changes.

## T04 — Build the scene transition lifecycle

**Depends:** T02, T03.

**Read:** `Assets/_Project/Unity/GameCompositionRoot.cs`, `Assets/_Project/Unity/WorldResetManager.cs`, `Assets/_Project/Unity/PlayerController.cs`, `Assets/_Project/Unity/TimeController.cs`, `Assets/_Project/Unity/MainMenuUI.cs`, `Assets/_Project/Unity/PauseUI.cs`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/_Project/Tests/PlayMode/GameLifecycleIntegrationTests.cs`; T02–T03 outputs.

**Do:** Create new `Assets/Scenes/Boot.unity`, `Assets/_Project/Unity/Flow/AppCompositionRoot.cs`, and `SceneFlowController.cs`. Implement guarded unload/load/ready/recovery, inject app context into scene roots, and split reset preparation from starting combat. Keep one playable scene active and all pools scene-owned; preserve direct demo entry.

**Verify:** Repeated hub/arena transitions, double clicks, failed loads, and teardown leave no stale references, duplicate roots/listeners/EventSystems, stuck time scale, or premature waves.

## T05 — Connect the workshop to the live selected weapon

**Depends:** T03, T04.

**Read:** `Assets/_Project/Application/Workshop/Sessions/WorkshopSession.cs`, `Assets/_Project/Unity/Workshop/UI/WorkshopUIController.cs`, `Assets/_Project/Unity/Workshop/Room/WorkshopBenchTrigger.cs`, `Assets/_Project/Unity/Workshop/Combat/WeaponCombatAdapter.cs`, `Assets/_Project/Unity/Workshop/Presentation/WeaponPreviewView.cs`, `Assets/_Project/Unity/Workshop/Presentation/WeaponModelAssembler.cs`, `Assets/_Project/Unity/WeaponLoadout.cs`, `Assets/_Project/Unity/GameCompositionRoot.cs`, `Assets/_Project/Tests/Workshop/A5/WorkshopUIControllerTests.cs`.

**Do:** Add new `Assets/_Project/Unity/Workshop/WorkshopRuntimeController.cs`; construct/bind the selected weapon's session, target, catalog, and preview. Connect visible slot/part selection and apply/discard/exit, refresh live stats/visuals, expose retry for pending committed saves, and unbind cleanly. Exit discards only unapplied drafts.

**Verify:** All five weapons edit correctly; applied changes persist and affect the live gun; reopening/switching never reuses another weapon's draft or subscriptions.

## T06 — Author the player's geometric base

**Depends:** T01, T04, T05.

**Read:** `Assets/Scenes/ArenaShowcase.unity`, `Assets/_Project/Editor/GeometricArenaPolishSetup.cs`, `Assets/_Project/Unity/CameraFollow.cs`, `Assets/_Project/Unity/PlayerVisualDynamics.cs`, `Assets/_Project/Unity/MainMenuUI.cs`, `Assets/_Project/Unity/Workshop/Room/WorkshopBenchTrigger.cs`, `Assets/_Project/Unity/Workshop/Room/WorkshopFiringRangeTrigger.cs`, `Assets/_Project/Unity/MobileDemoControlsBootstrap.cs`; T04–T05 outputs.

**Do:** Create new `Assets/Scenes/BaseHub.unity`, `Assets/_Project/PlayerRig.prefab`, and `Assets/_Project/UI/SharedTouchControls.prefab`. Build the compact base, weapon display/bench, firing lane, and terminal using existing geometry/material patterns. Wire title-to-roaming entry, readable interaction prompts, weapon selection, and local camera/audio ownership.

**Verify:** Fresh boot opens the base menu; entering the room, selecting weapons, and opening/closing the bench work without combat or mission activity.

## T07 — Make weapon testing safe and useful

**Depends:** T01, T05, T06.

**Read:** `Assets/_Project/Unity/Workshop/Room/WorkshopFiringRangeTrigger.cs`, `Assets/_Project/Unity/Gun.cs`, `Assets/_Project/Unity/ProjectileWeaponDelivery.cs`, `Assets/_Project/Unity/HitscanWeaponDelivery.cs`, `Assets/_Project/Unity/HealthComponent.cs`, `Assets/_Project/Unity/DamageAffiliation.cs`, `Assets/_Project/Unity/ScoreController.cs`, `Assets/_Project/Tests/Workshop/A5/WorkshopRoomTriggerTests.cs`; new `Assets/Scenes/BaseHub.unity`.

**Do:** Author new `Assets/_Project/RangeTarget.prefab` and range collision boundaries in BaseHub; use the committed live weapon with normal reload behavior and explicit practice refill/reset. Isolate targets from mission spawns/score/profile, restore target health, and clear outgoing range projectiles/actions on exit.

**Verify:** Test all five delivery modes/builds; targets reset, shots stay contained, range exit stops firing, and practice changes no mission records or deployment ammo.

## T08 — Deploy into a complete finite mission

**Depends:** T04, T06, T07.

**Read:** `Assets/_Project/Unity/WaveController.cs`, `Assets/_Project/Unity/WaveSet.cs`, `Assets/_Project/Unity/EnemySpawner.cs`, `Assets/_Project/Unity/SpawnZone.cs`, `Assets/_Project/Unity/WorldResetManager.cs`, `Assets/_Project/Unity/WeaponLoadout.cs`, `Assets/_Project/Unity/WaveUI.cs`, `Assets/_Project/Unity/AmmoPickup.cs`, `Assets/_Project/Data/EnemyStats_Boss.asset`, `Assets/_Project/Data/Waves/WaveSet_ArenaShowcase.asset`, `Assets/Scenes/ArenaShowcase.unity`, `Assets/_Project/Tests/WaveRunnerTests.cs`; T02–T04 outputs.

**Do:** Add new `Assets/_Project/Unity/Flow/MissionDefinition.cs`, `MissionRunController.cs`, and `DeploymentTerminal.cs`; author new `Assets/_Project/Data/Missions/Mission_ArenaSweep.asset`. Show objective/loadout briefing, snapshot committed equipment, prepare the arena before starting, and tune the existing four waves/boss/pickups toward a 3–5 minute mission with readable progress.

**Verify:** Pending build saves block deployment; deployment matches the tested build/active weapon, ammo supports completion, pause preserves progress, and all required enemies/boss produce exactly one victory. Invalid setup produces a recoverable technical error.

## T09 — Finish runs once and return to base

**Depends:** T03, T08.

**Read:** `Assets/_Project/Unity/GameStateController.cs`, `Assets/_Project/Unity/GameOverUI.cs`, `Assets/_Project/Unity/VictoryUI.cs`, `Assets/_Project/Unity/PauseUI.cs`, `Assets/_Project/Unity/HighScoreController.cs`, `Assets/_Project/Unity/ScoreController.cs`, `Assets/_Project/Application/ProfileData.cs`, `Assets/_Project/Unity/SaveManager.cs`, `Assets/_Project/Tests/PlayMode/GameLifecycleIntegrationTests.cs`; T02–T04 and T08 outputs.

**Do:** Route victory/death/abandon/error through the shared run result. Replace state-edge profile writing with one idempotent finalizer; apply the plan's outcome policies, persist summary/last run ID, and retain failed saves for retry. Wire results and pause return-to-base actions; rebuild the hub from committed session data.

**Verify:** Victory, death, paused abandon, simultaneous end events, save failure/retry, return, and relaunch never double-count or lose selected builds; practice/error outcomes leave records unchanged.

## T10 — Harden mobile controls and interruptions

**Depends:** T05–T09.

**Read:** `Assets/_Project/Unity/InputReader.cs`, `Assets/_Project/Unity/AlivePlayerState.cs`, `Assets/_Project/Unity/PlayerController.cs`, `Assets/_Project/Unity/MobileInputState.cs`, `Assets/_Project/Unity/MobileJoystick.cs`, `Assets/_Project/Unity/MobileActionButton.cs`, `Assets/_Project/Unity/MobileControlsVisibility.cs`, `Assets/_Project/Unity/PauseInputController.cs`, `Assets/_Project/Unity/SafeAreaFitter.cs`, `Assets/_Project/Tests/Workshop/A5/WorkshopInputRearmTests.cs`; T06 shared controls.

**Do:** Author one state-aware touch layout for hub/arena; show interact in base and combat controls during practice/missions. Route bench interaction through shared input, resolve E-key switching overlap, block modal touch leakage, and clear/rearm inputs across transitions/backgrounding. Require explicit resume and preserve safe-area layout.

**Verify:** Move/aim multi-touch, held-fire modal exit, joystick release outside bounds, background/foreground, pause during loading, and supported phone aspect ratios.

## T11 — Bound runtime cost and clean up scene objects

**Depends:** T07–T10.

**Read:** `Assets/_Project/Unity/EnemySpawner.cs`, `Assets/_Project/Unity/EnemyMovement.cs`, `Assets/_Project/Unity/EnemyController.cs`, `Assets/_Project/Unity/ProjectileWeaponDelivery.cs`, `Assets/_Project/Unity/Gun.cs`, `Assets/_Project/Unity/EffectPool.cs`, `Assets/_Project/Unity/ParticleBurst.cs`, `Assets/_Project/Unity/EnemyDeathDebris.cs`, `Assets/_Project/Unity/DebrisChunk.cs`, `Assets/_Project/Unity/WorldResetManager.cs`; T04/T08 outputs.

**Do:** Capture a device baseline; prewarm mission-used pools, apply measured active-object budgets, stagger navigation updates, and fix observed combat allocations. Parent/dispose every pool with its scene. Handle enemy capacity by deferring spawns; preserve gameplay shots/ammo while allowing cosmetic saturation to skip effects.

**Verify:** Worst-case shotgun/launcher and boss combat, capacity saturation, and ten complete scene cycles show bounded counts, no missing required spawns, no retained scene instances, and no recurring combat allocation spikes.

## T12 — Polish the shared presentation and Mobile URP profile

**Depends:** T10, T11.

**Read:** `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Settings/Mobile_Renderer.asset`, `ProjectSettings/QualitySettings.asset`, `Assets/_Project/Unity/Workshop/Presentation/WeaponPreviewView.cs`, `Assets/_Project/Unity/Workshop/Presentation/WeaponModelAssembler.cs`, `Assets/_Project/Unity/Workshop/UI/WorkshopUIController.cs`, `Assets/_Project/Unity/WeaponHUD.cs`, `Assets/_Project/Unity/WaveUI.cs`, `Assets/_Project/Unity/SettingsUI.cs`, `Assets/_Project/Unity/GameStateAudio.cs`; hub/arena and T11 captures.

**Do:** Preserve geometric styling while tightening camera framing, prompts, objective/results readability, sound levels, and transitions. Bound preview rendering/lists and optional effects; tune lights/shadows/render scale from measured bottlenecks. Verify mobile quality selection and event-driven UI updates.

**Verify:** Compare visuals at phone size; all screens remain readable and responsive; record sustained frame timings, thermal behavior, and memory against the plan's device budgets.

## T13 — Gate the production loop and build pipeline

**Depends:** T01–T12.

**Read:** `Assets/_Project/Tests/PlayMode/DemoSceneSmokeTests.cs`, `Assets/_Project/Tests/PlayMode/GameLifecycleIntegrationTests.cs`, `Assets/_Project/Tests/Workshop/WeaponWorkshopFinalIntegrationTests.cs`, `Assets/_Project/Tests/ReleaseGatesTests.cs`, `Assets/_Project/Editor/MiniTopDownShooterSetupCommands.cs`, `.github/workflows/unity-ci.yml`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/_Project/Documentation/QUICK_START.md`, `Assets/_Project/Documentation/MOBILE.md`; all new flow controllers and scenes.

**Do:** Add journey tests and role-aware hub/arena validation; configure production Boot/BaseHub/ArenaShowcase scene order and shared mobile controls. Preserve legacy scenes in the test configuration; update build/CI scene selection and concise play instructions. Run the existing suites and smoke build, then the mobile build/device gate.

**Verify:** Fresh save → menu/base → edit/apply → test → deploy → actual wave victory → results/base → relaunch; also death, abandon, load/save errors, repeated cycles, and interruption. Record hardware/build evidence; report unavailable device checks as pending, never passed.
