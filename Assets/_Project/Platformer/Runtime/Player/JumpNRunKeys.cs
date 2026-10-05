using System;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.Hooks;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Keyboard bindings of the Jump'n'Run (rebindable in the pause menu). With
    /// the default bindings the player reads GameInput exactly as before; with
    /// custom bindings the keyboard is read here and the gamepad directly
    /// (same buttons as GameInput: stick/d-pad, A jump, X dash, shoulders grab).
    /// GameInput itself is not changed.
    /// </summary>
    public static class JumpNRunKeys
    {
        private static KeyBindings bindings;
        private static bool custom;
#if ENABLE_INPUT_SYSTEM
        private static Key[][] parsed;
#else
        private static KeyCode[][] parsed;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            bindings = null;
            parsed = null;
            custom = false;
        }

        public static KeyBindings Bindings
        {
            get
            {
                if (bindings == null) Set(KeyBindings.Parse(JumpNRunSaveStore.Current.Keys), false);
                return bindings;
            }
        }

        public static bool IsCustom
        {
            get
            {
                _ = Bindings;
                return custom;
            }
        }

        /// <summary>Applies bindings (and stores them in the save when asked).</summary>
        public static void Set(KeyBindings value, bool save)
        {
            bindings = value ?? KeyBindings.Defaults();
            custom = !bindings.IsDefault;
            int n = KeyBindings.Actions.Length;
#if ENABLE_INPUT_SYSTEM
            parsed = new Key[n][];
#else
            parsed = new KeyCode[n][];
#endif
            for (int a = 0; a < n; a++)
            {
                var names = bindings.KeysFor(KeyBindings.Actions[a]);
#if ENABLE_INPUT_SYSTEM
                parsed[a] = new Key[names.Count];
                for (int i = 0; i < names.Count; i++) parsed[a][i] = Enum.TryParse(names[i], out Key k) ? k : Key.None;
#else
                parsed[a] = new KeyCode[names.Count];
                for (int i = 0; i < names.Count; i++) parsed[a][i] = ToKeyCode(names[i]);
#endif
            }

            if (save)
            {
                JumpNRunSaveStore.Current.Keys = custom ? bindings.Serialize() : "";
                JumpNRunSaveStore.Save();
            }
        }

        /// <summary>One frame of input from keyboard (custom bindings) and gamepad.</summary>
        public static PlayerInputFrame Read()
        {
            _ = Bindings;
            Vector2 move = Vector2.zero;
            if (Held(BindAction.Left)) move.x -= 1f;
            if (Held(BindAction.Right)) move.x += 1f;
            if (Held(BindAction.Down)) move.y -= 1f;
            if (Held(BindAction.Up)) move.y += 1f;

            var f = new PlayerInputFrame
            {
                jumpHeld = Held(BindAction.Jump),
                jumpPressed = Pressed(BindAction.Jump),
                dashPressed = Pressed(BindAction.Dash),
                grabHeld = Held(BindAction.Grab)
            };

#if ENABLE_INPUT_SYSTEM
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                Vector2 stick = g.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) move += stick;
                move += g.dpad.ReadValue();
                f.jumpHeld |= g.buttonSouth.isPressed;
                f.jumpPressed |= g.buttonSouth.wasPressedThisFrame;
                f.dashPressed |= g.buttonWest.wasPressedThisFrame;
                f.grabHeld |= g.rightShoulder.isPressed || g.leftShoulder.isPressed || g.rightTrigger.isPressed;
            }
#endif
            f.move = Vector2.ClampMagnitude(move, 1f);
            return f;
        }

        private static bool Held(BindAction action)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            foreach (Key key in parsed[(int)action])
            {
                if (key != Key.None && k[key].isPressed) return true;
            }
#else
            foreach (KeyCode key in parsed[(int)action])
            {
                if (key != KeyCode.None && Input.GetKey(key)) return true;
            }
#endif
            return false;
        }

        private static bool Pressed(BindAction action)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            foreach (Key key in parsed[(int)action])
            {
                if (key != Key.None && k[key].wasPressedThisFrame) return true;
            }
#else
            foreach (KeyCode key in parsed[(int)action])
            {
                if (key != KeyCode.None && Input.GetKeyDown(key)) return true;
            }
#endif
            return false;
        }

        /// <summary>The key pressed this frame (Input System name), or null. Escape is reported as "Escape".</summary>
        public static string AnyKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return null;
            foreach (var control in k.allKeys)
            {
                if (control != null && control.wasPressedThisFrame) return control.keyCode.ToString();
            }
#else
            foreach (KeyCode code in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
            {
                if (code >= KeyCode.Mouse0) break;
                if (Input.GetKeyDown(code)) return FromKeyCode(code);
            }
#endif
            return null;
        }

#if !ENABLE_INPUT_SYSTEM
        private static KeyCode ToKeyCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return KeyCode.None;
            if (name.StartsWith("Digit")) name = "Alpha" + name.Substring(5);
            else if (name == "LeftCtrl") name = "LeftControl";
            else if (name == "RightCtrl") name = "RightControl";
            else if (name == "Enter") name = "Return";
            else if (name == "Backquote") name = "BackQuote";
            return Enum.TryParse(name, out KeyCode c) ? c : KeyCode.None;
        }

        private static string FromKeyCode(KeyCode code)
        {
            string name = code.ToString();
            if (name.StartsWith("Alpha")) return "Digit" + name.Substring(5);
            if (name == "LeftControl") return "LeftCtrl";
            if (name == "RightControl") return "RightCtrl";
            if (name == "Return") return "Enter";
            if (name == "BackQuote") return "Backquote";
            return name;
        }
#endif
    }
}
