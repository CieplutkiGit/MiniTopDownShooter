using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class ChargerEnemyBehavior : EnemyBehaviorBase
    {
        private enum ChargeState
        {
            Approaching,
            Windup,
            Charging,
            Recovery
        }

        [Header("Trigger")]
        [Min(0.1f)]
        [SerializeField] private float _chargeTriggerRange = 7f;

        [Min(0f)]
        [SerializeField] private float _minimumChargeRange = 2.5f;

        [Header("Timing")]
        [Min(0f)]
        [SerializeField] private float _windupDuration = 0.45f;

        [Min(0.05f)]
        [SerializeField] private float _chargeDuration = 0.8f;

        [Min(0f)]
        [SerializeField] private float _recoveryDuration = 1f;

        [Header("Movement")]
        [Min(1f)]
        [SerializeField] private float _chargeSpeedMultiplier = 3f;

        [Min(0.1f)]
        [SerializeField] private float _overshootDistance = 3f;

        [Header("Impact")]
        [Min(0.1f)]
        [SerializeField] private float _impactRange = 1.5f;

        private ChargeState _state;
        private float _stateEndTime;
        private Vector3 _chargeDestination;
        private bool _dealtChargeDamage;

        public override void Tick()
        {
            if (Target == null || TargetDamageable == null)
            {
                return;
            }

            switch (_state)
            {
                case ChargeState.Windup:
                    TickWindup();
                    break;

                case ChargeState.Charging:
                    TickCharging();
                    break;

                case ChargeState.Recovery:
                    TickRecovery();
                    break;

                default:
                    TickApproach();
                    break;
            }
        }

        protected override void OnSpawn()
        {
            ResetCharge();
        }

        public override void OnDeath()
        {
            Movement.SetSpeedMultiplier(1f);
        }

        public override void OnDespawn()
        {
            Movement.SetSpeedMultiplier(1f);
        }

        private void TickApproach()
        {
            float distance = DistanceToTarget();

            if (distance <= _chargeTriggerRange && distance >= _minimumChargeRange)
            {
                _state = ChargeState.Windup;
                _stateEndTime = Time.time + _windupDuration;
                Movement.Stop();
                return;
            }

            if (Attack.IsInRange(distance))
            {
                Movement.Stop();
                Attack.TryAttack(TargetDamageable);
                return;
            }

            Movement.MoveToward(Target.position);
        }

        private void TickWindup()
        {
            Movement.Stop();
            if (Time.time < _stateEndTime)
            {
                return;
            }

            Vector3 direction = DirectionToTarget();

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                _state = ChargeState.Recovery;
                _stateEndTime = Time.time + _recoveryDuration;
                return;
            }

            _chargeDestination = Target.position + direction * _overshootDistance;
            _dealtChargeDamage = false;
            Movement.SetSpeedMultiplier(_chargeSpeedMultiplier);
            Movement.MoveToward(_chargeDestination);
            _state = ChargeState.Charging;
            _stateEndTime = Time.time + _chargeDuration;
        }

        private void TickCharging()
        {
            if (!_dealtChargeDamage && DistanceToTarget() <= _impactRange)
            {
                _dealtChargeDamage = true;
                Attack.TryAttack(TargetDamageable);
            }

            Movement.MoveToward(_chargeDestination);

            if (Time.time < _stateEndTime &&
                Vector3.Distance(transform.position, _chargeDestination) > 0.35f)
            {
                return;
            }

            Movement.SetSpeedMultiplier(1f);
            Movement.Stop();
            _state = ChargeState.Recovery;
            _stateEndTime = Time.time + _recoveryDuration;
        }

        private void TickRecovery()
        {
            Movement.Stop();
            if (Time.time >= _stateEndTime)
            {
                _state = ChargeState.Approaching;
            }
        }

        private void ResetCharge()
        {
            _state = ChargeState.Approaching;
            _stateEndTime = 0f;
            _dealtChargeDamage = false;
            Movement.SetSpeedMultiplier(1f);
        }

        private void OnValidate()
        {
            _chargeTriggerRange = Mathf.Max(0.1f, _chargeTriggerRange);
            _minimumChargeRange = Mathf.Clamp(_minimumChargeRange, 0f, _chargeTriggerRange);
            _windupDuration = Mathf.Max(0f, _windupDuration);
            _chargeDuration = Mathf.Max(0.05f, _chargeDuration);
            _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
            _chargeSpeedMultiplier = Mathf.Max(1f, _chargeSpeedMultiplier);
            _overshootDistance = Mathf.Max(0.1f, _overshootDistance);
            _impactRange = Mathf.Max(0.1f, _impactRange);
        }
    }
}
