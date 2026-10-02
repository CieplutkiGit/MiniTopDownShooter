# Dependencies

## Supported Unity version

Version 1.0 targets **Unity 6000.3.10f1**.

Do not advertise support for an earlier Unity release until the clean-import and test suite have been run successfully on that exact version.

## Required packages

The product runtime/demo requires:

- AI Navigation 2.0.10
- Input System 1.18.0
- Universal Render Pipeline 17.3.0
- uGUI 2.0.0
- Unity Test Framework 1.6.0 for the included tests

TextMesh Pro UI resources are used by the demo project. If the final commercial export does not include the repository's copied TextMesh Pro Essential Resources, instruct customers to import TMP Essential Resources from Unity before opening the demo scenes.

## Removed development-only dependencies

The release-hardening pass removed these unused direct dependencies from the project manifest:

- Unity Version Control / Collab proxy
- JetBrains Rider integration
- Visual Studio integration
- Multiplayer Center
- Timeline
- Visual Scripting
- VContainer

They are not runtime requirements and should not be advertised as product dependencies.

## Export rule

Export only the product code, prefabs/data, demo content, documentation, and support assets actually required by the framework.

Do not include IDE integrations, collaboration tools, unrelated project packages, or third-party sample assets that are not required by the product.

See CLEAN_IMPORT.md and RELEASE_CHECKLIST.md before publishing.
