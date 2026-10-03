namespace Application.Weapons
{
    public interface IWeaponBuildTarget
    {
        string WeaponId { get; }
        WeaponBuild CurrentBuild { get; }
        ResolvedWeaponStats CurrentStats { get; }

        /// <summary>
        /// Applies the resolved build and stats to the live weapon target, conserving ammo and failing safely.
        /// </summary>
        ApplyResult TryApply(WeaponBuild build, ResolvedWeaponStats stats);
    }
}
