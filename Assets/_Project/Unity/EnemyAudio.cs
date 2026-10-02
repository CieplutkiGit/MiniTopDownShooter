using Application;
using UnityEngine;

namespace Game
{
    public class EnemyAudio : AudioListenerBase
    {
        [SerializeField] private EnemySpawner _spawnerRef;
        [SerializeField] private AudioClip _deathClip;

        private ISpawner _spawner;
        private bool _isSubscribed;

        public void Initialize(EnemySpawner spawner)
        {
            UnsubscribeEvents();
            _spawnerRef = spawner;
            _spawner = spawner;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_spawnerRef == null)
            {
                _spawnerRef = FindFirstObjectByType<EnemySpawner>();
            }

            _spawner = _spawnerRef;
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
            if (_isSubscribed || _spawner == null)
            {
                return;
            }

            _spawner.EnemyKilled += HandleEnemyKilled;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _spawner == null)
            {
                return;
            }

            _spawner.EnemyKilled -= HandleEnemyKilled;
            _isSubscribed = false;
        }

        private void HandleEnemyKilled(int scoreValue)
        {
            PlayClip(_deathClip);
        }
    }
}
