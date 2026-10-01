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
- player reference on the spawner
- wave configuration
- baked NavMesh

## Controls

The current demo uses Unity's Input System.

- Move: WASD or gamepad left stick
- Aim: arrow keys or gamepad right stick
- Pause: Escape or gamepad Start

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

## Configure waves

Each `WaveController` entry controls:

- enemy count
- spawn interval
- delay after the wave

The `EnemySpawner` controls which enemy variants appear and their relative weights.

## Before publishing a game

- Run validation with zero errors.
- Verify the NavMesh is baked for every gameplay scene.
- Confirm every gun has a valid projectile configuration.
- Tune pool sizes for peak simultaneous objects.
- Test keyboard and gamepad input.
- Replace or license all sample presentation content appropriately.
