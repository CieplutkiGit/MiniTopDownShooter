using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public abstract class AudioListenerBase : MonoBehaviour
    {
        [SerializeField] protected AudioSource _source;

        protected void PlayClip(AudioClip clip)
        {
            if (clip == null || _source == null)
            {
                return;
            }

            _source.PlayOneShot(clip);
        }
    }
}
