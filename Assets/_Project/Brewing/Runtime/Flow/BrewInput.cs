using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Brewing.Flow
{
    /// <summary>
    /// Input adapter for the brewing prototype. Works with the Input System
    /// package (keyboard + gamepad) and the legacy Input Manager (keyboard).
    /// Keyboard: 1/2/3 tea, F fire, S ladle, W pre-warm, Space pour/lift,
    /// T thermometer, F1 debug, R restart.
    /// Gamepad: D-pad left/up/right tea, X fire, Y ladle, LB pre-warm, A pour/lift, Start restart.
    /// </summary>
    public static class BrewInput
    {
        /// <summary>Tea slot pressed this frame (0, 1, 2) or −1.</summary>
        public static int TeaSlotPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) return 0;
                if (k.digit2Key.wasPressedThisFrame) return 1;
                if (k.digit3Key.wasPressedThisFrame) return 2;
            }

            Gamepad g = Gamepad.current;
            if (g != null)
            {
                if (g.dpad.left.wasPressedThisFrame) return 0;
                if (g.dpad.up.wasPressedThisFrame) return 1;
                if (g.dpad.right.wasPressedThisFrame) return 2;
            }

            return -1;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            return -1;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool KeyDown(UnityEngine.InputSystem.Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        private static bool Pad(System.Func<Gamepad, UnityEngine.InputSystem.Controls.ButtonControl> button) =>
            Gamepad.current != null && button(Gamepad.current).wasPressedThisFrame;

        public static bool FirePressed => KeyDown(UnityEngine.InputSystem.Key.F) || Pad(g => g.buttonWest);
        public static bool LadlePressed => KeyDown(UnityEngine.InputSystem.Key.S) || Pad(g => g.buttonNorth);
        public static bool PrewarmPressed => KeyDown(UnityEngine.InputSystem.Key.W) || Pad(g => g.leftShoulder);
        public static bool ActionPressed => KeyDown(UnityEngine.InputSystem.Key.Space) || Pad(g => g.buttonSouth);
        public static bool ThermometerPressed => KeyDown(UnityEngine.InputSystem.Key.T);
        public static bool DebugPressed => KeyDown(UnityEngine.InputSystem.Key.F1);
        public static bool RestartPressed => KeyDown(UnityEngine.InputSystem.Key.R) || Pad(g => g.startButton);
#else
        public static bool FirePressed => Input.GetKeyDown(KeyCode.F);
        public static bool LadlePressed => Input.GetKeyDown(KeyCode.S);
        public static bool PrewarmPressed => Input.GetKeyDown(KeyCode.W);
        public static bool ActionPressed => Input.GetKeyDown(KeyCode.Space);
        public static bool ThermometerPressed => Input.GetKeyDown(KeyCode.T);
        public static bool DebugPressed => Input.GetKeyDown(KeyCode.F1);
        public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
#endif
    }
}
