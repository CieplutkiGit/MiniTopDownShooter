using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    /// <summary>
    /// Strict bounded pool for finite-lived, collision-safe geometric debris chunks.
    /// Used by staged enemy piece loss, lethal enemy breakup, and prop fragmentation.
    /// </summary>
    public class CombatDebrisPool : MonoBehaviour
    {
        [SerializeField] private int _maxActiveDebris = 50;

        private static CombatDebrisPool s_instance;
        private static bool s_isQuitting;
        private static Mesh s_defaultChunkMesh;

        private readonly Queue<DebrisChunk> _available = new Queue<DebrisChunk>();
        private readonly List<DebrisChunk> _active = new List<DebrisChunk>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_isQuitting = false;
            s_instance = null;
            if (s_defaultChunkMesh != null)
            {
                SafeDestroy(s_defaultChunkMesh);
                s_defaultChunkMesh = null;
            }
        }

        public static CombatDebrisPool Instance
        {
            get
            {
                if (s_isQuitting) return null;
                if (s_instance == null)
                {
                    s_instance = FindFirstObjectByType<CombatDebrisPool>();
                    if (s_instance == null && !s_isQuitting)
                    {
                        GameObject go = new GameObject("CombatDebrisPool");
                        s_instance = go.AddComponent<CombatDebrisPool>();
                    }
                }
                return s_instance;
            }
        }

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
        public int MaxActiveDebris => _maxActiveDebris;

        private void Awake()
        {
            _maxActiveDebris = Mathf.Max(1, _maxActiveDebris);

            if (s_instance == null)
            {
                s_instance = this;
            }
            UnityEngine.Application.quitting += HandleApplicationQuitting;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private static void HandleApplicationQuitting()
        {
            s_isQuitting = true;
        }

        private void OnDestroy()
        {
            UnityEngine.Application.quitting -= HandleApplicationQuitting;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            if (s_instance == this)
            {
                s_instance = null;
            }
            ClearAndDestroyAll();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            ClearAll();
        }

        public static Mesh GetOrCreateDefaultChunkMesh()
        {
            if (s_defaultChunkMesh == null)
            {
                // Create stylized faceted wedge / shard mesh
                s_defaultChunkMesh = new Mesh { name = "DebrisShardMesh" };
                Vector3[] verts = new Vector3[]
                {
                    new Vector3(-0.1f, -0.1f, -0.1f),
                    new Vector3(0.12f, -0.1f, -0.08f),
                    new Vector3(0.08f, -0.1f, 0.12f),
                    new Vector3(-0.08f, -0.1f, 0.1f),
                    new Vector3(0f, 0.15f, 0f) // apex
                };
                int[] tris = new int[]
                {
                    0, 2, 1, 0, 3, 2, // base
                    0, 1, 4, // side 1
                    1, 2, 4, // side 2
                    2, 3, 4, // side 3
                    3, 0, 4  // side 4
                };
                s_defaultChunkMesh.vertices = verts;
                s_defaultChunkMesh.triangles = tris;
                s_defaultChunkMesh.RecalculateNormals();
                s_defaultChunkMesh.RecalculateBounds();
            }
            return s_defaultChunkMesh;
        }

        public DebrisChunk SpawnChunk(
            Vector3 position,
            Vector3 velocity,
            Vector3 spin,
            float lifetime,
            float scale,
            Mesh mesh = null,
            Material material = null,
            Color? color = null,
            float floorY = 0f,
            float bounce = 0.4f,
            float gravity = 18f)
        {
            if (s_isQuitting) return null;

            _maxActiveDebris = Mathf.Max(1, _maxActiveDebris);

            // Enforce hard budget cap
            while (_active.Count >= _maxActiveDebris && _active.Count > 0)
            {
                DebrisChunk oldest = _active[0];
                _active.RemoveAt(0);
                if (oldest != null)
                {
                    oldest.gameObject.SetActive(false);
                    oldest.transform.SetParent(transform, false);
                    _available.Enqueue(oldest);
                }
            }

            DebrisChunk chunk = null;
            while (_available.Count > 0 && chunk == null)
            {
                chunk = _available.Dequeue();
            }

            if (chunk == null)
            {
                GameObject go = new GameObject("CombatDebrisChunk");
                go.transform.SetParent(transform, false);
                chunk = go.AddComponent<DebrisChunk>();
                chunk.SetReturnCallback(HandleChunkFinished);
            }

            chunk.transform.position = position;
            chunk.transform.rotation = UnityEngine.Random.rotation;
            chunk.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

            Mesh visualMesh = mesh != null ? mesh : GetOrCreateDefaultChunkMesh();
            chunk.SetVisuals(visualMesh, material, color);
            chunk.Initialize(velocity, spin, lifetime, gravity, bounce, floorY);

            chunk.gameObject.SetActive(true);
            _active.Add(chunk);
            return chunk;
        }

        private void HandleChunkFinished(DebrisChunk chunk)
        {
            if (chunk == null) return;
            _active.Remove(chunk);
            chunk.gameObject.SetActive(false);
            chunk.transform.SetParent(transform, false);
            if (!_available.Contains(chunk))
            {
                _available.Enqueue(chunk);
            }
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null)
                {
                    _active[i].gameObject.SetActive(false);
                    _active[i].transform.SetParent(transform, false);
                    if (!_available.Contains(_active[i]))
                    {
                        _available.Enqueue(_active[i]);
                    }
                }
            }
            _active.Clear();
        }

        public void ClearAndDestroyAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null)
                {
                    SafeDestroy(_active[i].gameObject);
                }
            }
            _active.Clear();

            while (_available.Count > 0)
            {
                DebrisChunk chunk = _available.Dequeue();
                if (chunk != null)
                {
                    SafeDestroy(chunk.gameObject);
                }
            }
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
