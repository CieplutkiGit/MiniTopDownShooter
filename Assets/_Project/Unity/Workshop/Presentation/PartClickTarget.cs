using UnityEngine;

namespace Game.Workshop.Presentation
{
    /// <summary>
    /// Identifies an interactive weapon part mesh or collider in 3D preview/inspection space.
    /// </summary>
    [DisallowMultipleComponent]
    public class PartClickTarget : MonoBehaviour
    {
        [SerializeField] private string _slotId = string.Empty;
        [SerializeField] private string _partId = string.Empty;

        public string SlotId
        {
            get => _slotId;
            set => _slotId = value;
        }

        public string PartId
        {
            get => _partId;
            set => _partId = value;
        }
    }
}
