namespace Application
{
    public interface IEnemyStats
    {
        float MoveSpeed { get; }
        int MaxHealth { get; }
        int Damage { get; }
        float AttackRange { get; }
        float AttackCooldown { get; }
        int ScoreValue { get; }
    }
}
