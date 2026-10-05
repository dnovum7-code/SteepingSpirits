# Jump'n'Run – Sammelausflüge in die Geisterwelt

Gemütlicher Plattform-Teil von „Steeping Spirits“: Die Figur sammelt Zutaten für das
Teehaus (Teeblätter, Kräuter, Blüten, Morgentau, Quellwasser – seltene Varianten an
optionalen Stellen). Es gibt **keinen Tod, keine Leben und keinen Zeitdruck**: Wer fällt,
wird von einem Geist aufgefangen und an der letzten Laterne (oder auf Wunsch an der letzten
sicheren Stelle) wieder abgesetzt.

> Der alte Kletterpfad (`Assets/Scenes/JumpAndRun.unity`, Celeste-Steuerung mit Dash,
> Greifen und Lianen) bleibt unverändert erhalten – siehe [JUMP_AND_RUN.md](JUMP_AND_RUN.md).

## Starten

1. Unity öffnen, Menü **SteepingSpirits → JumpNRun → Build Levels**.
   Der Builder legt Tuning-Assets an (falls noch nicht vorhanden), erzeugt alle Prefabs neu und
   baut die Szenen:
   - `Assets/Scenes/JumpNRun_Level1.unity` – „Morgenwiese“ (Einstieg)
   - `Assets/Scenes/JumpNRun_Level2.unity` – „Geisterhain“ (mehrere Wege, alle Geister-Elemente)
   - `Assets/Scenes/JumpNRun_Hub.unity` – Lichtung mit Portalen zu allen Leveln
   - `Assets/Scenes/TestMeadow.unity`, `Assets/Scenes/JumpAndRun.unity` (wie bisher)
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
| Pause + Komfort-Optionen | Esc | B |
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

Zeilen mit `@schlüssel wert` sind Einstellungen (`@id`, `@name`, `@next`, `@mood`, `@npc0 …`, `@hub 1`),
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

**Komfort-Optionen** (Esc): Spieltempo 60–100 %, Extra-Luftsprung, Absturzschutz. Sie werden in
`PlayerPrefs` gemerkt und verändern die Tuning-Assets nicht.

## Tuning-Assets (`Assets/_Project/Platformer/Data`)

| Asset | Inhalt | Wichtigste Regler |
|---|---|---|
| `MovementTuning` | Laufen, Springen, Assists | `jumpHeight`, `timeToApex`, `runSpeed`, `airControl`, `fallGravityFactor` |
| `CameraTuning` | Folgekamera | `lookAhead`, `deadZoneX/Y`, `followRateX/Y`, `airBandUp/Down`, `orthographicSize` |
| `FeedbackTuning` | Squash/Stretch, Partikel, Lautstärken, Auffang-Blende, Zeitlupe | `landSquashMax`, `*Volume`, `catchFadeOut/In`, `rareSlowMoScale` (1 = aus) |
| `SpiritElementsTuning` | Wind, Laternengeist, Blätter, Tau, Schaukel | `wind.acceleration`, `lanternSpirit.lightRadius`, `leaf.sinkSpeed`, `dew.bounceHeight`, `swing.*` |

Alle Werte wirken auch während Play (außer Kamera-Größe beim Start und Lautstärken der
bereits erzeugten Klänge).

## Schaukel

`SwingModel` (Core) ist ein Pendel (Periode ≈ 2π·√(L/g), Standard 2 s). Wer **in
Bewegungsrichtung drückt**, gibt Schwung (am stärksten unten); **gegen die Bewegung** bremst
es deutlich stärker – deshalb wird man auch langsamer, wenn man eine Taste einfach festhält.
Maximal 80° Ausschlag. Beim Absprung übernimmt die Figur die Sitzgeschwindigkeit
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

## Sammelbeutel

`IngredientBag` kennt nur IDs und Mengen (`tea_leaf:3,herb:1`) und hängt nicht vom
Brüh-System ab. Am Levelende steht der Inhalt auf der Abschlusskarte und in der Konsole
(`[JumpNRun] Bag: …`), damit das Teehaus ihn später übernehmen kann.

## Tests ohne Unity

```
dotnet test Tests/PlatformingCore.DotNet
```

Prüft Sprungformel und Assists, Kamera, Squash/Stretch, Klangpuffer, Checkpoints und
Auffangen, Komfort-Optionen, Beutel, Geister-Elemente, Schaukel, Telemetrie und – über
`LevelReachability` – dass jedes Level fertig spielbar ist (Level 2: jeder der beiden Wege
allein). In Unity laufen dieselben Tests im Test Runner (EditMode).

## Eigene Level

1. Textdatei in `Assets/_Project/Platformer/Levels/` anlegen (z. B. `Level3_Mondsee.txt`)
   mit `@id Level3`, `@name …`, optional `@next …`.
2. `dotnet test Tests/PlatformingCore.DotNet` – meldet unerreichbare Laternen, Zutaten oder Ziele.
3. In Unity **Build Levels** → `JumpNRun_Level3.unity`.

## Code-Überblick

```
Assets/_Project/Platformer/
├── Core/          engine-frei: PlayerMotor, JumpMath, CameraRig, Checkpoints, AssistOptions,
│                  Ingredients, SpiritElements, SwingModel, LevelLayout, LevelReachability,
│                  SectionTelemetry, SoftSynth, SquashStretch
├── Runtime/
│   ├── Data/          Tuning-ScriptableObjects
│   ├── Player/        JumpNRunPlayer, JumpNRunCamera, JumpNRunFeedback
│   ├── Elements/      Lantern, Ingredient, Goal, Bramble, WindSpirit, LanternSpirit,
│   │                  GhostPlatform, LeafPlatform, DewLeaf, PlaygroundSwing, SpiritNpc
│   ├── Flow/          JumpNRunLevel, JumpNRunSession, JumpNRunOptions, JumpNRunTime,
│   │                  JumpNRunTelemetry, JumpNRunTexts (alle Spielertexte, Deutsch)
│   └── Presentation/  Partikel, Klänge, HUD, Abschlusskarte, Debug-Overlay, Platzhalter-Grafik
├── Editor/        JumpNRunBuilder (Menü), LevelBuilder, ElementFactory, Prefabs
├── Levels/        Level-Textdateien
└── Tests/EditMode NUnit-Tests (auch per dotnet)
```

## Grenzen

- Alles ist Platzhalter-Grafik aus Code; echte Sprites einfach in `PlaceholderVisual` ersetzen.
- `LevelReachability` ist eine grobe Prüfung (ignoriert Kopfstöße an Überhängen, nimmt
  Geisterplattformen als beleuchtet an).
- Der Sammelbeutel wird noch nicht ins Inventar/Teehaus übertragen.
