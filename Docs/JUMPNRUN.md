# Jump'n'Run – Sammelausflüge in die Geisterwelt

Gemütlicher Plattform-Teil von „Steeping Spirits“: Die Figur sammelt Zutaten für das
Teehaus (Teeblätter, Kräuter, Blüten, Morgentau, Quellwasser – seltene Varianten an
optionalen Stellen). Es gibt **keinen Tod, keine Leben und keinen Zeitdruck**: Wer fällt,
wird von einem Geist aufgefangen und an der letzten Laterne (oder auf Wunsch an der letzten
sicheren Stelle) wieder abgesetzt.

> Der alte Kletterpfad (`Assets/Scenes/JumpAndRun.unity`, Celeste-Steuerung mit Dash,
> Greifen und Lianen) bleibt erhalten – siehe [JUMP_AND_RUN.md](JUMP_AND_RUN.md). Über den Menü-Schalter
> **SteepingSpirits → JumpNRun → Climbing Path Uses New Controller** läuft er mit dem neuen Controller
> und dem sanften Auffangen (Standard: aus, bis bestätigt).

**Weg durchs Spiel:** Wiese → türkises Portal → Lichtung → Türen zu den Leveln → Abschlusskarte →
„Zur Lichtung“. Neue Level öffnen sich, wenn das vorige geschafft ist.

## Starten

1. Unity öffnen, Menü **SteepingSpirits → JumpNRun → Build Levels**.
   Der Builder legt Tuning-Assets an (falls noch nicht vorhanden), erzeugt alle Prefabs neu und
   baut die Szenen:
   - `Assets/Scenes/JumpNRun_Level1.unity` – „Morgenwiese“ (Einstieg)
   - `Assets/Scenes/JumpNRun_Level2.unity` – „Geisterhain“ (mehrere Wege, alle Geister-Elemente)
   - `Assets/Scenes/JumpNRun_Level3.unity` – „Abendschaukel“ (Schaukel-Level)
   - `Assets/Scenes/JumpNRun_Level4.unity` – „Taunacht“ (Tau-Level bei Nacht)
   - `Assets/Scenes/JumpNRun_Hub.unity` – Lichtung mit Türen, Seltenheiten-Regal und Geistern
   - `Assets/Scenes/TestMeadow.unity` (plus Portal zur Lichtung), `Assets/Scenes/JumpAndRun.unity`
   Dabei prüft er die Level (Erreichbarkeit, Laternen-Reihenfolge, fehlende Prefabs) und warnt in der Konsole.
2. Eine Level-Szene öffnen → **Play** (oder über die Wiese/Lichtung).
3. **SteepingSpirits → JumpNRun → Run Smoke Test** baut alles, lässt einen Bot jedes Level mit echten
   Eingaben spielen und schreibt [SMOKE_REPORT.md](SMOKE_REPORT.md).
2. Eine Level-Szene öffnen → **Play**.

Szenen, Prefabs und Assets werden **nie von Hand** bearbeitet – Änderungen gehören in die
Level-Textdateien, den Builder-Code oder die Tuning-Assets.

## Steuerung

| Aktion | Tastatur | Gamepad |
|---|---|---|
| Laufen | A/D, Pfeile | linker Stick / Steuerkreuz |
| Springen (halten = höher) | Leertaste / C | A |
| Durch dünne Plattform fallen | S + Springen | runter + A |
| Schaukel: Schwung holen | **D**, wenn sie nach rechts schwingt, **A**, wenn nach links | Stick im Takt |
| Schaukel: abspringen / absteigen | Springen / S | A / runter |
| Pause + Komfort-Optionen, Tastenbelegung | Esc | B |
| Tür auf der Lichtung | W / E | hoch / A |
| Debug-Overlay | F1 (oder ^) | – |
| Eingaben aufnehmen / abspielen | F5 / F6 | – |

## Elemente (Level-Zeichen)

| Zeichen | Element | Verhalten |
|---|---|---|
| `#` | Boden | fest |
| `=` | dünne Plattform | von unten durchspringbar, mit S + Springen nach unten |
| `P` / `E` | Start / Ziel | Ziel zeigt die Abschlusskarte |
| `L` | Laterne | Checkpoint, glimmt warm auf; nur vorwärts |
| `t k b m q` | Zutat | Teeblatt, Wiesenkraut, Blüte, Morgentau, Quellwasser |
| `T K B M Q` | seltene Zutat | Goldspitze, Mondkraut, Geisterblüte, Sternentau, Quellkristall (kurze, weiche Zeitlupe) |
| `W` | Windgeist | Aufwind-Säule bis zur nächsten Decke (max. 9 Felder), fängt auch Stürze ab |
| `S` | Laternengeist | folgt nach Berührung; sein Licht macht Geisterplattformen fest |
| `G` | Geisterplattform | nur im Licht eines Laternengeists fest, sonst blasse Kontur |
| `F` | Blattplattform | sinkt langsam unter der Figur, steigt danach wieder |
| `D` | Tautropfen-Blatt | federt hoch (Springen halten = höher) |
| `O` | Schaukel | Aufhängepunkt; Sitz hängt 3 Felder tiefer |
| `N` | Geist (NPC) | sagt einen kurzen Satz, wenn man vorbeikommt |
| `^` | Dornenranke | kein Schaden – ein Geist trägt die Figur zurück |
| `l` | Pfadlaterne | Laternen-Herausforderung: alle entzünden (beliebige Reihenfolge, ohne Zeit) → Quellkristall als Dank |

| `0`–`9` | Tür (nur Hub) | Ziel über `@door<Ziffer> <Level-id | meadow | climb>` |

Zeilen mit `@schlüssel wert` sind Einstellungen (`@id`, `@name`, `@next`, `@mood morning|evening|night`,
`@npc0 …`, `@rope0 4.5`, `@angle0 60`, `@hub 1`, `@shelf x y`),
Zeilen mit `//` Kommentare. Die oberste Rasterzeile ist die höchste, ein Zeichen = 1 Einheit.

## Bewegung und Assists

Die gesamte Rechnung steckt in der engine-freien Assembly
`SteepingSpirits.Platforming.Core` (`Assets/_Project/Platformer/Core`, `noEngineReferences`).

**Sprungformel** (`JumpMath`): aus Sprunghöhe *h* und Zeit bis zum Scheitel *t*

```
g  = 2h / t²      v0 = 2h / t        (Standard: h = 3,2  t = 0,38 s → g ≈ 44,3  v0 ≈ 16,8)
```

| Assist | Wert | Wo |
|---|---|---|
| Coyote-Time | 0,10 s | `MovementParams.coyoteTime` |
| Sprung-Puffer | 0,12 s | `jumpBufferTime` |
| Sprung abbrechen | vy × 0,5 | `jumpCutFactor` |
| Scheitel-Schweben | Schwerkraft × 0,5 bei \|vy\| < 2,2 und gehaltener Taste | `apexGravityFactor`, `apexThreshold` |
| Fall-Schwerkraft | × 1,8 | `fallGravityFactor` |
| Max. Fallgeschwindigkeit | 2,5 × v0 | `maxFallFactor` |
| Eckenkorrektur | Kopf bis 0,15 seitlich, Füße bis 0,15 hoch | `cornerCorrection`, `ledgeCorrection` |
| Luftkontrolle | 65 % der Boden-Beschleunigung | `airControl` |
| Wandrutschen/-sprung, Dash | abschaltbar | `wallSlideEnabled`, `wallJumpEnabled`, `dashEnabled` (Dash aus) |

**Komfort-Optionen** (Esc): Spieltempo 60–100 %, Extra-Luftsprung, Absturzschutz, Auto-Schwung,
hoher Kontrast; außerdem **Tasten anpassen** (zwei Tasten je Aktion, Gamepad fest). Alles steht im
Spielstand und verändert die Tuning-Assets nicht.

## Tuning-Assets (`Assets/_Project/Platformer/Data`)

| Asset | Inhalt | Wichtigste Regler |
|---|---|---|
| `MovementTuning` | Laufen, Springen, Assists | `jumpHeight`, `timeToApex`, `runSpeed`, `airControl`, `fallGravityFactor` |
| `CameraTuning` | Folgekamera | `lookAhead`, `deadZoneX/Y`, `followRateX/Y`, `airBandUp/Down`, `orthographicSize` |
| `FeedbackTuning` | Squash/Stretch, Partikel, Lautstärken, Auffang-Blende, Zeitlupe | `landSquashMax`, `*Volume`, `catchFadeOut/In`, `rareSlowMoScale` (1 = aus) |
| `SpiritElementsTuning` | Wind, Laternengeist, Blätter, Tau, Schaukel | `wind.acceleration`, `lanternSpirit.lightRadius`, `leaf.sinkSpeed`, `dew.bounceHeight`, `swing.*` |
| `JumpNRunAudioSet` | echte Klänge je Platz (leer = Platzhalter) | `jump`, `land`, `collect`, `swingCreak` … |

Alle Werte wirken auch während Play (außer Kamera-Größe beim Start und Lautstärken der
bereits erzeugten Klänge).

## Schaukel

`SwingModel` (Core) ist ein Pendel (Periode ≈ 2π·√(L/g), Standard 2 s). Wer **in
Bewegungsrichtung drückt**, gibt Schwung (am stärksten unten); **gegen die Bewegung** bremst
es deutlich stärker – deshalb wird man auch langsamer, wenn man eine Taste einfach festhält.
Maximal 80° Ausschlag (pro Schaukel über `@angle<n>` und `@rope<n>` in der Level-Datei änderbar).
Ein Stick pumpt analog (leicht geneigt = sanft). Komfort-Option **Auto-Schwung**: Richtung halten genügt.
An den Umkehrpunkten knarzt das Holz leise, mit steigender Tonhöhe bei größerem Schwung. Beim Absprung übernimmt die Figur die Sitzgeschwindigkeit
(× `releaseBoost`) plus einen kleinen Aufwärtsschub und fliegt im Bogen weiter.

## Kamera und Feedback

- `CameraRig`: Totzone, Vorausschau in Laufrichtung, vertikal erst nach der Landung auf neuer
  Höhe (oder beim Verlassen eines Bandes im Flug), exponentielles, bildratenunabhängiges
  Glätten, Levelgrenzen. Läuft in `LateUpdate`, **kein Wackeln**.
- Squash/Stretch beim Springen/Landen, Staub, Blätter beim Laufen.
- Prozedurale, leise Klänge (`SoftSynth`: tiefpassgefiltertes Rauschen, weiche Zupftöne,
  Glockenakkorde) – einmal beim Laden erzeugt, nie im Audio-Thread.
- Partikel hinter der schmalen Schnittstelle `IJumpNRunParticles`.
- Keine Blitze; Zeitlupe nur kurz und über unskalierte Zeit (`JumpNRunTime`).

## Telemetrie

Pro Abschnitt (Start → Laterne → … → Ziel) eine JSON-Zeile in
`Application.persistentDataPath/jumpnrun_log.jsonl`:

```json
{"time":"…","run":"…","level":"Level1","section":1,"seconds":41.20,"falls":2,
 "ingredients":{"herb":1,"tea_leaf":3},"assists":"speed=1.0;air=0;fall=0","end":"lantern"}
```

## Sammelbeutel, Vorratsschrank, Spielstand

- `IngredientBag` kennt nur IDs und Mengen. Die IDs kommen aus dem gemeinsamen Katalog
  **`SteepingSpirits.Ingredients`** (`Assets/_Project/Ingredients`, engine-frei; Namen in `IngredientTexts`).
- Am Ziel übergibt `IngredientHandover` den Beutel: läuft das **Inventar** (über die Wiese gekommen),
  landen die Zutaten dort (Teeblatt = `item_teeblatt`, zählt für Oma Hildes Quest), sonst im
  **Vorratsschrank** `persistentDataPath/pantry.json` (versioniert, für das Brühsystem lesbar).
- **Spielstand** `persistentDataPath/jumpnrun_save.json` (versioniert): freigeschaltete und geschaffte
  Level, seltene Funde je Stelle (dort wächst danach die normale Variante), Laternen-Herausforderungen,
  Komfort-Optionen, Tastenbelegung.

## Werkzeuge

- **F5 / F6**: Eingaben aufnehmen / abspielen (`persistentDataPath/jumpnrun_recordings/*.jnrrec`, Text) –
  für reproduzierbare Fehlermeldungen.
- **Smoke-Test** (Menü): Bot spielt jedes Level entlang der geplanten Route (`RoutePlanner` + `RouteFollower`),
  meldet Exceptions, Fehler, Auffangen, langsame Frames → `Docs/SMOKE_REPORT.md`. Schreibt nie in Spielstand,
  Vorrat oder Inventar.
- **PlayMode-Tests** (Test Runner): Spawn, Laterne, Auffangen, Schaukel-Absprung, Allokationen pro Frame.

## Tests ohne Unity

```
dotnet test Tests/PlatformingCore.DotNet
```

Prüft Sprungformel und Assists, Kamera, Squash/Stretch, Klangpuffer, Checkpoints und
Auffangen, Komfort-Optionen, Tastenbelegung, Beutel, Spielstand, Geister-Elemente, Schaukel
(auch Kombinationen), Telemetrie, Aufnahmen, Level-Lint und – über `LevelReachability` – dass jedes
Level fertig spielbar ist (mehrere Wege jeweils allein). Zusätzlich spielt der Bot jedes Level in
der Kachel-Simulation `TileWorldSim` durch. `dotnet test Tests/Ingredients.DotNet` prüft Katalog und
Vorratsschrank. In Unity laufen dieselben Tests im Test Runner (EditMode).

## Eigene Level

1. Textdatei in `Assets/_Project/Platformer/Levels/` anlegen (z. B. `Level5_Mondsee.txt`)
   mit `@id Level5`, `@name …`, optional `@next …`; auf der Lichtung eine Tür `@doorN Level5` ergänzen.
2. `dotnet test Tests/PlatformingCore.DotNet` – meldet unerreichbare Laternen, Zutaten oder Ziele,
   Lint-Warnungen und ob der Bot das Level in der Simulation schafft.
3. In Unity **Build Levels** → `JumpNRun_Level5.unity`; danach **Run Smoke Test**.

Gestaltungsmuster der Level: **Lehren → Prüfen → Wendung**, mindestens zwei Wege in der Prüfung,
eine seltene Zutat abseits, drei Pfadlaternen.

## Code-Überblick

```
Assets/_Project/Platformer/
├── Core/          engine-frei: PlayerMotor, JumpMath, CameraRig, Checkpoints, AssistOptions,
│                  KeyBindings, Ingredients, SpiritElements, SwingModel, LevelLayout,
│                  LevelReachability, LevelLint, RoutePlanner, RouteFollower, TileWorldSim,
│                  InputRecording, JumpNRunSave, HubLines, SectionTelemetry, SoftSynth, SquashStretch
├── Hooks/         kleine Assembly für Tests/Werkzeuge: Eingabe-Schnittstelle, Level-Probe
├── Runtime/
│   ├── Data/          Tuning-ScriptableObjects
│   ├── Player/        JumpNRunPlayer, JumpNRunKeys, JumpNRunCamera, JumpNRunFeedback, ClimbGrab
│   ├── Elements/      Lantern, Ingredient, Goal, Bramble, WindSpirit, LanternSpirit,
│   │                  GhostPlatform, LeafPlatform, DewLeaf, PlaygroundSwing, SpiritNpc,
│   │                  PathLantern, LevelDoor, RareShelf
│   ├── Integration/   IngredientHandover, JumpNRunSaveStore, ClimbCourseMode
│   ├── Testing/       JumpNRunSmokeBot, JumpNRunInputRecorder
│   ├── Flow/          JumpNRunLevel, JumpNRunSession, JumpNRunOptions, JumpNRunTime,
│   │                  JumpNRunTelemetry, JumpNRunTexts (alle Spielertexte, Deutsch)
│   └── Presentation/  Partikel, Klänge, HUD, Abschlusskarte, Debug-Overlay, Platzhalter-Grafik, Ui/ (uGUI)
├── Editor/        JumpNRunBuilder (Menü), LevelBuilder, ElementFactory, Prefabs, SceneChecks, SmokeTest
├── Levels/        Level-Textdateien
└── Tests/         EditMode (auch per dotnet), PlayMode
```

## Grenzen

- Alles ist Platzhalter-Grafik aus Code; echte Sprites einfach in `PlaceholderVisual` ersetzen.
- `LevelReachability` ist eine grobe Prüfung (ignoriert Kopfstöße an Überhängen, nimmt
  Geisterplattformen als beleuchtet an); die Kachel-Simulation lässt Blätter nicht sinken.
- Die Aufnahme-Wiedergabe ist nicht bitgenau (Unity-Physik), zeigt aber denselben Ablauf.
- Tastenbelegung gilt nur im Jump'n'Run (GameInput unverändert); Gamepad nicht belegbar.
