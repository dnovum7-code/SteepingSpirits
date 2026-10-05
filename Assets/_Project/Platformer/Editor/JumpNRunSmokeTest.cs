using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>
    /// Menu SteepingSpirits → JumpNRun → Run Smoke Test: builds all levels,
    /// checks each scene statically, plays it with the route bot in play mode
    /// and writes Docs/SMOKE_REPORT.md. State survives the domain reloads of
    /// entering and leaving play mode through SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class JumpNRunSmokeTest
    {
        private const string KeyActive = "JNR.Smoke.Active";
        private const string KeyQueue = "JNR.Smoke.Queue";
        private const string KeyAll = "JNR.Smoke.All";
        private const string KeyStatic = "JNR.Smoke.Static";
        private const string KeyStarted = "JNR.Smoke.Started";
        private const string ResultFolder = "Temp/JumpNRunSmoke";
        public const string ReportPath = "Docs/SMOKE_REPORT.md";

        static JumpNRunSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("SteepingSpirits/JumpNRun/Run Smoke Test")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[JumpNRun] Leave play mode before running the smoke test.");
                return;
            }

            JumpNRunBuilder.BuildAll();

            var scenes = new List<string>();
            foreach (string f in Directory.GetFiles(JumpNRunBuilder.SceneFolder, "JumpNRun_*.unity"))
            {
                scenes.Add(f.Replace('\\', '/'));
            }

            scenes.Sort(StringComparer.Ordinal);
            var staticLines = new StringBuilder();
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (string problem in JumpNRunSceneChecks.Check(scene))
                {
                    staticLines.Append(path).Append('\t').Append(problem).Append('\n');
                }
            }

            if (Directory.Exists(ResultFolder))
            {
                Directory.Delete(ResultFolder, true);
            }

            Directory.CreateDirectory(ResultFolder);
            SessionState.SetString(KeyAll, string.Join("|", scenes));
            SessionState.SetString(KeyQueue, string.Join("|", scenes));
            SessionState.SetString(KeyStatic, staticLines.ToString());
            SessionState.SetString(KeyStarted, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            SessionState.SetBool(KeyActive, true);
            StartNext();
        }

        [MenuItem("SteepingSpirits/JumpNRun/Cancel Smoke Test")]
        public static void Cancel()
        {
            SessionState.SetBool(KeyActive, false);
            SessionState.SetString(KeyQueue, "");
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void StartNext()
        {
            string queue = SessionState.GetString(KeyQueue, "");
            if (string.IsNullOrEmpty(queue))
            {
                SessionState.SetBool(KeyActive, false);
                WriteReport();
                return;
            }

            string[] parts = queue.Split('|');
            SessionState.SetString(KeyQueue, string.Join("|", parts, 1, parts.Length - 1));
            EditorSceneManager.OpenScene(parts[0], OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(KeyActive, false))
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string scene = SceneManager.GetActiveScene().path;
                var bot = new GameObject("JumpNRunSmokeBot").AddComponent<JumpNRunSmokeBot>();
                bot.resultPath = ResultFile(scene);
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += StartNext;
            }
        }

        private static string ResultFile(string scenePath)
        {
            return Path.Combine(ResultFolder, Path.GetFileNameWithoutExtension(scenePath) + ".json");
        }

        private static void WriteReport()
        {
            string[] scenes = SessionState.GetString(KeyAll, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var staticProblems = new Dictionary<string, List<string>>();
            foreach (string line in SessionState.GetString(KeyStatic, "").Split('\n'))
            {
                int tab = line.IndexOf('\t');
                if (tab < 0) continue;
                string scene = line.Substring(0, tab);
                if (!staticProblems.ContainsKey(scene)) staticProblems[scene] = new List<string>();
                staticProblems[scene].Add(line.Substring(tab + 1));
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Smoke-Test Jump'n'Run");
            sb.AppendLine();
            sb.AppendLine($"Gestartet: {SessionState.GetString(KeyStarted, "?")} · Unity {Application.unityVersion}");
            sb.AppendLine();
            sb.AppendLine("Erzeugt von *SteepingSpirits → JumpNRun → Run Smoke Test*: alle Level gebaut, statisch geprüft und");
            sb.AppendLine("im Play-Modus von einem Bot mit echten Eingaben entlang der Route aus der Erreichbarkeitsprüfung gespielt.");
            sb.AppendLine($"„Langsame Frames“ = länger als {JumpNRunSmokeBot.SlowFrameMs:0.0} ms (unter 30 FPS).");
            sb.AppendLine();
            sb.AppendLine("| Szene | Ergebnis | Zeit | Auffangen | Neuplanungen | Zutaten | Exceptions | Fehler | langsame Frames | schlechtester Frame | Ø Frame | statische Befunde |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

            var details = new StringBuilder();
            int failures = 0;
            foreach (string scene in scenes)
            {
                SmokeLevelResult r = Load(scene);
                staticProblems.TryGetValue(scene, out List<string> sp);
                int staticCount = sp != null ? sp.Count : 0;
                string verdict;
                if (r == null) verdict = "❌ kein Ergebnis";
                else if (r.isHub) verdict = r.exceptions + r.errors == 0 ? "✅ Hub läuft" : "⚠️ Hub mit Fehlern";
                else verdict = r.reachedGoal ? "✅ Ziel erreicht" : "❌ Ziel verfehlt";
                bool bad = r == null || (!r.isHub && !r.reachedGoal) || r.exceptions > 0 || staticCount > 0;
                if (bad) failures++;

                string name = Path.GetFileNameWithoutExtension(scene);
                if (r == null)
                {
                    sb.AppendLine($"| {name} | {verdict} | – | – | – | – | – | – | – | – | – | {staticCount} |");
                }
                else
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2:0.0} s | {3} | {4} | {5} | {6} | {7} | {8} | {9:0.0} ms | {10:0.0} ms | {11} |",
                        name, verdict, r.seconds, r.catches, r.replans, r.collected, r.exceptions, r.errors, r.slowFrames,
                        r.worstFrameMs, r.averageFrameMs, staticCount));
                }

                details.AppendLine($"## {name}");
                details.AppendLine();
                if (sp != null)
                {
                    details.AppendLine("Statische Befunde:");
                    foreach (string p in sp) details.AppendLine("- " + p);
                    details.AppendLine();
                }

                if (r != null)
                {
                    if (r.route.Count > 0)
                    {
                        details.AppendLine("Route (letzte Planung): " + string.Join(" · ", r.route));
                        details.AppendLine();
                    }

                    details.AppendLine("Meldungen:");
                    foreach (string m in r.messages) details.AppendLine("- " + m.Replace("|", "/"));
                    details.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine(failures == 0 ? "**Alles grün.**" : $"**{failures} Szene(n) mit Befund.**");
            sb.AppendLine();
            sb.Append(details);

            string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString());
            Debug.Log($"[JumpNRun] Smoke test finished: {(failures == 0 ? "all green" : failures + " scene(s) with findings")} → {ReportPath}");
        }

        private static SmokeLevelResult Load(string scene)
        {
            string file = ResultFile(scene);
            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SmokeLevelResult>(File.ReadAllText(file));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
