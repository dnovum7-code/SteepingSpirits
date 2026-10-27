using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
using SteepingSpirits.Brewing.Integration;
using SteepingSpirits.Brewing.Telemetry;

namespace SteepingSpirits.Brewing.Flow
{
    /// <summary>
    /// Thin bridge between Unity and the plain-C# <see cref="BrewSession"/>:
    /// reads input, turns it into session commands, advances time, writes
    /// telemetry and plays the gentle slow-motion for a perfect cup.
    /// Views read <see cref="Session"/> and listen to its events.
    /// </summary>
    public class BrewSessionController : MonoBehaviour
    {
        [SerializeField] private BrewingTuning tuning;
        [SerializeField] private TeaDefinition[] teas = new TeaDefinition[0];

        [Tooltip("Thermometer is an option – off by default")]
        [SerializeField] private bool thermometerVisible;

        [SerializeField] private bool writeTelemetry = true;

        [Tooltip("The four base teas (keys 1–4) are free to choose – the sandbox. Opened from the world they are debug-only")]
        [SerializeField] private bool allowBaseTeas = true;

        private Coroutine slowMo;
        private float timeScaleBeforeSlowMo = 1f;
        private readonly Dictionary<int, TeaDefinition> lookFor = new Dictionary<int, TeaDefinition>();
        private List<OwnTea> ownTeas = new List<OwnTea>();
        private int ownIndex;
        private bool portionTaken;

        public BrewSession Session { get; private set; }
        public BrewingTuning Tuning => tuning;
        public IReadOnlyList<TeaDefinition> Teas => teas;

        /// <summary>The player's own teas (workshop teas and blends in inventory and pantry).</summary>
        public IReadOnlyList<OwnTea> OwnTeas => ownTeas;
        public int OwnIndex => ownIndex;

        /// <summary>The own tea being brewed, or null for a base tea.</summary>
        public OwnTea CurrentOwn { get; private set; }

        /// <summary>Base teas choosable: in the sandbox, or with the debug overlay open.</summary>
        public bool BaseTeasAllowed => (allowBaseTeas && !BrewHandoff.FromWorld) || DebugVisible;

        /// <summary>The TeaDefinition that gives looks (colours) – for own teas the base preset.</summary>
        public TeaDefinition CurrentTea
        {
            get
            {
                if (Session == null || Session.TeaIndex < 0) return null;
                if (Session.TeaIndex < teas.Length) return teas[Session.TeaIndex];
                return lookFor.TryGetValue(Session.TeaIndex, out TeaDefinition d) ? d : null;
            }
        }

        public string CurrentTeaName => CurrentOwn != null ? CurrentOwn.displayName : CurrentTea != null ? CurrentTea.displayName : "";

        public bool ThermometerVisible => thermometerVisible;
        public bool DebugVisible { get; private set; }

        /// <summary>Raised after a session was (re)built – views re-subscribe.</summary>
        public event Action<BrewSession> SessionCreated;

        public void Configure(BrewingTuning tuning, TeaDefinition[] teas)
        {
            this.tuning = tuning;
            this.teas = teas;
        }

        private void Awake()
        {
            BuildSession();
        }

        private void BuildSession()
        {
            if (tuning == null || teas == null || teas.Length == 0)
            {
                Debug.LogWarning("[BrewSessionController] Tuning or teas missing – run SteepingSpirits → Brewing → Build Sandbox.");
                enabled = false;
                return;
            }

            // The session reads the asset's parameter objects directly, so inspector
            // changes during Play take effect immediately (use Recalibrate for Qref).
            var teaParams = new List<TeaParams>();
            foreach (TeaDefinition tea in teas)
            {
                teaParams.Add(tea.parameters);
            }

            Session = new BrewSession(tuning.simulation, teaParams);
            Session.Finished += HandleFinished;
            Session.PhaseChanged += HandlePhaseChanged;
            SessionCreated?.Invoke(Session);
        }

        private void Start()
        {
            if (Session == null) return;
            RefreshOwnTeas();
            string wanted = BrewHandoff.RequestedTeaId;
            BrewHandoff.RequestedTeaId = null;
            if (string.IsNullOrEmpty(wanted)) return;
            int i = ownTeas.FindIndex(t => t.ingredientId == wanted);
            if (i >= 0)
            {
                ownIndex = i;
                ChooseOwnTea();
            }
        }

        /// <summary>Reads inventory and pantry again (on entering the tea choice).</summary>
        public void RefreshOwnTeas()
        {
            string keep = ownIndex >= 0 && ownIndex < ownTeas.Count ? ownTeas[ownIndex].ingredientId : null;
            ownTeas = OwnTeaShelf.Load(null);
            int i = keep != null ? ownTeas.FindIndex(t => t.ingredientId == keep) : -1;
            ownIndex = i >= 0 ? i : 0;
        }

        /// <summary>Brew the selected own tea: base preset adjusted for its oxidation, tier and notes.</summary>
        private void ChooseOwnTea()
        {
            if (ownIndex < 0 || ownIndex >= ownTeas.Count) return;
            OwnTea own = ownTeas[ownIndex];
            TeaDefinition look = PresetFor(own.info.baseTea);
            if (look == null) return;
            int index = Session.UseTea(OwnTeaInfo.Brewable(look.parameters, own, tuning.processed));
            if (!Session.SelectTea(index)) return;
            lookFor[index] = look;
            CurrentOwn = own;
            portionTaken = false;
        }

        private TeaDefinition PresetFor(BrewBase b)
        {
            string id = OwnTeaInfo.PresetId(b);
            foreach (TeaDefinition d in teas)
            {
                if (d != null && d.parameters != null && d.parameters.id == id) return d;
            }

            return null;
        }

        /// <summary>One portion is used when the leaves go into the pot (first pour); re-infusing is free.</summary>
        private void HandlePhaseChanged(BrewPhase from, BrewPhase to)
        {
            if (to == BrewPhase.SelectTea) RefreshOwnTeas();
            if (to == BrewPhase.Pour && CurrentOwn != null && !portionTaken && Session.InfusionIndex == 0)
            {
                portionTaken = true;
                OwnTeaShelf.TakeOne(CurrentOwn.ingredientId);
                CurrentOwn.count = Mathf.Max(0, CurrentOwn.count - 1);
            }
        }

        [ContextMenu("Recalibrate (after tuning changes)")]
        public void Recalibrate()
        {
            Session?.Recalibrate();
        }

        private void OnDisable()
        {
            RestoreTimeScale();
        }

        private void Update()
        {
            if (Session == null)
            {
                return;
            }

            HandleInput();
            if (thermometerVisible)
            {
                Session.MarkThermometerUsed();
            }

            Session.Tick(Time.deltaTime);
        }

        private void HandleInput()
        {
            if (BrewInput.DebugPressed) DebugVisible = !DebugVisible;
            if (BrewInput.ThermometerPressed) thermometerVisible = !thermometerVisible;

            if (BrewInput.RestartPressed)
            {
                RestoreTimeScale();
                Session.Restart();
                return;
            }

            int slot = BrewInput.TeaSlotPressed();
            if (slot >= 0 && BaseTeasAllowed && Session.SelectTea(slot))
            {
                CurrentOwn = null;
            }

            bool choosing = Session.Phase == BrewPhase.SelectTea || Session.Phase == BrewPhase.Result;
            int step = choosing ? BrewInput.OwnTeaStep : 0;
            if (step != 0)
            {
                if (Session.Phase == BrewPhase.Result) Session.NextCup();
                if (ownTeas.Count > 0) ownIndex = (ownIndex + step + ownTeas.Count) % ownTeas.Count;
            }

            if (choosing && BrewInput.BackPressed && BrewHandoff.FromWorld)
            {
                RestoreTimeScale();
                string back = BrewHandoff.ReturnScenePath;
                BrewHandoff.Clear();
                SteepingSpirits.Processing.WorkshopSceneLoader.Load(back);
                return;
            }

            if (Session.Phase == BrewPhase.SelectTea && BrewInput.ActionPressed)
            {
                ChooseOwnTea();
                return;
            }

            if (BrewInput.RefillPressed) Session.RefillWater();
            if (BrewInput.CatchSparkPressed) Session.CatchSpark();
            if (BrewInput.FirePressed) Session.ToggleHeat();
            if (BrewInput.LadlePressed) Session.LadleBack();
            if (BrewInput.PrewarmPressed) Session.StartPrewarm();

            if (BrewInput.ActionPressed)
            {
                if (Session.Phase == BrewPhase.HeatWater) Session.Pour();
                else if (Session.Phase == BrewPhase.Steep) Session.LiftLeaves();
                else if (Session.Phase == BrewPhase.Result) Session.NextInfusion();
            }
        }

        private void HandleFinished(BrewResult result)
        {
            if (writeTelemetry)
            {
                BrewTelemetry.Append(result);
            }

            if (result.Tier == QualityTier.Perfect)
            {
                if (slowMo != null) StopCoroutine(slowMo);
                slowMo = StartCoroutine(SlowMotion());
            }
        }

        // Gentle slow-motion on unscaled time – never a freeze, never a cut.
        private IEnumerator SlowMotion()
        {
            PresentationParams look = tuning.presentation;
            timeScaleBeforeSlowMo = Time.timeScale;
            Time.timeScale = timeScaleBeforeSlowMo * look.perfectTimeScale;
            yield return new WaitForSecondsRealtime(look.perfectSlowMoSeconds);
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (slowMo != null)
            {
                StopCoroutine(slowMo);
                slowMo = null;
                Time.timeScale = timeScaleBeforeSlowMo;
            }
        }
    }
}
