namespace Application.Economy
{
    public interface IEconomyPolicy
    {
        bool IsWeaponUnlocked(string weaponId);
        bool IsPartUnlocked(string weaponId, string slotId, string partId);
    }
}
