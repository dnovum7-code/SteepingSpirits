# Jump'n'Run – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann beim ersten Punkt mit Status
> „offen“ oder „in Arbeit“ weitermachen. Branch: `feature/jumpnrun` (von `main`),
> Arbeitsverzeichnis als eigener git worktree (`../SteepingSpirits-jnr`), damit die
> Brüh-Branches unberührt bleiben. Nie nach `main`, kein Force-Push.

## Nächster konkreter Schritt

→ siehe Backlog: erster Punkt mit Status „offen“.

## Erkundung (Stand `main` 5629997)

- **2D**, Seitenansicht. Physik: `Rigidbody2D` (Dynamic, gravityScale 0, eigene
  Schwerkraft im Controller), `CapsuleCollider2D`, Kontakt-Prüfung per `Rigidbody2D.Cast`.
- **Input:** zentrale Klasse `Core/Scripts/GameInput.cs` (neues Input System + Fallback
  alter Input Manager), Platformer nutzt `JumpPressed/JumpHeld/DashPressed/GrabHeld/Move`.
- **Kamera:** `Player/CameraFollow2D` (SmoothDamp, Bounds, statisches Shake). Kein Cinemachine.
- **Szenen:** `Assets/Scenes/JumpAndRun.unity` (ein GameObject mit `PlatformerCourse`) und
  `Assets/Scenes/TestMeadow.unity` (ein GameObject mit `TestMeadow`) – beide von Hand
  geschrieben, Skript-GUIDs über handgeschriebene `.cs.meta`.
- **Skripte:** `Platformer/Scripts/` – `PlatformerController2D` (Celeste-artig: variable
  Sprunghöhe, Coyote, Buffer, Apex, Wandrutschen/-sprung, 8-Wege-Dash, Greifen an
  `GrabPoint` Halten/Schwingen, Physik-Lianen `Vine`), `Hazard2D` (Tod → Checkpoint),
  `Checkpoint2D`, `Goal2D` (meldet Visit-Quest), `PlatformerHUD`, `RopeVisual`,
  `AfterImage`; Aufbau zur Laufzeit in `DevTools/Scripts/PlatformerCourse.cs`
  (6 Abschnitte Start/Dash/Wand/Halten/Schwingen/Lianen, Stacheln = Tod).
- **Ziel laut Historie:** Experimentier-Szene „Kletterpfad“, erreichbar über ein Portal auf
  der Wiese; das Ziel erfüllt die Quest „Der Kletterpfad“ (Quest-System läuft über
  Szenen weiter, DontDestroyOnLoad).
- **Helfer:** `PlaceholderSprites`, `GuiDraw`/`UiFactory`, `GamePause`, `GameInput`,
  `PlayerLocator`, `ProceduralSfx`, `ScenePortal`, `GameClock`.
- **CLAUDE.md:** keine vorhanden. Konventionen aus dem Brüh-Auftrag übernommen
  (Core-asmdef mit noEngineReferences, Parameter in `[Serializable]`-Klassen in
  ScriptableObjects, Szenen/Assets nur per Editor-Skript, Englisch im Code, Spielertexte
  zentral auf Deutsch, sanftes Feedback).

## Annahmen

1. **Zweck:** Der Jump'n'Run ist ein Sammelausflug – die Hauptfigur holt Zutaten fürs
   Teehaus (Teeblätter, Kräuter, Blüten, Morgentau, Quellwasser).
2. **„Die zwei Szenen“** sind `JumpAndRun.unity` und `TestMeadow.unity`. Der Builder
   erzeugt beide neu (gleicher Inhalt, jetzt per Editor-Skript) plus die neuen Level-Szenen.
   Die handgeschriebenen Dateien bleiben im Repo, bis der Builder sie in Unity überschreibt
   (nichts wird gelöscht).
3. **Bestehende Mechaniken bleiben** (Dash, Wand, Greifen, Schwingen, Lianen), werden aber
   über `MovementTuning` einzeln abschaltbar und laufen über die neue Core-Rechnung.
   Der Kletterpfad bleibt als anspruchsvolles Zusatzlevel erhalten.
4. **Gemütlich:** „Tod“ wird überall zum sanften Auffangen durch einen Geist. Stacheln der
   alten Strecke fangen ebenfalls nur auf.
5. **Andere Systeme** (GameInput, CameraFollow2D, ScenePortal, Brüh-Code) werden nicht
   geändert. Wo der Jump'n'Run eigene Bedürfnisse hat (Kamera, Optionen, Debug),
   bekommt er eigene Klassen im Platformer-Modul.
6. **F1** gehört in den neuen Level-Szenen dem Jump'n'Run-Debug-Overlay. In der
   Kletterpfad-Szene liegt zusätzlich das globale Cheat-Fenster auf F1 (beides erscheint).
7. **Schaukel (Nebenaufgabe):** eigenes Element „Schaukel“ – A/D im Takt pumpen baut
   Schwung auf, falscher Takt bremst, abspringen fliegt im Bogen. Wird als
   Geister-Element in Level 2 eingebaut.

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Szenen per Editor-Builder (Menü SteepingSpirits → JumpNRun → Build Levels) | erledigt (Kompilat geprüft, Unity ungeprüft) |
| A2 | Core-Assembly `SteepingSpirits.Platforming.Core` + dotnet-Tests | erledigt (5 Tests grün) |
| A3 | Tuning-ScriptableObjects (Movement, Camera, Feedback) | offen |
| A4 | Level als Daten (Tile-Strings) + Builder baut daraus | offen |
| B1 | Assist-Techniken (Coyote, Buffer, Cut, Apex, Fallschwerkraft, Max-Fall, Ecken, Luftkontrolle) | offen |
| B2 | Kamera (Folgen, Vorausschau, Totzone, vertikal nach Landung) | offen |
| B3 | Feedback (Squash/Stretch, Staub/Blätter, Töne) | offen |
| C1 | Laternen-Checkpoints + sanftes Auffangen | offen |
| C2 | Assist-Optionen (Tempo, Luftsprung, Absturzschutz) + Optionsmenü | offen |
| D1 | Zutaten + Sammelbeutel (Core) + Fang-Karte | offen |
| D2 | Geister-Elemente: Windgeist, Laternengeist, Blattplattform, Tautropfen-Blatt | offen |
| D3 | Level 1 (Einstieg ohne Text) | offen |
| D4 | Level 2 (mehrere Wege, Kletterpassagen, seltene Zutat) | offen |
| S1 | Nebenaufgabe: Schaukel (Pumpen im Takt, Absprung im Bogen) | offen |
| E1 | Debug-Overlay (F1) | offen |
| E2 | Telemetrie (jumpnrun_log.jsonl pro Abschnitt) | offen |
| E3 | Doku Docs/JUMPNRUN.md | offen |
| F* | Extras (Wand-Toggle, Parallax/Abendlicht, Geister-NPCs, Laternen-Herausforderung) | offen |

## Editor-Checklisten pro Commit

### A1 – Szenen-Builder
- [ ] Unity öffnen, Konsole ohne Fehler (ungeprüft).
- [ ] Menü **SteepingSpirits → JumpNRun → Build Levels** → Konsole meldet die gebauten Szenen.
- [ ] `Assets/Scenes/TestMeadow.unity` öffnen → Play → Wiese baut sich wie bisher auf.
- [ ] `Assets/Scenes/JumpAndRun.unity` öffnen → Play → Kletterpfad wie bisher.
- [ ] *File → Build Profiles*: beide Szenen stehen in der Liste.

### A2 – Core-Assembly
- [ ] Unity: Konsole ohne Fehler; im Projekt erscheinen die Assemblies
      `SteepingSpirits.Platforming.Core` und `SteepingSpirits.Platforming.Tests.EditMode` (ungeprüft).
- [ ] *Window → General → Test Runner → EditMode*: Platforming-Tests laufen grün.
- [x] Ohne Unity: `dotnet test Tests/PlatformingCore.DotNet` → grün.
