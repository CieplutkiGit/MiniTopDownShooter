using Application.Flow;
using Application.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Flow
{
    /// <summary>
    /// T09: Displays mission results (outcome, score, time, kills, waves)
    /// and provides the Return to Base action. Freezes arena and routes
    /// return through SceneFlowController.
    /// </summary>
    public class MissionResultsUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _outcomeText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _wavesText;
        [SerializeField] private TMP_Text _saveStatusText;
        [SerializeField] private TMP_Text _rewardSummaryText;
        [SerializeField] private Button _retrySaveButton;
        [SerializeField] private Button _returnButton;

        private AppCompositionRoot _app;

        public bool IsVisible => _panel != null && _panel.activeInHierarchy;

        private void Awake()
        {
            _app = AppCompositionRoot.Instance;

            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveAllListeners();
                _returnButton.onClick.AddListener(HandleReturn);
            }

            if (_retrySaveButton != null)
            {
                _retrySaveButton.onClick.RemoveAllListeners();
                _retrySaveButton.onClick.AddListener(RetrySave);
            }

            if (_panel != null)
                _panel.SetActive(false);

            TMP_Text[] texts = { _outcomeText, _scoreText, _timeText, _killsText, _wavesText, _saveStatusText };
            foreach (var t in texts)
            {
                if (t != null)
                {
                    t.raycastTarget = false;
                    t.enableAutoSizing = true;
                }
            }

            EnsureRewardSummaryText();
        }

        private void OnEnable()
        {
            RunFinalizer.SaveStatusChanged += HandleSaveStatusChanged;
            HandleSaveStatusChanged(RunFinalizer.CurrentSaveStatus);
        }

        private void OnDisable()
        {
            RunFinalizer.SaveStatusChanged -= HandleSaveStatusChanged;
        }

        public void Show(RunResult result)
        {
            if (result == null) return;

            if (_outcomeText != null)
                _outcomeText.text = result.Outcome.ToString();

            if (_scoreText != null)
                _scoreText.text = $"Score: {result.FinalScore}";

            if (_timeText != null)
            {
                int minutes = (int)(result.ElapsedActiveTime / 60f);
                int seconds = (int)(result.ElapsedActiveTime % 60f);
                _timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }

            if (_killsText != null)
                _killsText.text = $"Kills: {result.TotalKills}";

            if (_wavesText != null)
                _wavesText.text = $"Waves: {result.WavesCleared}";

            EconomyRewardCalculator.CalculateReward(result, out int coins, out int xp, out int scrap, out int alloy, out int core);
            if (_rewardSummaryText != null)
            {
                _rewardSummaryText.text = $"REWARDS  +{coins:N0} CR  ·  +{xp:N0} XP\nSALVAGE  Scrap {scrap}  |  Alloy {alloy}  |  Core {core}";
            }

            HandleSaveStatusChanged(RunFinalizer.CurrentSaveStatus);

            if (_panel != null)
                _panel.SetActive(true);

            // Freeze time while showing results
            Time.timeScale = 0f;
        }

        private void EnsureRewardSummaryText()
        {
            if (_rewardSummaryText != null || _panel == null) return;

            var labelObject = new GameObject("RewardSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(_panel.transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -104f);
            rect.sizeDelta = new Vector2(580f, 62f);
            _rewardSummaryText = labelObject.GetComponent<TextMeshProUGUI>();
            _rewardSummaryText.font = TMP_Settings.defaultFontAsset;
            _rewardSummaryText.fontSize = 17f;
            _rewardSummaryText.fontSizeMin = 14f;
            _rewardSummaryText.fontSizeMax = 17f;
            _rewardSummaryText.enableAutoSizing = true;
            _rewardSummaryText.alignment = TextAlignmentOptions.Center;
            _rewardSummaryText.color = Game.UI.UITheme.ColorAccentAmber;
            _rewardSummaryText.raycastTarget = false;
        }

        private void RetrySave()
        {
            RunFinalizer.RetryPending();
        }

        private void HandleSaveStatusChanged(RunSaveStatus status)
        {
            if (_saveStatusText != null)
            {
                _saveStatusText.text = status == RunSaveStatus.PendingRetry
                    ? "Save failed. Your run is queued; tap Retry Save."
                    : status == RunSaveStatus.Saved || status == RunSaveStatus.AlreadySaved
                        ? "Run saved."
                        : string.Empty;
            }

            if (_retrySaveButton != null)
                _retrySaveButton.gameObject.SetActive(status == RunSaveStatus.PendingRetry);
        }

        private void HandleReturn()
        {
            if (_app?.FlowCoordinator == null || _app.SceneFlow == null || _app.SceneFlow.IsTransitioning) return;

            if (_app.FlowCoordinator.TryReturnToBase())
            {
                _app.SceneFlow.GoToHub();
            }
        }
    }
}
