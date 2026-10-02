using UnityEngine;

namespace Game
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

        [Header("Telegraph & Line of Sight")]
        [SerializeField] private float _attackTelegraphDuration = 0.3f;
        [SerializeField] private LayerMask _lineOfSightMask = ~0;
        [SerializeField] private LineRenderer _telegraphLine;

        private bool _isTelegraphing;
        private float _telegraphEndTime;
        private Vector3 _telegraphDirection;

        public Gun Gun => _gun;
        public bool IsTelegraphing => _isTelegraphing;

        public override void Tick()
        {
            if (Target == null)
            {
                CancelTelegraph();
                return;
            }

            float distance = DistanceToTarget();
            bool hasLos = HasLineOfSight();

            if (!hasLos)
            {
                CancelTelegraph();
                // Move towards target to regain line of sight
                Movement.MoveToward(Target.position);
                return;
            }

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

            if (_gun != null && distance <= _fireRange && hasLos)
            {
                Vector3 direction = DirectionToTarget();

                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    if (!_isTelegraphing)
                    {
                        _isTelegraphing = true;
                        _telegraphEndTime = Time.time + _attackTelegraphDuration;
                        _telegraphDirection = direction;
                        UpdateTelegraphLine(true, direction);
                    }
                    else
                    {
                        _telegraphDirection = direction;
                        UpdateTelegraphLine(true, direction);

                        if (Time.time >= _telegraphEndTime)
                        {
                            _gun.Shoot(_telegraphDirection);
                            CancelTelegraph();
                        }
                    }
                }
            }
            else
            {
                CancelTelegraph();
            }
        }

        private bool HasLineOfSight()
        {
            if (Target == null)
            {
                return false;
            }

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 targetPos = Target.position + Vector3.up * 0.8f;
            Vector3 delta = targetPos - origin;
            float dist = delta.magnitude;

            if (dist <= Mathf.Epsilon)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, dist, _lineOfSightMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider != null && hits[i].transform.root == transform.root)
                    {
                        continue;
                    }

                    return hits[i].transform.root == Target.root;
                }
            }

            return true;
        }

        private void CancelTelegraph()
        {
            _isTelegraphing = false;
            UpdateTelegraphLine(false, Vector3.zero);
        }

        private void UpdateTelegraphLine(bool active, Vector3 direction)
        {
            if (_telegraphLine == null)
            {
                return;
            }

            _telegraphLine.enabled = active;
            if (active)
            {
                Vector3 start = transform.position + Vector3.up * 0.8f;
                _telegraphLine.SetPosition(0, start);
                _telegraphLine.SetPosition(1, start + direction * _fireRange);
            }
        }

        protected override void OnSpawn()
        {
            CancelTelegraph();
            if (_gun != null)
            {
                _gun.ResetRuntimeState();
                _gun.SetEquipped(true);
            }
        }

        public override void OnDeath()
        {
            CancelTelegraph();
            if (_gun != null)
            {
                _gun.SetEquipped(false);
            }
        }

        public override void OnDespawn()
        {
            CancelTelegraph();
            base.OnDespawn();
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
