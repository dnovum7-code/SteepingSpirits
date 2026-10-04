using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Tuning overlay (F1): water state, steep values, predicted tier and curves
    /// of A, B and Q over the steep time. Samples go into fixed buffers (no GC).
    /// </summary>
    public class BrewDebugOverlay : BrewView
    {
        [Tooltip("Seconds between curve samples")]
        [SerializeField] private float sampleInterval = 0.1f;
        [SerializeField] private int maxSamples = 600;
        [SerializeField] private Rect panel = new Rect(0f, 90f, 420f, 420f); // x < 0 → right-aligned

        private float[] aroma;
        private float[] bitter;
        private float[] quality;
        private int count;
        private float sampleTimer;
        private ExtractionModel sampledModel;
        private GUIStyle text;

        private static readonly Color AromaColor = new Color(0.55f, 0.9f, 0.45f);
        private static readonly Color BitterColor = new Color(0.95f, 0.5f, 0.35f);
        private static readonly Color QColor = new Color(0.55f, 0.75f, 1f);

        private void Awake()
        {
            aroma = new float[maxSamples];
            bitter = new float[maxSamples];
            quality = new float[maxSamples];
        }

        private void Update()
        {
            BrewSession s = Session;
            if (s == null || s.Extraction == null)
            {
                return;
            }

            if (sampledModel != s.Extraction)
            {
                sampledModel = s.Extraction;
                count = 0;
                sampleTimer = 0f;
            }

            if (s.Phase != BrewPhase.Steep)
            {
                return;
            }

            sampleTimer -= Time.deltaTime;
            if (sampleTimer <= 0f && count < maxSamples)
            {
                sampleTimer = sampleInterval;
                aroma[count] = s.Extraction.Aroma;
                bitter[count] = s.Extraction.Bitterness;
                quality[count] = s.PreviewQuality().Q;
                count++;
            }
        }

        private void OnGUI()
        {
            BrewSession s = Session;
            if (s == null || controller == null || !controller.DebugVisible)
            {
                return;
            }

            text ??= GuiDraw.Rich(12, new Color(0.95f, 0.95f, 0.95f));
            var box = new Rect(Screen.width - panel.width - 16f, panel.y, panel.width, panel.height);
            GuiDraw.Solid(box, new Color(0f, 0f, 0f, 0.72f));

            WaterModel w = s.Water;
            ExtractionModel e = s.Extraction;
            QualityEvaluation q = s.PreviewQuality();
            string tea = s.Tea != null ? s.Tea.id : "-";

            string lines =
                $"<b>Brew debug</b>  (F1)   tea: {tea}   phase: {s.Phase}\n" +
                $"Kettle {w.Temperature:0.0} °C   stage: {w.Stage} ({BrewTexts.Stage(w.Stage)})   fire: {(w.HeatOn ? "on" : "off")}\n" +
                $"Boiling {w.BoilingSeconds:0.0} s   stale: {(w.IsStale ? "YES" : "no")}   prewarmed: {(s.VesselPrewarmed ? "yes" : "no")}\n" +
                (s.Tea != null ? $"Window {s.Tea.idealMin:0}–{s.Tea.idealMax:0} °C   Qref {s.Calibration.QReference:0.000}   optimum {s.Calibration.OptimalSeconds:0.0} s\n" : "\n") +
                (e != null
                    ? $"Steep {e.ElapsedSeconds:0.0} s   vessel {e.Temperature:0.0} °C   onset {e.BitterOnset:0.00}\n" +
                      $"A {e.Aroma:0.000} / {e.AromaMax:0.00}   B {e.Bitterness:0.000}   Q {q.Q:0.000}   H {q.Harmony:0.00}\n" +
                      $"→ {q.Tier} ({BrewTexts.Tier(q.Tier)}){(q.IsTart ? "  · herb" : "")}"
                    : "Steep –");
            GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, 130f), lines, text);

            DrawCurves(new Rect(box.x + 10f, box.y + 150f, box.width - 20f, box.height - 170f), s);
        }

        private void DrawCurves(Rect area, BrewSession s)
        {
            GuiDraw.Solid(area, new Color(1f, 1f, 1f, 0.06f));
            const float minY = -0.5f, maxY = 1f;

            // Zero line and tier thresholds (relative to Qref).
            Line(area, 0f, minY, maxY, new Color(1f, 1f, 1f, 0.25f));
            if (s.Tea != null)
            {
                QualityParams qp = Sim.quality;
                float qRef = s.Calibration.QReference;
                Line(area, qp.perfectThreshold * qRef, minY, maxY, new Color(1f, 0.9f, 0.5f, 0.35f));
                Line(area, qp.harmoniousThreshold * qRef, minY, maxY, new Color(1f, 0.9f, 0.5f, 0.2f));
                Line(area, qp.decentThreshold * qRef, minY, maxY, new Color(1f, 0.9f, 0.5f, 0.12f));
            }

            int n = Mathf.Max(count, 2);
            for (int i = 0; i < count; i++)
            {
                float x = area.x + area.width * i / (maxSamples - 1);
                Dot(area, x, aroma[i], minY, maxY, AromaColor);
                Dot(area, x, bitter[i], minY, maxY, BitterColor);
                Dot(area, x, quality[i], minY, maxY, QColor);
            }

            GUI.Label(new Rect(area.x + 4f, area.yMax - 20f, area.width, 18f),
                $"<color=#8ce673>A</color>  <color=#f28059>B</color>  <color=#8cbfff>Q</color>   ·   0–{maxSamples * sampleInterval:0} s   (n={n})", text);
        }

        private static void Line(Rect area, float value, float minY, float maxY, Color color)
        {
            float y = area.yMax - Mathf.InverseLerp(minY, maxY, value) * area.height;
            GuiDraw.Solid(new Rect(area.x, y, area.width, 1f), color);
        }

        private static void Dot(Rect area, float x, float value, float minY, float maxY, Color color)
        {
            float y = area.yMax - Mathf.Clamp01(Mathf.InverseLerp(minY, maxY, value)) * area.height;
            GuiDraw.Solid(new Rect(x - 1f, y - 1f, 2.5f, 2.5f), color);
        }
    }
}
