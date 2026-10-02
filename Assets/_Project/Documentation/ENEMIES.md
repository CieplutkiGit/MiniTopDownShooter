# Enemy Archetypes

The original melee chase enemy remains the default behavior. EnemyController only switches to a custom behavior when an EnemyBehaviorBase component is assigned or present on the prefab.

This keeps the existing prefab path backwards-compatible while allowing focused behavior modules for enemies that need different combat logic.

## Melee / chase

Use the base EnemyController with no custom behavior component.

EnemyStats controls move speed, max health, melee damage, attack range, attack cooldown, and score value. The original chase and attack states remain unchanged.

## Ranged shooter

Add RangedEnemyBehavior and assign a Gun.

The behavior approaches when outside preferred range, stops near preferred range, retreats when the player is too close, and fires through the same Gun, WeaponDefinition, projectile/hitscan, damage, pooling, and feedback system used by the player.

Recommended setup:

1. Add a child Gun with a spawn point.
2. Assign a projectile or hitscan Weapon Definition.
3. Set the Gun to infinite ammo for a simple enemy shooter.
4. Add RangedEnemyBehavior to the enemy root.
5. Assign the Gun.
6. Tune retreat, preferred, and fire ranges.

## Fast / rusher

Add RusherEnemyBehavior.

The rusher uses normal NavMesh movement and melee attack, but applies a configurable speed multiplier while outside close range. Pair it with a lower-health, higher-move-speed EnemyStats asset for a clear fast-enemy role.

## Tank / bruiser

Use a high-health, lower-speed EnemyStats asset and add EnemyArmor.

EnemyArmor implements the reusable IDamageModifier hook and supports a damage multiplier, flat reduction, and minimum damage per hit.

Damage modifiers are resolved by HealthComponent before damage enters the engine-independent Health model.

## Charger

Add ChargerEnemyBehavior.

The charger has four phases: approach, windup, charge, and recovery. It exposes charge ranges, timing, speed multiplier, overshoot distance, and impact range.

Charge timing uses absolute timestamps rather than frame delta, so it behaves correctly even though enemy AI thinks at a lower frequency than the render loop.

## Boss example

Add BossPhaseController to any enemy prefab.

Each phase has a health-fraction threshold, movement speed multiplier, objects to enable, and objects to disable. The controller raises PhaseChanged(int) whenever the resolved phase changes.

Phase objects can enable extra Guns, alternate VFX, weak points, shields, phase-specific helpers, or different visuals.

BossPhaseController resets phase presentation when a pooled boss is disabled, so reused boss instances do not retain the previous run's phase objects.

## Pooling rules

All archetypes continue to use EnemySpawner pools.

Behavior components receive a fresh runtime context from EnemyController.Spawn(...) on every reuse and get OnDespawn() when the pooled enemy is disabled.

When writing a new behavior:

1. derive from EnemyBehaviorBase
2. reset temporary state in OnSpawn()
3. perform decision logic in Tick()
4. stop or reset special state in OnDeath() and OnDespawn()
5. keep presentation in separate event listeners/components where practical

## Wave composition

Any archetype can be used directly in a WaveEnemyGroup or as a boss prefab.

This supports authored combinations such as rushers first, ranged enemies from rear zones, bruisers mixed into later waves, charger mini-bosses, and phased bosses.
