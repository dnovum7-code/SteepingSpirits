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
| C7 | Erste Teestrauch-Ernte = `tea_leaf_first` | erledigt (Garten 109 Tests; Unity: ungeprüft) |
| D8 | `ForProcessedTea` + `ProcessedBrewParams` | erledigt (Brühen 90 Tests) |
| D9 | Teewahl aus Inventar/Vorratsschrank | erledigt (Brühen 92, TeaLoop 6 Tests; Unity: ungeprüft) |
| D10 | Ergebnis: Noten, `tea_tart` | erledigt (TeaLoop 7 Tests; Unity: ungeprüft) |
| D11 | Brühen aus der Welt erreichbar | erledigt (TeaLoop 8 Tests; Unity: ungeprüft) |
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
