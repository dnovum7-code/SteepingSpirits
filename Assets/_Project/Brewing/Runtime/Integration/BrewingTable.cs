using UnityEngine;
using SteepingSpirits.Brewing.Presentation;
using SteepingSpirits.Core;
using SteepingSpirits.Player;
using SteepingSpirits.Processing;

namespace SteepingSpirits.Brewing.Integration
{
    /// <summary>
    /// The brewing table the brewing builder places into the workshop scene. Stand next to it and press the
    /// use key: the brewing scene opens on the player's own teas, and Esc there leads back here. Loads with
    /// <see cref="WorkshopSceneLoader"/> (a sub-place), so the garden gate keeps leading to the meadow.
    /// </summary>
    public class BrewingTable : MonoBehaviour
    {
        [SerializeField] private string brewingScenePath = "Assets/_Project/Brewing/Scenes/BrewingSandbox.unity";
        [SerializeField] private float reach = 1.3f;

        private Transform player;
        private PlayerController2D mover;
        private SpriteRenderer glow;
        private TextMesh label;
        private float messageUntil;

        public void Configure(string scenePath) => brewingScenePath = scenePath;

        private void Start()
        {
            PlaceholderSprites.CreateSpriteObject("Tischplatte", PlaceholderSprites.Square, new Color(0.45f, 0.30f, 0.18f), transform.position, new Vector2(0.95f, 0.7f), 3, transform)
                .transform.localPosition = Vector3.zero;
            PlaceholderSprites.CreateSpriteObject("Kessel", PlaceholderSprites.Circle, new Color(0.55f, 0.52f, 0.50f), transform.position, new Vector2(0.38f, 0.38f), 4, transform)
                .transform.localPosition = new Vector3(-0.2f, 0.08f, 0f);
            PlaceholderSprites.CreateSpriteObject("Schale", PlaceholderSprites.Circle, new Color(0.92f, 0.88f, 0.80f), transform.position, new Vector2(0.24f, 0.18f), 4, transform)
                .transform.localPosition = new Vector3(0.22f, -0.05f, 0f);
            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.6f);
            glow = PlaceholderSprites.CreateSpriteObject("Licht", PlaceholderSprites.Circle, new Color(1f, 0.85f, 0.55f, 0f), transform.position, new Vector2(1.6f, 1.6f), 2, transform);
            glow.transform.localPosition = Vector3.zero;

            var text = new GameObject("Beschriftung");
            text.transform.SetParent(transform, false);
            text.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            label = text.AddComponent<TextMesh>();
            label.text = BrewTexts.TableLabel;
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
                mover = p.GetComponent<PlayerController2D>();
            }

            bool near = Vector2.Distance(player.position, transform.position) <= reach;
            float a = Mathf.MoveTowards(label.color.a, near || Time.unscaledTime < messageUntil ? 1f : 0f, Time.deltaTime * 3f);
            label.color = new Color(label.color.r, label.color.g, label.color.b, a);
            Color g = glow.color;
            g.a = a * (0.18f + 0.06f * Mathf.Sin(Time.time * 2f));
            glow.color = g;
            if (Time.unscaledTime >= messageUntil) label.text = BrewTexts.TableLabel;

            // A station window in the workshop locks the figure – then the use key belongs to that window.
            bool free = !GamePause.IsBlocked && (mover == null || !mover.MovementLocked);
            if (near && free && WorkshopInput.UsePressed) Open();
        }

        private void Open()
        {
            if (!WorkshopSceneLoader.CanLoad(brewingScenePath))
            {
                Say(BrewTexts.TableNoScene);
                return;
            }

            if (OwnTeaShelf.Load(null).Count == 0)
            {
                Say(BrewTexts.NoOwnTeas);
                return;
            }

            BrewHandoff.RequestedTeaId = null;
            BrewHandoff.ReturnScenePath = gameObject.scene.path;
            WorkshopSceneLoader.Load(brewingScenePath);
        }

        private void Say(string text)
        {
            label.text = text;
            messageUntil = Time.unscaledTime + 3f;
        }
    }
}
