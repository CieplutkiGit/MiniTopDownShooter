# Upgrade Guide

## General rule

Back up or commit the customer project before importing a framework update.

Prefer importing updates into a test branch first, especially when the release changes ScriptableObject fields, prefabs, assembly names, namespaces, or scene wiring.

## WeaponDefinition changes

Existing legacy Gun components remain supported when no WeaponDefinition is assigned.

When upgrading a WeaponDefinition:

1. inspect the asset after import
2. confirm fire mode and delivery mode
3. confirm finite/infinite ammo settings
4. confirm projectile or hitscan configuration
5. confirm spread, burst, reload, pool and falloff fields
6. enter Play Mode and verify ammo/reload events still drive custom HUD/audio code

Do not mutate WeaponDefinition assets at runtime; per-instance ammo lives in WeaponAmmoState.

## WaveSet changes

WaveController still supports legacy inline waves when no WaveSet is assigned.

When moving an older scene to WaveSet:

1. create or assign a WaveSet
2. copy pacing values
3. add explicit enemy groups only where required
4. add SpawnZone IDs only after matching SpawnZone components exist in the scene
5. validate boss prefab/count combinations
6. run scene validation

## Enemy behavior changes

The default EnemyController path remains the original melee chase/attack behavior.

Custom archetypes are opt-in EnemyBehaviorBase modules. When an update changes behavior modules, verify pooled respawn resets and any custom prefab references.

## Combat affiliation

Player and enemy examples use DamageAffiliation.

Legacy projects without DamageAffiliation can still fall back to Player/Enemy layers, but new integrations should add explicit affiliations so friendly-fire behavior does not depend on project-layer configuration.

## Input bindings

Runtime binding overrides are persisted through PlayerPrefs.

If an update changes action bindings or binding indexes, use the framework reset-to-default path during migration testing so stale local overrides do not hide configuration errors.

## Serialized API migrations

Changes to namespaces, assembly names, or serialized component types should be shipped as dedicated migration releases and verified in a copy of a customer-style project.

Never bulk-edit customer prefab/scene YAML by hand.

## Recommended upgrade test

After every framework upgrade:

- open Unity and wait for a clean compile
- run EditMode tests
- run PlayMode tests
- validate every gameplay scene
- enter each shipped demo scene
- test pause/restart/game-over
- test legacy Gun and WeaponDefinition
- test legacy inline waves and WaveSet
- test pooling after repeated deaths/restarts
- test input rebinding reset and persistence
