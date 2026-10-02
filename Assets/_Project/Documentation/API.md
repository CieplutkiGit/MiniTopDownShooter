# Public API and Extension Points

This document summarizes the main buyer-facing runtime APIs. Source remains the authority for exact signatures.

## Weapons

### Gun

Primary runtime weapon component.

Common members:

- HandleTrigger(direction, isHeld, wasPressed)
- Shoot(direction)
- Reload()
- CancelReload()
- AddAmmo(amount)
- SetEquipped(equipped)
- ResetRuntimeState()
- AmmoInMagazine
- ReserveAmmo
- MagazineSize
- IsReloading
- Definition

Events:

- Fired
- EmptyFired
- ReloadStarted
- ReloadCompleted
- ReloadCanceled
- AmmoChanged
- Equipped
- Unequipped

### WeaponDefinition

Reusable weapon configuration.

Use ScriptableObjects for authored data and keep runtime state in Gun/WeaponAmmoState.

### WeaponLoadout

Use for authored slots, runtime weapon addition, direct equip and next/previous switching.

### DamageAffiliation

Defines Player, Enemy or Neutral combat team and friendly-fire behavior.

Use this rather than hard-coding collision-layer assumptions in custom weapons.

## Damage and health

### IDamageable

Implement for objects that can receive DamageData.

### IDamageModifier

Implement for armor/resistance-style preprocessing before Health receives damage.

### HealthComponent

Unity-facing health bridge with OnDead and OnHealthChanged events.

## Enemies

### EnemyBehaviorBase

Extension base for custom enemy archetypes.

Override:

- Tick()
- OnSpawn()
- OnDeath()
- OnDespawn()

Use the supplied Movement, Attack, Health, Target and TargetDamageable context instead of duplicating EnemyController.

### EnemyController

Owns pooled spawn/reset integration and chooses the legacy melee state machine or an optional EnemyBehaviorBase.

### EnemySpawner

Supports global weighted prefabs and explicit per-wave prefab spawning.

SpawnOne() preserves the legacy ISpawner path.

SpawnOne(prefab, allowedZoneIds) is used by authored WaveSets.

## Waves

### WaveSet

Reusable authored wave data.

### WaveController

Turns WaveSet/inline configuration into engine-independent WaveRunner data and explicit spawn plans.

Events:

- WaveStarted
- WaveCompleted
- AllWavesCompleted

### SpawnZone

Reusable point/circle/box placement area with weighting, player-distance, off-screen and NavMesh rules.

## Input

### PlayerController

Exposes runtime rebinding helpers:

- BeginInputRebind(actionName, bindingIndex)
- GetBindingDisplayString(actionName, bindingIndex)
- ResetInputBindings()

### MobileInputState

Shared virtual input model for reusable touch controls.

### MobileJoystick / MobileActionButton

UI input components that write into MobileInputState.

## Game state

### GameStateController

Owns start/pause/resume/menu/game-over state transitions through the Application-layer game-state manager.

## Presentation events

Prefer subscribing presentation code to gameplay events rather than adding audio/UI/VFX concerns to core gameplay components.

Typical extension points include:

- Gun events
- health events
- player/enemy death events
- wave events
- boss PhaseChanged
- loadout events
