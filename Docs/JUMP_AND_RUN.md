# Jump'n'Run-Szene (Kletterpfad)

Eigene Szene in Seitenansicht: `Assets/Scenes/JumpAndRun.unity`. Die Steuerung
fühlt sich an wie in **Celeste** (Dash, Wandsprung, präzise Sprünge). Dazu
kommen drei Arten, sich festzuhalten: **Haltepunkte**, **Schwingpunkte** und
physikalische **Lianen**.

## Starten

- **Direkt:** `Assets/Scenes/JumpAndRun.unity` öffnen → Play.
- **Über die Wiese:** `Assets/Scenes/TestMeadow.unity` öffnen → Play → ganz links
  zum lila **Portal** → `E`. Kletter-Kai daneben vergibt die Quest
  „Der Kletterpfad". Erreichst du das Ziel, ist sie erledigt (das Quest-System
  läuft über Szenen hinweg weiter). Das Portal am Ziel bringt dich zurück.

> Im Editor werden Szenen über ihren Pfad geladen und müssen **nicht** in den
> Build-Einstellungen stehen. Für einen echten Build beide Szenen unter
> *File → Build Profiles* eintragen.
>
> Falls die Szene beim Öffnen leer ist (der Komponenten-Link ging verloren):
> leeres GameObject → **Add Component → „Platformer Course"** → Szene speichern.

## Steuerung

| Aktion | Tastatur | Gamepad |
|---|---|---|
| Laufen / Zielen | WASD / Pfeile | linker Stick / Steuerkreuz |
| Springen (länger halten = höher) | Leertaste / C | A |
| Dash (8 Richtungen) | Shift / X / L | X |
| Greifen (halten!) | K / Strg | RB / LB / RT |
| Hilfe ein/aus | N | – |

Tasten nach US-Position, wie es das Input System zählt.

## Mechaniken

| Mechanik | So geht's | Wichtige Inspector-Werte (`PlatformerController2D`) |
|---|---|---|
| Laufen & Springen | Sprung kurz drücken = kleiner Hüpfer, halten = voller Sprung. Kurz nach der Kante geht es noch (Coyote-Time), zu früh gedrückt wird gemerkt (Puffer). | `runSpeed`, `jumpVelocity`, `gravity`, `coyoteTime`, `jumpBufferTime` |
| Wand | In der Luft gegen die Wand → rutscht langsam. Springen → Wandsprung weg von der Wand. | `wallSlideSpeed`, `wallJumpVelocity` |
| Dash | Richtung halten + Dash. 1 Ladung, die am Boden oder beim Greifen wieder voll wird. Die Figur ist **rot**, wenn der Dash bereit ist, und **blau**, wenn er verbraucht ist. Springst du während eines Dashes am Boden, gibt es einen weiten **Super-Sprung**. | `dashSpeed`, `dashDuration`, `maxDashes` |
| Halten (gelbe Punkte) | Greifen gedrückt halten → man hängt still. Springen (dosiert!) oder Dash → weiter. Loslassen → fallen. | `holdOffset`, `holdJumpVelocity` |
| Schwingen (blaue Punkte) | Greifen → hängt am Seil. **Im Takt** links/rechts drücken (immer in Bewegungsrichtung) → jeder Schwung wird grösser. Springen/Loslassen → Abflug mit dem ganzen Schwung. Höchstens 110° Ausschlag, also kein Überschlag. | `pumpAcceleration`, `swingDamping`, `maxSwingSpeed`, `maxSwingAngle`, `swingReleaseBoost` |
| Lianen | Greifen → hängt an der Liane. Sie ist echte Physik (Kette aus Gelenken) und wird durch das eigene Gewicht ausgelenkt. Links/rechts schaukelt sie auf, hoch/runter klettert man. Springen → Abflug mit dem Tempo der Liane. | `vinePumpForce`, `vineWeight`, `vineReleaseBoost`, `vineCatchRadius` |

Alle Werte sind im Inspector änderbar, und zwar auch **während Play**. Ideal zum
Experimentieren.

## Der Parcours (`PlatformerCourse`)

1. **Start**: Laufen, Springen
2. **Dash**: eine 8er-Lücke (nur mit Dash) und eine 5er-Stufe (Sprung + Dash nach oben)
3. **Wand**: Schacht per Wandsprung hoch
4. **Halten**: 4 Haltepunkte über Stacheln
5. **Schwingen**: 3 Seilpunkte über einer grossen Grube
6. **Lianen**: 4 Lianen, danach das **Ziel** und das Rückkehr-Portal

Jeder Abschnitt hat einen **Checkpoint** (die Fahne wird grün). Stacheln bedeuten
sofortigen Neustart am Checkpoint. Unterwegs liegen 5 **Sternfrüchte**: Sie
landen im Inventar und zählen für Sammel-Quests. Das HUD oben links zeigt Zeit,
Tode und Dash-Anzeige. Im Cheat-Fenster (F1, Tab „Spieler") gibt es unendlich
Dashes und „Zum letzten Checkpoint".

Erreichbarkeit geprüft: `Tests/logic_test.py`, Abschnitt 15 (Sprung, Dash-Lücke,
Stufe, Aufbau des Schwungs, kein Überschlag).

## Eigene Level bauen

- **Spieler**: GameObject mit Tag `Player`, `Rigidbody2D`, `CapsuleCollider2D`,
  Sprite als **Kind**, `PlatformerController2D`, optional `Interactor2D` (für Portale).
- **Kamera**: `CameraFollow2D`.
- **Boden/Wände**: beliebige Collider2D (keine Trigger).
- **Haltepunkt/Schwingpunkt**: leeres Objekt + `GrabPoint` (Mode Hold/Swing,
  Catch Radius, Rope Length). Ein Collider ist nicht nötig.
- **Liane**: leeres Objekt am Aufhängepunkt + `Vine`. Die Segmente entstehen beim Start.
- **Stacheln**: Trigger-Collider2D + `Hazard2D`. **Checkpoint**: Trigger + `Checkpoint2D`.
  **Ziel**: Trigger + `Goal2D` (Location-ID für Visit-Quests).
- **Portal**: Collider2D + `ScenePortal` (Szenen-Pfad oder „Return To Previous").
