# Brüh-Prototyp (Phase 1) – Alltagsaufguss

Ein spielbarer Aufguss von Anfang bis Ende zum Testen von Gefühl und Tuning.
Platzhalter-Grafik und -Ton, keine Fehlschläge.

## Starten

1. Unity öffnen (das Test Framework wird über `Packages/manifest.json` geladen).
2. Menü **SteepingSpirits → Brewing → Build Sandbox**.
   Legt an (falls nicht vorhanden) bzw. erzeugt neu:
   - `Assets/_Project/Brewing/Data/`: `Tea_Black`, `Tea_White`, `Tea_Green`, `BrewingTuning`, `BrewAudioSet`
     (vorhandene Assets bleiben erhalten – dein Tuning überlebt einen Neuaufbau)
   - `Assets/_Project/Brewing/Scenes/BrewingSandbox.unity` (wird jedes Mal neu erzeugt)
3. **Play**.

## Steuerung

| Aktion | Tastatur | Gamepad | Wann |
|---|---|---|---|
| Teesorte wählen | 1 / 2 / 3 | Steuerkreuz ← ↑ → | Teewahl, Ergebnis |
| Feuer an/aus | F | X | Wasser erhitzen |
| Zurückschöpfen | S | Y | Wasser erhitzen |
| Gefäß vorwärmen (2 s) | W | LB | vor dem Aufgießen |
| Aufgießen | Leertaste | A | Wasser erhitzen |
| Blätter herausheben | Leertaste | A | Ziehen |
| Thermometer an/aus | T | – | immer |
| Debug-Overlay | F1 | – | immer |
| Neu starten (frisches Wasser) | R | Start | immer |

## Das Wasser lesen (ohne Thermometer)

| Stufe | °C | Blasen | Klang (Platzhalter-Rauschen) | Dampf |
|---|---|---|---|---|
| Still | < 70 | keine | sehr leise, dumpf | kaum |
| Garnelenaugen | 70–75 | winzig, nur am Boden | leises Knistern | kaum |
| Krabbenaugen | 75–80 | etwas größer | feines Zischen | dünne Fäden |
| Fischaugen | 80–85 | perlengroß, steigen höher | Rauschen | Schwaden |
| Perlenschnur | 85–95 | Ketten an den Wänden | gleichmäßiges Summen | kräftig |
| Tosende Wellen | 95–100 | überall, schnell | helles Brodeln | dicht |

Innerhalb einer Stufe gleiten Bild und Ton weich in die nächste über (`BoilLook`).
Mit Gamepad rumpelt es bei jedem Stufenwechsel sanft.

## Architektur

```
Assets/_Project/Brewing/
  Runtime/Core/          asmdef SteepingSpirits.Brewing.Core (noEngineReferences)
    WaterParams, BoilStage(+Thresholds), WaterModel
    BrewParameters (TeaParams, ExtractionParams, PourParams, QualityParams,
                    SessionParams, BrewConfig, TeaPresets)
    LeafState, ExtractionModel, QualityTier(+Evaluator), BrewCalibration
    BrewSession (Zustandsmaschine), BrewResult, BrewHint/BrewAdvisor
  Runtime/Data/          TeaDefinition, BrewingTuning, BrewAudioSet (ScriptableObjects)
  Runtime/Flow/          BrewInput (Adapter), BrewSessionController
  Runtime/Presentation/  IBrewParticles + SpriteParticleEmitter, BoilLook, KettleView,
                         BubbleStageView, SteamView, SteepVesselView, AromaWispView,
                         FilteredNoiseSource, BrewAudioController, BrewHaptics,
                         BrewPromptView, ThermometerView, BrewResultView,
                         BrewDebugOverlay, PlaceholderSpriteShape, BrewTexts
  Runtime/Telemetry/     BrewTelemetry
  Editor/                BrewingSandboxBuilder
  Tests/EditMode/        Water-, Extraction-, QualityTier-, BrewSession-Tests
Tests/BrewingCore.DotNet/  dieselben Tests mit dotnet (außerhalb von Assets/)
```

- **Core** ist reines C# ohne UnityEngine. Die Parameter-Klassen sind `[Serializable]` und
  liegen direkt in den ScriptableObjects – Änderungen im Inspector wirken im Play-Modus
  sofort. Nach Änderungen an Extraktionswerten im Kontextmenü des `BrewSessionController`
  **Recalibrate** wählen (Qref neu berechnen).
- **Datenfluss:** Eingabe → `BrewInput` → `BrewSessionController` → Befehle an `BrewSession`
  + `Tick(dt)` → Zustand/Events → Views, Audio, Haptik, Overlay, Telemetrie.
- **Partikel** laufen über `IBrewParticles` (2 Methoden). ParticleSystem oder VFX Graph
  brauchen nur eine eigene Implementierung.
- **Ton:** Fehlt ein Stufen-Clip im `BrewAudioSet`, übernimmt tiefpassgefiltertes
  Rauschen (`FilteredNoiseSource`, keine Allokationen im Audio-Thread, weiche Übergänge).
  Fehlende Lift-/Pour-/Glocken-Clips stören nicht (Glocke: leiser Code-Klang als Ersatz).

## Startwerte – Änderungen gegenüber dem Auftrag

| Sorte | geändert | Grund (gemessen bei Fenstermitte) |
|---|---|---|
| Schwarztee | tB 9 → **7,5**, kB 0,03 → **0,04** | Vorher Vollendet 7,3–12,5 s / Harmonisch 4,3–16,5 s, zu breit. Jetzt 6,5–9,8 s / 4,0–13,0 s (Ziel 7–10 / 4–13). |
| Weißtee | kA 0,15 → **0,16**, tB 14 → **12**, kB 0,03 → **0,035** | Vorher zu langsam (Vollendet 14–19,7 s). Jetzt 12,0–16,0 s / 8,0–19,8 s – weiterhin der verzeihendste Tee (Ziel 12–15 / 8–18). |
| Grüntee | – | Passt: 5,3–6,5 s / 3,8–7,8 s; mit kochendem Wasser beste Harmonie nur 0,36, nach ~2 s herb. |

Kalibrierung: Qref = bester Q bei Aufguss in Fenstermitte (wie spezifiziert). H wird auf
0..1 begrenzt – ein Aufguss am Fensterrand darf die Referenz leicht übertreffen und ist
dann einfach „Vollendet“. Den aktuellen Stand gibt der Test `PrintTuningReport` aus.

## Die wichtigsten Regler pro Sorte (`TeaDefinition.parameters`)

| Sorte | 1. | 2. | 3. |
|---|---|---|---|
| Schwarztee | `bitterOnsetSeconds` – wann es kippt (Länge des guten Fensters) | `aromaRate` – wie schnell er „fertig“ ist | `idealMin` – wie heiß er sein muss |
| Weißtee | `aromaRate` – Gesamtlänge (scheu/langsam) | `bitterRate` – wie verzeihend nach dem Optimum | `heatSensitivity` – Strafe für zu heißes Wasser |
| Grüntee | `heatSensitivity` – wie sehr kochendes Wasser schadet | `bitterOnsetSeconds` – Breite des schmalen Fensters | `idealMax` – Obergrenze der richtigen Blasen |

Global (`BrewingTuning.simulation`): `quality.bitterWeight`, Stufenschwellen, `pour.lossCold/lossPrewarmed`.

## Telemetrie

Pro Aufguss eine Zeile in `Application.persistentDataPath/brew_log.jsonl`:
Zeitstempel, Sorte, Aufguss-Index, Kessel- und Ziehstarttemperatur, vorgewärmt, altes Wasser,
Ziehzeit, A, B, H, Stufe, herb, Gesamtdauer, Thermometer benutzt.

## Tests

- **Unity:** Window → General → Test Runner → EditMode → Run All (50 Tests).
- **Ohne Unity:** `dotnet test Tests/BrewingCore.DotNet`.

## Bekannte Grenzen

- Platzhalter: Grafik aus Rechtecken/Kreisen, Ton als gefiltertes Rauschen. Der Ton ist eher
  „Wasser-ähnlich“ als schön – echte Clips im `BrewAudioSet` ersetzen ihn pro Stufe.
- Kein Wasservolumen: Aufgießen leert den Kessel nicht.
- Ein erster Aufguss aus kaltem Wasser dauert ~25–40 s, mit heißem Kessel kürzer.
- Die Darstellung wurde nur kompiliert, nicht im Editor gespielt (keine Unity-CLI in der
  Entwicklungsumgebung).
