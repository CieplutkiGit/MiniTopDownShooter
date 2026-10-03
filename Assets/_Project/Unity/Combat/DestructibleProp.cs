using System;
using System.Collections.Generic;
using Core;
using Game;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Suitable arena props receive weapon damage, display visible per-instance geometric
    /// face/triangle cutout and surface marks, fragment in stages, and are cleanly removed at zero health.
    /// Original mesh topology and vertices are 100% restored on reset without mutating shared assets.
    /// Safely handles non-readable/optimized asset meshes without crashing.
    /// </summary>
    public class DestructibleProp : MonoBehaviour, IDamageable
    {
        [Header("Health & Durability")]
        [SerializeField] private int _maxHealth = 80;
        [SerializeField] private int _debrisCountOnStage = 3;
        [SerializeField] private int _debrisCountOnDeath = 10;
        [SerializeField] private float _floorY = 0f;

        [Header("Pieces")]
        [Tooltip("Optional authored child pieces that detach on progressive damage stages.")]
        [SerializeField] private GameObject[] _detachableParts;

        private int _currentHealth;
        private int _currentStage; // 0 = pristine (>66%), 1 = damaged (33-66%), 2 = critical (0-33%), 3 = destroyed (0)
        private bool _isDestroyed;
        private bool _initialized;
        private Collider _collider;
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;
        private Mesh _originalMesh;
        private Mesh _instanceMesh;
        private int[] _originalTriangles;
        private Vector3[] _originalVertices;
        private Vector3[] _originalNormals;
        private Vector2[] _originalUV;
        private Vector3 _originalScale;
        private Material _propMaterial;
        private bool _isMeshCutoutSupported;
        private int _minProtectedTriangles;

        public event Action<DestructibleProp> OnDestroyed;
        public event Action<int, int> OnHealthChanged;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int CurrentStage => _currentStage;
        public bool IsDestroyed => _isDestroyed;
        public bool IsCutoutSupported => _isMeshCutoutSupported;
        public int RenderedTriangleCount => (_isMeshCutoutSupported && _instanceMesh != null) ? _instanceMesh.triangles.Length / 3 : 0;
        public int OriginalTriangleCount => (_isMeshCutoutSupported && _originalTriangles != null) ? _originalTriangles.Length / 3 : 0;

        public int ActivePartCount
        {
            get
            {
                int count = 0;
                if (_detachableParts != null)
                {
                    for (int i = 0; i < _detachableParts.Length; i++)
                    {
                        if (_detachableParts[i] != null && _detachableParts[i].activeSelf) count++;
                    }
                }
                return count;
            }
        }

        private void Awake()
        {
            InitializeProp();
        }

        public void InitializeProp()
        {
            if (_initialized) return;

            _collider = GetComponent<Collider>();
            _renderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _originalScale = transform.localScale;

            if (_renderer != null)
            {
                _propMaterial = _renderer.sharedMaterial;
            }

            // Create per-instance mesh for safe geometric cutout without mutating shared asset mesh
            if (_meshFilter != null && _meshFilter.sharedMesh != null)
            {
                _originalMesh = _meshFilter.sharedMesh;

                // Guard mesh.isReadable to avoid runtime exceptions on non-readable/optimized asset meshes
                if (_originalMesh.isReadable)
                {
                    _originalTriangles = (int[])_originalMesh.triangles.Clone();
                    _originalVertices = (Vector3[])_originalMesh.vertices.Clone();
                    _originalNormals = (Vector3[])_originalMesh.normals.Clone();
                    _originalUV = (Vector2[])_originalMesh.uv.Clone();

                    _instanceMesh = Instantiate(_originalMesh);
                    _instanceMesh.name = _originalMesh.name + "_PropInstance";
                    _meshFilter.sharedMesh = _instanceMesh;
                    _isMeshCutoutSupported = _originalTriangles != null && _originalTriangles.Length > 0;
                    _minProtectedTriangles = 0; // Props can be fully fragmented
                }
                else
                {
                    _isMeshCutoutSupported = false;
                    _instanceMesh = null;
                }
            }

            SetupAuthoredParts();

            _currentHealth = _maxHealth;
            _currentStage = 0;
            _isDestroyed = false;
            _initialized = true;
        }

        private void SetupAuthoredParts()
        {
            if (_detachableParts == null || _detachableParts.Length == 0)
            {
                List<GameObject> childPieces = new List<GameObject>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.GetComponent<MeshRenderer>() != null && child.GetComponent<CombatImpactMark>() == null)
                    {
                        childPieces.Add(child.gameObject);
                    }
                }

                if (childPieces.Count > 0)
                {
                    _detachableParts = childPieces.ToArray();
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

        public void Configure(int maxHealth, int debrisStage = 3, int debrisDeath = 10)
        {
            InitializeProp();

            _maxHealth = Mathf.Max(1, maxHealth);
            _debrisCountOnStage = debrisStage;
            _debrisCountOnDeath = debrisDeath;
            _currentHealth = _maxHealth;
            _currentStage = 0;
            _isDestroyed = false;
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(new DamageData(damage), default);
        }

        public void TakeDamage(DamageData data)
        {
            TakeDamage(data, default);
        }

        public void TakeDamage(DamageData data, HitContext context)
        {
            InitializeProp();

            if (_isDestroyed || data.Damage <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - data.Damage);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            Vector3 hitPoint = context.IsValid ? context.Point : transform.position;
            Vector3 hitNormal = context.IsValid && context.Normal != Vector3.zero ? context.Normal : Vector3.up;
            Vector3 hitDir = context.IsValid && context.Direction != Vector3.zero ? context.Direction : Vector3.forward;

            // Apply persistent 3D physical mark
            CombatImpactPool.SpawnMark(hitPoint, hitNormal, hitDir, transform, 1.1f);

            // Apply actual geometric face/triangle cutout on the prop's rendered instance mesh at the hit location
            if (_isMeshCutoutSupported)
            {
                RemoveNearestTriangle(hitPoint, hitDir);
            }

            // Spawn localized chip debris chunk
            CombatDebrisPool.Instance?.SpawnChunk(
                hitPoint + hitNormal * 0.08f,
                (hitNormal + UnityEngine.Random.insideUnitSphere * 0.4f).normalized * UnityEngine.Random.Range(2f, 4.5f),
                UnityEngine.Random.insideUnitSphere * 240f,
                0.9f,
                0.12f,
                null,
                _propMaterial,
                floorY: _floorY);

            // Check stage transitions
            float healthRatio = (float)_currentHealth / _maxHealth;
            int newStage = _currentStage;

            if (_currentHealth <= 0)
            {
                newStage = 3;
            }
            else if (healthRatio <= 0.33f)
            {
                newStage = 2;
            }
            else if (healthRatio <= 0.66f)
            {
                newStage = 1;
            }

            if (newStage > _currentStage)
            {
                for (int s = _currentStage + 1; s <= newStage; s++)
                {
                    HandleStageTransition(s, hitPoint, hitDir);
                }
                _currentStage = newStage;
            }

            if (_currentHealth <= 0)
            {
                HandleLethalDestruction(hitPoint, hitDir);
            }
        }

        public bool RemoveNearestTriangle(Vector3 worldHitPoint, Vector3 worldDirection)
        {
            if (!_isMeshCutoutSupported || _instanceMesh == null || _originalTriangles == null) return false;

            int[] currentTris = _instanceMesh.triangles;
            if (currentTris == null || currentTris.Length <= _minProtectedTriangles + 3) return false;

            Transform meshTransform = _meshFilter != null ? _meshFilter.transform : transform;
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
            if (currentTris == null || currentTris.Length <= _minProtectedTriangles + 6) return;

            Vector3[] verts = _instanceMesh.vertices;
            int triCount = currentTris.Length / 3;

            Vector3 targetDir = stage == 1 ? new Vector3(1f, 1f, 1f).normalized
                              : stage == 2 ? new Vector3(-1f, 1f, -1f).normalized
                              : new Vector3(0f, 1f, -1f).normalized;

            // Remove 2 closest triangles to the section quadrant (forms a visible missing face/corner)
            for (int pass = 0; pass < 2 && currentTris.Length > _minProtectedTriangles + 3; pass++)
            {
                triCount = currentTris.Length / 3;
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
                    currentTris = newTris;
                }
            }

            _instanceMesh.triangles = currentTris;
            _instanceMesh.RecalculateBounds();
        }

        private void HandleStageTransition(int stage, Vector3 hitPoint, Vector3 hitDir)
        {
            if (stage == 3) return; // Handled by HandleLethalDestruction

            // If we have authored detachable parts, detach one
            int partIdx = stage - 1;
            if (_detachableParts != null && partIdx < _detachableParts.Length && _detachableParts[partIdx] != null)
            {
                GameObject part = _detachableParts[partIdx];
                if (part.activeSelf)
                {
                    part.SetActive(false);
                }
            }

            // Remove deterministic section triangles from the instance mesh
            if (_isMeshCutoutSupported)
            {
                RemoveStageSection(stage);
            }

            // Spawn staged fragmentation debris chunks
            if (CombatDebrisPool.Instance != null)
            {
                for (int i = 0; i < _debrisCountOnStage; i++)
                {
                    Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.35f;
                    Vector3 blastDir = (hitDir + UnityEngine.Random.onUnitSphere * 0.7f).normalized;
                    if (blastDir.y < 0.1f) blastDir.y = 0.3f;

                    CombatDebrisPool.Instance.SpawnChunk(
                        transform.position + offset,
                        blastDir * UnityEngine.Random.Range(3f, 6.5f),
                        UnityEngine.Random.insideUnitSphere * 360f,
                        1.2f,
                        0.22f,
                        null,
                        _propMaterial,
                        floorY: _floorY);
                }
            }
        }

        private void HandleLethalDestruction(Vector3 hitPoint, Vector3 hitDir)
        {
            _isDestroyed = true;

            // Spawn final lethal breakup debris
            if (CombatDebrisPool.Instance != null)
            {
                for (int i = 0; i < _debrisCountOnDeath; i++)
                {
                    Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.5f;
                    Vector3 burstDir = UnityEngine.Random.onUnitSphere;
                    if (burstDir.y < 0.2f) burstDir.y = 0.4f;
                    burstDir = (burstDir + hitDir * 0.4f).normalized;

                    CombatDebrisPool.Instance.SpawnChunk(
                        transform.position + offset,
                        burstDir * UnityEngine.Random.Range(4f, 8.5f),
                        UnityEngine.Random.insideUnitSphere * 480f,
                        1.4f,
                        UnityEngine.Random.Range(0.18f, 0.38f),
                        null,
                        _propMaterial,
                        floorY: _floorY);
                }
            }

            // Immediately disable collider so navigation/bullets pass through cleanly
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // Disable renderer and attached marks
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            CombatImpactPool.ClearMarksFor(transform);
            OnDestroyed?.Invoke(this);

            // Hide whole GameObject after debris launch
            gameObject.SetActive(false);
        }

        public void ResetProp()
        {
            InitializeProp();

            _isDestroyed = false;
            _currentHealth = _maxHealth;
            _currentStage = 0;
            transform.localScale = _originalScale;

            if (_collider != null)
            {
                _collider.enabled = true;
            }
            if (_renderer != null)
            {
                _renderer.enabled = true;
            }
            if (_detachableParts != null)
            {
                for (int i = 0; i < _detachableParts.Length; i++)
                {
                    if (_detachableParts[i] != null)
                    {
                        _detachableParts[i].SetActive(true);
                    }
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
            gameObject.SetActive(true);
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
