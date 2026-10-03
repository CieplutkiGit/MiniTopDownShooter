using System;
using System.Collections.Generic;
using Application.Weapons;

namespace Game
{
    public class SaveManagerWeaponBuildStore : IWeaponBuildStore
    {
        public BuildLoadResult Load(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return BuildLoadResult.Failure("No saved build for weapon (empty weapon ID).");
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
