using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// The garden's own bindings (GameInput stays unchanged; movement still comes from it).
    ///   Use        Space / J / left mouse / E / F · pad X or A
    ///   Tools      1–5 · mouse wheel · pad shoulders
    ///   Seed       Q (next seed) · pad d-pad left/right
    ///   1x3 can    R · pad d-pad up        Garden help  H · pad d-pad down
    ///   Debug      F1 overlay · F2 next day
    /// </summary>
    public static class GardenInput
    {
        public static bool UsePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Mouse m = Mouse.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.jKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame))
                       || (m != null && m.leftButton.wasPressedThisFrame && !SteepingSpirits.Core.GameInput.IsPointerOverUi())
                       || (g != null && (g.buttonWest.wasPressedThisFrame || g.buttonSouth.wasPressedThisFrame));
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F)
                       || (Input.GetMouseButtonDown(0) && !SteepingSpirits.Core.GameInput.IsPointerOverUi());
#endif
            }
        }

        public static bool UseHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Mouse m = Mouse.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.isPressed || k.jKey.isPressed))
                       || (m != null && m.leftButton.isPressed)
                       || (g != null && g.buttonWest.isPressed);
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || Input.GetMouseButton(0);
#endif
            }
        }

        /// <summary>Tool chosen directly with 1–5 this frame, else -1.</summary>
        public static int ToolKey
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                if (k == null) return -1;
                if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) return 0;
                if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 1;
                if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 2;
                if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) return 3;
                if (k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame) return 4;
                return -1;
#else
                for (int i = 0; i < 5; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) return i;
                }

                return -1;
#endif
            }
        }

        /// <summary>-1 previous tool, +1 next tool, 0 nothing (shoulders, mouse wheel).</summary>
        public static int ToolStep
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Gamepad g = Gamepad.current;
                if (g != null && g.leftShoulder.wasPressedThisFrame) return -1;
                if (g != null && g.rightShoulder.wasPressedThisFrame) return 1;
                Mouse m = Mouse.current;
                float wheel = m != null ? m.scroll.ReadValue().y : 0f;
#else
                float wheel = Input.mouseScrollDelta.y;
#endif
                return wheel > 0.1f ? -1 : wheel < -0.1f ? 1 : 0;
            }
        }

        /// <summary>-1/+1 to change the seed in the bag.</summary>
        public static int SeedStep
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                if (k != null && k.qKey.wasPressedThisFrame) return 1;
                if (g != null && g.dpad.right.wasPressedThisFrame) return 1;
                if (g != null && g.dpad.left.wasPressedThisFrame) return -1;
                return 0;
#else
                return Input.GetKeyDown(KeyCode.Q) ? 1 : 0;
#endif
            }
        }

        public static bool WideCanTogglePressed => Pressed(KeyKind.WideCan);
        public static bool HelpTogglePressed => Pressed(KeyKind.Help);
        public static bool DebugTogglePressed => Pressed(KeyKind.Debug);
        public static bool DebugNextDayPressed => Pressed(KeyKind.NextDay);

        /// <summary>Mouse position in screen pixels and whether it moved this frame.</summary>
        public static Vector2 MouseScreen(out bool moved)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            if (m == null)
            {
                moved = false;
                return Vector2.zero;
            }

            moved = m.delta.ReadValue().sqrMagnitude > 0.5f;
            return m.position.ReadValue();
#else
            moved = Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f;
            return Input.mousePosition;
#endif
        }

        private enum KeyKind
        {
            WideCan,
            Help,
            Debug,
            NextDay
        }

        private static bool Pressed(KeyKind kind)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            switch (kind)
            {
                case KeyKind.WideCan: return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.dpad.up.wasPressedThisFrame);
                case KeyKind.Help: return (k != null && k.hKey.wasPressedThisFrame) || (g != null && g.dpad.down.wasPressedThisFrame);
                case KeyKind.Debug: return k != null && k.f1Key.wasPressedThisFrame;
                default: return k != null && k.f2Key.wasPressedThisFrame;
            }
#else
            switch (kind)
            {
                case KeyKind.WideCan: return Input.GetKeyDown(KeyCode.R);
                case KeyKind.Help: return Input.GetKeyDown(KeyCode.H);
                case KeyKind.Debug: return Input.GetKeyDown(KeyCode.F1);
                default: return Input.GetKeyDown(KeyCode.F2);
            }
#endif
        }
    }
}
