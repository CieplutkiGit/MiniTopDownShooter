using System;

namespace Application
{
    public interface IWaveProvider
    {
        int CurrentWaveNumber { get; }
        event Action<int> WaveStarted;
        event Action<int> WaveCompleted;
        event Action AllWavesCompleted;
    }
}
