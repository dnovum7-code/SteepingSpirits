using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// Loads garden ↔ workshop without ScenePortal: ScenePortal remembers the previous scene, and the
    /// garden gate returns there. Going through the workshop must not change that, so the gate keeps
    /// leading to the meadow.
    /// </summary>
    public static class WorkshopSceneLoader
    {
        public static bool CanLoad(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
#if UNITY_EDITOR
            return System.IO.File.Exists(path);
#else
            return Application.CanStreamedLevelBeLoaded(System.IO.Path.GetFileNameWithoutExtension(path));
#endif
        }

        public static void Load(string path)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(path));
#endif
        }
    }
}
