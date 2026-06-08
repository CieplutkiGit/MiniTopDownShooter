using System;
using UnityEngine;

namespace Game
{
    [Serializable]
    public struct WeightedPrefab
    {
        public EnemyController Prefab;

        [Min(1)]
        public int Weight;
    }
}
