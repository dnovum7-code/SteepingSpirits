using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Optionaler Marker auf einem Ziel-Objekt (Münze, Gegner, NPC, Ort).
    /// Liefert eine ID – standardmässig einfach der GameObject-Name, damit man
    /// KEINE ID von Hand tippen muss. Pickup / Reporter / NPC lesen sie
    /// automatisch, wenn ihr eigenes ID-Feld leer ist.
    /// </summary>
    public class QuestTarget : MonoBehaviour
    {
        [Tooltip("Leer lassen = der GameObject-Name wird als ID benutzt")]
        [SerializeField] private string id;

        public string Id => string.IsNullOrEmpty(id) ? gameObject.name : id;

        private void Reset()
        {
            id = gameObject.name;
        }
    }
}
