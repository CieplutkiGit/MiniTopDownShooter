using Application;
using UnityEngine;

namespace Game.Workshop
{
    public class WorkshopFiringRangeTrigger : MonoBehaviour
    {
        [SerializeField] private GameStateController _gameStateRef;

        private IGameStateController _gameState;
        private bool _isPlayerInside;

        public bool IsPlayerInside => _isPlayerInside;

        public void Initialize(GameStateController gameState)
        {
            _gameStateRef = gameState;
            _gameState = gameState;
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }
            _gameState = _gameStateRef;
        }

        public void EnterRange()
        {
            _isPlayerInside = true;
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopRoaming)
            {
                _gameState.EnterWorkshopFiringRange();
            }
        }

        public void ExitRange()
        {
            _isPlayerInside = false;
            if (_gameState != null && _gameState.CurrentState == GameState.WorkshopFiringRange)
            {
                _gameState.EnterWorkshopRoaming();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                EnterRange();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                ExitRange();
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.GetComponent<PlayerController>() != null ||
                   other.GetComponentInParent<PlayerController>() != null ||
                   other.CompareTag("Player");
        }
    }
}
