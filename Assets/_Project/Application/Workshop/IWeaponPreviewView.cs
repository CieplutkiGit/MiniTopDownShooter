using System;
using Application.Weapons;

namespace Application.Workshop
{
    public interface IWeaponPreviewView
    {
        void ShowBuild(WeaponBuild build);
        void SelectSlot(string slotId);
        void SetExploded(bool exploded);

        event Action<string> SlotSelected;
    }
}
