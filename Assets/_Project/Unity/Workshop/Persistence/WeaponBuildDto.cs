using System;
using System.Collections.Generic;
using Application.Weapons;

namespace Game
{
    [Serializable]
    public class SlotSelectionDto
    {
        public string SlotId;
        public string PartId;

        public SlotSelectionDto()
        {
        }

        public SlotSelectionDto(string slotId, string partId)
        {
            SlotId = slotId;
            PartId = partId;
        }
    }

    [Serializable]
    public class WeaponBuildDto
    {
        public string WeaponId;
        public List<SlotSelectionDto> Slots = new List<SlotSelectionDto>();

        public WeaponBuildDto()
        {
        }

        public WeaponBuildDto(string weaponId, List<SlotSelectionDto> slots = null)
        {
            WeaponId = weaponId;
            Slots = slots ?? new List<SlotSelectionDto>();
        }

        public WeaponBuildDto(WeaponBuild build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            WeaponId = build.WeaponId;
            Slots = new List<SlotSelectionDto>();
            if (build.Selections != null)
            {
                foreach (var kvp in build.Selections)
                {
                    Slots.Add(new SlotSelectionDto(kvp.Key, kvp.Value));
                }
            }
        }

        public WeaponBuild ToDomain()
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (Slots != null)
            {
                foreach (var slot in Slots)
                {
                    if (slot != null && !string.IsNullOrWhiteSpace(slot.SlotId) && !string.IsNullOrWhiteSpace(slot.PartId))
                    {
                        dict[slot.SlotId] = slot.PartId;
                    }
                }
            }
            return new WeaponBuild(WeaponId, dict);
        }

        public WeaponBuild ToWeaponBuild()
        {
            return ToDomain();
        }

        public static WeaponBuildDto FromDomain(WeaponBuild build)
        {
            return new WeaponBuildDto(build);
        }

        public static WeaponBuildDto FromWeaponBuild(WeaponBuild build)
        {
            return new WeaponBuildDto(build);
        }
    }
}
