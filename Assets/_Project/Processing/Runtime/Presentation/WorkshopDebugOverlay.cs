using System.Text;
using UnityEngine;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// Developer overlay (F1): every value of the focused batch, where it leads, expected quality and
    /// notes. F2 / F3 fast forward the game time (editor and development builds only).
    /// </summary>
    public class WorkshopDebugOverlay : MonoBehaviour
    {
        private TeaWorkshop root;
        private bool visible;
        private GUIStyle style;
        private readonly StringBuilder sb = new StringBuilder(768);

        public void Init(TeaWorkshop workshop) => root = workshop;

        private void Update()
        {
            if (root == null || root.Model == null) return;
            if (WorkshopInput.DebugTogglePressed) visible = !visible;
            if (!(Application.isEditor || Debug.isDebugBuild)) return;
            float minutes = WorkshopInput.ForwardSmallPressed ? root.Settings.comfort.forwardSmallMinutes
                : WorkshopInput.ForwardLargePressed ? root.Settings.comfort.forwardLargeMinutes : 0f;
            if (minutes <= 0f) return;
            root.Time.FastForward(minutes);
            root.Model.Sync();
            root.Ui.Message(ProcessingTexts.DebugForward + " (+" + minutes.ToString("0") + " min)");
        }

        private LeafBatch Focus()
        {
            int id = root.Controller != null ? root.Controller.FocusBatch : -1;
            LeafBatch b = id >= 0 ? root.Model.Get(id) : null;
            return b ?? (root.Model.Batches.Count > 0 ? root.Model.Batches[0] : null);
        }

        private void OnGUI()
        {
            if (!visible || root == null || root.Model == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, fontSize = 13, wordWrap = false, padding = new RectOffset(10, 10, 8, 8) };
            ProcessingParams p = root.Model.settings;
            sb.Length = 0;
            sb.Append("<b>Workshop debug</b>  (F1, F2 +").Append(root.Settings.comfort.forwardSmallMinutes.ToString("0"))
                .Append(" min, F3 +").Append(root.Settings.comfort.forwardLargeMinutes.ToString("0")).Append(" min)\n");
            sb.Append("Game time ").Append(root.Model.Now.ToString("0.0")).Append(" min (skipped ").Append(root.Time.Offset.ToString("0")).Append(")   batches ").Append(root.Model.Batches.Count).Append('\n');
            LeafBatch b = Focus();
            if (b == null)
            {
                sb.Append("(no batch)");
            }
            else
            {
                sb.Append("Batch ").Append(b.id).Append("  ").Append(b.amount).Append("× ").Append(b.sourceId).Append(" q").Append(b.originTier).Append(b.firstFlush ? " FIRST FLUSH" : "")
                    .Append("  at ").Append(b.place).Append("  path ").Append(b.PathCode()).Append('\n');
                sb.Append("moisture ").Append(b.moisture.ToString("0.000")).Append("  work moisture ").Append(b.workMoisture.ToString("0.000"))
                    .Append("  withered ").Append(b.IsWithered(p.wither) ? "yes" : "no").Append('\n');
                sb.Append("rolled ").Append(b.rolled.ToString("0.00")).Append(" ×").Append(b.rollCount).Append("  evenness ").Append(b.rollEvenness.ToString("0.00")).Append('\n');
                sb.Append("oxidation ").Append(b.oxidation.ToString("0.000")).Append(b.oxidationStopped ? "  stopped at " + b.oxidationAtStop.ToString("0.000") : "")
                    .Append("  fixed ").Append(b.fixedByWok ? "yes (care " + b.wokCare.ToString("0.00") + ", residual " + b.fixResidual.ToString("0.00") + ")" : "no").Append('\n');
                sb.Append("roast ").Append(b.roast.ToString("0.000")).Append(" (drying ").Append(b.roastFromDrying.ToString("0.000")).Append(")  dryness ").Append(b.dryness.ToString("0.000")).Append('\n');
                sb.Append("active seconds ").Append(b.activeSeconds.ToString("0.0")).Append('\n');
                CareReport q = TeaQuality.Evaluate(b, p);
                sb.Append("→ ").Append(TeaClassifier.Classify(b, p.variety)).Append("  quality ").Append(q.tier).Append(" (score ").Append(q.score.ToString("0.00"))
                    .Append(", ceiling ").Append(q.ceiling).Append(")\n");
                sb.Append("care: wither ").Append(q.wither.ToString("0.00")).Append("  roll ").Append(q.roll.ToString("0.00")).Append("  ox ").Append(q.oxidation.ToString("0.00"))
                    .Append("  wok ").Append(q.wok.ToString("0.00")).Append("  origin ").Append(q.origin.ToString("0.00")).Append('\n');
                sb.Append("notes: ");
                foreach (TeaNote n in TeaNotes.For(b, p)) sb.Append(TeaNotes.Key(n)).Append(' ');
                sb.Append("\nid if packed now: ").Append(ProcessedTea.MakeId(TeaClassifier.Classify(b, p.variety), TeaNotes.For(b, p), q.tier));
            }

            GUI.Box(new Rect(10f, 10f, 620f, 260f), sb.ToString(), style);
        }
    }
}
