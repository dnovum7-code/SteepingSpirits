using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SteepingSpirits.Quests.Data;

namespace SteepingSpirits.Quests.Editor
{
    /// <summary>
    /// Editor-Werkzeug: Quest-JSON einfügen (oder Datei laden), validieren und
    /// als QuestData-Asset auf Disk schreiben. Nutzt AssetDatabase und existiert
    /// deshalb nur im Editor (Editor-Ordner). Ersetzt den „Quest Generator" aus
    /// Everdawn – ohne KI und ohne key:value-Textformat, nur sauberes JSON.
    ///
    /// Menü: SteepingSpirits → Quest JSON Importer
    /// </summary>
    public class QuestJsonImporterWindow : EditorWindow
    {
        private const string DefaultBaseFolder = "Assets/_Project/Quests/Data";

        private string json = "";
        private string baseFolder = DefaultBaseFolder;
        private Vector2 scrollPosition;
        private readonly List<string> messages = new List<string>();
        private bool lastValidationPassed;

        [MenuItem("SteepingSpirits/Quest JSON Importer")]
        public static void Open()
        {
            GetWindow<QuestJsonImporterWindow>("Quest JSON Importer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Quest-JSON (Schema siehe Data/Samples)", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(200));
            json = EditorGUILayout.TextArea(json, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            baseFolder = EditorGUILayout.TextField("Ziel-Ordner", baseFolder);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("JSON-Datei laden…"))
            {
                LoadJsonFromFile();
            }

            if (GUILayout.Button("Validieren"))
            {
                Validate();
            }

            GUI.enabled = !string.IsNullOrEmpty(json);
            if (GUILayout.Button("QuestData-Asset erstellen"))
            {
                CreateAsset();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            foreach (string message in messages)
            {
                EditorGUILayout.HelpBox(message, lastValidationPassed ? MessageType.Info : MessageType.Error);
            }
        }

        private void LoadJsonFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Quest-JSON auswählen", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                json = System.IO.File.ReadAllText(path);
                messages.Clear();
            }
        }

        private bool Validate()
        {
            messages.Clear();

            lastValidationPassed = QuestJsonValidator.TryParse(json, out QuestDefinition definition, out List<string> errors);
            if (lastValidationPassed)
            {
                messages.Add($"Gültig: {definition.questID} ({definition.objectives.Length} Objectives).");
            }
            else
            {
                messages.AddRange(errors);
            }

            return lastValidationPassed;
        }

        private void CreateAsset()
        {
            if (!Validate())
            {
                return;
            }

            QuestJsonValidator.TryParse(json, out QuestDefinition definition, out _);
            QuestData asset = QuestJsonLoader.CreateFromDefinition(definition);

            string folder = baseFolder + "/" + CategoryFolderName(asset.category);
            EnsureFolder(folder);

            string assetPath = folder + "/" + asset.questID + ".asset";
            if (AssetDatabase.LoadAssetAtPath<QuestData>(assetPath) != null)
            {
                lastValidationPassed = false;
                messages.Clear();
                messages.Add($"Asset existiert bereits: {assetPath} – erst löschen oder questID ändern.");
                return;
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);

            messages.Clear();
            lastValidationPassed = true;
            messages.Add("Asset erstellt: " + assetPath);
        }

        private static string CategoryFolderName(QuestCategory category)
        {
            string name = category.ToString(); // z.B. "SIDE" → Ordner "Side"
            return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
