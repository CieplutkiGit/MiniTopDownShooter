# Continuous Integration

GitHub Actions runs the project through GameCI on pushes to main, pull requests, and manual dispatches.

The workflow performs:

1. Unity EditMode and PlayMode tests.
2. A Standalone Linux player smoke build containing all enabled demo scenes.
3. Upload of test results and the smoke build as workflow artifacts.

## Required repository secrets

Configure one of the supported Unity licensing paths before expecting the workflow to pass.

For a Unity Personal license, configure:

- UNITY_LICENSE
- UNITY_EMAIL
- UNITY_PASSWORD

For a Unity Professional/Plus-style serial workflow, configure:

- UNITY_SERIAL
- UNITY_EMAIL
- UNITY_PASSWORD

The workflow deliberately fails its preflight when neither UNITY_LICENSE nor UNITY_SERIAL is present. This prevents a missing-license setup problem from looking like a project compile failure.

## Branch protection

After the workflow has passed at least once, make the Unity CI test and build jobs required status checks for main.

Do not merge framework changes when either EditMode/PlayMode tests or the smoke player build fails.
