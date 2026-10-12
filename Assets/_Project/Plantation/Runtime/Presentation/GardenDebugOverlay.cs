using System.Text;
using UnityEngine;
using SteepingSpirits.World;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Developer overlay (F1): day and weather, the target cell's soil, growth progress, water history
    /// and expected quality. F2 = "next day" (editor and development builds only).
    /// </summary>
    public class GardenDebugOverlay : MonoBehaviour
    {
        private TeaGarden root;
        private bool visible;
        private readonly StringBuilder sb = new StringBuilder(512);
        private GUIStyle style;

        public void Init(TeaGarden garden) => root = garden;

        private void Update()
        {
            if (root == null || root.Garden == null) return;
            if (GardenInput.DebugTogglePressed) visible = !visible;
            if (GardenInput.DebugNextDayPressed && (Application.isEditor || Debug.isDebugBuild) && !root.Busy)
            {
                root.Hud.Message(GardenTexts.DebugNextDay);
                root.EndDay(true);
            }
        }

        private void OnGUI()
        {
            if (!visible || root == null || root.Garden == null || root.Controller == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 13, wordWrap = false };
                style.padding = new RectOffset(10, 10, 8, 8);
            }

            Garden g = root.Garden;
            GardenController c = root.Controller;
            sb.Length = 0;
            sb.Append("<b>Tea garden debug</b>  (F1, F2 = next day)\n");
            GameClock clock = GameClock.Instance;
            sb.Append("Garden day ").Append(g.Day).Append("   clock day ").Append(clock != null ? clock.TotalDays : 0)
                .Append("   rain today ").Append(g.RainyToday ? "yes" : "no").Append(", tomorrow ").Append(g.RainyOn(g.Day + 1) ? "yes" : "no").Append('\n');
            sb.Append("Can ").Append(g.CanWater).Append('/').Append(g.settings.canCapacity)
                .Append("   tool ").Append(c.Tool).Append("   seed ").Append(c.SelectedSeed ?? "-").Append('\n');
            sb.Append("Target ").Append(c.Target).Append("   preview ").Append(c.Preview).Append('\n');

            Plot plot = g.PlotAt(c.Target);
            if (plot == null)
            {
                sb.Append("(no bed cell)");
            }
            else
            {
                sb.Append("Soil ").Append(plot.soil).Append(plot.watered ? ", wet" : ", dry")
                    .Append("   idle nights ").Append(plot.idleDays).Append('/').Append(g.settings.tilledDecayDays)
                    .Append(plot.compost ? "   compost" : "").Append('\n');
                Crop crop = plot.crop;
                PlantParams p = crop != null ? g.plants.Get(crop.plantId) : null;
                if (p == null)
                {
                    sb.Append("No plant");
                }
                else
                {
                    int needed = crop.regrowing ? p.regrowDays : p.StageDays(crop.stage);
                    sb.Append("Plant ").Append(p.id).Append(" (").Append(p.water).Append(p.perennial ? ", perennial" : ", annual").Append(")\n");
                    sb.Append("Stage ").Append(crop.stage).Append('/').Append(p.RipeStage)
                        .Append(crop.ripe ? "  RIPE" : "   progress " + crop.progress + "/" + needed)
                        .Append(crop.regrowing ? "  regrowing" : "").Append("   harvests ").Append(crop.harvests).Append('\n');
                    if (p.water == WaterNeed.Normal) sb.Append("Forgiven dry day used this stage: ").Append(crop.forgivenUsed ? "yes" : "no").Append('\n');
                    sb.Append("Care this cycle: ").Append(crop.cycleDays).Append(" days, ").Append(crop.cyclePoints).Append(" points → ")
                        .Append(crop.CareRatio.ToString("0.00")).Append("   expected quality ").Append(g.ExpectedQuality(crop))
                        .Append(crop.compost && crop.harvests == 0 ? " (compost bonus)" : "").Append('\n');
                    sb.Append("Water history (left = yesterday): ");
                    for (int i = 0; i < 8; i++) sb.Append((crop.waterHistory >> i & 1) == 1 ? '●' : '○');
                    sb.Append("\nSeasons (data only): ").Append(p.seasons.Length > 0 ? string.Join(", ", p.seasons) : "-");
                }
            }

            GUI.Box(new Rect(10f, 120f, 560f, 230f), sb.ToString(), style);
        }
    }
}
