using UnityEngine;

namespace Game
{
    /// <summary>
    /// T07: Practice firing-range target. Restores health on reset.
    /// Isolated from mission score/profile accounting — damage is tracked
    /// only locally for visual feedback. Never contributes to ScoreController.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class RangeTarget : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _fullColor = Color.green;
        [SerializeField] private Color _emptyColor = Color.red;

        private HealthComponent _health;
        private DamageAffiliation _affiliation;
        private MaterialPropertyBlock _propertyBlock;
        private float _resetAt;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();

            // Mark as Neutral so it's ignored by combat affiliations and ScoreController
            _affiliation = GetComponent<DamageAffiliation>();
            if (_affiliation == null)
                _affiliation = gameObject.AddComponent<DamageAffiliation>();
            _affiliation.Configure(CombatTeam.Neutral, allowFriendlyFire: false, ignoreFriendlyCollisions: false);

            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            _health.OnHealthChanged += UpdateVisual;
            _propertyBlock = new MaterialPropertyBlock();
            ResetTarget();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChanged -= UpdateVisual;
        }

        /// <summary>Restore target to full health.</summary>
        public void ResetTarget()
        {
            _resetAt = 0f;
            if (_health != null)
            {
                _health.SetMaxHealth(_maxHealth);
                _health.ResetHealth();
            }

            UpdateVisual(_maxHealth, _maxHealth);
        }

        private void UpdateVisual(int current, int max)
        {
            if (current <= 0) _resetAt = Time.unscaledTime + 1.5f;
            if (_renderer == null) return;
            float t = max > 0 ? (float)current / max : 0f;
            Color c = Color.Lerp(_emptyColor, _fullColor, t);
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, c);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void Update()
        {
            if (_resetAt > 0f && Time.unscaledTime >= _resetAt) ResetTarget();
        }
    }
}
