namespace Application.Weapons
{
    public interface IWeaponBuildResolver
    {
        /// <summary>
        /// Validates a weapon build and calculates its deterministic resolved statistics against the catalog.
        /// </summary>
        BuildResolution Resolve(WeaponBuild build, IWeaponCatalog catalog);

        /// <summary>
        /// Sanitizes and repairs a build: fills missing slots with defaults, replaces obsolete or incompatible part IDs.
        /// </summary>
        WeaponBuild Normalize(WeaponBuild build, IWeaponCatalog catalog, out bool repaired);
    }
}
