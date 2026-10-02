# Mini Top Down Shooter

Mini Top Down Shooter is a lightweight Unity 6 framework and playable demo for building 3D top-down and twin-stick shooters.

The package focuses on a small, understandable runtime rather than taking over a customer's project. Core gameplay code is separated from Unity-facing code, enemies and effects are pooled, and common game systems are exposed through focused components and interfaces.

## Included systems

- Rigidbody player movement and rotation
- Mouse, keyboard, and gamepad aiming/input
- Saved runtime input rebinding hooks
- Reusable Weapon Definition assets
- Semi-auto, automatic, burst, and shotgun firing modes
- Projectile and hitscan weapon delivery
- Finite/infinite ammo, reload, empty-fire, ammo events, and ammo pickups
- Multi-weapon loadouts, next/previous switching, and weapon pickups
- Projectile pooling with per-weapon speed/lifetime overrides
- Multi-projectile spread, spread growth, and recovery
- Optional hitscan penetration and damage falloff
- Health and damage
- Enemy chase, attack, and death states
- NavMesh enemy movement
- Weighted enemy spawning with pooling
- Point, circle, and box spawn zones with distance/off-screen/NavMesh constraints
- Reusable Wave Set assets with per-wave enemy groups, weights, delays, zones, and boss entries
- Score and high score
- Pause, menu, game-over, and HUD logic
- Audio hooks
- Screen shake, hit flash, muzzle flash, projectile trails, impact FX, and death FX
- Editor setup and validation tools
- EditMode tests for core health, waves, and weapon ammo state

## Recommended Unity version

Developed with Unity 6000.3.10f1.

## First steps

1. Open `Assets/Scenes/SampleScene.unity`.
2. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
3. Click **Validate Open Scene**.
4. Enter Play Mode.
5. Create reusable weapon data with **Create Weapon Definition** and assign the asset to a `Gun`.
6. Create reusable wave composition with **Create Wave Set** and optionally add `SpawnZone` components.

See `QUICK_START.md` for setup, `WEAPONS.md` for weapon authoring, and `WAVES.md` for reusable wave composition.

## Design goal

The demo is a reference implementation, not a project-settings takeover. Buyers should be able to replace art, create weapon variants, create enemy variants, tune waves, and integrate the runtime into an existing project without rewriting the framework.
