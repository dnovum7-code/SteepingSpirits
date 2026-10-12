using System;
using System.IO;
using UnityEngine;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Reads and writes persistentDataPath/garden_save.json. Writes go to a temporary file first,
    /// so a crash never leaves half a save. A broken or newer file is moved aside
    /// (garden_save.broken.json) instead of being overwritten.
    /// </summary>
    public static class GardenSaveStore
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, GardenSave.FileName);

        /// <summary>Tools and tests switch writing off so they never touch the real save.</summary>
        public static bool DryRun { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DryRun = false;

        /// <summary>The saved garden, or null when there is none (or it could not be read).</summary>
        public static GardenSave Load()
        {
            string path = SavePath;
            if (DryRun || !File.Exists(path)) return null;
            try
            {
                return GardenSave.FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Plantation] garden_save.json could not be read (" + e.Message + ") – moved aside, starting fresh.");
                try
                {
                    string aside = Path.Combine(Application.persistentDataPath, "garden_save.broken.json");
                    if (File.Exists(aside)) File.Delete(aside);
                    File.Move(path, aside);
                }
                catch (Exception)
                {
                    // Keep going with a fresh garden; the next save overwrites the broken file.
                }

                return null;
            }
        }

        public static bool Save(GardenSave save)
        {
            if (DryRun || save == null) return true;
            string path = SavePath;
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, save.ToJson());
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Plantation] Garden not saved: " + e.Message);
                return false;
            }
        }
    }
}
