using Application.Economy;

namespace Game.Economy
{
    /// <summary>
    /// Test fixture / permissive economy policy where all weapons and parts are considered unlocked.
    /// Used by existing test suites or fixtures that explicitly bypass progression gating.
    /// </summary>
    public sealed class PermissiveEconomyPolicy : IEconomyPolicy
    {
        public static readonly PermissiveEconomyPolicy Instance = new PermissiveEconomyPolicy();

        public bool IsWeaponUnlocked(string weaponId) => true;

        public bool IsPartUnlocked(string weaponId, string slotId, string partId) => true;
    }
}
