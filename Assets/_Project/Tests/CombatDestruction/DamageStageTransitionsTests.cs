using NUnit.Framework;
using UnityEngine;
using Core;
using Game;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class DamageStageTransitionsTests
    {
        private GameObject _enemyGo;
        private HealthComponent _health;
        private EnemyDamageVisuals _visuals;

        [SetUp]
        public void SetUp()
        {
            _enemyGo = new GameObject("TestEnemy");
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
        public void InitialState_IsStageZero_WithAllPiecesActive()
        {
            Assert.AreEqual(0, _visuals.CurrentStage);
            Assert.Greater(_visuals.ActivePieceCount, 0);
        }

        [Test]
        public void DamageBelow75Percent_TransitionsToStageOne()
        {
            int initialCount = _visuals.ActivePieceCount;

            // Damage to 70% HP (takes 30 damage)
            _health.TakeDamage(new DamageData(30), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));

            Assert.AreEqual(1, _visuals.CurrentStage);
            Assert.AreEqual(initialCount - 1, _visuals.ActivePieceCount);
        }

        [Test]
        public void DamageBelow50Percent_TransitionsToStageTwo()
        {
            int initialCount = _visuals.ActivePieceCount;

            // Damage to 45% HP (takes 55 damage)
            _health.TakeDamage(new DamageData(55), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));

            Assert.AreEqual(2, _visuals.CurrentStage);
            Assert.AreEqual(initialCount - 2, _visuals.ActivePieceCount);
        }

        [Test]
        public void DamageBelow25Percent_TransitionsToStageThree()
        {
            int initialCount = _visuals.ActivePieceCount;

            // Damage to 20% HP (takes 80 damage)
            _health.TakeDamage(new DamageData(80), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));

            Assert.AreEqual(3, _visuals.CurrentStage);
            Assert.AreEqual(initialCount - 3, _visuals.ActivePieceCount);
        }

        [Test]
        public void PropDamage_TransitionsAcrossStages()
        {
            GameObject propGo = new GameObject("TestProp");
            propGo.AddComponent<BoxCollider>();
            MeshRenderer mr = propGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            DestructibleProp prop = propGo.AddComponent<DestructibleProp>();
            prop.InitializeProp();
            prop.Configure(100, debrisStage: 2, debrisDeath: 6);

            Assert.AreEqual(0, prop.CurrentStage);

            // Take 40 damage (60% HP -> Stage 1)
            prop.TakeDamage(new DamageData(40), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));
            Assert.AreEqual(1, prop.CurrentStage);

            // Take 40 more damage (20% HP -> Stage 2)
            prop.TakeDamage(new DamageData(40), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));
            Assert.AreEqual(2, prop.CurrentStage);

            Object.DestroyImmediate(propGo);
        }
    }
}
