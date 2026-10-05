using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Key rebinding screen (opened from the pause menu). Two keys per action;
    /// choose a slot, press a key. Escape cancels listening, "Standard" restores
    /// the defaults. Works with keyboard, mouse and pad navigation.
    /// </summary>
    public sealed class JumpNRunKeyPanel
    {
        private readonly GameObject root;
        private readonly List<Selectable> order = new List<Selectable>();
        private readonly Dictionary<Button, (BindAction action, int slot)> slots = new Dictionary<Button, (BindAction, int)>();
        private readonly Text hint;
        private readonly System.Action onClose;
        private Button listening;
        private int listenFrame;

        public bool IsOpen => root.activeSelf;
        public bool IsListening => listening != null;

        public JumpNRunKeyPanel(System.Action onClose)
        {
            this.onClose = onClose;
            root = UiFactory.NewRect("KeyPanel", JumpNRunUi.Root).gameObject;
            UiFactory.FullStretch((RectTransform)root.transform);
            JumpNRunUi.Dim(root.transform, 0.55f);
            RectTransform content = JumpNRunUi.Window(root.transform, JumpNRunTexts.KeysTitle, new Vector2(820f, 780f), out _);

            var columns = new List<Selectable[]>();
            foreach (BindAction action in KeyBindings.Actions)
            {
                RectTransform row = UiFactory.NewRect("Row", content);
                UiFactory.HorizontalGroup(row, 12f, new RectOffset(0, 0, 0, 0));
                UiFactory.Sizing(row.gameObject, 52f);
                Text name = UiFactory.Label("Action", row, JumpNRunTexts.ActionName(action), 24, UiFactory.TextMain, TextAnchor.MiddleLeft);
                UiFactory.Sizing(name.gameObject, 52f, 240f);

                var pair = new Selectable[KeyBindings.KeysPerAction];
                for (int s = 0; s < KeyBindings.KeysPerAction; s++)
                {
                    Button b = JumpNRunUi.Button(row, "", 52f);
                    UiFactory.Sizing(b.gameObject, 52f, 230f);
                    slots[b] = (action, s);
                    Button captured = b;
                    b.onClick.AddListener(() => Listen(captured));
                    pair[s] = b;
                }

                columns.Add(pair);
            }

            hint = JumpNRunUi.Line(content, JumpNRunTexts.KeysHint, 18, UiFactory.TextDim, TextAnchor.MiddleCenter, 40f);

            RectTransform buttons = UiFactory.NewRect("Buttons", content);
            UiFactory.HorizontalGroup(buttons, 14f, new RectOffset(0, 0, 6, 0)).childForceExpandWidth = true;
            UiFactory.Sizing(buttons.gameObject, 60f);
            Button reset = JumpNRunUi.Button(buttons, JumpNRunTexts.KeysReset);
            reset.onClick.AddListener(() =>
            {
                JumpNRunKeys.Set(KeyBindings.Defaults(), true);
                Refresh();
            });
            Button back = JumpNRunUi.Button(buttons, JumpNRunTexts.KeysBack);
            back.onClick.AddListener(Close);

            // Grid navigation: up/down within a column, left/right between the two keys.
            for (int r = 0; r < columns.Count; r++)
            {
                for (int s = 0; s < KeyBindings.KeysPerAction; s++)
                {
                    var nav = new Navigation { mode = Navigation.Mode.Explicit };
                    nav.selectOnUp = r > 0 ? columns[r - 1][s] : (Selectable)back;
                    nav.selectOnDown = r < columns.Count - 1 ? columns[r + 1][s] : (s == 0 ? reset : back);
                    nav.selectOnLeft = columns[r][(s + KeyBindings.KeysPerAction - 1) % KeyBindings.KeysPerAction];
                    nav.selectOnRight = columns[r][(s + 1) % KeyBindings.KeysPerAction];
                    columns[r][s].navigation = nav;
                }
            }

            reset.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnUp = columns[columns.Count - 1][0], selectOnRight = back, selectOnLeft = back,
                selectOnDown = columns[0][0]
            };
            back.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit, selectOnUp = columns[columns.Count - 1][1], selectOnLeft = reset, selectOnRight = reset,
                selectOnDown = columns[0][1]
            };

            order.Add(columns[0][0]);
            root.SetActive(false);
        }

        public void Open()
        {
            root.SetActive(true);
            Refresh();
            JumpNRunUi.Focus(order[0]);
        }

        public void Close()
        {
            listening = null;
            root.SetActive(false);
            onClose?.Invoke();
        }

        private void Listen(Button b)
        {
            listening = b;
            listenFrame = Time.frameCount;
            b.GetComponentInChildren<Text>().text = JumpNRunTexts.KeysPress;
            hint.text = JumpNRunTexts.KeysListening;
        }

        /// <summary>Called every frame by the options menu while the panel is open.</summary>
        public void Tick()
        {
            if (listening == null || Time.frameCount == listenFrame)
            {
                return;
            }

            string key = JumpNRunKeys.AnyKeyPressed();
            if (key == null)
            {
                return;
            }

            if (key != "Escape")
            {
                (BindAction action, int slot) = slots[listening];
                KeyBindings b = KeyBindings.Parse(JumpNRunKeys.Bindings.Serialize());
                b.Bind(action, slot, key);
                JumpNRunKeys.Set(b, true);
            }

            Button done = listening;
            listening = null;
            Refresh();
            JumpNRunUi.Focus(done);
        }

        private void Refresh()
        {
            KeyBindings b = JumpNRunKeys.Bindings;
            foreach (var kv in slots)
            {
                string name = b.KeysFor(kv.Value.action)[kv.Value.slot];
                kv.Key.GetComponentInChildren<Text>().text = JumpNRunTexts.KeyName(name);
            }

            hint.text = JumpNRunTexts.KeysHint;
        }
    }
}
