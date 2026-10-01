using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class WaveRunnerTests
    {
        [Test]
        public void StartWaves_RaisesFirstWaveStarted()
        {
            WaveRunner runner = new WaveRunner(new[]
            {
                new Wave(1, 1f, 0f)
            });

            int startedWave = 0;
            runner.WaveStarted += waveNumber => startedWave = waveNumber;

            runner.StartWaves();

            Assert.IsTrue(runner.IsRunning);
            Assert.AreEqual(1, runner.CurrentWaveNumber);
            Assert.AreEqual(1, startedWave);
        }

        [Test]
        public void Tick_RequestsSpawnUntilWaveEnemyCountIsReached()
        {
            WaveRunner runner = new WaveRunner(new[]
            {
                new Wave(2, 1f, 0f)
            });

            int spawnRequests = 0;
            runner.SpawnRequested += () => spawnRequests++;
            runner.StartWaves();

            runner.Tick(0f);
            runner.NotifyEnemySpawned();
            runner.Tick(1f);

            Assert.AreEqual(2, spawnRequests);
        }

        [Test]
        public void CompletingFinalWave_RaisesCompletionEventsAndStops()
        {
            WaveRunner runner = new WaveRunner(new[]
            {
                new Wave(1, 0f, 0f)
            });

            int completedWave = 0;
            int allCompletedCount = 0;

            runner.WaveCompleted += waveNumber => completedWave = waveNumber;
            runner.AllWavesCompleted += () => allCompletedCount++;

            runner.StartWaves();
            runner.Tick(0f);
            runner.NotifyEnemySpawned();
            runner.NotifyEnemyKilled();
            runner.Tick(0f);

            Assert.AreEqual(1, completedWave);
            Assert.AreEqual(1, allCompletedCount);
            Assert.IsFalse(runner.IsRunning);
        }
    }
}
