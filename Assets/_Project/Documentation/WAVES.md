# Waves

## Wave Set

`WaveSet` is the reusable ScriptableObject for wave pacing.

Create one using:

**Assets > Create > Mini Top Down Shooter > Wave Set**

or:

**Tools > Mini Top Down Shooter > Setup & Validation > Create Wave Set**

Assign the resulting asset to a `WaveController`.

## Wave fields

### Enemy Count

How many enemies must be spawned and killed before the wave completes.

### Spawn Interval

Minimum time between spawn requests.

### Delay After

Delay between completing this wave and starting the next one.

## Backwards compatibility

`WaveController` still supports its original inline wave list.

When a Wave Set is assigned, the reusable asset takes priority. If no Wave Set is assigned, the inline list is used.

## Enemy composition

Wave timing and enemy composition are intentionally separate in the current version.

- `WaveSet` controls pacing and counts.
- `EnemySpawner` controls the available enemy prefabs and their relative weights.

This keeps the base system small. A future wave-composition module can add per-wave enemy groups, bosses, or spawn zones without changing `WaveRunner`'s core responsibilities.
