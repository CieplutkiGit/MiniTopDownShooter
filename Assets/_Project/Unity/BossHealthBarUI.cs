using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class BossHealthBarUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _bossNameLabel;
        [SerializeField] private TMP_Text _phaseLabel;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _healthFillImage;

        private BossPhaseController _activeBoss;
        private HealthComponent _activeHealth;

        private void Awake()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (_activeBoss == null)
            {
                FindActiveBoss();
            }
        }

        private void FindActiveBoss()
        {
            BossPhaseController boss = FindFirstObjectByType<BossPhaseController>();
            if (boss != null && boss.gameObject.activeInHierarchy)
            {
                BindBoss(boss);
            }
        }

        public void BindBoss(BossPhaseController boss)
        {
            if (_activeBoss == boss)
            {
                return;
            }

            UnbindCurrentBoss();

            if (boss == null)
            {
                return;
            }

            _activeBoss = boss;
            _activeHealth = boss.GetComponent<HealthComponent>();

            if (_activeBoss != null)
            {
                _activeBoss.PhaseChanged += HandlePhaseChanged;
            }

            if (_activeHealth != null)
            {
                _activeHealth.OnHealthChanged += HandleHealthChanged;
                _activeHealth.OnDead += HandleBossDead;
                UpdateHealthDisplay(_activeHealth.CurrentHealth, _activeHealth.MaxHealth);
            }

            if (_bossNameLabel != null)
            {
                _bossNameLabel.text = _activeBoss.BossName;
            }

            UpdatePhaseDisplay(_activeBoss.ActivePhaseIndex);

            if (_panel != null)
            {
                _panel.SetActive(true);
            }
        }

        private void UnbindCurrentBoss()
        {
            if (_activeBoss != null)
            {
                _activeBoss.PhaseChanged -= HandlePhaseChanged;
            }

            if (_activeHealth != null)
            {
                _activeHealth.OnHealthChanged -= HandleHealthChanged;
                _activeHealth.OnDead -= HandleBossDead;
            }

            _activeBoss = null;
            _activeHealth = null;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            UpdateHealthDisplay(current, max);
        }

        private void HandlePhaseChanged(int phaseIndex)
        {
            UpdatePhaseDisplay(phaseIndex);
        }

        private void HandleBossDead()
        {
            UnbindCurrentBoss();
        }

        private void UpdateHealthDisplay(int current, int max)
        {
            if (max <= 0)
            {
                return;
            }

            float fraction = Mathf.Clamp01((float)current / max);

            if (_healthSlider != null)
            {
                _healthSlider.value = fraction;
            }

            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = fraction;
            }
        }

        private void UpdatePhaseDisplay(int phaseIndex)
        {
            if (_phaseLabel == null || _activeBoss == null)
            {
                return;
            }

            BossPhase current = _activeBoss.CurrentPhase;
            if (current != null && !string.IsNullOrEmpty(current.PhaseName))
            {
                _phaseLabel.text = current.PhaseName;
            }
            else
            {
                _phaseLabel.text = phaseIndex >= 0 ? $"Phase {phaseIndex + 1}" : "";
            }
        }

        private void OnDisable()
        {
            UnbindCurrentBoss();
        }
    }
}
