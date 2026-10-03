using System;
using Application;
using UnityEngine;

namespace Game
{
    public class GunAudio : AudioListenerBase
    {
        [SerializeField] private Gun _gunRef;
        [SerializeField] private AudioClip _shotClip;

        private IGunEvents _gun;
        private bool _isSubscribed;

        public event Action<AudioClip> PlaybackRequested;
        public int PlaybackCount { get; private set; }

        public Gun GunRef => _gunRef;

        public AudioClip ShotClip
        {
            get => _shotClip;
            set => _shotClip = value;
        }

        public void ResetPlaybackCount()
        {
            PlaybackCount = 0;
        }

        public void Initialize(Gun gun)
        {
            UnsubscribeEvents();
            _gunRef = gun;
            _gun = gun;

            if (_source == null)
            {
                _source = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();
            }

            ValidateReferences();

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (_gunRef == null)
            {
                _gunRef = GetComponent<Gun>() ?? GetComponentInParent<Gun>();
            }

            _gun = _gunRef;

            if (_source == null)
            {
                _source = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnValidate()
        {
            if (_gunRef == null)
            {
                _gunRef = GetComponent<Gun>() ?? GetComponentInParent<Gun>();
            }

            if (_source == null)
            {
                _source = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();
            }

            ValidateReferences();
        }

        public bool ValidateReferences()
        {
            bool isValid = true;

            if (_gunRef == null && GetComponent<Gun>() == null && GetComponentInParent<Gun>() == null)
            {
                Debug.LogWarning($"[GunAudio] Missing Gun reference on '{gameObject.name}'.", this);
                isValid = false;
            }

            AudioSource src = _source != null ? _source : (GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>());
            if (src == null)
            {
                Debug.LogWarning($"[GunAudio] Missing AudioSource on '{gameObject.name}'.", this);
                isValid = false;
            }
            else if (src.outputAudioMixerGroup == null)
            {
                Debug.LogWarning($"[GunAudio] AudioSource on '{gameObject.name}' is missing an AudioMixerGroup.", this);
                isValid = false;
            }

            if (_shotClip == null)
            {
                Debug.LogWarning($"[GunAudio] Missing shot AudioClip on '{gameObject.name}'.", this);
                isValid = false;
            }

            return isValid;
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed || _gun == null)
            {
                return;
            }

            _gun.Fired += HandleFired;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _gun == null)
            {
                return;
            }

            _gun.Fired -= HandleFired;
            _isSubscribed = false;
        }

        private void HandleFired()
        {
            PlaybackCount++;
            PlaybackRequested?.Invoke(_shotClip);
            PlayClip(_shotClip);
        }
    }
}
