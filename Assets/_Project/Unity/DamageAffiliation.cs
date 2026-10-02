using Core;
using UnityEngine;

namespace Game
{
    public enum CombatTeam
    {
        Neutral,
        Player,
        Enemy
    }

    [DisallowMultipleComponent]
    public class DamageAffiliation : MonoBehaviour
    {
        [SerializeField] private CombatTeam _team = CombatTeam.Neutral;
        [SerializeField] private bool _allowFriendlyFire;
        [SerializeField] private bool _ignoreFriendlyCollisions = true;

        public CombatTeam Team => _team;
        public bool AllowFriendlyFire => _allowFriendlyFire;
        public bool IgnoreFriendlyCollisions => _ignoreFriendlyCollisions;

        public bool CanDamage(DamageAffiliation target)
        {
            if (target == null)
            {
                return true;
            }

            if (_team == CombatTeam.Neutral || target._team == CombatTeam.Neutral)
            {
                return true;
            }

            return _team != target._team || _allowFriendlyFire;
        }

        public bool ShouldIgnoreFriendlyCollision(DamageAffiliation target)
        {
            return target != null &&
                   _team != CombatTeam.Neutral &&
                   _team == target._team &&
                   !_allowFriendlyFire &&
                   _ignoreFriendlyCollisions;
        }

        public static DamageAffiliation Find(Component component)
        {
            return component != null
                ? component.GetComponentInParent<DamageAffiliation>()
                : null;
        }

        public static CombatTeam ResolveTeam(Component component)
        {
            DamageAffiliation affiliation = Find(component);

            if (affiliation != null && affiliation.Team != CombatTeam.Neutral)
            {
                return affiliation.Team;
            }

            if (component == null)
            {
                return CombatTeam.Neutral;
            }

            Transform root = component.transform.root;
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            if (playerLayer >= 0 && root.gameObject.layer == playerLayer)
            {
                return CombatTeam.Player;
            }

            if (enemyLayer >= 0 && root.gameObject.layer == enemyLayer)
            {
                return CombatTeam.Enemy;
            }

            return CombatTeam.Neutral;
        }

        public static bool TryGetDamageable(
            Collider collider,
            out IDamageable damageable,
            out Component owner)
        {
            if (collider != null)
            {
                MonoBehaviour[] behaviours =
                    collider.GetComponentsInParent<MonoBehaviour>(true);

                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IDamageable found)
                    {
                        damageable = found;
                        owner = behaviours[i];
                        return true;
                    }
                }
            }

            damageable = null;
            owner = null;
            return false;
        }
    }
}
