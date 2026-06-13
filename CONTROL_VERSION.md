# Las Detox — Version Control

| Pole | Wartość |
| :--- | :--- |
| **Status projektu** | Wczesny prototyp — Prototype 0.3A (Patient Data Loader) zaimplementowany lokalnie; oczekuje commita i weryfikacji Play Mode |
| **Gałąź** | `main` (zsynchronizowany z `origin/main` na commicie `d9cae49`; implementacja 0.3A niezacommitowana) |
| **Data aktualizacji dokumentu** | 13.06.2026 |
| **Ostatni zapisany commit w repo** | `d9cae49` (`d9cae49…`) — *Update control version for Prototype 0.2 checkpoint* |
| **Aktualny stabilny checkpoint** | `d9cae49` — dokumentacja Prototype 0.2; ostatni commit implementacyjny: `1348cc0` (kamera management) |
| **Oznaczenie wersji roboczej** | Prototype 0.3A — Patient Data Loader v0.1 (lokalnie) |

---

## 2. Aktualny stabilny checkpoint

**Ostatni zapisany commit implementacyjny:** `1348cc0` — *Add management camera vertical slice for Detox ground floor*

**Wersja robocza (niezacommitowana):** Prototype 0.3A — Patient Data Loader v0.1

### Patient Data Loader (Prototype 0.3A — lokalnie)

- `PatientDataLoader` — [`Assets/_Project/Scripts/Patients/PatientDataLoader.cs`](Assets/_Project/Scripts/Patients/PatientDataLoader.cs), namespace `LasDetox.Patients`
- `PatientDefinitionDto`, `PatientCatalogDto`, `PatientDefinitionValidator` — deserializacja `JsonUtility`, walidacja sekcji/stringów/enumów/zakresów
- Katalog [`Assets/_Project/Data/Patients/Catalogs/detox_patients_catalog.json`](Assets/_Project/Data/Patients/Catalogs/detox_patients_catalog.json) — 3 ID w ustalonej kolejności
- 3 definicje JSON w [`Assets/_Project/Data/Patients/Definitions/`](Assets/_Project/Data/Patients/Definitions/) — pacjenci testowi (konfliktowy / wycofany / manipulacyjny)
- [`Assets/_Project/Data/Patients/README_PATIENT_DATA.md`](Assets/_Project/Data/Patients/README_PATIENT_DATA.md) — dokumentacja autora danych
- `TextAsset` + jawne referencje w Inspectorze na `Systems` → `PatientDataLoader`
- Loader: jednorazowa deserializacja → `Dictionary<string, PatientDefinitionDto>` → iteracja katalogu → `ValidPatients` + log podsumowania
- **Granica JsonUtility v0.1:** walidator nie wykrywa brakujących kluczy liczbowych/boolean vs `0`/`false`
- **Bez zmian:** Input System, kamera, UI, przyjęcia, runtime state pacjenta

**Weryfikacja 0.3A (13.06.2026):**

- Offline: struktura JSON 3 pacjentów OK (Python smoke test)
- Unity batch compile: niedostępny (projekt otwarty w edytorze) — **wymagany Play Mode w Unity**
- Oczekiwany log: `[PatientData] Loaded 3 patients, 0 errors.`

**Checkpoint Prototype 0.2 (`1348cc0` / `d9cae49`):**

### Harmonogram (dziedziczone z `09e83e6`)

- `GameClock` — zegar gry z tickiem minutowym, pauzą, skalowaniem czasu (×1/×2/×4) i zmianą dnia o północy
- `DailySchedule`, `DailyScheduleEntry`, `ScheduleEventType` — model harmonogramu (aktywności trwające vs zdarzenia punktowe)
- `ScheduleRunner` — rozwiązywanie `CurrentActivity`, `NextEntry`, `LastPointEvent`; obsługa skoków czasu
- `DetoxWeekdaySchedule.asset` — 17 wpisów harmonogramu dnia powszedniego na Detoxie (w tym `QuietHours` 22:00–06:00 z wrap przez północ)
- `ScheduleDebugPanel` — panel debugowy uGUI w scenie (`Pause`, `x1`/`x2`/`x4`, `+1h`)
- Podpięcie systemów w `DetoxPrototype.unity` pod rootami `Systems` i `DebugUI`
- `Tools/validate_schedule_logic.py` — walidator logiki harmonogramu (offline, Python)

### Kamera management (dodane w `1348cc0`)

- `ManagementCameraController` — [`Assets/_Project/Scripts/Camera/ManagementCameraController.cs`](Assets/_Project/Scripts/Camera/ManagementCameraController.cs), namespace `LasDetox.CameraSystem`
- Kamera **perspective** ze stałym **pitch** i **yaw** (brak orbitowania i obrotu runtime)
- Model widoku oparty o **punkt skupienia** (`_focusPoint`) i **odległość zoomu** (`_zoomDistance`) wzdłuż wektora widoku
- `_initialFocusPoint` i `_initialZoomDistance` jako źródło stanu startowego i resetu (`Home`)
- Przesuwanie **WASD** i **strzałkami** po płaszczyźnie XZ z wygładzaniem (`SmoothDamp`)
- Zoom **kółkiem myszy** z limitami min/max i wygładzaniem
- Ograniczenie ruchu przez serializowane `_boundsMin`, `_boundsMax`, `_boundsPadding` (bez `CameraBounds` / `BoxCollider`)
- Reset widoku klawiszem **Home**
- Blokowanie zoomu nad UI (`EventSystem.current.IsPointerOverGameObject()`, null-safe)
- New Input System przez bezpośredni `InputActionAsset` (`FindActionMap` / `FindAction`) — bez wrappera generowanego C#
- Mapa `ManagementCamera` w [`Assets/InputSystem_Actions.inputactions`](Assets/InputSystem_Actions.inputactions) — mapy `Player` i `UI` nietknięte
- Podpięcie kontrolera na `Main Camera` w `DetoxPrototype.unity`
- Brak Cinemachine, GameManagera, bootstrapu i obiektu `CameraBounds`

**Weryfikacja (13.06.2026, Unity Play Mode):**

- Unity kompiluje projekt bez błędów
- Scena `DetoxPrototype.unity` uruchamia się w Play Mode
- Kamera pokazuje greybox parteru Detoxu
- Przesuwanie WASD i strzałkami działa
- Zoom kółkiem myszy działa
- Reset widoku klawiszem `Home` działa
- Kamera zachowuje stały pitch i yaw
- `ScheduleDebugPanel` nadal działa
- Harmonogram nadal pracuje i emituje zdarzenia
- Logikę harmonogramu potwierdza walidator Python (`python3 Tools/validate_schedule_logic.py`)

**Wcześniejszy checkpoint harmonogramu:** `09e83e6` — *Add detox schedule simulation vertical slice* (zastąpiony jako główny checkpoint przez `1348cc0`, zachowany w historii).

---

## 3. Potwierdzone działające elementy

Poniższe elementy są obecne w repozytorium na checkpointcie `1348cc0`:

| Obszar | Stan |
| :--- | :--- |
| **Greybox parteru Detoxu** | Sala kominkowa, korytarze (pionowy i poziomy), dyżurka pielęgniarska, pralnia, 4 pokoje pacjentów — geometria podłóg i ścian w `Environment/Rooms` |
| **Hierarchia pomieszczeń** | Uporządkowane kontenery per pomieszczenie (`Floor`, `Walls`); wspólne ściany w `Shared Walls` |
| **Dokumentacja planu parteru** | `Dokumenty/PLAN_PARTERU_DETOX.md` — wymiary, połączenia, ściany wspólne |
| **Narzędzie raportowania layoutu** | `Tools/scene_layout_report.py` — read-only parser sceny Unity |
| **`GameClock`** | Dzień/godzina/minuta, pauza, przyspieszenie ×1/×2/×4, skok `+1h`, zmiana dnia o północy |
| **`DailySchedule`** | ScriptableObject z listą wpisów, sortowaniem i walidacją nakładających się aktywności |
| **`ScheduleRunner`** | `CurrentActivity`, `NextEntry`, `LastPointEvent`, emisja `OnScheduleEntryStarted` |
| **`ScheduleDebugPanel`** | Wyświetlanie czasu, aktywności, następnego wpisu, ostatniego zdarzenia punktowego; sterowanie czasem |
| **`DetoxWeekdaySchedule`** | Asset z pełnym harmonogramem dnia powszedniego (pobudka, posiłki, leki, grupa, spacery, cisza nocna itd.) |
| **Przyspieszenie, pauza, skok +1h** | Przyciski w panelu debugowym → `GameClock.SetTimeScale`, `TogglePaused`, `AdvanceHour` |
| **Aktywności trwające i zdarzenia punktowe** | `DurationMinutes > 0` → aktywność; `DurationMinutes = 0` → zdarzenie punktowe (nie staje się `CurrentActivity`) |
| **Zmiana dnia o północy** | `GameClock.AdvanceMinute` — `_hour >= 24` → `_hour = 0`, `_day++` |
| **Cisza nocna przez północ** | `QuietHours` (480 min od 22:00) — `DailySchedule.IsMinuteWithinActivity` z wrap (`end > 1440`) |
| **`ManagementCameraController`** | Kamera management na `Main Camera`; namespace `LasDetox.CameraSystem`; transform runtime wyliczany z focusu, zoomu, pitch i yaw |
| **Mapa input `ManagementCamera`** | Akcje `Pan`, `Zoom`, `Reset` w `InputSystem_Actions.inputactions` |
| **Pan WASD i strzałkami** | Przesuwanie po XZ z wygładzaniem |
| **Zoom kółkiem** | Zoom z limitami min/max i wygładzaniem |
| **Reset Home** | Przywraca `_initialFocusPoint` i `_initialZoomDistance` |
| **Stała rotacja kamery** | Stały pitch i yaw — brak orbitowania i obrotu runtime |
| **Serializowane bounds kamery** | `_boundsMin`, `_boundsMax`, `_boundsPadding` — bez collidera / `CameraBounds` |
| **`PatientDataLoader`** | Ładuje katalog + 3 definicje JSON; walidacja; `ValidPatients` w kolejności katalogu; log `[PatientData] Loaded N patients, E errors.` |
| **Dane pacjentów v0.1** | JSON w `Assets/_Project/Data/Patients/`; katalog ID; 3 testowe definicje |
| **Brak regresji harmonogramu / kamery / debug UI** | Zakres 0.3A nie modyfikuje istniejących systemów poza dodaniem komponentu na `Systems` |

**Poza zakresem (brak w repo):** Patient Roster UI (0.3B), Patient AI, NavMesh, system potrzeb, kolejki, konflikty, docelowe wyposażenie, produkcyjny HUD, ekonomia, przyjęcia/wypisy, kalendarz niedziel i świąt.

---

## 4. Stan sceny

**Główna scena:** `Assets/_Project/Scenes/DetoxPrototype.unity`

Scena jest **composition rootem** vertical slice — systemy gry są podpięte bezpośrednio w hierarchii sceny, bez osobnego bootstrapu ani `GameManager`.

**Rooty sceny (poziom 0):**

| Root | Zawartość |
| :--- | :--- |
| `Main Camera` | `Camera`, `AudioListener`, `ManagementCameraController` — transform runtime wyliczany przez kontroler z punktu skupienia, odległości zoomu, pitch i yaw |
| `Directional Light` | Oświetlenie |
| `Environment` | `Ground Placeholder` + `Rooms` (greybox parteru) |
| `Props` | Kontener na rekwizyty (obecnie pusty) |
| `Systems` | `GameClock` + `ScheduleRunner` + `PatientDataLoader` (jeden GameObject, trzy komponenty) |
| `DebugUI` | `ScheduleDebugPanel` (tworzy `DebugCanvas` w runtime) |

**`Environment/Rooms`:** Detox Common Room, Corridor Vertical, Corridor Horizontal, Nurse Station, Laundry, Patient Room 1–4, Shared Walls.

Obiekt `CameraBounds` **nie istnieje** w scenie.

---

## 5. Input System

| Element | Stan |
| :--- | :--- |
| **Pakiet** | `com.unity.inputsystem` 1.19.0 |
| **ProjectSettings** | `activeInputHandler: 1` — wyłącznie New Input System |
| **Asset** | [`Assets/InputSystem_Actions.inputactions`](Assets/InputSystem_Actions.inputactions) |

**Mapy w assetcie:**

| Mapa | Stan |
| :--- | :--- |
| `Player` | Szablon Unity — **nietknięty** |
| `UI` | Szablon Unity (`ScrollWheel` itd.) — **nietknięty**; `ScheduleDebugPanel` tworzy runtime `EventSystem` + `InputSystemUIInputModule` |
| `ManagementCamera` | Akcje `Pan` (WASD + strzałki), `Zoom` (`<Mouse>/scroll/y`), `Reset` (`<Keyboard>/home`) |

`ManagementCameraController` używa bezpośredniej referencji `InputActionAsset` z `FindActionMap` / `FindAction`. Brak wrappera generowanego C#.

---

## 6. Struktura kodu i narzędzia developerskie

### Skrypty gameplay (`Assets/_Project/Scripts/`)

| Skrypt | Namespace | Opis |
| :--- | :--- | :--- |
| `Time/GameClock.cs` | `LasDetox.Time` | Zegar gry |
| `Schedule/DailySchedule.cs` | `LasDetox.Schedule` | Model harmonogramu (SO) |
| `Schedule/DailyScheduleEntry.cs` | `LasDetox.Schedule` | Wpis harmonogramu |
| `Schedule/ScheduleEventType.cs` | `LasDetox.Schedule` | Typ zdarzenia |
| `Schedule/ScheduleRunner.cs` | `LasDetox.Schedule` | Runner harmonogramu |
| `Debug/ScheduleDebugPanel.cs` | `LasDetox.Debugging` | Panel debugowy uGUI |
| `Camera/ManagementCameraController.cs` | `LasDetox.CameraSystem` | Kamera management |
| `Patients/PatientDataLoader.cs` | `LasDetox.Patients` | Loader danych pacjentów z JSON |
| `Patients/PatientDefinitionDto.cs` | `LasDetox.Patients` | DTO definicji pacjenta |
| `Patients/PatientCatalogDto.cs` | `LasDetox.Patients` | DTO katalogu ID |
| `Patients/PatientDefinitionValidator.cs` | `LasDetox.Patients` | Walidacja definicji i katalogu |

### Narzędzia (`Tools/`)

| Narzędzie | Plik | Opis |
| :--- | :--- | :--- |
| Raportowanie layoutu sceny | `Tools/scene_layout_report.py` | Read-only analiza hierarchii `Environment/Rooms`; formaty `text`, `json`, `markdown` |
| Walidacja logiki harmonogramu | `Tools/validate_schedule_logic.py` | Offline testy logiki `ScheduleRunner` / `DailySchedule` (cisza nocna, skoki czasu, aktywności vs zdarzenia) |

**Przykłady uruchomienia** (z katalogu głównego repozytorium):

```bash
# Raport tekstowy layoutu sceny (domyślna scena DetoxPrototype)
python3 Tools/scene_layout_report.py

# Raport markdown do pliku
python3 Tools/scene_layout_report.py --format markdown --output /tmp/detox_layout.md

# Walidacja logiki harmonogramu
python3 Tools/validate_schedule_logic.py
```

Narzędzia **nie uruchamiają** Unity i **nie modyfikują** plików projektu.

---

## 7. Elementy jeszcze niewykonane

Najważniejsze systemy poza aktualnym checkpointem (planowane, niezaimplementowane):

- **Patient Roster UI** — panel listy i szczegółów (Prototype 0.3B)
- **Patient AI** — zachowanie i decyzje pacjentów
- **NavMesh i poruszanie postaci** — locomotion, pathfinding
- **System potrzeb** — głód, stres, uzależnienie itd.
- **Kolejki** — np. kawa przy dyżurce
- **Konflikty** — źródła napięcia z README
- **Właściwe wyposażenie** — meble, drzwi, sprzęt medyczny zamiast greyboxu
- **Docelowy HUD** — interfejs produkcyjny (obecnie tylko panel debugowy)
- **Ekonomia** — budżet, refundacja, zakupy
- **Przyjęcia i wypisy** — skierowania, kolejka łóżek
- **Pełny kalendarz niedziel i świąt** — osobny harmonogram (TODO w README)

---

## 8. Stan working tree

Stan na **13.06.2026** (zweryfikować przez `git status`):

| Plik / katalog | Status | Uwagi |
| :--- | :--- | :--- |
| `CONTROL_VERSION.md` | Zmodyfikowany | Aktualizacja Prototype 0.3A — oczekuje na commit dokumentacyjny |
| `Assets/_Project/Data/Patients/` | Nieśledzony | Implementacja 0.3A — JSON + README |
| `Assets/_Project/Scripts/Patients/` | Nieśledzony | Implementacja 0.3A — 4 skrypty C# |
| `Assets/_Project/Scenes/DetoxPrototype.unity` | Zmodyfikowany | `PatientDataLoader` na `Systems` |
| `ProjectSettings/ShaderGraphSettings.asset` | Zmodyfikowany lokalnie | Artefakt edytora Unity — poza zakresem |
| `.cursor/` | Nieśledzony | Plany Cursor — lokalne, nie commitować |
| `ProjectSettings/SceneTemplateSettings.json` | Nieśledzony | Artefakt edytora — poza zakresem |

Implementacja 0.3A **oczekuje** na precyzyjny commit (patrz plan — bez `git add .`).

**Zasada:** Nie używać `git add .`. Dodawać pliki precyzyjnie, po przejrzeniu `git status` i `git diff`.

---

## 9. Procedura bezpiecznego commita

1. Zakończyć **Play Mode** w Unity (jeśli był aktywny).
2. Zapisać scenę (`Ctrl+S` / *File → Save*).
3. Uruchomić `git status`.
4. Dodawać pliki **precyzyjnymi** poleceniami `git add <ścieżka>`.
5. Sprawdzić `git diff --staged`.
6. Ponownie sprawdzić `git status`.
7. Wykonać commit z jasnym komunikatem.
8. Wykonać `git push` (tylko po decyzji Operatora).
9. Potwierdzić czysty lub **świadomie nieczysty** working tree.

```text
Nie używać automatycznie `git add .`
```

---

## 10. Procedura rollbacku

**Przed rollbackiem:** sprawdzić `git status`, zabezpieczyć niezacommitowane zmiany (`git stash` lub osobna gałąź robocza).

**Historia:**

```bash
git log --oneline --decorate -n 15
```

**Podgląd checkpointu:**

```bash
git show <commit>
```

**Bezpieczny powrót do stabilnego stanu** (tymczasowa gałąź od wybranego commita):

```bash
git switch -c recovery/<nazwa> <commit>
```

Przykład powrotu do aktualnego checkpointu Prototype 0.2:

```bash
git switch -c recovery/management-camera-slice 1348cc0
```

Przykład powrotu do wcześniejszego checkpointu harmonogramu:

```bash
git switch -c recovery/schedule-slice 09e83e6
```

**Nie zalecać** `git reset --hard` jako podstawowej procedury — niszczy niezacommitowaną pracę na bieżącej gałęzi.

---

## 11. Zasady dla agentów

1. **Agent nie decyduje o architekturze** bez zatwierdzenia Operatora.
2. **Agent wykonuje tylko wskazany zakres** — bez rozszerzania zakresu „dla wygody”.
3. **Przed zmianą odczytuje aktualny stan repo** (`git status`, `git log`, istotne pliki źródłowe).
4. **Nie używa `git add .`** — tylko precyzyjne `git add <ścieżka>`.
5. **Nie wykonuje commita ani pushu** bez jawnej zgody Operatora.
6. **Nie modyfikuje plików spoza zakresu** zadania.
7. **Po pracy pokazuje `git diff` i `git status`** — Operator widzi pełny efekt zmian.
8. **Zmiany scen Unity wymagają testu w edytorze** (kompilacja + Play Mode tam, gdzie dotyczy runtime).
9. **Walidator Python nie zastępuje** kompilacji C# ani Play Mode w Unity.

---

## 12. Historia kluczowych checkpointów

| Commit | Opis | Znaczenie | Status |
| :--- | :--- | :--- | :--- |
| *(pending)* | Add patient data loader vertical slice (Prototype 0.3A) | JSON katalog + 3 definicje, loader, walidator, scena | **Roboczy — niezacommitowany** |
| `d9cae49` | Update control version for Prototype 0.2 checkpoint | Dokumentacja checkpointu 0.2 | Potwierdzony |
| `1348cc0` | Add management camera vertical slice for Detox ground floor | Prototype 0.2: kamera management | Potwierdzony |
| `9afb4d9` | Add project version control document | Dokument `CONTROL_VERSION.md` | Potwierdzony |
| `09e83e6` | Add detox schedule simulation vertical slice | Pierwszy działający vertical slice: zegar gry, harmonogram dnia powszedniego, runner, panel debugowy, walidator Python | Wcześniejszy checkpoint harmonogramu |
| `8f090d6` | Add Unity scene layout reporting tool | Narzędzie `scene_layout_report.py` do audytu layoutu sceny bez Unity | Potwierdzony |
| `da30738` | Document and finalize detox ground floor greybox | Finalizacja greyboxu + `Dokumenty/PLAN_PARTERU_DETOX.md` | Potwierdzony |
| `d123a76` | Reorganize detox room hierarchy | Uporządkowana hierarchia `Environment/Rooms` (kontenery per pomieszczenie) | Potwierdzony |
| `479ce52` | Build complete Detox greybox layout | Kompletny layout greyboxowy parteru | Potwierdzony |
| `da3dbe6` | Create Detox prototype scene | Utworzenie sceny `DetoxPrototype.unity` | Potwierdzony |
| `3bc3280` | Initial Unity 6 URP project setup | Bazowy projekt Unity 6 URP | Potwierdzony |

---

## 13. Następny bezpieczny krok

1. **Play Mode w Unity** — potwierdzić log `[PatientData] Loaded 3 patients, 0 errors.` oraz brak regresji harmonogramu/debug/kamery.
2. Zacommitować implementację 0.3A precyzyjnym `git add` (patrz plan Prototype 0.3A).
3. Zacommitować `CONTROL_VERSION.md` (osobny commit dokumentacyjny lub po akceptacji Operatora).
4. Wybrać **Prototype 0.3B — Patient Roster UI** jako kolejny slice po zamknięciu 0.3A.

> Kolejny slice UI powinien czytać `PatientDataLoader.ValidPatients` bez zmiany loadera.
