> [!WARNING] **Skepticism Disclaimer**
> While all 490 EditMode unit and integration tests passed cleanly with 0 failures, PlayMode tests were explicitly cancelled per user request and cannot guarantee that in-engine physics collisions behave identically across every level variant without live interactive playtesting.

## 1. What I changed
- `Assets/_Project/Data/Weapons/Weapon_Pistol.asset`: Updated `_deliveryMode` from 1 (Hitscan) to 0 (Projectile) and set `_projectilePrefab` to `Assets/_Project/Projectile.prefab`.
- `Assets/_Project/Data/WeaponCustomization/Pistol/Platform_Pistol.asset`: Updated `_deliveryMode` from 1 to 0 (Projectile).
- `Assets/_Project/Weapons/Gun_Pistol.prefab`: Set `_prefab` to `Assets/_Project/Projectile.prefab`.
- `Assets/_Project/Unity/Gun.cs`: Added `_spawnPoint` fallback to `transform` in `Awake()`, added editor fallback resolution for `Projectile.prefab` in `InitializeDelivery()` and `ApplyDeliveryParameters()`, and enabled child renderer toggling in `SetVisualState()` when `_equippedVisualRoot` is null.
- `Assets/_Project/Unity/ProjectileWeaponDelivery.cs`: Added editor fallback for null prefabs and procedural fallback projectile creation in `CreateProjectile()`, ensuring `_pool` is never null.
- `Assets/_Project/Unity/WeaponLoadout.cs`: Added `EnsureStarterWeapon()` to instantiate and equip `Gun_Pistol.prefab` if the player has no active gun, and refined `EffectivePolicy` to `EconomyPolicy ?? ActivePolicy` to avoid false gating in standalone scenes.
- `Assets/_Project/Unity/GameCompositionRoot.cs`: Added fallback array population in `ResolveMissingReferences()` for `_weaponPrefabs`, and added a check in `EnsureOwnedWeapons()` to re-equip the default starter weapon if the active gun is locked.
- `Assets/_Project/Unity/InputReader.cs`: Removed UI occlusion in `TryGetPointerLookDirection()` so player aiming tracks mouse screen position reliably, and filtered `IsPointerOverUI()` to only trigger on interactive components (`Selectable`, `IPointerClickHandler`, `IDragHandler`, `ScrollRect`), ignoring HUD text, health bars, and transparent canvases.
- `Assets/_Project/Tests/Flow/LobbyAndWeaponEditTests.cs`: Enforced atomic profile isolation with cleanup in `StarterWeapon_Pistol_IsEquippedOnStart_WhenOtherWeaponsLocked`.

## 2. Why
- R1 & R2: Starter pistol lacked projectile delivery mode and projectile prefab serialization, causing hitscan or null-pool execution instead of visible projectile emission matching rifle, SMG, and launcher. In scenes without initialized loadouts, players could be unarmed.
- R3: Aiming and firing were previously suppressed by non-interactive graphics or transparent HUD elements that had raycast targets enabled.
- R4: Full regression avoidance across the test suite, maintaining 100% passing tests.

## 3. Verification Record
- **Deep Verification (ran actual tests):** Ran full EditMode test suite via Unity 6000.3.10f1 batchmode on review clone (`.utmp/SwarmReviewEconomy`); **490 / 490 tests passed with 0 failures** (`.utmp/test-results-starter-weapon.xml`).
- **Shallow Verification (manual run only):** Inspected prefab and ScriptableObject YAML assets (`Weapon_Pistol.asset`, `Platform_Pistol.asset`, `Gun_Pistol.prefab`) and verified GUID bindings match `Assets/_Project/Projectile.prefab`.
- **Unverified aspects:** Full batchmode PlayMode suite was cancelled and skipped upon explicit user command ("bro fk those test they always stuck skip it dont do them") due to Windows URP batchmode backbuffer render stalls.

## 4. Known Issues
- `Shallow Verification` — PlayMode headless test loop was skipped per user directive and not run to completion in batchmode.
- `Minor Robustness Risk` — If a custom UI element in a new scene implements no standard Unity UI interfaces (`Selectable`, `IPointerClickHandler`, `IDragHandler`, `ScrollRect`) but relies solely on raw `Update()` mouse raycasting, `InputReader.IsPointerOverUI()` will treat it as non-interactive and allow firing through it.

## 5. Untested Edge Cases & Next Step
- Verify in the active Unity Editor (PID 15760) by pressing Play in `SampleScene` and `BaseHub`: verify that mouse aiming tracks seamlessly over HUD elements and that firing the pistol shoots visible blue projectile spheres dealing damage to enemy targets.
