# Teegarten – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann `Docs/PLAYTEST_NOTES.md` (Teegarten-Notizen haben
> Vorrang), dann beim ersten Backlog-Punkt mit Status „offen“ weitermachen.
> Branch `feature/plantation` (Worktree `../SteepingSpirits-garden`). Nichts nach `main`, kein Force-Push.

## Nächster konkreter Schritt

→ erster Backlog-Punkt mit Status „offen“.

## Basis und Merge-Hinweis

- `main` (5629997) enthält den Zutaten-Katalog `SteepingSpirits.Ingredients` **nicht**. Deshalb ist
  `feature/plantation` von **`feature/jumpnrun` (795c5f4)** abgezweigt. Beim Merge zuerst `feature/jumpnrun`,
  dann `feature/plantation` nach `main` bringen.
- `CLAUDE.md` gibt es im Repo noch nicht; die Konventionen aus dem Auftrag gelten (Vorschläge für die
  Datei stehen im Abschlussbericht).
- `Docs/PLAYTEST_NOTES.md` existiert nicht (Stand Start).

## Erkundung

- **Perspektive:** Die Welt außerhalb des Jump'n'Run ist **Top-down** (Zelda/Stardew). `PlayerController2D`
  bewegt die Figur frei (4,5 Einheiten/s) mit Blickrichtung in 4 Richtungen (`Facing`). Die Testwiese baut
  Spieler, Kamera (`CameraFollow2D`) und Systeme zur Laufzeit im Code auf.
- **Inventar:** `PlayerInventory` (DontDestroyOnLoad, Katalog ID → `ItemData`, `RegisterItem` zur Laufzeit,
  `AddById`, `Remove`, `Count`, eigenes `inventory_save.json`). Die Testwiese legt es an.
- **Zutaten-Katalog:** `SteepingSpirits.Ingredients` (engine-frei): IDs, Seltenheit, Familie, deutsche Namen;
  `Pantry` = Vorratsschrank `pantry.json` (versioniert) mit kleinem JSON-Leser `MiniJson`.
- **Übergabe:** `IngredientHandover` (Jump'n'Run-Modul) legt Zutaten ins laufende Inventar, sonst in den
  Vorratsschrank. Kennt keine Qualität.
- **Spielstände:** keine gemeinsame Speicherung; jedes Modul hat ein eigenes, versioniertes JSON
  (`jumpnrun_save.json`, `pantry.json`, `inventory_save.json`).
- **Tag:** `GameClock` (World-Modul, DontDestroyOnLoad): Tag, Jahreszeit (28 Tage), Uhrzeit, Event
  `OnNewDay`, `AdvanceToNextDay()`; `Bed` springt per Dialog zum nächsten Morgen. Kein Wetter.

## Annahmen

1. **Raster:** 1 Feld = 1 Unity-Einheit, Top-down. Der Core arbeitet nur mit Rasterkoordinaten.
2. **Tag:** Der Garten nutzt `GameClock` über eine schmale Schnittstelle `IDayClock` (Adapter im Modul).
   Läuft keine `GameClock` (Szene direkt gestartet), legt der Garten eine an – wie die Testwiese es tut.
   Verpasste Tage (Schlafen im Bett auf der Wiese) holt der Garten beim nächsten Betreten nach; sie zählen
   als ungegossene Tage (also nur Wachstumsverlust, nie Tod).
3. **Wetter:** gibt es noch nicht → deterministisches Regen-Orakel im Core (Seed + Tag, Startwert 15 %).
4. **Qualität:** drei Stufen 1 „gut“, 2 „fein“, 3 „erlesen“. Weil die bestehende Übergabe (Jump'n'Run-Code,
   nur lesen) keine Qualität kennt, bekommt der Garten einen eigenen kleinen Adapter mit demselben Verhalten
   (Inventar, sonst Vorratsschrank). Die Qualität steckt in der Zutaten-ID-Variante `<id>+q<n>`
   (Katalog-Erweiterung `IngredientQuality`, additiv); Stufe 1 benutzt die normale ID.
5. **Saatgut** sind Inventar-Gegenstände `seed_<pflanze>` (Kategorie Material), keine Zutaten.
6. **Benutzen-Taste:** Leertaste / J / linke Maustaste / Pad X (wie Angriff auf der Wiese – im Garten gibt
   es keinen Kampf). Brunnen und Laterne werden mit derselben Taste benutzt (Feld vor der Figur).

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Raster, Beetfelder, Aktionsgültigkeit (rein, getestet) | offen |
| A2 | Tageswechsel (`IDayClock`, Trocknen, Regen) | offen |
| A3 | Wachstum (Stufen, Nachwachsen, Determinismus) | offen |
| A4 | Wasserbedarf-Typen | offen |
| A5 | Qualität | offen |
| B6 | PlantDefinition (ScriptableObject) | offen |
| B7 | Startpflanzen + Katalog-Ergänzungen | offen |
| C8 | Werkzeuge (Hacke, Saatbeutel, Kanne, Hand, Schaufel) | offen |
| C9 | Zielfeld mit Vorschau | offen |
| C10 | Werkzeugleiste und Steuerung | offen |
| C11 | Komfort (1×3-Kanne, Gartenhilfe) | offen |
| D12 | Erde-Darstellung | offen |
| D13 | Pflanzen-Darstellung | offen |
| D14 | Klang und Partikel, `PlantationAudioSet` | offen |
| E15 | Ernte → Inventar/Vorratsschrank mit Qualität | offen |
| E16 | Speichern `garden_save.json` | offen |
| E17 | Szene per Builder | offen |
| E18 | Eingang von der Wiese | offen |
| F19 | Debug-Overlay F1 + „Nächster Tag“ | offen |
| F20 | Telemetrie `garden_log.jsonl` | offen |
| G21 | Kompost | offen |
| G22 | Saatgut aus Ernte | offen |
| G23 | Regengeist | offen |
| G24 | Jahreszeiten-Daten | offen |
| G25 | Eigene Verbesserungen | offen |

## Editor-Checklisten pro Commit

_(wird pro Commit ergänzt)_
