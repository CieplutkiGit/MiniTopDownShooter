using System.Reflection;
using Game;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace Tests.UIUX
{
    [TestFixture]
    public class CameraFramingAndAudioTests
    {
        private GameObject _holder;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("CameraFramingAndAudioTestHolder");
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void CalculateModelBounds_Encapsulates_All_Child_Renderers()
        {
            var rigGo = new GameObject("Turntable");
            rigGo.transform.SetParent(_holder.transform);

            // Child 1: Barrel (forward offset at z = 0.5)
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(rigGo.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            barrel.transform.localScale = new Vector3(0.1f, 0.1f, 0.8f);

            // Child 2: Stock (rear offset at z = -0.4)
            var stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(rigGo.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.1f, -0.4f);
            stock.transform.localScale = new Vector3(0.15f, 0.2f, 0.4f);

            var controller = rigGo.AddComponent<WeaponPinchRotateController>();
            controller.TargetTransform = rigGo.transform;

            Bounds bounds = controller.CalculateModelBounds();

            // Total z should extend from stock back (z ~ -0.6) to barrel tip (z ~ 0.9)
            Assert.LessOrEqual(bounds.min.z, -0.5f, "Calculated bounds min.z must encapsulate the rear stock.");
            Assert.GreaterOrEqual(bounds.max.z, 0.8f, "Calculated bounds max.z must encapsulate the front barrel.");
            Assert.Greater(bounds.size.z, 1.3f, "Composite weapon bounds extent must reflect all part renderers.");
        }

        [Test]
        public void FrameModelBounds_Expands_FramingDistance_For_Exploded_Model()
        {
            var camGo = new GameObject("WeaponCamera", typeof(Camera));
            camGo.transform.SetParent(_holder.transform);
            var cam = camGo.GetComponent<Camera>();

            var rigGo = new GameObject("Rig");
            rigGo.transform.SetParent(_holder.transform);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(rigGo.transform, false);
            cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var controller = rigGo.AddComponent<WeaponPinchRotateController>();
            controller.TargetTransform = rigGo.transform;
            controller.InspectionCamera = cam;

            // Frame assembled
            controller.FrameModelBounds(false);
            float assembledDist = controller.FramedDistance;

            // Frame exploded (with larger padding)
            controller.FrameModelBounds(true);
            float explodedDist = controller.FramedDistance;

            Assert.Greater(explodedDist, assembledDist,
                $"Framed distance for exploded state ({explodedDist:F2}) must be greater than assembled state ({assembledDist:F2}) to fit separated parts.");

            Assert.Greater(cam.transform.position.z, -10f);
            Assert.Less(cam.transform.position.z, -0.5f);
        }

        [Test]
        public void CameraFollow_Maintains_Scene_Local_AudioListener_Ownership()
        {
            var camGo1 = new GameObject("GameplayCamera1", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            camGo1.transform.SetParent(_holder.transform);

            var camGo2 = new GameObject("GameplayCamera2", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            camGo2.transform.SetParent(_holder.transform);
            camGo2.SetActive(false);

            var listener1 = camGo1.GetComponent<AudioListener>();
            var listener2 = camGo2.GetComponent<AudioListener>();

            // EditMode does not run MonoBehaviour lifecycle automatically. Exercise the real
            // camera ownership callbacks explicitly, as Unity does when scenes enter runtime.
            InvokeLifecycle(camGo1.GetComponent<CameraFollow>(), "Awake");
            InvokeLifecycle(camGo1.GetComponent<CameraFollow>(), "OnEnable");

            // Initially cam1 active -> listener1 enabled
            Assert.IsTrue(listener1.enabled);

            // Activate cam2 (transition to secondary camera)
            camGo2.SetActive(true);
            InvokeLifecycle(camGo2.GetComponent<CameraFollow>(), "Awake");
            InvokeLifecycle(camGo2.GetComponent<CameraFollow>(), "OnEnable");

            // cam2 should own the active listener, and listener1 should yield
            Assert.IsTrue(listener2.enabled, "Newly active camera must hold active AudioListener.");
            Assert.IsFalse(listener1.enabled, "Previous camera's AudioListener must yield to avoid competing listeners.");
        }

        private static void InvokeLifecycle(CameraFollow component, string methodName)
        {
            MethodInfo method = typeof(CameraFollow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected Unity lifecycle method CameraFollow.{methodName} to exist.");
            method.Invoke(component, null);
        }
    }
}
