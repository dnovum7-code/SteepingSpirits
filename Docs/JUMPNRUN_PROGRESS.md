# Jump'n'Run – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann beim ersten Punkt mit Status
> „offen“ oder „in Arbeit“ weitermachen. Branch: `feature/jumpnrun` (von `main`),
> Arbeitsverzeichnis als eigener git worktree (`../SteepingSpirits-jnr`), damit die
> Brüh-Branches unberührt bleiben. Nie nach `main`, kein Force-Push.

## Nächster konkreter Schritt

**Runde 2 läuft – siehe Abschnitt „Runde 2“: erster Punkt mit Status „offen“.**

Runde 1: Alle Backlog-Punkte A–F sind umgesetzt und gepusht (91 dotnet-Tests grün, Kompilat gegen
Unity-Referenzen ohne neue Warnungen). **Nächster Schritt: in Unity prüfen** – Projekt öffnen,
*SteepingSpirits → JumpNRun → Build Levels*, dann die Editor-Checklisten unten von A1 an
abhaken. Alles mit „ungeprüft“ ist noch nicht in Unity gesehen worden.

Offene Ideen für danach:
- Spielgefühl in Unity nachstellen (`MovementTuning`, `SpiritElementsTuning.swing`), Telemetrie auswerten.
- Portal von der Wiese zur Lichtung (braucht eine Änderung an `TestMeadow`, bewusst nicht gemacht).
- Sammelbeutel ans Inventar/Teehaus übergeben (IDs sind schon neutral).
- Echte Sprites statt `PlaceholderVisual`, Schaukel-Animation (Beine ausstrecken/anziehen im Takt).

## Runde 2 (Stand: in Arbeit)

**Playtest-Notizen:** `Docs/PLAYTEST_NOTES.md` existiert noch nicht. Sobald sie existiert,
hat sie Vorrang (Fehler → Gefühl → Wünsche, Status und Commit je Notiz).

**Neue Freigaben (je eigener kleiner Commit):** Portal Wiese → Lichtung, Sammelbeutel →
Inventar-Adapter, Kletterpfad auf neuen Controller hinter Schalter (`PlatformerController2D` bleibt).

### Annahmen Runde 2
- R4. Für den Zugriff von Tests und Werkzeugen gibt es eine kleine Assembly
  `SteepingSpirits.Platformer.Hooks` (Eingabe-Schnittstelle, Probe). Der Spielcode liegt in
  Assembly-CSharp; ein asmdef dafür würde andere Module mitziehen, das wäre außerhalb der Freigaben.
- R1. Es gibt ein Inventar (`PlayerInventory`, DontDestroyOnLoad, von der Testwiese angelegt).
  Läuft es, gehen die Zutaten dorthin (Teeblatt → vorhandene ID `item_teeblatt`, damit die
  Sammel-Quest von Oma Hilde mitzählt). Läuft es nicht (Level direkt gestartet), landen sie im
  „Vorratsschrank“ (`pantry.json`, versioniert). Beides gleichzeitig wäre doppelte Buchführung.
- R2. Ein allgemeines Speichersystem fehlt → kleines versioniertes JSON `jumpnrun_save.json`.
- R3. Der Smoke-Test-Bot spielt die Wege nicht pixelgenau nach, sondern steuert mit echten
  Eingaben (über eine Eingabe-Schnittstelle des Spielers) von Stehplatz zu Stehplatz entlang des
  Pfads der Erreichbarkeitsprüfung.

### Backlog Runde 2

| # | Punkt | Status |
|---|---|---|
| R2-1 | Smoke-Test per Menü + PlayMode-Tests | erledigt (Bot schafft alle Level in der Simulation, 97 Tests grün; Unity-Lauf ungeprüft) |
| R2-2 | Eingabe-Aufzeichnung F5/F6 | erledigt (Format + Wiedergabe getestet, auch Replay in der Simulation; Unity ungeprüft) |
| R2-3 | Builder-Prüfungen (Prefab-Referenzen, Erreichbarkeit, Laternen-Reihenfolge) | erledigt (LevelLint 4 Tests, ausgelieferte Level warnungsfrei) |
| R2-4 | Gemeinsamer Zutaten-Katalog `SteepingSpirits.Ingredients` | erledigt (4 eigene Tests + 106 Jump'n'Run-Tests grün) |
| R2-5 | Übergabe Sammelbeutel → Inventar / Vorratsschrank | offen |
| R2-6 | Wege verbinden (Wiese → Lichtung → Level → Lichtung) | offen |
| R2-7 | Speichern (Level, Seltenheiten, Herausforderungen, Komfort) | offen |
| R2-7b | Kletterpfad auf neuen Controller hinter Schalter | offen |
| R2-8 | Spieler-Oberflächen auf UI-Baukasten, Gamepad-Navigation | offen |
| R2-9 | Schaukel-Feinschliff (pro Schaukel, Knarzen, Blätter, Stick, Auto-Schwung) | offen |
| R2-10 | Schaukel-Kombinationen | offen |
| R2-11 | Level 3 (Schaukel-Thema) | offen |
| R2-12 | Hub-Lichtung lebendig | offen |
| R2-13 | Level 4 | offen |
| R2-14 | Pooling, keine Allokationen in Update | offen |
| R2-15 | AudioSet-Asset pro Modul | offen |
| R2-16 | Barrierefreiheit | offen |
| R2-17 | Probe-Merge `integration/probe` | offen |
| R2-18 | Eigene Verbesserungen | offen |

### Editor-Checklisten Runde 2

#### R2-1 – Smoke-Test
- [ ] Menü **SteepingSpirits → JumpNRun → Run Smoke Test** (ungeprüft): baut alles, öffnet jede
      `JumpNRun_*`-Szene, startet Play, ein Bot spielt (Figur läuft selbst), Play endet von allein,
      nächste Szene. Am Ende Konsole „Smoke test finished“ und `Docs/SMOKE_REPORT.md` neu.
- [ ] Abbrechen: **Cancel Smoke Test**.
- [ ] *Test Runner → PlayMode*: 4 Tests (Spawn, Laterne, Auffangen, Schaukel-Absprung) grün.
- [x] Ohne Unity: Bot schafft Level 1 und 2 in der Kachel-Simulation (`RouteBotTests`).

#### R2-4 – Zutaten-Katalog
- [ ] Unity: neue Assembly `SteepingSpirits.Ingredients` (Ordner `Assets/_Project/Ingredients`), Konsole ohne Fehler (ungeprüft).
- [ ] Test Runner EditMode: `IngredientCatalogTests` grün. Ohne Unity: `dotnet test Tests/Ingredients.DotNet`.
- Zuordnung: Jump'n'Run-IDs sind die Katalog-IDs; `item_teeblatt` (Wiese/Inventar/Quest) ist Alias für `tea_leaf`.
  Der Brüh-Code ist unverändert und kann den Katalog später referenzieren.

#### R2-3 – Builder-Prüfungen
- [ ] *Build Levels* mit den ausgelieferten Leveln → keine `[JumpNRun]`-Warnungen (ungeprüft).
- [ ] Probe: in einer Level-Datei eine Zutat hoch in die Luft setzen → Warnung „… is not reachable“;
      ein Prefab unter `Prefabs/Elements` löschen und *nur* die Szene bauen → Warnung „Prefab … missing“
      (beim normalen Build wird es neu erzeugt).

#### R2-2 – Eingabe-Aufzeichnung
- [ ] Level → Play → **F5**: oben rechts „● REC“; spielen; **F5** stoppt → „Aufnahme gespeichert“ (ungeprüft).
- [ ] **F6**: Figur springt an den Aufnahme-Start und spielt die Eingaben nach; F6 bricht ab.
- [ ] Dateien: `persistentDataPath/jumpnrun_recordings/<Level>_<Zeit>.jnrrec` (+ `latest_<Level>.jnrrec`),
      reiner Text, für Fehlermeldungen anhängen. Wiedergabe ist nicht bitgenau (Unity-Physik),
      aber zeigt denselben Ablauf.

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
11. **Spieltempo-Assist** läuft über `Time.timeScale` (nur in Jump'n'Run-Leveln, `JumpNRunTime`
    stellt beim Verlassen 1 wieder her). `GamePause` bleibt unberührt.
12. **Optionen** werden pro Spieler in `PlayerPrefs` gespeichert (Komfort-Einstellung, kein Spielstand).

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Szenen per Editor-Builder (Menü SteepingSpirits → JumpNRun → Build Levels) | erledigt (Kompilat geprüft, Unity ungeprüft) |
| A2 | Core-Assembly `SteepingSpirits.Platforming.Core` + dotnet-Tests | erledigt (5 Tests grün) |
| A3 | Tuning-ScriptableObjects (Movement, Camera, Feedback) | erledigt (Kompilat geprüft) |
| A4 | Level als Daten (Tile-Strings) + Builder baut daraus | erledigt (14 Tests grün, Szene ungeprüft) |
| B1 | Assist-Techniken (Coyote, Buffer, Cut, Apex, Fallschwerkraft, Max-Fall, Ecken, Luftkontrolle) | erledigt (31 Tests grün, Spielgefühl ungeprüft) |
| B2 | Kamera (Folgen, Vorausschau, Totzone, vertikal nach Landung) | erledigt (8 Kamera-Tests grün, Bild ungeprüft) |
| B3 | Feedback (Squash/Stretch, Staub/Blätter, Töne) | erledigt (45 Tests grün, Klang/Bild ungeprüft) |
| C1 | Laternen-Checkpoints + sanftes Auffangen | erledigt (48 Tests grün, Ablauf ungeprüft) |
| C2 | Assist-Optionen (Tempo, Luftsprung, Absturzschutz) + Optionsmenü | erledigt (54 Tests grün, Menü ungeprüft) |
| D1 | Zutaten + Sammelbeutel (Core) + Fang-Karte | erledigt (58 Tests grün, UI ungeprüft) |
| D2 | Geister-Elemente: Windgeist, Laternengeist, Blattplattform, Tautropfen-Blatt | erledigt (63 Tests grün, Verhalten ungeprüft) |
| D3 | Level 1 (Einstieg ohne Text) | erledigt (Erreichbarkeit per Test, Spielgefühl ungeprüft) |
| D4 | Level 2 (mehrere Wege, Kletterpassagen, seltene Zutat) | erledigt (Wege per Test, Spielgefühl ungeprüft) |
| S1 | Nebenaufgabe: Schaukel (Pumpen im Takt, Absprung im Bogen) | erledigt (9 Schaukel-Tests grün, Gefühl ungeprüft) |
| E1 | Debug-Overlay (F1) | erledigt (Kompilat geprüft, Anzeige ungeprüft) |
| E2 | Telemetrie (jumpnrun_log.jsonl pro Abschnitt) | erledigt (84 Tests grün, Datei ungeprüft) |
| E3 | Doku Docs/JUMPNRUN.md | erledigt (Hub und Geist-NPCs folgen in F) |
| F1 | Geister-NPCs mit Einzeilern (Texte zentral) | erledigt (Kompilat + Daten-Test, Anzeige ungeprüft) |
| F2 | Wandrutschen/-sprung abschaltbar | erledigt mit B1 (`MovementParams.wallSlideEnabled/wallJumpEnabled`) |
| F3 | Parallax-Hintergrund + Abendlicht | erledigt (Kompilat geprüft, Bild ungeprüft) |
| F4 | Laternen-Herausforderung | erledigt (89 Tests grün, Ablauf ungeprüft) |
| F5 | Eigene Verbesserung: Hub-Lichtung mit Türen zu allen Leveln | erledigt (91 Tests grün, Szene ungeprüft) |

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

### B2 – Kamera
- [ ] Level 1 → Play: Kamera folgt weich, schaut beim Laufen ~2 Felder voraus (ungeprüft).
- [ ] Auf der Stelle springen → Kamera bleibt vertikal ruhig; Landung auf höherer Plattform →
      Kamera gleitet nach oben. Tiefer Fall → Kamera folgt schon im Fall.
- [ ] Kein Ruckeln beim Laufen (Spieler interpoliert, Kamera in LateUpdate). Kein Wackeln.
- [ ] Am Levelrand zeigt die Kamera nichts außerhalb der Levelbreite.

### B3 – Feedback
- [ ] Level 1 → Play: beim Springen streckt sich die Figur kurz, bei der Landung staucht sie
      (stärker bei tiefem Fall), Füße bleiben am Boden (ungeprüft).
- [ ] Staubwölkchen bei Sprung/Landung, beim Laufen fliegen ab und zu Blätter nach hinten.
- [ ] Töne leise und weich (kein Klicken): Sprung = sanftes Zupfen, Landung = gedämpftes Pusten.
- [ ] Werte in `FeedbackTuning.asset` (Lautstärken, Stauchung) wirken nach Neustart von Play.

### C1 – Laternen und Auffangen
- [ ] Level 1 → Play: an der Laterne vorbeilaufen → sie wird langsam warm-gelb, leiser
      Glockenton, sanftes Glimmen (kein Blitz) (ungeprüft).
- [ ] In ein Loch fallen → Bild dunkelt weich ab (~0,35 s), Geist-Wölkchen, Figur steht an
      der letzten Laterne, Bild hellt auf. Kein Tod-Zähler, keine Wartezeit.
- [ ] Eine frühere Laterne nochmal berühren → Rücksetzpunkt springt nicht zurück.

### C2 – Komfort-Optionen
- [ ] Level 1 → Play → Esc: Pausemenü „Pause · Komfort“, Spiel steht (ungeprüft).
- [ ] Spieltempo mit A/D auf 70 % → alles läuft ruhiger; Extra-Luftsprung an → in der Luft
      nochmal springen; Absturzschutz an → nach Sturz an der letzten sicheren Stelle weiter.
- [ ] Play beenden und neu starten → Einstellungen sind gemerkt.
- [ ] Nach dem Verlassen des Levels läuft die Wiese wieder mit normalem Tempo.

### D1 – Zutaten und Abschlusskarte
- [ ] Level 1 → Play: Zutaten schweben sanft; einsammeln → kleiner Glitzer, Glockenton,
      oben links erscheint kurz der Beutel (ungeprüft).
- [ ] Seltene Zutat (Raute mit Schein) → kurze, weiche Zeitlupe.
- [ ] Ziel erreichen → Karte „Sammelausflug beendet“ mit „gefunden / vorhanden“ je Zutat,
      Laternen; Buttons Nochmal / Zurück zur Wiese (bzw. Weiter, falls `@next`). Leertaste = Hauptweg.
- [ ] Konsole zeigt `[JumpNRun] Bag: …` (IDs:Mengen für spätere Übergabe ans Teehaus).

### D2 – Geister-Elemente
- [ ] *Build Levels* → Prefabs unter `Assets/_Project/Platformer/Prefabs/Elements/` (Lantern, Bramble,
      Ingredient, Goal, WindSpirit, LanternSpirit, DewLeaf) und `SpiritElementsTuning.asset` (ungeprüft).
- [ ] Erst sichtbar, wenn ein Level die Zeichen `W S G F D` nutzt (Level 1/2, D3/D4).
- [ ] Windgeist: in die Säule springen/fallen → wird sanft nach oben getragen, oben lässt es nach.
- [ ] Laternengeist: berühren → folgt der Figur; blasse Geisterplattformen in seinem Licht
      werden fest und deutlich; außerhalb wieder durchlässig.
- [ ] Blattplattform: sinkt langsam unter der Figur, trägt sie mit, steigt danach wieder.
- [ ] Tautropfen-Blatt: Landung federt hoch (mit gehaltener Sprungtaste höher).

### S1 – Schaukel
- [ ] *Build Levels* → Prefab `Elements/PlaygroundSwing.prefab`; sichtbar in Level 2 (Zeichen `O`) (ungeprüft).
- [ ] Auf den Sitz springen oder hineinlaufen → Figur sitzt.
- [ ] D drücken, während die Schaukel nach rechts schwingt, A, während sie nach links schwingt →
      Ausschlag wächst (bis ~80°), beim Durchschwingen leises Rauschen und Blättchen.
- [ ] Eine Taste dauerhaft halten oder gegen die Bewegung drücken → Schaukel wird langsamer.
- [ ] Leertaste → Absprung mit dem Schwung der Schaukel, Flug im Bogen; S → einfach absteigen.
- [ ] Werte unter `SpiritElementsTuning.asset → swing` (Seillänge, Pump/Bremse, Absprung).

### D3 – Level 1 „Morgenwiese“
- [ ] *Build Levels* → `Assets/Scenes/JumpNRun_Level1.unity` öffnen → Play (ungeprüft).
- [ ] Ablauf ohne Text: laufen → kleine Stufe → Blätter in der Luft (Springen lohnt) → 2er-Lücke
      mit Blättern im Bogen → Laterne → Treppe → 3er-Lücke → Plattformen nach oben (Blüte),
      darüber optional die seltene Goldspitze → 4er-Lücke → Laterne → Brücke aus dünnen
      Plattformen (Morgentau) → Quellwasser → Ziel.
- [ ] In eine Lücke fallen → sanftes Auffangen an der letzten Laterne.
- [ ] Ohne Unity: `dotnet test Tests/PlatformingCore.DotNet` prüft, dass Ziel, Laternen und alle
      Zutaten erreichbar sind (grob, siehe `LevelReachability`).

### D4 – Level 2 „Geisterhain“
- [ ] *Build Levels* → `JumpNRun_Level2.unity`; Level 1 → Ziel → „Weiter“ lädt Level 2 (ungeprüft).
- [ ] Teich mit sinkenden Blättern → Laterne → zwei Wege:
      unten über Dornenranken und die **Schaukel** über die Schlucht,
      oben mit dem **Windgeist**, Laufsteg, **Tautropfen-Blatt** (optional hoch zum Sternentau).
- [ ] Beide Wege treffen sich an der zweiten Laterne; der **Laternengeist** folgt und macht die
      Geistertreppe über die Klamm fest; optional höher hinauf zur Geisterblüte.
- [ ] Ohne Unity: Test prüft, dass jeder Weg allein zum Ziel führt und ohne beide nicht.

### E1 – Debug-Overlay
- [ ] Level → Play → F1 (oder ^): Kasten unten links mit Tempo, grounded, Coyote-/Puffer-Balken,
      Schwerkraft-Phase (rise/apex/fall/wall), Luftsprüngen, Zone (ground/one-way/leaf/ghost/dew/
      wind/swing/catch), Abschnitt, letzter Laterne, sicherem Punkt, Assists und timeScale (ungeprüft).
- [ ] Springen: Coyote-Balken leert sich nach Verlassen der Kante, Puffer füllt sich beim Drücken.

### E2 – Telemetrie
- [ ] Level spielen, eine Laterne entzünden, einmal fallen, Ziel erreichen (ungeprüft).
- [ ] `Application.persistentDataPath/jumpnrun_log.jsonl` (Windows: `%USERPROFILE%/AppData/LocalLow/<Firma>/<Projekt>/`)
      enthält pro Abschnitt eine Zeile: `section`, `seconds`, `falls`, `ingredients`, `assists`, `end`
      (`lantern`/`goal`/`left`).
- [ ] Level mitten im Abschnitt verlassen → letzte Zeile mit `"end":"left"`.

### E3 – Doku
- [ ] `Docs/JUMPNRUN.md` lesen; README verlinkt darauf.

### F1 – Geister-NPCs
- [ ] Level 1/2 → an einem blassen Geist vorbeilaufen → Sprechblase mit einem Satz blendet
      weich ein und wieder aus (ungeprüft). Sätze stehen in `JumpNRunTexts.NpcLine`.

### F3 – Parallax und Abendlicht
- [ ] Level 1 (Morgen) und Level 2 (`@mood evening`): drei Hügel-Ebenen bewegen sich beim Laufen
      unterschiedlich schnell; Level 2 hat warmen Abendhimmel und einen leichten Lichtschleier (ungeprüft).
- [ ] `@mood night` in einer Level-Datei → dunkelblaue Palette nach *Build Levels*.

### F4 – Laternen-Herausforderung
- [ ] Level 1/2: kleine Rauten-Laternen (`l`) leuchten beim Berühren bläulich auf (ungeprüft).
- [ ] Alle entzünden → hellerer Glockenakkord, Quellkristall landet im Beutel; Abschlusskarte zeigt
      „Alle Pfadlaternen leuchten …“, sonst „Laternenpfad: x von y“. Kein Zeitlimit, kein Nachteil.

### F5 – Hub-Lichtung
- [ ] *Build Levels* → `Assets/Scenes/JumpNRun_Hub.unity` → Play (ungeprüft).
- [ ] Vor einer Tür steht ihr Name (Zur Wiese, Morgenwiese, Geisterhain, Kletterpfad); W oder E → Szene wechselt.
- [ ] Level beenden → „Zurück“ führt zur Lichtung zurück (bzw. zur Wiese, wenn das Level direkt gestartet wurde).
- [ ] Hinweis: Die Wiese selbst hat (noch) kein Portal zur Lichtung – `TestMeadow` wurde bewusst nicht geändert.
