using UnityEngine;
using UnityEngine.Rendering;

namespace SteepingSpirits.Player
{
    /// <summary>
    /// 2D-Kamera folgt dem Spieler weich (ersetzt CameraControll aus Everdawn).
    /// Optional auf einen Weltbereich begrenzt, damit man nicht über den
    /// Kartenrand hinaus schaut. Auf die Kamera legen.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Tooltip("Nachzieh-Zeit (s). 0 = starr")]
        [SerializeField] private float smoothTime = 0.12f;

        [Tooltip("Sichtbare halbe Höhe in Einheiten (orthographicSize)")]
        [SerializeField] private float orthographicSize = 6f;

        [Header("Begrenzung (optional)")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Rect worldBounds = new Rect(-20f, -15f, 40f, 30f);

        [Header("Sortierung")]
        [Tooltip("Sprites nach Y sortieren (weiter unten = davor) – typisch für Top-Down")]
        [SerializeField] private bool sortByY = true;

        private Camera cam;
        private Vector3 velocity;
        private Vector3 smoothed;

        // Bildschirm-Wackeln (Dash, Tod) – global, weil es pro Szene nur eine Kamera gibt.
        private static float shakeUntil;
        private static float shakeMagnitude;

        /// <summary>Kamera kurz wackeln lassen (z.B. beim Dash). magnitude in Einheiten.</summary>
        public static void Shake(float magnitude, float duration)
        {
            shakeMagnitude = Mathf.Max(magnitude, Time.unscaledTime < shakeUntil ? shakeMagnitude : 0f);
            shakeUntil = Mathf.Max(shakeUntil, Time.unscaledTime + duration);
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>Sichtbare halbe Höhe (Zoom).</summary>
        public float OrthographicSize
        {
            get => orthographicSize;
            set
            {
                orthographicSize = Mathf.Max(1f, value);
                if (cam != null) cam.orthographicSize = orthographicSize;
            }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;

            if (sortByY)
            {
                // Kamera + global (Built-in/URP Universal). Beim URP-2D-Renderer
                // stattdessen im Renderer2D-Asset „Transparency Sort Mode = Custom
                // Axis (0,1,0)" einstellen.
                cam.transparencySortMode = TransparencySortMode.CustomAxis;
                cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
                GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
                GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
            }
        }

        private void Start()
        {
            smoothed = transform.position;
            if (target == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    target = p.transform;
                }
            }

            SnapToTarget();
        }

        /// <summary>Begrenzung aus Code setzen (z.B. Test-Szene).</summary>
        public void SetBounds(Rect bounds)
        {
            worldBounds = bounds;
            useBounds = true;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothed = Clamp(new Vector3(target.position.x, target.position.y, transform.position.z));
            transform.position = smoothed;
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 next = smoothTime > 0f
                ? Vector3.SmoothDamp(smoothed, goal, ref velocity, smoothTime)
                : goal;
            smoothed = Clamp(next);

            Vector3 shake = Vector3.zero;
            if (Time.unscaledTime < shakeUntil)
            {
                Vector2 r = Random.insideUnitCircle * shakeMagnitude;
                shake = new Vector3(r.x, r.y, 0f);
            }

            transform.position = smoothed + shake;
        }

        private Vector3 Clamp(Vector3 pos)
        {
            if (!useBounds || cam == null)
            {
                return pos;
            }

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = worldBounds.xMin + halfW, maxX = worldBounds.xMax - halfW;
            float minY = worldBounds.yMin + halfH, maxY = worldBounds.yMax - halfH;

            pos.x = minX > maxX ? worldBounds.center.x : Mathf.Clamp(pos.x, minX, maxX);
            pos.y = minY > maxY ? worldBounds.center.y : Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }
    }
}
