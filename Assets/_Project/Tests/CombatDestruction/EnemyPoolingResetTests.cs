using NUnit.Framework;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class EnemyPoolingResetTests
    {
        private GameObject _enemyGo;
        private HealthComponent _health;
        private EnemyDamageVisuals _visuals;

        [SetUp]
        public void SetUp()
        {
            _enemyGo = new GameObject("TestEnemyPooling");
            _health = _enemyGo.AddComponent<HealthComponent>();
            _health.SetMaxHealth(100);

            MeshRenderer mr = _enemyGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            MeshFilter mf = _enemyGo.AddComponent<MeshFilter>();
            mf.sharedMesh = CombatDebrisPool.GetOrCreateDefaultChunkMesh();

            // Create authored child pieces for the enemy
            for (int i = 0; i < 3; i++)
            {
                GameObject piece = new GameObject($"AuthoredPiece_{i + 1}");
                piece.transform.SetParent(_enemyGo.transform);
                piece.transform.localPosition = new Vector3(i * 0.2f, 0.5f, 0f);
                MeshRenderer pieceMr = piece.AddComponent<MeshRenderer>();
                pieceMr.sharedMaterial = mr.sharedMaterial;
                MeshFilter pieceMf = piece.AddComponent<MeshFilter>();
                pieceMf.sharedMesh = mf.sharedMesh;
            }

            _visuals = _enemyGo.AddComponent<EnemyDamageVisuals>();
            _visuals.InitializeVisuals();
            _visuals.EnsureSubscribed();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemyGo != null)
            {
                Object.DestroyImmediate(_enemyGo);
            }
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [Test]
        public void Respawn_RestoresAllDetachedPieces()
        {
            int initialPieces = _visuals.ActivePieceCount;

            // Damage heavily to stage 2 (HP = 40)
            _health.TakeDamage(new DamageData(60), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));
            Assert.AreEqual(2, _visuals.CurrentStage);
            Assert.Less(_visuals.ActivePieceCount, initialPieces);

            // Simulate pool respawn / reset
            _health.ResetHealth();
            _visuals.ResetToPristine();

            Assert.AreEqual(0, _visuals.CurrentStage);
            Assert.AreEqual(initialPieces, _visuals.ActivePieceCount);
            Assert.AreEqual(100, _health.CurrentHealth);
        }

        [Test]
        public void Respawn_ClearsAttachedImpactMarks()
        {
            // Spawn impact marks on enemy
            CombatImpactPool.SpawnMark(_enemyGo.transform.position, Vector3.up, Vector3.forward, _enemyGo.transform);
            CombatImpactPool.SpawnMark(_enemyGo.transform.position + Vector3.right * 0.1f, Vector3.up, Vector3.forward, _enemyGo.transform);

            Assert.AreEqual(2, CombatImpactPool.Instance.ActiveMarkCount);

            // Resetting enemy clears marks parented to it
            _visuals.ResetToPristine();

            Assert.AreEqual(0, CombatImpactPool.Instance.ActiveMarkCount);
        }
    }
}
