# Quick Start

## Open and validate the demo

Open `Assets/Scenes/SampleScene.unity`, then choose:

**Tools > Mini Top Down Shooter > Setup & Validation**

Run **Validate Open Scene** before entering Play Mode.

The validator checks:

- player controller
- game-state controller
- enemy spawner
- wave controller
- effect pool
- gun setup
- projectile versus hitscan configuration
- enemy fallback configuration
- player and camera references
- wave configuration
- spawn-zone IDs
- baked NavMesh

## Controls

The current desktop/gamepad input layer uses Unity's Input System.

- Move: WASD or gamepad left stick
- Aim: mouse, arrow keys, or gamepad right stick
- Fire: left mouse button or gamepad right trigger
- Twin-stick auto-fire: arrow keys or gamepad right stick above the configured threshold
- Reload: R or gamepad west face button
- Next weapon: E or gamepad right shoulder
- Previous weapon: Q or gamepad left shoulder
- Pause: Escape or gamepad Start

Mouse aiming projects the pointer onto a horizontal plane through the player. If no explicit aim camera is assigned, `PlayerController` falls back to `Camera.main`.

Runtime rebinding hooks are exposed by `PlayerController`; see `WEAPONS.md`.

## Create a reusable weapon

1. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
2. Click **Create Weapon Definition**.
3. Choose a fire mode: semi-auto, automatic, burst, or shotgun.
4. Choose projectile or hitscan delivery.
5. Configure damage, cadence, spread, ammo, reload, and delivery-specific settings.
6. Assign the asset to a `Gun`.

Existing guns remain backwards-compatible. If no Weapon Definition is assigned, `Gun` uses its original inline projectile, damage, fire-rate, and pool settings with infinite ammo.

## Add weapon switching

1. Add `WeaponLoadout` to the player.
2. Add authored `Gun` references to its weapon list.
3. Optionally assign a weapon mount.
4. Keep `PlayerShoot` on the player; it automatically uses the active loadout gun.
5. Use `WeaponPickup` for runtime weapon additions and `AmmoPickup` for reserve ammo.

## Create an enemy variant

1. Duplicate an existing enemy prefab.
2. Create or duplicate an `EnemyStats` asset.
3. Assign the stats asset to the enemy's `EnemyController`.
4. Add the prefab to the `EnemySpawner` weighted prefab list for legacy/global spawning, or reference it directly from a WaveSet enemy group.
5. Run validation again.

## Create reusable waves

1. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
2. Click **Create Wave Set**.
3. Add wave entries and tune enemy count, spawn interval, initial delay, and delay after.
4. Optionally add per-wave enemy groups with guaranteed counts and weights.
5. Optionally add a boss prefab/count.
6. Assign the Wave Set to `WaveController`.

## Add spawn zones

1. Create scene objects with `SpawnZone`.
2. Pick point-group, circle, or box shape.
3. Give each zone a stable ID.
4. Configure player-distance, off-screen, and NavMesh rules.
5. Add the zones to `EnemySpawner`.
6. Put the desired IDs in each wave's **Spawn Zone IDs** list.

See `WAVES.md` for composition rules and fallback behavior.

## Before publishing a game

- Run validation with zero errors.
- Verify the NavMesh is baked for every gameplay scene.
- Confirm every projectile weapon has a projectile prefab.
- Confirm hitscan masks/ranges match the intended targets.
- Test finite ammo, reload, empty-fire, switching, and pickups.
- Tune pool sizes for peak simultaneous objects.
- Test mouse, keyboard, and gamepad input.
- Test saved rebinding and reset-to-default.
- Run the included EditMode tests.
- Replace or license all sample presentation content appropriately.
