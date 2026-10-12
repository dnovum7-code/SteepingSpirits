using System;
using System.IO;
using UnityEngine;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// One line per garden day in persistentDataPath/garden_log.jsonl: actions, wet cells, harvests,
    /// time in the garden. The open day survives scene changes (static), so leaving and coming back
    /// on the same day still gives one line. Nights caught up elsewhere are marked "caughtUp".
    /// </summary>
    public class GardenTelemetry : MonoBehaviour
    {
        public const string FileName = "garden_log.jsonl";

        private static GardenDayLog open;
        private TeaGarden root;
        private bool byLantern;

        public static string LogPath => Path.Combine(Application.persistentDataPath, FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => open = null;

        public void Init(TeaGarden garden)
        {
            root = garden;
            if (open == null || open.day != root.Garden.Day) open = new GardenDayLog { day = root.Garden.Day, rainy = root.Garden.RainyToday };
            root.ActionApplied += OnAction;
            root.NightPassed += OnNight;
            root.DayEnding += OnDayEnding;
        }

        private void OnDestroy()
        {
            if (root == null) return;
            root.ActionApplied -= OnAction;
            root.NightPassed -= OnNight;
            root.DayEnding -= OnDayEnding;
        }

        private void Update()
        {
            if (open != null && root != null && !root.Busy) open.secondsInGarden += Time.unscaledDeltaTime;
        }

        private void OnAction(ActionOutcome o, GardenAction a) => open?.Count(o, Time.unscaledTime);

        private void OnDayEnding() => byLantern = true;

        private void OnNight(DayReport night)
        {
            GardenDayLog log = open != null && open.day == night.day ? open : new GardenDayLog { day = night.day };
            log.caughtUp = !byLantern;
            log.Close(night, root.Garden);
            Write(log.ToJsonLine());
            byLantern = false;
            open = new GardenDayLog { day = night.day + 1, rainy = night.rainyNext };
        }

        private static void Write(string line)
        {
            if (GardenSaveStore.DryRun) return;
            try
            {
                File.AppendAllText(LogPath, line + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Plantation] Telemetry not written: " + e.Message);
            }
        }
    }
}
