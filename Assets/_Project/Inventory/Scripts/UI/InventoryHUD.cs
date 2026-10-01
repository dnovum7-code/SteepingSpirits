using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Inventory.UI
{
    /// <summary>
    /// Sofort einsatzbereite Inventar-Anzeige (IMGUI/OnGUI – kein Canvas,
    /// keine Referenzen), analog zur Quest-Demo-HUD. Ein-/Ausblenden mit I.
    /// Benutzbare Items bekommen einen „Benutzen"-Button.
    ///
    /// Für die finale UI später durch eine uGUI-Ansicht ersetzen, die auf
    /// InventoryEvents hört – die Logik bleibt gleich.
    /// </summary>
    public class InventoryHUD : MonoBehaviour
    {
        [SerializeField] private bool startOpen;
        private bool open;
        private readonly GuiDraggablePanel panel = new GuiDraggablePanel();

        private void Start()
        {
            open = startOpen;
        }

        private void Update()
        {
            if (GameInput.InventoryTogglePressed)
            {
                open = !open;
            }
        }

        private void OnGUI()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv == null)
            {
                return;
            }

            // Kleiner Hinweis unten links.
            GuiDraw.ShadowLabel(new Rect(20f, Screen.height - 30f, 300f, 24f),
                open ? "Inventar schliessen: I" : "Inventar öffnen: I", Rich(12));

            if (!open)
            {
                return;
            }

            const float w = 300f;
            float h = Mathf.Min(Screen.height - 80f, 120f + inv.Slots.Count * 30f);
            var area = panel.Apply(new Rect(Screen.width - w - 20f, 90f, w, h)); // oben ziehen

            GuiDraw.Panel(area);
            GuiDraggablePanel.DrawGrip(area);
            GameInput.ClaimGuiArea(area);
            GUILayout.BeginArea(new Rect(area.x + 24f, area.y + 12f, area.width - 38f, area.height - 24f));

            GUILayout.Label($"<b>Inventar</b>  ({inv.Slots.Count}/{inv.Capacity})", Rich(16));

            if (inv.Slots.Count == 0)
            {
                GUILayout.Label("Leer.", Rich(13, new Color(1, 1, 1, .6f)));
            }

            int gold = 0;
            // Kopie-frei über die Slots iterieren; „Benutzen" erst nach der Schleife.
            string useID = null;
            foreach (ItemStack stack in inv.Slots)
            {
                gold += stack.Item.goldValue * stack.Amount;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{stack.Item.displayName}  <b>x{stack.Amount}</b>", Rich(13));
                GUILayout.FlexibleSpace();
                if (stack.Item.usable && GUILayout.Button("Benutzen", GUILayout.Width(80f)))
                {
                    useID = stack.Item.itemID;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Gesamtwert: {gold} Gold", Rich(12, new Color(1, 1, 1, .6f)));
            GUILayout.EndArea();

            if (useID != null)
            {
                inv.Use(useID);
            }
        }

        private static GUIStyle Rich(int size, Color? color = null)
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = size, wordWrap = true };
            style.normal.textColor = color ?? Color.white;
            return style;
        }
    }
}
