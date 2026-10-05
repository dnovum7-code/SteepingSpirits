using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A friendly spirit that floats in place and says one line (from
    /// <see cref="JumpNRunTexts.NpcLine"/>) while the player is near. The
    /// speech bubble is uGUI (<see cref="WorldLabel"/>).
    /// </summary>
    public class SpiritNpc : MonoBehaviour
    {
        public string lineKey = "";
        [SerializeField] private float talkRadius = 3f;
        [SerializeField] private float bubbleWidth = 420f;

        private Vector3 home;
        private float visibility;
        private float phase;
        private WorldLabel bubble;

        /// <summary>Lets the hub swap the line depending on progress.</summary>
        public void SetLine(string key)
        {
            lineKey = key;
            if (bubble != null) bubble.SetText(JumpNRunTexts.NpcLine(lineKey), 2);
        }

        private void Awake()
        {
            home = transform.position;
            phase = home.x * 0.5f;
        }

        private void Start()
        {
            bubble = new WorldLabel("SpeechBubble", 20, Color.white, new Color(0.12f, 0.14f, 0.22f, 0.85f), bubbleWidth);
            bubble.SetText(JumpNRunTexts.NpcLine(lineKey), 2);
        }

        private void OnDestroy()
        {
            bubble?.Destroy();
        }

        private void Update()
        {
            transform.position = home + Vector3.up * (Mathf.Sin(Time.time * 1.3f + phase) * 0.15f);
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            bool near = player != null && Vector2.Distance(player.Feet, transform.position) <= talkRadius;
            visibility = Mathf.MoveTowards(visibility, near ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            if (bubble != null)
            {
                bubble.SetAlpha(visibility);
                bubble.Follow(transform.position + Vector3.up * 1.1f);
            }
        }
    }
}
