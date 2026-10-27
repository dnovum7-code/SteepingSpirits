using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Glass vessel: fills while pouring, liquor colour follows A/Amax along
    /// the tea's gradient, bitterness darkens and clouds it softly, leaves
    /// unfold with the extraction. The cup shows the finished tea.
    /// </summary>
    public class SteepVesselView : BrewView
    {
        [SerializeField] private SpriteRenderer vesselGlass;
        [SerializeField] private Transform liquorPivot;     // bottom-anchored, scale.y = fill
        [SerializeField] private SpriteRenderer liquor;
        [SerializeField] private SpriteRenderer cupLiquor;
        [SerializeField] private Transform[] leaves = new Transform[0];
        [SerializeField] private Color prewarmGlow = new Color(1f, 0.85f, 0.7f, 1f);
        [SerializeField] private Color hazeColor = new Color(0.35f, 0.25f, 0.18f, 1f);
        [Tooltip("Seconds for colour changes to settle (keeps everything soft)")]
        [SerializeField] private float colourSmoothing = 0.25f;

        private Color glassBase = Color.white;
        private Color shownColor;
        private float fill;
        private float[] leafAngles = new float[0];

        public void Configure(SpriteRenderer vesselGlass, Transform liquorPivot, SpriteRenderer liquor,
            SpriteRenderer cupLiquor, Transform[] leaves)
        {
            this.vesselGlass = vesselGlass;
            this.liquorPivot = liquorPivot;
            this.liquor = liquor;
            this.cupLiquor = cupLiquor;
            this.leaves = leaves;
        }

        private void Start()
        {
            if (vesselGlass != null) glassBase = vesselGlass.color;
            leafAngles = new float[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                leafAngles[i] = Random.Range(-30f, 30f);
            }
        }

        private void Update()
        {
            BrewSession s = Session;
            if (s == null || Look == null)
            {
                return;
            }

            TeaDefinition tea = controller.CurrentTea;
            UpdateFill(s);
            UpdateLiquor(s, tea);
            UpdateLeaves(s, tea);
            UpdateGlass(s);
            UpdateCup(s);
        }

        private void UpdateFill(BrewSession s)
        {
            float target;
            switch (s.Phase)
            {
                case BrewPhase.Pour: target = s.PhaseProgress; break;
                case BrewPhase.Steep: target = 1f; break;
                default: target = 0f; break;  // Result: tea went into the cup
            }

            // Pouring drives the fill directly; emptying eases out.
            fill = s.Phase == BrewPhase.Pour ? target : Mathf.MoveTowards(fill, target, Time.deltaTime * 1.5f);
            if (liquorPivot != null)
            {
                liquorPivot.localScale = new Vector3(1f, Mathf.Max(0.0001f, fill), 1f);
            }
        }

        /// <summary>Liquor colour for an extraction state – also used for the cup.</summary>
        private Color LiquorColor(TeaDefinition tea, float aromaShare, float bitterness)
        {
            Color c = tea != null ? tea.liquorColor.Evaluate(Mathf.Clamp01(aromaShare)) : Color.clear;
            float dark = Mathf.Clamp01(bitterness * Look.bitterDarkening);
            float haze = Mathf.Clamp01(bitterness * Look.bitterHaze);
            Color darker = new Color(c.r * (1f - dark * 0.5f), c.g * (1f - dark * 0.6f), c.b * (1f - dark * 0.6f), c.a);
            Color hazy = Color.Lerp(darker, hazeColor, haze * 0.35f);
            hazy.a = Mathf.Lerp(c.a, 1f, haze * 0.5f);
            return hazy;
        }

        private void UpdateLiquor(BrewSession s, TeaDefinition tea)
        {
            if (liquor == null)
            {
                return;
            }

            ExtractionModel e = s.Extraction;
            float share = e != null && e.AromaMax > 0f ? e.Aroma / e.AromaMax : 0f;
            Color target = s.Phase == BrewPhase.Pour || e == null
                ? LiquorColor(tea, 0f, 0f)
                : LiquorColor(tea, share, e.Bitterness);

            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, colourSmoothing));
            shownColor = Color.Lerp(shownColor, target, k);
            liquor.color = shownColor;
        }

        private void UpdateLeaves(BrewSession s, TeaDefinition tea)
        {
            bool inVessel = s.Phase == BrewPhase.Pour || s.Phase == BrewPhase.Steep
                            || (s.Phase == BrewPhase.HeatWater || s.Phase == BrewPhase.PreWarm) && s.Tea != null;
            ExtractionModel e = s.Extraction;
            float unfold = e != null && e.AromaMax > 0f && s.Phase == BrewPhase.Steep ? e.Aroma / e.AromaMax : 0f;

            float scale = Mathf.Lerp(Look.leafScaleRange.x, Look.leafScaleRange.y, unfold);
            for (int i = 0; i < leaves.Length; i++)
            {
                Transform leaf = leaves[i];
                if (leaf == null) continue;

                leaf.gameObject.SetActive(inVessel);
                leaf.localScale = new Vector3(scale, scale, 1f);
                leaf.localRotation = Quaternion.Euler(0f, 0f, leafAngles[i] + unfold * Look.leafUnfoldRotation * (i % 2 == 0 ? 1f : -1f));

                var sr = leaf.GetComponent<SpriteRenderer>();
                if (sr != null && tea != null) sr.color = tea.leafColor;
            }
        }

        private void UpdateGlass(BrewSession s)
        {
            if (vesselGlass == null)
            {
                return;
            }

            float warm = s.Phase == BrewPhase.PreWarm ? s.PhaseProgress : s.VesselPrewarmed ? 1f : 0f;
            vesselGlass.color = Color.Lerp(glassBase, glassBase * prewarmGlow, warm * 0.6f);
        }

        private void UpdateCup(BrewSession s)
        {
            if (cupLiquor == null)
            {
                return;
            }

            BrewResult r = s.Phase == BrewPhase.Result ? s.LastResult : null;
            TeaDefinition tea = controller.CurrentTea;
            Color target = r != null
                ? LiquorColor(tea, r.Aroma / Mathf.Max(0.0001f, s.Extraction != null ? s.Extraction.AromaMax : 1f), r.Bitterness)
                : new Color(0f, 0f, 0f, 0f);

            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, Look.resultFadeSeconds * 0.5f));
            cupLiquor.color = Color.Lerp(cupLiquor.color, target, k);
        }
    }
}
