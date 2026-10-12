using System.Collections.Generic;

namespace SteepingSpirits.Plantation.Tests
{
    /// <summary>Weather with exactly the given rainy days.</summary>
    public sealed class FixedWeather : IWeather
    {
        private readonly HashSet<int> rainy;

        public FixedWeather(params int[] rainyDays)
        {
            rainy = new HashSet<int>(rainyDays);
        }

        public bool IsRainy(int day) => rainy.Contains(day);
    }

    internal static class GardenTestKit
    {
        public static readonly GridPos A = new GridPos(0, 0);
        public static readonly GridPos B = new GridPos(1, 0);

        public static PlantParams Plant(string id, WaterNeed water, bool perennial = false, params int[] stageDays) => new PlantParams
        {
            id = id,
            ingredientId = id + "_leaf",
            seedItemId = "seed_" + id,
            stageDays = stageDays.Length > 0 ? stageDays : new[] { 2, 2 },
            water = water,
            perennial = perennial,
            regrowDays = 2,
            yieldMin = 2,
            yieldMax = 2,
            seedDropChance = 0f
        };

        public static PlantBook Book(params PlantParams[] plants)
        {
            var book = new PlantBook();
            foreach (PlantParams p in plants) book.Add(p);
            if (plants.Length == 0)
            {
                book.Add(Plant("thirsty", WaterNeed.Thirsty));
                book.Add(Plant("normal", WaterNeed.Normal));
                book.Add(Plant("frugal", WaterNeed.Frugal));
                book.Add(Plant("bush", WaterNeed.Normal, true));
                book.Add(Plant("fbush", WaterNeed.Frugal, true));
            }

            return book;
        }

        public static Garden Make(PlantBook book = null, GardenParams settings = null, IWeather weather = null, int beds = 2)
        {
            var cells = new List<GridPos>();
            for (int i = 0; i < beds; i++) cells.Add(new GridPos(i, 0));
            return new Garden(cells, settings ?? new GardenParams(), book ?? Book(), 1, weather ?? new FixedWeather());
        }

        /// <summary>Till and sow plant at p.</summary>
        public static void Plant(Garden g, GridPos p, string plantId)
        {
            g.Apply(p, GardenAction.Hoe);
            g.Apply(p, GardenAction.Sow, plantId, 1);
        }

        /// <summary>Run nights; water(day) decides whether p is watered that day.</summary>
        public static void Nights(Garden g, GridPos p, int n, System.Func<int, bool> water)
        {
            for (int i = 0; i < n; i++)
            {
                if (water(g.Day))
                {
                    g.Refill();
                    g.Apply(p, GardenAction.Water);
                }

                g.EndDay();
            }
        }

        /// <summary>Nights until the crop at p is ripe (or max); returns nights used.</summary>
        public static int UntilRipe(Garden g, GridPos p, System.Func<int, bool> water, int max = 60)
        {
            int n = 0;
            while (n < max && !(g.PlotAt(p).crop?.ripe ?? false))
            {
                Nights(g, p, 1, water);
                n++;
            }

            return n;
        }

        public static bool Always(int day) => true;
        public static bool Never(int day) => false;
        public static bool EveryOther(int day) => day % 2 == 1;
    }
}
