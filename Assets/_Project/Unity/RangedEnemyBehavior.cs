using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class RangedEnemyBehavior : EnemyBehaviorBase
    {
        [SerializeField] private Gun _gun;

        [Min(0.1f)]
        [SerializeField] private float _preferredRange = 9f;

        [Min(0.1f)]
        [SerializeField] private float _retreatRange = 5f;

        [Min(0.1f)]
        [SerializeField] private float _fireRange = 14f;

        [Min(0.1f)]
        [SerializeField] private float _retreatDistance = 4f;

        public Gun Gun => _gun;

        public override void Tick()
        {
            if (Target == null)
            {
                return;
            }

            float distance = DistanceToTarget();

            if (distance < _retreatRange)
            {
                Movement.MoveAwayFrom(Target.position, _retreatDistance);
            }
            else if (distance > _preferredRange)
            {
                Movement.MoveToward(Target.position);
            }
            else
            {
                Movement.Stop();
            }

            if (_gun != null && distance <= _fireRange)
            {
                Vector3 direction = DirectionToTarget();

                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    _gun.Shoot(direction);
                }
            }
        }

        protected override void OnSpawn()
        {
            if (_gun != null)
            {
                _gun.ResetRuntimeState();
                _gun.SetEquipped(true);
            }
        }

        public override void OnDeath()
        {
            if (_gun != null)
            {
                _gun.SetEquipped(false);
            }
        }

        private void OnValidate()
        {
            _retreatRange = Mathf.Max(0.1f, _retreatRange);
            _preferredRange = Mathf.Max(_retreatRange, _preferredRange);
            _fireRange = Mathf.Max(_preferredRange, _fireRange);
            _retreatDistance = Mathf.Max(0.1f, _retreatDistance);
        }
    }
}
