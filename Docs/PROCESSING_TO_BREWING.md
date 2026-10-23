# Vorschlag: Werkstatt-Tee → Brühen

Stand: Werkstatt auf `feature/processing`, Brühen auf `feature/brewing-phase2`. **Der Brüh-Code bleibt bis
zum Merge unverändert**; dieses Dokument beschreibt, wie die Anbindung danach aussehen könnte.

## Was die Werkstatt liefert

Jeder fertige Tee ist eine Zutat mit sprechender ID (siehe `ProcessedTea`):

```
tea_oolong:dark/honeyed.roasty+q3
└ Katalog-ID  └ Stil  └ Noten (sortiert)  └ Qualität (IngredientQuality, Stufe 1 ohne Endung)
```

- `ProcessedTea.TryParseId(id, out variety, out notes, out tier)` liest Sorte, Noten und Stufe.
- Die **genaue Oxidation** steht nicht in der ID, sondern im Tee-Register (`workshop_save.json`, Abschnitt
  `ledger`, Mittelwert je ID). Fehlt ein Eintrag, nimmt man das Optimum der Sorte (Weiß/Grün 2 %,
  heller Oolong 20 %, dunkler 50 %, Schwarztee 92 %).
- Sorte → bestehende `TeaPresets` des Brühens: `White()`, `Green()`, `Oolong()` (beide Oolong-Stile),
  `Black()`.

## Idee: Basis-Preset + kleine, nachvollziehbare Verschiebungen

Eine reine Funktion im Brüh-Core, z. B.

```csharp
public static TeaParams ForProcessedTea(TeaParams preset, TeaVariety variety, float oxidation,
                                        int tier, IReadOnlyList<TeaNote> notes, ProcessedBrewParams k)
```

liefert eine angepasste Kopie der `TeaParams`. Alle Faktoren in einer `[Serializable]`-Klasse
`ProcessedBrewParams` im `BrewingTuning`-Asset. Jede Verschiebung ist klein, damit das Brüh-Gefühl der
vier Grundtees erhalten bleibt.

### 1. Oxidation verschiebt das Temperaturfenster (Oolong)

Dunklerer Oolong verträgt heißeres Wasser, hellerer will es sanfter:

| Oxidation | Fenster (Vorschlag) | Begründung |
|---|---|---|
| 10 % (sehr hell) | 80–90 °C | nah am Grüntee, blumig, empfindlich |
| 20 % (heller Oolong, Optimum) | 83–93 °C | |
| 50 % (dunkler Oolong, Optimum) | 88–98 °C | entspricht etwa dem heutigen Oolong-Preset +3 °C |
| 70 % | 90–100 °C | nah am Schwarztee |

Formel: `shift = k.oolongWindowPerOxidation × (oxidation − 0.35)` mit `k.oolongWindowPerOxidation ≈ 16 °C`,
auf `idealMin`/`idealMax` addiert; zusätzlich `heatSensitivity ×= lerp(1.2, 0.8, (oxidation−0.1)/0.6)`.
Weiß-, Grün- und Schwarztee: Oxidation ändert das Fenster nicht (ihr Band ist schmal), nur Schwarztee unter
85 % bekommt `bitterOnsetSeconds −0.5 s` (noch etwas „grüner“).

### 2. Qualität = mehr Substanz und mehr Nachsicht

| Stufe | `leafCapacity` | `bitterTolerance` | `aromaRate` |
|---|---|---|---|
| 1 gut | × 0,9 | ± 0 | × 0,95 |
| 2 fein | × 1,0 | + 0,03 | × 1,0 |
| 3 erlesen | × 1,15 | + 0,06 | × 1,05 |

Bessere Blätter geben mehr Aroma her, mehr Aufgüsse (über die Kapazität) und verzeihen etwas mehr Bitterkeit.
Die vier Brüh-Qualitätsstufen (Fad … Vollendet) bleiben die Bewertung der *Tasse*.

### 3. Noten = Charakter beim Brühen

| Note | Wirkung (Vorschlag) | Warum |
|---|---|---|
| frisch | Fenster −2 °C, `heatSensitivity` × 1,15 | grüner, empfindlicher |
| blumig | erster Aufguss: `aromaMaxFactor` × 1,1 | duftet sofort |
| honigsüß | `bitterRate` × 0,9 | runder, weniger herb |
| malzig | `bitterTolerance` + 0,04 | kräftig, verträgt längeres Ziehen |
| röstig | Fenster +2 °C, `heatSensitivity` × 0,85, `bitterOnsetSeconds` + 1 s | geröstete Tees mögen es heiß |
| zart | `leafSaturation` × 1,15 (ein Aufguss mehr), `heatSensitivity` × 1,1 | erster Austrieb, viele feine Aufgüsse |

Mehrere Noten wirken nacheinander (multiplikativ / additiv), gedeckelt, z. B. Fenster ±4 °C insgesamt.

## Ablauf im Spiel nach dem Merge

1. Teewahl im Brühen listet **Tees aus Inventar und Vorratsschrank** (Familie `Tea` im Katalog) statt der drei
   festen `TeaDefinition`-Assets; die Assets bleiben als Basis-Presets und für Farben.
2. Gewählter Tee → `ForProcessedTea(...)` → Brühen wie bisher. Ein Aufguss verbraucht eine Portion.
3. Ergebnistext kann die Noten nennen („röstig, honigsüß“) – Grundlage dafür, dass Geister später den
   Unterschied schmecken.
4. Eine Tasse, die „herb“ wird, gibt das Item `tea_tart` aus → Komposter im Teegarten (Merge-Hinweis Garten).

## Tests, die beim Merge dazukommen sollten

- `ForProcessedTea` mit leerer Noten-Liste und Stufe 2 liefert das Preset unverändert (Rückwärtskompatibel).
- Dunklerer Oolong → höheres Fenster (monoton in der Oxidation).
- Verschiebungen sind gedeckelt (keine Kombination verschiebt das Fenster um mehr als ±4 °C).
- Kalibrierung (`BrewCalibration`, Qref) läuft auf den angepassten Parametern weiter, damit „Vollendet“
  erreichbar bleibt.
