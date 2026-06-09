using System;

namespace Application
{
    public interface ISpawner
    {
        event Action EnemyKilled;
        bool SpawnOne();
        void ClearAllAlive();
    }
}
