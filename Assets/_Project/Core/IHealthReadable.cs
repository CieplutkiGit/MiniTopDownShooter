using System;

namespace Core
{
    public interface IHealthReadable
    {
        int Current { get; }
        int Max { get; }
        event Action OnDead;
        event Action<int, int> OnHealthChanged;
    }
}
