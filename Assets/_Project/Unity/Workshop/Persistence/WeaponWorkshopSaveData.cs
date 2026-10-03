using System;
using System.Collections.Generic;

namespace Game
{
    [Serializable]
    public class WeaponWorkshopSaveData
    {
        public int Version = 1;
        public List<WeaponBuildDto> Builds = new List<WeaponBuildDto>();

        [NonSerialized]
        public bool WasMigratedOrRepaired;

        public void ValidateAndMigrate()
        {
            if (Version < 1)
            {
                Version = 1;
                WasMigratedOrRepaired = true;
            }

            if (Builds == null)
            {
                Builds = new List<WeaponBuildDto>();
                WasMigratedOrRepaired = true;
                return;
            }

            int removedBuilds = Builds.RemoveAll(b => b == null || string.IsNullOrWhiteSpace(b.WeaponId));
            if (removedBuilds > 0)
            {
                WasMigratedOrRepaired = true;
            }

            foreach (var build in Builds)
            {
                if (build.Slots == null)
                {
                    build.Slots = new List<SlotSelectionDto>();
                    WasMigratedOrRepaired = true;
                }
                else
                {
                    int removedSlots = build.Slots.RemoveAll(s => s == null || string.IsNullOrWhiteSpace(s.SlotId) || string.IsNullOrWhiteSpace(s.PartId));
                    if (removedSlots > 0)
                    {
                        WasMigratedOrRepaired = true;
                    }
                }
            }
        }
    }
}
