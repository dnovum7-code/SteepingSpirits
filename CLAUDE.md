# CLAUDE.md – Steeping Spirits

Gemütliches 2D-Spiel (Unity 6, Top-down + Jump'n'Run) über ein Teehaus mit Geistern als Gästen.
Module: Wiese/Grundsysteme, Jump'n'Run (`Platformer`), Teegarten (`Plantation`), Teewerkstatt
(`Processing`), Brühen (`Brewing`), gemeinsamer Zutaten-Katalog (`Ingredients`).

## Arbeitsregeln

- **Branch:** pro Auftrag ein eigener Branch (+ eigener `git worktree`, wenn parallel gearbeitet wird).
  Nichts nach `main`, kein Force-Push, regelmäßig pushen. Merge-Commits getrennt von Arbeits-Commits.
- **Progress-Datei** `Docs/<AUFTRAG>_PROGRESS.md`: ganz oben „Nächster konkreter Schritt“, dann Backlog mit
  Status, Annahmen, Editor-Checkliste pro Commit. Ein Commit pro Backlog-Punkt, sobald er kompiliert und die
  Tests grün sind.
- **Zuerst lesen:** Progress-Datei, dann `Docs/PLAYTEST_NOTES.md` – Befunde dort haben Vorrang und werden je
  ein eigener Backlog-Punkt.
- **Ohne Unity prüfen:** Core-Tests `dotnet test Tests/<Modul>.DotNet` (BrewingCore, Ingredients,
  PlantationCore, PlatformingCore, ProcessingCore); alles kompiliert gegen die Unity-Referenzen mit neuem
  Input System **und** altem Input Manager, ohne Fehler und ohne Warnungen. Was nur in Unity sichtbar ist,
  als **„ungeprüft“** markieren.
- **Sprache:** Code und Kommentare Englisch; Spielertexte Deutsch, zentral in einer Textklasse pro Modul
  (`GardenTexts`, `ProcessingTexts`, `BrewTexts`, `JumpNRunTexts`, `IngredientTexts`).
- **Feedback** immer sanft: kein Screenshake, keine grellen Blitze. Keine Assets aus dem Internet.
- **Abschlussbericht** (in der Progress-Datei und als Antwort): was gebaut ist / was offen ist, Tests,
  Simulator-Zahlen, geänderte Startwerte mit Begründung, Annahmen und Abweichungen, Editor-Checkliste pro
  Commit, Testplan ≤ 10 Minuten (wichtigstes zuerst), Merge-Hinweise.
- **Stoppen und fragen** nur bei: neues Package nötig, Code außerhalb der Freigaben, unwiderrufliches Löschen.

## Modulaufbau

```
Assets/_Project/<Modul>/
  Core/      reine Logik, eigene asmdef mit noEngineReferences, Tuning als [Serializable]-Parameterklassen
  Runtime/   MonoBehaviours (Assembly-CSharp): Data/ (ScriptableObjects mit den Parametern),
             Integration/ (Adapter zu Inventar, Speichern, Uhr), Presentation/ (Ansicht, Ton, Partikel)
  Editor/    Builder – Szenen und Prefabs entstehen NUR per Editor-Skript, nie von Hand
  Tests/EditMode/   NUnit-Tests (auch von dotnet ausgeführt)
Tests/<Modul>.DotNet/   dotnet-Testprojekt außerhalb von Assets/ (.gitignore-Ausnahme für die .csproj)
```

- Module kennen sich nicht direkt; gemeinsame Daten laufen über den Zutaten-Katalog, Inventar/Vorratsschrank
  und Events. Fremden Modul-Code nur lesen, außer der Auftrag gibt ihn frei.
- Tuning-Werte immer in ScriptableObjects/Daten-Dateien; Builder legen Assets einmal an und überschreiben sie nie.

## Zutaten-ID-Grammatik

```
<katalog-id>[:<stil>][~<kraut>][/<note>.<note>…][+q<stufe>]
tea_leaf  ·  tea_leaf+q3  ·  tea_oolong:dark/honeyed.roasty+q3  ·  tea_blend:green~jasmine/fresh.floral+q2
```

- Katalog-ID aus `IngredientCatalog`; **Varianten nie als neue Katalog-Einträge**, sondern über diese Grammatik.
- Qualität über `IngredientQuality` (`+q2`, `+q3`; Stufe 1 ohne Endung). Noten sortiert (`TeaNotes.Key`).
- Lesen/Schreiben: `ProcessedTea.MakeId/TryParseId/CatalogIdOf`, `TeaBlend`, `DriedHerb`.
- Inventar-Item-ID = `ingredient_<zutaten-id>`; Ausnahme Teeblatt Stufe 1 = `item_teeblatt` (Quests).
- Vorratsschrank (`pantry.json`) und Speicherstände bewahren unbekannte IDs unverändert auf.

## Szenenwechsel-Regel

- `ScenePortal.Load` nur für echte Ortswechsel, deren Rückweg über `ScenePortal.PreviousScenePath` geht
  (Wiese ↔ Garten, Wiese ↔ Lichtung).
- Unterorte (Werkstatt im Garten, Brühen in der Werkstatt) laden mit eigenem Lader ohne `PreviousScenePath`
  zu verändern (`WorkshopSceneLoader`), sonst führt das Gartentor danach in die Werkstatt statt zur Wiese.

## Uhr-Regel

- `GameClock` (DontDestroyOnLoad) ist die einzige Uhr. Module docken über Adapter an (`IDayClock` im Garten,
  `IGameTime` in der Werkstatt). Läuft die Uhr rückwärts (Neustart), vergeht für Module keine Zeit.
- UI, in der man Zeit beobachtet (Oxidationskorb), darf `GamePause` **nicht** setzen – sonst steht die Uhr.

## Speicherdienst

- `SaveService` (Modul `SaveGame`, startet automatisch vor allen `Start()`): Inventar **inklusive** zur Laufzeit
  angelegter Gegenstände und `GameClock` in `game_save.json` (versioniert, altes `inventory_save.json` wird
  übernommen). Gespeichert wird bei Szenenwechsel, neuem Tag, alle 20 s und beim Beenden.
- Laufzeit-Gegenstände werden beim Laden aus ihrer ID neu gebaut (`ItemRecipes`: ID-Grammatik + Katalog).
  Module mit eigenen Laufzeit-Items melden einen Resolver an (`ItemRecipes.Register`, z. B. Garten-Saatgut).
- Unbekannte IDs gehen nie verloren: sie warten in `pending` und kommen zurück, sobald eine Szene sie registriert.
- Module behalten ihre eigenen JSON-Dateien (`garden_save.json`, `workshop_save.json`, `jumpnrun_save.json`,
  `pantry.json`). Neue Laufzeit-Items brauchen eine Zutaten-ID (dann automatisch) oder einen Resolver.

## Tastentabelle

Siehe Abschnitt „Tasten“ unten (vollständig ab B6).

## Feste Entscheidungen

- Qualitäts-Obergrenze in der Werkstatt: gewöhnliche Blätter höchstens „fein“; erster Austrieb
  (`tea_leaf_first`) und Goldspitze erlauben „erlesen“.
- Die erste Teestrauch-Ernte liefert `tea_leaf_first`.
- Die vier Grundtees (Weiß, Grün, Oolong, Schwarz) brühen sich unverändert – Regressionstests sichern das.
- Mischverhältnis vorerst 1 Teil Tee + 1 Teil Kraut = 2 Teile Mischung (in Auswertung).

## Tasten

_(wird in B6 vervollständigt)_
