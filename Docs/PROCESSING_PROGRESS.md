# Teewerkstatt – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann `Docs/PLAYTEST_NOTES.md` (Werkstatt-Notizen haben Vorrang),
> dann beim ersten Backlog-Punkt mit Status „offen“ weitermachen.
> Branch `feature/processing` (Worktree `../SteepingSpirits-workshop`). Nichts nach `main`, kein Force-Push.

## Nächster konkreter Schritt

→ erster Backlog-Punkt mit Status „offen“.

## Basis und Merge-Hinweis

- `main` (5629997) enthält weder den Zutaten-Katalog noch den Teegarten. Basis ist daher
  **`feature/plantation` (e7dee0e)**, das selbst auf `feature/jumpnrun` aufbaut.
  Merge-Reihenfolge: `feature/jumpnrun` → `feature/plantation` → `feature/processing`.
- `CLAUDE.md` und `Docs/PLAYTEST_NOTES.md` gibt es im Repo (noch) nicht – keine Werkstatt-Notizen abzuarbeiten.

## Erkundung

- **Garten-Ernte:** Teestrauch liefert die Zutat `tea_leaf`, Qualität als ID-Variante: Stufe 1 „gut“ = `tea_leaf`
  (im Inventar `item_teeblatt`), Stufe 2/3 = `tea_leaf+q2` / `tea_leaf+q3` (Inventar `ingredient_tea_leaf+q2/+q3`,
  Hilfsklasse `IngredientQuality`). Der „erste Austrieb“ ist im Garten nur ein Qualitäts-Bonus (+1 Stufe) auf
  die erste Ernte – **er ist im Ergebnis nicht erkennbar** (kein eigenes Kennzeichen, keine eigene ID).
  Seltene Blattvariante aus dem Jump'n'Run: `golden_tip` („Goldspitze“, Familie Blatt).
- **Brühsystem** (`feature/brewing-phase2`, nur gelesen): vier Tees `TeaDefinition` (SO mit `displayName`,
  `description`, `TeaParams parameters`, Aufguss-Farbverlauf, Blattfarbe). `TeaParams.id` = `black`, `white`,
  `green`, `oolong`; Werte: ideales Temperaturfenster (z. B. Grün 70–80 °C, Weiß 75–85, Oolong 85–95, Schwarz
  90–100), Aroma-/Bitter-Raten, Bitter-Toleranz („herb“), Blattkapazität, Aufgüsse (Oolong 6, wanderndes Fenster).
  Qualität dort: 4 Stufen Fad/Ordentlich/Harmonisch/Vollendet. Das Brühen kennt **keine Zutaten-IDs** und
  nimmt keine Tees aus dem Inventar.
- **Inventar:** `PlayerInventory` (Katalog ID → `ItemData`, `RegisterItem` zur Laufzeit, `AddById`, `Remove`,
  `Count`); lädt laufzeit-registrierte Items nach einem Neustart nicht (siehe Garten, Saatgut-Sicherung).
- **Vorratsschrank:** `Pantry` (`pantry.json`, versioniert, Schlüssel = Zutaten-ID; unbekannte IDs werden
  unverändert gespeichert, so bleiben Qualitäts-/Varianten-IDs getrennt).
- **Spielstände:** kein gemeinsames System, jedes Modul hat ein versioniertes JSON (`garden_save.json`, …).
- **Zeit:** `IDayClock` (Garten-Core) kennt nur ganze Tage (`Today`, `EndDay`); `GameClock` (World) hat Tage
  und Uhrzeit (7 echte Sekunden pro 10 Spielminuten, fließend über `TimeOfDayHours`, Tagesende 2:00 → nächster
  Morgen 6:00), steht still, solange `GamePause` blockiert. `GameClock` wird nicht gespeichert.

## Modell (Core, `BatchRules`)

- **Charge** (`LeafBatch`): Menge, Herkunft (Zutat, Gartenstufe, erster Austrieb), Feuchte (frisch = 1),
  Rollgrad + Gleichmäßigkeit, Oxidation, fixiert + Wok-Sorgfalt + Rest-Oxidation, Röstnote, Trockengrad,
  Ort (Regal, Welkgestell, Oxidationskorb, Trockenkorb), Spielzeit, aktive Sekunden, Schritte (Weg).
- **Übergänge** sind reine Funktionen, die eine geänderte Kopie liefern: `Place` (auflegen/abnehmen), `Roll`,
  `Fix`, `Elapse`/`AdvanceTo` (Zeit). `Check` liefert pro Aktion und Zustand „ja“ oder einen ruhigen Grund.
- **Welken:** exponentiell zur Untergrenze 0,45 (τ 275 Spielminuten → ~4 h bis 0,68), abseits des Gestells
  4× langsamer. Ab Feuchte ≤ 0,85 gilt die Charge als gewelkt (rollen, fixieren, trocknen möglich).
- **Oxidation:** 1,2 %/Spielminute bei voll gerollten Blättern (Anteil 35 % unabhängig vom Rollgrad), ungerollt
  nur 0,8 % davon; aus dem Korb genommen (noch nicht fixiert) 15 %; nach dem Wok nur der Rest einer zu kalten
  Fixierung (40 % je Anteil „zu kalt“); Trocknen stoppt sie bis Trockengrad 0,5 ganz.
- **Rollen** stößt die Oxidation an (+4 % bei vollem Rollen) und presst etwas Wasser heraus. Höchstens 2× rollen.
- **Wok:** Zeitanteile unter/im/über dem Hitzefenster → Wok-Sorgfalt, Rest-Oxidation, Röstnote (+0,6 je Anteil
  „zu heiß“). Nie verbrannt.
- **Trocknen:** 120 Spielminuten bis trocken (fertig ab 0,95), danach leichte Röstung bis höchstens 0,45.
- **Zeit rückwärts** (Uhr neu gestartet) → es vergeht keine Zeit.

## Sorten (`TeaClassifier`)

| Oxidation | Sorte | Optimum (beste Sorgfalt) |
|---|---|---|
| < 10 % | **Weißtee** (nicht gerollt, nicht fixiert) oder **Grüntee** (gerollt ≥ 0,15 oder im Wok fixiert) | 2 % |
| 10–30 % | **heller Oolong** | 20 % |
| 30–70 % | **dunkler Oolong** | 50 % |
| ≥ 80 % | **Schwarztee** | 92 % |

Die Lücke 70–80 % geht an die nähere Sorte (bis 75 % dunkler Oolong, ab 75 % Schwarztee; Gleichstand → die
dunklere Sorte). Alle Grenzen sind tunbar (`VarietyParams`). Die Sortenfolge ist mit wachsender Oxidation
monoton: zu lange oxidiert heißt immer „dunkler“, nie „kaputt“. **Erreichbarkeit:** 2000 Zufallsabfolgen
erlaubter Schritte (mit Zufallswartezeiten) enden alle in einer gültigen Sorte und treffen alle fünf Sorten.

## Qualität (`TeaQuality`)

- **Sorgfalt** = Mittel der durchlaufenen Schritte: Welken (Feuchte beim ersten Bearbeiten im Fenster
  0,60–0,72 = 1, ±0,15 daneben = 0), Rollen (Gleichmäßigkeit), Oxidation (Abstand zum Optimum der
  entstandenen Sorte, quadratisch weich; Oolongs mit halber Bandbreite, Schwarztee ab 92 % immer 1,
  Weiß/Grün bis 2 % immer 1), Wok (Zeitanteil im Hitzefenster).
- **Wert** = 0,3 × Herkunft (gut 0 / fein 0,5 / erlesen 1; erster Austrieb 1) + 0,7 × Sorgfalt.
  **fein** ab 0,5, **erlesen** ab 0,8.
- **Obergrenze:** gewöhnliche Blätter (Gartenstufe gut/fein) höchstens **fein**; Gartenstufe erlesen und
  **erster Austrieb** heben sie auf **erlesen**. So zählt die Pflege im Garten weiter, und der erste Austrieb
  ist etwas Besonderes, selbst wenn er nicht erlesen ist.

## Charakternoten (`TeaNotes`, höchstens 3, stärkste zuerst)

| Note | Entsteht durch |
|---|---|
| **frisch** | Oxidation ≤ 6 %, kaum Röstung, fixiert oder kurz gewelkt (Grüntee) |
| **blumig** | gleichmäßig gerollt und 8–40 % Oxidation (heller Oolong) oder Weißtee lang gewelkt |
| **honigsüß** | 40–90 % Oxidation mit leichter Röstung (0,08–0,25) |
| **malzig** | ≥ 70 % Oxidation und kräftig gerollt (Schwarztee) |
| **röstig** | Röstung ≥ 0,25 (Wok zu heiß oder lange getrocknet) |
| **zart** | erster Austrieb / Goldspitze, oder unberührter Weißtee |

## Datenvertrag `ProcessedTea`

- Felder: Sorte (`TeaVariety`), Oxidation, Qualitätsstufe, Noten, Menge, Herkunft (Zutat, Gartenstufe,
  erster Austrieb), Weg (`W-R-O-F-R-D`), Oxidation beim Stoppen, aktive Sekunden.
- **Zutaten-ID** = Katalog-ID + Eigenschaften: `tea_oolong:dark/honeyed.roasty+q3`
  (Oolong-Stil `light`/`dark`, Noten sortiert, Qualität über `IngredientQuality`, Stufe 1 ohne Endung).
  Gleiche Sorte + gleiche Noten + gleiche Qualität → gleiche ID → stapelt; andere Noten bleiben getrennt.
  `ProcessedTea.TryParseId` liest eine ID zurück, `CatalogIdOf` liefert die Katalog-ID.
- Genaue Oxidation und Herkunft sind Zusatzdaten (nicht in der ID), gespeichert im Tee-Register der Werkstatt.
- **Katalog (additiv):** `tea_white`, `tea_green`, `tea_oolong`, `tea_black` (neue Familie `Tea`, häufig) und
  `tea_leaf_first` („Erster Austrieb“, Familie Blatt, häufig – nicht selten, damit das Seltenheitsregal im
  Jump'n'Run unverändert bleibt). Katalog-Test 15 → 20.

## Minispiele (Core, `Minigames.cs`)

- **Rolltisch** (`RollSession`): Jedes Kneten (Taste oder eine Stick-Runde) bringt 1/16 Rollgrad; Takt 0,75 s.
  Abweichung ≤ 0,08 s = gleichmäßig (1), ab 0,35 s = Untergrenze 0,35 – Fehler machen nur etwas ungleichmäßig.
  Volle Rolle nach 16× Kneten ≈ 12 s, höchstens 18 s; früher aufhören = leichtes Rollen (Oolong).
  Komfort „Gleichmäßig rollen“: Halten knetet im Takt, Gleichmäßigkeit 0,85.
- **Wok** (`WokSession`): 12 s. Halten = Glut anfachen, loslassen = abkühlen (träge). Das Hitzefenster (Breite
  0,24) wandert langsam (zwei überlagerte Sinus, je Charge anders, deterministisch). Ergebnis: Zeitanteile
  zu kalt / im Fenster / zu heiß.

## Werkstatt-Simulator (`WorkshopSimulator`, Test `WorkshopSimulatorTests`)

Je 100 Chargen, gewünschte Sorte zufällig, Blätter 50 % erlesen / 30 % fein / 20 % gut, 5 % erster Austrieb.
Die Bots benutzen dieselben Regeln und Minispiel-Modelle wie das Spiel.

| Bot | Wunsch getroffen | gut / fein / erlesen | Ø Stufe | Sorten W/G/hO/dO/S | Noten (häufigste) | unbrauchbar |
|---|---|---|---|---|---|---|
| sorgfältig | 100 % | 0 / 51 / 49 | 2,49 | 13/20/19/21/27 | malzig 27, zart 21, frisch 20, blumig 19 | 0 |
| nachlässig | 55 % | 18 / 62 / 20 | 2,02 | 21/6/20/28/25 | röstig 86, blumig 39, malzig 27, honigsüß 3 | 0 |
| zufällig | 19 % | 15 / 53 / 32 | 2,17 | 31/15/13/14/27 | röstig 60, blumig 21, malzig 18, honigsüß 12 | 0 |

- Sorgfältig erlesen = genau die Chargen mit erlesenen Blättern oder erstem Austrieb (Obergrenze greift wie gewollt).
- Nachlässig: immer ein Tee, meist „fein“; verfehlte Wünsche werden **dunkler** (zu spät gestoppt).
  Häufigste Note „röstig“ (zu heißer Wok, vergessener Trockenkorb) – die sanfte Überraschung statt Strafe.
- Zufällig landet oft beim einfachen Weißtee, daher eine etwas höhere Ø-Stufe als „nachlässig“.

**Aktive Zeit** (sorgfältig, Ø / max; Kneten + Wok + Korb beobachten + 1 s je Handgriff): Weißtee 5 / 5 s,
Grüntee 28 / 28 s, heller Oolong 49 / 50 s, dunkler Oolong 75 / 76 s, Schwarztee 69 / 69 s.
Ziel erfüllt: Oolong ≤ 90 s, Weißtee wenige Sekunden. (Nachlässig dunkler Oolong max 92 s, weil zu spät gestoppt.)

**Startwert-Anpassungen durch den Simulator:**
- Röstung beim Übertrocknen 0,0025 → **0,0015 pro Minute** (vorher wurden 91 % der nachlässigen Chargen röstig,
  die Noten wurden eintönig; jetzt erst nach ~2,8 Spielstunden zu lang im Korb).
- „Honigsüß“ bis **100 %** Oxidation statt 90 % (sonst kam die Note bei Schwarztee nie vor).

## Annahmen

1. **Basis** `feature/plantation`, weil `main` weder Katalog noch Garten enthält.
2. **Ungerollte Blätter oxidieren kaum** (0,008 × Rate): Startwert von 0,01 gesenkt, damit Weißtee auch nach
   14 Stunden Welken noch Weißtee ist (Test „white tea leaves stay pale over a long day“).
3. **Qualitätsstufen** wie im Garten (1 gut, 2 fein, 3 erlesen), nicht die vier Brüh-Stufen – die
   Verarbeitung ist eine Zutat, das Brühen bewertet die Tasse.
4. **Obergrenze** (siehe Qualität) ist meine Lesart von „der erste Austrieb hebt die Obergrenze an“.
5. **Zeit:** `IGameTime` liefert Spielminuten. `IDayClock` aus dem Garten kennt nur Tage; die Werkstatt dockt
   in Unity an: Tage aus `IDayClock` (`GameClockDays` des Gartens) + Minute des Tages aus `GameClock`
   (`DayAndMinuteTime`). Der Core hängt dadurch nicht vom Garten-Core ab; ein späteres Teehaus-Tagessystem
   ersetzt nur diesen Adapter. Vorspulen: `SkippableTime.FastForward` (Debug). Läuft die Uhr rückwärts
   (Neustart, `GameClock` wird nicht gespeichert), vergeht für die Chargen keine Zeit.
6. **Werkstatt-UI pausiert das Spiel nicht** (`GamePause` würde die Uhr anhalten und damit Welken und Oxidation).
7. **Erster Austrieb:** Der Garten liefert ihn heute nicht erkennbar (nur +1 Qualität, gleiche ID). Die
   Werkstatt kennt dafür die neue Zutat `tea_leaf_first` und behandelt auch `golden_tip` (Goldspitze) als
   ersten Austrieb. Merge-Vorschlag: Der Garten gibt bei der ersten Teestrauch-Ernte `tea_leaf_first` aus
   (eine Zeile in `GardenController.Harvest`). Das Startpaket der Werkstatt enthält einige davon.

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Blattcharge und reine Übergänge | erledigt (46 Tests) |
| A2 | Sortenbestimmung | erledigt (70 Tests gesamt) |
| A3 | Qualität | erledigt |
| A4 | Charakternoten | erledigt (86 Tests gesamt) |
| A5 | Zeit (`IGameTime`, Vorspulen) | erledigt (92 Tests gesamt) |
| B6 | ProcessedTea-Datenvertrag + Katalog | erledigt (99 Tests gesamt) |
| B7 | Übergabe Inventar/Vorratsschrank | offen |
| B8 | `Docs/PROCESSING_TO_BREWING.md` | offen |
| C9 | Welkgestell | offen |
| C10 | Rolltisch | Modell erledigt (`RollSession`), Station offen |
| C11 | Oxidationskorb | offen |
| C12 | Wok | Modell erledigt (`WokSession`), Station offen |
| C13 | Trockenkorb | offen |
| C14 | Vorschau an jeder Station | offen |
| D15 | Werkstatt-Szene per Builder | offen |
| D16 | Blatt-Darstellung | offen |
| D17 | Klang und Partikel, `ProcessingAudioSet` | offen |
| D18 | Oberfläche (UI-Baukasten, Gamepad) | offen |
| D19 | Eingang | offen |
| E20 | Speichern `workshop_save.json` | Core erledigt (Roundtrip getestet), Unity offen |
| E21 | Debug-Overlay F1 | offen |
| E22 | Telemetrie `processing_log.jsonl` | Zeile erledigt (getestet), Unity offen |
| F23 | Kräuter trocknen | offen |
| F24 | Mischtisch | offen |
| F25 | Teeziegel (Pu-Erh-Vorbereitung) | offen |
| F26 | Werkstattgeist | offen |
| F27 | Eigene Verbesserungen | offen |

## Editor-Checklisten pro Commit

- **A1** (Core-Grundlage): Unity öffnen → Konsole ohne Fehler; Test Runner → EditMode →
  `SteepingSpirits.Processing.Tests.EditMode` grün. Ohne Unity geprüft: `dotnet test Tests/ProcessingCore.DotNet`.
- **A2** (Sortenbestimmung): wie A1, zusätzlich `TeaClassifierTests` und `ReachabilityTests` grün.
- **A3** (Qualität): wie A1, `TeaQualityTests` grün.
- **A4** (Charakternoten): wie A1, `TeaNotesTests` grün.
- **A5** (Zeit): wie A1, `WorkshopTimeTests` grün.
- **B6** (Datenvertrag + Katalog): wie A1, `ProcessedTeaTests` grün; `SteepingSpirits.Ingredients.Tests.EditMode`
  grün (zählt jetzt 20 Zutaten). Im Jump'n'Run zeigt das Seltenheitsregal weiter nur die 5 seltenen Zutaten.
- **Minispiel-Modelle + Simulator:** wie A1, `MinigameTests`, `WorkshopSimulatorTests` grün (Zahlen im
  Testausgabe-Log). Noch nichts Sichtbares.
- **Spielstand/Telemetrie (Core):** wie A1, `WorkshopSaveTests` grün.
