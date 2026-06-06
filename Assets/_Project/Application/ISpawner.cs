using System;

namespace Application
{
    public interface ISpawner
    {
        event Action EnemyKilled;
        void SpawnOne();
    }
}
