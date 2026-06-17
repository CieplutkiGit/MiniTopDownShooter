namespace Core
{
    public struct EnemyStatsData
    {
        public readonly float MoveSpeed;
        public readonly int MaxHealth;
        public readonly int Damage;
        public readonly float AttackRange;
        public readonly float AttackCooldown;
        public readonly int ScoreValue;

        public EnemyStatsData(
            float moveSpeed,
            int maxHealth,
            int damage,
            float attackRange,
            float attackCooldown,
            int scoreValue)
        {
            MoveSpeed = moveSpeed;
            MaxHealth = maxHealth;
            Damage = damage;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            ScoreValue = scoreValue;
        }
    }
}
