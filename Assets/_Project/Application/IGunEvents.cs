using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IGunEvents
    {
        event Action Fired;
        event Action EmptyFired;
        event Action ReloadStarted;
        event Action ReloadCompleted;
        event Action ReloadCanceled;
        event Action Equipped;
        event Action Unequipped;
        event Action<int, int> AmmoChanged;
    }
}
