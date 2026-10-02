using Core;
using UnityEngine;

namespace Game
{
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private HealthComponent _playerHealthRef;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private float _magnitude = 0.3f;

        private IHealthReadable _playerHealth;
        private float _timer;
        private Vector3 _currentOffset;
        private int _lastHealth;
        private bool _isSubscribed;

        public Vector3 CurrentOffset
        {
            get { return _currentOffset; }
        }

        public void Initialize(HealthComponent playerHealth)
        {
            UnsubscribeEvents();
            _playerHealthRef = playerHealth;
            _playerHealth = playerHealth;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_playerHealthRef == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    _playerHealthRef = pc.GetComponent<HealthComponent>();
                }
            }

            _playerHealth = _playerHealthRef;
            _currentOffset = Vector3.zero;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        public void StopShake()
        {
            _timer = 0f;
            _currentOffset = Vector3.zero;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopShake();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _playerHealth == null)
            {
                return;
            }

            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _lastHealth = _playerHealth.Current;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _playerHealth == null)
            {
                return;
            }

            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _isSubscribed = false;
        }

        private void Update()
        {
            if (_timer <= 0f)
            {
                _currentOffset = Vector3.zero;
                return;
            }

            _timer -= Time.deltaTime;

            float x = Random.Range(-1f, 1f);
            float z = Random.Range(-1f, 1f);
            _currentOffset = new Vector3(x, 0f, z) * _magnitude;

            if (_timer <= 0f)
            {
                _currentOffset = Vector3.zero;
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (current < _lastHealth)
            {
                _timer = _duration;
            }

            _lastHealth = current;
        }
    }
}
