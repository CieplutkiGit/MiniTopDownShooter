using System;
using System.Collections;
using System.Collections.Generic;
using Application.Flow;
using Application.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    /// <summary>Owns additive transitions while the Boot scene remains loaded.</summary>
    [DefaultExecutionOrder(-201)]
    public class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private string _hubSceneName = "BaseHub";
        [SerializeField] private string _arenaSceneName = "ArenaShowcase";
        [SerializeField, Min(1)] private int _missionStartupFrameLimit = 600;

        private AppCompositionRoot _app;
        private Scene _bootScene;
        private bool _isTransitioning;

        public event Action OnTransitionStarted;
        public event Action OnTransitionCompleted;
        public event Action<string> OnTransitionFailed;
        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            // Capture the scene before AppCompositionRoot moves itself to DontDestroyOnLoad.
            _bootScene = gameObject.scene;
        }

        public void Initialize(AppCompositionRoot app) => _app = app;

        public void GoToHub()
        {
            if (!_isTransitioning && !IsTargetAlreadyActive(_hubSceneName))
                StartCoroutine(TransitionTo(_hubSceneName, false));
        }

        public void GoToArena(string sceneName = null)
        {
            if (_isTransitioning) return;
            string target = string.IsNullOrWhiteSpace(sceneName) ? _arenaSceneName : sceneName;
            if (IsTargetAlreadyActive(target)) return;
            StartCoroutine(TransitionTo(target, true));
        }

        private static bool IsTargetAlreadyActive(string sceneName)
        {
            Scene active = SceneManager.GetActiveScene();
            return active.IsValid() && active.isLoaded && string.Equals(active.name, sceneName, StringComparison.Ordinal);
        }

        private IEnumerator TransitionTo(string sceneName, bool mission)
        {
            _isTransitioning = true;
            int transitionToken = _app?.FlowCoordinator?.ActiveTransitionToken ?? 0;
            float previousTimeScale = Time.timeScale;
            InvokeSafely(OnTransitionStarted);

            string failure = null;
            AsyncOperation operation = null;
            List<Scene> previousScenes = SnapshotPlayableScenes();
            List<RootActivation> previousActivation = null;
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene loaded = default;
            if (!IsSceneLoadable(sceneName))
                failure = $"Scene is not present in Build Settings: {sceneName}";
            else
                previousActivation = SuspendScenes(previousScenes);

            if (failure == null)
            {
                try { operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive); }
                catch (Exception ex) { failure = $"Could not load {sceneName}: {ex.Message}"; }
                if (failure == null && operation == null) failure = $"Could not begin loading {sceneName}.";
            }

            if (failure == null)
            {
                while (!operation.isDone) yield return null;
                GameCompositionRoot root = null;
                try
                {
                    loaded = SceneManager.GetSceneByName(sceneName);
                    if (!loaded.IsValid() || !loaded.isLoaded)
                        failure = $"Scene load completed without a loaded scene: {sceneName}";
                    else if (!SceneManager.SetActiveScene(loaded))
                        failure = $"Could not make scene active: {sceneName}";
                    else
                        root = FindRootInScene(loaded);
                }
                catch (Exception ex) { failure = $"Could not initialize {sceneName}: {ex.Message}"; }

                if (failure == null && root == null)
                    failure = $"Scene {sceneName} has no GameCompositionRoot.";
                if (failure == null)
                {
                    try { _app?.InjectIntoSceneRoot(root); }
                    catch (Exception ex) { failure = $"Could not inject scene dependencies: {ex.Message}"; }
                }
                if (failure == null && mission && transitionToken != 0)
                {
                    if (_app?.FlowCoordinator == null || !_app.FlowCoordinator.NotifyMissionLoaded(transitionToken))
                        failure = "Mission scene loaded with a stale transition token.";
                    else
                    {
                        yield return WaitForMissionStartup(loaded, transitionToken);
                        failure = _missionStartupFailure;
                    }
                }
                else if (failure == null && !mission)
                {
                    yield return WaitForHubStartup(loaded);
                    failure = _missionStartupFailure;
                }
            }

            // Keep the old scene available until the destination is usable. A failure can then
            // remove the partial destination and restore the previous active scene.
            if (failure == null)
            {
                for (int i = 0; i < previousScenes.Count && failure == null; i++)
                {
                    AsyncOperation unload = null;
                    try { unload = SceneManager.UnloadSceneAsync(previousScenes[i]); }
                    catch (Exception ex) { failure = $"Could not unload {previousScenes[i].name}: {ex.Message}"; }
                    if (failure == null && unload == null) failure = $"Could not begin unloading {previousScenes[i].name}.";
                    while (failure == null && unload != null && !unload.isDone) yield return null;
                }
            }

            if (failure == null && transitionToken != 0 && _app?.FlowCoordinator != null)
            {
                bool notified = mission
                    ? _app.FlowCoordinator.NotifyMissionStarted(transitionToken)
                    : _app.FlowCoordinator.NotifyBaseLoaded(transitionToken);
                if (!notified) failure = "Scene transition completed with a stale transition token.";
            }

            if (failure == null)
            {
                GameStateController destinationState = FindStateController(loaded);
                Time.timeScale = destinationState != null && GameActivityPolicy.IsTimeFrozen(destinationState.CurrentState) ? 0f : 1f;
            }

            if (failure != null)
            {
                if (loaded.IsValid() && loaded.isLoaded && loaded != _bootScene)
                {
                    AsyncOperation cleanup = null;
                    try { cleanup = SceneManager.UnloadSceneAsync(loaded); } catch { }
                    while (cleanup != null && !cleanup.isDone) yield return null;
                }
                if (previousActivation != null) RestoreScenes(previousActivation);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
                Time.timeScale = previousTimeScale;
            }
            _isTransitioning = false;
            if (failure != null)
            {
                if (transitionToken != 0) _app?.FlowCoordinator?.NotifyTransitionFailed(transitionToken, failure);
                Debug.LogError($"[SceneFlowController] {failure}");
                InvokeSafely(OnTransitionFailed, failure);
                yield break;
            }

            InvokeSafely(OnTransitionCompleted);
        }

        private IEnumerator WaitForMissionStartup(Scene loaded, int token)
        {
            _missionStartupFailure = null;
            GameFlowCoordinator coordinator = _app?.FlowCoordinator;
            if (coordinator == null || token == 0) yield break;

            MissionRunController mission = FindMissionController(loaded);
            if (mission == null)
            {
                _missionStartupFailure = "Mission scene has no MissionRunController.";
                yield break;
            }

            int frames = 0;
            while (!mission.IsMissionStarted && frames++ < Mathf.Max(1, _missionStartupFrameLimit))
            {
                if (coordinator.ActiveRun?.IsCompleted == true) break;
                yield return null;
            }
            if (!mission.IsMissionPrepared || !mission.IsMissionStarted)
                _missionStartupFailure = "Mission startup did not complete; the run can be retried.";
        }

        private IEnumerator WaitForHubStartup(Scene loaded)
        {
            _missionStartupFailure = null;
            HubSceneRoot hub = null;
            foreach (GameObject go in loaded.GetRootGameObjects())
            {
                hub = go.GetComponentInChildren<HubSceneRoot>(true);
                if (hub != null) break;
            }
            if (hub == null)
            {
                _missionStartupFailure = "Hub scene has no HubSceneRoot.";
                yield break;
            }

            int frames = 0;
            while (!hub.IsInitialized && frames++ < Mathf.Max(1, _missionStartupFrameLimit))
                yield return null;
            if (!hub.IsInitialized)
                _missionStartupFailure = "Hub startup did not complete.";
        }

        private string _missionStartupFailure;

        private static void InvokeSafely(Action callback)
        {
            if (callback == null) return;
            foreach (Action handler in callback.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private static void InvokeSafely(Action<string> callback, string value)
        {
            if (callback == null) return;
            foreach (Action<string> handler in callback.GetInvocationList())
            {
                try { handler(value); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private bool IsSceneLoadable(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private List<Scene> SnapshotPlayableScenes()
        {
            var scenes = new List<Scene>();
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != _bootScene && scene.name != "DontDestroyOnLoad")
                    scenes.Add(scene);
            }
            return scenes;
        }

        private static GameCompositionRoot FindRootInScene(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                GameCompositionRoot root = go.GetComponentInChildren<GameCompositionRoot>(true);
                if (root != null) return root;
            }
            return null;
        }

        private static MissionRunController FindMissionController(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                MissionRunController controller = go.GetComponentInChildren<MissionRunController>(true);
                if (controller != null) return controller;
            }
            return null;
        }

        private static GameStateController FindStateController(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                GameStateController controller = go.GetComponentInChildren<GameStateController>(true);
                if (controller != null) return controller;
            }
            return null;
        }

        private static List<RootActivation> SuspendScenes(List<Scene> scenes)
        {
            var roots = new List<RootActivation>();
            for (int i = 0; i < scenes.Count; i++)
            {
                foreach (GameObject root in scenes[i].GetRootGameObjects())
                {
                    roots.Add(new RootActivation(root, root.activeSelf));
                    root.SetActive(false);
                }
            }
            return roots;
        }

        private static void RestoreScenes(List<RootActivation> roots)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i].Root != null) roots[i].Root.SetActive(roots[i].WasActive);
            }
        }

        private readonly struct RootActivation
        {
            public readonly GameObject Root;
            public readonly bool WasActive;

            public RootActivation(GameObject root, bool wasActive)
            {
                Root = root;
                WasActive = wasActive;
            }
        }
    }
}
