using System;
using System.Reflection;
using Application.Flow;
using Game;
using Game.Flow;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Flow
{
    public class MissionRunTrackingTests
    {
        [Test]
        public void VictorySnapshotContainsKillsAndOnlyCompletedWaves()
        {
            var appObject = new GameObject("TrackingApp");
            var missionObject = new GameObject("TrackingMission");
            var waveObject = new GameObject("TrackingWaves");
            waveObject.SetActive(false);
            try
            {
                var app = appObject.AddComponent<AppCompositionRoot>();
                SetField(app, "<FlowCoordinator>k__BackingField", new GameFlowCoordinator());
                app.FlowCoordinator.StartDirectMission("tracking");
                var state = missionObject.AddComponent<GameStateController>();
                var spawner = missionObject.AddComponent<EnemySpawner>();
                var waves = waveObject.AddComponent<WaveController>();
                var mission = missionObject.AddComponent<MissionRunController>();
                SetField(mission, "_app", app);
                SetField(mission, "_activeRun", app.FlowCoordinator.ActiveRun);
                SetField(mission, "_gameStateController", state);
                SetField(mission, "_enemySpawner", spawner);
                SetField(mission, "_waveController", waves);
                mission.StartMission();

                spawner.NotifyEnemyKilled(10);
                spawner.NotifyEnemyKilled(10);
                var completed = (Action<int>)typeof(WaveController)
                    .GetField("WaveCompleted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(waves);
                completed.Invoke(1);
                state.TriggerVictory();

                RunResult result = app.FlowCoordinator.LastFinalizedResult;
                Assert.IsNotNull(result);
                Assert.AreEqual(RunOutcome.Victory, result.Outcome);
                Assert.AreEqual(2, result.TotalKills);
                Assert.AreEqual(1, result.WavesCleared);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(missionObject);
                UnityEngine.Object.DestroyImmediate(waveObject);
                UnityEngine.Object.DestroyImmediate(appObject);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
