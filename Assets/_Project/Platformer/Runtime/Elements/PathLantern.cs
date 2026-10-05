using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Small lantern of the optional lantern challenge (not a checkpoint). Lights up when touched.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class PathLantern : MonoBehaviour
    {
        public int index;
        [SerializeField] private Color unlitColor = new Color(0.40f, 0.38f, 0.42f);
        [SerializeField] private Color litColor = new Color(0.75f, 0.85f, 1f);

        private PlaceholderVisual visual;
        private float litTime = -1f;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            visual = GetComponent<PlaceholderVisual>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (litTime >= 0f || other.GetComponentInParent<JumpNRunPlayer>() == null)
            {
                return;
            }

            litTime = 0f;
            JumpNRunSession session = JumpNRunSession.Current;
            bool wasComplete = session != null && session.Challenge.Complete;
            session?.LightPathLantern(index);
            bool nowComplete = session != null && session.Challenge.Complete;

            JumpNRunSounds.Play(nowComplete && !wasComplete ? JumpNRunSound.CollectRare : JumpNRunSound.Collect, 0.14f);
            JumpNRunParticles.Burst(transform.position, nowComplete && !wasComplete ? 18 : 6, Vector2.up, 70f, 1.2f,
                litColor, 0.12f, 0.9f, -0.4f);
        }

        private void Update()
        {
            if (visual == null || visual.Renderer == null)
            {
                return;
            }

            if (litTime < 0f)
            {
                visual.Renderer.color = unlitColor;
                return;
            }

            litTime += Time.deltaTime;
            float t = Mathf.Clamp01(litTime / 0.6f);
            float breathe = 0.9f + 0.1f * Mathf.Sin(litTime * 2f);
            visual.Renderer.color = Color.Lerp(unlitColor, litColor * breathe, t);
        }
    }
}
