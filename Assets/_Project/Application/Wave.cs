namespace Application
{
    public struct Wave
    {
        public int EnemyCount;
        public float SpawnInterval;
        public float DelayAfter;

        public Wave(int enemyCount, float spawnInterval, float delayAfter)
        {
            EnemyCount = enemyCount;
            SpawnInterval = spawnInterval;
            DelayAfter = delayAfter;
        }
    }
}
