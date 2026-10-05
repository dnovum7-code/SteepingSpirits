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
8. **Level-Format:** Textdateien `Assets/_Project/Platformer/Levels/*.txt` (Legende in
   `Core/LevelLayout.cs`). Kein `.asset` – Textdateien sind Daten, keine Unity-Assets im Sinne
   der Regel. Der Builder erzeugt daraus `Assets/Scenes/JumpNRun_<id>.unity`.
9. **Platzhalter-Grafik** entsteht zur Laufzeit (`PlaceholderVisual`), damit keine
   Laufzeit-Texturen in Szenen gespeichert werden; im Editor zeigen Gizmos die Objekte.
10. **Neuer Spieler statt Umbau:** Die neuen Level bekommen einen eigenen Controller auf
    Basis des Cores. Der Kletterpfad (`JumpAndRun.unity`) behält den alten
    `PlatformerController2D` unverändert (ersetzt Annahme 3/4 für diese eine Szene).

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Szenen per Editor-Builder (Menü SteepingSpirits → JumpNRun → Build Levels) | erledigt (Kompilat geprüft, Unity ungeprüft) |
| A2 | Core-Assembly `SteepingSpirits.Platforming.Core` + dotnet-Tests | erledigt (5 Tests grün) |
| A3 | Tuning-ScriptableObjects (Movement, Camera, Feedback) | erledigt (Kompilat geprüft) |
| A4 | Level als Daten (Tile-Strings) + Builder baut daraus | erledigt (14 Tests grün, Szene ungeprüft) |
| B1 | Assist-Techniken (Coyote, Buffer, Cut, Apex, Fallschwerkraft, Max-Fall, Ecken, Luftkontrolle) | erledigt (31 Tests grün, Spielgefühl ungeprüft) |
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

### A3 – Tuning-Assets
- [ ] *Build Levels* legt `Assets/_Project/Platformer/Data/MovementTuning.asset`,
      `CameraTuning.asset`, `FeedbackTuning.asset` an (ungeprüft).
- [ ] Wert in einem Asset ändern → *Build Levels* erneut → Wert bleibt erhalten.

### A4 – Level als Daten
- [ ] *Build Levels* erzeugt `Assets/Scenes/JumpNRun_Level1.unity` (ungeprüft).
- [ ] Szene öffnen: Hierarchie `Level_Level1/Geometry` (Boden-Blöcke, dünne Plattformen mit
      PlatformEffector2D) und `Objects` (pinke Rauten als Platzhalter); Gizmos zeigen die Blöcke.
- [ ] Tippfehler in der Level-Datei (z. B. `x`) → *Build Levels* meldet Zeile/Spalte in der Konsole.

### B1 – Bewegung mit Assists
- [ ] *Build Levels* → `Assets/_Project/Platformer/Prefabs/JumpNRunPlayer.prefab` entsteht (ungeprüft).
- [ ] `JumpNRun_Level1.unity` → Play: Figur (heller Block) steht am Start, A/D laufen,
      Leertaste springt; kurz tippen = kleiner Hüpfer, halten = voller Sprung (~3,2 Felder).
- [ ] Coyote: knapp nach der Kante noch springen geht; Puffer: kurz vor der Landung drücken springt sofort.
- [ ] Kopf stößt knapp an eine Kante → rutscht seitlich vorbei statt abzuprallen.
- [ ] Dünne Plattform (`=`): von unten durchspringen, mit S + Leertaste nach unten fallen.
- [ ] Werte in `MovementTuning.asset` während Play ändern → wirken sofort.
