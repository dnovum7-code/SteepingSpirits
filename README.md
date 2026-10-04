# Steeping Spirits – 2D-Testprojekt

Ein 2D-Top-Down-Spiel als Mischung aus den **alten Zeldas** (Schwert, Herzen,
Rückstoss, Gegner auf der Wiese) und **Stardew Valley** (Uhr mit Tagen und
Jahreszeiten, Dorf-NPCs mit Aufträgen, tägliche Aufgaben, Schlafen im Bett).

Die Grundsysteme stammen aus **EverdawnTest** und wurden für 2D umgebaut:
Quest-System (**ohne** KI-Generierung), Inventar, Gold, XP, Kampf, Dialoge,
Questlog, HUDs, Sounds, Dev-Cheats und Selbsttest. Was genau übernommen,
angepasst oder weggelassen wurde: [Docs/EVERDAWN_UEBERNAHME.md](Docs/EVERDAWN_UEBERNAHME.md).

**Neu: Brüh-Prototyp** (Alltagsaufguss: Wasser lesen, aufgießen, ziehen lassen) → [Docs/BREWING.md](Docs/BREWING.md)

**Neu: Jump'n'Run-Szene** (Celeste-artig mit Dash, Wandsprung, Haltepunkten,
Schwingseilen und Lianen) → [Docs/JUMP_AND_RUN.md](Docs/JUMP_AND_RUN.md)

> Alles läuft mit **Platzhalter-Grafiken aus Code** – zum Ausprobieren musst du
> kein einziges Sprite importieren.

---

## Schnellstart (2 Minuten)

1. **Unity 6** verwenden (dieselbe Version wie bei Everdawn).
2. Repo klonen und im Unity Hub **„Add project from disk"** → diesen Ordner wählen.
   Unity legt beim ersten Öffnen `ProjectSettings/`, `Library/` und die `.meta`-Dateien an.
   - Fragt Unity, ob die *Input System Backends* aktiviert werden sollen → **Yes**.
     (Der Code läuft aber auch mit dem alten Input Manager.)
3. **`Assets/Scenes/TestMeadow.unity`** öffnen (oder: neue Szene → leeres
   GameObject → **Add Component → „Test Meadow"**).
4. **Play** drücken. Taste **N** blendet die Hilfe ein/aus.
5. Ganz links auf der Wiese führt ein lila **Portal** in die Jump'n'Run-Szene
   (`Assets/Scenes/JumpAndRun.unity`, auch direkt startbar).

Die Testwiese baut dann alles selbst auf: Spieler, Kamera, Wiese mit Teich,
Bäumen, Haus und Bett, 6 NPCs mit je einer Quest-Art, Schleime, HUD, Uhr,
Tag/Nacht und das Cheat-Fenster.

> Benutzt du die Vorlage **„Universal 2D" (URP)**: Sprites brauchen dort Licht.
> Die Testwiese legt automatisch ein *Global Light 2D* an, falls keins da ist.
> Für saubere Y-Sortierung im *Renderer 2D*-Asset „Transparency Sort Mode" =
> *Custom Axis* (0, 1, 0) setzen.

### Was du auf der Testwiese testen kannst

| NPC | Quest-Art | Was tun |
|---|---|---|
| **Oma Hilde** | Sammeln | 5 Teeblätter im Südwesten aufsammeln (wachsen nach) |
| **Finn** | Reden | Wächterin Ida im Nordosten Bescheid geben |
| **Postbote Bruno** | Bringen | Paket zu Mira (ganz im Osten) – Belohnung: Heiltrank |
| **Späherin Lia** | Besuchen | Zum alten Turm im Südwesten laufen |
| **Hauptmann Rolf** | Besiegen | 3 Schleime im Südosten mit dem Schwert besiegen |
| **Anschlagbrett** | Daily (aus JSON) | 2 Schleime – jeden Morgen neu verfügbar |
| **Bett** (am Haus) | – | Schlafen → nächster Tag, volle Herzen, neuer Startpunkt |
| **Kletter-Kai** + Portal | Besuchen (andere Szene) | Durch das Portal und im Jump'n'Run das Ziel erreichen |

### Steuerung

| Aktion | Tastatur | Gamepad |
|---|---|---|
| Laufen | WASD / Pfeiltasten | linker Stick / Steuerkreuz |
| Schwert | Leertaste / J / linke Maustaste | X (West) |
| Reden / Interagieren | E (oder F) | A (Süd) |
| Dialog weiter | E / F / Leertaste / Enter | A (Süd) |
| Inventar | I | Y (Nord) |
| Questlog | B | Select / View |
| Schliessen | Esc | B (Ost) |
| Cheat-Fenster | F1 (oder ^) | – |
| Hilfe der Testwiese | N | – |

Alle Tasten stehen an **einer** Stelle: `Assets/_Project/Core/Scripts/GameInput.cs`.

---

## Was ist drin?

```
Assets/_Project/
├── Core/          GameInput (Tasten), GamePause, PlayerLocator, Platzhalter-Sprites,
│                  UI-Baukasten (UiFactory, GuiDraw), Code-Sounds (ProceduralSfx)
├── Player/        PlayerController2D (Top-Down), CameraFollow2D, PlayerRespawn2D
├── Combat/        Health (Herzen, i-Frames), PlayerAttack2D (Schwert), EnemyAI2D,
│                  Knockback2D, DamageFlash2D, WeaponData
├── Interaction/   IInteractable, Interactor2D („[E] Reden")
├── Dialogue/      DialogueHUD (Schreibmaschinen-Text, Portrait), InteractableDialogue (Schilder)
├── World/         GameClock (Stardew-Uhr, Jahreszeiten), DayNightTint, Bed, ScenePortal
├── HUD/           GameHUD (Herzen, Gold, Level, Uhr)
├── Platformer/    Jump'n'Run: PlatformerController2D (Dash, Wandsprung, Greifen),
│                  GrabPoint (Halten/Schwingen), Vine (Lianen), Hazard/Checkpoint/Goal, HUD
├── Quests/        Quest-System aus Everdawn (ohne KI) + JSON-Import + DailyQuestReset
├── Inventory/     Inventar aus Everdawn + ItemUseEffects (Trank heilt)
├── Economy/       Wallet (Gold) + GoldRewardCollector
├── Progression/   XP/Level + XpRewardCollector
├── Audio/         Ordner für eigene Quest-Sounds (Resources/QuestSfx)
└── DevTools/      TestMeadow (Testwiese), PlatformerCourse (Kletterpfad), DevCheatWindow (F1),
                   IntegrationSelfTest
Assets/Scenes/     TestMeadow.unity, JumpAndRun.unity
```

- Architektur, Event-Flüsse und „wie baue ich eigene Inhalte": [Docs/ARCHITEKTUR.md](Docs/ARCHITEKTUR.md)
- Quests per Asset, JSON oder Code anlegen: [Docs/ARCHITEKTUR.md#quests-anlegen](Docs/ARCHITEKTUR.md#quests-anlegen)

## Tests

- **In Unity:** leere Szene → leeres GameObject → `Integration Self Test` → Play.
  Fährt 28 Prüfungen durch (Quests, Inventar, Gold, XP, JSON, Daily-Reset,
  i-Frames) und zeigt PASS/FAIL oben links. *Nicht* zusammen mit der Testwiese starten.
- **Ohne Unity:** `python3 Tests/logic_test.py` – spiegelt die Kernlogik
  (Quests, Inventar, Uhr, Daily-Reset, Health, Tag/Nacht, Jump'n'Run-Physik) mit 55 Prüfungen.

## Nächste Schritte (Vorschläge)

1. **Echte Welt:** Tilemap statt Platzhalter-Wiese, eigene Sprites + Animator
   (der `PlayerController2D` setzt schon `MoveX`/`MoveY`/`Speed`).
2. **Farming (Stardew):** Hacke/Giesskanne als Werkzeuge, Felder, Pflanzen wachsen
   über `GameClock.OnNewDay`, Ernte meldet `GameplayEvents.ItemCollected` → zählt
   automatisch für Sammel-Quests.
3. **Speichern beim Schlafen:** Quests + Inventar können es schon; Uhr hat
   `ToSaveData()`; Gold/XP fehlen noch → alles im `Bed` zusammenführen.
4. **Zelda-Teil:** Bildschirm-/Raumwechsel, Höhlen/Dungeons mit Schlüsseln,
   Herzcontainer (`Health.SetMaxHealth` existiert), mehr Gegnertypen.
5. **Laden & Hotbar:** Verkaufen/Kaufen über `Wallet.TrySpend`, Werkzeug-Leiste unten.
