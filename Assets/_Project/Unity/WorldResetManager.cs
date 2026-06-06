using Application;
using UnityEngine;

namespace Game
{
    public class WorldResetManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private GameStateController _gameState;

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
