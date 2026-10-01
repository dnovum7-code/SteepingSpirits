# Eigene Quest-Sounds (QuestAudioHooks)

Lege hier eigene AudioClips ab, um die Code-Töne zu ersetzen. `QuestAudioHooks`
lädt sie beim Start automatisch **nach Dateiname** – ist eine passende Datei da,
wird sie genommen, sonst der Code-Ton (bzw. gar nichts, wenn
`useProceduralFallback` aus ist).

## Wichtig: Resources-Ordner
Dieser Ordner **muss** unter einem Ordner namens `Resources` liegen, sonst
findet Unity die Dateien zur Laufzeit nicht:

```
Assets/_Project/Audio/Resources/QuestSfx/
```

Der Unterordner-Name (`QuestSfx`) ist im Feld **Audio Folder** von
`QuestAudioHooks` einstellbar.

## Dateinamen (ohne Endung)
Erlaubte Formate: `.wav`, `.ogg`, `.mp3`, `.aif`.

| Dateiname          | Wird gespielt bei …                    |
|--------------------|----------------------------------------|
| `item_pickup`      | Item aufgesammelt                      |
| `quest_start`      | Quest angenommen                       |
| `quest_progress`   | Fortschritt an einem Ziel              |
| `quest_objective`  | Ein Ziel abgeschlossen                 |
| `quest_complete`   | Quest abgeschlossen                    |
| `quest_fail`       | Quest fehlgeschlagen                   |

Beispiel: `Assets/_Project/Audio/Resources/QuestSfx/quest_complete.wav`

Nicht alle Dateien nötig – für jeden fehlenden Namen greift der Fallback.
