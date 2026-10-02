# Replacing the Demo Art

The framework is designed so gameplay logic does not depend on the sample visuals.

## Player

Keep the gameplay root with PlayerController, movement, health, affiliation and weapon components.

Replace child renderers/models freely. If the replacement model has a different forward direction, adjust the visual child transform rather than changing shooting logic.

## Enemies

Keep EnemyController, EnemyMovement, EnemyAttack, HealthComponent and the selected behavior module on the gameplay root.

Replace:

- MeshFilter / SkinnedMeshRenderer
- materials
- animation presentation
- hit/death visual helpers

Do not remove the NavMeshAgent or damage collider unless replacing them with an equivalent gameplay setup.

## Projectiles

The projectile prefab needs:

- Projectile
- a trigger collider appropriate for the visual
- any renderer/trail/impact presentation required by the game

WeaponDefinition can override runtime projectile speed and lifetime.

## Particles and impact effects

Presentation components can reference replacement ParticleSystem prefabs.

Keep gameplay damage outside visual-effect scripts so effects can be removed without changing combat.

## Audio

Replace shot, hit, death, UI and music clips on their presentation components.

Weapon and gameplay events are intended as extension points for custom audio middleware.

## UI

Replace sprites, fonts, colors and layout while keeping the HUD/pause/game-over controllers or reconnecting their public/event hooks.

For mobile, preserve the MobileInputState and button/joystick components even when replacing every visual element.

## Materials and rendering pipeline

The shipped demos target URP.

When replacing materials, verify the replacement shaders support the customer's target render pipeline. Gameplay code does not require a particular material or shader.

## Verification

After replacing art:

1. run scene validation
2. test projectile colliders against the new visuals
3. test enemy NavMesh radius/height
4. test camera framing
5. test mobile safe-area layout
6. test pooling to ensure presentation state resets correctly
