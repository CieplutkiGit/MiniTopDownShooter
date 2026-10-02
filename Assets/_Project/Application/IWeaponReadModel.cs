namespace Application
{
    public interface IWeaponReadModel
    {
        int AmmoInMagazine { get; }
        int ReserveAmmo { get; }
        int MagazineSize { get; }
        bool InfiniteAmmo { get; }
        bool IsReloading { get; }
    }
}
