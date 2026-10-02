using Application;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class WorldResetManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _spawnPoint;
        [FormerlySerializedAs("_spawner")]
        [SerializeField] private EnemySpawner _spawnerRef;
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private EffectPool _effectPool;
        [SerializeField] private ScoreController _scoreRef;

        private ISpawner _spawner;
        private IGameStateController _gameState;

        public void Initialize(PlayerController player, EnemySpawner spawner, GameStateController gameState, WaveController waves, EffectPool effectPool, ScoreController score, Transform spawnPoint = null)
        {
            _player = player;
            _spawnerRef = spawner;
            _gameStateRef = gameState;
            _waveRef = waves;
            _effectPool = effectPool;
            _scoreRef = score;
            if (spawnPoint != null)
            {
                _spawnPoint = spawnPoint;
            }
            _spawner = spawner;
            _gameState = gameState;
        }

        private void Awake()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            if (_spawnerRef == null)
            {
                _spawnerRef = FindFirstObjectByType<EnemySpawner>();
            }

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            if (_effectPool == null)
            {
                _effectPool = FindFirstObjectByType<EffectPool>();
            }

            if (_scoreRef == null)
            {
                _scoreRef = FindFirstObjectByType<ScoreController>();
            }

            _spawner = _spawnerRef;
            _gameState = _gameStateRef;
        }

        public void Restart()
        {
            if (_gameState == null)
            {
                return;
            }

            GameState state = _gameState.CurrentState;
            if (state != GameState.Menu && state != GameState.GameOver && state != GameState.Victory)
            {
                return;
            }

            // 1. Clear active projectiles
            ClearActiveProjectiles();

            // 2. Clear active enemies and reset attack cooldowns
            if (_spawner != null)
            {
                _spawner.ClearAllAlive();
            }

            // 3. Clear active effects and debris chunks
            if (_effectPool != null)
            {
                _effectPool.ClearAllActive();
            }
            ClearActiveDebris();

            // 4. Clear active pickups (both ammo and weapon pickups)
            ClearActivePickups();

            // 5. Restore camera and screen shake state
            ResetCameraState();

            // 6. Restore timers and wave progress
            if (_waveRef != null)
            {
                _waveRef.ResetWaves();
            }

            // 7. Reset score tracker
            if (_scoreRef != null)
            {
                _scoreRef.ResetScore();
            }

            // 8. Restore player health, ammo, starting weapons, rotation, and input BEFORE entering Playing
            if (_player != null && _spawnPoint != null)
            {
                _player.ResetToSpawn(_spawnPoint.position, _spawnPoint.rotation);
            }
            else if (_player != null)
            {
                _player.ResetToSpawn(_player.transform.position, _player.transform.rotation);
            }

            // 9. Now safely enter Playing
            _gameState.StartGame();
        }

        private void ClearActiveProjectiles()
        {
            Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null)
                {
                    projectiles[i].ForceReturnToPool();
                }
            }
        }

        private void ClearActiveDebris()
        {
            DebrisChunk[] debris = FindObjectsByType<DebrisChunk>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < debris.Length; i++)
            {
                if (debris[i] != null)
                {
                    debris[i].gameObject.SetActive(false);
                }
            }
        }

        private void ClearActivePickups()
        {
            AmmoPickup[] ammoPickups = FindObjectsByType<AmmoPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < ammoPickups.Length; i++)
            {
                if (ammoPickups[i] != null)
                {
                    Destroy(ammoPickups[i].gameObject);
                }
            }

            WeaponPickup[] weaponPickups = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < weaponPickups.Length; i++)
            {
                if (weaponPickups[i] != null)
                {
                    Destroy(weaponPickups[i].gameObject);
                }
            }
        }

        private void ResetCameraState()
        {
            CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.ResetToTarget();
            }

            ScreenShake shake = FindFirstObjectByType<ScreenShake>();
            if (shake != null)
            {
                shake.StopShake();
            }
        }
    }
}
