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
            if (_spawner != null)
            {
                _spawner.ClearAllAlive();
            }

            if (_player != null && _spawnPoint != null)
            {
                _player.Respawn(_spawnPoint.position);
            }

            if (_gameState != null)
            {
                _gameState.StartGame();
            }
        }
    }
}
