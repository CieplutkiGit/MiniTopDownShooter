using Application;
using UnityEngine;

namespace Game
{
    public class EnemyAudio : AudioListenerBase
    {
        [SerializeField] private EnemySpawner _spawnerRef;
        [SerializeField] private AudioClip _deathClip;

        private ISpawner _spawner;

        private void Awake()
        {
            _spawner = _spawnerRef;
        }

        private void OnEnable()
        {
            if (_spawner == null)
            {
                return;
            }

            _spawner.EnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            if (_spawner == null)
            {
                return;
            }

            _spawner.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled()
        {
            PlayClip(_deathClip);
        }
    }
}
