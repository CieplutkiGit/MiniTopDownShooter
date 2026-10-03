using Application;
using Application.Workshop;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Mobile-aware input re-arm guard.
    /// Clears all held inputs when transitioning between scenes, entering/exiting
    /// modals, or when the app loses focus. Requires explicit resume on foreground.
    /// Works with the existing MobileInputState and InputReader.
    /// </summary>
    public class InputRearmController : MonoBehaviour
    {
        [SerializeField] private MobileInputState _mobileInput;
        [SerializeField] private PlayerController _player;
        [SerializeField] private GameStateController _gameStateController;

        private bool _wasBackgrounded;
        private bool _sceneFlowSubscribed;

        private void Awake()
        {
            ResolveSceneReferences();
        }

        private void OnEnable()
        {
            ResolveSceneReferences();
            if (_gameStateController != null)
                _gameStateController.OnStateChanged += HandleStateChanged;

            SubscribeSceneFlow();
        }

        private void Start()
        {
            ResolveSceneReferences();
            SubscribeSceneFlow();
        }

        private void OnDisable()
        {
            ClearAllInputs();
            if (_gameStateController != null)
                _gameStateController.OnStateChanged -= HandleStateChanged;

            UnsubscribeSceneFlow();
        }

        private void ResolveSceneReferences()
        {
            if (_mobileInput == null)
                _mobileInput = SceneComponents.Find<MobileInputState>(gameObject.scene);
            if (_player == null)
                _player = SceneComponents.Find<PlayerController>(gameObject.scene);
            if (_gameStateController == null)
                _gameStateController = SceneComponents.Find<GameStateController>(gameObject.scene);
        }

        private void SubscribeSceneFlow()
        {
            if (_sceneFlowSubscribed) return;
            SceneFlowController sceneFlow = AppCompositionRoot.Instance?.SceneFlow;
            if (sceneFlow != null)
            {
                sceneFlow.OnTransitionStarted += HandleTransitionStarted;
                sceneFlow.OnTransitionCompleted += HandleTransitionCompleted;
                sceneFlow.OnTransitionFailed += HandleTransitionFailed;
                _sceneFlowSubscribed = true;
            }
        }

        private void UnsubscribeSceneFlow()
        {
            if (!_sceneFlowSubscribed) return;
            SceneFlowController sceneFlow = AppCompositionRoot.Instance?.SceneFlow;
            if (sceneFlow != null)
            {
                sceneFlow.OnTransitionStarted -= HandleTransitionStarted;
                sceneFlow.OnTransitionCompleted -= HandleTransitionCompleted;
                sceneFlow.OnTransitionFailed -= HandleTransitionFailed;
            }
            _sceneFlowSubscribed = false;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _wasBackgrounded = true;
                ClearAllInputs();

                if (_gameStateController != null && !GameActivityPolicy.IsTimeFrozen(_gameStateController.CurrentState))
                {
                    _gameStateController.Pause();
                }
            }
            else if (_wasBackgrounded)
            {
                _wasBackgrounded = false;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                _wasBackgrounded = true;
                ClearAllInputs();

                if (_gameStateController != null && !GameActivityPolicy.IsTimeFrozen(_gameStateController.CurrentState))
                {
                    _gameStateController.Pause();
                }
            }
            else if (_wasBackgrounded)
            {
                _wasBackgrounded = false;
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            ClearAllInputs();
        }

        private void HandleTransitionStarted()
        {
            ClearAllInputs();
        }

        private void HandleTransitionCompleted()
        {
            ResolveSceneReferences();
            ClearAllInputs();
        }

        private void HandleTransitionFailed(string error)
        {
            ClearAllInputs();
        }

        public void ClearAllInputs()
        {
            if (_mobileInput == null)
                _mobileInput = SceneComponents.Find<MobileInputState>(gameObject.scene);
            _mobileInput?.ResetAll();

            if (_player == null)
                _player = SceneComponents.Find<PlayerController>(gameObject.scene);
            if (_player != null && _player.Input != null)
            {
                _player.Input.ResetGameplayTransientState();
                _player.Input.RequireNeutralToRearm();
            }
        }
    }
}
