using Application;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace Game
{
    public class EnemyDeathDebris : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemyRef;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private Mesh _chunkMesh;
        [SerializeField] private int _chunkCount = 7;
        [SerializeField] private float _chunkSize = 0.25f;
        [SerializeField] private float _minSpeed = 3f;
        [SerializeField] private float _maxSpeed = 7f;
        [SerializeField] private float _lifetime = 1.1f;
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _bounce = 0.4f;
        [SerializeField] private float _floorY = 0f;

        private IEnemyEvents _enemy;
        private UnityEngine.Pool.ObjectPool<DebrisChunk> _pool;

        public void EnsureInitialized()
        {
            if (_enemyRef == null)
            {
                _enemyRef = GetComponent<EnemyController>();
            }
            _enemy = _enemyRef;
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>();
            }
            if (_pool == null)
            {
                _pool = new UnityEngine.Pool.ObjectPool<DebrisChunk>(CreateChunk, OnGetChunk, OnReleaseChunk, OnDestroyChunk, true, _chunkCount, _chunkCount * 4);
            }
            if (_enemy != null)
            {
                _enemy.Died -= HandleDied;
                _enemy.Died += HandleDied;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.Died -= HandleDied;
            }
        }

        private void HandleDied()
        {
            EnsureInitialized();
            if (_chunkMesh == null)
            {
                _chunkMesh = Combat.CombatDebrisPool.GetOrCreateDefaultChunkMesh();
            }
            if (_chunkMesh == null)
            {
                return;
            }

            for (int i = 0; i < _chunkCount; i++)
            {
                SpawnChunk();
            }
        }

        private void SpawnChunk()
        {
            DebrisChunk chunk = _pool.Get();
            Transform chunkTransform = chunk.transform;

            chunkTransform.position = transform.position + Random.insideUnitSphere * 0.4f;
            chunkTransform.rotation = Random.rotation;

            float size = _chunkSize * Random.Range(0.7f, 1.4f);
            chunkTransform.localScale = new Vector3(size, size, size);

            Vector3 direction = Random.onUnitSphere;

            if (direction.y < 0f)
            {
                direction.y = -direction.y;
            }

            direction.y += 0.6f;
            direction = direction.normalized;

            float speed = Random.Range(_minSpeed, _maxSpeed);
            Vector3 spin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f));

            chunk.Initialize(direction * speed, spin, _lifetime, _gravity, _bounce, _floorY);
        }

        private DebrisChunk CreateChunk()
        {
            GameObject chunk = new GameObject("Debris");

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.sharedMesh = _chunkMesh;

            MeshRenderer chunkRenderer = chunk.AddComponent<MeshRenderer>();
            chunkRenderer.shadowCastingMode = ShadowCastingMode.Off;
            chunkRenderer.receiveShadows = false;

            if (_renderer != null)
            {
                chunkRenderer.sharedMaterial = _renderer.sharedMaterial;
            }

            DebrisChunk debris = chunk.AddComponent<DebrisChunk>();
            debris.SetReturnCallback(ReturnChunk);
            return debris;
        }

        private void OnGetChunk(DebrisChunk chunk)
        {
            chunk.gameObject.SetActive(true);
        }

        private void OnReleaseChunk(DebrisChunk chunk)
        {
            chunk.gameObject.SetActive(false);
        }

        private void OnDestroyChunk(DebrisChunk chunk)
        {
            Destroy(chunk.gameObject);
        }

        private void ReturnChunk(DebrisChunk chunk)
        {
            _pool.Release(chunk);
        }
    }
}
