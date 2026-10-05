using System;
using System.IO;
using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Loads and writes jumpnrun_save.json in persistentDataPath (one shared instance).</summary>
    public static class JumpNRunSaveStore
    {
        private static JumpNRunSave current;
        private static bool broken;

        /// <summary>Tools (smoke test) switch writing off so they never touch the player's progress.</summary>
        public static bool ReadOnly { get; set; }

        public static string SavePath => Path.Combine(Application.persistentDataPath, JumpNRunSave.FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            broken = false;
            ReadOnly = false;
        }

        public static JumpNRunSave Current
        {
            get
            {
                if (current == null)
                {
                    current = Load();
                }

                return current;
            }
        }

        private static JumpNRunSave Load()
        {
            try
            {
                return File.Exists(SavePath) ? JumpNRunSave.FromJson(File.ReadAllText(SavePath)) : new JumpNRunSave();
            }
            catch (Exception e)
            {
                // Keep the unreadable file untouched; play on with a fresh save that is not written back.
                broken = true;
                Debug.LogWarning("[JumpNRun] Save file unreadable, progress is not stored this session: " + e.Message);
                return new JumpNRunSave();
            }
        }

        public static void Save()
        {
            if (broken || ReadOnly || current == null)
            {
                return;
            }

            try
            {
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, current.ToJson());
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[JumpNRun] Save not written: " + e.Message);
            }
        }
    }
}
