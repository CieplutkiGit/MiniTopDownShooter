using Core;
using UnityEngine;

namespace Game
{
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private HealthComponent _healthRef;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _duration = 0.08f;
        [SerializeField] private string _colorProperty = "_BaseColor";

        private IHealthReadable _health;
        private MaterialPropertyBlock _block;
        private int _propertyId;
        private float _flashTimer;
        private bool _isFlashing;
        private int _lastHealth;

        private void Awake()
        {
            if (_healthRef == null)
            {
                _healthRef = GetComponent<HealthComponent>();
                if (_healthRef == null)
                {
                    _healthRef = GetComponentInParent<HealthComponent>();
                }
            }

            _health = _healthRef;
            _block = new MaterialPropertyBlock();
            _propertyId = Shader.PropertyToID(_colorProperty);
        }

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged += HandleHealthChanged;
            _lastHealth = _health.Current;
            ClearColor();
            _isFlashing = false;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
            }

            _isFlashing = false;
            ClearColor();
        }

        private void Update()
        {
            if (!_isFlashing)
            {
                return;
            }

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
                ClearColor();
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (current < _lastHealth)
            {
                StartFlash();
            }

            _lastHealth = current;
        }

        private void StartFlash()
        {
            _isFlashing = true;
            _flashTimer = _duration;
            ApplyColor(_flashColor);
        }

        [SerializeField] private Renderer[] _additionalRenderers;

        private void ApplyColor(Color color)
        {
            _block.SetColor(_propertyId, color);

            if (_renderer != null)
            {
                _renderer.SetPropertyBlock(_block);
            }

            if (_additionalRenderers != null)
            {
                for (int i = 0; i < _additionalRenderers.Length; i++)
                {
                    if (_additionalRenderers[i] != null)
                    {
                        _additionalRenderers[i].SetPropertyBlock(_block);
                    }
                }
            }
        }

        private void ClearColor()
        {
            _block.Clear();

            if (_renderer != null)
            {
                _renderer.SetPropertyBlock(_block);
            }

            if (_additionalRenderers != null)
            {
                for (int i = 0; i < _additionalRenderers.Length; i++)
                {
                    if (_additionalRenderers[i] != null)
                    {
                        _additionalRenderers[i].SetPropertyBlock(_block);
                    }
                }
            }
        }
    }
}
