# Teekreislauf – Fortschritt (Langlauf)

## Nächster konkreter Schritt

→ Alle Punkte A1–F13 erledigt. Als Nächstes: Testplan unten (≤ 10 min) in Unity durchgehen, Befunde in
`Docs/PLAYTEST_NOTES.md` eintragen; offene Entscheidungen (Mischverhältnis, Optionen O2/B/C) siehe E12.

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
| C7 | Erste Teestrauch-Ernte = `tea_leaf_first` | erledigt (Garten 109 Tests; Unity: ungeprüft) |
| D8 | `ForProcessedTea` + `ProcessedBrewParams` | erledigt (Brühen 90 Tests) |
| D9 | Teewahl aus Inventar/Vorratsschrank | erledigt (Brühen 92, TeaLoop 6 Tests; Unity: ungeprüft) |
| D10 | Ergebnis: Noten, `tea_tart` | erledigt (TeaLoop 7 Tests; Unity: ungeprüft) |
| D11 | Brühen aus der Welt erreichbar | erledigt (TeaLoop 8 Tests; Unity: ungeprüft) |
| E12 | Simulator-Auswertung | erledigt (nur Auswertung, keine Werte geändert; Werkstatt 145, TeaLoop 9 Tests) |
| F13 | Abschlussbericht | erledigt (siehe unten) |

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
| `Teewerkstatt.unity` | `Bruehtisch` | *Brewing → Build Sandbox* / *Add Brewing Table To Workshop* (D11) |

- Folge: *Build Garden* nach *Build Workshop* lässt die Werkstatttür stehen; *Build Levels* nach *Build Garden*
  lässt den Garten-Eingang auf der Wiese stehen. Die Hinweise „danach nochmal bauen“ entfallen.

## Erster Austrieb (C7)

- `PlantParams.firstHarvestIngredientId` (neu, Standard leer): Teestrauch = `tea_leaf_first`. Die erste Ernte
  gibt diese Zutat, alle späteren `tea_leaf`. Meldung und Inventar heißen „Erster Austrieb (…)“.
- **Zählt der alte +1-Bonus doppelt?** In der Werkstatt nicht – dort zählt ein erster Austrieb unabhängig von
  seiner Gartenstufe als beste Herkunft und hebt die Obergrenze auf „erlesen“. Der Bonus wäre also wirkungslos für
  den Tee, würde aber die Gartenpflege verschleiern (ein vernachlässigter Austrieb hieße „fein“ statt „gut“) und
  den Verkaufswert erhöhen. **Entscheidung:** Bonus für den Teestrauch 0 – das Besondere ist die eigene Zutat,
  die Stufe zeigt ehrlich die Pflege. Der allgemeine Mechanismus `firstHarvestQualityBonus` bleibt für spätere
  Pflanzen erhalten.
- **Bestehende Assets:** Builder überschreiben Tuning nie. *Build Garden* migriert deshalb nur dieses neue Feld:
  ist `firstHarvestIngredientId` leer und gibt der Startwert eines vor, wird er gesetzt und ein alter Bonus von
  genau 1 auf 0 gestellt (Konsolenmeldung). Andere Werte bleiben.

## Brüh-Anbindung, Kern (D8)

- `ProcessedBrewing.ForProcessedTea(preset, info, k)` im Brüh-Core: reine Funktion, liefert eine angepasste Kopie.
  Der Brüh-Core kennt die Werkstatt nicht; er bekommt `ProcessedTeaInfo` (Grundtee, Oxidation, Stufe, Noten als
  Flags). Die Übersetzung aus der Werkstatt-ID ist ein Adapter in D9.
- Werte wie im Vorschlag (`PROCESSING_TO_BREWING.md`), alle in `ProcessedBrewParams` (Feld `processed` im
  `BrewingTuning`): Oolong-Fenster 16 °C je Oxidation um 35 % (± 6 °C), Hitzeempfindlichkeit hell 1,2 → dunkel 0,8
  (relativ zum Neutralpunkt); Schwarztee unter 85 % −0,5 s bis bitter; Stufe → Kapazität 0,9/1,0/1,15, Toleranz
  ±0/+0,03/+0,06 (relativ zu „fein“), Aroma-Rate 0,95/1,0/1,05; Noten wie vorgeschlagen, zusammen höchstens ±4 °C.
- **Abweichung vom Vorschlag – Erreichbarkeit:** Das heißeste erreichbare Wasser im Gefäß ist 100 °C − 3 °C
  (vorgewärmt) = 97 °C. Ein dunkler Oolong hätte mit der Fensterwanderung der späteren Aufgüsse (+6 °C) seinen
  besten Aufguss nicht mehr erreicht. Neuer Parameter `reachableTop` (97 °C): Grundfenster und Wanderung werden so
  begrenzt, dass jede Fenstermitte erreichbar bleibt. Dunkler bleibt heißer (getestet), nur steigen die späten
  Aufgüsse weniger. Das Fenster endet nie über 100 °C und bleibt mindestens 4 °C breit.
- Tests: Preset unverändert bei Stufe 2/ohne Noten/neutraler Oxidation (alle vier Grundtees), Eingaben werden nie
  verändert, dunkler Oolong monoton heißer (hell 20 % ↔ dunkel 50 %: > 4 °C Unterschied), keine Notenkombination
  > ±4 °C, Stufe wirkt, Noten wirken, Schwarztee unteroxidiert früher bitter, **„Vollendet“ erreichbar** für über
  500 Kombinationen (jede Fenstermitte ≤ 97 °C), **Regression:** Kalibrierwerte der vier Grundtees exakt wie vorher
  (Qref und beste Ziehzeit als feste Zahlen).

## Teewahl (D9)

- **Eigene Tees** = Werkstatt-Tees und Mischungen (Familie `Tea`) aus Inventar **und** Vorratsschrank, mit genauer
  Oxidation aus dem Tee-Register der Werkstatt (`workshop_save.json`), sonst dem Optimum der Sorte. Übersetzung im
  engine-freien Adapter `OwnTeaInfo` (Brühen/Runtime/Integration), Laden/Verbrauchen in `OwnTeaShelf`.
- Gebrüht wird das Grund-Preset (`TeaDefinition`, liefert auch die Farben) angepasst durch `ForProcessedTea`, mit
  eigener Kalibrier-ID `own:<zutaten-id>`; `BrewSession.UseTea` nimmt ihn in die Auswahl auf (Grundtees behalten
  ihre Plätze 1–4).
- **Eine Portion pro Kanne:** Die Portion wird beim **ersten Aufgießen** der gewählten Blätter genommen (Inventar
  zuerst, dann Vorratsschrank); weitere Aufgüsse derselben Blätter (Phase 2) kosten nichts. Wählen allein kostet
  nichts. Das ist meine Lesart von „ein Aufguss verbraucht eine Portion“ – Blätter tragen mehrere Aufgüsse.
- **Grundtees** 1–4 frei in der Sandbox (direkt gestartet); aus der Welt geöffnet nur mit offenem Debug (F1).
- Tests (dotnet `Tests/TeaLoop.DotNet`, weil der Adapter Brühen und Werkstatt verbindet und Unity-Testassemblies
  Assembly-CSharp nicht referenzieren können): Register-Oxidation, Optimum ohne Register, Mischungen als Grundtee
  mit ihren Noten, fremde IDs, **heller ↔ dunkler Oolong: Fenstermitte > 4 °C auseinander und andere Ziehzeit**,
  jede Werkstatt-ID wird brühbar. Brüh-Core: eigene Kalibrier-ID je Tee, `UseTea` behält Grundtee-Plätze.

## Ergebnis der Tasse (D10)

- Die Ergebniskarte nennt bei eigenen Tees die Noten: „Im Tee: blumig, honigsüß.“ (Texte aus `ProcessingTexts`,
  also dieselben Wörter wie in der Werkstatt). Grundtees haben keine Noten → keine Zeile.
- **Herbe Tasse → `tea_tart`:** Ist das Ergebnis herb (`BrewResult.IsTart`), kommt ein „Herber Tee“ (`tea_tart`,
  schlichte Item-ID wie in `GardenSettings.compostInputIds`) ins Inventar, sonst in den Vorratsschrank; die Karte
  sagt „Den Rest gießt du in den Krug für den Komposter im Teegarten.“ Pro herbem Aufguss eins.
- Nur bei **eigenen Tees** (Annahme 2). Der Speicherdienst kennt `tea_tart` über einen Resolver und baut ihn nach
  dem Neustart wieder auf.
- Test: Vertrag Brühen ↔ Garten (`TartCompostTests`): die herbe Item-ID steht in der Standardliste des Komposters.

## Brühtisch (D11)

- Kleinste Lösung: ein **Brühtisch in der Teewerkstatt** (Wurzelobjekt `Bruehtisch`, Komponente
  `BrewingTable` aus dem Brühen-Modul) auf der freien Zelle 13,7 (Nordostecke). Davor stehen, Benutzen-Taste
  (E/Leertaste/J/F, Pad A/X) → die Brüh-Szene öffnet mit der Teewahl der eigenen Tees; **Esc/B** in der Teewahl
  oder im Ergebnis führt zurück in die Werkstatt. Ohne eigenen Tee bleibt man in der Werkstatt, der Tisch sagt
  „Noch keine eigenen Tees – …“.
- Szenenwechsel über `WorkshopSceneLoader` (Unterort) – `ScenePortal.PreviousScenePath` bleibt unberührt, das
  Gartentor führt danach weiter zur Wiese. Die Rückkehr kennt `BrewHandoff.ReturnScenePath`.
- Ist ein Werkstattfenster offen (Figur gesperrt), reagiert der Tisch nicht. Ein Test sichert, dass im Umkreis
  von zwei Zellen keine Station steht – die Benutzen-Taste hat dort also nur eine Bedeutung.
- Bauen in jeder Reihenfolge: *Build Sandbox* stellt den Tisch in die Werkstatt (wenn es sie gibt) und trägt die
  Brüh-Szene in die Build-Liste ein; *Build Workshop* ersetzt nur `TeaWorkshop` und ruft danach das Menü
  *Brewing → Add Brewing Table To Workshop* per Namen auf (keine Modul-Referenz) – fehlt die Brüh-Szene noch,
  kommt nur eine Warnung.
- Der Teeladen kommt später; bis dahin ist der Werkstatt-Tisch der einzige Weg aus der Welt.

## Simulator-Auswertung (E12)

Nur ausgewertet, **nichts geändert**. Der Werkstatt-Simulator zählt jetzt zusätzlich nach Blattherkunft, nach
entstandener Sorte (mit Ø Sorgfalt) und die Sorgfalt je Schritt; Menü *Processing → Simulate 100 Batches* gibt das
mit aus. Tabellen: `WorkshopEvaluationTests.Evaluation_Tables` (Werkstatt) und `BlendRatioTests` (TeaLoop).
Die bisherigen Zahlen bleiben gleich (gleiche Zufallsfolge).

**1. Nach Blattherkunft** (je 200 Chargen, nur diese Herkunft; gut / fein / erlesen, Ø Stufe):

| Bot | Gartenstufe gut | Gartenstufe fein | Gartenstufe erlesen | erster Austrieb |
|---|---|---|---|---|
| sorgfältig | 0/200/0 (2,00) | 0/200/0 (2,00) | 0/0/200 (3,00) | 0/0/200 (3,00) |
| nachlässig | 110/90/0 (1,45) | 18/182/0 (1,91) | 0/110/90 (2,45) | 0/110/90 (2,45) |
| zufällig | 69/131/0 (1,66) | 13/187/0 (1,94) | 3/66/131 (2,64) | 3/66/131 (2,64) |

- Gewöhnliche Blätter (Gartenstufe gut/fein) erreichen nie „erlesen“; Spitzenblätter (Gartenstufe erlesen, erster
  Austrieb, Goldspitze) bei sorgfältiger Arbeit immer. Austrieb und erlesenes Blatt rechnen identisch (Herkunft 1,
  Obergrenze 3) – der Austrieb ist das *sichere* Spitzenblatt der ersten Ernte.
- **Lesart der festen Entscheidung:** „gewöhnliche Blätter“ = Gartenstufe gut/fein, wie in
  `PROCESSING_PROGRESS.md` definiert und im Code umgesetzt. Die strengere Lesart (nur Austrieb/Goldspitze) steht
  unten als Option B zum Vergleich.

**2. Gemischte Blätter** (50 % erlesen / 30 % fein / 20 % gut, 5 % Austrieb; 100 Chargen), erlesen-Anteil je Sorte
und Ø Sorgfalt je Schritt:

| Bot | gut/fein/erlesen | Weiß | Grün | hell. Oolong | dunk. Oolong | Schwarz | welken | rollen | Oxidation | Wok |
|---|---|---|---|---|---|---|---|---|---|---|
| sorgfältig | 0/51/49 | 6/13 | 10/20 | 8/19 | 10/21 | 15/27 | 1,00 | 1,00 (87×) | 0,99 | 0,97 (60×) |
| nachlässig | 18/62/20 | 7/21 | 0/6 | 3/20 | 4/28 | 6/25 | 0,78 | 0,76 (79×) | 0,66 | 0,35 (64×) |
| zufällig | 15/53/32 | 12/31 | 1/15 | 2/13 | 1/14 | 16/27 | 0,81 | 0,77 (54×) | 0,79 | 0,34 (53×) |

**3. Wie stark die Pflege (Gartenstufe) zählt:** Wert = 0,3 × Herkunft + 0,7 × Sorgfalt. Für sorgfältige Spieler
entscheidet allein die Gartenstufe zwischen „fein“ und „erlesen“ (die Obergrenze greift, 31 von 100 Chargen
hätten sonst „erlesen“). Für ungenaue Spieler bringt jede Gartenstufe etwa eine halbe Qualitätsstufe
(nachlässig Ø 1,45 → 1,91 → 2,45). Die Pflege zählt also deutlich, ohne die Werkstatt zu entwerten.

**4. Warum „nachlässig“ weniger „erlesen“ hat als „zufällig“ (20 gegen 32):** Sorgfalt ist das Mittel der
*durchlaufenen* Schritte. Der Zufalls-Bot überspringt oft Rollen und Wok – die Schritte, die man verpatzen kann
(Wok-Sorgfalt nur 0,34) – und landet so oft bei Weiß- und Schwarztee (58 % gegen 46 %), deren Oxidation fast
immer 1 zählt (Weiß unter, Schwarz über dem Optimum). Am deutlichsten beim Schwarztee: zufällig 16 von 27
erlesen (oft ungerollt, lange liegen gelassen), nachlässig 6 von 25 (immer gerollt, ungleichmäßig). Der
nachlässige Bot geht dagegen jeden Weg vollständig und stoppt systematisch zu spät (Oxidation 0,66).

**5. Optionen** (Zahlen: gut/fein/erlesen bei gemischten Blättern):

| Option | sorgfältig | nachlässig | zufällig | Vorteile | Nachteile |
|---|---|---|---|---|---|
| **A – so lassen** | 0/51/49 | 18/62/20 | 32 erlesen | Einfach, verständlich; einfache Tees bleiben anfängerfreundlich, Grün/Oolong sind die Meisterklasse. | Wer sich am Wok müht, bekommt seltener „erlesen“ als jemand, der Schritte auslässt. |
| **O2 – „erlesen“ ab 0,75 statt 0,80** | 0/51/49 | 18/56/26 | 15/47/38 | Ein Wert; mehr Spitzentee für normale Spieler (auf erlesenen Blättern nachlässig 90 → 120 von 200), Sorgfältige unverändert. | Lücke zu „zufällig“ bleibt; „erlesen“ wird etwas gewöhnlicher. |
| **O3 – Wok-Fenster 0,32 statt 0,24** | 0/51/49 | 15/68/17 | 14/51/35 | Wok verzeiht mehr (Sorgfalt 0,35 → 0,48), nachlässig trifft öfter die Wunschsorte (55 → 65 %). | Kaum mehr „erlesen“; Wok verliert Spannung. |
| B – nur Austrieb/Goldspitze heben die Grenze | 0/91/9 | 18/80/2 | 15/83/2 | Strengste Lesart, „erlesen“ wird selten und besonders. | Gartenpflege zählt für die Spitze nicht mehr; Spitzentee hängt an 1 Ernte pro Strauch. |
| C – Herkunftsgewicht 0,45 statt 0,3 | 0/51/49 | 21/52/27 | 26/36/38 | Gartenpflege zählt mehr. | Mehr „gut“ bei schwachen Blättern; Werkstatt-Sorgfalt zählt weniger. |

**6. Mischverhältnis** (Brühen verbraucht jetzt 1 Portion pro Kanne; Werkstatt: 1 Blatt = 1 Portion):
Teestrauch erste Ernte nach 14 Tagen, dann 2–3 Blätter alle 3 Tage (Ø 2,5 je Ernte, 5,8 je Woche); Minze nach
4 Tagen 2–3 Stück – Kräuter sind reichlich, Teeblätter knapp.

| Tee | Aufgüsse je Kanne | Kannen je Ernte: pur / 1+1=2 / 1+1=1 | Tassen je Ernte: pur / =2 / =1 |
|---|---|---|---|
| Weiß | 4 | 2,5 / 5 / 2,5 | 10 / 20 / 10 |
| Grün | 3 | 2,5 / 5 / 2,5 | 8 / 15 / 8 |
| Oolong | 6 | 2,5 / 5 / 2,5 | 15 / 30 / 15 |
| Schwarz | 3 | 2,5 / 5 / 2,5 | 8 / 15 / 8 |

- **1+1=2** verdoppelt die Kannen aus den knappen Teeblättern und gibt den reichlichen Kräutern einen Zweck.
  Nebenwirkung: Wert 4 (Tee) + 2 (Kraut) = 6 Gold wird zu 2 × 6 = 12 Gold – Mischen ist dann immer die
  bessere Wahl, sobald es einen Verkauf gibt.
- **1+1=1** macht Mischen zur reinen Geschmacksfrage; Kräuter werden zum „Preis“ für die Noten, pure Tees sind
  gleichwertig.

**Empfehlung:** Alles so lassen (Option A), Mischverhältnis vorerst **1+1=2** – Teeblätter sind knapp, die
Teeschleife soll sich nach den 14 Tagen bis zur ersten Ernte nicht karg anfühlen. Mit dem Teeladen den Goldwert
der Mischung prüfen (z. B. 3 + 3 × (Stufe − 1) je Portion statt 6, dann 6 → 6 Gold). Falls Playtests „erlesen“
für normale Spieler als zu selten melden: zuerst **O2** (ein Wert in `ProcessingSettings`).

## Annahmen

1. **Speicherdienst legt Inventar und Uhr in jeder Szene an.** Folge: Auch eine direkt gestartete
   Jump'n'Run-Szene hat jetzt ein Inventar; die Übergabe dort geht ins Inventar statt in den Vorratsschrank
   (beides bleibt gespeichert, nichts geht verloren).
2. **`tea_tart` nur aus eigenen Tees.** Die Grundtees der Sandbox sind Übung und kosten nichts; gäben sie
   Kompost-Futter, ließe sich der Komposter beliebig füllen.
3. **Teewahl erst am Brühplatz, nicht am Tisch.** In der Werkstatt bewegen ←/→ die Figur; die Teewahl der
   Brüh-Szene (D9) ist schon da. „Öffnet das Brühen mit gewähltem Tee“ heißt hier: mit der Auswahl der eigenen
   Tees, ein Druck auf die Leertaste brüht. `BrewHandoff.RequestedTeaId` bleibt als Einhak-Punkt für den Teeladen.
4. **Die Brüh-Sandbox ist zugleich der Brühplatz.** Aus der Welt geöffnet sind die Grundtees gesperrt (nur F1);
   eine eigene Teehaus-Szene lohnt erst mit dem Teeladen. Zurück steht man am Werkstatt-Eingang, nicht am Tisch.

## Editor-Checklisten pro Commit

- **E12** (Auswertung): *Processing → Simulate 100 Batches* → Konsole zeigt je Bot zusätzlich „by origin“ und
  „by tea … steps“. Test Runner: `WorkshopEvaluationTests` grün (Ausgabe = Tabellen oben). Keine Werte geändert.
- **D11** (Brühtisch): *Brewing → Build Sandbox*, dann *Processing → Build Workshop* (und umgekehrt) →
  `Teewerkstatt` hat `TeaWorkshop` **und** `Bruehtisch`; Build Settings enthalten `BrewingSandbox`. Play in der
  Werkstatt: Tee einpacken, zum Tisch oben rechts → „Brühtisch – Tee aufgießen“ erscheint → E → Brühen mit „Deine
  Tees“, Grundtees 1–4 gesperrt. Brühen, Esc → wieder in der Werkstatt, Tee eine Portion weniger. Werkstatt →
  Garten → Gartentor führt zur Wiese (nicht in die Werkstatt). Ohne eigenen Tee: Tisch zeigt den Hinweis.
- **D10** (Ergebnis): Sandbox, eigenen Tee mit Noten brühen → Ergebniskarte zeigt „Im Tee: …“. Absichtlich zu
  lange ziehen (herb) → Karte sagt „Ein herber Nachklang. Den Rest gießt du in den Krug …“, Inventar hat
  „Herber Tee“. Im Teegarten am Komposter abgeben → wird angenommen. Grundtee herb → kein `tea_tart`.
- **A1** (Merge): Unity öffnen → Konsole ohne Fehler. Menü *SteepingSpirits → Brewing → Build Sandbox* ist da und
  baut die Sandbox; Play → Brühen wie bisher. Test Runner → EditMode: alle Module grün.
- **D9** (Teewahl): Sandbox bauen (*Brewing → Build Sandbox*), in der Werkstatt einen Tee einpacken, dann
  Sandbox → Play: unten „Deine Tees: ◂ Oolong (hell; …) ×4 ▸ [←/→] Tee wählen [Leertaste] brühen“, darunter die
  Grundtees 1–4. Eigenen Tee wählen → Titel zeigt seinen Namen, Farben wie der Grundtee. Aufgießen → Inventar hat
  eine Portion weniger; Leertaste im Ergebnis (nochmal aufgießen) kostet nichts. Ohne eigenen Tee: Hinweis
  „Noch keine eigenen Tees …“, Grundtees funktionieren wie bisher.
- **D8** (nur Core): Test Runner → `SteepingSpirits.Brewing.Tests.EditMode` grün (90). `BrewingTuning`-Asset im
  Inspector: neuer Abschnitt `Processed` mit allen Faktoren. Brühen in der Sandbox unverändert.
- **C7** (Erster Austrieb): *Plantation → Build Garden* (bei altem `tea.asset` erscheint „tea: first harvest now
  gives tea_leaf_first“). Garten: Teestrauch säen, mit F2 bis reif → ernten → Meldung „+n Erster Austrieb (…)“,
  Inventar zeigt „Erster Austrieb“; nächste Ernte wieder „Teeblatt“. In der Werkstatt erscheint der Austrieb im
  Vorrat als eigene Quelle.
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

## Abschlussbericht (F13)

### Gebaut

- **A – Fundament:** `feature/brewing-phase2` eingemergt (Merge-Commit 68d4c81 getrennt), `PLAYTEST_NOTES.md`
  (Vorlage, keine Befunde vorhanden), `CLAUDE.md` (Arbeitsregeln, Modulaufbau, ID-Grammatik, Szenen-/Uhr-Regel,
  Speicherdienst, feste Entscheidungen, Tastentabelle).
- **B – Festigen:** Speicherdienst `SaveGame` (Inventar inkl. Laufzeit-Items aus der ID-Grammatik, `GameClock`,
  versioniert, altes `inventory_save.json` wird übernommen, unbekannte IDs warten in `pending`); Builder ersetzen
  nur ihre eigenen Wurzelobjekte (`OwnedSceneObjects`, Besitzer-Tabelle B5); Tastentabelle aller Module, eine
  Kollision behoben (Pad A = Springen und Tür in Jump'n'Run).
- **C – Garten:** Erste Teestrauch-Ernte liefert `tea_leaf_first` („Erster Austrieb“), ohne Extra-Stufe.
- **D – Brühen angebunden:** `ProcessedBrewing.ForProcessedTea` + `ProcessedBrewParams` (rein, getestet, Grundtees
  unverändert); Teewahl aus Inventar und Vorratsschrank, eine Portion pro Kanne; Ergebnis nennt die Noten, herbe
  Tasse → `tea_tart` für den Komposter; Brühtisch in der Werkstatt öffnet das Brühen und führt zurück.
- **E – Auswertung:** Simulator nach Herkunft/Sorte/Schritt, Optionen, Mischverhältnis (siehe E12).

**Offen / bewusst nicht gemacht:** Teeladen (später), eigene Teehaus-Szene (die Sandbox ist der Brühplatz),
Teewahl am Tisch selbst (Annahme 3), Rückkehr direkt an den Tisch statt an den Werkstatt-Eingang.

### Tests (dotnet, alle grün)

| Suite | Tests | neu/geändert in diesem Auftrag |
|---|---|---|
| BrewingCore | 92 | +15: `ProcessedBrewingTests` (unverändertes Preset, Regression der vier Grundtees mit Goldwerten, Oxidation monoton, Stufen, Noten, Deckel, „Vollendet“ in > 500 Kombinationen erreichbar, späte Aufgüsse dunkler Oolong erreichbar), `UseTea` |
| Ingredients | 12 | – |
| PlantationCore | 109 | +1 erster Austrieb, Teestrauch-Erwartungen angepasst |
| PlatformingCore | 137 | – |
| ProcessingCore | 145 | +3 `WorkshopEvaluationTests` |
| SaveGame | 15 | neu: Rundreise dynamischer IDs, Migration alter Speicherstand, unbekannte ID bleibt erhalten, Uhr, Versionen |
| TeaLoop | 9 | neu (modulübergreifend): Werkstatt-Tee → Brühparameter, heller ↔ dunkler Oolong, `tea_tart` ↔ Komposter, Brühtisch-Zelle, Mischverhältnis |

Kompiliert gegen die Unity-Referenzen (Runtime, Editor, EditMode-Tests) mit neuem Input System und altem Input
Manager: 0 Fehler, 0 Warnungen. In Unity selbst ist alles **ungeprüft**.

### Simulator (Werkstatt, 100 Chargen, gemischte Blätter – unverändert)

sorgfältig 100 % Wunschsorte, 0/51/49 (Ø 2,49) · nachlässig 55 %, 18/62/20 (Ø 2,02) · zufällig 19 %, 15/53/32
(Ø 2,17). Aufschlüsselung, Pflege-Einfluss, Optionen und Mischverhältnis: Abschnitt E12.

### Geänderte Startwerte

| Wert | vorher | jetzt | Grund |
|---|---|---|---|
| Teestrauch `firstHarvestBonus` | 1 | 0 | Doppelzählung: der Austrieb hebt in der Werkstatt schon Herkunft und Obergrenze; der Bonus hätte die Gartenpflege verdeckt (C7). `Build Garden` setzt alte Assets einmalig um. |
| Teestrauch `firstHarvestIngredientId` | – | `tea_leaf_first` | feste Entscheidung (C7) |
| `BrewingTuning.processed` | – | neu (Oolong 16 °C je Oxidation um 0,35, max ± 6 °C; Stufen ±; Noten max ± 4 °C; `reachableTop` 97 °C) | D8, Vorschläge aus `PROCESSING_TO_BREWING.md` |

### Annahmen und Abweichungen

- Annahmen 1–4 oben (Inventar in jeder Szene; `tea_tart` nur aus eigenen Tees; Teewahl am Brühplatz; Sandbox =
  Brühplatz). Lesart „gewöhnliche Blätter“ = Gartenstufe gut/fein (E12).
- Eine Portion pro **Kanne** (erstes Aufgießen), weitere Aufgüsse derselben Blätter kosten nichts (D9).
- **Abweichung D8:** `reachableTop` (97 °C) begrenzt die Fenstermitte – sonst wäre der beste Aufguss eines sehr
  dunklen Oolongs (~99 °C) mit vorgewärmtem Gefäß nicht erreichbar. Steht auch in `PROCESSING_TO_BREWING.md`.
- Brühtisch per Menü-Name aus *Build Workshop* aufgerufen statt einer Modul-Referenz (D11).

### Editor-Checklisten

Pro Commit im Abschnitt „Editor-Checklisten pro Commit“ (neueste zuerst).

### Testplan (≤ 10 Minuten, wichtigstes zuerst)

1. **Bauen (1 min):** *Processing → Build Workshop*, dann *Plantation → Build Garden*, dann *Brewing → Build
   Sandbox*. `Teegarten` hat `TeaGarden` + `TeewerkstattTuer`; `Teewerkstatt` hat `TeaWorkshop` + `Bruehtisch`.
   Danach in umgekehrter Reihenfolge noch einmal (*Build Garden* → *Build Workshop*): wieder alles da, nichts
   doppelt. Konsole ohne Fehler.
2. **Speichern (1,5 min):** Play in `Teewerkstatt` → Startpaket nehmen, Weißtee machen (welken, mit F2/F3
   vorspulen, trocknen, einpacken) → Inventar (I) zeigt ihn. **Stop → Play:** Tee ist noch im Inventar,
   Uhrzeit/Tag wie vorher.
3. **Voller Kreislauf (4 min):** Play in `Teegarten` → Werkstatttür (E) → Tee machen oder den aus 2. nehmen →
   Brühtisch oben rechts (E) → Brühen zeigt „Deine Tees“, Grundtees gesperrt → Leertaste → absichtlich zu lange
   ziehen lassen → Karte: „Im Tee: …“, „Ein herber Nachklang. Den Rest gießt du …“ → **Esc** → Werkstatt (eine
   Portion weniger, „Herber Tee“ im Inventar) → Tür G → Garten → am Komposter abgeben (wird angenommen) →
   **Gartentor führt zur Wiese**, nicht in die Werkstatt.
4. **Heller ↔ dunkler Oolong (2,5 min):** In der Werkstatt einen hellen (früh aus dem Korb) und einen dunklen
   Oolong (spät) machen, mit F2/F3. Am Brühtisch nacheinander wählen (←/→), **F1** zeigt Fenster und Optimum:
   der dunkle liegt mehrere °C höher (Tests: > 4 °C), die Ziehzeit unterscheidet sich; der Aufguss fühlt sich
   anders an.
5. **Grundtees (1 min):** `BrewingSandbox` direkt starten → Tasten 1–4 brühen wie bisher; ohne eigenen Tee
   steht der Hinweis „Noch keine eigenen Tees …“.

### Merge-Hinweise

- `feature/tea-loop` enthält `main` (5629997), `feature/jumpnrun`, `feature/plantation`, `feature/processing` und
  `feature/brewing-phase2` vollständig. **`main` kann in einem Schritt nachziehen** (Fast-Forward oder ein
  Merge-Commit ohne Konflikte): `git checkout main && git merge --ff-only origin/feature/tea-loop`.
- Die anderen Feature-Branches danach nicht mehr einzeln mergen (sie sind enthalten).
- `Packages/manifest.json` bekommt `com.unity.test-framework` (EditMode-Tests). Nach dem Merge in Unity einmal die
  Builder laufen lassen (Testplan 1) – Szenen entstehen nur per Builder; neu in den Build Settings:
  `BrewingSandbox`.
- `.gitignore`: Ausnahmen für die `.csproj` von `SaveGame.DotNet` und `TeaLoop.DotNet` sowie deren Testdateien.

