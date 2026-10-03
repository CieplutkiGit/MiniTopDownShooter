namespace Application.Weapons
{
    public interface IWeaponBuildStore
    {
        /// <summary>
        /// Loads the preferred build for the weapon platform.
        /// </summary>
        BuildLoadResult Load(string weaponId);

        /// <summary>
        /// Atomically saves the committed build for the weapon platform.
        /// </summary>
        SaveResult Save(WeaponBuild build);
    }
}
