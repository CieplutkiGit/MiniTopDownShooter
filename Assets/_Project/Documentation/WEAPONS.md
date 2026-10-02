# Weapons

## Weapon Definition

`WeaponDefinition` is the reusable data asset for projectile and hitscan weapons.

Create one using:

**Assets > Create > Mini Top Down Shooter > Weapon Definition**

or:

**Tools > Mini Top Down Shooter > Setup & Validation > Create Weapon Definition**

Assign the asset to a `Gun`. A single definition contains firing behavior, ammo rules, accuracy tuning, delivery settings, and pool sizing.

Shipped presets are available under `Assets/_Project/Data/Weapons/`, with matching drag-and-drop Gun prefabs under `Assets/_Project/Weapons/`.

## Fire modes

### Semi Automatic

One accepted shot per trigger press. Mouse clicks, the gamepad trigger, and twin-stick aim threshold transitions can all produce a press.

### Automatic

Continues firing while the trigger is held, respecting `Fire Interval`.

### Burst

A trigger press starts a burst. `Burst Count` controls the number of rounds and `Burst Interval` controls the spacing inside the burst. `Fire Interval` still gates the next burst after the previous burst finishes.

### Shotgun

Fires once per trigger press and uses `Projectiles Per Shot` to emit multiple pellets/rays. Combine it with spread for a conventional shotgun pattern.

## Delivery modes

### Projectile

Uses the projectile prefab and object pool. Enable **Override Projectile Motion** to let the weapon definition override the projectile's serialized speed and lifetime.

### Hitscan

Uses immediate physics raycasts instead of a projectile prefab.

Hitscan settings include:

- range
- collision layer mask
- maximum penetrations
- optional distance-based damage falloff curve

`Projectiles Per Shot` also applies to hitscan, so a shotgun can use several hitscan rays.

## Combat affiliation

Projectile and hitscan weapons share the same `DamageAffiliation` rules.

- Player, Enemy, and Neutral teams are supported.
- The shooter root is ignored.
- Same-team damage is disabled by default.
- Friendly collisions can either be ignored or treated as blocking.
- Friendly fire can be enabled per source affiliation.
- Legacy setups can fall back to the existing `Player` and `Enemy` layers when no explicit affiliation component is present.

Player demos and shipped enemy prefabs use explicit affiliations. Enemy projectiles reuse the same projectile prefab and automatically switch to a collision-capable runtime layer when needed.

## Damage and accuracy

- **Damage**: base damage per projectile or ray.
- **Fire Interval**: minimum interval between normal trigger cycles.
- **Projectiles Per Shot**: projectiles/rays emitted by one consumed round.
- **Spread Angle**: base horizontal spread.
- **Spread Per Shot**: additional temporary spread accumulated after each round.
- **Max Spread Angle**: cap for base plus accumulated spread.
- **Spread Recovery Per Second**: rate at which accumulated spread returns to zero.

## Ammo and reload

Weapon definitions can use finite or infinite ammo.

Finite-ammo fields:

- **Magazine Size**
- **Starting Reserve Ammo**
- **Max Reserve Ammo**
- **Reload Duration**
- **Auto Reload On Empty**
- **Cancel Reload On Fire**

Runtime ammo is stored per `Gun` instance through `WeaponAmmoState`; the shared ScriptableObject is never mutated.

Useful runtime members include:

- `AmmoInMagazine`
- `ReserveAmmo`
- `MagazineSize`
- `IsReloading`
- `Reload()`
- `CancelReload()`
- `AddAmmo(int)`
- `ResetRuntimeState()`

`AmmoPickup` adds reserve ammo to the player's active weapon.

## Weapon events

`Gun` implements `IGunEvents` and exposes:

- `Fired`
- `EmptyFired`
- `ReloadStarted`
- `ReloadCompleted`
- `ReloadCanceled`
- `AmmoChanged(currentMagazine, reserve)`
- `Equipped`
- `Unequipped`

Use these for HUD, animation, audio, muzzle feedback, reload indicators, and other presentation without putting presentation logic inside the weapon.

## Loadouts and switching

Add `WeaponLoadout` to the player when using more than one gun.

The loadout supports:

- an authored starting list
- a starting slot
- next/previous switching
- direct slot equip through `EquipSlot(int)`
- runtime weapon addition
- optional weapon mount transform
- `WeaponEquipped` and `WeaponAdded` events

`PlayerShoot` automatically routes firing/reload/ammo to the active loadout gun when a loadout exists. Its original single `_gun` reference remains the fallback for legacy scenes.

`WeaponPickup` can instantiate a Gun prefab under the loadout's weapon mount and optionally equip it immediately.

## Default controls

- Fire: left mouse button or gamepad right trigger
- Twin-stick fire: gamepad/keyboard look input still fires when look magnitude crosses the configured threshold
- Reload: R or gamepad west face button
- Next weapon: E or gamepad right shoulder
- Previous weapon: Q or gamepad left shoulder

## Runtime rebinding

`PlayerController` exposes small UI-facing hooks:

- `BeginInputRebind(actionName, bindingIndex)`
- `GetBindingDisplayString(actionName, bindingIndex)`
- `ResetInputBindings()`

Supported action names include the generated `Move`, `Look`, and `Pause` actions plus:

- `Fire`
- `Reload`
- `NextWeapon`
- `PreviousWeapon`

Binding overrides are persisted with `PlayerPrefs` and loaded by `InputReader` on startup.

## Pooling

Projectile weapons still use Unity's `ObjectPool<Projectile>`.

- **Default Pool Size** controls prewarm count.
- **Max Pool Size** controls retained pool capacity.

Hitscan weapons do not require a projectile pool.

## Backwards compatibility

A `Gun` with no `WeaponDefinition` keeps the original behavior:

- legacy inline projectile prefab
- legacy damage
- legacy fire rate
- legacy pool sizes
- projectile delivery
- automatic firing
- infinite ammo

Existing serialized guns therefore do not need an immediate migration.
