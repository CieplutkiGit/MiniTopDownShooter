using System;
using System.Collections.Generic;
using Application.Weapons;
using Game.Workshop.Presentation;
using UnityEngine;

namespace Game.Workshop
{
    /// <summary>
    /// Central integration helper that loads saved weapon builds, resolves them against
    /// the weapon catalog, applies them to runtime Gun targets, and updates modular visuals.
    /// </summary>
    public static class WeaponBuildApplier
    {
        private static IWeaponCatalog s_catalog;
        private static IWeaponBuildResolver s_resolver = new WeaponBuildResolver();
        private static IWeaponBuildStore s_store = new SaveManagerWeaponBuildStore();
        private static readonly Dictionary<string, WeaponVisualProfile> s_visualProfiles = new Dictionary<string, WeaponVisualProfile>(StringComparer.Ordinal);

        public static IWeaponCatalog DefaultCatalog
        {
            get
            {
                if (s_catalog == null)
                {
#if UNITY_EDITOR
                    s_catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponCatalog>("Assets/_Project/Data/WeaponCustomization/MasterWeaponCatalog.asset");
#endif
                }
                return s_catalog;
            }
            set => s_catalog = value;
        }

        public static IWeaponBuildResolver DefaultResolver
        {
            get => s_resolver ?? (s_resolver = new WeaponBuildResolver());
            set => s_resolver = value ?? new WeaponBuildResolver();
        }

        public static IWeaponBuildStore DefaultStore
        {
            get => s_store ?? (s_store = new SaveManagerWeaponBuildStore());
            set => s_store = value ?? new SaveManagerWeaponBuildStore();
        }

        public static void SetCatalog(IWeaponCatalog catalog) => s_catalog = catalog;
        public static void SetResolver(IWeaponBuildResolver resolver) => DefaultResolver = resolver;
        public static void SetStore(IWeaponBuildStore store) => DefaultStore = store;

        public static void RegisterVisualProfile(WeaponVisualProfile profile)
        {
            if (profile != null && !string.IsNullOrEmpty(profile.WeaponId))
            {
                s_visualProfiles[profile.WeaponId] = profile;
            }
        }

        public static WeaponVisualProfile GetVisualProfile(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return null;

            if (s_visualProfiles.TryGetValue(weaponId, out var profile) && profile != null)
            {
                return profile;
            }

#if UNITY_EDITOR
            string assetPath = weaponId switch
            {
                WeaponWorkshopIds.Pistol => "Assets/_Project/Data/WeaponCustomization/Pistol/VisualProfile_Pistol.asset",
                WeaponWorkshopIds.Rifle => "Assets/_Project/Data/WeaponCustomization/Rifle/VisualProfile_Rifle.asset",
                WeaponWorkshopIds.SMG => "Assets/_Project/Data/WeaponCustomization/SMG/VisualProfile_SMG.asset",
                WeaponWorkshopIds.Shotgun => "Assets/_Project/Data/WeaponCustomization/Shotgun/VisualProfile_Shotgun.asset",
                WeaponWorkshopIds.Launcher => "Assets/_Project/Data/WeaponCustomization/Launcher/VisualProfile_Launcher.asset",
                _ => null
            };

            if (assetPath != null)
            {
                profile = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(assetPath);
                if (profile != null)
                {
                    s_visualProfiles[weaponId] = profile;
                    return profile;
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// Loads the saved build for the gun's weapon ID (or default build), normalizes it,
        /// resolves its stats, and applies it to the gun runtime and visual assembler.
        /// </summary>
        public static bool ApplySavedBuild(Gun gun, IWeaponCatalog catalog = null, IWeaponBuildResolver resolver = null, IWeaponBuildStore store = null)
        {
            if (gun == null || gun.Definition == null) return false;

            catalog = catalog ?? DefaultCatalog;
            resolver = resolver ?? DefaultResolver;
            store = store ?? DefaultStore;

            if (catalog == null) return false;

            string weaponId = gun.WeaponId;
            if (string.IsNullOrEmpty(weaponId)) return false;

            WeaponBuild buildToApply = null;

            // 1. Try loading saved build
            BuildLoadResult loadResult = store.Load(weaponId);
            if (loadResult.IsSuccess && loadResult.Build != null)
            {
                buildToApply = resolver.Normalize(loadResult.Build, catalog, out bool repaired);
                if (repaired)
                {
                    store.Save(buildToApply);
                }
            }
            else
            {
                // Fallback: create default platform build
                if (catalog.TryGetPlatform(weaponId, out var platformSpec))
                {
                    buildToApply = platformSpec.CreateDefaultBuild();
                }
            }

            if (buildToApply == null) return false;

            return ApplyBuild(gun, buildToApply, catalog, resolver);
        }

        /// <summary>
        /// Resolves and applies an explicit WeaponBuild to a Gun target and updates visuals if an assembler is present.
        /// </summary>
        public static bool ApplyBuild(Gun gun, WeaponBuild build, IWeaponCatalog catalog = null, IWeaponBuildResolver resolver = null)
        {
            if (gun == null || build == null) return false;

            catalog = catalog ?? DefaultCatalog;
            resolver = resolver ?? DefaultResolver;

            if (catalog == null) return false;

            BuildResolution resolution = resolver.Resolve(build, catalog);
            if (!resolution.IsValid) return false;

            ApplyResult applyResult = gun.TryApply(build, resolution.Stats);
            if (!applyResult.IsSuccess) return false;

            // Update visual assembler if present
            WeaponModelAssembler assembler = gun.GetComponentInChildren<WeaponModelAssembler>();
            if (assembler != null)
            {
                WeaponVisualProfile profile = GetVisualProfile(build.WeaponId);
                if (profile != null)
                {
                    assembler.Assemble(build, profile);
                    if (assembler.LiveMuzzleAnchor != null)
                    {
                        gun.SetSpawnPoint(assembler.LiveMuzzleAnchor);
                    }
                }
            }

            return true;
        }
    }
}
