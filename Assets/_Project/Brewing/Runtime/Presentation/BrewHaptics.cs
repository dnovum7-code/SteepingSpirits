using System.Collections;
using UnityEngine;
using SteepingSpirits.Brewing.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// A soft rumble on every boil stage change – only with the Input System
    /// package and a connected gamepad. Does nothing otherwise.
    /// </summary>
    public class BrewHaptics : BrewView
    {
        private BrewSession subscribed;
        private Coroutine pulse;

        private void Update()
        {
            BrewSession s = Session;
            if (s != null && s != subscribed)
            {
                if (subscribed != null) subscribed.BoilStageChanged -= HandleStage;
                s.BoilStageChanged += HandleStage;
                subscribed = s;
            }
        }

        private void HandleStage(BoilStage previous, BoilStage current)
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current == null || Look == null)
            {
                return;
            }

            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(Pulse(Look.hapticStrength, Look.hapticSeconds));
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private IEnumerator Pulse(float strength, float seconds)
        {
            Gamepad pad = Gamepad.current;
            pad.SetMotorSpeeds(strength * 0.6f, strength);
            yield return new WaitForSecondsRealtime(seconds);
            pad.SetMotorSpeeds(0f, 0f);
            pulse = null;
        }
#endif

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad.current?.ResetHaptics();
#endif
            if (subscribed != null) subscribed.BoilStageChanged -= HandleStage;
            subscribed = null;
        }
    }
}
