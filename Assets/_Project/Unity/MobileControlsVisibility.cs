using UnityEngine;

namespace Game
{
    public class MobileControlsVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject _controlsRoot;
        [SerializeField] private bool _forceVisibleInEditor = true;
        [SerializeField] private bool _forceVisible;

        private void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (_controlsRoot == null)
            {
                return;
            }

            bool visible = _forceVisible || Application.isMobilePlatform;

#if UNITY_EDITOR
            visible |= _forceVisibleInEditor;
#endif

            _controlsRoot.SetActive(visible);
        }
    }
}
