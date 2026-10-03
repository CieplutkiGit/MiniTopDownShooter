# Original User Request

## 2026-10-03T19:48:08Z

This is a single self-contained fix; keep it small and focused.
Requested team: Small focused team (single implementer with adversarial review passes)

Restore player weapon shooting and visible projectile emission across all scenes so that firing any equipped weapon—including the starter pistol—spawns and propels visible projectiles the same as other weapons have been doing, accurately hitting targets and applying damage with responsive player controls and zero test regressions.

Working directory: c:\Users\jerz\UnityProjects\MiniTopDownShooter
Integrity mode: demo

## Requirements

### R1. Starter Weapon Availability & Loadout Mounting
Ensure that a valid unlocked weapon (defaulting to the starter pistol) is present on the player and equipped automatically upon loading into any gameplay scene (direct scene play or deployment runs), ensuring the player is never unarmed when starting a mission.

### R2. Visible Projectile Firing Consistent Across Weapons
Ensure weapon firing (including the starter pistol) spawns and launches visible projectiles in the aim direction—matching the delivery style of the other project weapons—that travel through physical space, register collisions, damage destructible obstacles and enemy units, and trigger appropriate impact and audio feedback.

### R3. Unimpeded Aim & Fire Controls
Ensure left-click firing, continuous fire holding, and rotation/aiming register reliably during gameplay without being intercepted or blocked by non-interactive UI elements or transparent HUD raycast targets.

### R4. Test Suite and Regression Verification
Maintain full test suite stability across the project. Ensure all unit, integration, and playmode tests pass cleanly without errors, regressions, or null references.

## Acceptance Criteria

### Combat & Firing Behavior
- [ ] On mission start in all gameplay scenes, the player holds an equipped, ready-to-fire weapon.
- [ ] Firing the weapon spawns visible projectiles that travel forward along the aim vector, consistent with other weapons in the game.
- [ ] Projectiles collide with and deal damage to valid target colliders, destructible props, and enemy targets.
- [ ] Firing responds directly to mouse clicks and hold inputs anywhere on the gameplay screen without UI obstruction.

### Verification & Testing
- [ ] All 490+ EditMode tests pass cleanly with 0 failures (`Unity.exe -runTests -testPlatform editmode`).
- [ ] PlayMode integration tests pass with 0 failures (`Unity.exe -runTests -testPlatform playmode`).
