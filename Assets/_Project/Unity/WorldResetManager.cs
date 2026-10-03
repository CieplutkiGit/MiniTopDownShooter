using Application;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class WorldResetManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _spawnPoint;
        [FormerlySerializedAs("_spawner")]
        [SerializeField] private EnemySpawner _spawnerRef;
        [FormerlySerializedAs("_gameState")]
        [SerializeField] private GameStateController _gameStateRef;
        [SerializeField] private WaveController _waveRef;
        [SerializeField] private EffectPool _effectPool;
        [SerializeField] private ScoreController _scoreRef;

        private ISpawner _spawner;
        private IGameStateController _gameState;

        private class AuthoredAmmoData
        {
            public AmmoPickup Component;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
            public Transform Parent;
            public int Amount;
            public bool InitialActiveSelf;
        }

        private class AuthoredWeaponData
        {
            public WeaponPickup Component;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
            public Transform Parent;
            public Gun WeaponPrefab;
            public bool EquipImmediately;
            public bool InitialActiveSelf;
        }

        private readonly System.Collections.Generic.List<AuthoredAmmoData> _authoredAmmo = new System.Collections.Generic.List<AuthoredAmmoData>();
        private readonly System.Collections.Generic.List<AuthoredWeaponData> _authoredWeapons = new System.Collections.Generic.List<AuthoredWeaponData>();
        private bool _hasRegisteredAuthoredPickups;

        public void RegisterAuthoredPickups()
        {
            if (_hasRegisteredAuthoredPickups)
            {
                return;
            }

            _authoredAmmo.Clear();
            _authoredWeapons.Clear();

            AmmoPickup[] ammo = FindObjectsByType<AmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < ammo.Length; i++)
            {
                if (ammo[i] == null || ammo[i].IsRuntimeDrop) continue;
                _authoredAmmo.Add(new AuthoredAmmoData
                {
                    Component = ammo[i],
                    Position = ammo[i].transform.position,
                    Rotation = ammo[i].transform.rotation,
                    LocalScale = ammo[i].transform.localScale,
                    Parent = ammo[i].transform.parent,
                    Amount = ammo[i].Amount,
                    InitialActiveSelf = ammo[i].gameObject.activeSelf
                });
            }

            WeaponPickup[] weapons = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null || weapons[i].IsRuntimeDrop) continue;
                _authoredWeapons.Add(new AuthoredWeaponData
                {
                    Component = weapons[i],
                    Position = weapons[i].transform.position,
                    Rotation = weapons[i].transform.rotation,
                    LocalScale = weapons[i].transform.localScale,
                    Parent = weapons[i].transform.parent,
                    WeaponPrefab = weapons[i].WeaponPrefab,
                    EquipImmediately = weapons[i].EquipImmediately,
                    InitialActiveSelf = weapons[i].gameObject.activeSelf
                });
            }

            _hasRegisteredAuthoredPickups = true;
        }

        public void Initialize(PlayerController player, EnemySpawner spawner, GameStateController gameState, WaveController waves, EffectPool effectPool, ScoreController score, Transform spawnPoint = null)
        {
            RegisterAuthoredPickups();
            _player = player;
            _spawnerRef = spawner;
            _gameStateRef = gameState;
            _waveRef = waves;
            _effectPool = effectPool;
            _scoreRef = score;
            if (spawnPoint != null)
            {
                _spawnPoint = spawnPoint;
            }
            _spawner = spawner;
            _gameState = gameState;
        }

        private void Awake()
        {
            RegisterAuthoredPickups();

            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            if (_spawnerRef == null)
            {
                _spawnerRef = FindFirstObjectByType<EnemySpawner>();
            }

            if (_gameStateRef == null)
            {
                _gameStateRef = FindFirstObjectByType<GameStateController>();
            }

            if (_waveRef == null)
            {
                _waveRef = FindFirstObjectByType<WaveController>();
            }

            if (_effectPool == null)
            {
                _effectPool = FindFirstObjectByType<EffectPool>();
            }

            if (_scoreRef == null)
            {
                _scoreRef = FindFirstObjectByType<ScoreController>();
            }

            _spawner = _spawnerRef;
            _gameState = _gameStateRef;
        }

        public void ResetWorld() => Restart();

        public void Restart()
        {
            if (_gameState == null)
            {
                return;
            }

            GameState state = _gameState.CurrentState;
            if (state != GameState.Menu && state != GameState.GameOver && state != GameState.Victory)
            {
                return;
            }

            // 1. Clear active projectiles
            ClearActiveProjectiles();

            // 2. Clear active enemies and reset attack cooldowns
            if (_spawner != null)
            {
                _spawner.ClearAllAlive();
            }

            // 3. Clear active effects and debris chunks
            if (_effectPool != null)
            {
                _effectPool.ClearAllActive();
            }
            ClearActiveDebris();

            // 4. Restore authored pickups and clear runtime drops
            RestorePickups();

            // 5. Restore timers and wave progress
            if (_waveRef != null)
            {
                _waveRef.ResetWaves();
            }

            // 6. Reset score tracker
            if (_scoreRef != null)
            {
                _scoreRef.ResetScore();
            }

            // 7. Restore player health, ammo, starting weapons, rotation, and input BEFORE entering Playing
            if (_player != null)
            {
                var loadout = _player.GetComponent<WeaponLoadout>();
                if (loadout != null)
                {
                    loadout.ApplySavedBuildsToAll();
                }

                if (_spawnPoint != null)
                {
                    _player.ResetToSpawn(_spawnPoint.position, _spawnPoint.rotation);
                }
                else
                {
                    _player.ResetToSpawn(_player.transform.position, _player.transform.rotation);
                }
            }

            // 8. Restore camera and screen shake state AFTER player reset to spawn
            ResetCameraState();

            // 9. Now safely enter Playing
            _gameState.StartGame();
        }

        private void ClearActiveProjectiles()
        {
            Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null)
                {
                    projectiles[i].ForceReturnToPool();
                }
            }
        }

        private void ClearActiveDebris()
        {
            DebrisChunk[] debris = FindObjectsByType<DebrisChunk>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < debris.Length; i++)
            {
                if (debris[i] != null)
                {
                    debris[i].gameObject.SetActive(false);
                }
            }
        }

        private void RestorePickups()
        {
            if (!_hasRegisteredAuthoredPickups)
            {
                RegisterAuthoredPickups();
            }

            // Remove runtime drops (any pickup currently in scene that was not registered as authored, or marked as runtime drop)
            System.Collections.Generic.HashSet<AmmoPickup> authoredAmmoSet = new System.Collections.Generic.HashSet<AmmoPickup>();
            for (int i = 0; i < _authoredAmmo.Count; i++)
            {
                if (_authoredAmmo[i].Component != null)
                {
                    authoredAmmoSet.Add(_authoredAmmo[i].Component);
                }
            }

            AmmoPickup[] allAmmo = FindObjectsByType<AmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allAmmo.Length; i++)
            {
                if (allAmmo[i] != null && (!authoredAmmoSet.Contains(allAmmo[i]) || allAmmo[i].IsRuntimeDrop))
                {
                    SafeDestroy(allAmmo[i].gameObject);
                }
            }

            System.Collections.Generic.HashSet<WeaponPickup> authoredWeaponSet = new System.Collections.Generic.HashSet<WeaponPickup>();
            for (int i = 0; i < _authoredWeapons.Count; i++)
            {
                if (_authoredWeapons[i].Component != null)
                {
                    authoredWeaponSet.Add(_authoredWeapons[i].Component);
                }
            }

            WeaponPickup[] allWeapons = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allWeapons.Length; i++)
            {
                if (allWeapons[i] != null && (!authoredWeaponSet.Contains(allWeapons[i]) || allWeapons[i].IsRuntimeDrop))
                {
                    SafeDestroy(allWeapons[i].gameObject);
                }
            }

            // Restore each authored pickup exactly once
            for (int i = 0; i < _authoredAmmo.Count; i++)
            {
                AuthoredAmmoData data = _authoredAmmo[i];
                if (data.Component != null)
                {
                    data.Component.transform.SetParent(data.Parent);
                    data.Component.transform.position = data.Position;
                    data.Component.transform.rotation = data.Rotation;
                    data.Component.transform.localScale = data.LocalScale;
                    data.Component.Amount = data.Amount;
                    data.Component.IsRuntimeDrop = false;
                    data.Component.gameObject.SetActive(data.InitialActiveSelf);
                }
            }

            for (int i = 0; i < _authoredWeapons.Count; i++)
            {
                AuthoredWeaponData data = _authoredWeapons[i];
                if (data.Component != null)
                {
                    data.Component.transform.SetParent(data.Parent);
                    data.Component.transform.position = data.Position;
                    data.Component.transform.rotation = data.Rotation;
                    data.Component.transform.localScale = data.LocalScale;
                    data.Component.WeaponPrefab = data.WeaponPrefab;
                    data.Component.EquipImmediately = data.EquipImmediately;
                    data.Component.IsRuntimeDrop = false;
                    data.Component.gameObject.SetActive(data.InitialActiveSelf);
                }
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void ResetCameraState()
        {
            CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.ResetToTarget();
            }

            ScreenShake shake = FindFirstObjectByType<ScreenShake>();
            if (shake != null)
            {
                shake.StopShake();
            }
        }
    }
}
