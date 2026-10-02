using Cieplutki.MiniTopDownShooter.Application;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace Cieplutki.MiniTopDownShooter.Runtime
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
        private ObjectPool<DebrisChunk> _pool;

        private void Awake()
        {
            _enemy = _enemyRef;
            _pool = new ObjectPool<DebrisChunk>(CreateChunk, OnGetChunk, OnReleaseChunk, OnDestroyChunk, true, _chunkCount, _chunkCount * 4);
        }

        private void OnEnable()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_enemy == null)
            {
                return;
            }

            _enemy.Died -= HandleDied;
        }

        private void HandleDied()
        {
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
