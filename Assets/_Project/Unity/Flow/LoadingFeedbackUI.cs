using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Flow
{
    /// <summary>
    /// Runtime overlay canvas providing loading feedback, an unscaled animated text,
    /// a loading click blocker, and an error panel with dismissal.
    /// </summary>
    [DefaultExecutionOrder(-199)]
    public class LoadingFeedbackUI : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GameObject _clickBlocker;
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _dismissButton;

        private SceneFlowController _sceneFlow;
        private Coroutine _animationCoroutine;
        private bool _isSubscribed;
        private bool _isLoading;
        private string _lastErrorMessage;

        public Canvas Canvas => _canvas;
        public GameObject ClickBlocker => _clickBlocker;
        public GameObject LoadingPanel => _loadingPanel;
        public TMP_Text LoadingText => _loadingText;
        public GameObject ErrorPanel => _errorPanel;
        public TMP_Text ErrorText => _errorText;
        public Button DismissButton => _dismissButton;
        public bool IsLoading => _isLoading;
        public bool IsShowingError => _errorPanel != null && _errorPanel.activeSelf;
        public string CurrentErrorMessage => _lastErrorMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            AppCompositionRoot app = AppCompositionRoot.Instance;
            if (app == null) return;
            if (app.GetComponentInChildren<LoadingFeedbackUI>(true) != null) return;

            GameObject uiGO = new GameObject("LoadingFeedbackUI");
            uiGO.transform.SetParent(app.transform, false);
            uiGO.AddComponent<LoadingFeedbackUI>();
        }

        private void Awake()
        {
            if (AppCompositionRoot.Instance != null && transform != AppCompositionRoot.Instance.transform && transform.parent != AppCompositionRoot.Instance.transform)
            {
                transform.SetParent(AppCompositionRoot.Instance.transform, false);
            }
            EnsureUI();
            HideAll();
        }

        private void OnEnable()
        {
            EnsureUI();
            Subscribe();
        }

        private void Start()
        {
            if (!_isSubscribed)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
            _isLoading = false;
            HideAll();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;
            AppCompositionRoot app = AppCompositionRoot.Instance != null
                ? AppCompositionRoot.Instance
                : FindFirstObjectByType<AppCompositionRoot>();

            if (app != null && app.SceneFlow != null)
            {
                _sceneFlow = app.SceneFlow;
                _sceneFlow.OnTransitionStarted += HandleTransitionStarted;
                _sceneFlow.OnTransitionCompleted += HandleTransitionCompleted;
                _sceneFlow.OnTransitionFailed += HandleTransitionFailed;
                _isSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;
            if (_sceneFlow != null)
            {
                _sceneFlow.OnTransitionStarted -= HandleTransitionStarted;
                _sceneFlow.OnTransitionCompleted -= HandleTransitionCompleted;
                _sceneFlow.OnTransitionFailed -= HandleTransitionFailed;
                _sceneFlow = null;
            }
            _isSubscribed = false;
        }

        public void HandleTransitionStarted()
        {
            _isLoading = true;
            _lastErrorMessage = null;

            EnsureUI();
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_clickBlocker != null) _clickBlocker.SetActive(true);
            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_errorPanel != null) _errorPanel.SetActive(false);

            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateLoadingText());
        }

        public void HandleTransitionCompleted()
        {
            _isLoading = false;
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            HideAll();
        }

        public void HandleTransitionFailed(string error)
        {
            _isLoading = false;
            _lastErrorMessage = error;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            EnsureUI();
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_clickBlocker != null) _clickBlocker.SetActive(true);
            if (_errorPanel != null) _errorPanel.SetActive(true);
            if (_errorText != null)
            {
                _errorText.text = !string.IsNullOrEmpty(error) ? error : "An unexpected transition error occurred.";
            }
        }

        public void DismissError()
        {
            _lastErrorMessage = null;
            HideAll();
        }

        private void HideAll()
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_errorPanel != null) _errorPanel.SetActive(false);
            if (_clickBlocker != null) _clickBlocker.SetActive(false);
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private IEnumerator AnimateLoadingText()
        {
            string[] frames = new string[] { "Loading", "Loading.", "Loading..", "Loading..." };
            int index = 0;
            while (_isLoading)
            {
                if (_loadingText != null)
                {
                    _loadingText.text = frames[index];
                }
                index = (index + 1) % frames.Length;
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private void EnsureUI()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<Canvas>(true);
            }

            if (_canvas == null)
            {
                GameObject canvasGO = new GameObject("LoadingFeedbackCanvas");
                canvasGO.transform.SetParent(transform, false);

                _canvas = canvasGO.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 999;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();

                _clickBlocker = new GameObject("ClickBlocker");
                _clickBlocker.transform.SetParent(_canvas.transform, false);
                Image blockerImg = _clickBlocker.AddComponent<Image>();
                blockerImg.color = new Color(0f, 0f, 0f, 0.5f);
                blockerImg.raycastTarget = true;
                RectTransform blockerRect = _clickBlocker.GetComponent<RectTransform>();
                blockerRect.anchorMin = Vector2.zero;
                blockerRect.anchorMax = Vector2.one;
                blockerRect.offsetMin = Vector2.zero;
                blockerRect.offsetMax = Vector2.zero;

                _loadingPanel = new GameObject("LoadingPanel");
                _loadingPanel.transform.SetParent(_canvas.transform, false);
                RectTransform loadingRect = _loadingPanel.AddComponent<RectTransform>();
                loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
                loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
                loadingRect.sizeDelta = new Vector2(500, 100);
                loadingRect.anchoredPosition = Vector2.zero;

                GameObject loadingTextGO = new GameObject("LoadingText");
                loadingTextGO.transform.SetParent(_loadingPanel.transform, false);
                _loadingText = loadingTextGO.AddComponent<TextMeshProUGUI>();
                _loadingText.text = "Loading...";
                _loadingText.fontSize = 32;
                _loadingText.alignment = TextAlignmentOptions.Center;
                _loadingText.color = Color.white;
                RectTransform loadingTextRect = loadingTextGO.GetComponent<RectTransform>();
                loadingTextRect.anchorMin = Vector2.zero;
                loadingTextRect.anchorMax = Vector2.one;
                loadingTextRect.offsetMin = Vector2.zero;
                loadingTextRect.offsetMax = Vector2.zero;

                _errorPanel = new GameObject("ErrorPanel");
                _errorPanel.transform.SetParent(_canvas.transform, false);
                Image errorBg = _errorPanel.AddComponent<Image>();
                errorBg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
                RectTransform errorRect = _errorPanel.GetComponent<RectTransform>();
                errorRect.anchorMin = new Vector2(0.5f, 0.5f);
                errorRect.anchorMax = new Vector2(0.5f, 0.5f);
                errorRect.sizeDelta = new Vector2(600, 300);
                errorRect.anchoredPosition = Vector2.zero;

                GameObject titleGO = new GameObject("ErrorTitle");
                titleGO.transform.SetParent(_errorPanel.transform, false);
                TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
                titleText.text = "Transition Failed";
                titleText.fontSize = 24;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.35f, 0.35f, 1f);
                RectTransform titleRect = titleGO.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 0.7f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = new Vector2(20, 0);
                titleRect.offsetMax = new Vector2(-20, -15);

                GameObject msgGO = new GameObject("ErrorMessage");
                msgGO.transform.SetParent(_errorPanel.transform, false);
                _errorText = msgGO.AddComponent<TextMeshProUGUI>();
                _errorText.text = string.Empty;
                _errorText.fontSize = 18;
                _errorText.alignment = TextAlignmentOptions.Center;
                _errorText.color = Color.white;
                RectTransform msgRect = msgGO.GetComponent<RectTransform>();
                msgRect.anchorMin = new Vector2(0f, 0.3f);
                msgRect.anchorMax = new Vector2(1f, 0.7f);
                msgRect.offsetMin = new Vector2(20, 0);
                msgRect.offsetMax = new Vector2(-20, 0);

                GameObject buttonGO = new GameObject("DismissButton");
                buttonGO.transform.SetParent(_errorPanel.transform, false);
                Image btnImg = buttonGO.AddComponent<Image>();
                btnImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                _dismissButton = buttonGO.AddComponent<Button>();
                _dismissButton.onClick.AddListener(DismissError);
                RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0.1f);
                btnRect.anchorMax = new Vector2(0.5f, 0.1f);
                btnRect.sizeDelta = new Vector2(160, 45);
                btnRect.anchoredPosition = new Vector2(0, 15);

                GameObject btnTextGO = new GameObject("ButtonText");
                btnTextGO.transform.SetParent(buttonGO.transform, false);
                TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
                btnText.text = "Dismiss";
                btnText.fontSize = 18;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.color = Color.white;
                RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;
            }
            else if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(DismissError);
                _dismissButton.onClick.AddListener(DismissError);
            }

            if (!_isLoading && !IsShowingError)
            {
                HideAll();
            }
        }
    }
}
