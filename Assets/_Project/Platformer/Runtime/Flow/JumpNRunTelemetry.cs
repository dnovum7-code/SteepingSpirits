using System;
using System.IO;
using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Writes one JSON line per level section to
    /// Application.persistentDataPath/jumpnrun_log.jsonl (duration in real
    /// seconds, falls, ingredients, assist settings). Local only, for tuning.
    /// </summary>
    public class JumpNRunTelemetry : MonoBehaviour
    {
        public const string FileName = "jumpnrun_log.jsonl";

        [SerializeField] private bool writeFile = true;

        private SectionTelemetry telemetry;
        private JumpNRunSession session;
        private string path;

        public static string LogPath => Path.Combine(Application.persistentDataPath, FileName);

        private void Start()
        {
            session = JumpNRunSession.Current;
            JumpNRunLevel level = JumpNRunLevel.Current;
            if (session == null)
            {
                return;
            }

            path = LogPath;
            string runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + UnityEngine.Random.Range(1000, 9999);
            telemetry = new SectionTelemetry(runId, level != null ? level.levelId : "level", Time.unscaledTime, AssistText());

            session.LanternLit += OnLanternLit;
            session.Caught += OnCaught;
            session.Finish += OnFinish;
            session.Bag.Added += OnIngredient;
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.LanternLit -= OnLanternLit;
                session.Caught -= OnCaught;
                session.Finish -= OnFinish;
                session.Bag.Added -= OnIngredient;
            }

            if (telemetry != null)
            {
                Write(telemetry.Left(Time.unscaledTime));
            }
        }

        private static string AssistText()
        {
            return JumpNRunOptions.Instance != null ? JumpNRunOptions.Instance.Options.Serialize() : "";
        }

        private void OnLanternLit(int index)
        {
            telemetry.Assists(AssistText());
            Write(telemetry.LanternLit(index, Time.unscaledTime));
        }

        private void OnCaught() => telemetry.Fall();

        private void OnIngredient(string id, int amount)
        {
            for (int i = 0; i < amount; i++) telemetry.Ingredient(id);
        }

        private void OnFinish()
        {
            telemetry.Assists(AssistText());
            Write(telemetry.Goal(Time.unscaledTime));
        }

        private void Write(SectionRecord record)
        {
            if (record == null || !writeFile)
            {
                return;
            }

            string line = SectionTelemetry.ToJson(record, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            try
            {
                File.AppendAllText(path, line + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[JumpNRun] Telemetry not written: " + e.Message);
            }
        }
    }
}
