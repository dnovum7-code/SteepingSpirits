using UnityEditor;
using UnityEngine;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>
    /// Creates the Jump'n'Run prefabs from code. Prefabs are regenerated on every
    /// build (they contain no hand-tuned values; tuning lives in the tuning assets).
    /// </summary>
    public static class JumpNRunPrefabs
    {
        public const string Folder = "Assets/_Project/Platformer/Prefabs";
        public const string PlayerPath = Folder + "/JumpNRunPlayer.prefab";

        public static readonly Vector2 PlayerSize = new Vector2(0.7f, 1.4f);

        public const string ElementFolder = Folder + "/Elements";

        public static void BuildAll()
        {
            JumpNRunBuilder.EnsureFolder(Folder);
            JumpNRunBuilder.EnsureFolder(ElementFolder);
            BuildPlayer();
            Save(JumpNRunElementFactory.CreateLantern(), ElementPath("Lantern"));
            Save(JumpNRunElementFactory.CreateBramble(), ElementPath("Bramble"));
            Save(JumpNRunElementFactory.CreateIngredient(), ElementPath("Ingredient"));
            Save(JumpNRunElementFactory.CreateGoal(), ElementPath("Goal"));
            Save(JumpNRunElementFactory.CreateWindSpirit(), ElementPath("WindSpirit"));
            Save(JumpNRunElementFactory.CreateLanternSpirit(), ElementPath("LanternSpirit"));
            Save(JumpNRunElementFactory.CreateDewLeaf(), ElementPath("DewLeaf"));
            Save(JumpNRunElementFactory.CreateSwing(), ElementPath("PlaygroundSwing"));
            Save(JumpNRunElementFactory.CreateSpiritNpc(), ElementPath("SpiritNpc"));
            Save(JumpNRunElementFactory.CreatePathLantern(), ElementPath("PathLantern"));
        }

        public static string ElementPath(string name) => ElementFolder + "/" + name + ".prefab";

        public static GameObject Element(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ElementPath(name));

        public static GameObject Player => AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);

        private static void BuildPlayer()
        {
            var go = new GameObject("JumpNRunPlayer");
            go.tag = "Player";

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = PlayerSize;
            box.edgeRadius = 0.02f;

            var player = go.AddComponent<JumpNRunPlayer>();
            player.Configure(AssetDatabase.LoadAssetAtPath<MovementTuning>(JumpNRunBuilder.DataFolder + "/MovementTuning.asset"));

            go.AddComponent<JumpNRunFeedback>();

            var visual = go.AddComponent<PlaceholderVisual>();
            visual.shape = PlaceholderVisual.Shape.Square;
            visual.color = new Color(0.96f, 0.86f, 0.70f);
            visual.size = PlayerSize;
            visual.sortingOrder = 10;

            Save(go, PlayerPath);
        }

        public static GameObject Save(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Places a prefab instance (keeps the prefab link).</summary>
        public static GameObject Place(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }
    }
}
