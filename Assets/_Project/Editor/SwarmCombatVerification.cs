using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

public static class SwarmCombatVerification
{
    public static void CaptureAndVerify()
    {
        Game.Editor.SwarmCombatVerification.CaptureAndVerify();
    }
}

namespace Game.Editor
{
    /// <summary>
    /// Demonstrable visual and runtime verification runner for combat destruction.
    /// Captures staged enemy piece loss and body cutouts, bullet holes, prop fragmentation,
    /// lethal breakup, respawn restoration, and explosion delivery under .utmp/swarm-combat.
    /// </summary>
    public static class SwarmCombatVerification
    {
        private const string OutputDirectory = ".utmp/swarm-combat";

        [MenuItem("Tools/Mini Top Down Shooter/Run Swarm Combat Verification")]
        public static void RunFromMenu()
        {
            CaptureAndVerifyInternal(isBatchMode: false);
        }

        public static void CaptureAndVerify()
        {
            CaptureAndVerifyInternal(isBatchMode: UnityEngine.Application.isBatchMode);
        }

        private static void CaptureAndVerifyInternal(bool isBatchMode)
        {
            int exitCode = 0;
            string reportPath = Path.Combine(OutputDirectory, "report.json");
            GameObject verificationRoot = null;
            Material floorMat = null;
            Material enemyMat = null;
            Material propMat = null;

            bool enemyPieceLossVerified = false;
            bool enemyRestoreVerified = false;
            bool propDestructionVerified = false;
            bool explosiveDeliveryVerified = false;
            bool debrisPoolBounded = false;
            bool impactMarksBounded = false;

            int initialEnemyPieces = 0;
            int piecesAfterStage1 = 0;
            int piecesAfterStage2 = 0;
            int restoredPieces = 0;

            int initialEnemyTris = 0;
            int trisAfterStage1 = 0;
            int trisAfterStage2 = 0;
            int restoredTris = 0;

            try
            {
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }

                Debug.Log("[SwarmCombatVerification] Starting verification capture sequence...");

                // Setup verification scene root
                verificationRoot = new GameObject("VerificationRoot");

                // Setup Camera
                GameObject camGo = new GameObject("VerificationCamera");
                camGo.transform.SetParent(verificationRoot.transform);
                camGo.transform.position = new Vector3(0f, 3.2f, -4.5f);
                camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
                cam.fieldOfView = 50f;

                // Setup directional light
                GameObject lightGo = new GameObject("VerificationLight");
                lightGo.transform.SetParent(verificationRoot.transform);
                Light dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.color = Color.white;
                dirLight.intensity = 1.2f;
                lightGo.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

                // Setup Floor
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
                floor.transform.SetParent(verificationRoot.transform);
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(20f, 1f, 20f);
                floorMat = new Material(Shader.Find("Sprites/Default"));
                floorMat.color = new Color(0.18f, 0.20f, 0.24f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

                // Setup Production Enemy with full component stack
                GameObject enemyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                enemyGo.name = "Enemy_Showcase";
                enemyGo.transform.SetParent(verificationRoot.transform);
                enemyGo.transform.position = new Vector3(-1.2f, 0.6f, 0f);
                enemyGo.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                enemyMat = new Material(Shader.Find("Sprites/Default"));
                enemyMat.color = new Color(0.85f, 0.22f, 0.22f); // Red geometric enemy
                enemyGo.GetComponent<MeshRenderer>().sharedMaterial = enemyMat;

                // Authored child breakaway pieces
                for (int i = 0; i < 3; i++)
                {
                    GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.name = $"AuthoredArmor_{i + 1}";
                    piece.transform.SetParent(enemyGo.transform);
                    piece.transform.localPosition = new Vector3((i - 1) * 0.35f, 0.6f, 0.45f);
                    piece.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    piece.GetComponent<MeshRenderer>().sharedMaterial = enemyMat;
                }

                enemyGo.AddComponent<EnemyMovement>();
                enemyGo.AddComponent<EnemyAttack>();
                EnemyController enemyController = enemyGo.AddComponent<EnemyController>();
                enemyController.EnsureInitialized();
                HealthComponent enemyHealth = enemyGo.GetComponent<HealthComponent>();
                enemyHealth.SetMaxHealth(100);

                bool enemyDiedFired = false;
                enemyController.Died += _ => enemyDiedFired = true;

                EnemyDamageVisuals enemyVisuals = enemyGo.GetComponent<EnemyDamageVisuals>();
                if (enemyVisuals == null) enemyVisuals = enemyGo.AddComponent<EnemyDamageVisuals>();
                enemyVisuals.InitializeVisuals();
                enemyVisuals.EnsureSubscribed();

                EnemyDeathDebris enemyDeathDebris = enemyGo.AddComponent<EnemyDeathDebris>();
                enemyDeathDebris.EnsureInitialized();

                // Setup Destructible Prop
                GameObject propGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                propGo.name = "CornerBlock_Cover";
                propGo.transform.SetParent(verificationRoot.transform);
                propGo.transform.position = new Vector3(1.4f, 0.6f, 0f);
                propGo.transform.localScale = new Vector3(1f, 1.2f, 1f);
                propMat = new Material(Shader.Find("Sprites/Default"));
                propMat.color = new Color(0.35f, 0.45f, 0.55f); // Blue-gray cover prop
                propGo.GetComponent<MeshRenderer>().sharedMaterial = propMat;

                DestructibleProp prop = propGo.AddComponent<DestructibleProp>();
                prop.InitializeProp();
                prop.Configure(80, debrisStage: 3, debrisDeath: 10);

                Physics.SyncTransforms();

                // Ensure pools exist
                _ = CombatImpactPool.Instance;
                _ = CombatDebrisPool.Instance;

                // 1. Capture pristine state
                CaptureFrame(cam, Path.Combine(OutputDirectory, "01_pristine_before_combat.png"));
                initialEnemyPieces = enemyVisuals.ActivePieceCount;
                initialEnemyTris = enemyVisuals.RenderedTriangleCount;

                // 2. Deliver hit 1: Enemy takes damage -> 3D bullet marks and face cutout appear
                Vector3 enemyHitPoint = enemyGo.transform.position + new Vector3(0.1f, 0.2f, -0.45f);
                enemyHealth.TakeDamage(new DamageData(15), new HitContext(enemyHitPoint, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "02_enemy_hit_marks.png"));

                // 3. Deliver hit 2: Enemy HP drops below 75% -> Stage 1 piece loss & body cutout
                enemyHealth.TakeDamage(new DamageData(20), new HitContext(enemyHitPoint + Vector3.up * 0.2f, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "03_enemy_piece_loss_stage1.png"));
                piecesAfterStage1 = enemyVisuals.ActivePieceCount;
                trisAfterStage1 = enemyVisuals.RenderedTriangleCount;

                // 4. Deliver hit 3: Enemy HP drops below 50% -> Stage 2 piece loss & body cutout
                enemyHealth.TakeDamage(new DamageData(25), new HitContext(enemyHitPoint - Vector3.right * 0.2f, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "04_enemy_piece_loss_stage2.png"));
                piecesAfterStage2 = enemyVisuals.ActivePieceCount;
                trisAfterStage2 = enemyVisuals.RenderedTriangleCount;

                // 5. Deliver hit 4: Lethal damage to enemy -> lethal breakup into debris
                enemyHealth.TakeDamage(new DamageData(50), new HitContext(enemyHitPoint, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "05_enemy_death_breakup.png"));

                // 6. Respawn enemy via controller -> verify full restoration of all pieces & marks cleared
                enemyController.Spawn(new Vector3(-1.2f, 0.6f, 0f), null);
                CaptureFrame(cam, Path.Combine(OutputDirectory, "06_enemy_respawn_restored.png"));
                restoredPieces = enemyVisuals.ActivePieceCount;
                restoredTris = enemyVisuals.RenderedTriangleCount;
                // Record health at the respawn checkpoint, before later explosion
                // scenarios can legitimately damage this enemy again.
                bool enemyHealthRestored = enemyHealth != null && enemyHealth.CurrentHealth == 100;

                // 7. Shoot destructible prop -> persistent 3D marks & surface indentation
                Vector3 propHitPoint = propGo.transform.position + new Vector3(-0.1f, 0.2f, -0.5f);
                prop.TakeDamage(new DamageData(20), new HitContext(propHitPoint, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "07_prop_hit_marks.png"));

                // 8. Continued damage to prop -> Stage fragmentation & missing geometry
                prop.TakeDamage(new DamageData(30), new HitContext(propHitPoint + Vector3.up * 0.15f, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "08_prop_fragmented.png"));

                // 9. Lethal damage to prop -> full destruction & removal
                prop.TakeDamage(new DamageData(40), new HitContext(propHitPoint, Vector3.back, Vector3.forward));
                CaptureFrame(cam, Path.Combine(OutputDirectory, "09_prop_destroyed_removed.png"));
                propDestructionVerified = prop.IsDestroyed && !propGo.activeSelf;

                // 10. Measure and verify Explosive weapon delivery
                GameObject explosiveTargetGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                explosiveTargetGo.name = "ExplosiveTargetProp";
                explosiveTargetGo.transform.SetParent(verificationRoot.transform);
                explosiveTargetGo.transform.position = new Vector3(0f, 0.5f, 0.5f);
                DestructibleProp explosiveTargetProp = explosiveTargetGo.AddComponent<DestructibleProp>();
                explosiveTargetProp.InitializeProp();
                explosiveTargetProp.Configure(60);

                ExplosiveWeaponDelivery explosiveDelivery = new ExplosiveWeaponDelivery(radius: 3.5f, range: 10f);
                GameObject detonateSpawn = new GameObject("ExplosiveSpawn");
                detonateSpawn.transform.position = new Vector3(0f, 0.5f, -3f);
                detonateSpawn.transform.SetParent(verificationRoot.transform);

                Physics.SyncTransforms();

                int propHealthBeforeExplosion = explosiveTargetProp.CurrentHealth;
                explosiveDelivery.Deliver(detonateSpawn.transform, Vector3.forward, 50, null, null, null);
                explosiveDeliveryVerified = explosiveTargetProp.CurrentHealth < propHealthBeforeExplosion;

                CaptureFrame(cam, Path.Combine(OutputDirectory, "10_explosion_destruction.png"));

                // 11. Measure Debris pool cap enforcement
                for (int i = 0; i < 75; i++)
                {
                    CombatDebrisPool.Instance.SpawnChunk(Vector3.up, Vector3.up, Vector3.zero, 2f, 0.2f);
                }
                debrisPoolBounded = CombatDebrisPool.Instance.ActiveCount <= CombatDebrisPool.Instance.MaxActiveDebris;

                // 12. Measure Impact Marks surface cap enforcement (35 shots on one surface)
                for (int i = 0; i < 35; i++)
                {
                    CombatImpactPool.SpawnMark(floor.transform.position, Vector3.up, Vector3.down, floor.transform);
                }
                impactMarksBounded = CombatImpactPool.Instance.ActiveMarkCount <= CombatImpactPool.Instance.GlobalMaxMarks;

                enemyPieceLossVerified = (piecesAfterStage1 < initialEnemyPieces || trisAfterStage1 < initialEnemyTris) &&
                                         (piecesAfterStage2 < piecesAfterStage1 || trisAfterStage2 < trisAfterStage1);
                bool enemyDeathVerified = enemyDiedFired;
                enemyRestoreVerified = (restoredPieces == initialEnemyPieces) &&
                                       (restoredTris == initialEnemyTris) &&
                                       (restoredPieces > 0 || restoredTris > 0) &&
                                       enemyHealthRestored;

                bool allVerified =
                    enemyPieceLossVerified &&
                    enemyDeathVerified &&
                    enemyRestoreVerified &&
                    propDestructionVerified &&
                    explosiveDeliveryVerified &&
                    debrisPoolBounded &&
                    impactMarksBounded;

                string status = allVerified ? "success" : "failed";

                string summaryJson = $@"{{
  ""status"": ""{status}"",
  ""timestamp"": ""{DateTime.UtcNow:O}"",
  ""verified"": {{
    ""enemyInitialPieces"": {initialEnemyPieces},
    ""enemyPiecesStage1"": {piecesAfterStage1},
    ""enemyPiecesStage2"": {piecesAfterStage2},
    ""enemyRestoredPieces"": {restoredPieces},
    ""enemyInitialTriangles"": {initialEnemyTris},
    ""enemyTrianglesStage1"": {trisAfterStage1},
    ""enemyTrianglesStage2"": {trisAfterStage2},
    ""enemyRestoredTriangles"": {restoredTris},
    ""enemyPieceLossVerified"": {enemyPieceLossVerified.ToString().ToLower()},
    ""enemyDeathVerified"": {enemyDeathVerified.ToString().ToLower()},
    ""enemyHealthRestored"": {enemyHealthRestored.ToString().ToLower()},
    ""enemyRestoreVerified"": {enemyRestoreVerified.ToString().ToLower()},
    ""propDestructionVerified"": {propDestructionVerified.ToString().ToLower()},
    ""explosiveDeliveryVerified"": {explosiveDeliveryVerified.ToString().ToLower()},
    ""debrisPoolBounded"": {debrisPoolBounded.ToString().ToLower()},
    ""impactMarksBounded"": {impactMarksBounded.ToString().ToLower()}
  }},
  ""captures"": [
    ""01_pristine_before_combat.png"",
    ""02_enemy_hit_marks.png"",
    ""03_enemy_piece_loss_stage1.png"",
    ""04_enemy_piece_loss_stage2.png"",
    ""05_enemy_death_breakup.png"",
    ""06_enemy_respawn_restored.png"",
    ""07_prop_hit_marks.png"",
    ""08_prop_fragmented.png"",
    ""09_prop_destroyed_removed.png"",
    ""10_explosion_destruction.png""
  ]
}}";
                File.WriteAllText(reportPath, summaryJson);

                if (!allVerified)
                {
                    Debug.LogError("[SwarmCombatVerification] One or more verification assertions failed!");
                    exitCode = 1;
                }
                else
                {
                    Debug.Log($"[SwarmCombatVerification] Verification completed successfully! Report: {reportPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SwarmCombatVerification] Exception occurred: {ex}");
                exitCode = 1;
                string failJson = $@"{{
  ""status"": ""failed"",
  ""timestamp"": ""{DateTime.UtcNow:O}"",
  ""error"": ""{ex.Message.Replace("\"", "\\\"")}""
}}";
                File.WriteAllText(reportPath, failJson);
            }
            finally
            {
                if (verificationRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(verificationRoot);
                }
                if (floorMat != null) UnityEngine.Object.DestroyImmediate(floorMat);
                if (enemyMat != null) UnityEngine.Object.DestroyImmediate(enemyMat);
                if (propMat != null) UnityEngine.Object.DestroyImmediate(propMat);

                CombatImpactPool.Instance?.ClearAndDestroyAll();
                CombatDebrisPool.Instance?.ClearAndDestroyAll();
            }

            if (isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static void CaptureFrame(Camera cam, string destinationPath)
        {
            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(screenShot);

            File.WriteAllBytes(destinationPath, bytes);
        }
    }
}
