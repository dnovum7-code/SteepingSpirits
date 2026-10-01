# Was aus EverdawnTest übernommen wurde

Quelle: `dnovum7-code/EverdawnTest` (Stand: Commit `f822d1a`). Namespace überall
`Everdawn.*` → `SteepingSpirits.*`, Menüs `Everdawn/…` → `SteepingSpirits/…`.

## 1:1 übernommen (nur Namespace)

| Bereich | Skripte |
|---|---|
| Quest-Kern | `QuestInstance`, `ObjectiveData`, `Reward`, `QuestState`, `QuestCategory`, `QuestStepType` |
| Quest-Events | `GameplayEvents`, `QuestEvents` |
| Quest-Steps | `QuestStepBase`, `QuestStepFactory`, `CollectStep`, `DefeatStep`, `VisitStep`, `BringStep`, `TalkStep`, `QuestTarget`, `QuestTargetLink`, `QuestTargetRegistry` |
| Quest-UI | `QuestUI`, `QuestLogView`, `QuestTracker`, `QuestPopup` |
| Inventar | `Inventory`, `ItemStack`, `ItemCategory`, `InventoryEvents`, `PlayerInventory`, `InventoryPickupBridge`, `QuestRewardCollector`, `CollectQuestInventoryTracker` |
| Wirtschaft / XP | `Wallet`, `GoldRewardCollector`, `PlayerProgression`, `XpRewardCollector` |
| Sonstiges | `QuestAudioHooks`, `ProceduralSfx` (→ `Core/Audio`), `GuiDraggablePanel` (→ `Core/UI`), Beispiel-JSONs, Audio-README |

## Übernommen und angepasst

| Everdawn | Steeping Spirits | Änderung |
|---|---|---|
| `QuestData` | `QuestData` | `startLocation` jetzt `Vector2`; `aiContinuation` entfernt |
| `QuestManager` | `QuestManager` | KI-Teile raus (`AIQuestClient`, `RequestQuestFromAI`, Text-Format); neu: `questJsonFiles`, JSON aus Resources, `ResetQuest()` |
| `QuestJsonValidator`, `QuestDefinition`, `QuestGeneratorRuntime` | `Quests/Data/`: `QuestJsonValidator`, `QuestDefinition`, `QuestJsonLoader` | Aus dem KI-Ordner gelöst – JSON ist jetzt nur Autoren-Format für handgeschriebene Quests; `countExistingInventory` im JSON möglich |
| `QuestGeneratorEditor` („Quest Generator") | `QuestJsonImporterWindow` („Quest JSON Importer") | Nur noch JSON, kein key:value-Textformat |
| `QuestNpc` | `QuestNpc` | 2D: Sprite spiegeln statt drehen, Namensschild in Welt-Einheiten; Lore-/KI-Hooks (Layer 1/2) entfernt; Dialog über `DialogueHUD` |
| `QuestItemPickup`, `ItemPickup`, `QuestLocationTrigger` | gleich | `Collider2D` / `OnTriggerEnter2D`; `QuestLocationTrigger.fireOnce` standardmässig aus |
| `QuestEnemyReporter` | gleich | nutzt `SteepingSpirits.Combat.Health` |
| `QuestWorldMarker` | gleich | `IInteractable` + Prompt-Text |
| `IQuestInteractable` | `Interaction/IInteractable` | umbenannt – gilt für alles Interaktive (Schilder, Bett, …) |
| `QuestInteractor` (Raycast aus der Kamera) | `Interaction/Interactor2D` | Kreis um den Spieler, Blickrichtung bevorzugt, Taste **E** |
| `QuestDialogueHUD` | `Dialogue/DialogueHUD` | Schreibmaschinen-Text, Portrait, pausiert die Welt |
| `QuestUiFactory` | `Core/UI/UiFactory` | warme „Holz"-Farbpalette |
| `QuestJournalCanvas`, `QuestTrackerHUD`, `QuestToastHUD`, `QuestMarkerHUD` | gleich | Tasten über `GameInput`, Journal pausiert die Welt, Layout an das 2D-HUD angepasst |
| `InventoryHUD` | gleich | Tasten über `GameInput`, Position unter der Uhr |
| `ItemData` | gleich | neu: `healAmount`, `tint` |
| `InventoryDemoManager` (Trank heilt) | `Inventory/Integration/ItemUseEffects` | als echte Brücke statt Demo |
| `Health` | `Combat/Health` | Rückstoss-Richtung, i-Frames, `Revive`, `SetMaxHealth`, Events `Damaged/Healed/Revived` |
| `WeaponData` | `Combat/WeaponData` | 2D-Werte (Reichweite vor dem Spieler, Trefferradius, Rückstoss) |
| `PlayerAttack` + `WalkSwordAnimation` | `Combat/PlayerAttack2D` | Hieb in Blickrichtung, Platzhalter-Hieb-Grafik, Sounds |
| `SimpleEnemyAI` | `Combat/EnemyAI2D` | Umherstreifen, Verfolgen, Ausholen, Kontaktschaden |
| `FirstPersonController` | `Player/PlayerController2D` | Top-Down, 4-Wege-Blickrichtung, Animator-Parameter |
| `CameraControll` | `Player/CameraFollow2D` | orthografisch, weich, begrenzbar, Y-Sortierung |
| `DayNightCycle` + `SkyBoxManager` | `World/GameClock` + `World/DayNightTint` | Stardew-Uhr mit Tagen/Jahreszeiten + Farb-Overlay statt Sonne/Skybox |
| `DevCheatWindow` | gleich | ohne KI-Tab-Inhalte; Fliegen/Luftsprung → Tempo, One-Hit, Herzcontainer, Uhr-Steuerung |
| `QuestTestArena` | `DevTools/TestMeadow` | komplette 2D-Testwiese |
| `IntegrationSelfTest` | gleich | + JSON, Daily-Reset, i-Frames (28 Checks) |
| `Tests/quest_integration_logic_test.py` | `Tests/logic_test.py` | + Uhr, Kalender, Daily, Health, Tag/Nacht (48 Checks) |

## Neu dazugekommen

`GameInput`, `GamePause`, `PlayerLocator`, `PlaceholderSprites`, `GuiDraw`,
`Knockback2D`, `DamageFlash2D`, `PlayerRespawn2D`, `InteractableDialogue`,
`Bed`, `GameHUD`, `DailyQuestReset`.

## Bewusst NICHT übernommen

| Was | Warum |
|---|---|
| Kompletter KI-Teil: `AIQuestClient`, `OllamaQuestClient`, `QuestTextParser`, `AI/Pipeline/*` (`QuestAiLauncher`, `QuestAiRunner`, `QuestPromptBuilder`, `QuestResponseValidator`, `QuestWorldCatalog`) | Wunsch: Quest-System ohne KI-Generierung |
| `AiQuestTestbed`, `AiQuestSandbox`, `AiQuestVillage` | KI-Testfelder |
| Doku zu KI (`KI-Verbindung…`, `Lokale-AI-testen`, `Team-Setup-KI-Quests`) | KI-spezifisch |
| `HorseController`, `HorseMount` | 3D-Reittier – in 2D später neu, falls gewünscht |
| `CursorFreeLook`, `CameraLookTuning`, `StarterAssets`-Bezüge | First-Person-spezifisch |
| `QuestDemoManager`, `DemoSpin`, `InventoryDemoManager` | ersetzt durch `TestMeadow` / `ItemUseEffects` |
| `Assets/Settings/` (URP-3D-Assets) | 3D-Renderer; das 2D-Projekt läuft ohne Pipeline-Assets |
