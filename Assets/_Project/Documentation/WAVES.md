# Waves

## Wave Set

`WaveSet` is the reusable ScriptableObject for authored wave pacing and composition.

Create one using:

**Assets > Create > Mini Top Down Shooter > Wave Set**

or:

**Tools > Mini Top Down Shooter > Setup & Validation > Create Wave Set**

Assign the asset to a `WaveController`.

## Wave fields

### Enemy Count

Base number of non-boss enemies for the wave.

When explicit enemy groups are configured, guaranteed group counts are inserted first and weighted groups fill the remaining slots. If guaranteed counts already exceed **Enemy Count**, the guaranteed composition wins and the wave expands to fit it.

### Spawn Interval

Minimum time between spawn requests.

### Initial Delay

Delay after the wave starts before its first spawn request.

### Delay After

Delay between completing this wave and starting the next wave.

## Enemy groups

Each `WaveEnemyGroup` contains:

- **Prefab**: enemy prefab for the group
- **Guaranteed Count**: instances that must appear
- **Weight**: relative chance when filling remaining base enemy slots
- **Delay Before**: one-time delay before the first enemy from that group

If **Enemy Groups** is empty, the wave keeps the original behavior and asks `EnemySpawner` to choose from its global weighted prefab list.

This makes migration incremental: old WaveSets continue to work, while new WaveSets can author exact or weighted per-wave composition.

## Spawn zones

Add `SpawnZone` components to the scene and assign them to `EnemySpawner`.

Supported shapes:

- point group
- circle
- box

Each zone can configure:

- stable string ID
- selection weight
- minimum player distance
- optional maximum player distance
- off-screen requirement
- visibility camera override
- NavMesh sample radius
- candidate attempt count

A wave can list **Spawn Zone IDs**. When the list is non-empty, only matching zones participate in weighted zone selection. When it is empty, any configured zone can be used.

If no configured zone can produce a valid point, the spawner falls back to its original player-radius/NavMesh spawn logic so a bad zone does not permanently stall a wave. The editor validator warns about missing zone IDs.

## Boss entries

A wave can optionally set:

- **Boss Prefab**
- **Boss Count**
- **Boss Delay**

Bosses are appended after the wave's base enemy plan and count toward wave completion. Boss prefabs get pooled lazily even when they are not part of the spawner's global prefab list.

This is spawn/composition-level boss support. Boss phases, boss-specific UI, and bespoke boss behaviors remain separate gameplay modules.

## Runtime behavior

`WaveRunner` remains engine-independent and is responsible only for timing, spawn request count, kill completion, and wave transitions.

`WaveController` translates authored Unity data into a runtime spawn plan and resolves:

- explicit prefab selection
- group delays
- spawn-zone selection
- boss entries

`EnemySpawner` remains responsible for pooling and valid world placement.

## Backwards compatibility

`WaveController` still supports its original inline wave list.

When a `WaveSet` is assigned, it takes priority. If no `WaveSet` is assigned, inline waves are used.

A wave with no enemy groups and no spawn-zone IDs behaves like the previous system: global enemy weights plus legacy radius spawning.
