using System;
using System.Collections.Generic;
using System.IO;
using Game;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GeometricArenaPolishSetup
{
    private const string MaterialsFolder = "Assets/_Project/Materials";
    private const string ArenaScenePath = "Assets/Scenes/ArenaShowcase.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Mini Top Down Shooter/Apply Geometric Arena Polish")]
    public static void ApplyPolish()
    {
        Debug.Log("[GeometricArenaPolish] Starting full geometric polish setup...");

        EnsureFolder(MaterialsFolder);

        // 1. Create or update shared materials
        var materials = CreateSharedMaterials();

        // 2. Setup Weapon Prefabs
        SetupWeaponPrefabs(materials);

        // 3. Setup Enemy Prefabs
        SetupEnemyPrefabs(materials);

        // 4. Setup Projectile and FX Prefabs
        SetupCombatFXPrefabs(materials);

        // 5. Setup ArenaShowcase scene
        SetupArenaShowcaseScene(materials);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GeometricArenaPolish] Full geometric polish applied successfully!");
    }

    public static void ApplyPolishBatch()
    {
        try
        {
            ApplyPolish();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeometricArenaPolish] Batch execution failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Mini Top Down Shooter/Validate Arena Showcase")]
    public static void ValidateArenaShowcase()
    {
        EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        MiniTopDownShooterWindow.ValidateFromMenu();
    }

    public static void ValidateArenaShowcaseBatch()
    {
        try
        {
            ValidateArenaShowcase();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeometricArenaPolish] Validation failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Mini Top Down Shooter/Inspect Scene Hierarchy")]
    public static void InspectSceneHierarchy()
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            PrintHierarchy(root, 0);
        }
    }

    private static void PrintHierarchy(GameObject go, int depth)
    {
        string indent = new string(' ', depth * 2);
        Renderer r = go.GetComponent<Renderer>();
        string rendInfo = r != null ? $" [Renderer: enabled={r.enabled}, type={r.GetType().Name}, mat={r.sharedMaterial?.name}]" : "";
        MeshFilter mf = go.GetComponent<MeshFilter>();
        string meshInfo = mf != null ? $" [Mesh: {mf.sharedMesh?.name}]" : "";

        var comps = go.GetComponents<Component>();
        List<string> cNames = new List<string>();
        foreach (var c in comps)
        {
            if (c != null && !(c is Transform)) cNames.Add(c.GetType().Name);
        }
        string compInfo = cNames.Count > 0 ? $" ({string.Join(", ", cNames)})" : "";

        Debug.Log($"{indent}- {go.name} at {go.transform.position}{rendInfo}{meshInfo}{compInfo}");
        for (int i = 0; i < go.transform.childCount; i++)
        {
            PrintHierarchy(go.transform.GetChild(i).gameObject, depth + 1);
        }
    }

    public static void InspectHierarchyBatch()
    {
        try
        {
            InspectSceneHierarchy();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeometricArenaPolish] Inspect failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    public static void PrintUIDebugBatch()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    Debug.Log($"[UIDebug] Path: {GetPath(t.transform)}, Text: '{t.text}', Active: {t.gameObject.activeInHierarchy}, LocalPos: {t.transform.localPosition}");
                }
            }
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UIDebug] Failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    private static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }

    public class MaterialPalette
    {
        public Material ArenaFloor;
        public Material ArenaFloorAccent;
        public Material ArenaWall;
        public Material ArenaWallTrim;
        public Material ArenaCover;
        public Material ArenaPillarAccent;
        public Material ArenaSpawnPad;

        public Material PlayerBody;
        public Material PlayerVisor;
        public Material PlayerDark;

        public Material WeaponMetal;
        public Material WeaponAccent;
        public Material WeaponGlow;

        public Material EnemyBasic;
        public Material EnemyFast;
        public Material EnemyCharger;
        public Material EnemyRanged;
        public Material EnemyTank;
        public Material EnemyTankArmor;
        public Material EnemyBossBase;
        public Material EnemyBossGlow;
        public Material EnemyEye;

        public Material FXPlayerBullet;
        public Material FXEnemyBullet;
        public Material FXPlayerParticle;
        public Material FXEnemyParticle;
        public Material FXTelegraph;
    }

    public static MaterialPalette CreateSharedMaterials()
    {
        EnsureFolder(MaterialsFolder);
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = unlitShader;

        MaterialPalette p = new MaterialPalette();

        // Environment
        p.ArenaFloor = GetOrCreateMaterial("Mat_Arena_Floor", litShader, new Color(0.086f, 0.094f, 0.11f), 0.12f, 0.0f);
        p.ArenaFloorAccent = GetOrCreateMaterial("Mat_Arena_FloorAccent", litShader, new Color(0.13f, 0.15f, 0.18f), 0.2f, 0.05f);
        p.ArenaWall = GetOrCreateMaterial("Mat_Arena_Wall", litShader, new Color(0.10f, 0.11f, 0.13f), 0.18f, 0.1f);
        p.ArenaWallTrim = GetOrCreateMaterial("Mat_Arena_WallTrim", litShader, new Color(0.16f, 0.19f, 0.24f), 0.4f, 0.2f, new Color(0.0f, 0.35f, 0.45f) * 1.2f);
        p.ArenaCover = GetOrCreateMaterial("Mat_Arena_Cover", litShader, new Color(0.16f, 0.18f, 0.22f), 0.25f, 0.15f);
        p.ArenaPillarAccent = GetOrCreateMaterial("Mat_Arena_PillarAccent", litShader, new Color(0.05f, 0.12f, 0.16f), 0.5f, 0.1f, new Color(0.0f, 0.9f, 1.0f) * 2.2f);
        p.ArenaSpawnPad = GetOrCreateMaterial("Mat_Arena_SpawnPad", litShader, new Color(0.12f, 0.12f, 0.15f), 0.2f, 0.05f, new Color(0.9f, 0.35f, 0.05f) * 0.8f);

        // Player
        p.PlayerBody = GetOrCreateMaterial("Mat_Player_Body", litShader, new Color(0.0f, 0.78f, 0.95f), 0.35f, 0.1f);
        p.PlayerVisor = GetOrCreateMaterial("Mat_Player_Visor", litShader, new Color(0.4f, 0.95f, 1.0f), 0.6f, 0.0f, new Color(0.0f, 0.95f, 1.0f) * 2.8f);
        p.PlayerDark = GetOrCreateMaterial("Mat_Player_Dark", litShader, new Color(0.09f, 0.10f, 0.12f), 0.4f, 0.25f);

        // Weapons
        p.WeaponMetal = GetOrCreateMaterial("Mat_Weapon_Metal", litShader, new Color(0.14f, 0.15f, 0.18f), 0.55f, 0.5f);
        p.WeaponAccent = GetOrCreateMaterial("Mat_Weapon_Accent", litShader, new Color(0.22f, 0.24f, 0.28f), 0.3f, 0.1f);
        p.WeaponGlow = GetOrCreateMaterial("Mat_Weapon_Glow", litShader, new Color(0.0f, 0.85f, 1.0f), 0.5f, 0.0f, new Color(0.0f, 0.85f, 1.0f) * 2.0f);

        // Enemies
        p.EnemyBasic = GetOrCreateMaterial("Mat_Enemy_Basic", litShader, new Color(0.85f, 0.32f, 0.08f), 0.25f, 0.05f);
        p.EnemyFast = GetOrCreateMaterial("Mat_Enemy_Fast", litShader, new Color(0.96f, 0.65f, 0.04f), 0.35f, 0.1f);
        p.EnemyCharger = GetOrCreateMaterial("Mat_Enemy_Charger", litShader, new Color(0.82f, 0.12f, 0.12f), 0.25f, 0.1f);
        p.EnemyRanged = GetOrCreateMaterial("Mat_Enemy_Ranged", litShader, new Color(0.55f, 0.18f, 0.75f), 0.35f, 0.15f);
        p.EnemyTank = GetOrCreateMaterial("Mat_Enemy_Tank", litShader, new Color(0.42f, 0.28f, 0.22f), 0.4f, 0.25f);
        p.EnemyTankArmor = GetOrCreateMaterial("Mat_Enemy_TankArmor", litShader, new Color(0.24f, 0.28f, 0.33f), 0.45f, 0.5f);
        p.EnemyBossBase = GetOrCreateMaterial("Mat_Enemy_Boss_Base", litShader, new Color(0.09f, 0.08f, 0.11f), 0.35f, 0.2f);
        p.EnemyBossGlow = GetOrCreateMaterial("Mat_Enemy_Boss_Glow", litShader, new Color(1.0f, 0.28f, 0.0f), 0.6f, 0.0f, new Color(1.0f, 0.35f, 0.0f) * 3.0f);
        p.EnemyEye = GetOrCreateMaterial("Mat_Enemy_Eye", litShader, new Color(1.0f, 0.08f, 0.22f), 0.5f, 0.0f, new Color(1.0f, 0.1f, 0.25f) * 2.5f);

        // Projectiles & FX
        p.FXPlayerBullet = GetOrCreateMaterial("Mat_FX_PlayerBullet", litShader, new Color(0.0f, 0.95f, 1.0f), 0.5f, 0.0f, new Color(0.0f, 0.95f, 1.0f) * 3.0f);
        p.FXEnemyBullet = GetOrCreateMaterial("Mat_FX_EnemyBullet", litShader, new Color(1.0f, 0.35f, 0.05f), 0.5f, 0.0f, new Color(1.0f, 0.4f, 0.05f) * 3.0f);

        p.FXPlayerParticle = GetOrCreateParticleMaterial("Mat_FX_PlayerParticle", particleShader, new Color(0.2f, 0.95f, 1f, 1f), true);
        p.FXEnemyParticle = GetOrCreateParticleMaterial("Mat_FX_EnemyParticle", particleShader, new Color(1.0f, 0.55f, 0.1f, 1f), true);
        p.FXTelegraph = GetOrCreateTelegraphMaterial("Mat_FX_Telegraph", particleShader, new Color(1.0f, 0.2f, 0.05f, 0.65f));

        AssetDatabase.SaveAssets();
        return p;
    }

    private static Material GetOrCreateMaterial(string name, Shader shader, Color baseColor, float smoothness, float metallic, Color? emission = null)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        if (emission.HasValue && emission.Value != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string name, Shader shader, Color color, bool additive = false)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", additive ? 1 : 0);   // 0 = Alpha, 1 = Additive
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (additive) mat.EnableKeyword("_BLENDMODE_ADD");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateTelegraphMaterial(string name, Shader shader, Color color)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    public static void SetupWeaponPrefabs(MaterialPalette p)
    {
        SetupSingleWeapon("Assets/_Project/Weapons/Gun_Pistol.prefab", p, WeaponType.Pistol);
        SetupSingleWeapon("Assets/_Project/Weapons/Gun_Rifle.prefab", p, WeaponType.Rifle);
        SetupSingleWeapon("Assets/_Project/Weapons/Gun_SMG.prefab", p, WeaponType.SMG);
        SetupSingleWeapon("Assets/_Project/Weapons/Gun_Shotgun.prefab", p, WeaponType.Shotgun);
        SetupSingleWeapon("Assets/_Project/Weapons/Gun_Launcher.prefab", p, WeaponType.Launcher);
    }

    private enum WeaponType { Pistol, Rifle, SMG, Shotgun, Launcher }

    private static void SetupSingleWeapon(string prefabPath, MaterialPalette p, WeaponType type)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"Could not load prefab at {prefabPath}");
            return;
        }

        try
        {
            Gun gun = root.GetComponent<Gun>();
            if (gun == null) gun = root.AddComponent<Gun>();

            // Remove existing Visual and Muzzle if re-authoring
            Transform existingVisual = root.transform.Find("Visual");
            if (existingVisual != null) Object.DestroyImmediate(existingVisual.gameObject);

            Transform existingMuzzle = root.transform.Find("Muzzle");
            if (existingMuzzle != null) Object.DestroyImmediate(existingMuzzle.gameObject);

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(root.transform, false);

            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(root.transform, false);

            Vector3 muzzlePos = Vector3.forward * 0.5f;

            switch (type)
            {
                case WeaponType.Pistol:
                    CreateVisualPrimitive("Frame", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0f, 0.05f), new Vector3(0.12f, 0.15f, 0.32f), p.WeaponMetal);
                    CreateVisualPrimitive("Slide", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.10f, 0.07f), new Vector3(0.11f, 0.08f, 0.36f), p.WeaponAccent);
                    CreateVisualPrimitive("Barrel", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.10f, 0.28f), new Vector3(0.07f, 0.07f, 0.12f), p.WeaponMetal);
                    CreateVisualPrimitive("Grip", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.12f, -0.05f), new Vector3(0.10f, 0.20f, 0.12f), p.WeaponAccent, Quaternion.Euler(15f, 0f, 0f));
                    CreateVisualPrimitive("SightDot", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.15f, 0.24f), new Vector3(0.03f, 0.03f, 0.03f), p.WeaponGlow);
                    muzzlePos = new Vector3(0f, 0.10f, 0.36f);
                    break;

                case WeaponType.Rifle:
                    CreateVisualPrimitive("Receiver", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.13f, 0.18f, 0.44f), p.WeaponMetal);
                    CreateVisualPrimitive("Barrel", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.08f, 0.38f), new Vector3(0.08f, 0.08f, 0.44f), p.WeaponMetal);
                    CreateVisualPrimitive("Handguard", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.05f, 0.25f), new Vector3(0.14f, 0.14f, 0.30f), p.WeaponAccent);
                    CreateVisualPrimitive("Stock", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.02f, -0.32f), new Vector3(0.11f, 0.16f, 0.28f), p.WeaponAccent);
                    CreateVisualPrimitive("Magazine", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.14f, 0.06f), new Vector3(0.08f, 0.26f, 0.12f), p.WeaponAccent, Quaternion.Euler(-12f, 0f, 0f));
                    CreateVisualPrimitive("Grip", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.12f, -0.12f), new Vector3(0.09f, 0.18f, 0.10f), p.WeaponMetal, Quaternion.Euler(18f, 0f, 0f));
                    CreateVisualPrimitive("OpticRail", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.06f, 0.04f, 0.32f), p.WeaponGlow);
                    muzzlePos = new Vector3(0f, 0.08f, 0.62f);
                    break;

                case WeaponType.SMG:
                    CreateVisualPrimitive("Body", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.04f, 0.02f), new Vector3(0.13f, 0.17f, 0.32f), p.WeaponMetal);
                    CreateVisualPrimitive("BarrelShroud", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.06f, 0.24f), new Vector3(0.11f, 0.11f, 0.20f), p.WeaponAccent);
                    CreateVisualPrimitive("Foregrip", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.10f, 0.22f), new Vector3(0.07f, 0.18f, 0.07f), p.WeaponAccent);
                    CreateVisualPrimitive("MainGrip", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.10f, -0.08f), new Vector3(0.09f, 0.18f, 0.09f), p.WeaponMetal, Quaternion.Euler(15f, 0f, 0f));
                    CreateVisualPrimitive("StickMag", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.18f, 0.06f), new Vector3(0.07f, 0.32f, 0.08f), p.WeaponAccent, Quaternion.Euler(-10f, 0f, 0f));
                    CreateVisualPrimitive("TopSight", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.14f, 0.05f), new Vector3(0.05f, 0.04f, 0.22f), p.WeaponGlow);
                    muzzlePos = new Vector3(0f, 0.06f, 0.36f);
                    break;

                case WeaponType.Shotgun:
                    CreateVisualPrimitive("Receiver", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.16f, 0.20f, 0.38f), p.WeaponMetal);
                    CreateVisualPrimitive("TwinBarrel", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.08f, 0.35f), new Vector3(0.18f, 0.10f, 0.50f), p.WeaponMetal);
                    CreateVisualPrimitive("PumpSlide", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.01f, 0.25f), new Vector3(0.16f, 0.11f, 0.22f), p.WeaponAccent);
                    CreateVisualPrimitive("SolidStock", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.02f, -0.32f), new Vector3(0.13f, 0.18f, 0.34f), p.WeaponAccent);
                    CreateVisualPrimitive("Grip", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.12f, -0.10f), new Vector3(0.10f, 0.18f, 0.11f), p.WeaponMetal, Quaternion.Euler(20f, 0f, 0f));
                    muzzlePos = new Vector3(0f, 0.08f, 0.62f);
                    break;

                case WeaponType.Launcher:
                    CreateVisualPrimitive("CannonTube", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.06f, 0.15f), new Vector3(0.24f, 0.24f, 0.55f), p.WeaponMetal);
                    CreateVisualPrimitive("RevolvingDrum", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.04f, -0.10f), new Vector3(0.30f, 0.30f, 0.26f), p.WeaponAccent, Quaternion.Euler(0f, 0f, 45f));
                    CreateVisualPrimitive("FrontHandle", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.16f, 0.22f), new Vector3(0.09f, 0.20f, 0.09f), p.WeaponMetal);
                    CreateVisualPrimitive("RearHandle", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, -0.16f, -0.18f), new Vector3(0.09f, 0.20f, 0.09f), p.WeaponMetal);
                    CreateVisualPrimitive("SightMount", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.22f, 0.05f), new Vector3(0.08f, 0.08f, 0.16f), p.WeaponGlow);
                    muzzlePos = new Vector3(0f, 0.06f, 0.44f);
                    break;
            }

            muzzleObj.transform.localPosition = muzzlePos;

            // Setup MuzzleFlashFX with particle unlit material
            MuzzleFlashFX flashFX = root.GetComponent<MuzzleFlashFX>();
            if (flashFX == null) flashFX = root.AddComponent<MuzzleFlashFX>();

            ParticleSystem muzzlePS = muzzleObj.GetComponent<ParticleSystem>();
            if (muzzlePS == null) muzzlePS = muzzleObj.AddComponent<ParticleSystem>();
            ConfigureMuzzleParticleSystem(muzzlePS, p.FXPlayerParticle);

            // Wire Gun references via SerializedObject
            SerializedObject serGun = new SerializedObject(gun);
            serGun.FindProperty("_spawnPoint").objectReferenceValue = muzzleObj.transform;
            serGun.FindProperty("_equippedVisualRoot").objectReferenceValue = visualObj;
            serGun.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serFlash = new SerializedObject(flashFX);
            serFlash.FindProperty("_gunRef").objectReferenceValue = gun;
            serFlash.FindProperty("_muzzleEffect").objectReferenceValue = muzzlePS;
            serFlash.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[GeometricArenaPolish] Successfully styled weapon prefab: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureMuzzleParticleSystem(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.duration = 0.05f;
        main.loop = false;
        main.startLifetime = 0.06f;
        main.startSpeed = 3.5f;
        main.startSize = 0.22f;
        main.startColor = new Color(0.2f, 0.95f, 1f, 1f);
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    public static void SetupEnemyPrefabs(MaterialPalette p)
    {
        SetupSingleEnemy("Assets/_Project/Enemy.prefab", p, EnemySilhouetteRole.BasicBlock);
        SetupSingleEnemy("Assets/_Project/Enemy_Fast.prefab", p, EnemySilhouetteRole.NarrowFast);
        SetupSingleEnemy("Assets/_Project/Enemy_Charger.prefab", p, EnemySilhouetteRole.DirectionalCharger);
        SetupSingleEnemy("Assets/_Project/Enemy_Ranged.prefab", p, EnemySilhouetteRole.TurretRanged);
        SetupSingleEnemy("Assets/_Project/Enemy_Tank.prefab", p, EnemySilhouetteRole.WideArmoredTank);
        SetupSingleEnemy("Assets/_Project/Enemy_Boss.prefab", p, EnemySilhouetteRole.MultipartBoss);
    }

    private enum EnemySilhouetteRole
    {
        BasicBlock,
        NarrowFast,
        DirectionalCharger,
        TurretRanged,
        WideArmoredTank,
        MultipartBoss
    }

    private static void SetupSingleEnemy(string prefabPath, MaterialPalette p, EnemySilhouetteRole role)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"Could not load enemy prefab at {prefabPath}");
            return;
        }

        try
        {
            // Remove root capsule mesh renderer / filter so root does not show capsule
            MeshRenderer rootMR = root.GetComponent<MeshRenderer>();
            if (rootMR != null) rootMR.enabled = false;

            // Remove previous Visual child if re-authoring
            Transform existingVisual = root.transform.Find("Visual");
            if (existingVisual != null) Object.DestroyImmediate(existingVisual.gameObject);

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(root.transform, false);
            visualObj.transform.localPosition = Vector3.zero;

            MeshRenderer primaryBodyRenderer = null;
            List<Renderer> additionalRenderers = new List<Renderer>();

            switch (role)
            {
                case EnemySilhouetteRole.BasicBlock:
                {
                    var body = CreateVisualPrimitive("Body", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.9f, 0.9f), p.EnemyBasic);
                    primaryBodyRenderer = body.GetComponent<MeshRenderer>();

                    var head = CreateVisualPrimitive("HeadBrow", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 1.05f, 0.05f), new Vector3(0.72f, 0.35f, 0.72f), p.EnemyBasic);
                    additionalRenderers.Add(head.GetComponent<MeshRenderer>());

                    var visor = CreateVisualPrimitive("EyeVisor", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.95f, 0.44f), new Vector3(0.55f, 0.12f, 0.12f), p.EnemyEye);
                    additionalRenderers.Add(visor.GetComponent<MeshRenderer>());

                    var hornL = CreateVisualPrimitive("Horn_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-0.48f, 0.75f, 0.1f), new Vector3(0.18f, 0.28f, 0.18f), p.EnemyBasic, Quaternion.Euler(0f, 0f, 25f));
                    var hornR = CreateVisualPrimitive("Horn_R", PrimitiveType.Cube, visualObj.transform, new Vector3(0.48f, 0.75f, 0.1f), new Vector3(0.18f, 0.28f, 0.18f), p.EnemyBasic, Quaternion.Euler(0f, 0f, -25f));
                    additionalRenderers.Add(hornL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(hornR.GetComponent<MeshRenderer>());
                    break;
                }

                case EnemySilhouetteRole.NarrowFast:
                {
                    var body = CreateVisualPrimitive("Body", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.65f, 0.05f), new Vector3(0.42f, 1.25f, 0.42f), p.EnemyFast, Quaternion.Euler(12f, 0f, 0f));
                    primaryBodyRenderer = body.GetComponent<MeshRenderer>();

                    var wingL = CreateVisualPrimitive("Wing_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-0.45f, 0.75f, -0.15f), new Vector3(0.65f, 0.12f, 0.45f), p.EnemyFast, Quaternion.Euler(10f, 25f, 25f));
                    var wingR = CreateVisualPrimitive("Wing_R", PrimitiveType.Cube, visualObj.transform, new Vector3(0.45f, 0.75f, -0.15f), new Vector3(0.65f, 0.12f, 0.45f), p.EnemyFast, Quaternion.Euler(10f, -25f, -25f));
                    additionalRenderers.Add(wingL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(wingR.GetComponent<MeshRenderer>());

                    var eye = CreateVisualPrimitive("EyeDart", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.85f, 0.26f), new Vector3(0.18f, 0.18f, 0.18f), p.EnemyEye, Quaternion.Euler(45f, 45f, 0f));
                    additionalRenderers.Add(eye.GetComponent<MeshRenderer>());
                    break;
                }

                case EnemySilhouetteRole.DirectionalCharger:
                {
                    var body = CreateVisualPrimitive("WedgeBody", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.55f, 0.1f), new Vector3(1.1f, 0.85f, 1.35f), p.EnemyCharger, Quaternion.Euler(-6f, 0f, 0f));
                    primaryBodyRenderer = body.GetComponent<MeshRenderer>();

                    var hornL = CreateVisualPrimitive("RamHorn_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-0.62f, 0.45f, 0.75f), new Vector3(0.22f, 0.32f, 0.70f), p.EnemyCharger, Quaternion.Euler(0f, -12f, 0f));
                    var hornR = CreateVisualPrimitive("RamHorn_R", PrimitiveType.Cube, visualObj.transform, new Vector3(0.62f, 0.45f, 0.75f), new Vector3(0.22f, 0.32f, 0.70f), p.EnemyCharger, Quaternion.Euler(0f, 12f, 0f));
                    additionalRenderers.Add(hornL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(hornR.GetComponent<MeshRenderer>());

                    var engine = CreateVisualPrimitive("RearBlock", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.65f, -0.65f), new Vector3(0.95f, 0.75f, 0.45f), p.WeaponMetal);
                    additionalRenderers.Add(engine.GetComponent<MeshRenderer>());

                    var eye = CreateVisualPrimitive("EyeBar", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.62f, 0.78f), new Vector3(0.68f, 0.12f, 0.12f), p.EnemyEye);
                    additionalRenderers.Add(eye.GetComponent<MeshRenderer>());

                    // Author ground-level TelegraphLine
                    Transform existingTelegraph = root.transform.Find("TelegraphLine");
                    if (existingTelegraph != null) Object.DestroyImmediate(existingTelegraph.gameObject);

                    GameObject telObj = new GameObject("TelegraphLine");
                    telObj.transform.SetParent(root.transform, false);
                    telObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

                    LineRenderer line = telObj.AddComponent<LineRenderer>();
                    line.useWorldSpace = true;
                    line.startWidth = 0.35f;
                    line.endWidth = 0.35f;
                    line.positionCount = 2;
                    line.material = p.FXTelegraph;
                    line.shadowCastingMode = ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    line.enabled = false;

                    ChargerEnemyBehavior charger = root.GetComponent<ChargerEnemyBehavior>();
                    if (charger != null)
                    {
                        SerializedObject serCharger = new SerializedObject(charger);
                        serCharger.FindProperty("_telegraphLine").objectReferenceValue = line;
                        serCharger.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;
                }

                case EnemySilhouetteRole.TurretRanged:
                {
                    var baseRing = CreateVisualPrimitive("PedestalBase", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.15f, 0.24f, 1.15f), p.WeaponMetal, Quaternion.Euler(0f, 45f, 0f));
                    additionalRenderers.Add(baseRing.GetComponent<MeshRenderer>());

                    var turretHead = CreateVisualPrimitive("TurretCore", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.85f, 0.85f, 0.85f), p.EnemyRanged, Quaternion.Euler(0f, 45f, 0f));
                    primaryBodyRenderer = turretHead.GetComponent<MeshRenderer>();

                    var barrel = CreateVisualPrimitive("CannonBarrel", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.82f, 0.58f), new Vector3(0.24f, 0.24f, 0.70f), p.WeaponMetal);
                    additionalRenderers.Add(barrel.GetComponent<MeshRenderer>());

                    var eye = CreateVisualPrimitive("CenterEye", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.82f, 0.94f), new Vector3(0.12f, 0.12f, 0.10f), p.EnemyEye);
                    additionalRenderers.Add(eye.GetComponent<MeshRenderer>());

                    var orbiterL = CreateVisualPrimitive("Orbiter_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-0.75f, 0.95f, 0f), new Vector3(0.20f, 0.45f, 0.35f), p.EnemyRanged);
                    var orbiterR = CreateVisualPrimitive("Orbiter_R", PrimitiveType.Cube, visualObj.transform, new Vector3(0.75f, 0.95f, 0f), new Vector3(0.20f, 0.45f, 0.35f), p.EnemyRanged);
                    additionalRenderers.Add(orbiterL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(orbiterR.GetComponent<MeshRenderer>());
                    break;
                }

                case EnemySilhouetteRole.WideArmoredTank:
                {
                    var body = CreateVisualPrimitive("MainChassis", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.75f, 1.1f, 1.35f), p.EnemyTank);
                    primaryBodyRenderer = body.GetComponent<MeshRenderer>();

                    var shieldL = CreateVisualPrimitive("ArmorShield_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-1.05f, 0.65f, 0.05f), new Vector3(0.38f, 1.15f, 1.45f), p.EnemyTankArmor);
                    var shieldR = CreateVisualPrimitive("ArmorShield_R", PrimitiveType.Cube, visualObj.transform, new Vector3(1.05f, 0.65f, 0.05f), new Vector3(0.38f, 1.15f, 1.45f), p.EnemyTankArmor);
                    additionalRenderers.Add(shieldL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(shieldR.GetComponent<MeshRenderer>());

                    var frontGrill = CreateVisualPrimitive("FrontGrill", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.45f, 0.72f), new Vector3(1.25f, 0.75f, 0.22f), p.EnemyTankArmor);
                    additionalRenderers.Add(frontGrill.GetComponent<MeshRenderer>());

                    var browVisor = CreateVisualPrimitive("ArmoredVisor", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.88f, 0.70f), new Vector3(0.85f, 0.12f, 0.12f), p.EnemyEye);
                    additionalRenderers.Add(browVisor.GetComponent<MeshRenderer>());
                    break;
                }

                case EnemySilhouetteRole.MultipartBoss:
                {
                    var core = CreateVisualPrimitive("BossCore", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.8f, 2.2f, 1.8f), p.EnemyBossBase);
                    primaryBodyRenderer = core.GetComponent<MeshRenderer>();

                    var crown = CreateVisualPrimitive("CrownPauldrons", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 2.35f, 0f), new Vector3(2.5f, 0.55f, 2.2f), p.EnemyBossBase);
                    additionalRenderers.Add(crown.GetComponent<MeshRenderer>());

                    var eyeCore = CreateVisualPrimitive("MoltenCoreEye", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 1.45f, 0.92f), new Vector3(0.65f, 0.45f, 0.15f), p.EnemyBossGlow);
                    additionalRenderers.Add(eyeCore.GetComponent<MeshRenderer>());

                    // Base Phase 1 floating pylons
                    var pylonFL = CreateVisualPrimitive("Pylon_FL", PrimitiveType.Cube, visualObj.transform, new Vector3(-1.4f, 1.1f, 1.1f), new Vector3(0.45f, 1.6f, 0.45f), p.EnemyBossBase);
                    var pylonFR = CreateVisualPrimitive("Pylon_FR", PrimitiveType.Cube, visualObj.transform, new Vector3(1.4f, 1.1f, 1.1f), new Vector3(0.45f, 1.6f, 0.45f), p.EnemyBossBase);
                    additionalRenderers.Add(pylonFL.GetComponent<MeshRenderer>());
                    additionalRenderers.Add(pylonFR.GetComponent<MeshRenderer>());

                    // Multipart Attachments for Phase 2 and 3
                    var pylonRL = CreateVisualPrimitive("Pylon_Phase2_RL", PrimitiveType.Cube, visualObj.transform, new Vector3(-1.6f, 1.7f, -1.1f), new Vector3(0.55f, 2.0f, 0.55f), p.EnemyBossGlow);
                    var pylonRR = CreateVisualPrimitive("Pylon_Phase2_RR", PrimitiveType.Cube, visualObj.transform, new Vector3(1.6f, 1.7f, -1.1f), new Vector3(0.55f, 2.0f, 0.55f), p.EnemyBossGlow);
                    pylonRL.SetActive(false);
                    pylonRR.SetActive(false);

                    var wingsP3 = CreateVisualPrimitive("Wings_Phase3", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 2.4f, -1.2f), new Vector3(3.8f, 0.8f, 0.35f), p.EnemyBossGlow, Quaternion.Euler(15f, 0f, 0f));
                    wingsP3.SetActive(false);

                    // Floor shockwave telegraph ring
                    Transform existingTelegraph = root.transform.Find("ShockwaveTelegraph");
                    if (existingTelegraph != null) Object.DestroyImmediate(existingTelegraph.gameObject);

                    GameObject shockwaveTelegraph = new GameObject("ShockwaveTelegraph");
                    shockwaveTelegraph.transform.SetParent(root.transform, false);
                    shockwaveTelegraph.transform.localPosition = new Vector3(0f, 0.05f, 0f);

                    LineRenderer ring = shockwaveTelegraph.AddComponent<LineRenderer>();
                    ring.useWorldSpace = false;
                    ring.loop = true;
                    ring.startWidth = 0.35f;
                    ring.endWidth = 0.35f;
                    ring.positionCount = 48;
                    ring.material = p.FXTelegraph;
                    ring.shadowCastingMode = ShadowCastingMode.Off;
                    ring.receiveShadows = false;

                    float radius = 7f;
                    for (int i = 0; i < 48; i++)
                    {
                        float angle = (i * 360f / 48f) * Mathf.Deg2Rad;
                        ring.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius));
                    }
                    shockwaveTelegraph.SetActive(false);

                    // Ensure BossPhaseController is attached and wired
                    BossPhaseController bossCtrl = root.GetComponent<BossPhaseController>();
                    if (bossCtrl == null) bossCtrl = root.AddComponent<BossPhaseController>();

                    SerializedObject serBoss = new SerializedObject(bossCtrl);
                    serBoss.FindProperty("_bossName").stringValue = "TITAN GOLIATH";
                    SerializedProperty phasesProp = serBoss.FindProperty("_phases");
                    if (phasesProp != null)
                    {
                        phasesProp.arraySize = 2;

                        // Phase 2
                        SerializedProperty p2 = phasesProp.GetArrayElementAtIndex(0);
                        p2.FindPropertyRelative("PhaseName").stringValue = "Phase 2: Overcharge";
                        p2.FindPropertyRelative("EnterAtHealthFraction").floatValue = 0.66f;
                        p2.FindPropertyRelative("MovementSpeedMultiplier").floatValue = 1.25f;
                        p2.FindPropertyRelative("AttackCooldownMultiplier").floatValue = 0.8f;
                        p2.FindPropertyRelative("BonusDamage").intValue = 5;
                        p2.FindPropertyRelative("TriggerShockwaveOnEnter").boolValue = true;
                        p2.FindPropertyRelative("ShockwaveRadius").floatValue = 7f;
                        p2.FindPropertyRelative("ShockwaveDamage").intValue = 15;
                        p2.FindPropertyRelative("ShockwaveWindup").floatValue = 0.75f;
                        p2.FindPropertyRelative("ShockwaveRecovery").floatValue = 0.35f;
                        p2.FindPropertyRelative("ShockwaveTelegraph").objectReferenceValue = shockwaveTelegraph;
                        SerializedProperty p2Enables = p2.FindPropertyRelative("EnableObjects");
                        p2Enables.arraySize = 2;
                        p2Enables.GetArrayElementAtIndex(0).objectReferenceValue = pylonRL;
                        p2Enables.GetArrayElementAtIndex(1).objectReferenceValue = pylonRR;

                        // Phase 3
                        SerializedProperty p3 = phasesProp.GetArrayElementAtIndex(1);
                        p3.FindPropertyRelative("PhaseName").stringValue = "Phase 3: Final Stand";
                        p3.FindPropertyRelative("EnterAtHealthFraction").floatValue = 0.33f;
                        p3.FindPropertyRelative("MovementSpeedMultiplier").floatValue = 1.45f;
                        p3.FindPropertyRelative("AttackCooldownMultiplier").floatValue = 0.65f;
                        p3.FindPropertyRelative("BonusDamage").intValue = 10;
                        p3.FindPropertyRelative("TriggerShockwaveOnEnter").boolValue = true;
                        p3.FindPropertyRelative("ShockwaveRadius").floatValue = 9f;
                        p3.FindPropertyRelative("ShockwaveDamage").intValue = 25;
                        p3.FindPropertyRelative("ShockwaveWindup").floatValue = 0.6f;
                        p3.FindPropertyRelative("ShockwaveRecovery").floatValue = 0.3f;
                        p3.FindPropertyRelative("ShockwaveTelegraph").objectReferenceValue = shockwaveTelegraph;
                        SerializedProperty p3Enables = p3.FindPropertyRelative("EnableObjects");
                        p3Enables.arraySize = 1;
                        p3Enables.GetArrayElementAtIndex(0).objectReferenceValue = wingsP3;
                    }
                    serBoss.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
            }

            // Ensure HitFlash is present and wired on all enemy prefabs
            HitFlash hitFlash = root.GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = root.AddComponent<HitFlash>();
            if (primaryBodyRenderer != null)
            {
                SerializedObject serHit = new SerializedObject(hitFlash);
                serHit.FindProperty("_renderer").objectReferenceValue = primaryBodyRenderer;
                serHit.FindProperty("_duration").floatValue = 0.08f;
                serHit.FindProperty("_flashColor").colorValue = Color.white;
                serHit.FindProperty("_healthRef").objectReferenceValue = root.GetComponent<HealthComponent>();
                SerializedProperty addProp = serHit.FindProperty("_additionalRenderers");
                if (addProp != null)
                {
                    addProp.arraySize = additionalRenderers.Count;
                    for (int r = 0; r < additionalRenderers.Count; r++)
                    {
                        addProp.GetArrayElementAtIndex(r).objectReferenceValue = additionalRenderers[r];
                    }
                }
                serHit.ApplyModifiedPropertiesWithoutUndo();
            }

            // Update EnemyDeathDebris references
            EnemyDeathDebris deathDebris = root.GetComponent<EnemyDeathDebris>();
            if (deathDebris != null && primaryBodyRenderer != null)
            {
                SerializedObject serDebris = new SerializedObject(deathDebris);
                serDebris.FindProperty("_renderer").objectReferenceValue = primaryBodyRenderer;
                serDebris.FindProperty("_chunkMesh").objectReferenceValue = primaryBodyRenderer.GetComponent<MeshFilter>()?.sharedMesh;
                serDebris.FindProperty("_chunkCount").intValue = 10;
                serDebris.FindProperty("_chunkSize").floatValue = 0.22f;
                serDebris.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[GeometricArenaPolish] Successfully styled enemy prefab: {prefabPath} ({role})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void SetupCombatFXPrefabs(MaterialPalette p)
    {
        // 1. Projectile.prefab
        GameObject projRoot = PrefabUtility.LoadPrefabContents("Assets/_Project/Projectile.prefab");
        if (projRoot != null)
        {
            try
            {
                MeshRenderer mr = projRoot.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = p.FXPlayerBullet;

                BulletTrail trail = projRoot.GetComponent<BulletTrail>();
                if (trail != null)
                {
                    SerializedObject serTrail = new SerializedObject(trail);
                    serTrail.FindProperty("_material").objectReferenceValue = p.FXPlayerParticle;
                    serTrail.FindProperty("_startColor").colorValue = new Color(0.1f, 0.95f, 1f, 1f);
                    serTrail.FindProperty("_endColor").colorValue = new Color(0f, 0.5f, 1f, 0f);
                    serTrail.FindProperty("_time").floatValue = 0.12f;
                    serTrail.FindProperty("_width").floatValue = 0.14f;
                    serTrail.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(projRoot, "Assets/_Project/Projectile.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(projRoot);
            }
        }

        // 2. BulletImpact.prefab
        TuneParticleBurst("Assets/_Project/BulletImpact.prefab", p.FXPlayerParticle, new Color(0.4f, 0.95f, 1f, 1f), 10, 0.15f, 6f, 0.12f);

        // 3. EnemyHitEffect.prefab
        TuneParticleBurst("Assets/_Project/EnemyHitEffect.prefab", p.FXEnemyParticle, new Color(1f, 0.75f, 0.1f, 1f), 10, 0.18f, 5.5f, 0.14f);

        // 4. EnemyDeathEffect.prefab
        TuneParticleBurst("Assets/_Project/EnemyDeathEffect.prefab", p.FXEnemyParticle, new Color(1f, 0.45f, 0.05f, 1f), 18, 0.35f, 7.0f, 0.22f);
    }

    private static void TuneParticleBurst(string prefabPath, Material mat, Color col, int count, float lifetime, float speed, float size)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;
        try
        {
            ParticleBurst burst = root.GetComponent<ParticleBurst>();
            if (burst != null)
            {
                SerializedObject serBurst = new SerializedObject(burst);
                serBurst.FindProperty("_material").objectReferenceValue = mat;
                serBurst.FindProperty("_color").colorValue = col;
                serBurst.FindProperty("_count").intValue = count;
                serBurst.FindProperty("_lifetime").floatValue = lifetime;
                serBurst.FindProperty("_speed").floatValue = speed;
                serBurst.FindProperty("_size").floatValue = size;
                serBurst.ApplyModifiedPropertiesWithoutUndo();
            }

            ParticleSystemRenderer psr = root.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = mat;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void SetupArenaShowcaseScene(MaterialPalette p)
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        Debug.Log($"[GeometricArenaPolish] Configuring scene: {scene.name}");

        // 1. Clean up stray cube visual and collider on Spawner
        GameObject spawner = GameObject.Find("Spawner");
        if (spawner != null)
        {
            MeshRenderer mr = spawner.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);
            MeshFilter mf = spawner.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            Collider col = spawner.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            EditorUtility.SetDirty(spawner);
        }

        // 2. Clean up pre-placed Enemy in scene
        EnemyController enemy = Object.FindFirstObjectByType<EnemyController>(FindObjectsInactive.Include);
        if (enemy != null)
        {
            MeshRenderer mr = enemy.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            EditorUtility.SetDirty(enemy.gameObject);
        }

        // 3. Style Player in scene
        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            SetupPlayerInScene(player, p);
        }

        // 4. Build coherent Arena Environment
        BuildArenaEnvironment(p);

        // 5. Setup Directional Light & Global Volume
        SetupLightingAndAtmosphere(p);

        // 6. Style UI & Correct Game Over "Pause" Title & Layouts
        SetupSceneUI();

        // 7. Wire all game connections
        MiniTopDownShooterSetupCommands.FixCommonSetupIssues();

        GameCompositionRoot root = Object.FindFirstObjectByType<GameCompositionRoot>(FindObjectsInactive.Include);
        if (root != null)
        {
            root.ComposeDependencies();
            EditorUtility.SetDirty(root);
        }

        // 8. Bake NavMesh
        BakeArenaNavMesh();

        // 9. Verify navigation and spawn zones
        VerifyNavigationAndSpawnZones();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GeometricArenaPolish] Saved polished scene: {scene.name}");
    }

    private static void SetupPlayerInScene(PlayerController player, MaterialPalette p)
    {
        GameObject pGo = player.gameObject;
        pGo.transform.position = new Vector3(0f, 1.0f, 0f);
        pGo.transform.rotation = Quaternion.identity;

        // Hide root capsule mesh
        MeshRenderer rootMR = pGo.GetComponent<MeshRenderer>();
        if (rootMR != null) rootMR.enabled = false;

        // Remove legacy child Gun if present
        Transform existingGun = pGo.transform.Find("Gun");
        if (existingGun != null)
        {
            Object.DestroyImmediate(existingGun.gameObject);
        }

        Transform existingVisual = pGo.transform.Find("Visual");
        if (existingVisual != null) Object.DestroyImmediate(existingVisual.gameObject);

        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(pGo.transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;

        // Visual Dynamics component for tilt & recoil
        PlayerVisualDynamics dynamics = visualObj.GetComponent<PlayerVisualDynamics>();
        if (dynamics == null) dynamics = visualObj.AddComponent<PlayerVisualDynamics>();

        // Compact cube body: (0.75, 0.75, 0.75) at y=0.5
        GameObject bodyObj = CreateVisualPrimitive("Body", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 0.75f, 0.75f), p.PlayerBody);
        MeshRenderer bodyMR = bodyObj.GetComponent<MeshRenderer>();

        // Bright glowing Cyan Visor facing forward +Z
        GameObject visorObj = CreateVisualPrimitive("Visor", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.62f, 0.38f), new Vector3(0.48f, 0.16f, 0.12f), p.PlayerVisor);

        // Backpack core
        GameObject bpObj = CreateVisualPrimitive("BackpackCore", PrimitiveType.Cube, visualObj.transform, new Vector3(0f, 0.55f, -0.38f), new Vector3(0.36f, 0.46f, 0.14f), p.PlayerDark);

        // Shoulder pads
        GameObject shLObj = CreateVisualPrimitive("Shoulder_L", PrimitiveType.Cube, visualObj.transform, new Vector3(-0.42f, 0.65f, 0f), new Vector3(0.14f, 0.30f, 0.40f), p.PlayerDark);
        GameObject shRObj = CreateVisualPrimitive("Shoulder_R", PrimitiveType.Cube, visualObj.transform, new Vector3(0.42f, 0.65f, 0f), new Vector3(0.14f, 0.30f, 0.40f), p.PlayerDark);

        // Dedicated right-hand weapon mount
        GameObject mountObj = new GameObject("WeaponMount");
        mountObj.transform.SetParent(visualObj.transform, false);
        mountObj.transform.localPosition = new Vector3(0.38f, 0.45f, 0.40f);
        mountObj.transform.localRotation = Quaternion.identity;

        // Instantiate stylized Gun_Rifle prefab under WeaponMount
        GameObject riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Gun_Rifle.prefab");
        Gun rifleGun = null;
        if (riflePrefab != null)
        {
            GameObject rifleObj = PrefabUtility.InstantiatePrefab(riflePrefab) as GameObject;
            if (rifleObj != null)
            {
                rifleObj.name = "Gun_Rifle";
                rifleObj.transform.SetParent(mountObj.transform, false);
                rifleObj.transform.localPosition = Vector3.zero;
                rifleObj.transform.localRotation = Quaternion.identity;
                rifleGun = rifleObj.GetComponent<Gun>();
            }
        }

        // Configure WeaponLoadout
        WeaponLoadout loadout = pGo.GetComponent<WeaponLoadout>();
        if (loadout == null) loadout = pGo.AddComponent<WeaponLoadout>();
        SerializedObject serLoadout = new SerializedObject(loadout);
        serLoadout.FindProperty("_weaponMount").objectReferenceValue = mountObj.transform;
        SerializedProperty weapsProp = serLoadout.FindProperty("_weapons");
        if (rifleGun != null)
        {
            weapsProp.arraySize = 1;
            weapsProp.GetArrayElementAtIndex(0).objectReferenceValue = rifleGun;
        }
        serLoadout.FindProperty("_startingSlot").intValue = 0;
        serLoadout.ApplyModifiedPropertiesWithoutUndo();

        // Configure PlayerShoot
        PlayerShoot shoot = pGo.GetComponent<PlayerShoot>();
        if (shoot != null && rifleGun != null)
        {
            SerializedObject serShoot = new SerializedObject(shoot);
            serShoot.FindProperty("_gun").objectReferenceValue = rifleGun;
            serShoot.FindProperty("_loadout").objectReferenceValue = loadout;
            serShoot.ApplyModifiedPropertiesWithoutUndo();
        }

        // Attach HitFlash to Player
        HitFlash playerHitFlash = pGo.GetComponent<HitFlash>();
        if (playerHitFlash == null) playerHitFlash = pGo.AddComponent<HitFlash>();
        if (bodyMR != null)
        {
            SerializedObject serHit = new SerializedObject(playerHitFlash);
            serHit.FindProperty("_renderer").objectReferenceValue = bodyMR;
            serHit.FindProperty("_duration").floatValue = 0.08f;
            serHit.FindProperty("_flashColor").colorValue = Color.white;
            serHit.FindProperty("_healthRef").objectReferenceValue = pGo.GetComponent<HealthComponent>();
            SerializedProperty addProp = serHit.FindProperty("_additionalRenderers");
            if (addProp != null)
            {
                Renderer[] extras = { visorObj.GetComponent<MeshRenderer>(), bpObj.GetComponent<MeshRenderer>(), shLObj.GetComponent<MeshRenderer>(), shRObj.GetComponent<MeshRenderer>() };
                addProp.arraySize = extras.Length;
                for (int r = 0; r < extras.Length; r++)
                {
                    addProp.GetArrayElementAtIndex(r).objectReferenceValue = extras[r];
                }
            }
            serHit.ApplyModifiedPropertiesWithoutUndo();
        }

        // Attach PlayerHitFX to Player
        PlayerHitFX playerHitFX = pGo.GetComponent<PlayerHitFX>();
        if (playerHitFX == null) playerHitFX = pGo.AddComponent<PlayerHitFX>();
        SerializedObject serHitFX = new SerializedObject(playerHitFX);
        serHitFX.FindProperty("_playerHealthRef").objectReferenceValue = pGo.GetComponent<HealthComponent>();
        serHitFX.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(pGo);
    }

    private static void BuildArenaEnvironment(MaterialPalette p)
    {
        // 1. Clean up old stray primitives in scene
        string[] oldNames = { "Plane", "Cube", "Cube (1)", "Cube (2)", "Cube (3)", "Cube (5)", "ArenaEnvironment" };
        foreach (string name in oldNames)
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        GameObject env = new GameObject("ArenaEnvironment");
        env.transform.position = Vector3.zero;

        // 2. Floor Hierarchy
        GameObject floorRoot = new GameObject("Floor");
        floorRoot.transform.SetParent(env.transform, false);

        // Main Base Floor: 54m x 54m
        CreateEnvironmentBlock("BaseFloor", PrimitiveType.Cube, floorRoot.transform, new Vector3(0f, -0.25f, 0f), new Vector3(54f, 0.5f, 54f), p.ArenaFloor, true);

        // Central Combat Plaza: 26m x 26m slightly raised
        CreateEnvironmentBlock("CenterPlaza", PrimitiveType.Cube, floorRoot.transform, new Vector3(0f, 0.02f, 0f), new Vector3(26f, 0.06f, 26f), p.ArenaFloorAccent, true);

        // Central Emblem / Dais: 6m x 6m center plateau
        CreateEnvironmentBlock("CenterEmblem", PrimitiveType.Cube, floorRoot.transform, new Vector3(0f, 0.05f, 0f), new Vector3(6f, 0.04f, 6f), p.ArenaWallTrim, true, Quaternion.Euler(0f, 45f, 0f));

        // Spawn Zone Inset Floor Pads (North, West, East, South)
        CreateEnvironmentBlock("SpawnPad_North", PrimitiveType.Cube, floorRoot.transform, new Vector3(0f, 0.015f, 18f), new Vector3(8f, 0.04f, 8f), p.ArenaSpawnPad, false, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("SpawnPad_West", PrimitiveType.Cube, floorRoot.transform, new Vector3(-18f, 0.015f, 0f), new Vector3(8f, 0.04f, 8f), p.ArenaSpawnPad, false, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("SpawnPad_East", PrimitiveType.Cube, floorRoot.transform, new Vector3(18f, 0.015f, 0f), new Vector3(8f, 0.04f, 8f), p.ArenaSpawnPad, false, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("SpawnPad_South", PrimitiveType.Cube, floorRoot.transform, new Vector3(0f, 0.015f, -18f), new Vector3(8f, 0.04f, 8f), p.ArenaSpawnPad, false, Quaternion.Euler(0f, 45f, 0f));

        // Ensure cardinal spawn zones exist
        EnsureSpawnZone("SpawnZone_North", new Vector3(0f, 0f, 18f));
        EnsureSpawnZone("SpawnZone_South", new Vector3(0f, 0f, -18f));
        EnsureSpawnZone("SpawnZone_West", new Vector3(-18f, 0f, 0f));
        EnsureSpawnZone("SpawnZone_East", new Vector3(18f, 0f, 0f));

        // 3. Boundary Walls Hierarchy (Height 3.5m, thickness 1.5m)
        GameObject wallsRoot = new GameObject("BoundaryWalls");
        wallsRoot.transform.SetParent(env.transform, false);

        // 4 Main Walls with Trim (Parented to wallsRoot to avoid scale multiplication)
        CreateWallWithTrim("Wall_North", wallsRoot.transform, new Vector3(0f, 1.75f, 27f), new Vector3(54f, 3.5f, 1.5f), p);
        CreateWallWithTrim("Wall_South", wallsRoot.transform, new Vector3(0f, 1.75f, -27f), new Vector3(54f, 3.5f, 1.5f), p);
        CreateWallWithTrim("Wall_West", wallsRoot.transform, new Vector3(-27f, 1.75f, 0f), new Vector3(1.5f, 3.5f, 54f), p);
        CreateWallWithTrim("Wall_East", wallsRoot.transform, new Vector3(27f, 1.75f, 0f), new Vector3(1.5f, 3.5f, 54f), p);

        // 4 Corner Bastions / Chamfers
        CreateEnvironmentBlock("Bastion_NE", PrimitiveType.Cube, wallsRoot.transform, new Vector3(25.5f, 2.25f, 25.5f), new Vector3(6f, 4.5f, 6f), p.ArenaWall, true, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("Bastion_NW", PrimitiveType.Cube, wallsRoot.transform, new Vector3(-25.5f, 2.25f, 25.5f), new Vector3(6f, 4.5f, 6f), p.ArenaWall, true, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("Bastion_SE", PrimitiveType.Cube, wallsRoot.transform, new Vector3(25.5f, 2.25f, -25.5f), new Vector3(6f, 4.5f, 6f), p.ArenaWall, true, Quaternion.Euler(0f, 45f, 0f));
        CreateEnvironmentBlock("Bastion_SW", PrimitiveType.Cube, wallsRoot.transform, new Vector3(-25.5f, 2.25f, -25.5f), new Vector3(6f, 4.5f, 6f), p.ArenaWall, true, Quaternion.Euler(0f, 45f, 0f));

        // 4. Tactical Cover Clusters in 4 quadrants (+/-9, +/-9)
        GameObject coverRoot = new GameObject("Cover");
        coverRoot.transform.SetParent(env.transform, false);

        BuildCoverCluster("Cover_NE", coverRoot.transform, new Vector3(9f, 0f, 9f), 1f, p);
        BuildCoverCluster("Cover_NW", coverRoot.transform, new Vector3(-9f, 0f, 9f), -1f, p);
        BuildCoverCluster("Cover_SE", coverRoot.transform, new Vector3(9f, 0f, -9f), 1f, p, true);
        BuildCoverCluster("Cover_SW", coverRoot.transform, new Vector3(-9f, 0f, -9f), -1f, p, true);

        // 5. Landmarks: 2 Monolithic Geometric Pylons at (+/-13.5, 0)
        GameObject landmarksRoot = new GameObject("Landmarks");
        landmarksRoot.transform.SetParent(env.transform, false);

        BuildLandmarkPylon("Pylon_West", landmarksRoot.transform, new Vector3(-13.5f, 0f, 0f), p);
        BuildLandmarkPylon("Pylon_East", landmarksRoot.transform, new Vector3(13.5f, 0f, 0f), p);

        // 6. NavMeshSurface
        NavMeshSurface surface = env.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
    }

    private static void EnsureSpawnZone(string name, Vector3 pos)
    {
        GameObject szObj = GameObject.Find(name);
        if (szObj == null)
        {
            szObj = new GameObject(name);
            szObj.transform.position = pos;
            SpawnZone zone = szObj.AddComponent<SpawnZone>();
            SerializedObject serZone = new SerializedObject(zone);
            serZone.FindProperty("_id").stringValue = name;
            serZone.FindProperty("_weight").intValue = 1;
            serZone.FindProperty("_radius").floatValue = 4f;
            serZone.FindProperty("_shape").enumValueIndex = (int)SpawnZoneShape.Circle;
            serZone.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            szObj.transform.position = pos;
        }
    }

    private static void CreateWallWithTrim(string name, Transform parent, Vector3 pos, Vector3 size, MaterialPalette p)
    {
        CreateEnvironmentBlock(name, PrimitiveType.Cube, parent, pos, size, p.ArenaWall, true);

        // Glowing trim cap on top edge - parented to parent (wallsRoot) to prevent scale multiplication
        Vector3 trimSize = new Vector3(size.x > size.z ? size.x : 0.4f, 0.18f, size.z > size.x ? size.z : 0.4f);
        Vector3 trimPos = pos + Vector3.up * (size.y * 0.5f + 0.09f);
        CreateEnvironmentBlock($"{name}_Trim", PrimitiveType.Cube, parent, trimPos, trimSize, p.ArenaWallTrim, false);
    }

    private static void BuildCoverCluster(string name, Transform parent, Vector3 center, float dirX, MaterialPalette p, bool flipZ = false)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = center;

        float zSign = flipZ ? -1f : 1f;

        // Low cover barricade (1.1m high, 2.8m long)
        CreateEnvironmentBlock("LowBarricade_1", PrimitiveType.Cube, cluster.transform, new Vector3(dirX * 0.8f, 0.55f, 0f), new Vector3(2.8f, 1.1f, 0.85f), p.ArenaCover, true);
        CreateEnvironmentBlock("Trim_1", PrimitiveType.Cube, cluster.transform, new Vector3(dirX * 0.8f, 1.12f, 0f), new Vector3(2.84f, 0.08f, 0.89f), p.ArenaWallTrim, false);

        // Medium L-shaped sightline blocker (2.0m high, 1.1m wide, 2.2m deep)
        CreateEnvironmentBlock("CornerBlock_2", PrimitiveType.Cube, cluster.transform, new Vector3(dirX * -1.4f, 1.0f, zSign * 1.2f), new Vector3(1.1f, 2.0f, 2.2f), p.ArenaCover, true);
        CreateEnvironmentBlock("Trim_2", PrimitiveType.Cube, cluster.transform, new Vector3(dirX * -1.4f, 2.03f, zSign * 1.2f), new Vector3(1.15f, 0.08f, 2.25f), p.ArenaWallTrim, false);
    }

    private static void BuildLandmarkPylon(string name, Transform parent, Vector3 center, MaterialPalette p)
    {
        GameObject pylon = new GameObject(name);
        pylon.transform.SetParent(parent, false);
        pylon.transform.localPosition = center;

        // Heavy base
        CreateEnvironmentBlock("Base", PrimitiveType.Cube, pylon.transform, new Vector3(0f, 0.4f, 0f), new Vector3(2.2f, 0.8f, 2.2f), p.ArenaCover, true);

        // Tall column
        CreateEnvironmentBlock("Column", PrimitiveType.Cube, pylon.transform, new Vector3(0f, 2.8f, 0f), new Vector3(1.5f, 4.4f, 1.5f), p.ArenaWall, true);

        // Vertical glowing energy conduit strips on 4 faces
        CreateEnvironmentBlock("Conduit_N", PrimitiveType.Cube, pylon.transform, new Vector3(0f, 2.8f, 0.77f), new Vector3(0.25f, 3.8f, 0.08f), p.ArenaPillarAccent, false);
        CreateEnvironmentBlock("Conduit_S", PrimitiveType.Cube, pylon.transform, new Vector3(0f, 2.8f, -0.77f), new Vector3(0.25f, 3.8f, 0.08f), p.ArenaPillarAccent, false);
        CreateEnvironmentBlock("Conduit_W", PrimitiveType.Cube, pylon.transform, new Vector3(-0.77f, 2.8f, 0f), new Vector3(0.08f, 3.8f, 0.25f), p.ArenaPillarAccent, false);
        CreateEnvironmentBlock("Conduit_E", PrimitiveType.Cube, pylon.transform, new Vector3(0.77f, 2.8f, 0f), new Vector3(0.08f, 3.8f, 0.25f), p.ArenaPillarAccent, false);

        // Spire cap
        CreateEnvironmentBlock("Cap", PrimitiveType.Cube, pylon.transform, new Vector3(0f, 5.2f, 0f), new Vector3(1.8f, 0.4f, 1.8f), p.ArenaWallTrim, true);
    }

    private static GameObject CreateEnvironmentBlock(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider, Quaternion? rot = null)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        obj.transform.localRotation = rot ?? Quaternion.identity;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        Collider col = obj.GetComponent<Collider>();
        if (col != null && !keepCollider)
        {
            Object.DestroyImmediate(col);
        }

        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        return obj;
    }

    private static GameObject CreateVisualPrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material material, Quaternion? localRot = null)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        obj.transform.localRotation = localRot ?? Quaternion.identity;
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
        }
        return obj;
    }

    private static void SetupLightingAndAtmosphere(MaterialPalette p)
    {
        // 1. Directional Light
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.96f, 0.96f, 1.0f);
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowNormalBias = 0.4f;
            dirLight.shadowBias = 0.05f;
            dirLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            EditorUtility.SetDirty(dirLight);
        }

        // 2. Ambient Lighting
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.14f, 0.16f, 0.20f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.09f, 0.12f);
        RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.04f);
        RenderSettings.ambientIntensity = 1.0f;

        // 3. Global Volume Profile
        Volume vol = Object.FindFirstObjectByType<Volume>();
        if (vol != null && vol.sharedProfile != null)
        {
            VolumeProfile profile = vol.sharedProfile;
            if (profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {
                bloom.threshold.Override(1.0f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.5f);
            }
            if (profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vig))
            {
                vig.intensity.Override(0.22f);
                vig.smoothness.Override(0.25f);
            }
            EditorUtility.SetDirty(profile);
        }
    }

    private static void SetupSceneUI()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // 1. Clean & Style HUD (Top-Left Health, Top-Right Wave, Score & HighScore)
        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null)
        {
            Image hudImg = hudObj.GetComponent<Image>();
            if (hudImg != null) hudImg.color = Color.clear;

            Transform healthBarTrans = hudObj.transform.Find("HealthBar");
            if (healthBarTrans != null)
            {
                RectTransform rt = healthBarTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(25f, -25f);
                rt.sizeDelta = new Vector2(260f, 22f);

                if (healthBarTrans.TryGetComponent<Image>(out var barImg))
                {
                    barImg.color = Color.clear;
                }

                Slider s = healthBarTrans.GetComponentInChildren<Slider>(true);
                if (s != null)
                {
                    s.handleRect = null;
                    Transform handleArea = s.transform.Find("Handle Slide Area");
                    if (handleArea != null)
                    {
                        Object.DestroyImmediate(handleArea.gameObject);
                    }

                    Transform bg = s.transform.Find("Background");
                    if (bg != null && bg.TryGetComponent<Image>(out var bgImg))
                    {
                        bgImg.color = new Color(0.08f, 0.10f, 0.13f, 0.85f);
                    }

                    Transform fillArea = s.transform.Find("Fill Area");
                    if (fillArea != null)
                    {
                        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
                        fillAreaRT.anchorMin = Vector2.zero;
                        fillAreaRT.anchorMax = Vector2.one;
                        fillAreaRT.offsetMin = Vector2.zero;
                        fillAreaRT.offsetMax = Vector2.zero;

                        Transform fill = fillArea.Find("Fill");
                        if (fill != null && fill.TryGetComponent<Image>(out var fillImg))
                        {
                            fillImg.color = new Color(0.0f, 0.85f, 1.0f, 0.95f);
                        }
                    }
                }
            }

            Transform waveTrans = hudObj.transform.Find("Wave");
            if (waveTrans != null)
            {
                RectTransform rt = waveTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -25f);
                rt.sizeDelta = new Vector2(140f, 32f);

                if (waveTrans.TryGetComponent<Image>(out var img))
                {
                    img.color = new Color(0.07f, 0.08f, 0.11f, 0.80f);
                }
                var txt = waveTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    if (font != null) txt.font = font;
                    txt.color = new Color(0.96f, 0.75f, 0.15f);
                    txt.fontSize = 18;
                    txt.alignment = TextAlignmentOptions.Center;
                }
            }

            Transform scoreTrans = hudObj.transform.Find("Score");
            if (scoreTrans != null)
            {
                RectTransform rt = scoreTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -65f);
                rt.sizeDelta = new Vector2(140f, 32f);

                if (scoreTrans.TryGetComponent<Image>(out var img))
                {
                    img.color = new Color(0.07f, 0.08f, 0.11f, 0.80f);
                }
                var txt = scoreTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    if (font != null) txt.font = font;
                    txt.color = Color.white;
                    txt.fontSize = 18;
                    txt.alignment = TextAlignmentOptions.Center;
                }
            }

            Transform highScoreTrans = hudObj.transform.Find("HighScore");
            if (highScoreTrans != null)
            {
                RectTransform rt = highScoreTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -105f);
                rt.sizeDelta = new Vector2(140f, 32f);

                if (highScoreTrans.TryGetComponent<Image>(out var img))
                {
                    img.color = new Color(0.07f, 0.08f, 0.11f, 0.80f);
                }
                var txt = highScoreTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    if (font != null) txt.font = font;
                    txt.color = new Color(0.85f, 0.90f, 0.95f, 0.9f);
                    txt.fontSize = 16;
                    txt.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        // 2. Style WeaponHUD (Bottom-Right)
        WeaponHUD weaponHUD = Object.FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
        if (weaponHUD != null)
        {
            RectTransform rt = weaponHUD.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-25f, 25f);
            rt.sizeDelta = new Vector2(240f, 95f);

            var texts = weaponHUD.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (font != null) t.font = font;
            }
        }

        // 3. Style BossHealthBarUI (Top-Center)
        BossHealthBarUI bossUI = Object.FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
        if (bossUI != null)
        {
            RectTransform rt = bossUI.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -25f);
            rt.sizeDelta = new Vector2(440f, 55f);

            var texts = bossUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (font != null) t.font = font;
            }

            Slider s = bossUI.GetComponentInChildren<Slider>(true);
            if (s != null)
            {
                Transform fill = s.transform.Find("Fill Area/Fill");
                if (fill != null && fill.TryGetComponent<Image>(out var fillImg))
                {
                    fillImg.color = new Color(1.0f, 0.35f, 0.05f, 1f);
                }
            }
        }

        // 4. Style PauseUI & Fix Button Overlap
        PauseUI pause = Object.FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
        if (pause != null)
        {
            SerializedObject serPause = new SerializedObject(pause);
            SerializedProperty panelProp = serPause.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
            {
                RectTransform panelRT = pObj.GetComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0.5f, 0.5f);
                panelRT.anchorMax = new Vector2(0.5f, 0.5f);
                panelRT.pivot = new Vector2(0.5f, 0.5f);
                panelRT.anchoredPosition = Vector2.zero;
                panelRT.sizeDelta = new Vector2(360f, 320f);

                Image img = pObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.06f, 0.07f, 0.09f, 0.94f);
                    EditorUtility.SetDirty(img);
                }

                Button resumeBtn = serPause.FindProperty("_resumeButton")?.objectReferenceValue as Button;
                if (resumeBtn == null) resumeBtn = pObj.transform.Find("Resume")?.GetComponent<Button>();
                if (resumeBtn != null)
                {
                    serPause.FindProperty("_resumeButton").objectReferenceValue = resumeBtn;
                    StyleButton(resumeBtn, "Resume", new Vector2(0f, 25f), font);
                }

                Button settingsBtn = serPause.FindProperty("_settingsButton")?.objectReferenceValue as Button;
                if (settingsBtn == null) settingsBtn = pObj.transform.Find("SettingsButton")?.GetComponent<Button>() ?? pObj.transform.Find("Settings")?.GetComponent<Button>();
                if (settingsBtn != null)
                {
                    serPause.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
                    StyleButton(settingsBtn, "Settings", new Vector2(0f, -35f), font);
                }

                Button menuBtn = serPause.FindProperty("_menuButton")?.objectReferenceValue as Button;
                if (menuBtn == null) menuBtn = pObj.transform.Find("MenuButton")?.GetComponent<Button>() ?? pObj.transform.Find("Menu")?.GetComponent<Button>();
                if (menuBtn != null)
                {
                    serPause.FindProperty("_menuButton").objectReferenceValue = menuBtn;
                    StyleButton(menuBtn, "Menu", new Vector2(0f, -95f), font);
                }

                TextMeshProUGUI[] texts = pObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI t in texts)
                {
                    if (font != null) t.font = font;
                    if (t.transform.parent == pObj.transform && (t.text.Contains("Pause") || t.text.Contains("PAUSE")))
                    {
                        RectTransform rt = t.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2(0f, 95f);
                        rt.sizeDelta = new Vector2(280f, 50f);
                        t.text = "PAUSED";
                        t.fontSize = 36;
                        t.alignment = TextAlignmentOptions.Center;
                    }
                }
                pObj.SetActive(false);
            }
        }

        // 5. Style GameOverUI & Fix Overlap, Title & Stats
        GameOverUI gameOver = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        if (gameOver != null)
        {
            SerializedObject serGO = new SerializedObject(gameOver);
            SerializedProperty panelProp = serGO.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
            {
                RectTransform panelRT = pObj.GetComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0.5f, 0.5f);
                panelRT.anchorMax = new Vector2(0.5f, 0.5f);
                panelRT.pivot = new Vector2(0.5f, 0.5f);
                panelRT.anchoredPosition = Vector2.zero;
                panelRT.sizeDelta = new Vector2(420f, 380f);

                Image img = pObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.06f, 0.07f, 0.09f, 0.94f);
                    EditorUtility.SetDirty(img);
                }

                // Correct Title
                TextMeshProUGUI[] texts = pObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI t in texts)
                {
                    if (font != null) t.font = font;
                    if (string.Equals(t.text.Trim(), "Pause", StringComparison.OrdinalIgnoreCase) || string.Equals(t.text.Trim(), "GAME OVER", StringComparison.OrdinalIgnoreCase))
                    {
                        t.text = "GAME OVER";
                        t.color = new Color(1f, 0.25f, 0.2f);
                        t.fontSize = 40;
                        t.alignment = TextAlignmentOptions.Center;
                        RectTransform rt = t.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchorMin = new Vector2(0.5f, 0.5f);
                            rt.anchorMax = new Vector2(0.5f, 0.5f);
                            rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.anchoredPosition = new Vector2(0f, 130f);
                            rt.sizeDelta = new Vector2(350f, 55f);
                        }
                        EditorUtility.SetDirty(t);
                    }
                }

                // Ensure Score, High Score, and Waves labels exist in GOPanel
                TMP_Text scoreLabel = serGO.FindProperty("_scoreLabel")?.objectReferenceValue as TMP_Text;
                if (scoreLabel == null)
                {
                    scoreLabel = CreateOrGetLabel(pObj.transform, "ScoreText", font, "Final Score: 0", 20, new Vector2(0f, 65f), new Vector2(320f, 28f), Color.white);
                    serGO.FindProperty("_scoreLabel").objectReferenceValue = scoreLabel;
                }
                else
                {
                    RectTransform rt = scoreLabel.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 65f);
                    rt.sizeDelta = new Vector2(320f, 28f);
                    if (font != null) scoreLabel.font = font;
                    scoreLabel.alignment = TextAlignmentOptions.Center;
                }

                TMP_Text highScoreLabel = serGO.FindProperty("_highScoreLabel")?.objectReferenceValue as TMP_Text;
                if (highScoreLabel == null)
                {
                    highScoreLabel = CreateOrGetLabel(pObj.transform, "HighScoreText", font, "High Score: 0", 18, new Vector2(0f, 35f), new Vector2(320f, 26f), new Color(0.96f, 0.75f, 0.15f));
                    serGO.FindProperty("_highScoreLabel").objectReferenceValue = highScoreLabel;
                }
                else
                {
                    RectTransform rt = highScoreLabel.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 35f);
                    rt.sizeDelta = new Vector2(320f, 26f);
                    if (font != null) highScoreLabel.font = font;
                    highScoreLabel.alignment = TextAlignmentOptions.Center;
                }

                TMP_Text wavesLabel = serGO.FindProperty("_wavesLabel")?.objectReferenceValue as TMP_Text;
                if (wavesLabel == null)
                {
                    wavesLabel = CreateOrGetLabel(pObj.transform, "WavesText", font, "Waves Cleared: 0", 18, new Vector2(0f, 5f), new Vector2(320f, 26f), new Color(0.0f, 0.85f, 1.0f));
                    serGO.FindProperty("_wavesLabel").objectReferenceValue = wavesLabel;
                }
                else
                {
                    RectTransform rt = wavesLabel.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f, 5f);
                    rt.sizeDelta = new Vector2(320f, 26f);
                    if (font != null) wavesLabel.font = font;
                    wavesLabel.alignment = TextAlignmentOptions.Center;
                }

                // Layout buttons: Restart (Y = -55), Menu (Y = -115)
                Button restartBtn = serGO.FindProperty("_restartButton")?.objectReferenceValue as Button;
                if (restartBtn == null) restartBtn = pObj.transform.Find("Restart")?.GetComponent<Button>() ?? pObj.transform.Find("RestartButton")?.GetComponent<Button>();
                if (restartBtn != null)
                {
                    serGO.FindProperty("_restartButton").objectReferenceValue = restartBtn;
                    StyleButton(restartBtn, "Play Again", new Vector2(0f, -55f), font);
                }

                Button menuBtn = serGO.FindProperty("_menuButton")?.objectReferenceValue as Button;
                if (menuBtn == null) menuBtn = pObj.transform.Find("Menu")?.GetComponent<Button>() ?? pObj.transform.Find("MenuButton")?.GetComponent<Button>();
                if (menuBtn != null)
                {
                    serGO.FindProperty("_menuButton").objectReferenceValue = menuBtn;
                    StyleButton(menuBtn, "Main Menu", new Vector2(0f, -115f), font);
                }

                serGO.ApplyModifiedPropertiesWithoutUndo();
                pObj.SetActive(false);
            }
        }

        // 6. Style MainMenuUI & Button Spacing
        MainMenuUI mainMenu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            SerializedObject serMenu = new SerializedObject(mainMenu);
            SerializedProperty panelProp = serMenu.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
            {
                Button playBtn = serMenu.FindProperty("_playButton")?.objectReferenceValue as Button;
                if (playBtn != null)
                {
                    StyleButton(playBtn, "Play", new Vector2(0f, 20f), font);
                }

                Button setBtn = serMenu.FindProperty("_settingsButton")?.objectReferenceValue as Button;
                if (setBtn != null)
                {
                    StyleButton(setBtn, "Settings", new Vector2(0f, -40f), font);
                }
            }
        }

        // 7. Style VictoryUI
        VictoryUI victory = Object.FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
        if (victory != null)
        {
            SerializedObject serVic = new SerializedObject(victory);
            SerializedProperty panelProp = serVic.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
            {
                RectTransform panelRT = pObj.GetComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0.5f, 0.5f);
                panelRT.anchorMax = new Vector2(0.5f, 0.5f);
                panelRT.pivot = new Vector2(0.5f, 0.5f);
                panelRT.anchoredPosition = Vector2.zero;
                panelRT.sizeDelta = new Vector2(420f, 380f);

                Image img = pObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.05f, 0.08f, 0.10f, 0.94f);
                    EditorUtility.SetDirty(img);
                }
                TextMeshProUGUI[] texts = pObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI t in texts)
                {
                    if (string.Equals(t.text.Trim(), "VICTORY!", StringComparison.OrdinalIgnoreCase))
                    {
                        t.color = new Color(0.0f, 0.95f, 1f);
                    }
                    if (font != null) t.font = font;
                }

                Button vicRestart = serVic.FindProperty("_restartButton")?.objectReferenceValue as Button;
                if (vicRestart == null) vicRestart = pObj.transform.Find("RestartButton")?.GetComponent<Button>() ?? pObj.transform.Find("Restart")?.GetComponent<Button>();
                if (vicRestart != null)
                {
                    serVic.FindProperty("_restartButton").objectReferenceValue = vicRestart;
                    StyleButton(vicRestart, "Play Again", new Vector2(0f, -80f), font);
                }

                Button vicMenu = serVic.FindProperty("_menuButton")?.objectReferenceValue as Button;
                if (vicMenu == null) vicMenu = pObj.transform.Find("MenuButton")?.GetComponent<Button>() ?? pObj.transform.Find("Menu")?.GetComponent<Button>();
                if (vicMenu != null)
                {
                    serVic.FindProperty("_menuButton").objectReferenceValue = vicMenu;
                    StyleButton(vicMenu, "Main Menu", new Vector2(0f, -140f), font);
                }

                serVic.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 8. Style SettingsUI
        SettingsUI settings = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settings != null)
        {
            SerializedObject serSet = new SerializedObject(settings);
            SerializedProperty panelProp = serSet.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue is GameObject pObj)
            {
                Image img = pObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.06f, 0.07f, 0.09f, 0.94f);
                    EditorUtility.SetDirty(img);
                }
            }
        }

        // 9. Style Buttons across all UI
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            ColorBlock cb = b.colors;
            cb.normalColor = new Color(0.16f, 0.20f, 0.26f, 0.95f);
            cb.highlightedColor = new Color(0.0f, 0.75f, 0.95f, 1.0f);
            cb.pressedColor = new Color(0.0f, 0.50f, 0.70f, 1.0f);
            cb.selectedColor = cb.highlightedColor;
            b.colors = cb;
            EditorUtility.SetDirty(b);

            TextMeshProUGUI btnText = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnText != null)
            {
                btnText.color = Color.white;
                if (font != null) btnText.font = font;
                EditorUtility.SetDirty(btnText);
            }
            Text legText = b.GetComponentInChildren<Text>(true);
            if (legText != null)
            {
                legText.color = Color.white;
                EditorUtility.SetDirty(legText);
            }
        }
    }

    private static void StyleButton(Button btn, string text, Vector2 pos, TMP_FontAsset font)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(true);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220f, 44f);

        Image bImg = btn.GetComponent<Image>();
        if (bImg != null)
        {
            bImg.color = new Color(0.16f, 0.20f, 0.26f, 0.95f);
            EditorUtility.SetDirty(bImg);
        }
        TextMeshProUGUI t = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t != null)
        {
            if (!string.IsNullOrEmpty(text)) t.text = text;
            t.color = Color.white;
            t.fontSize = 20;
            if (font != null) t.font = font;
            t.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(t);
        }
        else
        {
            Text legT = btn.GetComponentInChildren<Text>(true);
            if (legT != null)
            {
                if (!string.IsNullOrEmpty(text)) legT.text = text;
                legT.color = Color.white;
                legT.fontSize = 20;
                legT.alignment = TextAnchor.MiddleCenter;
                EditorUtility.SetDirty(legT);
            }
        }
    }

    private static TextMeshProUGUI CreateOrGetLabel(Transform parent, string name, TMP_FontAsset font, string text, float size, Vector2 pos, Vector2 delta, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = delta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void BakeArenaNavMesh()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
        if (surface == null)
        {
            Debug.LogError("[GeometricArenaPolish] No NavMeshSurface found in scene to bake!");
            return;
        }

        Debug.Log("[GeometricArenaPolish] Baking NavMesh...");
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        Debug.Log("[GeometricArenaPolish] NavMesh baking completed successfully!");
    }

    private static void VerifyNavigationAndSpawnZones()
    {
        // 1. Check triangulation
        var triangulation = NavMesh.CalculateTriangulation();
        int vertCount = triangulation.vertices.Length;
        Debug.Log($"[GeometricArenaPolish] NavMesh triangulation vertices: {vertCount}");
        if (vertCount == 0)
        {
            throw new Exception("Bake failed: NavMesh has 0 vertices!");
        }

        // 2. Check Player position on NavMesh
        Vector3 playerPos = Vector3.zero;
        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            playerPos = player.transform.position;
        }

        if (!NavMesh.SamplePosition(playerPos, out NavMeshHit playerHit, 2.0f, NavMesh.AllAreas))
        {
            throw new Exception($"Player at {playerPos} is not on valid NavMesh!");
        }
        Debug.Log($"[GeometricArenaPolish] Player is on NavMesh at {playerHit.position}");

        // 3. Check SpawnZones
        SpawnZone[] zones = Object.FindObjectsByType<SpawnZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[GeometricArenaPolish] Verifying {zones.Length} spawn zones...");

        foreach (SpawnZone zone in zones)
        {
            Vector3 zoneCenter = zone.transform.position;
            if (!NavMesh.SamplePosition(zoneCenter, out NavMeshHit zoneHit, 3.0f, NavMesh.AllAreas))
            {
                throw new Exception($"SpawnZone '{zone.Id}' at {zoneCenter} is not near valid NavMesh!");
            }

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(zoneHit.position, playerHit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                throw new Exception($"SpawnZone '{zone.Id}' cannot reach player! Path status: {path.status}");
            }
            Debug.Log($"[GeometricArenaPolish] SpawnZone '{zone.Id}' path to player complete ({path.corners.Length} corners).");
        }
    }

    [MenuItem("Tools/Mini Top Down Shooter/Capture Gameplay Screenshots")]
    public static void CaptureScreenshots()
    {
        ShaderUtil.allowAsyncCompilation = false;
        Shader.WarmupAllShaders();

        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        string[] outputDirs = { "Builds/Screenshots", "Assets/_Project/Documentation/Screenshots" };
        foreach (var dir in outputDirs)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null) cam = camObj.GetComponent<Camera>();
        }

        if (cam == null)
        {
            Debug.LogError("[GeometricArenaPolish] No camera found for screenshot capture.");
            return;
        }

        Vector3 originalCamPos = cam.transform.position;
        Quaternion originalCamRot = cam.transform.rotation;
        CameraClearFlags originalClearFlags = cam.clearFlags;
        Color originalBgColor = cam.backgroundColor;
        int originalCullingMask = cam.cullingMask;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        RenderMode originalRenderMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
        Camera originalCanvasCam = canvas != null ? canvas.worldCamera : null;
        float originalPlaneDist = canvas != null ? canvas.planeDistance : 100f;

        MainMenuUI mainMenu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        GameObject mainMenuPanel = null;
        if (mainMenu != null)
        {
            SerializedObject serMenu = new SerializedObject(mainMenu);
            mainMenuPanel = serMenu.FindProperty("_panel")?.objectReferenceValue as GameObject;
            mainMenu.gameObject.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        PauseUI pauseUI = Object.FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
        GameObject pausePanel = null;
        if (pauseUI != null)
        {
            SerializedObject serPause = new SerializedObject(pauseUI);
            pausePanel = serPause.FindProperty("_panel")?.objectReferenceValue as GameObject;
            pauseUI.gameObject.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        GameOverUI gameOver = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        GameObject goPanel = null;
        if (gameOver != null)
        {
            SerializedObject serGO = new SerializedObject(gameOver);
            goPanel = serGO.FindProperty("_panel")?.objectReferenceValue as GameObject;
            gameOver.gameObject.SetActive(false);
            if (goPanel != null) goPanel.SetActive(false);
        }

        VictoryUI victoryUI = Object.FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
        if (victoryUI != null)
        {
            SerializedObject serVic = new SerializedObject(victoryUI);
            GameObject vicPanel = serVic.FindProperty("_panel")?.objectReferenceValue as GameObject;
            victoryUI.gameObject.SetActive(false);
            if (vicPanel != null) vicPanel.SetActive(false);
        }

        SettingsUI settingsUI = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settingsUI != null)
        {
            SerializedObject serSet = new SerializedObject(settingsUI);
            GameObject setPanel = serSet.FindProperty("_panel")?.objectReferenceValue as GameObject;
            settingsUI.gameObject.SetActive(false);
            if (setPanel != null) setPanel.SetActive(false);
        }

        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null) hudObj.SetActive(true);

        GameObject weaponHudObj = GameObject.Find("WeaponHUD");
        if (weaponHudObj != null) weaponHudObj.SetActive(true);

        // Warmup render to ensure all shaders and materials are loaded
        cam.Render();

        // 1. Shot 1: Arena Overview (elevated angle) - UI hidden
        cam.transform.position = new Vector3(0f, 24f, -24f);
        cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        SaveCaptureToAllDirs("Arena_Overview.png", cam, outputDirs, 1920, 1080);

        // Setup ScreenSpaceCamera for UI capture
        if (canvas != null)
        {
            cam.cullingMask |= (1 << LayerMask.NameToLayer("UI"));
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;
        }

        // 2. Shot 2: Combat Action Encounter (elevated combat angle with active HUD)
        cam.transform.position = new Vector3(0f, 13f, -9f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        List<GameObject> tempCombatObjects = new List<GameObject>();
        string[] prefabs = {
            "Assets/_Project/Enemy.prefab",
            "Assets/_Project/Enemy_Fast.prefab",
            "Assets/_Project/Enemy_Charger.prefab",
            "Assets/_Project/Enemy_Ranged.prefab",
            "Assets/_Project/Enemy_Tank.prefab",
            "Assets/_Project/Enemy_Boss.prefab"
        };
        Vector3[] spawnPositions = {
            new Vector3(0f, 0f, 7f),
            new Vector3(-5f, 0f, 5.5f),
            new Vector3(5f, 0f, 6.5f),
            new Vector3(-8f, 0f, 10f),
            new Vector3(8f, 0f, 9.5f),
            new Vector3(0f, 0f, 14f)
        };

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject pObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
            if (pObj != null)
            {
                GameObject inst = Object.Instantiate(pObj, spawnPositions[i], Quaternion.Euler(0f, 180f, 0f));
                tempCombatObjects.Add(inst);

                Transform tel = inst.transform.Find("TelegraphLine");
                if (tel != null)
                {
                    LineRenderer lr = tel.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        lr.enabled = true;
                        lr.SetPosition(0, spawnPositions[i] + new Vector3(0f, 0.05f, 0f));
                        lr.SetPosition(1, new Vector3(0f, 0.05f, 0f));
                    }
                }

                Transform shock = inst.transform.Find("ShockwaveTelegraph");
                if (shock != null) shock.gameObject.SetActive(true);
            }
        }

        GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Projectile.prefab");
        if (projPrefab != null)
        {
            GameObject bullet = Object.Instantiate(projPrefab, new Vector3(0f, 0.6f, 2.8f), Quaternion.identity);
            tempCombatObjects.Add(bullet);
        }

        SaveCaptureToAllDirs("Arena_Combat_Action.png", cam, outputDirs, 1920, 1080);

        foreach (var obj in tempCombatObjects)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }

        // 3. Shot 3: Game Over UI
        if (gameOver != null) gameOver.gameObject.SetActive(true);
        if (goPanel != null) goPanel.SetActive(true);
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        SaveCaptureToAllDirs("Arena_GameOver_UI.png", cam, outputDirs, 1920, 1080);
        if (goPanel != null) goPanel.SetActive(false);
        if (gameOver != null) gameOver.gameObject.SetActive(false);

        // 4. Shot 4: Pause UI
        if (pauseUI != null) pauseUI.gameObject.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(true);
        SaveCaptureToAllDirs("Arena_Pause_UI.png", cam, outputDirs, 1920, 1080);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);

        // Restore canvas & camera
        if (canvas != null)
        {
            canvas.renderMode = originalRenderMode;
            canvas.worldCamera = originalCanvasCam;
            canvas.planeDistance = originalPlaneDist;
        }

        cam.transform.position = originalCamPos;
        cam.transform.rotation = originalCamRot;
        cam.clearFlags = originalClearFlags;
        cam.backgroundColor = originalBgColor;
        cam.cullingMask = originalCullingMask;
    }

    private static void SaveCaptureToAllDirs(string filename, Camera cam, string[] dirs, int width, int height)
    {
        byte[] png = CaptureCameraToPNGData(cam, width, height);
        foreach (var dir in dirs)
        {
            string p = Path.Combine(dir, filename).Replace("\\", "/");
            File.WriteAllBytes(p, png);
            Debug.Log($"[GeometricArenaPolish] Captured: {p}");
        }
    }

    public static void CaptureScreenshotsBatch()
    {
        try
        {
            CaptureScreenshots();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeometricArenaPolish] Screenshot capture failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    private static byte[] CaptureCameraToPNGData(Camera cam, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRT;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        return pngData;
    }
}
