using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Represents a persistent stylized 3D geometric impact mark / bullet dent / crater.
    /// Real faceted low-poly geometry parented to the hit surface,
    /// directionally aligned along the impact normal and penetration vector.
    /// </summary>
    public class CombatImpactMark : MonoBehaviour
    {
        [SerializeField] private float _baseRadius = 0.12f;
        [SerializeField] private float _baseDepth = 0.04f;

        private Transform _targetTransform;
        private Action<CombatImpactMark> _onRecycle;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private float _spawnTime;
        private static Mesh s_sharedCraterMesh;
        private static Material s_sharedMarkMaterial;

        public Transform TargetTransform => _targetTransform;
        public float SpawnTime => _spawnTime;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (_filter == null) _filter = GetComponent<MeshFilter>();
            if (_filter == null) _filter = gameObject.AddComponent<MeshFilter>();

            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();

            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            if (s_sharedCraterMesh == null)
            {
                s_sharedCraterMesh = CreateProceduralCraterMesh(_baseRadius, _baseDepth);
            }
            _filter.sharedMesh = s_sharedCraterMesh;

            if (s_sharedMarkMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                s_sharedMarkMaterial = new Material(shader);
                s_sharedMarkMaterial.color = new Color(0.06f, 0.05f, 0.07f, 1f);
            }
            _renderer.sharedMaterial = s_sharedMarkMaterial;

            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
        }

        public void Initialize(
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            Transform parent,
            Action<CombatImpactMark> onRecycle,
            float scaleMultiplier = 1f,
            Color? customColor = null)
        {
            EnsureComponents();
            _targetTransform = parent;
            _onRecycle = onRecycle;
            _spawnTime = Time.time;

            transform.SetParent(parent, true);
            transform.position = position + normal * 0.004f;

            Vector3 forward = normal.sqrMagnitude > 0.001f ? normal : Vector3.up;
            Vector3 tangent = Vector3.Cross(forward, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f)
            {
                tangent = Vector3.Cross(forward, Vector3.right);
            }
            tangent.Normalize();

            Quaternion baseRotation = Quaternion.LookRotation(forward, tangent);

            // Directional penetration angle
            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 projectedDir = Vector3.ProjectOnPlane(direction, normal);
                if (projectedDir.sqrMagnitude > 0.001f)
                {
                    Quaternion tilt = Quaternion.AngleAxis(15f, Vector3.Cross(normal, projectedDir.normalized));
                    baseRotation = tilt * baseRotation;
                }
            }

            transform.rotation = baseRotation;
            float scale = Mathf.Clamp(scaleMultiplier, 0.4f, 2.5f);
            transform.localScale = new Vector3(scale, scale, scale);

            Color markColor = customColor ?? new Color(0.07f, 0.06f, 0.08f, 1f);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", markColor);
            _propBlock.SetColor("_Color", markColor);
            _renderer.SetPropertyBlock(_propBlock);

            gameObject.SetActive(true);
        }

        public void Recycle()
        {
            if (!gameObject.activeSelf) return;

            gameObject.SetActive(false);
            transform.SetParent(null, false);

            // Invoke recycle callback BEFORE clearing _targetTransform so the pool can remove it from per-target map
            try
            {
                _onRecycle?.Invoke(this);
            }
            finally
            {
                _targetTransform = null;
            }
        }

        public static void ResetStaticResources()
        {
            if (s_sharedCraterMesh != null)
            {
                SafeDestroy(s_sharedCraterMesh);
                s_sharedCraterMesh = null;
            }
            if (s_sharedMarkMaterial != null)
            {
                SafeDestroy(s_sharedMarkMaterial);
                s_sharedMarkMaterial = null;
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

        /// <summary>
        /// Generates a stylized 3D low-poly faceted crater/dent with a visible beveled rim and recessed cavity.
        /// Stays visible in front of opaque surfaces without being occluded.
        /// </summary>
        public static Mesh CreateProceduralCraterMesh(float radius, float depth)
        {
            Mesh mesh = new Mesh { name = "ProceduralCraterMesh" };
            const int segments = 8;
            int vertexCount = 1 + segments * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];

            // Center apex slightly above surface
            vertices[0] = new Vector3(0f, 0f, 0.001f);
            normals[0] = Vector3.forward;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float innerR = radius * 0.45f;
            float ridgeZ = 0.006f; // Raised bevel rim
            float outerZ = 0.001f; // Flush outer rim

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Inner raised ridge ring
                int idxInner = 1 + i;
                vertices[idxInner] = new Vector3(cos * innerR, sin * innerR, ridgeZ);
                normals[idxInner] = new Vector3(cos * 0.4f, sin * 0.4f, 0.8f).normalized;
                uvs[idxInner] = new Vector2(0.5f + cos * 0.25f, 0.5f + sin * 0.25f);

                // Outer flush rim
                int idxOuter = 1 + segments + i;
                vertices[idxOuter] = new Vector3(cos * radius, sin * radius, outerZ);
                normals[idxOuter] = Vector3.forward;
                uvs[idxOuter] = new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f);
            }

            int triCount = segments * 3 + segments * 6;
            int[] triangles = new int[triCount];
            int t = 0;

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int currInner = 1 + i;
                int nextInner = 1 + next;
                int currOuter = 1 + segments + i;
                int nextOuter = 1 + segments + next;

                // Center cavity cone
                triangles[t++] = 0;
                triangles[t++] = currInner;
                triangles[t++] = nextInner;

                // Bevel outer ring quad
                triangles[t++] = currInner;
                triangles[t++] = currOuter;
                triangles[t++] = nextOuter;

                triangles[t++] = currInner;
                triangles[t++] = nextOuter;
                triangles[t++] = nextInner;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
