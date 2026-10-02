# Continuous Integration

GitHub Actions runs a license-free Asset Store preflight on pushes to main, pull requests, and manual dispatches. Unity tests and the Linux smoke build run when valid Unity license secrets are available.

The workflow performs:

1. Static Asset Store/package preflight without requiring Unity.
2. Unity EditMode and PlayMode tests when licensing is configured.
3. A Standalone Linux player smoke build containing all enabled demo scenes when licensing is configured.
4. Upload of Unity test results and the smoke build as workflow artifacts.

## Static Asset Store preflight

`.github/scripts/release_preflight.py` checks the repository for common submission blockers, including:

- all commercial content living under `Assets/_Project`
- all three demo scenes being present under the product root and referenced by Build Settings
- required buyer documentation
- missing `.meta` files and duplicate Unity GUIDs
- Asset Store path length limits
- TODO/FIXME release blockers
- forbidden archives/executables and accidental TMP/EmojiOne content inside the product root
- collision-prone assembly names
- development-only direct package dependencies

This preflight is useful even before Unity licensing is configured, but it does not prove that the project compiles or that Unity tests pass.

## Required repository secrets

For a Unity Personal/manual license workflow, configure:

- UNITY_LICENSE

For a Unity serial workflow, configure all of:

- UNITY_SERIAL
- UNITY_EMAIL
- UNITY_PASSWORD

When neither valid licensing path is configured, the static preflight still runs and the workflow reports a notice while Unity tests/builds are skipped.

A green workflow with skipped Unity jobs is **not** sufficient release evidence. Before submission, confirm the EditMode/PlayMode test job actually ran and the Linux smoke build completed successfully.

## Branch protection

After Unity licensing is configured and the workflow has passed with real Unity execution, make the Asset Store preflight and Unity test/build checks required for main.

Do not merge framework changes when the preflight fails, Unity tests fail, or the smoke player build fails.
