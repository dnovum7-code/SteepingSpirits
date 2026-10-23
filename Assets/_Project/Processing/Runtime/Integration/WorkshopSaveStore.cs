using System;
using System.IO;
using UnityEngine;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// persistentDataPath/workshop_save.json – written via a temporary file; a broken or newer file
    /// is moved aside (workshop_save.broken.json) instead of being overwritten.
    /// </summary>
    public static class WorkshopSaveStore
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, WorkshopSave.FileName);

        public static bool DryRun { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DryRun = false;

        public static WorkshopSave Load()
        {
            string path = SavePath;
            if (DryRun || !File.Exists(path)) return null;
            try
            {
                return WorkshopSave.FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Processing] workshop_save.json could not be read (" + e.Message + ") – moved aside, starting fresh.");
                try
                {
                    string aside = Path.Combine(Application.persistentDataPath, "workshop_save.broken.json");
                    if (File.Exists(aside)) File.Delete(aside);
                    File.Move(path, aside);
                }
                catch (Exception)
                {
                    // Keep going; the next save overwrites the broken file.
                }

                return null;
            }
        }

        public static bool Save(WorkshopSave save)
        {
            if (DryRun || save == null) return true;
            string path = SavePath, tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, save.ToJson());
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Processing] Workshop not saved: " + e.Message);
                return false;
            }
        }

        /// <summary>One line per finished batch in processing_log.jsonl.</summary>
        public static void Log(ProcessedTea tea, double gameMinutes)
        {
            if (DryRun || tea == null) return;
            try
            {
                File.AppendAllText(Path.Combine(Application.persistentDataPath, "processing_log.jsonl"), ProcessingLog.Line(tea, gameMinutes) + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Processing] Telemetry not written: " + e.Message);
            }
        }
    }
}
