using Cieplutki.MiniTopDownShooter.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private HealthComponent _healthRef;
        [SerializeField] private Slider _slider;

        private IHealthReadable _health;

        private void Awake()
        {
            _health = _healthRef;
        }

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged += HandleHealthChanged;
            UpdateSlider(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            UpdateSlider(current, max);
        }

        private void UpdateSlider(int current, int max)
        {
            if (_slider == null)
            {
                return;
            }

            if (max <= 0)
            {
                _slider.value = 0f;
                return;
            }

            float ratio = (float)current / (float)max;
            _slider.value = ratio;
        }
    }
}
