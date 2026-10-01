using System;
using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.World
{
    public enum Season
    {
        Fruehling,
        Sommer,
        Herbst,
        Winter
    }

    /// <summary>
    /// Spielzeit im Stardew-Stil (ersetzt den 3D-DayNightCycle aus Everdawn):
    /// Der Tag läuft von 6:00 bis 2:00 nachts in 10-Minuten-Schritten, danach
    /// beginnt automatisch der nächste Tag. 28 Tage pro Jahreszeit, 4 Jahreszeiten.
    ///
    /// Andere Systeme hören auf die Events (Tag/Nacht-Tönung, Daily-Quests,
    /// später Pflanzen-Wachstum, Läden, NPC-Tagesabläufe …). Während eines
    /// Dialogs/Menüs steht die Uhr still (GamePause).
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public const int MinutesPerTick = 10;

        private static readonly string[] WeekdayNames = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
        private static readonly string[] SeasonNames = { "Frühling", "Sommer", "Herbst", "Winter" };

        public static GameClock Instance { get; private set; }

        [Header("Tempo")]
        [Tooltip("Echte Sekunden pro 10 Spielminuten (Stardew: ~7s → ein Tag ≈ 14 Min.)")]
        [SerializeField] private float realSecondsPerTick = 7f;

        [Header("Tagesablauf")]
        [SerializeField] private int dayStartHour = 6;
        [Tooltip("Ende des Tages. Werte über 24 = nach Mitternacht (26 = 2:00 Uhr)")]
        [SerializeField] private int dayEndHour = 26;

        [Header("Kalender")]
        [SerializeField] private int daysPerSeason = 28;
        [SerializeField] private int startDay = 1;
        [SerializeField] private Season startSeason = Season.Fruehling;
        [SerializeField] private int startYear = 1;

        [Tooltip("Steht still, solange Dialog/Questlog offen ist")]
        [SerializeField] private bool pauseWhileBlocked = true;

        private int minuteOfDay;   // Minuten seit 0:00 (kann > 24*60 sein, bis dayEndHour)
        private float tickTimer;

        /// <summary>Alle 10 Spielminuten.</summary>
        public static event Action<GameClock> OnTimeChanged;

        /// <summary>Morgens um dayStartHour eines NEUEN Tages (nicht beim Spielstart).</summary>
        public static event Action<GameClock> OnNewDay;

        /// <summary>Wenn ein neuer Tag in einer neuen Jahreszeit beginnt.</summary>
        public static event Action<GameClock> OnSeasonChanged;

        public int Day { get; private set; }
        public Season Season { get; private set; }
        public int Year { get; private set; }

        /// <summary>Stunde 0–23 (für die Anzeige).</summary>
        public int Hour => (minuteOfDay / 60) % 24;
        public int Minute => minuteOfDay % 60;

        /// <summary>Uhrzeit als Kommazahl inkl. Bruchteil des laufenden Ticks (6.5 = 6:30). Kann über 24 gehen.</summary>
        public float TimeOfDayHours => (minuteOfDay + Mathf.Clamp01(tickTimer / Mathf.Max(0.01f, realSecondsPerTick)) * MinutesPerTick) / 60f;

        /// <summary>Fortschritt des Tages 0..1 (dayStartHour → dayEndHour).</summary>
        public float DayProgress01 => Mathf.InverseLerp(dayStartHour, dayEndHour, TimeOfDayHours);

        /// <summary>Fortlaufende Tagesnummer seit Spielbeginn (1, 2, 3, …).</summary>
        public int TotalDays => (Year - 1) * daysPerSeason * 4 + (int)Season * daysPerSeason + Day;

        public string WeekdayName => WeekdayNames[(Day - 1) % 7];
        public string SeasonName => SeasonNames[(int)Season];

        /// <summary>„08:40" (24h-Format).</summary>
        public string TimeLabel => $"{Hour:00}:{Minute:00}";

        /// <summary>„Mo, 3. Frühling" .</summary>
        public string DateLabel => $"{WeekdayName}, {Day}. {SeasonName}";

        /// <summary>Faktor aufs Uhrtempo (Dev-Cheats). 0 = angehalten.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Manuell anhalten (z.B. Zwischensequenz).</summary>
        public bool Paused { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Datum/Uhrzeit bleiben beim Szenenwechsel erhalten
            Day = Mathf.Clamp(startDay, 1, daysPerSeason);
            Season = startSeason;
            Year = Mathf.Max(1, startYear);
            minuteOfDay = dayStartHour * 60;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            OnTimeChanged?.Invoke(this);
        }

        private void Update()
        {
            if (Paused || (pauseWhileBlocked && GamePause.IsBlocked) || realSecondsPerTick <= 0f)
            {
                return;
            }

            tickTimer += Time.deltaTime * SpeedMultiplier;
            while (tickTimer >= realSecondsPerTick)
            {
                tickTimer -= realSecondsPerTick;
                Tick();
            }
        }

        private void Tick()
        {
            minuteOfDay += MinutesPerTick;

            if (minuteOfDay >= dayEndHour * 60)
            {
                // Zu lange wach → umkippen, nächster Morgen (wie Stardew um 2:00).
                AdvanceToNextDay();
                return;
            }

            OnTimeChanged?.Invoke(this);
        }

        /// <summary>Springt zum nächsten Morgen (Schlafen / Umkippen).</summary>
        public void AdvanceToNextDay()
        {
            Season before = Season;

            Day++;
            if (Day > daysPerSeason)
            {
                Day = 1;
                if (Season == Season.Winter)
                {
                    Season = Season.Fruehling;
                    Year++;
                }
                else
                {
                    Season = (Season)((int)Season + 1);
                }
            }

            minuteOfDay = dayStartHour * 60;
            tickTimer = 0f;

            OnNewDay?.Invoke(this);
            if (Season != before)
            {
                OnSeasonChanged?.Invoke(this);
            }

            OnTimeChanged?.Invoke(this);
        }

        /// <summary>Uhrzeit setzen (z.B. Cheats). hour darf bis dayEndHour gehen.</summary>
        public void SetTime(int hour, int minute = 0)
        {
            int target = Mathf.Clamp(hour * 60 + minute, dayStartHour * 60, dayEndHour * 60 - MinutesPerTick);
            minuteOfDay = target - target % MinutesPerTick;
            tickTimer = 0f;
            OnTimeChanged?.Invoke(this);
        }

        // ---------------- Speichern / Laden ----------------

        [Serializable]
        public class ClockSaveData
        {
            public int day;
            public Season season;
            public int year;
            public int minuteOfDay;
        }

        public ClockSaveData ToSaveData()
        {
            return new ClockSaveData { day = Day, season = Season, year = Year, minuteOfDay = minuteOfDay };
        }

        public void ApplySaveData(ClockSaveData save)
        {
            if (save == null)
            {
                return;
            }

            Day = Mathf.Clamp(save.day, 1, daysPerSeason);
            Season = save.season;
            Year = Mathf.Max(1, save.year);
            minuteOfDay = Mathf.Clamp(save.minuteOfDay, dayStartHour * 60, dayEndHour * 60 - MinutesPerTick);
            tickTimer = 0f;
            OnTimeChanged?.Invoke(this);
        }
    }
}
