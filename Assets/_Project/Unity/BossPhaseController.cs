using System;
using Core;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class BossPhase
    {
        public string PhaseName = "Phase 1";

        [Range(0f, 1f)]
        [Tooltip("Phase becomes active when current health fraction is at or below this value.")]
        public float EnterAtHealthFraction = 0.5f;

        [Min(0f)]
        public float MovementSpeedMultiplier = 1f;

        [Min(0.1f)]
        public float AttackCooldownMultiplier = 1f;

        public int BonusDamage = 0;

        [Header("Distinct Attack")]
        public bool TriggerShockwaveOnEnter = true;
        public float ShockwaveRadius = 6f;
        public int ShockwaveDamage = 15;

        public GameObject[] EnableObjects;
        public GameObject[] DisableObjects;
    }

    public class BossPhaseController : MonoBehaviour
    {
        [SerializeField] private string _bossName = "Boss";
        [SerializeField] private HealthComponent _health;
        [SerializeField] private EnemyMovement _movement;
        [SerializeField] private EnemyAttack _attack;
        [SerializeField] private ParticleSystem _transitionEffect;
        [SerializeField] private BossPhase[] _phases;

        private int _activePhaseIndex = -1;
        private int _baseDamage = -1;
        private float _baseCooldown = -1f;

        public event Action<int> PhaseChanged;

        public string BossName => _bossName;
        public int ActivePhaseIndex => _activePhaseIndex;
        public int PhaseCount => _phases != null ? _phases.Length : 0;
        public BossPhase CurrentPhase => _activePhaseIndex >= 0 && _phases != null && _activePhaseIndex < _phases.Length ? _phases[_activePhaseIndex] : null;

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }

            if (_movement == null)
            {
                _movement = GetComponent<EnemyMovement>();
            }

            if (_attack == null)
            {
                _attack = GetComponent<EnemyAttack>();
            }

            if (_attack != null)
            {
                _baseDamage = _attack.Damage;
                _baseCooldown = _attack.AttackCooldown;
            }
        }

        private void OnEnable()
        {
            _activePhaseIndex = int.MinValue;

            if (_health != null)
            {
                _health.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
            }

            ResetPhasePresentation();
            _activePhaseIndex = int.MinValue;
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (_phases == null || _phases.Length == 0 || max <= 0)
            {
                return;
            }

            float fraction = Mathf.Clamp01((float)current / max);
            int resolvedIndex = -1;

            for (int i = 0; i < _phases.Length; i++)
            {
                BossPhase phase = _phases[i];

                if (phase != null && fraction <= phase.EnterAtHealthFraction)
                {
                    resolvedIndex = i;
                }
            }

            if (resolvedIndex == _activePhaseIndex)
            {
                return;
            }

            _activePhaseIndex = resolvedIndex;
            ApplyPhase(resolvedIndex);
            PhaseChanged?.Invoke(resolvedIndex);
        }

        private void ApplyPhase(int phaseIndex)
        {
            if (_movement != null)
            {
                float multiplier = phaseIndex >= 0
                    ? Mathf.Max(0f, _phases[phaseIndex].MovementSpeedMultiplier)
                    : 1f;

                _movement.SetSpeedMultiplier(multiplier);
            }

            if (phaseIndex < 0)
            {
                ResetPhasePresentation();
                return;
            }

            BossPhase phase = _phases[phaseIndex];

            if (_attack != null)
            {
                if (_baseDamage < 0)
                {
                    _baseDamage = _attack.Damage;
                    _baseCooldown = _attack.AttackCooldown;
                }

                _attack.SetDamage(_baseDamage + phase.BonusDamage);
                _attack.SetAttackCooldown(_baseCooldown * Mathf.Max(0.1f, phase.AttackCooldownMultiplier));
            }

            SetObjectsActive(phase.EnableObjects, true);
            SetObjectsActive(phase.DisableObjects, false);

            if (_transitionEffect != null)
            {
                _transitionEffect.Play();
            }

            if (phase.TriggerShockwaveOnEnter && phase.ShockwaveRadius > 0f)
            {
                ExecuteShockwave(phase.ShockwaveRadius, phase.ShockwaveDamage);
            }
        }

        private void ExecuteShockwave(float radius, int damage)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore);

            DamageAffiliation bossAffiliation = DamageAffiliation.Find(this);
            CombatTeam bossTeam = bossAffiliation != null ? bossAffiliation.Team : CombatTeam.Enemy;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || col.transform.root == transform.root)
                {
                    continue;
                }

                DamageAffiliation targetAff = DamageAffiliation.Find(col);
                CombatTeam targetTeam = targetAff != null ? targetAff.Team : DamageAffiliation.ResolveTeam(col);

                if (DamageAffiliation.CanDamage(bossAffiliation, bossTeam, targetAff, targetTeam))
                {
                    if (DamageAffiliation.TryGetDamageable(col, out IDamageable damageable, out _))
                    {
                        damageable.TakeDamage(new DamageData(damage));
                    }
                }
            }
        }

        private void ResetPhasePresentation()
        {
            if (_movement != null)
            {
                _movement.SetSpeedMultiplier(1f);
            }

            if (_attack != null && _baseDamage >= 0)
            {
                _attack.SetDamage(_baseDamage);
                _attack.SetAttackCooldown(_baseCooldown);
            }

            if (_phases == null)
            {
                return;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                BossPhase phase = _phases[i];

                if (phase == null)
                {
                    continue;
                }

                SetObjectsActive(phase.EnableObjects, false);
                SetObjectsActive(phase.DisableObjects, true);
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }

        private void OnValidate()
        {
            if (_phases == null)
            {
                return;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i] == null)
                {
                    continue;
                }

                _phases[i].EnterAtHealthFraction =
                    Mathf.Clamp01(_phases[i].EnterAtHealthFraction);

                _phases[i].MovementSpeedMultiplier =
                    Mathf.Max(0f, _phases[i].MovementSpeedMultiplier);
            }
        }
    }
}
