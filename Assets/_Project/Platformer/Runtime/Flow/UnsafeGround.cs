using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Marks ground that does not count as safe for fall protection (moving, appearing).</summary>
    public class UnsafeGround : MonoBehaviour
    {
        private static readonly HashSet<Collider2D> colliders = new HashSet<Collider2D>();
        private readonly List<Collider2D> own = new List<Collider2D>();

        /// <summary>Allocation-free check used every physics step by the session.</summary>
        public static bool Contains(Collider2D c) => c != null && colliders.Contains(c);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => colliders.Clear();

        private void OnEnable()
        {
            GetComponentsInChildren(true, own);
            foreach (Collider2D c in own) colliders.Add(c);
        }

        private void OnDisable()
        {
            foreach (Collider2D c in own) colliders.Remove(c);
        }
    }
}
