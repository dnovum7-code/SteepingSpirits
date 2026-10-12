# Teegarten – Fortschritt (Langlauf)

> **Neue Sitzung?** Zuerst diese Datei lesen, dann `Docs/PLAYTEST_NOTES.md` (Teegarten-Notizen haben
> Vorrang), dann beim ersten Backlog-Punkt mit Status „offen“ weitermachen.
> Branch `feature/plantation` (Worktree `../SteepingSpirits-garden`). Nichts nach `main`, kein Force-Push.

## Nächster konkreter Schritt

→ erster Backlog-Punkt mit Status „offen“.

## Basis und Merge-Hinweis

- `main` (5629997) enthält den Zutaten-Katalog `SteepingSpirits.Ingredients` **nicht**. Deshalb ist
  `feature/plantation` von **`feature/jumpnrun` (795c5f4)** abgezweigt. Beim Merge zuerst `feature/jumpnrun`,
  dann `feature/plantation` nach `main` bringen.
- `CLAUDE.md` gibt es im Repo noch nicht; die Konventionen aus dem Auftrag gelten (Vorschläge für die
  Datei stehen im Abschlussbericht).
- `Docs/PLAYTEST_NOTES.md` existiert nicht (Stand Start).

## Erkundung

- **Perspektive:** Die Welt außerhalb des Jump'n'Run ist **Top-down** (Zelda/Stardew). `PlayerController2D`
  bewegt die Figur frei (4,5 Einheiten/s) mit Blickrichtung in 4 Richtungen (`Facing`). Die Testwiese baut
  Spieler, Kamera (`CameraFollow2D`) und Systeme zur Laufzeit im Code auf.
- **Inventar:** `PlayerInventory` (DontDestroyOnLoad, Katalog ID → `ItemData`, `RegisterItem` zur Laufzeit,
  `AddById`, `Remove`, `Count`, eigenes `inventory_save.json`). Die Testwiese legt es an.
- **Zutaten-Katalog:** `SteepingSpirits.Ingredients` (engine-frei): IDs, Seltenheit, Familie, deutsche Namen;
  `Pantry` = Vorratsschrank `pantry.json` (versioniert) mit kleinem JSON-Leser `MiniJson`.
- **Übergabe:** `IngredientHandover` (Jump'n'Run-Modul) legt Zutaten ins laufende Inventar, sonst in den
  Vorratsschrank. Kennt keine Qualität.
- **Spielstände:** keine gemeinsame Speicherung; jedes Modul hat ein eigenes, versioniertes JSON
  (`jumpnrun_save.json`, `pantry.json`, `inventory_save.json`).
- **Tag:** `GameClock` (World-Modul, DontDestroyOnLoad): Tag, Jahreszeit (28 Tage), Uhrzeit, Event
  `OnNewDay`, `AdvanceToNextDay()`; `Bed` springt per Dialog zum nächsten Morgen. Kein Wetter.

## Annahmen

1. **Raster:** 1 Feld = 1 Unity-Einheit, Top-down. Der Core arbeitet nur mit Rasterkoordinaten.
2. **Tag:** Der Garten nutzt `GameClock` über eine schmale Schnittstelle `IDayClock` (Adapter im Modul).
   Läuft keine `GameClock` (Szene direkt gestartet), legt der Garten eine an – wie die Testwiese es tut.
   Verpasste Tage (Schlafen im Bett auf der Wiese) holt der Garten beim nächsten Betreten nach; sie zählen
   als ungegossene Tage (also nur Wachstumsverlust, nie Tod).
3. **Wetter:** gibt es noch nicht → deterministisches Regen-Orakel im Core (Seed + Tag, Startwert 15 %).
4. **Qualität:** drei Stufen 1 „gut“, 2 „fein“, 3 „erlesen“. Weil die bestehende Übergabe (Jump'n'Run-Code,
   nur lesen) keine Qualität kennt, bekommt der Garten einen eigenen kleinen Adapter mit demselben Verhalten
   (Inventar, sonst Vorratsschrank). Die Qualität steckt in der Zutaten-ID-Variante `<id>+q<n>`
   (Katalog-Erweiterung `IngredientQuality`, additiv); Stufe 1 benutzt die normale ID.
5. **Saatgut** sind Inventar-Gegenstände `seed_<pflanze>` (Kategorie Material), keine Zutaten.
6. **Benutzen-Taste:** Leertaste / J / linke Maustaste / Pad X (wie Angriff auf der Wiese – im Garten gibt
   es keinen Kampf). Brunnen und Laterne werden mit derselben Taste benutzt (Feld vor der Figur).

## Regeln (Core, `Garden.cs`)

- **Nacht** (`Garden.EndDay`): Für jede Pflanze zählt, ob ihr Feld heute nass war (gegossen oder Regen).
  Danach trocknet jede Erde, der Tag springt weiter, und morgens regnet es eventuell (`IWeather`; Regen
  macht jede gehackte Erde nass).
- **Wasser-Typen:** *durstig* wächst nur an nassen Tagen. *normal* wächst an nassen Tagen; der erste
  trockene Tag pro Stufe wird verziehen (wächst trotzdem), jeder weitere wartet. *genügsam* wächst immer.
- **Pflegepunkte pro Tag** (nur solange unreif): nass = 2; *normal* verziehen = 1; *genügsam* trocken,
  aber gestern nass = 2; sonst 0. Quote = Punkte / (2 · Tage) im aktuellen Zyklus (seit Aussaat bzw. letzter
  Ernte). **erlesen** ab 0,9, **fein** ab 0,6, sonst **gut**. Reife Pflanzen warten ohne Malus.
- **Boni** nur auf die erste Ernte einer Aussaat: Teestrauch „erster Austrieb“ +1 Stufe; Kompost +1 Stufe
  und +1 Stück.
- **Ertrag** und **Saatgut-Fund**: deterministisch aus Seed, Tag, Feld und Ernte-Nummer.
- **Nie Tod:** Vernachlässigung kostet nur Wachstumstage und Qualität (getestet über 100 Tage).
- Gehackte, leere Erde wird nach 3 Nächten wieder unberührt. Hacken an Regentagen ergibt nasse Erde.
- Verpasste Nächte werden nachgeholt (trocken), höchstens 56 auf einmal.
- A2–A5 sind ein gemeinsamer Commit, weil Tageswechsel, Wachstum, Wasser-Typen und Qualität dieselbe
  Nachtverarbeitung sind.

## Startpflanzen (Startwerte, `StarterPlants.cs` → PlantDefinition-Assets)

| Pflanze | Zutat | Wasser | Art | Tage bis reif (Stufen) | Nachwachsen | Ertrag | Besonderes |
|---|---|---|---|---|---|---|---|
| Teestrauch | `tea_leaf` | normal | mehrjährig | 14 (3/3/4/4) | 3 | 2–3 | erste Ernte „erster Austrieb“ +1 Qualität |
| Minze | `mint` | durstig | einjährig | 4 (1/2/1) | – | 2–3 | Saatgut-Fund 60 % |
| Kamille | `chamomile` | normal | einjährig | 7 (2/2/3) | – | 2–4 | Saatgut-Fund 60 % |
| Jasmin | `jasmine` | durstig | mehrjährig | 16 (3/4/4/5) | 5 | 1–2 | wertvoll |
| Lavendel | `lavender` | genügsam | mehrjährig | 10 (3/3/4) | 4 | 2–3 | verzeiht viel |
| Zitronenmelisse | `lemon_balm` | normal | mehrjährig | 8 (2/3/3) | 3 | 2–3 | zuverlässig |

**Katalog-Ergänzungen (additiv):** `mint`, `chamomile`, `jasmine`, `lavender`, `lemon_balm` (alle
„common“, Familien Kraut/Blüte) mit deutschen Namen/Beschreibungen, dazu `IngredientQuality` (Qualitäts-IDs
`<id>+q2`/`+q3`). Der bestehende Test „10 Zutaten“ zählt jetzt 15 – die einzige Änderung an einem
bestehenden Test.

## Simulator und Rundendauer (`GardenSimulator`, Test `GardenSimulatorTests`)

28 Tage im echten Layout `Layouts/Teegarten.txt` (48 Beetfelder in 6 Blöcken à 4×2, Startpaket aus der
Samenkiste, Block → Pflanze laut `@blocks`). Die Bots laufen über das Raster (BFS, 4,5 Felder/s) und
stellen sich neben das Zielfeld; jede Aktion kostet 0,35 s, Werkzeugwechsel 0,15 s, Nachfüllen 0,8 s,
Tag beenden 1,5 s.

| Bot | Ernten | Stück | gut/fein/erlesen | Ø Qualität | Gießen | Pflanzen verloren | Runde Ø / max |
|---|---|---|---|---|---|---|---|
| sorgfältig | 218 | 534 | 0/0/534 | 3,00 | 1024 | 0 | 48 s / 82 s |
| schlampig | 124 | 314 | 126/148/40 | 1,73 | 428 | 0 | 35 s / 65 s |
| nie gießen | 40 | 94 | 94/0/0 | 1,00 | 0 | 0 | 8 s / 65 s |
| sorgfältig + 1×3-Kanne | 218 | 534 | 0/0/534 | 3,00 | 1024 | 0 | 43 s / 74 s |

- Sorgfältig erntet am meisten und am besten, schlampig erntet trotzdem ~60 %, nichts stirbt.
- Ohne Gießen trägt nur der genügsame Lavendel (normale Pflanzen warten nach dem verziehenen Tag pro Stufe,
  Regen 15 % hilft langsam). Das ist gewollt: es zeigt den Wert der Wasser-Typen.
- Rundendauer: Bot ~1 Minute, ein Mensch braucht etwa doppelt so lang → **2–3 Minuten**, im Rahmen.
- Saatgut sorgfältig: 60 gefunden, 24 übrig, 42 von 48 Feldern am Ende bepflanzt (einjährige Felder
  schrumpfen langsam, wenn das Saatgut ausgeht – siehe G22/G25).

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Raster, Beetfelder, Aktionsgültigkeit (rein, getestet) | erledigt (44 Tests) |
| A2 | Tageswechsel (`IDayClock`, Trocknen, Regen) | erledigt |
| A3 | Wachstum (Stufen, Nachwachsen, Determinismus) | erledigt |
| A4 | Wasserbedarf-Typen | erledigt |
| A5 | Qualität | erledigt (A2–A5: 82 Tests gesamt) |
| B6 | PlantDefinition (ScriptableObject) | erledigt |
| B7 | Startpflanzen + Katalog-Ergänzungen | erledigt |
| C8 | Werkzeuge (Hacke, Saatbeutel, Kanne, Hand, Schaufel) | offen |
| C9 | Zielfeld mit Vorschau | offen |
| C10 | Werkzeugleiste und Steuerung | offen |
| C11 | Komfort (1×3-Kanne, Gartenhilfe) | offen |
| D12 | Erde-Darstellung | offen |
| D13 | Pflanzen-Darstellung | offen |
| D14 | Klang und Partikel, `PlantationAudioSet` | offen |
| E15 | Ernte → Inventar/Vorratsschrank mit Qualität | offen |
| E16 | Speichern `garden_save.json` | offen |
| E17 | Szene per Builder | offen |
| E18 | Eingang von der Wiese | offen |
| F19 | Debug-Overlay F1 + „Nächster Tag“ | offen |
| F20 | Telemetrie `garden_log.jsonl` | offen |
| G21 | Kompost | offen |
| G22 | Saatgut aus Ernte | offen |
| G23 | Regengeist | offen |
| G24 | Jahreszeiten-Daten | offen |
| G25 | Eigene Verbesserungen | offen |

## Editor-Checklisten pro Commit

- **A1** (Core-Grundlage): Unity öffnen → Konsole ohne Fehler; Test Runner → EditMode →
  `SteepingSpirits.Plantation.Tests.EditMode` grün. (Ohne Unity geprüft: `dotnet test Tests/PlantationCore.DotNet`.)
- **A2–A5** (Garten-Simulation): wie A1, Testklassen `GardenDayTests`, `GardenGrowthTests`,
  `GardenWaterTests`, `GardenQualityTests` grün. Noch nichts Sichtbares.
- **B7** (Startpflanzen, Katalog): Test Runner → `SteepingSpirits.Ingredients.Tests.EditMode` und
  `…Plantation.Tests.EditMode` grün. Im Jump'n'Run prüfen: das Seltenheitsregal zeigt weiter nur die 5
  seltenen Zutaten.
- **B6** (Daten-Assets): Konsole ohne Fehler. Rechtsklick im Project → Create → SteepingSpirits → Plantation →
  „Plant Definition“, „Garden Settings“, „Audio Set“ legen je ein Asset an; im Inspector sind alle Werte
  (Stufen-Tage, Wasser-Typ, Ertrag, Qualitätsgrenzen, Kanne, Regen, Komfort, Feedback) sichtbar.
