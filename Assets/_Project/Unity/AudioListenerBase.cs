using UnityEngine;

namespace Game
{
    public abstract class AudioListenerBase : MonoBehaviour
    {
        [SerializeField] protected AudioSource _source;

        public AudioSource AudioSource
        {
            get => _source;
            set => _source = value;
        }

        public void SetAudioSource(AudioSource source)
        {
            _source = source;
        }

        protected void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
                if (_source == null)
                {
                    _source = GetComponentInParent<AudioSource>();
                }
            }

            if (_source == null || !_source.isActiveAndEnabled)
            {
                return;
            }

            _source.PlayOneShot(clip);
        }
    }
}
