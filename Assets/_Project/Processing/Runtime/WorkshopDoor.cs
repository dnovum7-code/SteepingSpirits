using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The workshop door placed into the garden scene by the builder. The garden has no Interactor2D,
    /// so the door reacts on its own: stand next to it and press the use key. It loads the workshop
    /// without ScenePortal, so the garden gate keeps leading back to the meadow.
    /// </summary>
    public class WorkshopDoor : MonoBehaviour
    {
        [SerializeField] private string workshopScenePath = "Assets/Scenes/Teewerkstatt.unity";
        [SerializeField] private float reach = 1.3f;

        private Transform player;
        private SpriteRenderer glow;
        private TextMesh label;

        public void Configure(string scenePath) => workshopScenePath = scenePath;

        private void Start()
        {
            var frame = PlaceholderSprites.CreateSpriteObject("Türrahmen", PlaceholderSprites.Square, new Color(0.35f, 0.24f, 0.15f), transform.position, new Vector2(0.9f, 1.1f), 3, transform);
            frame.transform.localPosition = Vector3.zero;
            var door = PlaceholderSprites.CreateSpriteObject("Tür", PlaceholderSprites.Square, new Color(0.62f, 0.44f, 0.26f), transform.position, new Vector2(0.7f, 0.95f), 4, transform);
            door.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            glow = PlaceholderSprites.CreateSpriteObject("Licht", PlaceholderSprites.Circle, new Color(1f, 0.85f, 0.55f, 0f), transform.position, new Vector2(1.6f, 1.6f), 2, transform);
            glow.transform.localPosition = Vector3.zero;

            var text = new GameObject("Beschriftung");
            text.transform.SetParent(transform, false);
            text.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            label = text.AddComponent<TextMesh>();
            label.text = ProcessingTexts.GardenDoorLabel;
            label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.08f;
            label.fontSize = 48;
            label.color = new Color(1f, 0.96f, 0.86f, 0f);
            label.font = SteepingSpirits.Core.UI.UiFactory.DefaultFont();
            if (text.TryGetComponent(out MeshRenderer mr))
            {
                if (label.font != null) mr.sharedMaterial = label.font.material;
                mr.sortingOrder = 50;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }

            bool near = Vector2.Distance(player.position, transform.position) <= reach;
            float a = Mathf.MoveTowards(label.color.a, near ? 1f : 0f, Time.deltaTime * 3f);
            label.color = new Color(label.color.r, label.color.g, label.color.b, a);
            Color g = glow.color;
            g.a = a * (0.18f + 0.06f * Mathf.Sin(Time.time * 2f));
            glow.color = g;
            if (near && !GamePause.IsBlocked && WorkshopInput.UsePressed && WorkshopSceneLoader.CanLoad(workshopScenePath))
            {
                WorkshopSceneLoader.Load(workshopScenePath);
            }
        }
    }
}
