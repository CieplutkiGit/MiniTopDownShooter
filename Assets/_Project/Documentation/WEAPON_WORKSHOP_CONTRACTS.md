# Weapon Workshop — Frozen System Contracts & Architecture

This document defines the frozen application contracts, data models, interfaces, identifiers, and activity policies for the MiniTopDownShooter modular weapon workshop. All technical agents (Wave A) and weapon-content agents (Wave B) must adhere strictly to these types and signatures.

---

## 1. Module Boundaries and Assemblies

The repository uses three core assemblies:

```
[Unity (MiniTopDownShooter.Unity)]
       │              │
       ▼              ▼
[Application (MiniTopDownShooter.Application)]
       │
       ▼
[Core (MiniTopDownShooter.Core)]
```

- **Application** (`noEngineReferences: true`): Contains all domain models, stat calculation, compatibility rules, session workflow, persistence ports, and activity policies. No `UnityEngine` dependencies.
- **Core** (`noEngineReferences: true`): General non-engine primitives.
- **Unity**: Concrete adapters, ScriptableObjects, UI presentation, MonoBehaviours, input handling, and SaveManager integration.

---

## 2. Standard Identifiers

### 2.1 Weapon Platform IDs
- `weapon.pistol` — Sidearm
- `weapon.rifle` — Assault Rifle
- `weapon.smg` — Submachine Gun
- `weapon.shotgun` — Pump / Combat Shotgun
- `weapon.launcher` — Rotary Grenade Launcher

### 2.2 Functional Slot IDs
| Platform | Slot 1 | Slot 2 | Slot 3 | Slot 4 |
| :--- | :--- | :--- | :--- | :--- |
| **Pistol** | `pistol.slot.barrel` | `pistol.slot.magazine` | `pistol.slot.grip` | `pistol.slot.slide` |
| **Rifle** | `rifle.slot.barrel` | `rifle.slot.magazine` | `rifle.slot.grip` | `rifle.slot.stock` |
| **SMG** | `smg.slot.barrel` | `smg.slot.magazine` | `smg.slot.grip` | `smg.slot.action` |
| **Shotgun** | `shotgun.slot.barrel` | `shotgun.slot.feed` | `shotgun.slot.stock` | `shotgun.slot.action` |
| **Launcher** | `launcher.slot.tube` | `launcher.slot.drum` | `launcher.slot.handles` | `launcher.slot.action` |

### 2.3 Part ID Naming Convention
Part IDs must follow the format `<weapon>.<slot_category>.<variant>`.
Examples:
- `rifle.barrel.standard`, `rifle.barrel.long`, `rifle.barrel.short`
- `rifle.magazine.standard`, `rifle.magazine.extended`, `rifle.magazine.drum`
- `rifle.grip.standard`, `rifle.grip.angled`, `rifle.grip.vertical`
- `rifle.stock.standard`, `rifle.stock.heavy`, `rifle.stock.light`

---

## 3. Core Domain Models (`Application.Weapons`)

### 3.1 `WeaponBuild`
- Represents an immutable snapshot of a weapon platform and its equipped parts.
- Backed by an ordered dictionary mapping `SlotId` -> `PartId`.
- Free of any scene references, ammunition state, or engine types.
- Methods: `GetPart(string slotId)`, `HasSlot(string slotId)`, `WithSelection(string slotId, string partId)`.

### 3.2 `WeaponPlatformSpec`
- Defines the base characteristics and default configuration of a weapon platform.
- Immutable; created from ScriptableObject definitions or test fixtures.
- Contains base numerical stats (damage, fire interval, magazine capacity, reserve, reload, spread, range, projectile physics, etc.) and `DefaultParts` mapping.

### 3.3 `WeaponPartSpec`
- Specifies an attachment for a single functional slot.
- Contains additive stat deltas (`DamageDelta`, `MagazineCapacityDelta`, etc.), multipliers (`DamageMultiplier`, `SpreadMultiplier`, etc.), and behavior overrides (`FireModeOverride`, `BurstCountOverride`, etc.).
- Includes compatibility lists: `IncompatiblePartIds`, `RequiredMountTags`, `ProvidedMountTags`.

### 3.4 `ResolvedWeaponStats`
- Immutable output of deterministic build resolution.
- Rounding and units:
  - `Damage`: HP points (clamped >= 1, rounded to 0.1).
  - `FireInterval`: Seconds between shots (clamped >= 0.02, rounded to 4 decimals).
  - `FireRate`: Rounds per second (`1.0 / FireInterval`).
  - `MagazineCapacity`: Integer count >= 1.
  - `MaxReserveAmmo`: Integer count >= `MagazineCapacity`.
  - `StartingReserveAmmo`: Integer count >= 0.
  - `ReloadDuration`: Seconds (clamped >= 0.1, rounded to 2 decimals).
  - `BaseSpreadAngle`: Degrees (clamped >= 0, rounded to 2 decimals).
  - `MaxSpreadAngle`: Degrees (clamped >= `BaseSpreadAngle`, rounded to 2 decimals).
  - `RecoilPerShot`: Degrees added per shot (clamped >= 0, rounded to 2 decimals).
  - `SpreadRecoveryRate`: Degrees recovered per second (clamped >= 0.1, rounded to 2 decimals).
  - `Range`: Meters (clamped >= 1.0, rounded to 1 decimal).
  - `ProjectileSpeed`: Meters/sec (clamped >= 0, rounded to 1 decimal).
  - `ProjectileLifetime`: Seconds (clamped >= 0.1, rounded to 2 decimals).
  - `PelletCount`: Integer count >= 1.
  - `AimTurnSpeed`: Degrees/sec (clamped >= 10.0, rounded to 1 decimal).

### 3.5 Operation Results
- `BuildResolution`: `IsValid`, `Stats`, `Errors`, `AffectedSlotOrPartIds`.
- `BuildLoadResult`: `IsSuccess`, `Build`, `WasMigratedOrRepaired`, `ErrorMessage`.
- `ApplyResult`: `IsSuccess`, `ErrorCode`, `ErrorMessage`, `AffectedSlotOrPartIds`.
- `SaveResult`: `IsSuccess`, `ErrorMessage`.

---

## 4. Interfaces and Module Ports

### 4.1 `IWeaponCatalog` (`Application.Weapons`)
Read-only repository for querying platforms and parts by ID:
- `WeaponPlatformSpec GetPlatform(string weaponId);`
- `bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform);`
- `WeaponPartSpec GetPart(string partId);`
- `bool TryGetPart(string partId, out WeaponPartSpec part);`
- `IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId);`
- `IReadOnlyList<string> GetAllWeaponIds();`

### 4.2 `IWeaponBuildResolver` (`Application.Weapons`)
Stat evaluation and data repair:
- `BuildResolution Resolve(WeaponBuild build, IWeaponCatalog catalog);`
  - Formula: Base stats + Additive modifiers * Multipliers -> Clamp & Round.
  - Detects missing required slots, unknown part IDs, mutual incompatibilities, and conflicting behavior overrides.
- `WeaponBuild Normalize(WeaponBuild build, IWeaponCatalog catalog, out bool repaired);`
  - Replaces obsolete or incompatible part IDs with platform defaults.

### 4.3 `IWeaponBuildTarget` (`Application.Weapons`)
Interface implemented by live combat weapon controllers (e.g. `Gun`):
- `string WeaponId { get; }`
- `WeaponBuild CurrentBuild { get; }`
- `ResolvedWeaponStats CurrentStats { get; }`
- `ApplyResult TryApply(WeaponBuild build, ResolvedWeaponStats stats);`

### 4.4 `IWeaponBuildStore` (`Application.Weapons`)
Persistence abstraction:
- `BuildLoadResult Load(string weaponId);`
- `SaveResult Save(WeaponBuild build);`

### 4.5 `IWeaponWorkshopSession` (`Application.Workshop`)
Interactive draft/apply session workflow:
- `WeaponBuild CommittedBuild { get; }`
- `WeaponBuild DraftBuild { get; }`
- `ResolvedWeaponStats DraftStats { get; }`
- `BuildResolution DraftResolution { get; }`
- `bool HasUnappliedChanges { get; }`
- `bool IsValid { get; }`
- `bool LastSaveFailed { get; }`
- `string LastSaveError { get; }`
- `void SelectPart(string slotId, string partId);`
- `ApplyResult Apply();`
- `void Discard();`
- `void BindTarget(IWeaponBuildTarget target);`
- `void SwitchWeapon(string weaponId, IWeaponBuildTarget target);`
- `event Action<IWeaponWorkshopSession> SessionChanged;`

### 4.6 `IWeaponPreviewView` (`Application.Workshop`)
Visual presentation contract:
- `void ShowBuild(WeaponBuild build);`
- `void SelectSlot(string slotId);`
- `void SetExploded(bool exploded);`
- `event Action<string> SlotSelected;`

---

## 5. Game State and Activity Policy

### 5.1 GameState Additions (`Application.GameState`)
Appended without modifying existing indices:
- `Menu = 0`
- `Playing = 1`
- `Paused = 2`
- `GameOver = 3`
- `Victory = 4`
- `WorkshopRoaming = 5` — Player walks around the workshop area; firing disabled.
- `WorkshopEditing = 6` — Player interacting at the workbench; simulation frozen.
- `WorkshopFiringRange = 7` — Player testing weapons in the shooting lane; firing enabled; wave progression disabled.

### 5.2 `GameActivityPolicy` (`Application.Workshop`)
Centralized authority on permissions:
| State | `CanMove` | `CanFire` | `AdvancesRun` | `IsTimeFrozen` |
| :--- | :---: | :---: | :---: | :---: |
| `Menu` | False | False | False | True |
| `Playing` | True | True | True | False |
| `Paused` | False | False | False | True |
| `GameOver` | False | False | False | False |
| `Victory` | False | False | False | True |
| `WorkshopRoaming` | True | False | False | False |
| `WorkshopEditing` | False | False | False | True |
| `WorkshopFiringRange` | True | True | False | False |

---

## 6. Combat and Ammunition Behavior Rules
1. **Ammo Conservation**: Customizing a weapon does not grant free rounds.
   - If `NewCapacity > CurrentCapacity`: `InMagazine` remains unchanged.
   - If `NewCapacity < CurrentCapacity`: excess rounds return to `ReserveAmmo`. If reserve cannot fit excess, excess is clamped to `MaxReserveAmmo` or rejected if strictly constrained.
2. **Runtime Isolation**: Modifying one weapon instance leaves other instances untouched. Projectiles already in flight preserve their original parameters.
3. **Safe Apply Sequence**:
   `Validate Draft` -> `Preflight Apply` -> `Cancel Burst/Reload` -> `Apply Config to Runtime & Delivery` -> `Publish BuildChanged` -> `Persist Build`.
