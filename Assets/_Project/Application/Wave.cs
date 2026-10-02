namespace Application
{
    public struct Wave
    {
        public int EnemyCount;
        public float SpawnInterval;
        public float DelayAfter;
        public float InitialDelay;

        public Wave(
            int enemyCount,
            float spawnInterval,
            float delayAfter,
            float initialDelay = 0f)
        {
            EnemyCount = enemyCount;
            SpawnInterval = spawnInterval;
            DelayAfter = delayAfter;
            InitialDelay = initialDelay;
        }
    }
}
