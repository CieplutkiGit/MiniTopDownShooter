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
- enemy prefab configuration
- player and camera references
- wave configuration
- baked NavMesh

## Controls

The current demo uses Unity's Input System.

- Move: WASD or gamepad left stick
- Aim: mouse, arrow keys, or gamepad right stick
- Fire: left mouse button, or aim with arrow keys / gamepad right stick
- Pause: Escape or gamepad Start

Mouse aiming projects the pointer onto a horizontal plane through the player. If no explicit aim camera is assigned, `PlayerController` falls back to `Camera.main`.

## Create a reusable weapon

1. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
2. Click **Create Weapon Definition**.
3. Configure projectile prefab, damage, fire interval, projectiles per shot, spread, and pool sizes.
4. Assign the asset to a `Gun`.

Existing guns remain backwards-compatible. If no Weapon Definition is assigned, `Gun` uses its original inline projectile, damage, fire-rate, and pool settings.

## Create an enemy variant

1. Duplicate an existing enemy prefab.
2. Create or duplicate an `EnemyStats` asset.
3. Assign the stats asset to the enemy's `EnemyController`.
4. Add the prefab to the `EnemySpawner` weighted prefab list.
5. Run validation again.

## Create reusable waves

1. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
2. Click **Create Wave Set**.
3. Add wave entries and tune enemy count, spawn interval, and delay after each wave.
4. Assign the Wave Set to `WaveController`.

A Wave Set overrides the controller's inline wave list. Existing scenes using inline waves remain compatible.

See `WAVES.md` for details.

## Before publishing a game

- Run validation with zero errors.
- Verify the NavMesh is baked for every gameplay scene.
- Confirm every gun has a valid projectile configuration.
- Tune pool sizes for peak simultaneous objects.
- Test mouse, keyboard, and gamepad input.
- Run the included EditMode tests.
- Replace or license all sample presentation content appropriately.
