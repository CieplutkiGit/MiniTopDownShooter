using UnityEngine;

namespace Game
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

        [Header("Telegraph")]
        [SerializeField] private LineRenderer _telegraphLine;
        [SerializeField] private ParticleSystem _windupEffect;

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
            UpdateTelegraphLine(false);
            Movement.SetSpeedMultiplier(1f);
        }

        public override void OnDespawn()
        {
            UpdateTelegraphLine(false);
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
                UpdateTelegraphLine(true);
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
            UpdateTelegraphLine(true);

            if (Time.time < _stateEndTime)
            {
                return;
            }

            UpdateTelegraphLine(false);

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
            if (!_dealtChargeDamage)
            {
                if (DistanceToTarget() <= _impactRange)
                {
                    _dealtChargeDamage = true;
                    Attack.TryAttack(TargetDamageable);
                }
                else
                {
                    Collider[] colliders = Physics.OverlapSphere(
                        transform.position,
                        _impactRange,
                        ~0,
                        QueryTriggerInteraction.Ignore);

                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (colliders[i] != null && Target != null && colliders[i].transform.root == Target.root)
                        {
                            _dealtChargeDamage = true;
                            Attack.TryAttack(TargetDamageable);
                            break;
                        }
                    }
                }
            }

            Movement.MoveToward(_chargeDestination);

            if (Time.time < _stateEndTime &&
                Vector3.Distance(transform.position, _chargeDestination) > 0.35f)
            {
                return;
            }

            Movement.SetSpeedMultiplier(0f);
            Movement.Stop();
            _state = ChargeState.Recovery;
            _stateEndTime = Time.time + _recoveryDuration;
        }

        private void TickRecovery()
        {
            UpdateTelegraphLine(false);
            Movement.SetSpeedMultiplier(0f);
            Movement.Stop();
            if (Time.time >= _stateEndTime)
            {
                Movement.SetSpeedMultiplier(1f);
                _state = ChargeState.Approaching;
            }
        }

        private void ResetCharge()
        {
            _state = ChargeState.Approaching;
            _stateEndTime = 0f;
            _dealtChargeDamage = false;
            Movement.SetSpeedMultiplier(1f);
            UpdateTelegraphLine(false);
        }

        private void UpdateTelegraphLine(bool active)
        {
            if (_telegraphLine != null)
            {
                _telegraphLine.enabled = active;
                if (active && Target != null)
                {
                    Vector3 origin = transform.position;
                    origin.y = 0.05f;
                    Vector3 targetPos = Target.position;
                    targetPos.y = 0.05f;
                    _telegraphLine.SetPosition(0, origin);
                    _telegraphLine.SetPosition(1, targetPos);
                }
            }

            if (_windupEffect != null)
            {
                if (active && !_windupEffect.isPlaying)
                {
                    _windupEffect.Play();
                }
                else if (!active && _windupEffect.isPlaying)
                {
                    _windupEffect.Stop();
                }
            }
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
