# Smoke-Test Jump'n'Run

> **Noch nicht in Unity gelaufen.** Diese Datei wird von *SteepingSpirits → JumpNRun → Run Smoke Test*
> überschrieben (alle Level bauen, statisch prüfen, im Play-Modus vom Bot mit echten Eingaben spielen lassen).

## Vorab-Ergebnis ohne Unity (Kachel-Simulation, `dotnet test Tests/PlatformingCore.DotNet`)

Derselbe Bot (`RoutePlanner` + `RouteFollower`) spielt die Level in `TileWorldSim` – Kollision gegen die
Kacheln, echter `PlayerMotor`, Wind, Tau, Schaukeln, Laternen, Dornen. Blätter sinken dort nicht,
Geisterplattformen gelten als beleuchtet.

| Level | Ergebnis | Spielzeit (Bot) | Auffangen | Weg |
|---|---|---|---|---|
| Level1 Morgenwiese | ✅ Ziel erreicht | 13,5 s | 0 | Hauptweg |
| Level2 Geisterhain | ✅ Ziel erreicht | 19,0 s | 0 | unten über die Schaukel |
| Level2 ohne Schaukel | ✅ Ziel erreicht | – | – | oben mit dem Windgeist |
| Level3 Abendschaukel | ✅ Ziel erreicht | 21,6 s | 0 | Schaukeln, Geister-Trittsteine, Schaukel → Wind |
| Level4 Taunacht | ✅ Ziel erreicht | 16,8 s | 0 | Tau-Säulen, Geisterstufen |

Level-Lint (`LevelLint`): alle ausgelieferten Level ohne Warnung.
