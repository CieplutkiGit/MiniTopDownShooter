using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public sealed class CatalogValidationReport
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public override string ToString()
        {
            if (IsValid)
            {
                return Warnings.Count > 0
                    ? $"Catalog Valid with {Warnings.Count} warnings:\n" + string.Join("\n", Warnings)
                    : "Catalog Valid. No issues found.";
            }

            return $"Catalog Invalid with {Errors.Count} errors:\n" + string.Join("\n", Errors);
        }
    }

    public static class WeaponCatalogValidation
    {
        public static CatalogValidationReport Validate(WeaponCatalog catalog)
        {
            if (catalog == null)
            {
                var report = new CatalogValidationReport();
                report.Errors.Add("Catalog reference is null.");
                return report;
            }

            return Validate(catalog.PlatformDefinitions, catalog.PartDefinitions);
        }

        public static CatalogValidationReport Validate(
            IEnumerable<WeaponPlatformDefinition> platforms,
            IEnumerable<WeaponPartDefinition> parts)
        {
            var report = new CatalogValidationReport();

            var platformList = platforms != null ? platforms.ToList() : new List<WeaponPlatformDefinition>();
            var partList = parts != null ? parts.ToList() : new List<WeaponPartDefinition>();

            // 1. Check for null elements
            for (int i = 0; i < platformList.Count; i++)
            {
                if (platformList[i] == null)
                {
                    report.Errors.Add($"Platform list contains null entry at index {i}.");
                }
            }

            for (int i = 0; i < partList.Count; i++)
            {
                if (partList[i] == null)
                {
                    report.Errors.Add($"Part list contains null entry at index {i}.");
                }
            }

            // 2. Validate Platform IDs & Duplicates
            var seenWeaponIds = new HashSet<string>(StringComparer.Ordinal);
            var validPlatforms = new List<WeaponPlatformDefinition>();
            foreach (var platform in platformList)
            {
                if (platform == null) continue;
                if (string.IsNullOrEmpty(platform.WeaponId))
                {
                    report.Errors.Add("Platform definition has null or empty WeaponId.");
                    continue;
                }

                if (!seenWeaponIds.Add(platform.WeaponId))
                {
                    report.Errors.Add($"Duplicate platform ID detected: '{platform.WeaponId}'.");
                }
                else
                {
                    validPlatforms.Add(platform);
                }
            }

            // 3. Validate Part IDs & Duplicates
            var seenPartIds = new HashSet<string>(StringComparer.Ordinal);
            var partLookup = new Dictionary<string, WeaponPartDefinition>(StringComparer.Ordinal);
            var validParts = new List<WeaponPartDefinition>();
            foreach (var part in partList)
            {
                if (part == null) continue;
                if (string.IsNullOrEmpty(part.PartId))
                {
                    report.Errors.Add("Part definition has null or empty PartId.");
                    continue;
                }

                if (!seenPartIds.Add(part.PartId))
                {
                    report.Errors.Add($"Duplicate part ID detected: '{part.PartId}'.");
                }
                else
                {
                    partLookup[part.PartId] = part;
                    validParts.Add(part);
                }
            }

            // 4. Collect all supported slot IDs across platforms
            var allSupportedSlotIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var platform in validPlatforms)
            {
                if (platform.SupportedSlots != null)
                {
                    foreach (var slot in platform.SupportedSlots)
                    {
                        if (string.IsNullOrEmpty(slot))
                        {
                            report.Errors.Add($"Platform '{platform.WeaponId}' contains a null or empty supported slot ID.");
                        }
                        else
                        {
                            allSupportedSlotIds.Add(slot);
                        }
                    }
                }
            }

            // 5. Validate that all parts reference supported slot IDs
            foreach (var part in validParts)
            {
                if (string.IsNullOrEmpty(part.SlotId))
                {
                    report.Errors.Add($"Part '{part.PartId}' has null or empty SlotId.");
                }
                else if (!allSupportedSlotIds.Contains(part.SlotId))
                {
                    report.Errors.Add($"Part '{part.PartId}' references slot '{part.SlotId}' which is not supported by any platform in the catalog.");
                }
            }

            // 6. Validate platform default parts
            foreach (var platform in validPlatforms)
            {
                var supportedSlotsSet = new HashSet<string>(
                    platform.SupportedSlots != null ? platform.SupportedSlots : Enumerable.Empty<string>(),
                    StringComparer.Ordinal);

                var defaultPartsMap = new Dictionary<string, string>(StringComparer.Ordinal);
                if (platform.DefaultParts != null)
                {
                    foreach (var pair in platform.DefaultParts)
                    {
                        if (string.IsNullOrEmpty(pair.SlotId))
                        {
                            report.Errors.Add($"Platform '{platform.WeaponId}' has a default part mapping with empty slot ID.");
                            continue;
                        }

                        if (defaultPartsMap.ContainsKey(pair.SlotId))
                        {
                            report.Errors.Add($"Platform '{platform.WeaponId}' has duplicate default part mapping for slot '{pair.SlotId}'.");
                        }
                        else
                        {
                            defaultPartsMap[pair.SlotId] = pair.PartId;
                        }
                    }
                }

                // Check that every supported slot has a default part
                foreach (var slot in supportedSlotsSet)
                {
                    if (!defaultPartsMap.TryGetValue(slot, out var defaultPartId) || string.IsNullOrEmpty(defaultPartId))
                    {
                        report.Errors.Add($"Platform '{platform.WeaponId}' is missing default part for supported slot '{slot}'.");
                        continue;
                    }

                    // Check that the default part exists in the catalog
                    if (!partLookup.TryGetValue(defaultPartId, out var partDef))
                    {
                        report.Errors.Add($"Platform '{platform.WeaponId}' default part '{defaultPartId}' for slot '{slot}' does not exist in catalog.");
                    }
                    else if (partDef.SlotId != slot)
                    {
                        report.Errors.Add($"Platform '{platform.WeaponId}' default part '{defaultPartId}' is configured for slot '{slot}', but part belongs to slot '{partDef.SlotId}'.");
                    }
                }

                // Check for default parts that specify a slot not supported by the platform
                foreach (var kvp in defaultPartsMap)
                {
                    if (!supportedSlotsSet.Contains(kvp.Key))
                    {
                        report.Errors.Add($"Platform '{platform.WeaponId}' specifies default part '{kvp.Value}' for slot '{kvp.Key}' which is not in its supported slots.");
                    }
                }
            }

            // 7. Validate that incompatible part references actually exist in the catalog
            foreach (var part in validParts)
            {
                if (part.IncompatiblePartIds != null)
                {
                    foreach (var incompatibleId in part.IncompatiblePartIds)
                    {
                        if (string.IsNullOrEmpty(incompatibleId))
                        {
                            report.Errors.Add($"Part '{part.PartId}' has null or empty entry in incompatible part list.");
                        }
                        else if (!seenPartIds.Contains(incompatibleId))
                        {
                            report.Errors.Add($"Part '{part.PartId}' references incompatible part '{incompatibleId}' which does not exist in the catalog.");
                        }
                        else if (string.Equals(incompatibleId, part.PartId, StringComparison.Ordinal))
                        {
                            report.Warnings.Add($"Part '{part.PartId}' references itself as incompatible.");
                        }
                    }
                }
            }

            return report;
        }
    }
}
