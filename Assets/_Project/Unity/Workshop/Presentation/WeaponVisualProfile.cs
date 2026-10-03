using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Workshop.Presentation
{
    /// <summary>
    /// Visual pose and asset data for a single weapon part attachment.
    /// </summary>
    [Serializable]
    public class PartVisualData
    {
        [SerializeField] private string _slotId = string.Empty;
        [SerializeField] private string _partId = string.Empty;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        [SerializeField] private Vector3 _assembledLocalPosition = Vector3.zero;
        [SerializeField] private Quaternion _assembledLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 _assembledLocalScale = Vector3.one;
        [SerializeField] private Vector3 _explodedLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 _muzzleOffset = Vector3.zero;

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

        public GameObject Prefab
        {
            get => _prefab;
            set => _prefab = value;
        }

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public Vector3 AssembledLocalPosition
        {
            get => _assembledLocalPosition;
            set => _assembledLocalPosition = value;
        }

        public Quaternion AssembledLocalRotation
        {
            get => _assembledLocalRotation;
            set => _assembledLocalRotation = value;
        }

        public Vector3 AssembledLocalScale
        {
            get => _assembledLocalScale == Vector3.zero ? Vector3.one : _assembledLocalScale;
            set => _assembledLocalScale = value == Vector3.zero ? Vector3.one : value;
        }

        public Vector3 ExplodedLocalOffset
        {
            get => _explodedLocalOffset;
            set => _explodedLocalOffset = value;
        }

        public Vector3 MuzzleOffset
        {
            get => _muzzleOffset;
            set => _muzzleOffset = value;
        }

        public PartVisualData()
        {
            _assembledLocalScale = Vector3.one;
            _assembledLocalRotation = Quaternion.identity;
        }

        public PartVisualData(string slotId, string partId) : this()
        {
            _slotId = slotId;
            _partId = partId;
        }

        public PartVisualData Clone()
        {
            return new PartVisualData
            {
                _slotId = _slotId,
                _partId = _partId,
                _prefab = _prefab,
                _mesh = _mesh,
                _material = _material,
                _assembledLocalPosition = _assembledLocalPosition,
                _assembledLocalRotation = _assembledLocalRotation,
                _assembledLocalScale = _assembledLocalScale == Vector3.zero ? Vector3.one : _assembledLocalScale,
                _explodedLocalOffset = _explodedLocalOffset,
                _muzzleOffset = _muzzleOffset
            };
        }
    }

    /// <summary>
    /// Visual assembly layout definition for a modular weapon platform.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponVisualProfile", menuName = "Mini Top Down Shooter/Workshop/Weapon Visual Profile")]
    public class WeaponVisualProfile : ScriptableObject
    {
        [Header("Weapon Identity")]
        [SerializeField] private string _weaponId = string.Empty;

        [Header("Base Receiver / Root")]
        [SerializeField] private GameObject _receiverPrefab;
        [SerializeField] private Mesh _receiverMesh;
        [SerializeField] private Material _receiverMaterial;
        [SerializeField] private Vector3 _receiverLocalPosition = Vector3.zero;
        [SerializeField] private Quaternion _receiverLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 _receiverLocalScale = Vector3.one;

        [Header("Muzzle Anchor Configuration")]
        [SerializeField] private Vector3 _defaultMuzzleOffset = new Vector3(0f, 0f, 1f);

        [Header("Part Visual Entries")]
        [SerializeField] private List<PartVisualData> _parts = new List<PartVisualData>();

        public string WeaponId
        {
            get => _weaponId;
            set => _weaponId = value;
        }

        public GameObject ReceiverPrefab
        {
            get => _receiverPrefab;
            set => _receiverPrefab = value;
        }

        public Mesh ReceiverMesh
        {
            get => _receiverMesh;
            set => _receiverMesh = value;
        }

        public Material ReceiverMaterial
        {
            get => _receiverMaterial;
            set => _receiverMaterial = value;
        }

        public Vector3 ReceiverLocalPosition
        {
            get => _receiverLocalPosition;
            set => _receiverLocalPosition = value;
        }

        public Quaternion ReceiverLocalRotation
        {
            get => _receiverLocalRotation;
            set => _receiverLocalRotation = value;
        }

        public Vector3 ReceiverLocalScale
        {
            get => _receiverLocalScale == Vector3.zero ? Vector3.one : _receiverLocalScale;
            set => _receiverLocalScale = value == Vector3.zero ? Vector3.one : value;
        }

        public Vector3 DefaultMuzzleOffset
        {
            get => _defaultMuzzleOffset;
            set => _defaultMuzzleOffset = value;
        }

        public IReadOnlyList<PartVisualData> Parts => _parts;

        public bool TryGetPartPose(string slotId, string partId, out PartVisualData data)
        {
            if (string.IsNullOrEmpty(slotId) && string.IsNullOrEmpty(partId))
            {
                data = null;
                return false;
            }

            for (int i = 0; i < _parts.Count; i++)
            {
                var entry = _parts[i];
                if (entry == null) continue;

                bool slotMatches = string.IsNullOrEmpty(slotId) || string.Equals(entry.SlotId, slotId, StringComparison.OrdinalIgnoreCase);
                bool partMatches = string.Equals(entry.PartId, partId, StringComparison.OrdinalIgnoreCase);

                if (slotMatches && partMatches)
                {
                    data = entry.Clone();
                    return true;
                }
            }

            data = null;
            return false;
        }

        public Vector3 GetMuzzleOffset(string barrelPartId)
        {
            if (!string.IsNullOrEmpty(barrelPartId))
            {
                for (int i = 0; i < _parts.Count; i++)
                {
                    var entry = _parts[i];
                    if (entry != null && string.Equals(entry.PartId, barrelPartId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (entry.MuzzleOffset != Vector3.zero)
                        {
                            return entry.MuzzleOffset;
                        }
                    }
                }
            }

            return _defaultMuzzleOffset;
        }

        public void AddOrUpdatePart(PartVisualData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            for (int i = 0; i < _parts.Count; i++)
            {
                if (string.Equals(_parts[i].SlotId, data.SlotId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_parts[i].PartId, data.PartId, StringComparison.OrdinalIgnoreCase))
                {
                    _parts[i] = data.Clone();
                    return;
                }
            }

            _parts.Add(data.Clone());
        }

        public void SetReceiver(GameObject prefab, Mesh mesh, Material material, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            _receiverPrefab = prefab;
            _receiverMesh = mesh;
            _receiverMaterial = material;
            _receiverLocalPosition = localPos;
            _receiverLocalRotation = localRot;
            _receiverLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
        }

        public void SetDefaultMuzzleOffset(Vector3 offset)
        {
            _defaultMuzzleOffset = offset;
        }

        public void ClearParts()
        {
            _parts.Clear();
        }
    }
}
