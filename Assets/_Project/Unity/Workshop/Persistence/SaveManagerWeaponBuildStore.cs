using System;
using System.Collections.Generic;
using Application.Economy;
using Application.Weapons;

namespace Game
{
    public class SaveManagerWeaponBuildStore : IWeaponBuildStore
    {
        public static IEconomyPolicy ActivePolicy { get; set; }
        public IEconomyPolicy EconomyPolicy { get; set; }
        private IEconomyPolicy EffectivePolicy => EconomyPolicy ?? ActivePolicy ?? EconomyPolicyProvider.DefaultPolicy;

        public SaveManagerWeaponBuildStore()
        {
        }

        public SaveManagerWeaponBuildStore(IEconomyPolicy economyPolicy)
        {
            EconomyPolicy = economyPolicy;
        }

        public BuildLoadResult Load(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return BuildLoadResult.Failure("No saved build for weapon (empty weapon ID).");
            }

            var policy = EffectivePolicy;
            if (policy != null && !policy.IsWeaponUnlocked(weaponId))
            {
                return BuildLoadResult.Failure($"Cannot load build: Weapon '{weaponId}' is locked.");
            }

            try
            {
                WeaponWorkshopSaveData data = SaveManager.LoadWorkshopData();
                if (data == null)
                {
                    return BuildLoadResult.Failure("No saved build for weapon (workshop save data unavailable).");
                }

                WeaponBuildDto dto = data.Builds?.Find(b => b != null && string.Equals(b.WeaponId, weaponId, StringComparison.Ordinal));
                if (dto == null)
                {
                    return BuildLoadResult.Failure($"No saved build for weapon: {weaponId}");
                }

                WeaponBuild build = dto.ToDomain();
                if (policy != null && build.Selections != null)
                {
                    var sanitized = new Dictionary<string, string>();
                    bool repaired = false;
                    foreach (var kvp in build.Selections)
                    {
                        if (string.IsNullOrEmpty(kvp.Value) || policy.IsPartUnlocked(weaponId, kvp.Key, kvp.Value))
                        {
                            sanitized[kvp.Key] = kvp.Value;
                        }
                        else
                        {
                            repaired = true;
                        }
                    }

                    if (repaired)
                    {
                        build = new WeaponBuild(weaponId, sanitized);
                        return BuildLoadResult.Success(build, wasMigratedOrRepaired: true);
                    }
                }

                return BuildLoadResult.Success(build, data.WasMigratedOrRepaired);
            }
            catch (Exception ex)
            {
                return BuildLoadResult.Failure($"Failed to load build for weapon '{weaponId}': {ex.Message}");
            }
        }

        public SaveResult Save(WeaponBuild build)
        {
            if (build == null)
            {
                return SaveResult.Failure("Weapon build cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(build.WeaponId))
            {
                return SaveResult.Failure("Weapon build must have a valid WeaponId.");
            }

            var policy = EffectivePolicy;
            if (policy != null)
            {
                if (!policy.IsWeaponUnlocked(build.WeaponId))
                {
                    return SaveResult.Failure($"Cannot save build: Weapon platform '{build.WeaponId}' is locked.");
                }

                if (build.Selections != null)
                {
                    foreach (var kvp in build.Selections)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value) && !policy.IsPartUnlocked(build.WeaponId, kvp.Key, kvp.Value))
                        {
                            return SaveResult.Failure($"Cannot save build: Part '{kvp.Value}' is unowned.");
                        }
                    }
                }
            }

            try
            {
                WeaponWorkshopSaveData data = SaveManager.LoadWorkshopData();
                if (data == null)
                {
                    data = new WeaponWorkshopSaveData();
                }

                if (data.Builds == null)
                {
                    data.Builds = new List<WeaponBuildDto>();
                }

                WeaponBuildDto dto = new WeaponBuildDto(build);
                int index = data.Builds.FindIndex(b => b != null && string.Equals(b.WeaponId, build.WeaponId, StringComparison.Ordinal));
                if (index >= 0)
                {
                    data.Builds[index] = dto;
                }
                else
                {
                    data.Builds.Add(dto);
                }

                bool success = SaveManager.SaveWorkshopData(data);
                if (!success)
                {
                    return SaveResult.Failure("Failed to persist workshop save data via SaveManager.");
                }

                return SaveResult.Success();
            }
            catch (Exception ex)
            {
                return SaveResult.Failure($"Exception occurred while saving workshop build: {ex.Message}");
            }
        }
    }
}
