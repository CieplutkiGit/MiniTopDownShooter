using System;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    [Serializable]
    public class BossPhase
    {
        [Range(0f, 1f)]
        [Tooltip("Phase becomes active when current health fraction is at or below this value.")]
        public float EnterAtHealthFraction = 0.5f;

        [Min(0f)]
        public float MovementSpeedMultiplier = 1f;

        public GameObject[] EnableObjects;
        public GameObject[] DisableObjects;
    }

    public class BossPhaseController : MonoBehaviour
    {
        [SerializeField] private HealthComponent _health;
        [SerializeField] private EnemyMovement _movement;
        [SerializeField] private BossPhase[] _phases;

        private int _activePhaseIndex = -1;

        public event Action<int> PhaseChanged;

        public int ActivePhaseIndex => _activePhaseIndex;
        public int PhaseCount => _phases != null ? _phases.Length : 0;

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
            SetObjectsActive(phase.EnableObjects, true);
            SetObjectsActive(phase.DisableObjects, false);
        }

        private void ResetPhasePresentation()
        {
            if (_movement != null)
            {
                _movement.SetSpeedMultiplier(1f);
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
