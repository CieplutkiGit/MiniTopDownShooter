using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class RusherEnemyBehavior : EnemyBehaviorBase
    {
        [Min(1f)]
        [SerializeField] private float _rushSpeedMultiplier = 1.75f;

        [Min(0f)]
        [SerializeField] private float _rushUntilDistance = 3f;

        public override void Tick()
        {
            if (Target == null || TargetDamageable == null)
            {
                return;
            }

            float distance = DistanceToTarget();

            if (Attack.IsInRange(distance))
            {
                Movement.SetSpeedMultiplier(1f);
                Movement.Stop();
                Attack.TryAttack(TargetDamageable);
                return;
            }

            Movement.SetSpeedMultiplier(
                distance > _rushUntilDistance
                    ? _rushSpeedMultiplier
                    : 1f);

            Movement.MoveToward(Target.position);
        }

        protected override void OnSpawn()
        {
            Movement.SetSpeedMultiplier(1f);
        }

        public override void OnDeath()
        {
            Movement.SetSpeedMultiplier(1f);
        }

        public override void OnDespawn()
        {
            Movement.SetSpeedMultiplier(1f);
        }

        private void OnValidate()
        {
            _rushSpeedMultiplier = Mathf.Max(1f, _rushSpeedMultiplier);
            _rushUntilDistance = Mathf.Max(0f, _rushUntilDistance);
        }
    }
}
