using System;
using Application.Weapons;

namespace Application.Workshop
{
    public interface IWeaponWorkshopSession
    {
        string WeaponId { get; }
        WeaponBuild CommittedBuild { get; }
        WeaponBuild DraftBuild { get; }
        ResolvedWeaponStats DraftStats { get; }
        BuildResolution DraftResolution { get; }
        bool HasUnappliedChanges { get; }
        bool IsValid { get; }
        bool LastSaveFailed { get; }
        string LastSaveError { get; }
        IWeaponBuildTarget Target { get; }

        void SelectPart(string slotId, string partId);
        ApplyResult Apply();
        void Discard();
        void BindTarget(IWeaponBuildTarget target);
        void SwitchWeapon(string weaponId, IWeaponBuildTarget target);

        event Action<IWeaponWorkshopSession> SessionChanged;
    }
}
