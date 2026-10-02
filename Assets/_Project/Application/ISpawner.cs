using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface ISpawner
    {
        event Action<int> EnemyKilled;
        bool SpawnOne();
        void ClearAllAlive();
    }
}
