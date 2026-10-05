using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Moves a background layer with the camera by a factor (0 = fixed in the
    /// world, 1 = glued to the camera). Far layers use high factors.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float factor = 0.5f;

        /// <summary>Vertical movement is damped further so hills do not bob.</summary>
        [Range(0f, 1f)] public float verticalFactor = 0.9f;

        private Transform cam;
        private Vector3 startPosition;
        private Vector3 cameraStart;

        private void Start()
        {
            if (Camera.main != null)
            {
                cam = Camera.main.transform;
                cameraStart = cam.position;
            }

            startPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                return;
            }

            Vector3 d = cam.position - cameraStart;
            transform.position = startPosition + new Vector3(d.x * factor, d.y * Mathf.Max(factor, verticalFactor), 0f);
        }
    }
}
