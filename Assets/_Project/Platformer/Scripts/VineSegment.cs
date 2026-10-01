using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Platformer
{
    /// <summary>Ein Glied einer <see cref="Vine"/> – wird von der Liane selbst angelegt.</summary>
    public class VineSegment : MonoBehaviour
    {
        private static readonly List<VineSegment> all = new List<VineSegment>();

        /// <summary>Alle Lianen-Segmente der Szene (für die Greif-Suche).</summary>
        public static IReadOnlyList<VineSegment> All => all;

        public Vine Vine { get; private set; }
        public int Index { get; private set; }
        public Rigidbody2D Body { get; private set; }

        public bool IsGrabbable => Vine != null && Index >= Vine.FirstGrabbableIndex;

        public void Init(Vine vine, int index, Rigidbody2D body)
        {
            Vine = vine;
            Index = index;
            Body = body;
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}
