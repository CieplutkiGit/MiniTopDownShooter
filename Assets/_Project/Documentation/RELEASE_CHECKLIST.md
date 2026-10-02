# Release Checklist

Use this checklist for every Asset Store upload.

## Code and tests

- [ ] Supported Unity version is explicit.
- [ ] Project compiles with zero errors.
- [ ] EditMode tests pass.
- [ ] PlayMode smoke tests pass.
- [ ] GitHub Actions Unity CI passes.
- [ ] No TODO/FIXME release blockers remain.
- [ ] Public APIs used by examples are documented.
- [ ] Backwards compatibility notes are current.

## Demo content

- [ ] SampleScene opens and validates.
- [ ] ArenaShowcase opens and validates.
- [ ] MobileDemo opens and validates.
- [ ] NavMesh is baked in every gameplay demo.
- [ ] No missing prefab/material/audio references.
- [ ] Weapon presets demonstrate meaningfully different modes.
- [ ] Enemy archetypes are visible in the showcase.
- [ ] Boss entry and SpawnZones work.
- [ ] Pause/restart/game-over work after repeated runs.

## Input and platforms

- [ ] Mouse/keyboard tested.
- [ ] Gamepad tested.
- [ ] Runtime rebinding save/reset tested.
- [ ] Multi-touch move + aim/fire tested.
- [ ] Safe area tested on notched/rounded device profiles.
- [ ] Mobile performance tested on the documented reference device.

## Package hygiene

- [ ] Commercial export uses one product root.
- [ ] Only true runtime/editor dependencies are documented.
- [ ] Development-only dependencies are excluded.
- [ ] No destructive ProjectSettings takeover.
- [ ] Namespace/assembly names are collision-safe.
- [ ] Export contains no duplicate GUIDs.
- [ ] Clean-project import passes.

## Licensing

- [ ] THIRD_PARTY_NOTICES.md matches the actual exported files.
- [ ] Every redistributed third-party asset has a verified redistribution license.
- [ ] Required attribution/license files are included.
- [ ] Unverified EmojiOne sample assets are excluded unless separately cleared.
- [ ] Fonts, icons, audio, meshes, materials, shaders and code dependencies are audited.

## Store release

- [ ] Version number finalized.
- [ ] Changelog updated.
- [ ] Upgrade guide updated.
- [ ] Screenshots match the current package.
- [ ] Store copy lists only verified features/dependencies.
- [ ] Documentation links work.
- [ ] Support contact is correct.
- [ ] Final package file has been re-imported and smoke-tested.
