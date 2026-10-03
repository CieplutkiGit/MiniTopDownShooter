using System;
using System.Collections.Generic;
using Application;
using Application.Flow;
using Application.Weapons;
using Application.Workshop;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Flow
{
    [TestFixture]
    public class FlowAndTransitionTests
    {
        [Test]
        public void GameStateEnum_PreservesOriginalValues_AndAppendsLoadingAndBriefing()
        {
            Assert.AreEqual(0, (int)GameState.Menu);
            Assert.AreEqual(1, (int)GameState.Playing);
            Assert.AreEqual(2, (int)GameState.Paused);
            Assert.AreEqual(3, (int)GameState.GameOver);
            Assert.AreEqual(4, (int)GameState.Victory);
            Assert.AreEqual(5, (int)GameState.WorkshopRoaming);
            Assert.AreEqual(6, (int)GameState.WorkshopEditing);
            Assert.AreEqual(7, (int)GameState.WorkshopFiringRange);
            Assert.AreEqual(8, (int)GameState.Loading);
            Assert.AreEqual(9, (int)GameState.DeploymentBriefing);
        }

        #region GameStateManager Transitions & Guards

        [Test]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.Victory)]
        [TestCase(GameState.DeploymentBriefing)]
        [TestCase(GameState.Loading)]
        [TestCase(GameState.WorkshopRoaming)]
        public void StartGame_AllowedFromAuthorizedStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.StartGame();
            Assert.AreEqual(GameState.Playing, manager.CurrentState);
        }

        [Test]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.WorkshopFiringRange)]
        [TestCase(GameState.Paused)]
        public void StartGame_RejectedFromGuardedStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.StartGame();
            Assert.AreEqual(origin, manager.CurrentState, $"StartGame should be rejected from {origin}");
        }

        [Test]
        public void EnterWorkshopEditing_AllowedOnlyFromWorkshopRoaming()
        {
            var manager = new GameStateManager();
            manager.EnterWorkshopRoaming();
            manager.EnterWorkshopEditing();
            Assert.AreEqual(GameState.WorkshopEditing, manager.CurrentState);
        }

        [Test]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.Paused)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.Victory)]
        [TestCase(GameState.WorkshopFiringRange)]
        [TestCase(GameState.DeploymentBriefing)]
        [TestCase(GameState.Loading)]
        public void EnterWorkshopEditing_RejectedFromNonRoamingStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.EnterWorkshopEditing();
            Assert.AreEqual(origin, manager.CurrentState, $"EnterWorkshopEditing must be rejected from {origin}");
        }

        [Test]
        public void EnterWorkshopFiringRange_AllowedOnlyFromWorkshopRoaming()
        {
            var manager = new GameStateManager();
            manager.EnterWorkshopRoaming();
            manager.EnterWorkshopFiringRange();
            Assert.AreEqual(GameState.WorkshopFiringRange, manager.CurrentState);
        }

        [Test]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.Paused)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.Victory)]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.DeploymentBriefing)]
        [TestCase(GameState.Loading)]
        public void EnterWorkshopFiringRange_RejectedFromNonRoamingStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.EnterWorkshopFiringRange();
            Assert.AreEqual(origin, manager.CurrentState, $"EnterWorkshopFiringRange must be rejected from {origin}");
        }

        [Test]
        public void EnterDeploymentBriefing_AllowedOnlyFromWorkshopRoaming()
        {
            var manager = new GameStateManager();
            manager.EnterWorkshopRoaming();
            manager.EnterDeploymentBriefing();
            Assert.AreEqual(GameState.DeploymentBriefing, manager.CurrentState);
        }

        [Test]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.Paused)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.Victory)]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.WorkshopFiringRange)]
        [TestCase(GameState.Loading)]
        public void EnterDeploymentBriefing_RejectedFromNonRoamingStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.EnterDeploymentBriefing();
            Assert.AreEqual(origin, manager.CurrentState, $"EnterDeploymentBriefing must be rejected from {origin}");
        }

        [Test]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.WorkshopRoaming)]
        [TestCase(GameState.WorkshopEditing)]
        [TestCase(GameState.WorkshopFiringRange)]
        [TestCase(GameState.DeploymentBriefing)]
        public void PauseAndResume_RestoresExactOriginActivity_ForAllActivities(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);
            Assert.AreEqual(origin, manager.CurrentState);

            manager.Pause();
            Assert.AreEqual(GameState.Paused, manager.CurrentState);
            Assert.AreEqual(origin, manager.PreviousStateBeforePause);

            manager.Resume();
            Assert.AreEqual(origin, manager.CurrentState);
        }

        [Test]
        [TestCase(GameState.Menu)]
        [TestCase(GameState.Loading)]
        [TestCase(GameState.GameOver)]
        [TestCase(GameState.Victory)]
        public void Pause_RejectedFromNonGameplayStates(GameState origin)
        {
            var manager = new GameStateManager();
            SetManagerOrigin(manager, origin);

            manager.Pause();
            Assert.AreEqual(origin, manager.CurrentState, $"Pause must be rejected from {origin}");
        }

        #endregion

        #region GameActivityPolicy Switching vs Firing

        [Test]
        public void GameActivityPolicy_SeparatesWeaponSwitchingFromFiring()
        {
            // WorkshopRoaming allows switching weapons but blocks firing
            Assert.IsTrue(GameActivityPolicy.CanSwitchWeapon(GameState.WorkshopRoaming));
            Assert.IsTrue(GameActivityPolicy.CanSelectWeapon(GameState.WorkshopRoaming));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.WorkshopRoaming));

            // Playing allows both
            Assert.IsTrue(GameActivityPolicy.CanSwitchWeapon(GameState.Playing));
            Assert.IsTrue(GameActivityPolicy.CanFire(GameState.Playing));

            // Firing range allows both
            Assert.IsTrue(GameActivityPolicy.CanSwitchWeapon(GameState.WorkshopFiringRange));
            Assert.IsTrue(GameActivityPolicy.CanFire(GameState.WorkshopFiringRange));

            // Editing blocks both
            Assert.IsFalse(GameActivityPolicy.CanSwitchWeapon(GameState.WorkshopEditing));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.WorkshopEditing));

            // Briefing blocks both
            Assert.IsFalse(GameActivityPolicy.CanSwitchWeapon(GameState.DeploymentBriefing));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.DeploymentBriefing));

            // Loading blocks both
            Assert.IsFalse(GameActivityPolicy.CanSwitchWeapon(GameState.Loading));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.Loading));

            // Paused blocks both
            Assert.IsFalse(GameActivityPolicy.CanSwitchWeapon(GameState.Paused));
            Assert.IsFalse(GameActivityPolicy.CanFire(GameState.Paused));
        }

        #endregion

        #region GameFlowCoordinator Deploy, Return, & Token Guarding

        [Test]
        public void GameFlowCoordinator_InitialState_IsInHub()
        {
            var coordinator = new GameFlowCoordinator();
            Assert.AreEqual(GameFlowState.InHub, coordinator.FlowState);
            Assert.AreEqual(GameFlowState.InHub, coordinator.FlowState);
            Assert.IsTrue(coordinator.CanDeploy);
            Assert.IsFalse(coordinator.CanReturnToBase);
            Assert.IsNull(coordinator.ActiveRun);
        }

        [Test]
        public void Deploy_AllowedFromHub_StartsRunAndAdvancesToDeploying()
        {
            var coordinator = new GameFlowCoordinator();
            var snapshot = CreateSampleLoadout();
            var command = new DeployCommand("arena_sweep", snapshot);

            bool accepted = coordinator.TryDeploy(command, out int token);

            Assert.IsTrue(accepted);
            Assert.Greater(token, 0);
            Assert.AreEqual(GameFlowState.Deploying, coordinator.FlowState);
            Assert.IsNotNull(coordinator.ActiveRun);
            Assert.AreEqual("arena_sweep", coordinator.ActiveRun.MissionId);
            Assert.AreEqual(RunOutcome.None, coordinator.ActiveRun.Outcome);
            Assert.IsFalse(coordinator.ActiveRun.IsCompleted);
        }

        [Test]
        public void Deploy_DuplicateOrWhileDeploying_IsRejected()
        {
            var coordinator = new GameFlowCoordinator();
            var snapshot = CreateSampleLoadout();
            var command = new DeployCommand("arena_sweep", snapshot);

            bool first = coordinator.TryDeploy(command, out int token1);
            Assert.IsTrue(first);

            // Duplicate deploy click while already deploying
            bool second = coordinator.TryDeploy(command, out int token2);
            Assert.IsFalse(second);
            Assert.AreEqual(0, token2);
            Assert.AreEqual(token1, coordinator.ActiveTransitionToken);
        }

        [Test]
        public void Deploy_WhileInMission_IsRejected()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.StartDirectMission("arena_sweep");

            Assert.AreEqual(GameFlowState.InMission, coordinator.FlowState);
            Assert.IsFalse(coordinator.CanDeploy);

            bool accepted = coordinator.TryDeploy(new DeployCommand("arena_sweep"));
            Assert.IsFalse(accepted);
        }

        [Test]
        public void SceneLoaded_WithValidToken_TransitionsToInMission()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);

            bool loaded = coordinator.NotifyMissionLoaded(token);
            Assert.IsTrue(loaded);
            Assert.AreEqual(GameFlowState.InMission, coordinator.FlowState);
            Assert.AreEqual(token, coordinator.ActiveTransitionToken);
            Assert.IsTrue(coordinator.NotifyMissionStarted(token));
            Assert.AreEqual(0, coordinator.ActiveTransitionToken);
        }

        [Test]
        public void SceneLoaded_WithStaleToken_IsIgnored()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int validToken);

            int staleToken = validToken - 1;
            bool accepted = coordinator.NotifyMissionLoaded(staleToken);

            Assert.IsFalse(accepted);
            Assert.AreEqual(GameFlowState.Deploying, coordinator.FlowState);
        }

        [Test]
        public void TransitionFailedDuringDeploy_ClearsTokenAndAllowsRetry()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);

            Assert.IsTrue(coordinator.NotifyTransitionFailed(token, "load failed"));
            Assert.AreEqual(0, coordinator.ActiveTransitionToken);
            Assert.IsTrue(coordinator.ActiveRun.IsCompleted);
            Assert.AreEqual(RunOutcome.TechnicalError, coordinator.ActiveRun.Outcome);
            Assert.IsTrue(coordinator.CanDeploy);
            Assert.IsFalse(coordinator.NotifyTransitionFailed(token, "stale failure"));
        }

        [Test]
        public void TransitionFailedDuringMissionStartup_FinalizesAndAllowsDeploymentRetry()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            Assert.IsTrue(coordinator.NotifyMissionLoaded(token));

            Assert.IsTrue(coordinator.NotifyTransitionFailed(token, "startup failed"));
            Assert.AreEqual(0, coordinator.ActiveTransitionToken);
            Assert.AreEqual(GameFlowState.InHub, coordinator.FlowState);
            Assert.AreEqual(RunOutcome.TechnicalError, coordinator.LastFinalizedResult.Outcome);
            Assert.IsTrue(coordinator.CanDeploy);
        }

        [Test]
        public void OutcomeDuringStartup_CompletesTransitionWithoutLosingResults()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            Assert.IsTrue(coordinator.NotifyMissionLoaded(token));
            Assert.IsTrue(coordinator.TryReportRunOutcome(RunOutcome.TechnicalError));
            Assert.IsTrue(coordinator.NotifyMissionStarted(token));
            Assert.AreEqual(0, coordinator.ActiveTransitionToken);
            Assert.AreEqual(GameFlowState.MissionResults, coordinator.FlowState);
            Assert.IsTrue(coordinator.CanReturnToBase);
        }

        [Test]
        public void TransitionFailedDuringReturn_ClearsTokenAndAllowsRetry()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int deployToken);
            coordinator.NotifyMissionLoaded(deployToken);
            coordinator.NotifyMissionStarted(deployToken);
            coordinator.TryReturnToBase(ReturnToBaseCommand.Default, out int returnToken);

            Assert.IsTrue(coordinator.NotifyTransitionFailed(returnToken, "hub load failed"));
            Assert.AreEqual(0, coordinator.ActiveTransitionToken);
            Assert.IsTrue(coordinator.CanReturnToBase);
            Assert.IsFalse(coordinator.NotifyBaseLoaded(returnToken));
        }

        [Test]
        public void ReturnToBase_AllowedFromResults_TransitionsToReturningToBase()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            coordinator.NotifyMissionLoaded(token);
            coordinator.NotifyMissionStarted(token);
            coordinator.TryReportRunOutcome(RunOutcome.Victory, score: 500, kills: 20, wavesCleared: 4);

            Assert.AreEqual(GameFlowState.MissionResults, coordinator.FlowState);
            Assert.IsTrue(coordinator.CanReturnToBase);

            bool accepted = coordinator.TryReturnToBase(new ReturnToBaseCommand("user_exit"), out int returnToken);
            Assert.IsTrue(accepted);
            Assert.Greater(returnToken, 0);
            Assert.AreEqual(GameFlowState.ReturningToBase, coordinator.FlowState);
        }

        [Test]
        public void ReturnToBase_DuplicateClick_IsRejected()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            coordinator.NotifyMissionLoaded(token);
            coordinator.NotifyMissionStarted(token);
            coordinator.TryReportRunOutcome(RunOutcome.Victory, score: 500, kills: 20, wavesCleared: 4);

            bool first = coordinator.TryReturnToBase(ReturnToBaseCommand.Default, out int token1);
            Assert.IsTrue(first);

            // Duplicate return click
            bool second = coordinator.TryReturnToBase(ReturnToBaseCommand.Default, out int token2);
            Assert.IsFalse(second);
            Assert.AreEqual(0, token2);
            Assert.AreEqual(token1, coordinator.ActiveTransitionToken);
        }

        [Test]
        public void ReturnToBase_DirectAbandonFromMission_CompletesAsAbandoned()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            coordinator.NotifyMissionLoaded(token);
            coordinator.NotifyMissionStarted(token);

            Assert.AreEqual(GameFlowState.InMission, coordinator.FlowState);
            RunResult finalized = null;
            coordinator.OnRunCompleted += r => finalized = r;

            bool accepted = coordinator.TryReturnToBase(new ReturnToBaseCommand("pause_menu_abandon"), out _);
            Assert.IsTrue(accepted);
            Assert.AreEqual(GameFlowState.ReturningToBase, coordinator.FlowState);
            Assert.IsNotNull(finalized);
            Assert.AreEqual(RunOutcome.Abandoned, finalized.Outcome);
            Assert.IsTrue(finalized.CountsAsRun);
            Assert.IsFalse(finalized.IncrementsWins);
            Assert.IsFalse(finalized.IncrementsLosses);
        }

        [Test]
        public void ReturnToBase_RejectedWhenInHub()
        {
            var coordinator = new GameFlowCoordinator();
            Assert.AreEqual(GameFlowState.InHub, coordinator.FlowState);
            Assert.IsFalse(coordinator.CanReturnToBase);

            bool accepted = coordinator.TryReturnToBase();
            Assert.IsFalse(accepted);
        }

        [Test]
        public void BaseLoaded_WithValidToken_ResetsActiveRunAndReturnsToHub()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int deployToken);
            coordinator.NotifyMissionLoaded(deployToken);
            coordinator.NotifyMissionStarted(deployToken);
            coordinator.TryReportRunOutcome(RunOutcome.Victory);
            coordinator.TryReturnToBase(ReturnToBaseCommand.Default, out int returnToken);

            bool loaded = coordinator.NotifyBaseLoaded(returnToken);
            Assert.IsTrue(loaded);
            Assert.AreEqual(GameFlowState.InHub, coordinator.FlowState);
            Assert.IsNull(coordinator.ActiveRun);
        }

        #endregion

        #region Terminal-Result Idempotence & Run Outcomes

        [Test]
        public void TerminalResult_IsIdempotent_CannotOverwriteVictoryWithDefeat()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            coordinator.NotifyMissionLoaded(token);
            coordinator.NotifyMissionStarted(token);

            bool first = coordinator.TryReportRunOutcome(RunOutcome.Victory, score: 1000, kills: 50, wavesCleared: 4);
            Assert.IsTrue(first);
            Assert.AreEqual(RunOutcome.Victory, coordinator.LastFinalizedResult.Outcome);
            Assert.AreEqual(1000, coordinator.LastFinalizedResult.FinalScore);

            // Attempt to report Defeat on the already completed run
            bool second = coordinator.TryReportRunOutcome(RunOutcome.Defeat, score: 200, kills: 10, wavesCleared: 1);
            Assert.IsFalse(second, "Second outcome report must be rejected.");
            Assert.AreEqual(RunOutcome.Victory, coordinator.LastFinalizedResult.Outcome, "Outcome must remain Victory.");
            Assert.AreEqual(1000, coordinator.LastFinalizedResult.FinalScore, "Score must remain unchanged.");
        }

        [Test]
        public void TerminalResult_IsIdempotent_CannotOverwriteDefeatWithAbandoned()
        {
            var coordinator = new GameFlowCoordinator();
            coordinator.TryDeploy(new DeployCommand("arena_sweep"), out int token);
            coordinator.NotifyMissionLoaded(token);
            coordinator.NotifyMissionStarted(token);

            bool first = coordinator.TryReportRunOutcome(RunOutcome.Defeat, score: 300, kills: 15, wavesCleared: 2);
            Assert.IsTrue(first);

            // Attempt to abandon
            bool second = coordinator.TryReturnToBase();
            Assert.IsTrue(second); // return is accepted, but run result must NOT change
            Assert.AreEqual(RunOutcome.Defeat, coordinator.LastFinalizedResult.Outcome);
            Assert.AreEqual(300, coordinator.LastFinalizedResult.FinalScore);
        }

        [Test]
        public void RunAccountingRules_AreConsistentWithPlan()
        {
            // Victory: counts as run, increments wins, not losses, records score/kills
            Assert.IsTrue(RunOutcome.Victory.CountsAsRun());
            Assert.IsTrue(RunOutcome.Victory.IncrementsWins());
            Assert.IsFalse(RunOutcome.Victory.IncrementsLosses());
            Assert.IsTrue(RunOutcome.Victory.RecordsScoreAndKills());

            // Defeat: counts as run, not wins, increments losses, records score/kills
            Assert.IsTrue(RunOutcome.Defeat.CountsAsRun());
            Assert.IsFalse(RunOutcome.Defeat.IncrementsWins());
            Assert.IsTrue(RunOutcome.Defeat.IncrementsLosses());
            Assert.IsTrue(RunOutcome.Defeat.RecordsScoreAndKills());

            // Abandoned: counts as run, neither win nor loss, no score/kills
            Assert.IsTrue(RunOutcome.Abandoned.CountsAsRun());
            Assert.IsFalse(RunOutcome.Abandoned.IncrementsWins());
            Assert.IsFalse(RunOutcome.Abandoned.IncrementsLosses());
            Assert.IsFalse(RunOutcome.Abandoned.RecordsScoreAndKills());

            // TechnicalError: does not count as run, neither win nor loss, no score/kills
            Assert.IsFalse(RunOutcome.TechnicalError.CountsAsRun());
            Assert.IsFalse(RunOutcome.TechnicalError.IncrementsWins());
            Assert.IsFalse(RunOutcome.TechnicalError.IncrementsLosses());
            Assert.IsFalse(RunOutcome.TechnicalError.RecordsScoreAndKills());
        }

        [Test]
        public void RunSession_ElapsedActiveTime_AccumulatesCorrectly_OnlyWhileRunning()
        {
            var session = new RunSession("run_1", "arena_sweep");
            session.RecordActiveTime(0.5f);
            session.RecordActiveTime(1.2f);
            Assert.AreEqual(1.7f, session.ElapsedActiveTime, 0.001f);

            session.TryComplete(RunOutcome.Victory);
            // Ignored after completion
            session.RecordActiveTime(5.0f);
            Assert.AreEqual(1.7f, session.ElapsedActiveTime, 0.001f);
            Assert.AreEqual(1.7f, session.FinalResult.ElapsedActiveTime, 0.001f);
        }

        #endregion

        #region Helpers

        private static DeploymentLoadoutSnapshot CreateSampleLoadout()
        {
            var builds = new Dictionary<string, WeaponBuild>
            {
                { "rifle", new WeaponBuild("rifle") }
            };
            return new DeploymentLoadoutSnapshot(new[] { "rifle" }, "rifle", builds);
        }

        private static void SetManagerOrigin(GameStateManager manager, GameState target)
        {
            switch (target)
            {
                case GameState.Menu:
                    manager.ReturnToMenu();
                    break;
                case GameState.Playing:
                    manager.StartGame();
                    break;
                case GameState.Paused:
                    manager.StartGame();
                    manager.Pause();
                    break;
                case GameState.GameOver:
                    manager.StartGame();
                    manager.EndGame();
                    break;
                case GameState.Victory:
                    manager.StartGame();
                    manager.TriggerVictory();
                    break;
                case GameState.WorkshopRoaming:
                    manager.EnterWorkshopRoaming();
                    break;
                case GameState.WorkshopEditing:
                    manager.EnterWorkshopRoaming();
                    manager.EnterWorkshopEditing();
                    break;
                case GameState.WorkshopFiringRange:
                    manager.EnterWorkshopRoaming();
                    manager.EnterWorkshopFiringRange();
                    break;
                case GameState.DeploymentBriefing:
                    manager.EnterWorkshopRoaming();
                    manager.EnterDeploymentBriefing();
                    break;
                case GameState.Loading:
                    manager.EnterLoading();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        #endregion
    }
}
