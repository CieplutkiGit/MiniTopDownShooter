using System.Collections.Generic;

namespace Application.Weapons
{
    public interface IWeaponCatalog
    {
        WeaponPlatformSpec GetPlatform(string weaponId);
        bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform);
        WeaponPartSpec GetPart(string partId);
        bool TryGetPart(string partId, out WeaponPartSpec part);
        IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId);
        IReadOnlyList<string> GetAllWeaponIds();
    }
}
