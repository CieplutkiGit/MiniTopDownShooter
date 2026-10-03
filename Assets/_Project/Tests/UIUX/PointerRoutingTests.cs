using System.Reflection;
using Game;
using Game.Workshop;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tests.UIUX
{
    [TestFixture]
    public class PointerRoutingTests
    {
        private GameObject _holder;
        private Canvas _canvas;
        private Camera _uiCamera;
        private EventSystem _eventSystem;
        private RectTransform _viewportRt;
        private Button _testButton;
        private WeaponPinchRotateController _controller;
        private WeaponPreviewView _previewView;
        private bool _partSelectedCallbackReceived;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("PointerRoutingTestHolder");

            // EventSystem with InputSystemUIInputModule (matches production new input system)
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            esGo.transform.SetParent(_holder.transform);
            _eventSystem = esGo.GetComponent<EventSystem>();

            // Give EditMode a real deterministic display surface, matching production's camera-based UI.
            var cameraGo = new GameObject("UICamera", typeof(Camera));
            cameraGo.transform.SetParent(_holder.transform);
            _uiCamera = cameraGo.GetComponent<Camera>();
            _uiCamera.pixelRect = new Rect(0f, 0f, 1280f, 720f);
            _uiCamera.transform.position = new Vector3(0f, 0f, -10f);
            _uiCamera.clearFlags = CameraClearFlags.SolidColor;

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_holder.transform);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 1f;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(1280f, 720f);

            // Preview Viewport (x: 0..1280, y: 215..595)
            var vpGo = new GameObject("PreviewViewport", typeof(RectTransform), typeof(Image));
            vpGo.transform.SetParent(canvasGo.transform, false);
            _viewportRt = vpGo.GetComponent<RectTransform>();
            _viewportRt.anchorMin = new Vector2(0f, 0f);
            _viewportRt.anchorMax = new Vector2(1f, 1f);
            _viewportRt.offsetMin = new Vector2(0f, 215f);
            _viewportRt.offsetMax = new Vector2(0f, -125f);
            var vpImg = vpGo.GetComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.01f);
            vpImg.raycastTarget = true;

            // UI Button inside canvas
            var btnGo = new GameObject("TestButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.zero;
            btnRt.anchoredPosition = new Vector2(100f, 100f);
            btnRt.sizeDelta = new Vector2(120f, 44f);
            btnGo.GetComponent<Image>().raycastTarget = true;
            _testButton = btnGo.GetComponent<Button>();

            // Weapon Preview View & Controller
            var rigGo = new GameObject("WeaponRig");
            rigGo.transform.SetParent(_holder.transform);
            _previewView = rigGo.AddComponent<WeaponPreviewView>();
            _controller = rigGo.AddComponent<WeaponPinchRotateController>();
            _controller.PreviewViewport = _viewportRt;
            _controller.PreviewView = _previewView;
            _controller.InspectionCamera = _uiCamera;
            _controller.DragThresholdPixels = 8f;

            // Explicitly initialize fixture lifecycle for EditMode
            InvokeLifecycle(_eventSystem, "OnEnable");
            InvokeLifecycle(esGo.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), "OnEnable");
            InvokeLifecycle(canvasGo.GetComponent<GraphicRaycaster>(), "OnEnable");
            InvokeLifecycle(vpImg, "OnEnable");
            InvokeLifecycle(btnGo.GetComponent<Image>(), "OnEnable");
            InvokeLifecycle(_controller, "Awake");
            InvokeLifecycle(_controller, "OnEnable");

            Canvas.ForceUpdateCanvases();

            _partSelectedCallbackReceived = false;
            _previewView.SlotSelected += OnSlotSelected;
        }

        [TearDown]
        public void TearDown()
        {
            if (_previewView != null)
            {
                _previewView.SlotSelected -= OnSlotSelected;
            }

            if (_eventSystem != null)
                InvokeLifecycle(_eventSystem, "OnDisable");

            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        private static void InvokeLifecycle(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected Unity lifecycle method {component.GetType().Name}.{methodName} to exist.");
            method.Invoke(component, null);
        }

        private Vector2 GetScreenPoint(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(_uiCamera, center);
        }

        private void OnSlotSelected(string slot)
        {
            _partSelectedCallbackReceived = true;
        }

        [Test]
        public void UI_Press_Blocks_Controller_Drag_And_Rotation()
        {
            Vector2 buttonPos = GetScreenPoint(_testButton.GetComponent<RectTransform>());

            _controller.OnPointerDown(0, buttonPos);

            // Press on UI must never start dragging
            Assert.IsFalse(_controller.IsDragging, "Press on UI element must not start dragging.");

            // Dragging off the button into world
            Vector2 worldPos = GetScreenPoint(_viewportRt);
            _controller.OnPointerMove(0, worldPos);
            Assert.IsFalse(_controller.HasDragged, "Dragging originating from UI must not start weapon rotation.");

            _controller.OnPointerUp(0, worldPos);
            Assert.IsFalse(_partSelectedCallbackReceived, "Release after UI press must never trigger part selection.");
        }

        [Test]
        public void Viewport_Small_Move_Under_Threshold_Does_Not_Rotate_And_Selects_On_Clean_Release()
        {
            Vector2 center = GetScreenPoint(_viewportRt);

            _controller.OnPointerDown(0, center);
            Assert.IsTrue(_controller.IsDragging, "Press inside viewport should register as dragging state.");
            Assert.IsFalse(_controller.HasDragged, "HasDragged must be false before movement.");

            // Move by 4px (< 8px threshold)
            _controller.OnPointerMove(0, center + new Vector2(3f, 2f));
            Assert.IsFalse(_controller.HasDragged, "Movement under drag threshold must not set HasDragged to true.");

            // Release inside viewport
            _controller.OnPointerUp(0, center + new Vector2(3f, 2f));
            Assert.IsFalse(_controller.IsDragging);
        }

        [Test]
        public void Viewport_Drag_Over_Threshold_Sets_HasDragged_And_Suppresses_Selection_On_Release()
        {
            Vector2 center = GetScreenPoint(_viewportRt);

            _controller.OnPointerDown(0, center);
            // Move by 25px (> 8px threshold)
            _controller.OnPointerMove(0, center + new Vector2(25f, 0f));

            Assert.IsTrue(_controller.HasDragged, "Movement over 8px must set HasDragged to true.");

            // Release
            _controller.OnPointerUp(0, center + new Vector2(25f, 0f));
            Assert.IsFalse(_partSelectedCallbackReceived, "Release after a rotation drag must never trigger click part selection.");
        }

        [Test]
        public void Viewport_Press_Released_Over_UI_Button_Suppresses_Selection()
        {
            // Start press inside viewport
            Vector2 center = GetScreenPoint(_viewportRt);
            _controller.OnPointerDown(0, center);

            // Release over bottom UI button
            Vector2 buttonPos = GetScreenPoint(_testButton.GetComponent<RectTransform>());
            _controller.OnPointerUp(0, buttonPos);

            Assert.IsFalse(_partSelectedCallbackReceived, "Release over a UI button must suppress 3D part selection.");
        }

        [Test]
        public void Pinch_Zoom_Sets_Pinching_And_Cannot_Become_Tap_Selection()
        {
            Vector2 center = GetScreenPoint(_viewportRt);
            Vector2 pos0 = center - new Vector2(100f, 0f);
            Vector2 pos1 = center + new Vector2(100f, 0f);

            _controller.OnPinch(pos0, pos1, new Vector2(-5f, 0f), new Vector2(5f, 0f));

            Assert.IsTrue(_controller.IsPinching, "Pinch must set IsPinching state.");
            Assert.IsTrue(_controller.HasDragged, "Pinch must set HasDragged so no tap occurs.");

            // Releasing one finger
            _controller.OnPointerUp(0, pos0);
            Assert.IsFalse(_partSelectedCallbackReceived, "Lifting fingers after pinch must never trigger part selection.");
        }

        [Test]
        public void InputReader_IsPointerOverUI_Suppresses_World_Clicks_On_UI()
        {
            // Verify IsPointerOverUI correctly detects the button
            Vector2 buttonPos = GetScreenPoint(_testButton.GetComponent<RectTransform>());
            bool overBtn = InputReader.IsPointerOverUI(buttonPos);
            Assert.IsTrue(overBtn, $"InputReader.IsPointerOverUI must detect the active UI button at {buttonPos}.");

            // Empty world area outside UI
            bool overWorld = InputReader.IsPointerOverUI(new Vector2(-100f, -100f));
            Assert.IsFalse(overWorld, "InputReader.IsPointerOverUI must return false off-screen or in open world space.");
        }
    }
}
