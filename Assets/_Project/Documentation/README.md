# Mini Top Down Shooter

Mini Top Down Shooter is a lightweight Unity 6 framework and playable demo for building 3D top-down and twin-stick shooters.

The package focuses on a small, understandable runtime rather than taking over a customer's project. Core gameplay code is separated from Unity-facing code, enemies and effects are pooled, and common game systems are exposed through focused components and interfaces.

## Included systems

- Rigidbody player movement and rotation
- Keyboard and gamepad twin-stick input
- Reusable Weapon Definition assets
- Projectile pooling
- Multi-projectile spread support
- Health and damage
- Enemy chase, attack, and death states
- NavMesh enemy movement
- Weighted enemy spawning with pooling
- Configurable waves
- Score and high score
- Pause, menu, game-over, and HUD logic
- Audio hooks
- Screen shake, hit flash, muzzle flash, projectile trails, impact FX, and death FX
- Editor setup and validation tools

## Recommended Unity version

Developed with Unity 6000.3.10f1.

## First steps

1. Open `Assets/Scenes/SampleScene.unity`.
2. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
3. Click **Validate Open Scene**.
4. Enter Play Mode.
5. Create reusable weapon data with **Create Weapon Definition** and assign the asset to a `Gun`.

See `QUICK_START.md` for setup and `WEAPONS.md` for weapon authoring.

## Design goal

The demo is a reference implementation, not a project-settings takeover. Buyers should be able to replace art, create weapon variants, create enemy variants, and tune waves without rewriting the framework.
