# Asset Store Submission Sheet

This document is the source of truth for version 1.0 Publisher Portal copy and marketing capture. Verify all fields against the final exported package before submission.

## Product identity

**Title:** Mini Top Down Shooter

**Tagline:** Modular 3D top-down and twin-stick shooter framework for Unity 6.

**Version:** 1.0.0

**Commercial package root:** `Assets/_Project`

**Supported Unity version for 1.0:** Unity 6000.3.10f1

## Short description

Build 3D top-down and twin-stick shooters with reusable weapons, enemy archetypes, authored waves, desktop/gamepad/touch input, mobile controls, pooling, editor setup tools, demo scenes, tests, and documented extension points.

## Long description

Mini Top Down Shooter is a compact Unity 6 framework and playable reference implementation for 3D top-down and twin-stick shooters.

The package is designed to add focused gameplay systems without taking over a customer's project. Core gameplay logic is separated from Unity-facing components, common configuration is authored with reusable data assets, enemies and presentation effects use pooling, and the included editor tools help create and validate common setups.

### Weapons and combat

- Reusable WeaponDefinition assets.
- Pistol, rifle, SMG, shotgun, and launcher examples.
- Semi-automatic, automatic, burst, and shotgun firing.
- Projectile and hitscan delivery.
- Finite or infinite ammo, reload timing, auto-reload, empty-fire events, and ammo pickups.
- Multi-weapon loadouts, direct/next/previous switching, and weapon pickups.
- Spread, spread growth/recovery, projectile speed/lifetime overrides, hitscan penetration, and optional damage falloff.
- Team-aware damage affiliation and friendly-fire filtering.
- Health plus optional damage modifiers such as armor.

### Enemies, spawning, and waves

- Legacy melee chase enemy plus ranged, rusher, charger, tank/armor, and boss examples.
- NavMesh movement and pooled spawning.
- Point, circle, and box SpawnZones with weighting, player-distance, off-screen, and NavMesh constraints.
- Reusable WaveSet assets with per-wave enemy groups, weights, delays, zone selection, and boss entries.
- EnemyBehaviorBase extension path for custom archetypes.
- Boss phase example with phase-change events and pooled reset behavior.

### Input and mobile

- Mouse and keyboard support.
- Gamepad/twin-stick support.
- Saved runtime input rebinding hooks.
- Dual-stick mobile controls with touch ownership and configurable aim-to-fire.
- Mobile action buttons for fire, reload, weapon switching, and pause.
- Safe-area support and platform-aware mobile control visibility.

### Game flow and presentation hooks

- Score and high score.
- Pause, menu, restart, and game-over flow.
- Audio hooks.
- Screen shake, hit flash, muzzle flash, projectile trails, impact effects, and death effects.
- Event-driven extension points so custom presentation can subscribe without being embedded in gameplay logic.

### Editor workflow and examples

- Setup & Validation editor window.
- One-click Player, Enemy, Gun, Arena, Spawn Zone, and Wave Controller creation.
- Common-reference repair.
- Custom inspectors for WeaponDefinition, WaveSet, EnemyStats, and EnemySpawner.
- Demo scenes for the minimal learning path, full arena showcase, and mobile controls.
- EditMode tests and PlayMode demo-scene smoke tests.
- Local documentation for quick start, weapons, waves, enemies, mobile controls, public API, extension, upgrading, clean import, art replacement, CI, licensing, and release verification.

## Requirements and dependencies

Version 1.0 is supported on **Unity 6000.3.10f1** until additional editor versions are explicitly verified.

Required Unity packages used by the runtime/demo:

- AI Navigation 2.0.10
- Input System 1.18.0
- Universal Render Pipeline 17.3.0
- uGUI 2.0.0

Unity Test Framework 1.6.0 is required only for the included tests.

The demo UI uses TextMesh Pro. The commercial export intentionally excludes the repository's root-level copied TMP sample/resources. If TMP Essential Resources are not already present in the customer project, import them from Unity before opening the demo scenes.

## Scope and limitations to disclose

- The package is a gameplay framework/reference implementation, not a finished game's art pack.
- Demo presentation primarily uses simple/project-authored presentation content intended to be replaced.
- Version 1.0 support is limited to Unity 6000.3.10f1 until other versions are explicitly tested.
- The package does not provide networking or multiplayer infrastructure.
- The package does not require an external service or AI service at runtime.
- Root-level development/template assets in the repository are not part of the commercial export.

## Suggested keywords

top down shooter, twin stick shooter, shooter framework, shooter template, weapon system, wave system, enemy AI, mobile controls, gamepad, input system, navmesh, pooling, hitscan, projectile, Unity 6

## AI description field — draft

OpenAI ChatGPT was used to assist with functional source code, editor tooling, automated test/CI and release-preflight scripts, documentation, package organization, and release hardening. The shipped package does not call an AI service and does not require AI functionality at runtime. Review this disclosure before submission and add any other AI tools used during production.

## Screenshot and key-image capture plan

Capture marketing media from the exact final package after clean-import verification.

1. **Cover/key image:** ArenaShowcase gameplay with the product title and a short tagline only.
2. **Combat variety:** player using several shipped weapon presets; make projectile/hitscan and spread differences visually understandable.
3. **Enemy archetypes:** ranged, rusher/charger, tank, and boss examples visible in one showcase.
4. **Wave authoring:** WaveSet inspector plus SpawnZones visible in Scene view.
5. **Mobile:** MobileDemo with dual sticks, action buttons, and safe-area layout.
6. **Setup tooling:** Setup & Validation window with quick-create actions and a clean validation result.
7. **Reusable data:** WeaponDefinition and EnemyStats custom inspectors.
8. **Architecture/docs:** clean Project window view of the single `Assets/_Project` root and local documentation.

Do not add sale/discount graphics. Key images should accurately reflect the package and should not replace actual screenshots.

## Publisher Portal fields still requiring publisher input

- Publisher name/profile.
- Support email/contact.
- Final category selection.
- Final price.
- Final keywords after portal validation.
- Final key images/screenshots.
- Final AI description verification.
- Links to any external support/documentation pages, if used.

## Final submission gate

Do not submit until RELEASE_CHECKLIST.md is complete against the exact exported package, including real Unity CI execution, clean-project import, device/input testing, third-party audit, final media, and re-import smoke testing.
