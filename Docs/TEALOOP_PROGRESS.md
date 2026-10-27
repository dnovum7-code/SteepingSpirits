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
| B4 | Gemeinsamer Speicherdienst (Inventar + Uhr) | offen |
| B5 | Builder zerstören sich nicht mehr gegenseitig | offen |
| B6 | Tastentabelle, Kollisionen | offen |
| C7 | Erste Teestrauch-Ernte = `tea_leaf_first` | offen |
| D8 | `ForProcessedTea` + `ProcessedBrewParams` | offen |
| D9 | Teewahl aus Inventar/Vorratsschrank | offen |
| D10 | Ergebnis: Noten, `tea_tart` | offen |
| D11 | Brühen aus der Welt erreichbar | offen |
| E12 | Simulator-Auswertung | offen |
| F13 | Abschlussbericht | offen |

## Annahmen

_(werden ergänzt)_

## Editor-Checklisten pro Commit

- **A1** (Merge): Unity öffnen → Konsole ohne Fehler. Menü *SteepingSpirits → Brewing → Build Sandbox* ist da und
  baut die Sandbox; Play → Brühen wie bisher. Test Runner → EditMode: alle Module grün.
- **A3** (nur Doku): `CLAUDE.md` im Repo-Wurzelverzeichnis lesen.
- **A2** (nur Doku): `Docs/PLAYTEST_NOTES.md` hat die Abschnitte Werkstatt, Brühen, Garten, Jump'n'Run mit Vorlage.
