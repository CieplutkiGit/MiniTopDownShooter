using Application.Flow;
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
        [SerializeField] private Button _retrySaveButton;
        [SerializeField] private Button _returnButton;

        private AppCompositionRoot _app;

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

            HandleSaveStatusChanged(RunFinalizer.CurrentSaveStatus);

            if (_panel != null)
                _panel.SetActive(true);

            // Freeze time while showing results
            Time.timeScale = 0f;
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
