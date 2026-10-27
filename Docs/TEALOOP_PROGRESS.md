# Teekreislauf – Fortschritt (Langlauf)

## Nächster konkreter Schritt

→ erster Backlog-Punkt mit Status „offen“ (Reihenfolge = Priorität; D erst nach B4).

> **Neue Sitzung?** Zuerst diese Datei lesen, dann `Docs/PLAYTEST_NOTES.md` (Befunde haben Vorrang, jeder als
> eigener Backlog-Punkt), dann `CLAUDE.md`. Branch `feature/tea-loop` (Worktree `../SteepingSpirits-tealoop`).
> Nichts nach `main`, kein Force-Push.

## Basis

- `feature/tea-loop` = `origin/feature/processing` (17bced0) + Merge von `origin/feature/brewing-phase2` (70c8dec)
  im Merge-Commit 68d4c81. Konflikte nur in `README.md` und `.gitignore`, beide Seiten übernommen.
  `Packages/manifest.json` bekommt `com.unity.test-framework` aus dem Brühen (brauchen alle EditMode-Tests).
- Damit enthält der Branch alles: Jump'n'Run, Garten, Werkstatt, Brühen. `main` (5629997) ist Vorfahre.

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Branch + Merge Brühen | erledigt (Tests: Brühen 77, Zutaten 12, Garten 108, Jump'n'Run 137, Werkstatt 142) |
| A2 | `PLAYTEST_NOTES.md` | erledigt (Datei fehlte → mit Vorlagen angelegt, keine Befunde) |
| A3 | `CLAUDE.md` | erledigt (Tastentabelle folgt in B6, Speicherdienst in B4) |
| B4 | Gemeinsamer Speicherdienst (Inventar + Uhr) | erledigt (15 Tests; Unity: ungeprüft) |
| B5 | Builder zerstören sich nicht mehr gegenseitig | erledigt (Unity: ungeprüft) |
| B6 | Tastentabelle, Kollisionen | erledigt (1 Kollision behoben; Unity: ungeprüft) |
| C7 | Erste Teestrauch-Ernte = `tea_leaf_first` | offen |
| D8 | `ForProcessedTea` + `ProcessedBrewParams` | offen |
| D9 | Teewahl aus Inventar/Vorratsschrank | offen |
| D10 | Ergebnis: Noten, `tea_tart` | offen |
| D11 | Brühen aus der Welt erreichbar | offen |
| E12 | Simulator-Auswertung | offen |
| F13 | Abschlussbericht | offen |

## Speicherdienst (B4, Modul `SaveGame`)

- Vorher: `PlayerInventory` schrieb beim Beenden `inventory_save.json` (nur IDs/Mengen), lud aber nie – und hätte
  zur Laufzeit angelegte Gegenstände beim Laden verworfen. `GameClock` hatte Speicher-Methoden, die niemand rief.
- Jetzt: `SaveService` (Runtime, `DontDestroyOnLoad`) startet per `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`
  – nach `Awake`, **vor** allen `Start()` – legt `PlayerInventory`/`GameClock` an, falls nötig, und lädt sofort.
  So sieht z. B. die Saatgut-Sicherung des Gartens schon das gespeicherte Saatgut und legt nichts doppelt hin.
- `game_save.json` (Core `GameSaveData`, Version 2): Inventar in Slot-Reihenfolge, `pending` (unbekannt oder kein
  Platz) und Uhr. Version 1 = altes `inventory_save.json` → wird beim ersten Laden übernommen und sofort als
  Version 2 geschrieben (alte Datei bleibt liegen).
- `ItemRecipes` (Core) baut Gegenstände aus der ID: Werkstatt-Tees, Mischungen, getrocknete Kräuter, Blätter
  mit `+qN`, erster Austrieb, Goldspitze, `item_teeblatt`; Werte wie in den Modulen. Der Garten meldet Saatgut
  und Kompost an. Unbekannte IDs warten in `pending` und kommen zurück, sobald eine Szene sie registriert
  (z. B. Tränke der Wiese, die erst in `TestMeadow.Start` angemeldet werden).

## Builder (B5)

- Gewählt: **„jeder Builder entfernt nur, was er selbst angelegt hat“** (kleiner als ein Anmelde-System, und
  robust gegen jede Reihenfolge). Helfer `Core/Editor/OwnedSceneObjects`: Szene öffnen (oder neu anlegen),
  nur die eigenen Wurzelobjekte (per Name) entfernen, neu aufbauen, speichern.
- Geteilte Szenen und ihre Besitzer:

| Szene | Objekt | Besitzer (Builder) |
|---|---|---|
| `TestMeadow.unity` | `TestMeadow`, `HubPortal` | *JumpNRun → Build Levels* |
| `TestMeadow.unity` | `TeegartenPortal` | *Plantation → Build Garden* |
| `Teegarten.unity` | `TeaGarden` | *Plantation → Build Garden* |
| `Teegarten.unity` | `TeewerkstattTuer` | *Processing → Build Workshop* |
| `Teewerkstatt.unity` | `TeaWorkshop` | *Processing → Build Workshop* |

- Folge: *Build Garden* nach *Build Workshop* lässt die Werkstatttür stehen; *Build Levels* nach *Build Garden*
  lässt den Garten-Eingang auf der Wiese stehen. Die Hinweise „danach nochmal bauen“ entfallen.

## Annahmen

1. **Speicherdienst legt Inventar und Uhr in jeder Szene an.** Folge: Auch eine direkt gestartete
   Jump'n'Run-Szene hat jetzt ein Inventar; die Übergabe dort geht ins Inventar statt in den Vorratsschrank
   (beides bleibt gespeichert, nichts geht verloren).

## Editor-Checklisten pro Commit

- **A1** (Merge): Unity öffnen → Konsole ohne Fehler. Menü *SteepingSpirits → Brewing → Build Sandbox* ist da und
  baut die Sandbox; Play → Brühen wie bisher. Test Runner → EditMode: alle Module grün.
- **B6** (Tasten): Tabelle in `CLAUDE.md`. Jump'n'Run-Lichtung mit Gamepad: vor einer Leveltür A drücken →
  Figur springt, Tür öffnet sich **nicht**; Stick/Steuerkreuz hoch → Tür lädt das Level. Tastatur E/F/W wie bisher.
- **B5** (Builder): Reihenfolge testen: *Processing → Build Workshop*, dann *Plantation → Build Garden* →
  `Teegarten` öffnen: Hierarchy hat `TeaGarden` **und** `TeewerkstattTuer`. Dann *JumpNRun → Build Levels* →
  `TestMeadow` hat `TestMeadow`, `HubPortal` **und** `TeegartenPortal`. Jeden Builder zweimal laufen lassen:
  kein Objekt doppelt.
- **B4** (Speicherdienst): Test Runner → `SteepingSpirits.SaveGame.Tests.EditMode` grün (`dotnet test
  Tests/SaveGame.DotNet`). Werkstatt: Tee einpacken → Inventar (I) zeigt ihn → Stop → Play → Tee ist noch im
  Inventar, Uhrzeit/Datum sind wie vorher. In `persistentDataPath` liegt `game_save.json`. Hierarchy im Play:
  `SaveService` unter DontDestroyOnLoad.
- **A3** (nur Doku): `CLAUDE.md` im Repo-Wurzelverzeichnis lesen.
- **A2** (nur Doku): `Docs/PLAYTEST_NOTES.md` hat die Abschnitte Werkstatt, Brühen, Garten, Jump'n'Run mit Vorlage.
