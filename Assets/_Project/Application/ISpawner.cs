using System;

namespace Application
{
    public interface ISpawner
    {
        event Action<int> EnemyKilled;
        bool SpawnOne();
        void ClearAllAlive();
    }
}
