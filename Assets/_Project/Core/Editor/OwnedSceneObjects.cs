using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteepingSpirits.Core.EditorTools
{
    /// <summary>
    /// Builders share some scenes (the meadow gets the garden entrance, the garden gets the workshop
    /// door). Rule: a builder never recreates a shared scene from scratch – it opens it and replaces
    /// only the root objects it owns (by name). Then the build order no longer matters.
    /// </summary>
    public static class OwnedSceneObjects
    {
        /// <summary>Opens the scene if it exists (single), otherwise creates an empty one.</summary>
        public static Scene OpenOrNew(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>Removes the root objects with these names (what this builder created earlier); others stay.</summary>
        public static int RemoveOwned(Scene scene, params string[] names)
        {
            var owned = new HashSet<string>(names);
            int removed = 0;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (!owned.Contains(go.name)) continue;
                Object.DestroyImmediate(go);
                removed++;
            }

            return removed;
        }

        /// <summary>Open (or create) the scene and clear this builder's objects in one step.</summary>
        public static Scene OpenForBuilder(string path, params string[] ownedNames)
        {
            Scene scene = OpenOrNew(path);
            RemoveOwned(scene, ownedNames);
            return scene;
        }
    }
}
