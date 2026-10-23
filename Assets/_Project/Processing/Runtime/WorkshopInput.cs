using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The workshop's own bindings (GameInput stays unchanged; walking still uses it).
    ///   Use / knead / stoke   Space · J · E · F · pad A or X (knead also: one left-stick circle)
    ///   Close / finish        Esc · pad B
    ///   Debug                 F1 overlay · F2 / F3 fast forward
    /// </summary>
    public static class WorkshopInput
    {
        private static float stickAngle;
        private static float stickTurn;
        private static bool stickWasOut;

        public static bool UsePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.jKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.fKey.wasPressedThisFrame))
                       || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame));
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
#endif
            }
        }

        /// <summary>Knead / stoke key held (keyboard, pad, right trigger, left mouse).</summary>
        public static bool ActionHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                Mouse m = Mouse.current;
                return (k != null && (k.spaceKey.isPressed || k.jKey.isPressed))
                       || (g != null && (g.buttonSouth.isPressed || g.buttonWest.isPressed || g.rightTrigger.ReadValue() > 0.5f))
                       || (m != null && m.leftButton.isPressed && !SteepingSpirits.Core.GameInput.IsPointerOverUi());
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || (Input.GetMouseButton(0) && !SteepingSpirits.Core.GameInput.IsPointerOverUi());
#endif
            }
        }

        public static bool KneadPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                Gamepad g = Gamepad.current;
                Mouse m = Mouse.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.jKey.wasPressedThisFrame))
                       || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame))
                       || (m != null && m.leftButton.wasPressedThisFrame && !SteepingSpirits.Core.GameInput.IsPointerOverUi());
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || (Input.GetMouseButtonDown(0) && !SteepingSpirits.Core.GameInput.IsPointerOverUi());
#endif
            }
        }

        public static bool ClosePressed
        {
            get
            {
                bool pad = false;
#if ENABLE_INPUT_SYSTEM
                Gamepad g = Gamepad.current;
                pad = g != null && g.buttonEast.wasPressedThisFrame;
#endif
                return pad || SteepingSpirits.Core.GameInput.CancelPressed;
            }
        }

        /// <summary>True once per full circle of the left stick (rolling with the stick). Call once per frame.</summary>
        public static bool StickCircle()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Gamepad g = Gamepad.current;
            if (g != null) v = g.leftStick.ReadValue();
#endif
            if (v.sqrMagnitude < 0.5f)
            {
                stickWasOut = false;
                stickTurn = 0f;
                return false;
            }

            float angle = Mathf.Atan2(v.y, v.x);
            if (stickWasOut)
            {
                float d = Mathf.DeltaAngle(stickAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                stickTurn += d;
            }

            stickAngle = angle;
            stickWasOut = true;
            if (Mathf.Abs(stickTurn) >= Mathf.PI * 2f)
            {
                stickTurn = 0f;
                return true;
            }

            return false;
        }

        public static bool DebugTogglePressed => Pressed(0);
        public static bool ForwardSmallPressed => Pressed(1);
        public static bool ForwardLargePressed => Pressed(2);

        private static bool Pressed(int which)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            return which == 0 ? k.f1Key.wasPressedThisFrame : which == 1 ? k.f2Key.wasPressedThisFrame : k.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(which == 0 ? KeyCode.F1 : which == 1 ? KeyCode.F2 : KeyCode.F3);
#endif
        }
    }
}
