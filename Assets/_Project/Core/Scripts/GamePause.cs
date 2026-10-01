using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Core
{
    /// <summary>
    /// „Spielwelt anhalten / Spieler-Eingabe sperren", solange z.B. ein Dialog
    /// oder das Questlog offen ist (wie in Zelda/Stardew: Zeit und Gegner
    /// stehen still). Mehrere Systeme können gleichzeitig blockieren – erst
    /// wenn alle freigegeben haben, läuft das Spiel weiter.
    ///
    /// Bewusst NICHT über Time.timeScale, damit Dev-Cheats (Zeitlupe) und
    /// UI-Animationen unabhängig bleiben.
    /// </summary>
    public static class GamePause
    {
        private static readonly HashSet<object> blockers = new HashSet<object>();
        private static int releasedFrame = -1;

        /// <summary>Neuer Zustand (true = blockiert) bei jeder Änderung.</summary>
        public static event Action<bool> OnChanged;

        /// <summary>
        /// True, solange mindestens ein Blocker aktiv ist – und noch im Frame
        /// der Freigabe. Letzteres verhindert, dass die Taste, die einen Dialog
        /// schliesst, im selben Frame gleich angreift oder den NPC neu anspricht.
        /// </summary>
        public static bool IsBlocked => blockers.Count > 0 || Time.frameCount == releasedFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            blockers.Clear();
            releasedFrame = -1;
            OnChanged = null;
        }

        /// <summary>Blockiert (true) oder gibt frei (false) für den angegebenen Besitzer.</summary>
        public static void Set(object owner, bool blocked)
        {
            if (owner == null)
            {
                return;
            }

            bool wasBlocked = blockers.Count > 0;
            if (blocked)
            {
                blockers.Add(owner);
            }
            else if (blockers.Remove(owner) && blockers.Count == 0)
            {
                releasedFrame = Time.frameCount;
            }

            bool isBlocked = blockers.Count > 0;
            if (isBlocked != wasBlocked)
            {
                OnChanged?.Invoke(isBlocked);
            }
        }
    }
}
