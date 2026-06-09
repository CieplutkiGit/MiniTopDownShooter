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
        [SerializeField] private GameStateController _gameState;

        private ISpawner _spawner;

        private void Awake()
        {
            _spawner = _spawnerRef;
        }

        public void Restart()
        {
            if (_gameState == null)
            {
                return;
            }

            GameState state = _gameState.CurrentState;
            if (state != GameState.Menu && state != GameState.GameOver)
            {
                return;
            }

            if (_spawner != null)
            {
                _spawner.ClearAllAlive();
            }

            _gameState.StartGame();

            if (_player != null && _spawnPoint != null)
            {
                _player.Respawn(_spawnPoint.position);
            }
        }
    }
}
