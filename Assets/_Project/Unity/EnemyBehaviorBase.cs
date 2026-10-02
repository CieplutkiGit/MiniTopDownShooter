using Core;
using UnityEngine;

namespace Game
{
    public abstract class EnemyBehaviorBase : MonoBehaviour
    {
        protected EnemyController Owner { get; private set; }
        protected EnemyMovement Movement { get; private set; }
        protected EnemyAttack Attack { get; private set; }
        protected HealthComponent Health { get; private set; }
        protected Transform Target { get; private set; }
        protected IDamageable TargetDamageable { get; private set; }

        public void Initialize(
            EnemyController owner,
            EnemyMovement movement,
            EnemyAttack attack,
            HealthComponent health,
            Transform target,
            IDamageable targetDamageable)
        {
            Owner = owner;
            Movement = movement;
            Attack = attack;
            Health = health;
            Target = target;
            TargetDamageable = targetDamageable;
            OnSpawn();
        }

        public abstract void Tick();

        public virtual void OnDeath()
        {
        }

        public virtual void OnDespawn()
        {
        }

        protected virtual void OnSpawn()
        {
        }

        protected float DistanceToTarget()
        {
            return Target == null
                ? float.PositiveInfinity
                : Vector3.Distance(transform.position, Target.position);
        }

        protected Vector3 DirectionToTarget()
        {
            if (Target == null)
            {
                return Vector3.zero;
            }

            Vector3 direction = Target.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
        }
    }
}
