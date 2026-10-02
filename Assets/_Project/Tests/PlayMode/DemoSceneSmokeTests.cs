using System.Collections;
using Cieplutki.MiniTopDownShooter.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cieplutki.MiniTopDownShooter.PlayModeTests
{
    public class DemoSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SampleScene_LoadsLegacyLearningPath()
        {
            yield return LoadScene("SampleScene");

            Assert.IsNotNull(
                Object.FindFirstObjectByType<PlayerController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<GameStateController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<EnemySpawner>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<WaveController>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<Gun>());
        }

        [UnityTest]
        public IEnumerator ArenaShowcase_UsesAuthoredFrameworkData()
        {
            yield return LoadScene("ArenaShowcase");

            WaveController wave =
                Object.FindFirstObjectByType<WaveController>();

            Assert.IsNotNull(wave);
            Assert.IsNotNull(
                wave.WaveSet,
                "Arena showcase should use a reusable WaveSet.");

            SpawnZone[] zones =
                Object.FindObjectsByType<SpawnZone>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.GreaterOrEqual(
                zones.Length,
                3,
                "Arena showcase should demonstrate spawn zones.");

            Gun[] guns =
                Object.FindObjectsByType<Gun>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            bool hasDefinition = false;

            for (int i = 0; i < guns.Length; i++)
            {
                hasDefinition |= guns[i].Definition != null;
            }

            Assert.IsTrue(
                hasDefinition,
                "Arena showcase should demonstrate WeaponDefinition.");
        }

        [UnityTest]
        public IEnumerator MobileDemo_BuildsTouchInputPath()
        {
            yield return LoadScene("MobileDemo");
            yield return null;

            Assert.IsNotNull(
                Object.FindFirstObjectByType<MobileDemoControlsBootstrap>());

            Assert.IsNotNull(
                Object.FindFirstObjectByType<MobileInputState>());

            Assert.IsNotNull(
                GameObject.Find("MobileDemoControls"));
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Single);

            Assert.IsNotNull(
                operation,
                $"Scene '{sceneName}' is not in build settings.");

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}
