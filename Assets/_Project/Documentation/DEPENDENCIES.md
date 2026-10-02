# Dependencies

The repository is developed with Unity 6000.3.10f1.

The current gameplay/demo project uses Unity packages including:

- Input System
- AI Navigation
- Universal Render Pipeline
- uGUI / TextMesh Pro integration

The repository manifest also contains workspace and editor tooling used during development. Those should not automatically be advertised as runtime requirements for the Asset Store package.

## Export rule

Before submission, export only the product code, prefabs/data, demo content, documentation, and support assets actually required by the product.

Test the exported `.unitypackage` in a clean project using the minimum Unity version you plan to support. The clean-project test should confirm:

- no missing scripts
- no compile errors
- no unexpected project-setting overwrite
- documented dependencies install cleanly
- the demo scene opens with no missing references
