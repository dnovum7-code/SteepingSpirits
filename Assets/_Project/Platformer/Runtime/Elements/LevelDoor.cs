using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.World;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>A door on the hub clearing. Stand in front of it and press up (or E) to travel.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelDoor : MonoBehaviour
    {
        public string targetScenePath = "";
        public string label = "";

        /// <summary>Level behind the door (empty = always open, e.g. the meadow).</summary>
        public string levelId = "";

        public bool IsOpen => string.IsNullOrEmpty(levelId) || JumpNRunSaveStore.Current.IsUnlocked(levelId);

        private bool playerInside;
        private float lastMoveY;
        private GUIStyle labelStyle, hintStyle;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<JumpNRunPlayer>() != null) playerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<JumpNRunPlayer>() != null) playerInside = false;
        }

        private void Update()
        {
            float moveY = GameInput.Move.y;
            bool upPressed = moveY > 0.5f && lastMoveY <= 0.5f;
            lastMoveY = moveY;

            if (!playerInside || GamePause.IsBlocked || string.IsNullOrEmpty(targetScenePath))
            {
                return;
            }

            if ((upPressed || GameInput.InteractPressed) && !IsOpen)
            {
                JumpNRunSounds.Play(JumpNRunSound.Land, 0.1f);
                return;
            }

            if (upPressed || GameInput.InteractPressed)
            {
                JumpNRunSounds.Play(JumpNRunSound.Lantern, 0.15f);
                ScenePortal.Load(targetScenePath);
            }
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = GuiDraw.Rich(14, new Color(1f, 0.93f, 0.8f), FontStyle.Bold, TextAnchor.MiddleCenter);
                hintStyle = GuiDraw.Rich(12, new Color(1f, 1f, 1f, 0.75f), FontStyle.Normal, TextAnchor.MiddleCenter);
            }

            Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.9f);
            if (s.z < 0f)
            {
                return;
            }

            var r = new Rect(s.x - 90f, Screen.height - s.y - 12f, 180f, 24f);
            bool open = IsOpen;
            GuiDraw.ShadowLabel(r, label, labelStyle);
            if (!open)
            {
                GuiDraw.ShadowLabel(new Rect(r.x, r.y + 20f, r.width, 20f), JumpNRunTexts.DoorLocked, hintStyle);
            }
            else if (playerInside)
            {
                GuiDraw.ShadowLabel(new Rect(r.x, r.y + 20f, r.width, 20f), JumpNRunTexts.DoorHint, hintStyle);
            }
        }
    }
}
