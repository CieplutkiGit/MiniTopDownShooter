# From arena demo to a complete game loop

Planning baseline: `eb0e6d8`, inspected 2026-10-03. Implementation tasks: [GAME_LOOP_TASKS.md](GAME_LOOP_TASKS.md).

## The playable slice

Launch into the player's small base, with a title/menu overlay. Enter the room, choose and customize an existing weapon at the bench, test it in an adjoining firing lane, then deploy from a mission terminal. Complete a finite arena mission, see the result, and return to the same base with the committed weapon builds intact.

**Menu → Base → Customize → Test → Deploy → Four waves + existing boss → Results → Base.** Customizing and testing are optional on subsequent runs. Death and abandoning a mission also return to base.

The first mission is **Arena Sweep**: clear all four authored waves and defeat the existing boss. Aim for a 3–5 minute run through pacing and balance. The decision loop is prepare a build, prove it against the mission, review the outcome, adjust, and try again. Saved personal bests and existing run statistics provide the initial reason to replay. Keep all five existing weapon platforms and their parts available; currency, unlock trees, crafting, procedural levels, and additional enemy types stay outside this pass.

## What the repository already provides

| Existing foundation | Gap to close |
| --- | --- |
| `GameStateManager`, `GameActivityPolicy`, pause/death/victory | Local states exist, but no owner connects hub, arena, and return journeys. `StartGame` currently cannot start from a workshop state. |
| `WorkshopSession`, preview, bench/range triggers, build persistence | The Unity runtime has no caller that constructs a session and binds `WorkshopUIController`. The composition root only initializes its state reference. |
| Master catalog and five modular weapon prefabs | Runtime catalog registration exists, but visual-profile lookup relies on editor fallback unless profiles are explicitly registered. |
| Authored four-wave arena, enemy archetypes, boss, pickups | It needs a briefing, readable objective/progress, deliberate pacing, and a reliable terminal result. |
| `GunAudio`, shot clips, mixer | Shipped gun prefabs contain neither `GunAudio` nor an `AudioSource`; the composition root does not wire gun audio. The existing listener follows one fixed gun. This is a likely cause of silent loadout weapons; confirm arena bindings in Unity. |
| `WorldResetManager`, profile save, high score | Restart starts combat immediately. Profile accounting is attached to selected state transitions rather than a unique run completion. |
| Mobile input and Mobile URP quality | Shared controls exist, but production touch layout, transition behavior, and device performance need verification. |
| Enemy/projectile/effect pools | Enemy and effect pools grow lazily. Pool storage sizes alone do not enforce simultaneous-object budgets. |

`ArenaShowcase.unity` is currently binary serialized, so its exact object references need Unity inspection. Resave that scene as text in the first implementation task, preserving its content and GUID. This planning pass has not reproduced the audio bug in Play Mode or run the test suite.

## Architecture and ownership

Keep the existing dependency direction: **Unity adapters → Application → Core**. Application remains free of Unity types. Add a small flow boundary; reuse the workshop, combat, wave, input, and save systems. Preserve the existing [workshop data and interface contracts](WEAPON_WORKSHOP_CONTRACTS.md).

| Owner | Responsibility | Lifetime |
| --- | --- | --- |
| `GameFlowCoordinator` in Application | Accept deploy/return commands, guard transitions, own the active run and its terminal result | App |
| `PlayerSession` in Application | Committed builds, ordered loadout IDs, equipped weapon ID, settings/profile snapshot | App; data only |
| `RunSession` / `RunResult` in Application | Unique run ID, frozen deployment loadout, elapsed active time, score/kills/waves, outcome | One run |
| `AppCompositionRoot` / `SceneFlowController` in Unity | Load saves/catalog once; execute asynchronous scene changes; bind scene roots | Boot scene |
| Existing `GameCompositionRoot` | Wire the scene's player, local state, UI, combat, pools, and injected app context | Scene |
| `WorkshopRuntimeController` in Unity | Create/bind a workshop session for the selected gun and preview; unbind on exit | Hub scene |
| `MissionDefinition` in Unity | Stable mission ID, display/briefing data, arena scene reference, existing WaveSet | Authored asset |
| `MissionRunController` in Unity | Prepare arena, apply deployment snapshot, start waves, report one terminal outcome | Arena scene |

Proposed new runtime files belong under `Assets/_Project/Application/Flow/` and `Assets/_Project/Unity/Flow/`; the workshop controller belongs beside the existing workshop adapters. Introduce only the contracts required by these owners. UI submits commands; scene loading and profile writes have a single owner each.

### Scenes

- **`Boot.unity`**: first production scene; app composition, the production EventSystem, and an unscaled loading/error overlay. Remains loaded. No player, gameplay camera, spawner, or audio listener.
- **`BaseHub.unity`**: one compact geometric room with weapon display/bench, adjoining firing lane, and deployment terminal. Reuse the player/camera and weapon assets. Local camera owns the single audio listener.
- **`ArenaShowcase.unity`**: reuse the existing arena as the first mission. Adapt its root to accept an injected run context. Maintain intentional direct-scene demo/testing support.
- Keep `SampleScene` and `MobileDemo` as development examples. Production uses the same touch-control prefab in hub and arena, rather than a separate mobile game flow.

Boot plus **one playable scene** is the normal runtime layout. For a change: lock input, clear queued actions, stop and detach the outgoing scene, unload it, load the destination additively, make it active, compose/prewarm its scene-owned objects, then enter the destination state. Do not keep both worlds resident during play. Every pooled instance must belong to its playable scene, never accidentally to Boot. Production scene canvases use Boot's EventSystem; direct demo entry retains a local fallback, with exactly one enabled EventSystem.

Reject duplicate deploy/return clicks. Ignore callbacks from an obsolete transition. Scene failure leaves the Boot overlay usable with retry/return recovery; it must not start waves or strand a frozen invisible player. Use unscaled time for loading and modal animation.

### Local activity states

Preserve existing enum values. Reuse `WorkshopRoaming` as hub roaming; append only `Loading` and `DeploymentBriefing` if needed. Application flow chooses the destination; local `GameStateManager` controls activity inside that scene.

| State | Move | Fire | Advance mission | World time |
| --- | --- | --- | --- | --- |
| Menu / Loading / DeploymentBriefing | No | No | No | Frozen |
| WorkshopRoaming | Yes | No | No | Running |
| WorkshopEditing | No | No | No | Frozen |
| WorkshopFiringRange | Yes | Yes | No | Running |
| Playing | Yes | Yes | Yes | Running |
| Paused / GameOver / Victory | No | No | No | Frozen |

Weapon selection must be allowed while roaming even though firing is blocked; do not continue using `CanFire` as the switching policy. Pausing restores the previous local activity, including workshop editing and briefing. A held fire/touch input must return to neutral before rearming after a modal, scene change, or interruption.

### Data and run rules

- Weapon IDs/builds cross scenes; player, gun, camera, UI, and pool references do not. Register the catalog and all five visual profiles explicitly in a player build.
- Applying edits updates the committed runtime build through the existing workshop contract. Discard/exit removes only unapplied edits. A save failure retains the pending committed build in `PlayerSession` and exposes a retry action; block deployment until committed builds are durably saved. Do not silently reload an older disk build over that pending build.
- Deployment copies the ordered loadout, active weapon ID, and committed builds into an immutable run snapshot. Arena setup restores health/ammo, applies that snapshot, then enables combat. Prevent `ResetToDefault` or a later save reread from replacing it.
- Practice uses the same weapon stats, sound, delivery, and reload behavior. Refill practice ammo on range entry/reset. Practice damage, kills, ammo, and elapsed time never enter mission/profile accounting; clear lane projectiles on exit.
- One accepted deployment creates one run. Track time only while `Playing`. Victory = all authored enemies/boss defeated; defeat = player death; abandon = explicit return from pause; setup/spawn failure = technical error.
- Finalize once by run ID. Wins/losses, kills, high score, and run count have one writer. Count victory/defeat/abandon as runs; only victory increments wins, only death increments losses; technical errors do not alter records. High score and accumulated kills come only from victory/defeat. Practice never counts.
- Results freeze the arena and display outcome, score, active time, kills, and waves cleared. The primary action is **Return to Base**. Restore the selected weapon and committed builds in the hub; clear all transient combat state.
- Persist results atomically with the last finalized run ID and existing profile migration/backup behavior. Keep failed writes pending in memory and retry explicitly; do not apply statistics twice. Cold launch always returns to base; this pass does not save a live mission.
- On mobile background/focus loss, clear input and pause activity. Returning to foreground requires explicit resume. Flush already committed settings/build/profile changes at meaningful boundaries, not per frame or per kill.

## Visual direction and mobile performance

Keep the current simple geometry, URP materials, restrained emissive accents, and readable combat effects. The base should feel intentional through layout, light, labels, and camera framing: bench on one side, firing lane on the other, deployment exit ahead. Share materials and reusable props. Keep gameplay readable at phone size; decorative detail must not hide enemies, bullets, or prompts.

Performance targets are **provisional acceptance budgets**, not measured claims: sustained 30 FPS on the chosen minimum phone and 60 FPS on a representative midrange phone, with roughly 20% CPU/GPU headroom. Record hardware, resolution, build configuration, and a 15-minute warm-device run before sign-off. Reference phones and supported OS versions still need to be selected.

- Start with a 20-enemy concurrent mission cap, including the boss, shared across platforms. Preserve wave totals by waiting for capacity; never drop a required spawn or turn capacity pressure into a technical loss. Tune against device captures.
- Size/prewarm projectile pools from maximum fire rate × lifetime × pellets plus enemy fire; guard aggregate active counts independently of retained pool sizes. Never consume ammunition for a projectile shot silently discarded by a limit. Cosmetic effects/debris may skip excess instances.
- Prewarm only the prefabs used by the mission during loading. Keep active enemies, projectiles, effects, trails, debris, subscriptions, and NavMesh activity owned by the scene; prove they clean up on return.
- Stagger NavMesh destination updates and avoid allocations in steady combat. Optimize the measured paths first; retain current Rigidbody/NavMesh systems. Avoid runtime material clones and unnecessary scene searches.
- Disable preview rendering outside workshop editing. Rebuild workshop lists and HUD text on relevant events, keep preview resolution bounded, and release its render texture on teardown.
- Keep mobile lights/shadows/postprocessing bounded. Measure a short shadow distance, few additional lights, shared Simple Lit materials where suitable, and reduced optional effects against the current look. Unity documents the costs of additional cameras, lights, and shadow passes in [URP performance guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/configure-for-better-performance.html).
- Verify Android/iOS actually select the authored Mobile URP quality. Profile on devices using the [Unity Profiler](https://docs.unity3d.com/6000.3/Documentation/Manual/profiler-profiling-applications.html). An empty [ObjectPool](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html) can create more instances; its retained-size limit is not a gameplay concurrency guarantee.

## Delivery order and acceptance

1. **Repair foundations — T01–T05:** shooting audio, guarded flow, session ownership, scene lifecycle, actual workshop binding.
2. **Complete the playable loop — T06–T09:** base, range, deployment/mission, results/return and durable records.
3. **Harden and polish — T10–T12:** mobile interaction/interruption, measured runtime performance, visual/UI and URP tuning.
4. **Release gate — T13:** full journey coverage, production scene/build configuration, repeated-cycle and device evidence.

The slice is done when a fresh player build starts in base, all five weapons can be customized/tested with audible shots, the deployed configuration matches the tested build, a real four-wave victory and death both return to base, records survive relaunch, and repeated journeys leave no duplicate services or growing object counts. Mobile sign-off additionally requires touch/interruption checks and measured sustained performance on the selected reference devices.

Future missions should be added by authoring a scene and `MissionDefinition`/WaveSet; future parts use the existing catalog. Neither should require rewriting flow, save ownership, or combat lifecycle.
