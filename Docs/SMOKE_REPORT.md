# Smoke-Test Jump'n'Run

> **Noch nicht in Unity gelaufen.** Diese Datei wird von *SteepingSpirits → JumpNRun → Run Smoke Test*
> überschrieben (alle Level bauen, statisch prüfen, im Play-Modus vom Bot spielen lassen).

## Vorab-Ergebnis ohne Unity (Kachel-Simulation, `dotnet test Tests/PlatformingCore.DotNet`)

Derselbe Bot (`RouteFollower`) spielt die Level in `TileWorldSim` – Kollision gegen die Kacheln,
echter `PlayerMotor`, Wind, Tau, Schaukel, Laternen, Dornen. Blätter sinken dort nicht, Geisterplattformen
gelten als beleuchtet.

| Level | Ergebnis | Spielzeit | Auffangen |
|---|---|---|---|
| Level1 Morgenwiese | ✅ Ziel erreicht | 13,5 s | 0 |
| Level2 Geisterhain (über die Schaukel) | ✅ Ziel erreicht | 18,6 s | 0 |
| Level2 ohne Schaukel (oberer Weg, Windgeist) | ✅ Ziel erreicht | – | – |
