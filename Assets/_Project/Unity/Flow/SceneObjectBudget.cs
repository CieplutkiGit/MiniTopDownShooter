using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Game.Flow
{
    /// <summary>
    /// T11: Runtime object budget enforcer.
    /// Tracks active counts for enemies, projectiles, and effects independently
    /// of pool retained sizes. Defers enemy spawns when at capacity rather than
    /// dropping them; cosmetic effects may be skipped when saturated.
    /// </summary>
    public class SceneObjectBudget : MonoBehaviour
    {
        [Header("Concurrent Caps")]
        [Tooltip("Maximum simultaneously active enemies (including boss). Plan target: 20.")]
        [SerializeField] private int _maxActiveEnemies = 20;

        [Tooltip("Maximum simultaneously active player projectiles.")]
        [SerializeField] private int _maxActiveProjectiles = 80;

        [Tooltip("Maximum simultaneously active effect instances.")]
        [SerializeField] private int _maxActiveEffects = 60;

        private int _activeEnemies;
        private int _activeProjectiles;
        private int _activeEffects;

        public static SceneObjectBudget Instance { get; private set; }
        private static readonly Dictionary<int, SceneObjectBudget> BudgetsByScene = new Dictionary<int, SceneObjectBudget>();

        public static SceneObjectBudget FindForScene(Scene scene)
        {
            return BudgetsByScene.TryGetValue(scene.handle, out SceneObjectBudget budget) && budget != null ? budget : null;
        }

        public static SceneObjectBudget EnsureForScene(Scene scene, GameObject owner)
        {
            SceneObjectBudget budget = FindForScene(scene);
            if (budget != null) return budget;
            SceneObjectBudget[] existing = FindObjectsByType<SceneObjectBudget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].gameObject.scene == scene)
                {
                    BudgetsByScene[scene.handle] = existing[i];
                    return existing[i];
                }
            }
            return owner.AddComponent<SceneObjectBudget>();
        }

        private void Awake()
        {
            Instance = this;
            BudgetsByScene[gameObject.scene.handle] = this;
        }

        private void OnDestroy()
        {
            int sceneHandle = gameObject.scene.handle;
            if (BudgetsByScene.TryGetValue(sceneHandle, out SceneObjectBudget registered) && registered == this)
                BudgetsByScene.Remove(sceneHandle);
            if (Instance == this)
                Instance = null;
        }

        // ── Enemy slots ───────────────────────────────────────────────────

        public bool TryReserveEnemy()
        {
            if (_activeEnemies >= _maxActiveEnemies) return false;
            _activeEnemies++;
            return true;
        }

        public void ReleaseEnemy()
        {
            _activeEnemies = Mathf.Max(0, _activeEnemies - 1);
        }

        public bool HasEnemyCapacity => _activeEnemies < _maxActiveEnemies;
        public int ActiveEnemies => _activeEnemies;
        public int MaxActiveEnemies => _maxActiveEnemies;

        // ── Projectile slots ──────────────────────────────────────────────

        public bool TryReserveProjectile()
        {
            if (_activeProjectiles >= _maxActiveProjectiles) return false;
            _activeProjectiles++;
            return true;
        }

        public void ReleaseProjectile()
        {
            _activeProjectiles = Mathf.Max(0, _activeProjectiles - 1);
        }

        public bool HasProjectileCapacity => _activeProjectiles < _maxActiveProjectiles;

        // ── Effect slots (cosmetic — may skip, never block gameplay) ──────

        public bool TryReserveEffect()
        {
            if (_activeEffects >= _maxActiveEffects) return false;
            _activeEffects++;
            return true;
        }

        public void ReleaseEffect()
        {
            _activeEffects = Mathf.Max(0, _activeEffects - 1);
        }

        public bool HasEffectCapacity => _activeEffects < _maxActiveEffects;

        // ── Reset ─────────────────────────────────────────────────────────

        public void ResetAll()
        {
            _activeEnemies = 0;
            _activeProjectiles = 0;
            _activeEffects = 0;
        }
    }
}
