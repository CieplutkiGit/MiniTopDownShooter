using System;
using System.Collections.Generic;
using Core;
using Game;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Drives progressive physical piece loss, directional 3D marks, and bounded
    /// per-instance mesh face/triangle cutout directly on enemy body geometry.
    /// Core geometry is protected while living, and all geometry and marks are 100% restored on pooled respawn.
    /// Safely handles non-readable/optimized asset meshes without crashing.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyDamageVisuals : MonoBehaviour
    {
        [Header("Breakaway Parts")]
        [Tooltip("Optional child parts that detach sequentially as health decreases.")]
        [SerializeField] private GameObject[] _breakawayPieces;

        private HealthComponent _health;
        private EnemyController _enemyController;
        private MeshRenderer _mainRenderer;
        private MeshFilter _mainMeshFilter;
        private Mesh _originalMesh;
        private Mesh _instanceMesh;
        private int[] _originalTriangles;
        private Vector3[] _originalVertices;
        private Vector3[] _originalNormals;
        private Vector2[] _originalUV;
        private Material _enemyMaterial;
        private int _currentStage; // 0 = pristine (>75%), 1 = (50-75%), 2 = (25-50%), 3 = (0-25%), 4 = dead
        private readonly List<GameObject> _activePieces = new List<GameObject>();
        private bool _initialized;
        private bool _isSubscribed;
        private bool _isMeshCutoutSupported;
        private int _minProtectedIndexCount;

        public int CurrentStage => _currentStage;
        public bool IsCutoutSupported => _isMeshCutoutSupported;
        public int RenderedTriangleCount => (_isMeshCutoutSupported && _instanceMesh != null) ? _instanceMesh.triangles.Length / 3 : 0;
        public int OriginalTriangleCount => (_isMeshCutoutSupported && _originalTriangles != null) ? _originalTriangles.Length / 3 : 0;
        public int MinProtectedTriangleCount => _minProtectedIndexCount / 3;

        public int ActivePieceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _activePieces.Count; i++)
                {
                    if (_activePieces[i] != null && _activePieces[i].activeSelf)
                    {
                        var r = _activePieces[i].GetComponent<Renderer>();
                        if (r == null || r.enabled)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        public int TotalPieceCount => _activePieces.Count;

        private void Awake()
        {
            InitializeVisuals();
        }

        public void InitializeVisuals()
        {
            if (_initialized) return;

            _health = GetComponent<HealthComponent>();
            _enemyController = GetComponent<EnemyController>();
            _mainRenderer = GetComponentInChildren<MeshRenderer>();
            _mainMeshFilter = GetComponentInChildren<MeshFilter>();

            if (_mainRenderer != null)
            {
                _enemyMaterial = _mainRenderer.sharedMaterial;
            }

            // Create per-instance mesh for safe geometric cutout directly on the enemy body
            if (_mainMeshFilter != null && _mainMeshFilter.sharedMesh != null)
            {
                _originalMesh = _mainMeshFilter.sharedMesh;

                // Guard mesh.isReadable to avoid runtime exceptions on non-readable/optimized asset meshes
                if (_originalMesh.isReadable)
                {
                    _originalTriangles = (int[])_originalMesh.triangles.Clone();
                    _originalVertices = (Vector3[])_originalMesh.vertices.Clone();
                    _originalNormals = (Vector3[])_originalMesh.normals.Clone();
                    _originalUV = (Vector2[])_originalMesh.uv.Clone();

                    _instanceMesh = Instantiate(_originalMesh);
                    _instanceMesh.name = _originalMesh.name + "_EnemyInstance";
                    // Assign through sharedMesh: the clone is already explicitly owned
                    // here, and reading MeshFilter.mesh in EditMode can create another
                    // implicit clone that Unity reports as leaked.
                    _mainMeshFilter.sharedMesh = _instanceMesh;

                    _isMeshCutoutSupported = _originalTriangles != null && _originalTriangles.Length > 0;
                    // Protect core body: living enemies must always retain at least 40% of their body geometry (minimum 6 triangles / 18 indices)
                    _minProtectedIndexCount = Mathf.Max(18, Mathf.RoundToInt(_originalTriangles.Length * 0.4f));
                }
                else
                {
                    _isMeshCutoutSupported = false;
                    _instanceMesh = null;
                }
            }

            SetupPieces();
            _initialized = true;
        }

        public void EnsureSubscribed()
        {
            InitializeVisuals();
            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }

            if (_health != null && !_isSubscribed)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnHealthChanged += HandleHealthChanged;
                _health.OnDamagedWithContext -= HandleDamagedWithContext;
                _health.OnDamagedWithContext += HandleDamagedWithContext;
                _isSubscribed = true;
            }
        }

        private void SetupPieces()
        {
            _activePieces.Clear();

            if (_breakawayPieces != null && _breakawayPieces.Length > 0)
            {
                for (int i = 0; i < _breakawayPieces.Length; i++)
                {
                    if (_breakawayPieces[i] != null)
                    {
                        _activePieces.Add(_breakawayPieces[i]);
                    }
                }
            }
            else
            {
                // Inspect child transforms for distinct authored geometric parts (excluding main body)
                MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < childRenderers.Length; i++)
                {
                    if (childRenderers[i] != null && childRenderers[i] != _mainRenderer && childRenderers[i].transform != transform)
                    {
                        _activePieces.Add(childRenderers[i].gameObject);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_instanceMesh != null)
            {
                SafeDestroy(_instanceMesh);
                _instanceMesh = null;
            }
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            ResetToPristine();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnDamagedWithContext -= HandleDamagedWithContext;
            }
            _isSubscribed = false;

            CombatImpactPool.ClearMarksFor(transform);
        }

        private void HandleDamagedWithContext(int damage, HitContext context)
        {
            if (_health != null && _health.CurrentHealth <= 0) return;

            Vector3 hitPoint = context.IsValid ? context.Point : transform.position;
            Vector3 hitNormal = context.IsValid && context.Normal != Vector3.zero ? context.Normal : Vector3.up;
            Vector3 hitDir = context.IsValid && context.Direction != Vector3.zero ? context.Direction : transform.forward;

            // Apply persistent 3D physical mark (tracks enemy motion)
            CombatImpactPool.SpawnMark(hitPoint, hitNormal, hitDir, transform, 0.9f);

            // Apply actual geometric face/triangle cutout directly on the enemy's rendered instance mesh at the wound location
            if (_isMeshCutoutSupported)
            {
                RemoveNearestTriangle(hitPoint, hitDir);
            }
        }

        public bool RemoveNearestTriangle(Vector3 worldHitPoint, Vector3 worldDirection)
        {
            if (!_isMeshCutoutSupported || _instanceMesh == null || _originalTriangles == null) return false;

            int[] currentTris = _instanceMesh.triangles;
            if (currentTris == null || currentTris.Length <= _minProtectedIndexCount + 3) return false;

            // Use the actual mesh filter transform to convert wound coordinates, correctly handling child-mesh models
            Transform meshTransform = _mainMeshFilter != null ? _mainMeshFilter.transform : transform;
            Vector3 localHit = meshTransform.InverseTransformPoint(worldHitPoint);
            Vector3[] verts = _instanceMesh.vertices;
            int triCount = currentTris.Length / 3;

            int bestTri = -1;
            float bestDist = float.MaxValue;

            for (int t = 0; t < triCount; t++)
            {
                int i0 = currentTris[t * 3];
                int i1 = currentTris[t * 3 + 1];
                int i2 = currentTris[t * 3 + 2];

                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];
                Vector3 center = (v0 + v1 + v2) / 3f;

                float dist = Vector3.Distance(center, localHit);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTri = t;
                }
            }

            if (bestTri >= 0)
            {
                int[] newTris = new int[currentTris.Length - 3];
                int dst = 0;
                for (int t = 0; t < triCount; t++)
                {
                    if (t == bestTri) continue;
                    newTris[dst++] = currentTris[t * 3];
                    newTris[dst++] = currentTris[t * 3 + 1];
                    newTris[dst++] = currentTris[t * 3 + 2];
                }

                _instanceMesh.triangles = newTris;
                _instanceMesh.RecalculateBounds();
                return true;
            }

            return false;
        }

        private void RemoveStageSection(int stage)
        {
            if (!_isMeshCutoutSupported || _instanceMesh == null || _originalTriangles == null) return;
            int[] currentTris = _instanceMesh.triangles;
            if (currentTris == null || currentTris.Length <= _minProtectedIndexCount + 3) return;

            Vector3[] verts = _instanceMesh.vertices;
            int triCount = currentTris.Length / 3;

            Vector3 targetDir = stage == 1 ? new Vector3(1f, 1f, 0.5f).normalized
                              : stage == 2 ? new Vector3(-1f, 1f, -0.5f).normalized
                              : new Vector3(0f, 1f, -1f).normalized;

            int bestTri = -1;
            float maxDot = -float.MaxValue;

            for (int t = 0; t < triCount; t++)
            {
                Vector3 c = (verts[currentTris[t * 3]] + verts[currentTris[t * 3 + 1]] + verts[currentTris[t * 3 + 2]]) / 3f;
                float dot = Vector3.Dot(c.normalized, targetDir);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestTri = t;
                }
            }

            if (bestTri >= 0)
            {
                int[] newTris = new int[currentTris.Length - 3];
                int dst = 0;
                for (int t = 0; t < triCount; t++)
                {
                    if (t == bestTri) continue;
                    newTris[dst++] = currentTris[t * 3];
                    newTris[dst++] = currentTris[t * 3 + 1];
                    newTris[dst++] = currentTris[t * 3 + 2];
                }

                _instanceMesh.triangles = newTris;
                _instanceMesh.RecalculateBounds();
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (max <= 0) return;

            // If health restored to max (e.g. pool respawn)
            if (current >= max)
            {
                ResetToPristine();
                return;
            }

            if (current <= 0)
            {
                _currentStage = 4;
                return;
            }

            float ratio = (float)current / max;
            int targetStage = _currentStage;

            if (ratio <= 0.25f) targetStage = 3;
            else if (ratio <= 0.50f) targetStage = 2;
            else if (ratio <= 0.75f) targetStage = 1;
            else targetStage = 0;

            if (targetStage > _currentStage)
            {
                for (int s = _currentStage + 1; s <= targetStage; s++)
                {
                    DetachPieceForStage(s);
                    if (_isMeshCutoutSupported)
                    {
                        RemoveStageSection(s);
                    }
                }
                _currentStage = targetStage;
            }
        }

        private void DetachPieceForStage(int stage)
        {
            int pieceIndex = stage - 1;
            if (pieceIndex >= 0 && pieceIndex < _activePieces.Count)
            {
                GameObject piece = _activePieces[pieceIndex];
                if (piece != null && piece.activeSelf)
                {
                    Vector3 piecePos = piece.transform.position;

                    // Preserve critical components on transforms essential to behavior
                    bool hasCriticalComponent =
                        piece.GetComponent<Collider>() != null ||
                        piece.GetComponent<HealthComponent>() != null ||
                        piece.GetComponent<EnemyController>() != null ||
                        piece == gameObject;

                    if (hasCriticalComponent)
                    {
                        var r = piece.GetComponent<MeshRenderer>();
                        if (r != null) r.enabled = false;
                    }
                    else
                    {
                        piece.SetActive(false);
                    }

                    // Launch as physical tumbling debris chunk
                    Vector3 blastDir = (piecePos - transform.position).normalized;
                    if (blastDir.sqrMagnitude < 0.01f) blastDir = UnityEngine.Random.onUnitSphere;
                    if (blastDir.y < 0.1f) blastDir.y = 0.35f;

                    CombatDebrisPool.Instance?.SpawnChunk(
                        piecePos,
                        blastDir * UnityEngine.Random.Range(3f, 6.5f),
                        UnityEngine.Random.insideUnitSphere * 360f,
                        1.2f,
                        0.25f,
                        null,
                        _enemyMaterial);
                }
            }
        }

        /// <summary>
        /// Fully restores all detached geometry pieces, restores original mesh topology and vertices,
        /// and clears marks on pooled respawn.
        /// </summary>
        public void ResetToPristine()
        {
            _currentStage = 0;

            for (int i = 0; i < _activePieces.Count; i++)
            {
                if (_activePieces[i] != null)
                {
                    _activePieces[i].SetActive(true);
                    var r = _activePieces[i].GetComponent<MeshRenderer>();
                    if (r != null) r.enabled = true;
                }
            }

            // 100% restore original topology, triangles, and vertices
            if (_isMeshCutoutSupported && _instanceMesh != null && _originalTriangles != null)
            {
                _instanceMesh.triangles = (int[])_originalTriangles.Clone();
                if (_originalVertices != null) _instanceMesh.vertices = (Vector3[])_originalVertices.Clone();
                if (_originalNormals != null) _instanceMesh.normals = (Vector3[])_originalNormals.Clone();
                if (_originalUV != null) _instanceMesh.uv = (Vector2[])_originalUV.Clone();
                _instanceMesh.RecalculateBounds();
            }

            CombatImpactPool.ClearMarksFor(transform);
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
