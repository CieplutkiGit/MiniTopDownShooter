using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Weapons
{
    public sealed class WeaponBuildResolver : IWeaponBuildResolver
    {
        public BuildResolution Resolve(WeaponBuild build, IWeaponCatalog catalog)
        {
            if (build == null)
            {
                return BuildResolution.Failure("Build cannot be null.");
            }

            if (catalog == null)
            {
                return BuildResolution.Failure("Catalog cannot be null.");
            }

            if (!catalog.TryGetPlatform(build.WeaponId, out var platform))
            {
                return BuildResolution.Failure($"Unknown weapon platform '{build.WeaponId}'.", build.WeaponId);
            }

            var errors = new List<string>();
            var affectedIds = new List<string>();

            // 1. Check that all supported slots in platform are present in build
            if (platform.SupportedSlots != null)
            {
                foreach (var slot in platform.SupportedSlots)
                {
                    string partId = build.GetPart(slot);
                    if (string.IsNullOrWhiteSpace(partId))
                    {
                        errors.Add($"Missing required slot '{slot}'.");
                        if (!affectedIds.Contains(slot)) affectedIds.Add(slot);
                    }
                }
            }

            // 2. Check for unsupported slots in build
            if (build.Selections != null)
            {
                foreach (var kvp in build.Selections)
                {
                    string slot = kvp.Key;
                    if (platform.SupportedSlots != null && !platform.SupportedSlots.Contains(slot))
                    {
                        errors.Add($"Slot '{slot}' is not supported by platform '{platform.WeaponId}'.");
                        if (!affectedIds.Contains(slot)) affectedIds.Add(slot);
                    }
                }
            }

            // 3. Lookup all parts in catalog and verify slot matching
            var equippedParts = new List<WeaponPartSpec>();
            if (build.Selections != null)
            {
                foreach (var kvp in build.Selections)
                {
                    string slotId = kvp.Key;
                    string partId = kvp.Value;

                    if (!catalog.TryGetPart(partId, out var part))
                    {
                        errors.Add($"Part '{partId}' not found in catalog for slot '{slotId}'.");
                        if (!affectedIds.Contains(partId)) affectedIds.Add(partId);
                    }
                    else
                    {
                        if (!string.Equals(part.SlotId, slotId, StringComparison.Ordinal))
                        {
                            errors.Add($"Part '{partId}' belongs to slot '{part.SlotId}', not '{slotId}'.");
                            if (!affectedIds.Contains(partId)) affectedIds.Add(partId);
                        }
                        else
                        {
                            equippedParts.Add(part);
                        }
                    }
                }
            }

            // If any slots are missing or parts are not found/invalid, fail early
            if (errors.Count > 0)
            {
                return BuildResolution.Failure(errors, affectedIds);
            }

            // 4. Check compatibility: IncompatiblePartIds across all equipped parts
            for (int i = 0; i < equippedParts.Count; i++)
            {
                for (int j = i + 1; j < equippedParts.Count; j++)
                {
                    var p1 = equippedParts[i];
                    var p2 = equippedParts[j];

                    bool p1IncompatibleWithP2 = p1.IncompatiblePartIds != null && p1.IncompatiblePartIds.Contains(p2.PartId);
                    bool p2IncompatibleWithP1 = p2.IncompatiblePartIds != null && p2.IncompatiblePartIds.Contains(p1.PartId);

                    if (p1IncompatibleWithP2 || p2IncompatibleWithP1)
                    {
                        errors.Add($"Incompatible parts: '{p1.PartId}' and '{p2.PartId}' cannot be equipped together.");
                        if (!affectedIds.Contains(p1.PartId)) affectedIds.Add(p1.PartId);
                        if (!affectedIds.Contains(p2.PartId)) affectedIds.Add(p2.PartId);
                    }
                }
            }

            // 5. Check RequiredMountTags vs ProvidedMountTags
            var providedMountTags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in equippedParts)
            {
                if (p.ProvidedMountTags != null)
                {
                    foreach (var tag in p.ProvidedMountTags)
                    {
                        if (!string.IsNullOrEmpty(tag))
                        {
                            providedMountTags.Add(tag);
                        }
                    }
                }
            }

            foreach (var p in equippedParts)
            {
                if (p.RequiredMountTags != null)
                {
                    foreach (var tag in p.RequiredMountTags)
                    {
                        if (!string.IsNullOrEmpty(tag) && !providedMountTags.Contains(tag))
                        {
                            errors.Add($"Part '{p.PartId}' requires missing mount tag '{tag}'.");
                            if (!affectedIds.Contains(p.PartId)) affectedIds.Add(p.PartId);
                        }
                    }
                }
            }

            // 6. Check behavior overrides for conflicts
            var fireModeParts = equippedParts.Where(p => p.FireModeOverride.HasValue).ToList();
            if (fireModeParts.Select(p => p.FireModeOverride.Value).Distinct().Count() > 1)
            {
                errors.Add("Conflicting FireMode overrides between equipped parts.");
                foreach (var p in fireModeParts)
                {
                    if (!affectedIds.Contains(p.PartId)) affectedIds.Add(p.PartId);
                }
            }

            var deliveryModeParts = equippedParts.Where(p => p.DeliveryModeOverride.HasValue).ToList();
            if (deliveryModeParts.Select(p => p.DeliveryModeOverride.Value).Distinct().Count() > 1)
            {
                errors.Add("Conflicting DeliveryMode overrides between equipped parts.");
                foreach (var p in deliveryModeParts)
                {
                    if (!affectedIds.Contains(p.PartId)) affectedIds.Add(p.PartId);
                }
            }

            var burstCountParts = equippedParts.Where(p => p.BurstCountOverride.HasValue).ToList();
            if (burstCountParts.Select(p => p.BurstCountOverride.Value).Distinct().Count() > 1)
            {
                errors.Add("Conflicting BurstCount overrides between equipped parts.");
                foreach (var p in burstCountParts)
                {
                    if (!affectedIds.Contains(p.PartId)) affectedIds.Add(p.PartId);
                }
            }

            var burstIntervalParts = equippedParts.Where(p => p.BurstIntervalOverride.HasValue).ToList();
            if (burstIntervalParts.Select(p => p.BurstIntervalOverride.Value).Distinct().Count() > 1)
            {
                errors.Add("Conflicting BurstInterval overrides between equipped parts.");
                foreach (var p in burstIntervalParts)
                {
                    if (!affectedIds.Contains(p.PartId)) affectedIds.Add(p.PartId);
                }
            }

            if (errors.Count > 0)
            {
                return BuildResolution.Failure(errors, affectedIds);
            }

            // 7. Calculate stats deterministically
            float damageDelta = 0f;
            float fireIntervalDelta = 0f;
            int magazineCapacityDelta = 0;
            int maxReserveAmmoDelta = 0;
            float reloadDurationDelta = 0f;
            float baseSpreadAngleDelta = 0f;
            float maxSpreadAngleDelta = 0f;
            float spreadPerShotDelta = 0f;
            float spreadRecoveryDelta = 0f;
            float rangeDelta = 0f;
            float projectileSpeedDelta = 0f;
            float projectileLifetimeDelta = 0f;
            int pelletsDelta = 0;
            float aimTurnSpeedDelta = 0f;

            float damageMultiplier = 1f;
            float fireIntervalMultiplier = 1f;
            float reloadDurationMultiplier = 1f;
            float spreadMultiplier = 1f;
            float rangeMultiplier = 1f;
            float projectileSpeedMultiplier = 1f;
            float aimTurnSpeedMultiplier = 1f;

            foreach (var p in equippedParts)
            {
                damageDelta += p.DamageDelta;
                fireIntervalDelta += p.FireIntervalDelta;
                magazineCapacityDelta += p.MagazineCapacityDelta;
                maxReserveAmmoDelta += p.MaxReserveAmmoDelta;
                reloadDurationDelta += p.ReloadDurationDelta;
                baseSpreadAngleDelta += p.BaseSpreadAngleDelta;
                maxSpreadAngleDelta += p.MaxSpreadAngleDelta;
                spreadPerShotDelta += p.SpreadPerShotDelta;
                spreadRecoveryDelta += p.SpreadRecoveryDelta;
                rangeDelta += p.RangeDelta;
                projectileSpeedDelta += p.ProjectileSpeedDelta;
                projectileLifetimeDelta += p.ProjectileLifetimeDelta;
                pelletsDelta += p.PelletsDelta;
                aimTurnSpeedDelta += p.AimTurnSpeedDelta;

                damageMultiplier *= p.DamageMultiplier;
                fireIntervalMultiplier *= p.FireIntervalMultiplier;
                reloadDurationMultiplier *= p.ReloadDurationMultiplier;
                spreadMultiplier *= p.SpreadMultiplier;
                rangeMultiplier *= p.RangeMultiplier;
                projectileSpeedMultiplier *= p.ProjectileSpeedMultiplier;
                aimTurnSpeedMultiplier *= p.AimTurnSpeedMultiplier;
            }

            float damage = (platform.BaseDamage + damageDelta) * damageMultiplier;
            float fireInterval = (platform.BaseFireInterval + fireIntervalDelta) * fireIntervalMultiplier;
            int magazineCapacity = platform.BaseMagazineCapacity + magazineCapacityDelta;
            int maxReserveAmmo = platform.BaseMaxReserveAmmo + maxReserveAmmoDelta;
            int startingReserveAmmo = platform.BaseStartingReserveAmmo;
            float reloadDuration = (platform.BaseReloadDuration + reloadDurationDelta) * reloadDurationMultiplier;
            float baseSpreadAngle = (platform.BaseSpreadAngle + baseSpreadAngleDelta) * spreadMultiplier;
            float maxSpreadAngle = (platform.MaxSpreadAngle + maxSpreadAngleDelta) * spreadMultiplier;
            float recoilPerShot = platform.SpreadPerShot + spreadPerShotDelta;
            float spreadRecoveryRate = platform.SpreadRecoveryPerSecond + spreadRecoveryDelta;
            float range = (platform.Range + rangeDelta) * rangeMultiplier;
            float projectileSpeed = (platform.ProjectileSpeed + projectileSpeedDelta) * projectileSpeedMultiplier;
            float projectileLifetime = platform.ProjectileLifetime + projectileLifetimeDelta;
            int pelletCount = platform.BasePellets + pelletsDelta;
            float aimTurnSpeed = (platform.AimTurnSpeed + aimTurnSpeedDelta) * aimTurnSpeedMultiplier;

            WeaponFireMode fireMode = platform.BaseFireMode;
            if (fireModeParts.Count > 0)
            {
                fireMode = fireModeParts[0].FireModeOverride.Value;
            }

            int burstCount = platform.BurstCount;
            if (burstCountParts.Count > 0)
            {
                burstCount = burstCountParts[0].BurstCountOverride.Value;
            }

            float burstInterval = platform.BurstInterval;
            if (burstIntervalParts.Count > 0)
            {
                burstInterval = burstIntervalParts[0].BurstIntervalOverride.Value;
            }

            WeaponDeliveryMode deliveryMode = platform.DeliveryMode;
            if (deliveryModeParts.Count > 0)
            {
                deliveryMode = deliveryModeParts[0].DeliveryModeOverride.Value;
            }

            var stats = new ResolvedWeaponStats(
                damage: damage,
                fireInterval: fireInterval,
                magazineCapacity: magazineCapacity,
                maxReserveAmmo: maxReserveAmmo,
                startingReserveAmmo: startingReserveAmmo,
                reloadDuration: reloadDuration,
                baseSpreadAngle: baseSpreadAngle,
                maxSpreadAngle: maxSpreadAngle,
                recoilPerShot: recoilPerShot,
                spreadRecoveryRate: spreadRecoveryRate,
                range: range,
                projectileSpeed: projectileSpeed,
                projectileLifetime: projectileLifetime,
                pelletCount: pelletCount,
                fireMode: fireMode,
                burstCount: burstCount,
                burstInterval: burstInterval,
                aimTurnSpeed: aimTurnSpeed,
                deliveryMode: deliveryMode,
                infiniteAmmo: platform.InfiniteAmmo,
                autoReloadOnEmpty: platform.AutoReloadOnEmpty,
                cancelReloadOnFire: platform.CancelReloadOnFire,
                damageFalloffStart: platform.DamageFalloffStart,
                damageFalloffEnd: platform.DamageFalloffEnd,
                minDamageRatio: platform.MinDamageRatio);

            return BuildResolution.Success(stats);
        }

        public WeaponBuild Normalize(WeaponBuild build, IWeaponCatalog catalog, out bool repaired)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            // If build is null or unknown platform, return default build for first platform in catalog (repaired = true)
            if (build == null || !catalog.TryGetPlatform(build.WeaponId, out var platform))
            {
                repaired = true;
                var allIds = catalog.GetAllWeaponIds();
                if (allIds != null && allIds.Count > 0 && catalog.TryGetPlatform(allIds[0], out var firstPlatform))
                {
                    return firstPlatform.CreateDefaultBuild();
                }
                return null;
            }

            bool anyRepaired = false;

            // Check if build had extra slots not supported by platform
            if (build.Selections != null)
            {
                foreach (var slot in build.Selections.Keys)
                {
                    if (platform.SupportedSlots == null || !platform.SupportedSlots.Contains(slot))
                    {
                        anyRepaired = true;
                        break;
                    }
                }
            }

            // Fill supported slots
            var candidateParts = new Dictionary<string, string>(StringComparer.Ordinal);
            if (platform.SupportedSlots != null)
            {
                foreach (var slotId in platform.SupportedSlots)
                {
                    string partId = build.GetPart(slotId);
                    bool isValidPart = !string.IsNullOrEmpty(partId) &&
                                       catalog.TryGetPart(partId, out var part) &&
                                       string.Equals(part.SlotId, slotId, StringComparison.Ordinal);

                    if (isValidPart)
                    {
                        candidateParts[slotId] = partId;
                    }
                    else
                    {
                        anyRepaired = true;
                        if (platform.DefaultParts.TryGetValue(slotId, out var defaultPartId))
                        {
                            candidateParts[slotId] = defaultPartId;
                        }
                    }
                }
            }

            // Check compatibility & overrides among candidate parts.
            // If any part causes incompatibility or conflicting overrides, replace non-default with default.
            bool changedInLoop;
            do
            {
                changedInLoop = false;
                var partSpecs = new Dictionary<string, WeaponPartSpec>(StringComparer.Ordinal);
                foreach (var kvp in candidateParts)
                {
                    if (catalog.TryGetPart(kvp.Value, out var spec))
                    {
                        partSpecs[kvp.Key] = spec;
                    }
                }

                if (platform.SupportedSlots != null)
                {
                    for (int i = 0; i < platform.SupportedSlots.Count; i++)
                    {
                        string slotI = platform.SupportedSlots[i];
                        if (!partSpecs.TryGetValue(slotI, out var specI)) continue;

                        platform.DefaultParts.TryGetValue(slotI, out var defaultI);
                        bool isDefaultI = string.Equals(candidateParts[slotI], defaultI, StringComparison.Ordinal);

                        for (int j = 0; j < i; j++)
                        {
                            string slotJ = platform.SupportedSlots[j];
                            if (!partSpecs.TryGetValue(slotJ, out var specJ)) continue;

                            bool incompatible = (specI.IncompatiblePartIds != null && specI.IncompatiblePartIds.Contains(specJ.PartId)) ||
                                                (specJ.IncompatiblePartIds != null && specJ.IncompatiblePartIds.Contains(specI.PartId));

                            bool conflictOverrides =
                                (specI.FireModeOverride.HasValue && specJ.FireModeOverride.HasValue && specI.FireModeOverride.Value != specJ.FireModeOverride.Value) ||
                                (specI.DeliveryModeOverride.HasValue && specJ.DeliveryModeOverride.HasValue && specI.DeliveryModeOverride.Value != specJ.DeliveryModeOverride.Value) ||
                                (specI.BurstCountOverride.HasValue && specJ.BurstCountOverride.HasValue && specI.BurstCountOverride.Value != specJ.BurstCountOverride.Value) ||
                                (specI.BurstIntervalOverride.HasValue && specJ.BurstIntervalOverride.HasValue && Math.Abs(specI.BurstIntervalOverride.Value - specJ.BurstIntervalOverride.Value) > 0.0001f);

                            if (incompatible || conflictOverrides)
                            {
                                if (!isDefaultI && !string.IsNullOrEmpty(defaultI))
                                {
                                    candidateParts[slotI] = defaultI;
                                    anyRepaired = true;
                                    changedInLoop = true;
                                    break;
                                }
                                else
                                {
                                    platform.DefaultParts.TryGetValue(slotJ, out var defaultJ);
                                    if (!string.Equals(candidateParts[slotJ], defaultJ, StringComparison.Ordinal) && !string.IsNullOrEmpty(defaultJ))
                                    {
                                        candidateParts[slotJ] = defaultJ;
                                        anyRepaired = true;
                                        changedInLoop = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (changedInLoop) break;
                    }
                }
            } while (changedInLoop);

            var repairedBuild = new WeaponBuild(platform.WeaponId, candidateParts);
            repaired = anyRepaired || !repairedBuild.Equals(build);
            return repairedBuild;
        }
    }
}
