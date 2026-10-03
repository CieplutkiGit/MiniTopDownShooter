using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private HealthComponent _healthRef;
        [SerializeField] private Slider _slider;

        private IHealthReadable _health;
        private bool _isSubscribed;

        public void Initialize(HealthComponent health)
        {
            UnsubscribeEvents();
            _healthRef = health;
            _health = health;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_healthRef == null)
            {
                _healthRef = GetComponentInParent<HealthComponent>();
                if (_healthRef == null)
                {
                    PlayerController pc = FindFirstObjectByType<PlayerController>();
                    if (pc != null)
                    {
                        _healthRef = pc.GetComponent<HealthComponent>();
                    }
                }
            }

            if (_slider != null)
            {
                _slider.interactable = false;
                _slider.transition = Selectable.Transition.None;
                foreach (var img in _slider.GetComponentsInChildren<Image>(true))
                {
                    img.raycastTarget = false;
                }
            }

            _health = _healthRef;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _health == null)
            {
                return;
            }

            _health.OnHealthChanged += HandleHealthChanged;
            _isSubscribed = true;
            UpdateSlider(_health.Current, _health.Max);
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _health == null)
            {
                return;
            }

            _health.OnHealthChanged -= HandleHealthChanged;
            _isSubscribed = false;
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
