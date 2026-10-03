using NUnit.Framework;
using UnityEngine;
using Game.Combat;

namespace MiniTopDownShooter.Tests.CombatDestruction
{
    [TestFixture]
    public class BudgetAndCleanupTests
    {
        private GameObject _surfaceGo;

        [SetUp]
        public void SetUp()
        {
            _surfaceGo = new GameObject("TestSurface");
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            if (_surfaceGo != null)
            {
                Object.DestroyImmediate(_surfaceGo);
            }
            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();
        }

        [Test]
        public void PerSurfaceMarks_EnforcesLimitViaFIFORecycling()
        {
            int maxPerSurface = CombatImpactPool.Instance.MaxMarksPerSurface;

            // Spawn up to maxPerSurface
            for (int i = 0; i < maxPerSurface; i++)
            {
                CombatImpactPool.SpawnMark(Vector3.up * i, Vector3.up, Vector3.forward, _surfaceGo.transform);
            }
            Assert.AreEqual(maxPerSurface, CombatImpactPool.Instance.ActiveMarkCount);

            // Spawn 3 more on the same surface -> should recycle oldest 3
            CombatImpactPool.SpawnMark(Vector3.one, Vector3.up, Vector3.forward, _surfaceGo.transform);
            CombatImpactPool.SpawnMark(Vector3.one * 2, Vector3.up, Vector3.forward, _surfaceGo.transform);
            CombatImpactPool.SpawnMark(Vector3.one * 3, Vector3.up, Vector3.forward, _surfaceGo.transform);

            // Active count on this surface should remain capped
            Assert.AreEqual(maxPerSurface, CombatImpactPool.Instance.ActiveMarkCount);
        }

        [Test]
        public void GlobalMarks_EnforcesHardCap()
        {
            int globalMax = CombatImpactPool.Instance.GlobalMaxMarks;

            // Spawn beyond global capacity across multiple surfaces
            for (int i = 0; i < globalMax + 20; i++)
            {
                GameObject dummy = new GameObject($"Dummy_{i}");
                CombatImpactPool.SpawnMark(Vector3.forward * i, Vector3.up, Vector3.forward, dummy.transform);
                Object.DestroyImmediate(dummy);
            }

            Assert.LessOrEqual(CombatImpactPool.Instance.ActiveMarkCount, globalMax);
        }

        [Test]
        public void DebrisPool_EnforcesActiveBudget()
        {
            int maxDebris = CombatDebrisPool.Instance.MaxActiveDebris;

            for (int i = 0; i < maxDebris + 15; i++)
            {
                CombatDebrisPool.Instance.SpawnChunk(
                    Vector3.up * i,
                    Vector3.up * 5f,
                    Vector3.zero,
                    1.5f,
                    0.2f);
            }

            Assert.LessOrEqual(CombatDebrisPool.Instance.ActiveCount, maxDebris);
        }

        [Test]
        public void ClearAll_ReleasesAllActiveObjects()
        {
            CombatImpactPool.SpawnMark(Vector3.zero, Vector3.up, Vector3.forward, _surfaceGo.transform);
            CombatDebrisPool.Instance.SpawnChunk(Vector3.zero, Vector3.up, Vector3.zero, 1f, 0.2f);

            Assert.AreEqual(1, CombatImpactPool.Instance.ActiveMarkCount);
            Assert.AreEqual(1, CombatDebrisPool.Instance.ActiveCount);

            CombatImpactPool.Instance.ClearAll();
            CombatDebrisPool.Instance.ClearAll();

            Assert.AreEqual(0, CombatImpactPool.Instance.ActiveMarkCount);
            Assert.AreEqual(0, CombatDebrisPool.Instance.ActiveCount);
        }
    }
}
