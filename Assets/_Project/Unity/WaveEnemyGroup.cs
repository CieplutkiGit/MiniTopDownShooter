using System;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class WaveEnemyGroup
    {
        [Tooltip("Enemy prefab used by this group.")]
        public EnemyController Prefab;

        [Min(0)]
        [Tooltip("Guaranteed instances placed into the wave before weighted fill.")]
        public int GuaranteedCount;

        [Min(1)]
        [Tooltip("Relative chance when filling the remaining wave slots.")]
        public int Weight = 1;

        [Min(0f)]
        [Tooltip("Optional pause before the first guaranteed enemy from this group.")]
        public float DelayBefore;
    }
}
