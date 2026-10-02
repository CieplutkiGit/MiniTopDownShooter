# Third-Party Notices

This file records third-party content currently visible in the repository and the release action required for each item.

The **final commercial export must be audited again**, because this repository contains development/demo resources that may intentionally be excluded from the sold package.

For version 1.0, the intended commercial export root is `Assets/_Project`. Root-level template/development content such as `Assets/TextMesh Pro`, `Assets/Settings`, `Assets/InputSystem_Actions.inputactions`, and `Assets/Readme.asset` is not part of the submission unless it is deliberately re-audited and moved into the product root.

## Liberation Sans

Repository files include:

- Assets/TextMesh Pro/Fonts/LiberationSans.ttf
- Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt
- generated TextMesh Pro LiberationSans SDF assets/materials

Copyright notices embedded in the repository license file include Google Corporation (2010) and Red Hat, Inc. (2012).

License: **SIL Open Font License 1.1**.

Release requirement: if Liberation Sans is redistributed, keep the included OFL license/copyright notice with the redistributed font software.

## EmojiOne sample resources

Repository files include:

- Assets/TextMesh Pro/Sprites/EmojiOne.png
- Assets/TextMesh Pro/Sprites/EmojiOne.json
- Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt
- Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset

The repository attribution file identifies EmojiOne as the source but does **not** embed a redistribution license; it instructs users to review EmojiOne licensing terms.

Release action: **exclude these EmojiOne sample resources from the commercial package unless redistribution rights for the exact included version have been independently verified**.

Do not treat the attribution file alone as proof of Asset Store redistribution rights.

## Unity / TextMesh Pro resources

The repository includes copied TextMesh Pro Essential Resources and shaders under Assets/TextMesh Pro.

Before release, choose one of these approaches:

1. exclude copied TMP Essential Resources and document that customers should import TMP Essential Resources from Unity, or
2. confirm that the exact Unity-provided resources may be redistributed in the chosen Asset Store package and retain all required notices.

Do not duplicate Unity package content in the commercial export unless it is actually required.

## Project-authored content

Current project-specific presentation content under Assets/_Project is primarily Unity primitives, generated materials/configuration and framework source.

Before release, re-audit every exported:

- mesh
- texture
- sprite/icon
- font
- audio clip
- shader
- material
- particle/VFX asset
- source-code dependency

For each third-party item that remains in the final export, record the source, author/publisher, license, required attribution and exact exported files here.
