using Core;
using UnityEngine;

namespace Game
{
    public class EnemyArmor : MonoBehaviour, IDamageModifier
    {
        [Range(0f, 1f)]
        [SerializeField] private float _damageMultiplier = 0.6f;

        [Min(0)]
        [SerializeField] private int _flatReduction;

        [Min(0)]
        [SerializeField] private int _minimumDamage = 1;

        public DamageData ModifyDamage(DamageData damage)
        {
            int reduced = Mathf.RoundToInt(damage.Damage * _damageMultiplier) - _flatReduction;
            int resolved = damage.Damage > 0
                ? Mathf.Max(_minimumDamage, reduced)
                : reduced;

            return new DamageData(Mathf.Max(0, resolved));
        }

        private void OnValidate()
        {
            _damageMultiplier = Mathf.Clamp01(_damageMultiplier);
            _flatReduction = Mathf.Max(0, _flatReduction);
            _minimumDamage = Mathf.Max(0, _minimumDamage);
        }
    }
}
