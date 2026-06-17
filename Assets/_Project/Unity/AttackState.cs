using Application;
using Core;

namespace Game
{
    public class AttackState : IEnemyState
    {
        private readonly EnemyAttack _attack;
        private readonly IDamageable _target;

        public AttackState(EnemyAttack attack, IDamageable target)
        {
            _attack = attack;
            _target = target;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            _attack.TryAttack(_target);
        }

        public void Exit()
        {
        }
    }
}
