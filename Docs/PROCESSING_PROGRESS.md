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

## Annahmen

_(werden ergänzt)_

## Backlog

| # | Punkt | Status |
|---|---|---|
| A1 | Blattcharge und reine Übergänge | offen |
| A2 | Sortenbestimmung | offen |
| A3 | Qualität | offen |
| A4 | Charakternoten | offen |
| A5 | Zeit (`IGameTime`, Vorspulen) | offen |
| B6 | ProcessedTea-Datenvertrag + Katalog | offen |
| B7 | Übergabe Inventar/Vorratsschrank | offen |
| B8 | `Docs/PROCESSING_TO_BREWING.md` | offen |
| C9 | Welkgestell | offen |
| C10 | Rolltisch | offen |
| C11 | Oxidationskorb | offen |
| C12 | Wok | offen |
| C13 | Trockenkorb | offen |
| C14 | Vorschau an jeder Station | offen |
| D15 | Werkstatt-Szene per Builder | offen |
| D16 | Blatt-Darstellung | offen |
| D17 | Klang und Partikel, `ProcessingAudioSet` | offen |
| D18 | Oberfläche (UI-Baukasten, Gamepad) | offen |
| D19 | Eingang | offen |
| E20 | Speichern `workshop_save.json` | offen |
| E21 | Debug-Overlay F1 | offen |
| E22 | Telemetrie `processing_log.jsonl` | offen |
| F23 | Kräuter trocknen | offen |
| F24 | Mischtisch | offen |
| F25 | Teeziegel (Pu-Erh-Vorbereitung) | offen |
| F26 | Werkstattgeist | offen |
| F27 | Eigene Verbesserungen | offen |

## Editor-Checklisten pro Commit

_(wird pro Commit ergänzt)_
