using System;
using System.Collections.Generic;
using System.Text;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Actions the Jump'n'Run player can rebind.</summary>
    public enum BindAction
    {
        Left,
        Right,
        Up,
        Down,
        Jump,
        Dash,
        Grab
    }

    /// <summary>
    /// Keyboard bindings of the Jump'n'Run (two keys per action). Key names use
    /// the Input System's Key names ("Space", "A", "LeftArrow", "Digit1" …).
    /// Stored as text: "Left=A,LeftArrow;Right=D,RightArrow;…". Gamepad buttons
    /// are not rebindable.
    /// </summary>
    public sealed class KeyBindings
    {
        public const int KeysPerAction = 2;

        private readonly Dictionary<BindAction, string[]> keys = new Dictionary<BindAction, string[]>();

        public static readonly BindAction[] Actions = (BindAction[])Enum.GetValues(typeof(BindAction));

        public KeyBindings()
        {
            ResetToDefaults();
        }

        public static KeyBindings Defaults() => new KeyBindings();

        public void ResetToDefaults()
        {
            keys[BindAction.Left] = new[] { "A", "LeftArrow" };
            keys[BindAction.Right] = new[] { "D", "RightArrow" };
            keys[BindAction.Up] = new[] { "W", "UpArrow" };
            keys[BindAction.Down] = new[] { "S", "DownArrow" };
            keys[BindAction.Jump] = new[] { "Space", "C" };
            keys[BindAction.Dash] = new[] { "LeftShift", "X" };
            keys[BindAction.Grab] = new[] { "K", "LeftCtrl" };
        }

        public IReadOnlyList<string> KeysFor(BindAction action) => keys[action];

        public bool IsDefault => Serialize() == Defaults().Serialize();

        /// <summary>
        /// Puts a key into an action's slot. If another action used that key, it
        /// loses it (each key does one thing). Returns the action that lost it, if any.
        /// </summary>
        public BindAction? Bind(BindAction action, int slot, string key)
        {
            if (string.IsNullOrEmpty(key) || slot < 0 || slot >= KeysPerAction)
            {
                return null;
            }

            BindAction? lost = null;
            foreach (BindAction other in Actions)
            {
                string[] k = keys[other];
                for (int i = 0; i < k.Length; i++)
                {
                    if (k[i] == key && !(other == action && i == slot))
                    {
                        k[i] = "";
                        if (other != action) lost = other;
                    }
                }
            }

            keys[action][slot] = key;
            return lost;
        }

        /// <summary>Actions without any key (the menu shows a hint; defaults fill them back in).</summary>
        public List<BindAction> Unbound()
        {
            var list = new List<BindAction>();
            foreach (BindAction a in Actions)
            {
                bool any = false;
                foreach (string k in keys[a]) any |= !string.IsNullOrEmpty(k);
                if (!any) list.Add(a);
            }

            return list;
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (BindAction a in Actions)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(a).Append('=').Append(string.Join(",", keys[a]));
            }

            return sb.ToString();
        }

        /// <summary>Reads bindings; anything missing or broken keeps its default.</summary>
        public static KeyBindings Parse(string text)
        {
            var b = new KeyBindings();
            if (string.IsNullOrEmpty(text)) return b;
            foreach (string part in text.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0 || !Enum.TryParse(part.Substring(0, eq), out BindAction action)) continue;
                string[] values = part.Substring(eq + 1).Split(',');
                for (int i = 0; i < KeysPerAction; i++)
                {
                    b.keys[action][i] = i < values.Length ? values[i].Trim() : "";
                }
            }

            // An action left without any key falls back to its defaults (never lock the player out).
            KeyBindings defaults = Defaults();
            foreach (BindAction a in b.Unbound())
            {
                for (int i = 0; i < KeysPerAction; i++)
                {
                    b.Bind(a, i, defaults.keys[a][i]);
                }
            }

            return b;
        }
    }
}
