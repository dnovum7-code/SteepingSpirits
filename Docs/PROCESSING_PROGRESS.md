# Teewerkstatt – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann `Docs/PLAYTEST_NOTES.md` (Werkstatt-Notizen haben Vorrang),
> dann beim ersten Backlog-Punkt mit Status „offen“ weitermachen.
> Branch `feature/processing` (Worktree `../SteepingSpirits-workshop`). Nichts nach `main`, kein Force-Push.

## Nächster konkreter Schritt

Alle Backlog-Punkte sind umgesetzt; alles, was Unity braucht, ist **ungeprüft**. Nächster Schritt: den Testplan
im Abschlussbericht in Unity durchspielen und Befunde in `Docs/PLAYTEST_NOTES.md` (Abschnitt „Werkstatt“)
eintragen. Die nächste Sitzung arbeitet diese Notizen zuerst ab.

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
- **F23 (additiv):** getrocknete Kräuter `mint_dried`, `chamomile_dried`, `jasmine_dried`, `lavender_dried`,
  `lemon_balm_dried` (Familien Kraut/Blüte, häufig) → Katalog-Test 20 → 25. Kräuter-Chargen (`isHerb`) dürfen nur
  in den Trockenkorb (kein Welken nötig), behalten die Gartenqualität (`jasmine_dried+q3`).
- **F24 (additiv):** `tea_blend` (Familie Tee) → Katalog-Test 25 → 26. Mischung = 1 Teil Werkstatt-Tee + 1 Teil
  getrocknetes Kraut → **2 Teile** (beide Zutaten gehen hinein; ich wollte das Mischen spürbar belohnen).
  ID `tea_blend:green~jasmine/fresh.floral+q2`: Grundtee, Kraut, Noten, Qualität = die niedrigere der beiden.
  Das Kraut bringt seine Note mit (Blüten blumig, Minze/Melisse frisch) und bleibt beim Kürzen auf 3 Noten
  immer erhalten. Name z. B. „Jasmin-Grüntee (frisch, blumig; fein)“.

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

## Werkstatt im Spiel

- **Raum** aus `Layouts/Werkstatt.txt` (15×9): Vorrat (V), Regal (S), zwei Welkgestelle (R), Rolltisch (T),
  Oxidationskorb (O), Wok (K), Trockenkorb über der Glut (D), Mischtisch (M), Tür zum Garten (G). Top-down wie
  Wiese und Garten; die Figur läuft mit `PlayerController2D`.
- **Benutzen** (Leertaste/J/E/F, Pad A/X) vor einer Station öffnet ihr Fenster (UI-Baukasten, Maus, Tastatur,
  Gamepad-Navigation; Esc/B schließt). Das Fenster pausiert das Spiel **nicht** – nur die Figur steht.
  Jedes Fenster beschreibt die Chargen dort über **Farbe, Duft, Gefühl**, sagt „Würde jetzt zu: …“ und welche
  Noten sich abzeichnen. Zahlen nur mit „Zahlen an“ (Regal).
- **Vorrat:** neue Charge (4 Blätter) aus Inventar/Vorratsschrank, nach Quelle und Gartenstufe getrennt; einmal
  das Startpaket (8× erlesen, 6× fein, 6× gut, 4× erster Austrieb).
- **Gestell / Korb / Trockenkorb:** auflegen, abnehmen; im Korb heißt Abnehmen „Jetzt stoppen“ (sanfter Klang,
  Meldung mit der Sorte). Gründe, warum eine Charge nicht passt, stehen ruhig darunter.
- **Rolltisch:** Charge wählen → Kneten im Takt (pulsierender Ring zeigt den Takt, das Blattbündel wird runder),
  Esc/B = fertig rollen. „Gleichmäßig rollen“: Halten statt Takt.
- **Wok:** 12 s Hitze im hellen Band halten (Halten = anfachen), Zischen folgt der Hitze, Dampf steigt.
- **Regal:** alle Chargen im Überblick; trockene „Als Tee einpacken“ → Inventar (sonst Vorratsschrank), Register,
  Telemetrie.
- **Speichern:** nach jeder Änderung (gebündelt, 1 s), beim Verlassen und Beenden; laufende passive Schritte
  laufen auf Spielzeit weiter.
- **Debug:** F1 Overlay (alle Werte der gewählten Charge, Sorgfalt je Schritt, Stufe, Noten, ID), F2 +30 min,
  F3 +4 h (nur Editor/Development-Build).

## F25 – Teeziegel (Pu-Erh-Vorbereitung, nur Daten)

Weg: welken → im Wok **sanft** fixieren (Rest-Oxidation ≥ 0,05, also absichtlich etwas zu kühl) → fest rollen
(≥ 0,6) → trocknen = **Maocha** (Oxidation ≤ 30 %) → pressen zu `TeaBrick` (Pressdatum, `maturationDays = 0`,
ID-Vorschlag `tea_brick:raw/<noten>+qN`, noch **kein** Katalog-Eintrag). `PuErh.CheckMaocha` nennt den Grund,
warum eine Charge (noch) kein Maocha ist. Keine Station und keine Reifung – Vorbereitung für später.

## F26 – Werkstattgeist

- **Lernt durch Zuschauen** (gespeichert in `workshop_save.json`): 3 fertige Tees mit Welk-Sorgfalt ≥ 0,9 →
  er nimmt Chargen im idealen Moment vom Welkgestell; 3 fertige Chargen → er nimmt trockene Chargen aus dem
  Trockenkorb (bevor sie röstig werden); zusätzlich 2 Weißtees ≥ „fein“ → am Welkgestell erscheint
  „Dem Werkstattgeist überlassen (Weißtee)“: er legt die Charge dann selbst vom Gestell in den Trockenkorb.
- **Er entscheidet nie eine Sorte:** Rollen, Wok und das Stoppen der Oxidation bleiben beim Spieler (getestet).
- Sichtbar als kleiner Geist über dem Regal, der zur Station und zurück schwebt; ruhige Meldung bei jedem Handgriff.

## F27 – eigene Verbesserungen (mit Begründung)

1. **„Die Farbe ist kurz davor zu kippen“** im Korb-Fenster (≤ 5 Spielminuten bis zur nächsten Sorte, reine
   Funktion `OxidationForecast`, getestet) und ein leiser Klang + Meldung, wenn die Sorte tatsächlich wechselt.
   Begründung: „lesen statt ablesen“ – man lernt die Farben, ohne eine Prozentzahl zu brauchen.
2. **Passive Schritte rufen einmal zurück:** „Charge n ist auf dem Gestell schön geschmeidig geworden“ /
   „… ist im Trockenkorb trocken“ (leises Rascheln bzw. Klang). Begründung: „starten und später zurückkommen“
   wie im Garten, ohne ständig nachsehen zu müssen.
3. **„Würde jetzt zu: …“** an jeder Station und in jeder Zeile (C14) – die Sorte wird nicht gewählt, aber man sieht
   immer, wohin der Weg führt.
4. **Ruhige Gründe statt gesperrter Knöpfe:** Chargen, die an eine Station nicht passen, stehen mit einem Satz
   darunter („Die Blätter sind noch zu frisch – erst welken lassen.“).
5. **Startpaket mit erstem Austrieb**, damit die angehobene Qualitätsgrenze sofort erlebbar ist.
6. **Tuning-Hilfe im Editor:** Build Workshop druckt die Simulator-Zahlen; Menü *Processing → Simulate 100 Batches*.

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
8. **Szenenwechsel Garten ↔ Werkstatt ohne `ScenePortal`:** `ScenePortal.Load` merkt sich die vorige Szene,
   und das Gartentor geht dorthin zurück. Über die Werkstatt würde das Gartentor sonst in die Werkstatt statt
   zur Wiese führen. Eigener Lader `WorkshopSceneLoader` (gleicher Mechanismus, ohne Merken).
9. **Inventar-IDs** wie Garten/Jump'n'Run: `item_teeblatt` (Teeblatt gut), sonst `ingredient_<id>`; Tees
   `ingredient_<ProcessedTea-ID>`. Das Inventar lädt laufzeit-registrierte Gegenstände nach einem Neustart nicht
   (bekannt aus dem Garten) – das betrifft Blätter und Tees im Inventar gleichermaßen; der Vorratsschrank ist dauerhaft.

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Blattcharge und reine Übergänge | erledigt (46 Tests) |
| A2 | Sortenbestimmung | erledigt (70 Tests gesamt) |
| A3 | Qualität | erledigt |
| A4 | Charakternoten | erledigt (86 Tests gesamt) |
| A5 | Zeit (`IGameTime`, Vorspulen) | erledigt (92 Tests gesamt) |
| B6 | ProcessedTea-Datenvertrag + Katalog | erledigt (99 Tests gesamt) |
| B7 | Übergabe Inventar/Vorratsschrank | erledigt (Unity: ungeprüft) |
| B8 | `Docs/PROCESSING_TO_BREWING.md` | erledigt |
| C9 | Welkgestell | erledigt (Unity: ungeprüft) |
| C10 | Rolltisch | erledigt (Unity: ungeprüft) |
| C11 | Oxidationskorb | erledigt (Unity: ungeprüft) |
| C12 | Wok | erledigt (Unity: ungeprüft) |
| C13 | Trockenkorb | erledigt (Unity: ungeprüft) |
| C14 | Vorschau an jeder Station | erledigt (Unity: ungeprüft) |
| D15 | Werkstatt-Szene per Builder | erledigt (Unity: ungeprüft) |
| D16 | Blatt-Darstellung | erledigt (Unity: ungeprüft) |
| D17 | Klang und Partikel, `ProcessingAudioSet` | erledigt (Unity: ungeprüft) |
| D18 | Oberfläche (UI-Baukasten, Gamepad) | erledigt (Unity: ungeprüft) |
| D19 | Eingang | erledigt (Unity: ungeprüft) |
| E20 | Speichern `workshop_save.json` | erledigt (Roundtrip getestet; Unity: ungeprüft) |
| E21 | Debug-Overlay F1 | erledigt (Unity: ungeprüft) |
| E22 | Telemetrie `processing_log.jsonl` | erledigt (Zeile getestet; Unity: ungeprüft) |
| F23 | Kräuter trocknen | erledigt (Core getestet; Unity: ungeprüft) |
| F24 | Mischtisch | erledigt (Core getestet; Unity: ungeprüft) |
| F25 | Teeziegel (Pu-Erh-Vorbereitung) | erledigt (nur Daten/Weg, getestet; keine UI, keine Reifung) |
| F26 | Werkstattgeist | erledigt (Core getestet; Unity: ungeprüft) |
| F27 | Eigene Verbesserungen | erledigt (Liste unten) |

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
- **B8** (nur Doku): `Docs/PROCESSING_TO_BREWING.md` lesen – nichts im Editor zu prüfen.
- **B7, C9–C14, D15–D18, E20–E22** (spielbare Werkstatt, ein Commit, weil die Teile nur zusammen kompilieren):
  Menü **SteepingSpirits → Processing → Build Workshop** → Konsole meldet `Built Assets/Scenes/Teewerkstatt.unity`
  und die Simulator-Zahlen; unter `Assets/_Project/Processing/Data` liegen `ProcessingSettings` und
  `ProcessingAudioSet`. Szene `Teewerkstatt` öffnen → Play:
  1. Raum mit Möbeln, Figur unten links neben der Tür. Vor einer Station steht ihr Name unten am Rand.
  2. **Vorrat** (oben links) → „Startpaket nehmen“ → dann „Neue Charge …“ → Regal zeigt die Charge (grünes Häufchen).
  3. **Welkgestell** → „Auflegen“; F2 mehrmals (je +30 min) → Text wechselt von „prall und frisch“ zu „weich und
     geschmeidig“, das Häufchen wird kleiner/dunkler. „Abnehmen“.
  4. **Rolltisch** → Charge wählen → Leertaste im Takt (Ring pulsiert, dumpfes Kneten) → Esc nach halber Zeit
     → „leicht gedreht – gut für Oolong“.
  5. **Oxidationskorb** → „In den Korb“ → Fenster offen lassen: Farbe wandert von grün zu kupfer, Duft von grasig
     über blumig zu fruchtig, „Würde jetzt zu“ wechselt → „Jetzt stoppen“.
  6. **Wok** → 12 s Hitze im hellen Band halten (Zischen, Dampf). Danach **Rolltisch** erneut (fest rollen).
  7. **Trockenkorb** → „Über die Glut“ (Glut glimmt, leises Knistern, Funken) → F3 → „Abnehmen“.
  8. **Regal** → „Als Tee einpacken“ → Meldung „+4 Oolong (hell; blumig; …)“; Inventar (I) zeigt den Tee.
  9. F1 zeigt alle Werte. Stop und Play: Chargen in Arbeit sind noch da (auch mitten im Welken).
  Ohne Sound-Assets sind leise Platzhalter zu hören; das Audio-Set darf leer bleiben.
- **D19** (Eingang vom Garten, eigener Commit): Build Workshop setzt in `Assets/Scenes/Teegarten.unity` genau ein
  Objekt `TeewerkstattTuer` (Komponente `WorkshopDoor`) auf die Hecke am Ostende des langen Gartenwegs (21,5 | 6,5).
  Sonst ändert sich am Garten nichts (kein Garten-Code, kein Layout). Prüfen: `Teegarten` öffnen → Hierarchy zeigt
  `TeewerkstattTuer`; Play → den Mittelweg nach rechts gehen: Tür mit warmem Schein und Schild „Teewerkstatt
  betreten“; E/Leertaste → Werkstatt lädt; durch die Werkstatttür (unten links) zurück in den Garten; das Gartentor
  führt weiterhin zur Wiese. **Achtung:** *Plantation → Build Garden* baut die Gartenszene neu und entfernt die Tür
  – danach *Processing → Build Workshop* (oder *Add Workshop Door To Garden*) erneut ausführen.
- **F23** (Kräuter trocknen): Build Workshop, Play. Kräuter aus dem Garten im Inventar (oder `mint+q2` per Hand
  in `pantry.json`) → **Vorrat** zeigt „Kräuter und Blüten zum Trocknen“ → „Trocknen: …“ → Trockenkorb → F3 →
  Abnehmen → Regal „Einpacken“ → Meldung „+n Getrocknete Minze (fein)“. Am Rolltisch/Wok steht bei der
  Kräuter-Charge „Kräuter und Blüten werden nur getrocknet.“
- **F24** (Mischtisch): mit einem fertigen Tee und einem getrockneten Kraut im Inventar → **Mischtisch** (unten
  Mitte) zeigt „Mischen: Jasmin-Grüntee (…)“ → klicken → Meldung „+2 Jasmin-Grüntee …“, Tee und Kraut je −1.
  Ohne passende Zutaten steht dort ein ruhiger Hinweis.
- **F25** (nur Core): `PuErhTests` grün; im Spiel nichts zu sehen.
- **F26** (Werkstattgeist): drei Weißtees machen (Welkgestell bis „weich und geschmeidig“, Trockenkorb, einpacken)
  → beim dritten erscheint die Meldung „Der Werkstattgeist schaut dir über die Schulter …“, über dem Regal schwebt
  ein kleiner Geist. Neue Charge aufs Gestell → „Dem Werkstattgeist überlassen (Weißtee)“ → mit F2 vorspulen:
  der Geist schwebt zum Gestell, dann zum Trockenkorb, Meldung; später nimmt er sie trocken heraus.
- **F27:** Charge im Oxidationskorb, Fenster offen: kurz vor dem Wechsel erscheint „Die Farbe ist kurz davor zu
  kippen.“, beim Wechsel ein leiser Klang und „Charge n kippt: jetzt …“. Gestell/Trockenkorb melden sich einmal,
  wenn die Charge bereit ist (auch bei geschlossenem Fenster). `OxidationForecastTests` grün.

## Abschlussbericht

### Gebaut
- **Core** (`SteepingSpirits.Processing.Core`, engine-frei): Blattcharge mit reinen Übergängen, Sortenbestimmung
  (Bänder, Lücken → nächste Sorte), Qualität (Herkunft + Sorgfalt, Obergrenze), Charakternoten, Spielzeit
  (`IGameTime`, Vorspulen), Datenvertrag `ProcessedTea` mit sprechender ID, Minispiel-Modelle Rollen/Wok,
  Spielstand `workshop_save.json` + Tee-Register, Telemetrie-Zeile, Werkstatt-Simulator, deutsche Textklasse,
  Raumlayout, Kräuter trocknen, Mischungen, Pu-Erh-Vorbereitung, Werkstattgeist, Oxidations-Vorhersage.
- **Laufzeit:** begehbare Werkstatt (`TeaWorkshop`), Stationsfenster mit Gamepad-Navigation
  (`WorkshopController`, `WorkshopUi`), Blatt-Darstellung (`LeafLook`, `StationView`), Klang/Partikel
  (`ProcessingSynth`, `WorkshopFeedback`, `ProcessingAudioSet`), Übergabe (`WorkshopItems`), Speichern,
  Telemetrie, Debug-Overlay, Werkstattgeist (`SpiritView`), Mischtisch (`BlendStation`), Gartentür (`WorkshopDoor`).
- **Editor:** *SteepingSpirits → Processing → Build Workshop*, *Add Workshop Door To Garden*, *Simulate 100 Batches*.
- **Doku:** `Docs/PROCESSING_TO_BREWING.md` (Vorschlag zur Brüh-Anbindung).

### Offen / Risiken
- **Nichts davon lief in Unity** (Fenster-Layout, Gamepad-Navigation, Sortierung, Klang, Gefühl der Minispiele).
- Inventar lädt laufzeit-registrierte Gegenstände nach einem Neustart nicht (bekannt aus dem Garten) – Tees und
  Blätter im Inventar sind dann weg; der Vorratsschrank bleibt. Gemeinsamer Speicherdienst nötig.
- Der Garten liefert den „ersten Austrieb“ noch nicht als eigene Zutat (Merge-Vorschlag, Annahme 7).
- Pu-Erh: nur Daten, keine Station, keine Reifung (bewusst, F25).

### Testergebnisse (ohne Unity)
- `dotnet test Tests/ProcessingCore.DotNet`: **142/142** (Übergänge-Wahrheitstabelle, Sorten inkl. Grenzwerte,
  2000 Zufallsabfolgen → immer gültige Sorte, Qualität, Noten, Zeit, Datenvertrag/IDs, Minispiele, Speichern als
  Rundreise und Fortsetzung, Determinismus, Simulator, Kräuter, Mischungen, Pu-Erh, Geist, Vorhersage, Texte).
- `Tests/Ingredients.DotNet` **12/12**, `Tests/PlantationCore.DotNet` **108/108**, `Tests/PlatformingCore.DotNet` **137/137**.
- Kompilieren gegen Unity-Referenzen inkl. Editor und Tests: neues Input System und alter Input Manager
  **fehlerfrei, keine Warnungen**.
- Werkstatt-Simulator (je 100 Chargen): siehe Tabelle oben – sorgfältig 100 % Treffer, Ø Stufe 2,49, nie „gut“;
  nachlässig 55 % Treffer, Ø 2,02, 0 unbrauchbar; zufällig 19 %, 0 unbrauchbar. Aktive Zeit sorgfältig:
  dunkler Oolong max 76 s, heller Oolong 50 s, Weißtee 5 s.

### Testplan für dich (≤ 10 Minuten, wichtigstes zuerst)
1. **(2 min)** *Processing → Build Workshop*, Szene `Teewerkstatt`, Play. Vorrat → Startpaket → neue Charge →
   Welkgestell → F2 ein paar Mal bis „weich und geschmeidig“ → abnehmen. Lesen sich Farbe/Duft/Gefühl gut?
2. **(2 min)** Rolltisch: im Takt kneten, nach halber Zeit Esc → Oxidationskorb → Fenster offen lassen und
   zusehen, wie Farbe und Duft wandern; bei „heller Oolong“ stoppen. Fühlt sich das Beobachten gut an?
3. **(1 min)** Wok: 12 s Hitze im hellen Band halten (Leertaste halten/loslassen). Zu ruhig, zu hektisch?
4. **(1 min)** Rolltisch erneut, Trockenkorb, F3, abnehmen, Regal → „Als Tee einpacken“ → Meldung mit Sorte, Noten,
   Qualität; Inventar (I).
5. **(1 min)** Zweite Charge im Korb absichtlich zu lange liegen lassen → wird dunkler Oolong/Schwarztee, kein
   Fehlschlag.
6. **(1 min)** Gamepad: Fenster mit Steuerkreuz/A bedienen, B schließt; Rollen per Stick-Kreisen.
7. **(1 min)** Stop/Play: Chargen in Arbeit sind noch da. Garten bauen (*Plantation → Build Garden*), dann
   *Build Workshop* erneut → im Garten am Ostende des Wegs die Werkstatttür; hin und zurück; das Gartentor führt
   weiter zur Wiese.
8. **(1 min)** F1-Overlay ansehen; Test Runner → EditMode `SteepingSpirits.Processing.Tests.EditMode` grün.

### Merge-Hinweise
- **Basis:** `feature/plantation` (e7dee0e). Reihenfolge: `feature/jumpnrun` → `feature/plantation` →
  `feature/processing`.
- **Katalog (additiv):** `tea_white`, `tea_green`, `tea_oolong`, `tea_black`, `tea_blend` (neue Familie `Tea`),
  `tea_leaf_first`, `mint_dried`, `chamomile_dried`, `jasmine_dried`, `lavender_dried`, `lemon_balm_dried`;
  Katalog-Test 15 → 26. Alle „häufig“ – das Seltenheitsregal im Jump'n'Run bleibt unverändert.
- **IDs:** Tees `tea_oolong:dark/honeyed.roasty+q3`, Mischungen `tea_blend:green~jasmine/fresh.floral+q2`,
  getrocknete Kräuter `mint_dried+q2`; im Inventar mit Präfix `ingredient_`.
- **Eingang:** genau ein Objekt `TeewerkstattTuer` in `Teegarten.unity` per Builder. *Plantation → Build Garden*
  baut die Gartenszene neu und entfernt es → danach *Processing → Build Workshop* erneut.
- **Garten (Vorschlag):** erste Teestrauch-Ernte als `tea_leaf_first` ausgeben.
- **Brühen:** siehe `Docs/PROCESSING_TO_BREWING.md` – Basis-Preset je Sorte + kleine Verschiebungen durch
  Oxidation (Oolong-Fenster wandert mit), Qualität (Kapazität/Toleranz) und Noten; Teewahl aus Inventar/Vorrat
  (Familie `Tea`); eine „herbe“ Tasse sollte `tea_tart` ausgeben (Kompost im Garten).

### Vorschläge für `CLAUDE.md`
- Modulaufbau festschreiben (`Core` engine-frei mit `Tests/<Modul>.DotNet`, `Runtime`, `Editor`-Builder als einzige
  Quelle für Szenen) und die ID-Grammatik für Zutaten-Varianten (`:stil`, `/noten`, `~kraut`, `+qN`) als
  Projektregel – neue Varianten nie als neue Katalog-Einträge.
- **Szenenwechsel-Regel:** `ScenePortal.Load` nur für „echte“ Ortswechsel mit Rückweg über `PreviousScenePath`;
  Unterorte (Werkstatt im Garten) mit eigenem Lader, sonst verbiegt sich der Rückweg.
- **Uhr-Regel:** UI, die Zeit beobachten lässt, darf `GamePause` nicht setzen (die Uhr steht sonst still).
- Gemeinsamer Speicherdienst (Inventar mit Laufzeit-Items, `GameClock`) und eine Tastenbelegungs-Tabelle für alle
  Module (F1 ist überall Debug, F2/F3 vorspulen).
- Builder, die fremde Szenen ergänzen (Wiese ← Garten, Garten ← Werkstatt), brauchen eine gemeinsame Lösung,
  z. B. einen Szenen-Builder, bei dem Module ihre Eingänge anmelden.
