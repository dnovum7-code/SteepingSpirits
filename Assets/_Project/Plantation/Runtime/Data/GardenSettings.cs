using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Plantation
{
    /// <summary>Comfort options of the garden.</summary>
    [Serializable]
    public class GardenComfortParams
    {
        [Tooltip("Players may switch the watering can to 1x3 (front cell and the two behind it)")]
        public bool wideCanAvailable = true;

        [Tooltip("1x3 watering on for a new garden")]
        public bool wideCanDefault;

        [Tooltip("Garden help (drop above cells that want water today) on for a new garden")]
        public bool gardenHelpDefault = true;

        [Tooltip("Mouse: a cell under the cursor is targeted when it is this close to the player (units)")]
        public float mouseReach = 1.7f;

        [Tooltip("Seconds between two tool uses while the key is held")]
        public float repeatSeconds = 0.28f;

        [Tooltip("Seconds the lantern waits for the second press that ends the day")]
        public float endDayConfirmSeconds = 3f;
    }

    /// <summary>Soft feedback values (no screen shake, no bright flashes).</summary>
    [Serializable]
    public class GardenFeedbackParams
    {
        [Range(0f, 1f)] public float volume = 0.45f;

        [Tooltip("Pitch variation of repeated sounds")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.08f;

        [Tooltip("Plants grow from this scale to full size after a night")]
        [Range(0.5f, 1f)] public float growPopFrom = 0.82f;

        public float growPopSeconds = 0.5f;

        [Tooltip("Seconds of one slow sparkle cycle on ripe plants")]
        public float sparklePeriod = 2.6f;

        [Range(0f, 1f)] public float sparkleAlpha = 0.5f;

        [Tooltip("Harvested pieces hop to the player in this time")]
        public float hopSeconds = 0.5f;

        [Tooltip("Upper limit of particles alive at once")]
        public int particleBudget = 80;

        [Tooltip("Seconds of the soft fade when the day ends")]
        public float dayFadeSeconds = 0.7f;
    }

    /// <summary>
    /// All garden tuning in one asset: growth and quality limits, can size, rain, the plants and
    /// comfort/feedback values. Created by the builder; tune it in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/Plantation/Garden Settings", fileName = "GardenSettings")]
    public class GardenSettings : ScriptableObject
    {
        public GardenParams garden = new GardenParams();
        public PlantDefinition[] plants = new PlantDefinition[0];
        public GardenComfortParams comfort = new GardenComfortParams();
        public GardenFeedbackParams feedback = new GardenFeedbackParams();

        [Tooltip("Inventory items that can be turned into compost at the compost bin (e.g. over-steeped tart tea)")]
        public string[] compostInputIds = { "tea_tart" };

        /// <summary>The plant book for the simulation; the starter plants when no definitions are set.</summary>
        public PlantBook BuildBook()
        {
            var book = new PlantBook();
            if (plants != null)
            {
                foreach (PlantDefinition d in plants)
                {
                    if (d != null && d.plant != null && !string.IsNullOrEmpty(d.plant.id)) book.Add(d.plant);
                }
            }

            if (book.All.Count == 0) book = StarterPlants.Book();
            return book;
        }

        /// <summary>Definition (look) of a plant, or null.</summary>
        public PlantDefinition Definition(string plantId)
        {
            if (plants == null) return null;
            foreach (PlantDefinition d in plants)
            {
                if (d != null && d.plant != null && d.plant.id == plantId) return d;
            }

            return null;
        }

        /// <summary>Settings with all defaults (scene started without the builder's assets).</summary>
        public static GardenSettings CreateDefault()
        {
            var s = CreateInstance<GardenSettings>();
            s.name = "GardenSettings (default)";
            var list = new List<PlantDefinition>();
            foreach (string id in StarterPlants.Ids)
            {
                var d = CreateInstance<PlantDefinition>();
                d.name = id;
                d.plant = StarterPlants.Create(id);
                d.leafColor = PlantPalette.Leaf(id);
                d.ripeColor = PlantPalette.Ripe(id);
                list.Add(d);
            }

            s.plants = list.ToArray();
            return s;
        }
    }
}
