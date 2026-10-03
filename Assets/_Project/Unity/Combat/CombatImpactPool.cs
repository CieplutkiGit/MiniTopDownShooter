using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    /// <summary>
    /// Enforces bounded pools and per-surface budgets for persistent 3D impact marks.
    /// Recycles oldest marks (FIFO) when caps are exceeded.
    /// Resets cleanly on respawn and scene transitions.
    /// </summary>
    public class CombatImpactPool : MonoBehaviour
    {
        [SerializeField] private int _globalMaxMarks = 120;
        [SerializeField] private int _maxMarksPerSurface = 10;

        private static CombatImpactPool s_instance;
        private static bool s_isQuitting;

        private readonly Queue<CombatImpactMark> _available = new Queue<CombatImpactMark>();
        private readonly List<CombatImpactMark> _activeMarks = new List<CombatImpactMark>();
        private readonly Dictionary<Transform, List<CombatImpactMark>> _marksByTarget = new Dictionary<Transform, List<CombatImpactMark>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_isQuitting = false;
            s_instance = null;
            CombatImpactMark.ResetStaticResources();
        }

        public static CombatImpactPool Instance
        {
            get
            {
                if (s_isQuitting) return null;
                if (s_instance == null)
                {
                    s_instance = FindFirstObjectByType<CombatImpactPool>();
                    if (s_instance == null && !s_isQuitting)
                    {
                        GameObject go = new GameObject("CombatImpactPool");
                        s_instance = go.AddComponent<CombatImpactPool>();
                    }
                }
                return s_instance;
            }
        }

        public int ActiveMarkCount => _activeMarks.Count;
        public int AvailableMarkCount => _available.Count;
        public int GlobalMaxMarks => _globalMaxMarks;
        public int MaxMarksPerSurface => _maxMarksPerSurface;

        private void Awake()
        {
            _globalMaxMarks = Mathf.Max(1, _globalMaxMarks);
            _maxMarksPerSurface = Mathf.Clamp(_maxMarksPerSurface, 1, _globalMaxMarks);

            if (s_instance == null)
            {
                s_instance = this;
            }
            UnityEngine.Application.quitting += HandleApplicationQuitting;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private static void HandleApplicationQuitting()
        {
            s_isQuitting = true;
        }

        private void OnDestroy()
        {
            UnityEngine.Application.quitting -= HandleApplicationQuitting;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            if (s_instance == this)
            {
                s_instance = null;
            }
            ClearAndDestroyAll();
            CombatImpactMark.ResetStaticResources();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            ClearAll();
        }

        public static CombatImpactMark SpawnMark(
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            Transform parent,
            float scaleMultiplier = 1f,
            Color? customColor = null)
        {
            if (s_isQuitting) return null;
            CombatImpactPool pool = Instance;
            if (pool == null) return null;
            return pool.GetOrCreateMark(position, normal, direction, parent, scaleMultiplier, customColor);
        }

        public static void ClearMarksFor(Transform parent)
        {
            if (s_instance != null && parent != null)
            {
                s_instance.InternalClearMarksFor(parent);
            }
        }

        public CombatImpactMark GetOrCreateMark(
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            Transform parent,
            float scaleMultiplier = 1f,
            Color? customColor = null)
        {
            _globalMaxMarks = Mathf.Max(1, _globalMaxMarks);
            _maxMarksPerSurface = Mathf.Clamp(_maxMarksPerSurface, 1, _globalMaxMarks);

            // Prune dead/destroyed parent targets
            PruneDeadTargets();

            // Enforce per-surface limit first
            if (parent != null && _marksByTarget.TryGetValue(parent, out List<CombatImpactMark> targetList))
            {
                while (targetList.Count >= _maxMarksPerSurface && targetList.Count > 0)
                {
                    CombatImpactMark oldestOnTarget = targetList[0];
                    targetList.RemoveAt(0);
                    if (oldestOnTarget != null)
                    {
                        oldestOnTarget.Recycle();
                    }
                }
            }

            // Enforce global cap
            while (_activeMarks.Count >= _globalMaxMarks && _activeMarks.Count > 0)
            {
                CombatImpactMark oldest = _activeMarks[0];
                _activeMarks.RemoveAt(0);
                if (oldest != null)
                {
                    oldest.Recycle();
                }
            }

            CombatImpactMark mark = null;
            while (_available.Count > 0 && mark == null)
            {
                mark = _available.Dequeue();
            }

            if (mark == null)
            {
                GameObject markGo = new GameObject("ImpactMark");
                markGo.transform.SetParent(transform, false);
                mark = markGo.AddComponent<CombatImpactMark>();
            }

            mark.Initialize(position, normal, direction, parent, HandleMarkRecycled, scaleMultiplier, customColor);
            _activeMarks.Add(mark);

            if (parent != null)
            {
                if (!_marksByTarget.TryGetValue(parent, out List<CombatImpactMark> list))
                {
                    list = new List<CombatImpactMark>();
                    _marksByTarget[parent] = list;
                }
                if (!list.Contains(mark))
                {
                    list.Add(mark);
                }
            }

            return mark;
        }

        private void HandleMarkRecycled(CombatImpactMark mark)
        {
            if (mark == null) return;

            _activeMarks.Remove(mark);

            Transform target = mark.TargetTransform;
            if (target != null && _marksByTarget.TryGetValue(target, out List<CombatImpactMark> list))
            {
                list.Remove(mark);
                if (list.Count == 0)
                {
                    _marksByTarget.Remove(target);
                }
            }

            mark.transform.SetParent(transform, false);
            if (!_available.Contains(mark))
            {
                _available.Enqueue(mark);
            }
        }

        private void PruneDeadTargets()
        {
            List<Transform> deadKeys = null;
            foreach (var kvp in _marksByTarget)
            {
                if (kvp.Key == null)
                {
                    if (deadKeys == null) deadKeys = new List<Transform>();
                    deadKeys.Add(kvp.Key);
                }
            }
            if (deadKeys != null)
            {
                for (int i = 0; i < deadKeys.Count; i++)
                {
                    List<CombatImpactMark> list = _marksByTarget[deadKeys[i]];
                    if (list != null)
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            if (list[j] != null) list[j].Recycle();
                        }
                    }
                    _marksByTarget.Remove(deadKeys[i]);
                }
            }
        }

        private void InternalClearMarksFor(Transform parent)
        {
            if (parent == null) return;

            if (_marksByTarget.TryGetValue(parent, out List<CombatImpactMark> list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null)
                    {
                        list[i].Recycle();
                    }
                }
                _marksByTarget.Remove(parent);
            }
        }

        public void ClearAll()
        {
            for (int i = _activeMarks.Count - 1; i >= 0; i--)
            {
                if (_activeMarks[i] != null)
                {
                    _activeMarks[i].Recycle();
                }
            }
            _activeMarks.Clear();
            _marksByTarget.Clear();
        }

        public void ClearAndDestroyAll()
        {
            for (int i = 0; i < _activeMarks.Count; i++)
            {
                if (_activeMarks[i] != null)
                {
                    SafeDestroy(_activeMarks[i].gameObject);
                }
            }
            _activeMarks.Clear();
            _marksByTarget.Clear();

            while (_available.Count > 0)
            {
                CombatImpactMark mark = _available.Dequeue();
                if (mark != null)
                {
                    SafeDestroy(mark.gameObject);
                }
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
