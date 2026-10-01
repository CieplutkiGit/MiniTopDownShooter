# Extending Mini Top Down Shooter

## Architecture

The project is separated into three assemblies:

- `Core`: engine-independent gameplay rules such as health and damage
- `Application`: orchestration and interfaces such as waves, state providers, and score tracking
- `Unity`: MonoBehaviours, ScriptableObjects, input, pooling, and presentation

Keep reusable gameplay rules out of MonoBehaviours when they do not need Unity APIs.

## Add an enemy variant

For a stat variant:

1. Duplicate an existing enemy prefab.
2. Create or duplicate an `EnemyStats` asset.
3. Assign the stats to the enemy.
4. Add the prefab to the `EnemySpawner` weighted list.

For fundamentally different behavior, add a focused state or controller rather than growing a single controller with many mode flags.

## Add feedback

Presentation is intentionally separated from core rules. Existing examples include `GunAudio`, `MuzzleFlashFX`, `EnemyHitFX`, `EnemyDeathFX`, `PlayerHitFX`, and `ScreenShake`.

Prefer subscribing presentation components to gameplay events instead of embedding audio or particles directly into core logic.

## Pooling

Projectiles, enemies, particle effects, and death debris use pooling. When adding frequently spawned objects, make their reset behavior explicit so pooled instances do not retain stale state.

## Extension guidelines

For buyer-facing systems:

- expose a small interface or event where practical
- keep serialized scene references private
- use ScriptableObjects for shared authoring data
- validate required references in `OnValidate` or the editor validator
- avoid global singleton dependencies where direct references or interfaces are sufficient
