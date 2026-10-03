using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Core;
using Game;
using Game.Combat;

namespace Tests.CombatDestruction
{
    public class CombatDestructionTests
    {
        private readonly List<GameObject> _cleanupList = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _cleanupList.Count; i++)
            {
                if (_cleanupList[i] != null)
                {
                    Object.DestroyImmediate(_cleanupList[i]);
                }
            }
            _cleanupList.Clear();

            CombatImpactPool.Instance?.ClearAndDestroyAll();
            CombatDebrisPool.Instance?.ClearAndDestroyAll();
        }

        private GameObject CreateTracked(string name)
        {
            GameObject go = new GameObject(name);
            _cleanupList.Add(go);
            return go;
        }

        [Test]
        public void DenseSameSurfaceMarks_StayBounded_AtLeast30Impacts()
        {
            GameObject target = CreateTracked("DenseSurfaceTarget");

            // Rapid fire 35 dense impacts on the exact same surface (>= 30 required by contract)
            for (int i = 0; i < 35; i++)
            {
                CombatImpactPool.SpawnMark(Vector3.forward * (i * 0.005f), Vector3.up, Vector3.down, target.transform);
            }

            Assert.LessOrEqual(CombatImpactPool.Instance.ActiveMarkCount, CombatImpactPool.Instance.GlobalMaxMarks);
            Assert.Greater(CombatImpactPool.Instance.ActiveMarkCount, 0);
        }

        [Test]
        public void WorldOriginHit_IsValidAndSpawnsMarkAtExactOrigin()
        {
            GameObject target = CreateTracked("OriginTarget");
            target.transform.position = Vector3.zero;

            // Vector3.zero is a valid world coordinate, not a missing sentinel
            HitContext ctx = new HitContext(Vector3.zero, Vector3.up, Vector3.forward);
            Assert.IsTrue(ctx.IsValid, "HitContext must explicitly report IsValid == true");
            Assert.AreEqual(Vector3.zero, ctx.Point, "HitContext.Point must be exact world origin");

            CombatImpactMark mark = CombatImpactPool.SpawnMark(ctx.Point, ctx.Normal, ctx.Direction, target.transform);
            Assert.IsNotNull(mark, "Mark must spawn at valid world origin");
            Assert.Less(Vector3.Distance(mark.transform.position, Vector3.zero), 0.05f);
        }

        [Test]
        public void DestructibleProp_MeshTriangleCutout_ModifiesInstance_PreservesSharedMesh_RestoresTopology()
        {
            GameObject propGo = CreateTracked("CoverBlock_Test");
            propGo.AddComponent<BoxCollider>();
            MeshFilter mf = propGo.AddComponent<MeshFilter>();
            GameObject cubePrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh sharedMesh = cubePrimitive.GetComponent<MeshFilter>().sharedMesh;
            mf.sharedMesh = sharedMesh;
            Object.DestroyImmediate(cubePrimitive);
            propGo.AddComponent<MeshRenderer>();

            DestructibleProp prop = propGo.AddComponent<DestructibleProp>();
            prop.Configure(80);

            int originalPropTriangles = prop.OriginalTriangleCount;
            int sharedMeshTrianglesBefore = sharedMesh.triangles.Length / 3;
            Assert.Greater(originalPropTriangles, 0, "Original mesh must have triangles");

            // Hit 1: 30 damage (50/80 HP -> Stage 1)
            prop.TakeDamage(new DamageData(30), new HitContext(new Vector3(0f, 0.4f, 0.5f), Vector3.back, Vector3.forward));
            Assert.AreEqual(1, prop.CurrentStage);

            // Verify instance mesh triangles were physically cut out / reduced
            Assert.Less(prop.RenderedTriangleCount, originalPropTriangles,
                "Instance mesh triangle count must decrease on damage to visibly alter original rendered geometry");

            // Verify shared asset mesh was NEVER mutated
            Assert.AreEqual(sharedMeshTrianglesBefore, sharedMesh.triangles.Length / 3,
                "Shared asset mesh must remain completely untouched");

            // Restore prop
            prop.ResetProp();

            // Verify 100% topology restoration
            Assert.AreEqual(originalPropTriangles, prop.RenderedTriangleCount,
                "Original mesh topology must be 100% restored after ResetProp");
        }

        [Test]
        public void EnemyDamageVisuals_MeshTriangleCutout_ModifiesInstance_PreservesSharedMesh_RestoresOnRespawn()
        {
            GameObject enemyGo = CreateTracked("DynamicEnemy");
            enemyGo.AddComponent<EnemyMovement>();
            enemyGo.AddComponent<EnemyAttack>();
            EnemyController enemy = enemyGo.AddComponent<EnemyController>();
            enemy.EnsureInitialized();
            HealthComponent health = enemyGo.GetComponent<HealthComponent>();

            MeshFilter mf = enemyGo.AddComponent<MeshFilter>();
            GameObject cubePrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh sharedMesh = cubePrimitive.GetComponent<MeshFilter>().sharedMesh;
            mf.sharedMesh = sharedMesh;
            Object.DestroyImmediate(cubePrimitive);
            enemyGo.AddComponent<MeshRenderer>();

            // Spawn enemy dynamically
            enemy.Spawn(Vector3.zero, null);

            EnemyDamageVisuals visuals = enemyGo.GetComponent<EnemyDamageVisuals>();
            Assert.IsNotNull(visuals, "EnemyDamageVisuals must be installed on dynamic spawn");

            int originalTriangles = visuals.OriginalTriangleCount;
            int sharedTrianglesBefore = sharedMesh.triangles.Length / 3;
            Assert.Greater(originalTriangles, 0);

            // Hit 1: 30 damage (HP 70/100, drops below 75% -> stage 1)
            health.TakeDamage(new DamageData(30), new HitContext(new Vector3(0f, 0.3f, 0.45f), Vector3.back, Vector3.forward));
            Assert.AreEqual(1, visuals.CurrentStage);
            Assert.Less(visuals.RenderedTriangleCount, originalTriangles,
                "Living enemy body geometry must physically lose triangles at hit location");

            // Hit 2: 30 damage (HP 40/100, drops below 50% -> stage 2)
            health.TakeDamage(new DamageData(30), new HitContext(new Vector3(-0.2f, 0.3f, 0.45f), Vector3.back, Vector3.forward));
            Assert.AreEqual(2, visuals.CurrentStage);

            // Shared mesh must be untouched
            Assert.AreEqual(sharedTrianglesBefore, sharedMesh.triangles.Length / 3,
                "Shared enemy asset mesh must never be mutated");

            // Respawn enemy via controller
            enemy.Spawn(Vector3.one, null);
            Assert.AreEqual(100, health.CurrentHealth, "Pooled respawn must restore full health");
            Assert.AreEqual(0, visuals.CurrentStage, "Pooled respawn must reset damage stage to 0");
            Assert.AreEqual(originalTriangles, visuals.RenderedTriangleCount,
                "Pooled respawn must 100% restore original topology and vertices");
            Assert.AreEqual(sharedTrianglesBefore, sharedMesh.triangles.Length / 3,
                "Shared enemy asset mesh must remain untouched after respawn");
            Assert.AreNotSame(sharedMesh, mf.sharedMesh, "Enemy must use an isolated instance mesh");
            Assert.AreEqual(sharedMesh.vertexCount, mf.sharedMesh.vertexCount,
                "Restored instance mesh vertices must match shared source mesh");
        }

        [Test]
        public void DestroyedParent_MarksPrunedWithoutSpinsOrErrors()
        {
            GameObject target = CreateTracked("TemporaryTarget");
            for (int i = 0; i < 5; i++)
            {
                CombatImpactPool.SpawnMark(Vector3.zero, Vector3.up, Vector3.down, target.transform);
            }

            // Target is destroyed (e.g. prop or enemy destroyed)
            Object.DestroyImmediate(target);

            // Subsequent mark spawn should safely prune dead key without errors or hanging
            GameObject newTarget = CreateTracked("NewTarget");
            for (int i = 0; i < 15; i++)
            {
                CombatImpactPool.SpawnMark(Vector3.zero, Vector3.up, Vector3.down, newTarget.transform);
            }

            Assert.Pass();
        }

        [Test]
        public void HealthComponent_LethalDamage_DoesNotFireDamagedWithContext()
        {
            GameObject go = CreateTracked("TargetHealth");
            HealthComponent hc = go.AddComponent<HealthComponent>();
            hc.SetMaxHealth(50);

            bool contextFired = false;
            hc.OnDamagedWithContext += (dmg, ctx) => contextFired = true;

            // Deal lethal damage in one hit
            hc.TakeDamage(new DamageData(60), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));

            Assert.IsFalse(contextFired, "Lethal damage must not fire OnDamagedWithContext to prevent marks on dead/recycled instances");
        }

        [Test]
        public void UnsupportedMesh_DoesNotCrash_RetainsMarksAndFeedback()
        {
            GameObject enemyGo = CreateTracked("NoMeshEnemy");
            enemyGo.AddComponent<EnemyMovement>();
            enemyGo.AddComponent<EnemyAttack>();
            EnemyController enemy = enemyGo.AddComponent<EnemyController>();
            enemy.EnsureInitialized();
            HealthComponent health = enemyGo.GetComponent<HealthComponent>();

            // MeshFilter with null mesh simulates unsupported/missing/non-readable mesh
            enemyGo.AddComponent<MeshFilter>();
            enemyGo.AddComponent<MeshRenderer>();

            // Add authored pieces
            for (int i = 0; i < 3; i++)
            {
                GameObject piece = new GameObject($"Piece_{i + 1}");
                piece.transform.SetParent(enemyGo.transform);
                piece.AddComponent<MeshRenderer>();
            }

            enemy.Spawn(Vector3.zero, null);
            EnemyDamageVisuals visuals = enemyGo.GetComponent<EnemyDamageVisuals>();

            Assert.IsFalse(visuals.IsCutoutSupported, "Unsupported mesh must report IsCutoutSupported == false");
            Assert.AreEqual(3, visuals.ActivePieceCount);

            // Damaging must NOT throw, must spawn marks, and must detach pieces across stages
            Assert.DoesNotThrow(() =>
            {
                health.TakeDamage(new DamageData(30), new HitContext(Vector3.zero, Vector3.up, Vector3.forward));
            });

            Assert.AreEqual(1, visuals.CurrentStage);
            Assert.AreEqual(2, visuals.ActivePieceCount);

            // Resetting must NOT throw and must restore pieces
            Assert.DoesNotThrow(() =>
            {
                enemy.Spawn(Vector3.one, null);
            });

            Assert.AreEqual(0, visuals.CurrentStage);
            Assert.AreEqual(3, visuals.ActivePieceCount);
        }
    }
}
