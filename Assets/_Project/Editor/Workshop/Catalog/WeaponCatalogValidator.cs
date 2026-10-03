using System.Collections.Generic;
using Game;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class WeaponCatalogValidator
    {
        public static CatalogValidationReport Validate(WeaponCatalog catalog)
        {
            return WeaponCatalogValidation.Validate(catalog);
        }

        public static CatalogValidationReport Validate(
            IEnumerable<WeaponPlatformDefinition> platforms,
            IEnumerable<WeaponPartDefinition> parts)
        {
            return WeaponCatalogValidation.Validate(platforms, parts);
        }

        [MenuItem("Tools/Mini Top Down Shooter/Workshop/Validate All Weapon Catalogs")]
        public static void ValidateAllCatalogs()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponCatalog");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[WeaponCatalogValidator] No WeaponCatalog assets found in project.");
                return;
            }

            int totalErrors = 0;
            int totalWarnings = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(path);
                if (catalog == null) continue;

                var report = Validate(catalog);
                if (!report.IsValid)
                {
                    totalErrors += report.Errors.Count;
                    Debug.LogError($"[WeaponCatalogValidator] Validation FAILED for '{path}':\n{report}");
                }
                else
                {
                    if (report.Warnings.Count > 0)
                    {
                        totalWarnings += report.Warnings.Count;
                        Debug.LogWarning($"[WeaponCatalogValidator] Validation PASSED with warnings for '{path}':\n{report}");
                    }
                    else
                    {
                        Debug.Log($"[WeaponCatalogValidator] Validation PASSED for '{path}'.");
                    }
                }
            }

            if (totalErrors == 0)
            {
                Debug.Log($"[WeaponCatalogValidator] All {guids.Length} WeaponCatalog asset(s) are valid. (Warnings: {totalWarnings})");
            }
            else
            {
                Debug.LogError($"[WeaponCatalogValidator] Completed with {totalErrors} total error(s) across {guids.Length} catalog(s).");
            }
        }
    }
}
