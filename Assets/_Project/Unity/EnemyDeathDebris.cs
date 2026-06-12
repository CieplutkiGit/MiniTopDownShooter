using Application;
using UnityEngine;
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

        private void Awake()
        {
            _enemy = _enemyRef;
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
            GameObject chunk = new GameObject("Debris");
            chunk.transform.position = transform.position + Random.insideUnitSphere * 0.4f;
            chunk.transform.rotation = Random.rotation;

            float size = _chunkSize * Random.Range(0.7f, 1.4f);
            chunk.transform.localScale = new Vector3(size, size, size);

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.sharedMesh = _chunkMesh;

            MeshRenderer chunkRenderer = chunk.AddComponent<MeshRenderer>();
            chunkRenderer.shadowCastingMode = ShadowCastingMode.Off;
            chunkRenderer.receiveShadows = false;

            if (_renderer != null)
            {
                chunkRenderer.sharedMaterial = _renderer.sharedMaterial;
            }

            Vector3 direction = Random.onUnitSphere;

            if (direction.y < 0f)
            {
                direction.y = -direction.y;
            }

            direction.y += 0.6f;
            direction = direction.normalized;

            float speed = Random.Range(_minSpeed, _maxSpeed);
            Vector3 spin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f));

            DebrisChunk debris = chunk.AddComponent<DebrisChunk>();
            debris.Initialize(direction * speed, spin, _lifetime, _gravity, _bounce, _floorY);
        }
    }
}
