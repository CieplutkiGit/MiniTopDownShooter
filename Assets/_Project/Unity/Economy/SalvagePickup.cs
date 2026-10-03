using System;
using Application.Economy;
using UnityEngine;

namespace Game.Economy
{
    [RequireComponent(typeof(Collider))]
    public class SalvagePickup : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private EconomyComponentType _componentType = EconomyComponentType.Scrap;
        [SerializeField] private int _amount = 1;
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _bobFrequency = 2f;
        [SerializeField] private float _bobAmplitude = 0.2f;
        [SerializeField] private float _magnetRadius = 3.5f;
        [SerializeField] private float _magnetSpeed = 12f;

        private Vector3 _basePosition;
        private float _spawnTime;
        private Transform _targetPlayer;
        private bool _isCollected;
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;
        private MaterialPropertyBlock _propBlock;

        public EconomyComponentType ComponentType => _componentType;
        public int Amount => _amount;
        public bool IsCollected => _isCollected;

        public event Action<SalvagePickup> OnCollected;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            _meshFilter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();

            EnsureVisualComponents();
        }

        public void Initialize(EconomyComponentType type, int amount, Vector3 spawnPosition)
        {
            _componentType = type;
            _amount = Math.Max(1, amount);
            _basePosition = spawnPosition;
            transform.position = spawnPosition;
            _spawnTime = Time.time;
            _targetPlayer = null;
            _isCollected = false;

            ApplyVisuals();
            gameObject.SetActive(true);
        }

        public void SetTargetPlayer(Transform player)
        {
            _targetPlayer = player;
        }

        private void Update()
        {
            if (_isCollected) return;

            // Bobbing & Rotation
            float elapsed = Time.time - _spawnTime;
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            // Magnet towards player
            if (_targetPlayer != null)
            {
                float dist = Vector3.Distance(transform.position, _targetPlayer.position);
                if (dist <= _magnetRadius)
                {
                    transform.position = Vector3.MoveTowards(transform.position, _targetPlayer.position, _magnetSpeed * Time.deltaTime);
                    if (dist <= 0.6f)
                    {
                        Collect();
                        return;
                    }
                }
                else
                {
                    Vector3 bobOffset = new Vector3(0f, Mathf.Sin(elapsed * _bobFrequency) * _bobAmplitude, 0f);
                    transform.position = _basePosition + bobOffset;
                }
            }
            else
            {
                Vector3 bobOffset = new Vector3(0f, Mathf.Sin(elapsed * _bobFrequency) * _bobAmplitude, 0f);
                transform.position = _basePosition + bobOffset;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null || other.CompareTag("Player"))
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;
            OnCollected?.Invoke(this);
        }

        public void AddAmount(int additional)
        {
            if (additional <= 0) return;
            long total = (long)_amount + additional;
            _amount = total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public void ResetForPool()
        {
            _isCollected = true;
            _targetPlayer = null;
            gameObject.SetActive(false);
        }

        private Material _customMaterial;

        private void EnsureVisualComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
                if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
                if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (_renderer != null && _customMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    _customMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                    _renderer.material = _customMaterial;
                }
            }
        }

        private void ApplyVisuals()
        {
            EnsureVisualComponents();

            // Set shape and color according to geometric component style
            Color color;
            Vector3 scale;

            switch (_componentType)
            {
                case EconomyComponentType.Alloy:
                    color = new Color(0.25f, 0.85f, 1f); // Metallic Cyan
                    scale = new Vector3(0.4f, 0.5f, 0.4f);
                    break;

                case EconomyComponentType.Core:
                    color = new Color(0.9f, 0.25f, 1f); // Glowing Purple
                    scale = new Vector3(0.5f, 0.5f, 0.5f);
                    break;

                case EconomyComponentType.Scrap:
                default:
                    color = new Color(0.95f, 0.6f, 0.15f); // Copper / Bronze
                    scale = new Vector3(0.35f, 0.35f, 0.35f);
                    break;
            }

            transform.localScale = scale;

            if (_customMaterial != null)
            {
                _customMaterial.color = color;
                if (_customMaterial.HasProperty("_BaseColor"))
                {
                    _customMaterial.SetColor("_BaseColor", color);
                }
            }

            if (_renderer != null)
            {
                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", color);
                _propBlock.SetColor("_BaseColor", color);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void OnDestroy()
        {
            if (_customMaterial != null)
            {
                SafeDestroy(_customMaterial);
                _customMaterial = null;
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }
    }
}
