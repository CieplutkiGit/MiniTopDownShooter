using System;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    [Serializable]
    public struct WeightedPrefab
    {
        public EnemyController Prefab;

        [Min(1)]
        public int Weight;
    }
}
