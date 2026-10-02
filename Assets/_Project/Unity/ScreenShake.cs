using Cieplutki.MiniTopDownShooter.Core;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
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

        public Vector3 CurrentOffset
        {
            get { return _currentOffset; }
        }

        private void Awake()
        {
            _playerHealth = _playerHealthRef;
            _currentOffset = Vector3.zero;
        }

        private void OnEnable()
        {
            if (_playerHealth == null)
            {
                return;
            }

            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _lastHealth = _playerHealth.Current;
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
            }

            _timer = 0f;
            _currentOffset = Vector3.zero;
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
