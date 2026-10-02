using Application;
using UnityEngine;

namespace Game
{
    public class GameStateAudio : AudioListenerBase
    {
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private AudioClip _gameOverClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _combatMusicClip;
        [SerializeField] private AudioSource _musicSource;

        private IGameStateProvider _gameState;
        private bool _isSubscribed;

        public void Initialize(GameStateController gameState)
        {
            UnsubscribeEvents();
            _gameStateRef = gameState;
            _gameState = gameState;
            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            _gameState = _gameStateRef;

            if (_musicSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != _source)
                    {
                        _musicSource = sources[i];
                        break;
                    }
                }

                if (_musicSource == null)
                {
                    GameObject musicObj = new GameObject("MusicSource");
                    musicObj.transform.SetParent(transform, false);
                    _musicSource = musicObj.AddComponent<AudioSource>();
                }
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gameState == null)
            {
                return;
            }

            _gameState.OnStateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Playing)
            {
                if (oldState == GameState.Paused && _musicSource != null && _musicSource.clip == _combatMusicClip)
                {
                    _musicSource.UnPause();
                }
                else if (_combatMusicClip != null && _musicSource != null)
                {
                    _musicSource.clip = _combatMusicClip;
                    _musicSource.loop = true;
                    _musicSource.Play();
                }
            }
            else if (newState == GameState.Paused)
            {
                if (_musicSource != null && _musicSource.isPlaying)
                {
                    _musicSource.Pause();
                }
            }
            else if (newState == GameState.GameOver)
            {
                if (_musicSource != null)
                {
                    _musicSource.Stop();
                }
                PlayClip(_gameOverClip);
            }
            else if (newState == GameState.Victory)
            {
                if (_musicSource != null)
                {
                    _musicSource.Stop();
                }
                PlayClip(_victoryClip);
            }
            else if (newState == GameState.Menu)
            {
                if (_musicSource != null)
                {
                    _musicSource.Stop();
                }
            }
        }
    }
}
