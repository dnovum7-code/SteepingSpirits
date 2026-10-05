using UnityEngine;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A friendly spirit that floats in place and says one line (from
    /// <see cref="JumpNRunTexts.NpcLine"/>) while the player is near.
    /// </summary>
    public class SpiritNpc : MonoBehaviour
    {
        public string lineKey = "";
        [SerializeField] private float talkRadius = 3f;
        [SerializeField] private float bubbleWidth = 260f;

        private Vector3 home;
        private float visibility;
        private float phase;
        private GUIStyle style;

        private void Awake()
        {
            home = transform.position;
            phase = home.x * 0.5f;
        }

        private void Update()
        {
            transform.position = home + Vector3.up * (Mathf.Sin(Time.time * 1.3f + phase) * 0.15f);
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            bool near = player != null && Vector2.Distance(player.Feet, transform.position) <= talkRadius;
            visibility = Mathf.MoveTowards(visibility, near ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (visibility <= 0.01f || cam == null)
            {
                return;
            }

            if (style == null)
            {
                style = GuiDraw.Rich(13, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);
            }

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
            if (screen.z < 0f)
            {
                return;
            }

            string line = JumpNRunTexts.NpcLine(lineKey);
            float h = style.CalcHeight(new GUIContent(line), bubbleWidth - 20f) + 14f;
            var r = new Rect(screen.x - bubbleWidth * 0.5f, Screen.height - screen.y - h, bubbleWidth, h);
            GuiDraw.Solid(r, new Color(0.12f, 0.14f, 0.22f, 0.8f * visibility));
            style.normal.textColor = new Color(1f, 1f, 1f, visibility);
            GUI.Label(new Rect(r.x + 10f, r.y + 7f, r.width - 20f, r.height - 14f), line, style);
        }
    }
}
