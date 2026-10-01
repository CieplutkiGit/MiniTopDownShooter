# Changelog

## Unreleased - Asset Store readiness

### Added

- Reusable `WeaponDefinition` ScriptableObject
- Multi-projectile shots
- Configurable projectile spread
- Asset Store setup and validation editor window
- Open-scene checks for core gameplay objects, gun setup, enemy spawning, waves, and NavMesh
- Buyer-facing quick start and weapon documentation

### Changed

- `Gun` can use a Weapon Definition while preserving existing inline serialized values as backwards-compatible defaults
- Gun startup reports missing projectile and spawn-point configuration clearly

## 1.0.0

- Initial playable top-down shooter foundation
- Player movement and aiming
- Projectile combat
- Enemy AI and pooling
- Waves and scoring
- Menus, UI, audio, and gameplay feedback
