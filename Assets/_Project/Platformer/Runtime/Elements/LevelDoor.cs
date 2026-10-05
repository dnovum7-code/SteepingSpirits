using UnityEngine;
using SteepingSpirits.Core;
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
        private WorldLabel sign;
        private int shownState = -1;

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

        private void Start()
        {
            sign = new WorldLabel("DoorSign", 22, new Color(1f, 0.93f, 0.8f), new Color(0.2f, 0.13f, 0.08f, 0.7f), 340f);
        }

        private void OnDestroy()
        {
            sign?.Destroy();
        }

        private void LateUpdate()
        {
            if (sign == null)
            {
                return;
            }

            int state = !IsOpen ? 2 : playerInside ? 1 : 0;
            if (state != shownState)
            {
                shownState = state;
                string hintLine = state == 2 ? JumpNRunTexts.DoorLocked : state == 1 ? JumpNRunTexts.DoorHint : "";
                sign.SetText(hintLine.Length > 0 ? label + "\n<size=16>" + hintLine + "</size>" : label, hintLine.Length > 0 ? 2 : 1);
                sign.SetAlpha(1f);
            }

            sign.Follow(transform.position + Vector3.up * 1.9f);
        }
    }
}
