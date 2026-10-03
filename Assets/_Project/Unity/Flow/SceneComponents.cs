using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    internal static class SceneComponents
    {
        public static T Find<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }
    }
}
