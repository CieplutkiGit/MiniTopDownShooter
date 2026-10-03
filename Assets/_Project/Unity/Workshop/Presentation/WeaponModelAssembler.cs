using System;
using System.Collections.Generic;
using Application.Weapons;
using UnityEngine;

namespace Game.Workshop.Presentation
{
    /// <summary>
    /// Constructs and updates the modular 3D visual hierarchy of a weapon build based on a WeaponVisualProfile.
    /// Manages sockets, part meshes/prefabs, muzzle anchor, and inspection click targets.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponModelAssembler : MonoBehaviour
    {
        [Header("Hierarchy Roots")]
        [Tooltip("Transform under which sockets and meshes are parented. Defaults to this transform.")]
        [SerializeField] private Transform _visualRoot;

        [Tooltip("Live or preview muzzle anchor transform whose position is driven by fitted barrel parts.")]
        [SerializeField] private Transform _muzzleAnchor;

        [Header("Interaction Configuration")]
        [Tooltip("When enabled, adds colliders and PartClickTarget components to parts for 3D inspection/selection.")]
        [SerializeField] private bool _addCollidersForSelection = true;

        private readonly Dictionary<string, Transform> _sockets = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> _partInstances = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _equippedPartIds = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, PartVisualData> _activePartData = new Dictionary<string, PartVisualData>(StringComparer.Ordinal);

        private GameObject _receiverInstance;
        private WeaponBuild _currentBuild;
        private WeaponVisualProfile _currentProfile;

        public Transform VisualRoot
        {
            get => _visualRoot != null ? _visualRoot : transform;
            set => _visualRoot = value;
        }

        public Transform LiveMuzzleAnchor
        {
            get => _muzzleAnchor;
            set => _muzzleAnchor = value;
        }

        public bool AddCollidersForSelection
        {
            get => _addCollidersForSelection;
            set => _addCollidersForSelection = value;
        }

        public GameObject ReceiverInstance => _receiverInstance;
        public WeaponBuild CurrentBuild => _currentBuild;
        public WeaponVisualProfile CurrentProfile => _currentProfile;
        public IReadOnlyDictionary<string, GameObject> SpawnedParts => _partInstances;
        public IReadOnlyDictionary<string, PartVisualData> ActivePartData => _activePartData;

        private void Awake()
        {
            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }

            EnsureMuzzleAnchor();
        }

        /// <summary>
        /// Assembles or updates the modular weapon visual matching the given build and profile.
        /// </summary>
        public void Assemble(WeaponBuild build, WeaponVisualProfile profile)
        {
            _currentBuild = build;
            _currentProfile = profile;

            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }

            EnsureMuzzleAnchor();

            if (profile == null)
            {
                return;
            }

            // 1. Setup / Update Base Receiver
            UpdateReceiver(profile);

            // 2. Identify active slots in the build
            var activeSlots = new HashSet<string>(StringComparer.Ordinal);
            string barrelPartId = null;

            if (build != null && build.Selections != null)
            {
                foreach (var kvp in build.Selections)
                {
                    string slotId = kvp.Key;
                    string partId = kvp.Value;

                    activeSlots.Add(slotId);

                    if (IsBarrelSlot(slotId))
                    {
                        barrelPartId = partId;
                    }

                    AssembleSlot(slotId, partId, profile);
                }
            }

            // 3. Remove obsolete sockets that are no longer part of this build
            var socketsToRemove = new List<string>();
            foreach (var slotId in _sockets.Keys)
            {
                if (!activeSlots.Contains(slotId))
                {
                    socketsToRemove.Add(slotId);
                }
            }

            for (int i = 0; i < socketsToRemove.Count; i++)
            {
                RemoveSlot(socketsToRemove[i]);
            }

            // 4. Update Muzzle Anchor
            UpdateMuzzleAnchor(barrelPartId, profile);
        }

        /// <summary>
        /// Updates the muzzle anchor position based on the fitted barrel part ID or platform default.
        /// </summary>
        public void UpdateMuzzleAnchor(string barrelPartId, WeaponVisualProfile profile)
        {
            EnsureMuzzleAnchor();

            if (_muzzleAnchor == null) return;

            Vector3 offset = profile != null ? profile.GetMuzzleOffset(barrelPartId) : new Vector3(0f, 0f, 1f);
            _muzzleAnchor.localPosition = offset;
        }

        public Transform GetSocket(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            return _sockets.TryGetValue(slotId, out var socket) ? socket : null;
        }

        public GameObject GetPartObject(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            return _partInstances.TryGetValue(slotId, out var partObj) ? partObj : null;
        }

        public bool TryGetActivePartData(string slotId, out PartVisualData data)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                data = null;
                return false;
            }

            return _activePartData.TryGetValue(slotId, out data);
        }

        /// <summary>
        /// Clears all instantiated weapon parts, sockets, and receiver.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in _sockets)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value.gameObject);
                }
            }

            _sockets.Clear();
            _partInstances.Clear();
            _equippedPartIds.Clear();
            _activePartData.Clear();

            if (_receiverInstance != null)
            {
                DestroyImmediate(_receiverInstance);
                _receiverInstance = null;
            }

            _currentBuild = null;
            _currentProfile = null;
        }

        private void AssembleSlot(string slotId, string partId, WeaponVisualProfile profile)
        {
            // Resolve part pose and visual asset
            PartVisualData data;
            if (!profile.TryGetPartPose(slotId, partId, out data))
            {
                // Fallback default pose if not defined in profile
                data = new PartVisualData(slotId, partId);
            }

            _activePartData[slotId] = data;

            // Get or create socket
            Transform socket = GetOrCreateSocket(slotId);
            socket.localPosition = data.AssembledLocalPosition;
            socket.localRotation = data.AssembledLocalRotation;
            socket.localScale = data.AssembledLocalScale;

            // Check if existing part instance matches partId
            if (_equippedPartIds.TryGetValue(slotId, out string currentPartId) &&
                string.Equals(currentPartId, partId, StringComparison.Ordinal) &&
                _partInstances.TryGetValue(slotId, out var existingObj) &&
                existingObj != null)
            {
                // Already fitted with matching part ID
                return;
            }

            // Destroy previous part instance under this socket
            if (_partInstances.TryGetValue(slotId, out var oldObj) && oldObj != null)
            {
                DestroyImmediate(oldObj);
                _partInstances.Remove(slotId);
            }

            // Spawn part visual
            GameObject partObj = null;
            if (data.Prefab != null)
            {
                partObj = Instantiate(data.Prefab, socket);
                partObj.name = $"Part_{partId}";
                partObj.transform.localPosition = Vector3.zero;
                partObj.transform.localRotation = Quaternion.identity;
                partObj.transform.localScale = Vector3.one;
            }
            else if (data.Mesh != null)
            {
                partObj = new GameObject($"Part_{partId}");
                partObj.transform.SetParent(socket, false);
                partObj.transform.localPosition = Vector3.zero;
                partObj.transform.localRotation = Quaternion.identity;
                partObj.transform.localScale = Vector3.one;

                var mf = partObj.AddComponent<MeshFilter>();
                mf.sharedMesh = data.Mesh;

                var mr = partObj.AddComponent<MeshRenderer>();
                if (data.Material != null)
                {
                    mr.sharedMaterial = data.Material;
                }
            }
            else
            {
                // Visual placeholder node
                partObj = new GameObject($"Part_{partId}");
                partObj.transform.SetParent(socket, false);
                partObj.transform.localPosition = Vector3.zero;
                partObj.transform.localRotation = Quaternion.identity;
                partObj.transform.localScale = Vector3.one;
            }

            // Add collider and click target for inspection/selection if configured
            if (_addCollidersForSelection && partObj != null)
            {
                ConfigureClickTarget(partObj, slotId, partId);
            }

            _partInstances[slotId] = partObj;
            _equippedPartIds[slotId] = partId;
        }

        private void RemoveSlot(string slotId)
        {
            if (_partInstances.TryGetValue(slotId, out var partObj) && partObj != null)
            {
                DestroyImmediate(partObj);
            }
            _partInstances.Remove(slotId);
            _equippedPartIds.Remove(slotId);
            _activePartData.Remove(slotId);

            if (_sockets.TryGetValue(slotId, out var socket) && socket != null)
            {
                DestroyImmediate(socket.gameObject);
            }
            _sockets.Remove(slotId);
        }

        private Transform GetOrCreateSocket(string slotId)
        {
            if (_sockets.TryGetValue(slotId, out var socket) && socket != null)
            {
                return socket;
            }

            // Look for existing child socket
            string socketName = $"Socket_{slotId}";
            var existingChild = VisualRoot.Find(socketName);
            if (existingChild != null)
            {
                _sockets[slotId] = existingChild;
                return existingChild;
            }

            var socketGo = new GameObject(socketName);
            socketGo.transform.SetParent(VisualRoot, false);
            _sockets[slotId] = socketGo.transform;
            return socketGo.transform;
        }

        private void UpdateReceiver(WeaponVisualProfile profile)
        {
            if (_receiverInstance != null)
            {
                DestroyImmediate(_receiverInstance);
                _receiverInstance = null;
            }

            if (profile.ReceiverPrefab != null)
            {
                _receiverInstance = Instantiate(profile.ReceiverPrefab, VisualRoot);
                _receiverInstance.name = "Receiver";
                _receiverInstance.transform.localPosition = profile.ReceiverLocalPosition;
                _receiverInstance.transform.localRotation = profile.ReceiverLocalRotation;
                _receiverInstance.transform.localScale = profile.ReceiverLocalScale;
            }
            else if (profile.ReceiverMesh != null)
            {
                _receiverInstance = new GameObject("Receiver");
                _receiverInstance.transform.SetParent(VisualRoot, false);
                _receiverInstance.transform.localPosition = profile.ReceiverLocalPosition;
                _receiverInstance.transform.localRotation = profile.ReceiverLocalRotation;
                _receiverInstance.transform.localScale = profile.ReceiverLocalScale;

                var mf = _receiverInstance.AddComponent<MeshFilter>();
                mf.sharedMesh = profile.ReceiverMesh;

                var mr = _receiverInstance.AddComponent<MeshRenderer>();
                if (profile.ReceiverMaterial != null)
                {
                    mr.sharedMaterial = profile.ReceiverMaterial;
                }
            }
        }

        private void EnsureMuzzleAnchor()
        {
            if (_muzzleAnchor != null) return;

            var existing = VisualRoot.Find("MuzzleAnchor");
            if (existing != null)
            {
                _muzzleAnchor = existing;
                return;
            }

            var go = new GameObject("MuzzleAnchor");
            go.transform.SetParent(VisualRoot, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);
            _muzzleAnchor = go.transform;
        }

        private void ConfigureClickTarget(GameObject partObj, string slotId, string partId)
        {
            var col = partObj.GetComponentInChildren<Collider>();
            if (col == null)
            {
                var mf = partObj.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = partObj.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                }
                else
                {
                    var bc = partObj.AddComponent<BoxCollider>();
                    bc.size = new Vector3(0.2f, 0.2f, 0.2f);
                }
            }

            var target = partObj.GetComponent<PartClickTarget>() ?? partObj.AddComponent<PartClickTarget>();
            target.SlotId = slotId;
            target.PartId = partId;
        }

        private static bool IsBarrelSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return false;
            return slotId.IndexOf("barrel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   slotId.IndexOf("tube", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
