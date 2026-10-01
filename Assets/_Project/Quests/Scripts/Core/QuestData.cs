using UnityEngine;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Unveränderliche Quest-Definition (der "Bauplan"). Beschreibt was eine
    /// Quest ist – nie was sie gerade tut. Fortschritt und Zustand leben in
    /// QuestInstance.
    ///
    /// Anlegen: Rechtsklick → Create → SteepingSpirits → Quests → Quest Data,
    /// per JSON (Menü SteepingSpirits → Quest JSON Importer) oder im Code
    /// (ScriptableObject.CreateInstance, siehe TestMeadow).
    ///
    /// Speicherort: Assets/_Project/Quests/Data/[Kategorie]/Q_[KAT]_[Name].asset
    /// </summary>
    [CreateAssetMenu(fileName = "Q_SIDE_NeueQuest", menuName = "SteepingSpirits/Quests/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Identität")]
        [Tooltip("Eindeutiger Bezeichner nach Schema Q_[KATEGORIE]_[Name], z.B. Q_SIDE_SammelKraeuter")]
        public string questID;

        public string displayName;

        [TextArea(2, 5)]
        public string description;

        [Header("Einordnung")]
        public QuestCategory category = QuestCategory.SIDE;

        [Tooltip("Darf nach Abschluss erneut angenommen werden. DAILY-Quests werden " +
                 "jeden Morgen wieder verfügbar (DailyQuestReset)")]
        public bool isRepeatable;

        [Header("Welt (2D)")]
        [Tooltip("Position des World-Markers (Quest-Startpunkt). 0,0 = kein Marker.")]
        public Vector2 startLocation;

        [Header("Inhalt")]
        public ObjectiveData[] objectives;
        public Reward[] rewards;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Asset-Name als Fallback für die questID, damit neue Assets
            // direkt der Namenskonvention folgen.
            if (string.IsNullOrEmpty(questID))
            {
                questID = name;
            }
        }
#endif
    }
}
