using UnityEngine;

namespace Game
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 10, -5);
        [SerializeField] private ScreenShake _shake;

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _basePosition;

        private AudioListener _myListener;
        private static AudioListener s_ActiveSceneListener;

        private void Awake()
        {
            EnsureListener();
        }

        private void OnEnable()
        {
            EnsureListener();

            if (_target == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) _target = pc.transform;
            }
            ResetToTarget();

            // Enforce scene-local active-camera ownership with restoration on transitions
            if (_myListener != null)
            {
                var allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < allListeners.Length; i++)
                {
                    var l = allListeners[i];
                    if (l != _myListener && l.enabled)
                    {
                        l.enabled = false;
                    }
                }
                _myListener.enabled = true;
                s_ActiveSceneListener = _myListener;
            }
        }

        private void OnDisable()
        {
            if (s_ActiveSceneListener == _myListener)
            {
                s_ActiveSceneListener = null;
                var allFollowers = FindObjectsByType<CameraFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < allFollowers.Length; i++)
                {
                    var cf = allFollowers[i];
                    if (cf != this && cf.enabled && cf.gameObject.activeInHierarchy)
                    {
                        var l = cf.GetComponent<AudioListener>();
                        if (l != null)
                        {
                            l.enabled = true;
                            s_ActiveSceneListener = l;
                            break;
                        }
                    }
                }
            }
        }

        private void EnsureListener()
        {
            if (_myListener == null)
            {
                _myListener = GetComponent<AudioListener>();
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            ResetToTarget();
        }

        public void ResetToTarget()
        {
            if (_target != null)
            {
                _velocity = Vector3.zero;
                _basePosition = _target.position + _offset;
                transform.position = _basePosition;
            }
        }

        private void LateUpdate()
        {
            Vector3 targetPosition = _target.position + _offset;
            _basePosition = Vector3.SmoothDamp(_basePosition, targetPosition, ref _velocity, _smoothTime);

            Vector3 result = _basePosition;
            if (_shake != null)
            {
                result += _shake.CurrentOffset;
            }

            transform.position = result;
        }

        private void OnValidate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = _target.position + _offset;
        }
    }
}
