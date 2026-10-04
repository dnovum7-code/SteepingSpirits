using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Telemetry
{
    /// <summary>
    /// Appends one JSON line per finished infusion to
    /// Application.persistentDataPath/brew_log.jsonl – for tuning, not for players.
    /// </summary>
    public static class BrewTelemetry
    {
        public const string FileName = "brew_log.jsonl";

        [Serializable]
        private struct Record
        {
            public string timestamp;
            public string tea;
            public int infusion;
            public float pourTemperature;
            public float steepStartTemperature;
            public bool prewarmed;
            public bool staleWater;
            public float steepSeconds;
            public float aroma;
            public float bitterness;
            public float harmony;
            public string tier;
            public bool tart;
            public float totalSeconds;
            public bool thermometerUsed;
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static string ToJson(BrewResult r)
        {
            var record = new Record
            {
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                tea = r.TeaId,
                infusion = r.InfusionIndex,
                pourTemperature = Round(r.PourTemperature),
                steepStartTemperature = Round(r.SteepStartTemperature),
                prewarmed = r.Prewarmed,
                staleWater = r.StaleWater,
                steepSeconds = Round(r.SteepSeconds),
                aroma = Round(r.Aroma),
                bitterness = Round(r.Bitterness),
                harmony = Round(r.Harmony),
                tier = r.Tier.ToString(),
                tart = r.IsTart,
                totalSeconds = Round(r.TotalSeconds),
                thermometerUsed = r.ThermometerUsed
            };

            return JsonUtility.ToJson(record);
        }

        public static void Append(BrewResult result)
        {
            if (result == null)
            {
                return;
            }

            try
            {
                File.AppendAllText(FilePath, ToJson(result) + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BrewTelemetry] Could not write log: {e.Message}");
            }
        }

        private static float Round(float v) => (float)Math.Round(v, 3);
    }
}
