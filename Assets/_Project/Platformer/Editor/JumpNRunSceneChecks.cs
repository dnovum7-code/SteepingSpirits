using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>Static checks of a built level scene (missing scripts, broken prefab links, empty references).</summary>
    public static class JumpNRunSceneChecks
    {
        public static List<string> Check(Scene scene)
        {
            var problems = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (missing > 0)
                    {
                        problems.Add($"'{Path(t)}': {missing} missing script(s)");
                    }

                    if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                    {
                        problems.Add($"'{Path(t)}': prefab asset missing");
                    }
                }
            }

            JumpNRunLevel level = Object.FindAnyObjectByType<JumpNRunLevel>();
            if (level == null)
            {
                problems.Add("no JumpNRunLevel in the scene");
                return problems;
            }

            if (level.movementTuning == null) problems.Add("JumpNRunLevel: MovementTuning missing");
            if (level.cameraTuning == null) problems.Add("JumpNRunLevel: CameraTuning missing");
            if (level.feedbackTuning == null) problems.Add("JumpNRunLevel: FeedbackTuning missing");
            if (level.elementsTuning == null) problems.Add("JumpNRunLevel: SpiritElementsTuning missing");
            if (string.IsNullOrEmpty(level.layoutText)) problems.Add("JumpNRunLevel: level text missing (rebuild)");
            if (Object.FindAnyObjectByType<JumpNRunPlayer>() == null) problems.Add("no player in the scene");
            if (Object.FindAnyObjectByType<JumpNRunCamera>() == null) problems.Add("no JumpNRunCamera in the scene");
            return problems;
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }

            return p;
        }
    }
}
