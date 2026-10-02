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
        public float ShockwaveWindup = 0.8f;
        public float ShockwaveRecovery = 0.4f;
        public GameObject ShockwaveTelegraph;

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
        private Coroutine _shockwaveCoroutine;
        private GameObject _proceduralTelegraph;

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
                if (_shockwaveCoroutine != null)
                {
                    StopCoroutine(_shockwaveCoroutine);
                }
                _shockwaveCoroutine = StartCoroutine(ShockwaveRoutine(phase));
            }
        }

        private System.Collections.IEnumerator ShockwaveRoutine(BossPhase phase)
        {
            if (_movement != null)
            {
                _movement.SetSpeedMultiplier(0f);
            }

            GameObject telegraph = phase.ShockwaveTelegraph;
            if (telegraph != null)
            {
                telegraph.SetActive(true);
            }
            else
            {
                telegraph = EnsureProceduralTelegraph(phase.ShockwaveRadius);
                if (telegraph != null)
                {
                    telegraph.SetActive(true);
                }
            }

            float windup = Mathf.Max(0.1f, phase.ShockwaveWindup);
            yield return new WaitForSeconds(windup);

            ExecuteShockwave(phase.ShockwaveRadius, phase.ShockwaveDamage);

            if (_transitionEffect != null)
            {
                _transitionEffect.Play();
            }

            if (telegraph != null)
            {
                telegraph.SetActive(false);
            }

            float recovery = Mathf.Max(0f, phase.ShockwaveRecovery);
            if (recovery > 0f)
            {
                yield return new WaitForSeconds(recovery);
            }

            if (_movement != null && _activePhaseIndex >= 0 && _phases != null && _activePhaseIndex < _phases.Length)
            {
                _movement.SetSpeedMultiplier(Mathf.Max(0f, _phases[_activePhaseIndex].MovementSpeedMultiplier));
            }

            _shockwaveCoroutine = null;
        }

        private GameObject EnsureProceduralTelegraph(float radius)
        {
            if (_proceduralTelegraph == null)
            {
                _proceduralTelegraph = new GameObject("ShockwaveTelegraph");
                _proceduralTelegraph.transform.SetParent(transform, false);
                _proceduralTelegraph.transform.localPosition = new Vector3(0f, 0.05f, 0f);

                LineRenderer line = _proceduralTelegraph.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.startWidth = 0.15f;
                line.endWidth = 0.15f;
                line.positionCount = 36;

                for (int i = 0; i < 36; i++)
                {
                    float angle = (i * 360f / 36f) * Mathf.Deg2Rad;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
                line.endColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            }
            else
            {
                LineRenderer line = _proceduralTelegraph.GetComponent<LineRenderer>();
                if (line != null)
                {
                    for (int i = 0; i < 36; i++)
                    {
                        float angle = (i * 360f / 36f) * Mathf.Deg2Rad;
                        line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                    }
                }
            }

            return _proceduralTelegraph;
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
            if (_shockwaveCoroutine != null)
            {
                StopCoroutine(_shockwaveCoroutine);
                _shockwaveCoroutine = null;
            }

            if (_proceduralTelegraph != null)
            {
                _proceduralTelegraph.SetActive(false);
            }

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

                if (phase.ShockwaveTelegraph != null)
                {
                    phase.ShockwaveTelegraph.SetActive(false);
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
