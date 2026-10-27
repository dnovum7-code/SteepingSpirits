using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
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

        private Coroutine slowMo;
        private float timeScaleBeforeSlowMo = 1f;

        public BrewSession Session { get; private set; }
        public BrewingTuning Tuning => tuning;
        public IReadOnlyList<TeaDefinition> Teas => teas;

        public TeaDefinition CurrentTea =>
            Session != null && Session.TeaIndex >= 0 && Session.TeaIndex < teas.Length ? teas[Session.TeaIndex] : null;

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
            SessionCreated?.Invoke(Session);
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
            if (slot >= 0)
            {
                Session.SelectTea(slot);
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
