# Clean Project Import

Run this procedure against the exact package that will be sold.

## Baseline project

Create a brand-new project using the supported Unity version listed in DEPENDENCIES.md.

Install only the documented dependencies.

Do not copy the development repository's ProjectSettings or Packages folder into the clean project unless the product explicitly requires those settings.

## Import procedure

1. Import the commercial package.
2. Wait for compilation to finish.
3. Confirm there are zero compile errors.
4. Resolve only the documented package dependencies.
5. Import TMP Essential Resources if the commercial package intentionally excludes the repository copy.
6. Open SampleScene.
7. Run Tools > Mini Top Down Shooter > Validate Open Scene.
8. Open ArenaShowcase and validate it.
9. Open MobileDemo and validate it.
10. Run all EditMode tests.
11. Run all PlayMode tests.

## Functional smoke test

Verify:

- mouse movement/aim/fire
- keyboard movement, reload and switching
- gamepad movement/aim/fire
- mobile dual-stick input
- pause/resume
- restart and game-over
- legacy Gun behavior
- WeaponDefinition presets
- projectile and hitscan damage
- friendly-fire filtering
- ammo/reload
- weapon switching and pickups
- WaveSet composition
- SpawnZone selection
- ranged/rusher/tank/charger/boss enemies
- pooled respawn/reset behavior

## Import safety

The product must not unexpectedly overwrite:

- customer Input Actions
- rendering pipeline settings
- tags/layers
- physics collision matrix
- quality settings
- player settings
- package manifest

Any required project-setting change must be documented and opt-in.

## Pass criteria

The release passes clean import only when:

- zero compile errors
- zero missing-script references
- demo scenes open correctly
- tests pass
- validation has zero errors
- no undocumented dependency is required
- no destructive project-setting takeover occurs
