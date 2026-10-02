using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class MobileDemoControlsBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _showOnDesktop = true;
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
        [Range(0f, 0.95f)]
        [SerializeField] private float _moveDeadZone = 0.12f;
        [Range(0f, 0.95f)]
        [SerializeField] private float _aimDeadZone = 0.12f;
        [Range(0f, 1f)]
        [SerializeField] private float _aimFireThreshold = 0.2f;

        private MobileInputState _input;
        private GameObject _canvasObject;
        private RectTransform _safeArea;

        public MobileInputState Input => _input;
        public GameObject CanvasObject => _canvasObject;
        public RectTransform SafeArea => _safeArea;

        private void Awake()
        {
            _input = GetComponent<MobileInputState>();

            if (_input == null)
            {
                _input = gameObject.AddComponent<MobileInputState>();
            }

            BuildControls();

            Application.GameSettingsData settings = SaveManager.LoadSettings();
            if (settings != null)
            {
                ApplySettings(settings);
            }
        }

        public void ApplySettings(Application.GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            if (_canvasObject == null)
            {
                BuildControls();
            }

            if (_canvasObject != null)
            {
                bool shouldBeActive = settings.MobileTouchControls || UnityEngine.Application.isMobilePlatform || _showOnDesktop;
                _canvasObject.SetActive(shouldBeActive);
            }

            if (_safeArea != null)
            {
                _safeArea.localScale = Vector3.one * Mathf.Clamp(settings.TouchControlScale, 0.5f, 2.5f);
            }
        }

        private void BuildControls()
        {
            if (GameObject.Find("MobileDemoControls") != null)
            {
                _canvasObject = GameObject.Find("MobileDemoControls");
                if (_canvasObject != null)
                {
                    Transform safeChild = _canvasObject.transform.Find("SafeArea");
                    _safeArea = safeChild != null ? safeChild as RectTransform : null;
                }
                return;
            }

            GameObject canvasObject = new GameObject(
                "MobileDemoControls",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            _canvasObject = canvasObject;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.SetParent(transform, false);

            RectTransform safeArea = CreateRect(
                "SafeArea",
                canvasRect,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            _safeArea = safeArea;
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            CreateJoystick(
                safeArea,
                "MoveJoystick",
                MobileJoystickChannel.Move,
                new Vector2(180f, 180f),
                false,
                _moveDeadZone);

            CreateJoystick(
                safeArea,
                "AimJoystick",
                MobileJoystickChannel.Look,
                new Vector2(-180f, 180f),
                true,
                _aimDeadZone);

            CreateActionButton(
                safeArea,
                "Fire",
                MobileInputAction.Fire,
                new Vector2(-360f, 220f),
                new Vector2(120f, 120f));

            CreateActionButton(
                safeArea,
                "Reload",
                MobileInputAction.Reload,
                new Vector2(-500f, 110f),
                new Vector2(105f, 105f));

            CreateActionButton(
                safeArea,
                "Next",
                MobileInputAction.NextWeapon,
                new Vector2(-360f, 370f),
                new Vector2(95f, 95f));

            CreateActionButton(
                safeArea,
                "Prev",
                MobileInputAction.PreviousWeapon,
                new Vector2(-490f, 340f),
                new Vector2(95f, 95f));

            CreatePauseButton(safeArea);
        }

        private void CreateJoystick(
            RectTransform parent,
            string name,
            MobileJoystickChannel channel,
            Vector2 anchoredPosition,
            bool rightAnchored,
            float deadZone)
        {
            Vector2 anchor = rightAnchored
                ? new Vector2(1f, 0f)
                : Vector2.zero;

            RectTransform background = CreateRect(
                name,
                parent,
                anchor,
                anchor,
                anchoredPosition,
                new Vector2(270f, 270f));

            background.pivot = new Vector2(0.5f, 0.5f);

            Image backgroundImage =
                background.gameObject.AddComponent<Image>();

            backgroundImage.color =
                new Color(1f, 1f, 1f, 0.16f);

            RectTransform handle = CreateRect(
                "Handle",
                background,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(120f, 120f));

            handle.pivot = new Vector2(0.5f, 0.5f);

            Image handleImage =
                handle.gameObject.AddComponent<Image>();

            handleImage.color =
                new Color(1f, 1f, 1f, 0.48f);

            MobileJoystick joystick =
                background.gameObject.AddComponent<MobileJoystick>();

            joystick.Configure(
                _input,
                channel,
                background,
                handle,
                deadZone,
                0.68f,
                channel == MobileJoystickChannel.Look,
                _aimFireThreshold);
        }

        private void CreateActionButton(
            RectTransform parent,
            string label,
            MobileInputAction action,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform buttonRect = CreateRect(
                label + "Button",
                parent,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                anchoredPosition,
                size);

            buttonRect.pivot = new Vector2(1f, 0f);

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.22f);

            MobileActionButton button =
                buttonRect.gameObject.AddComponent<MobileActionButton>();

            button.Configure(_input, action);
            CreateLabel(buttonRect, label);
        }

        private void CreatePauseButton(RectTransform parent)
        {
            RectTransform buttonRect = CreateRect(
                "PauseButton",
                parent,
                Vector2.one,
                Vector2.one,
                new Vector2(-30f, -30f),
                new Vector2(110f, 70f));

            buttonRect.pivot = Vector2.one;

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.22f);

            MobileActionButton button =
                buttonRect.gameObject.AddComponent<MobileActionButton>();

            button.Configure(_input, MobileInputAction.Pause);
            CreateLabel(buttonRect, "Pause");
        }

        private static RectTransform CreateRect(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform));

            RectTransform rect =
                gameObject.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static void CreateLabel(
            RectTransform parent,
            string label)
        {
            RectTransform labelRect = CreateRect(
                "Label",
                parent,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            Text text = labelRect.gameObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 24;
            text.raycastTarget = false;

            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            if (font != null)
            {
                text.font = font;
            }
        }

        private void OnValidate()
        {
            _referenceResolution.x =
                Mathf.Max(320f, _referenceResolution.x);

            _referenceResolution.y =
                Mathf.Max(320f, _referenceResolution.y);

            _moveDeadZone =
                Mathf.Clamp(_moveDeadZone, 0f, 0.95f);

            _aimDeadZone =
                Mathf.Clamp(_aimDeadZone, 0f, 0.95f);

            _aimFireThreshold =
                Mathf.Clamp01(_aimFireThreshold);
        }
    }
}
