# Las Detox — Version Control

| Pole | Wartość |
| :--- | :--- |
| **Status projektu** | Wczesny prototyp — Vertical Slice 0.1 w toku; stabilny checkpoint obejmuje greybox parteru Detoxu i symulację harmonogramu dnia powszedniego |
| **Gałąź** | `main` (zsynchronizowana z `origin/main`) |
| **Data aktualizacji dokumentu** | 13.06.2026 |
| **Aktualny stabilny commit** | `09e83e6` (`09e83e6664687d203325ee2f6a618da40a6e9fc5`) |
| **Oznaczenie wersji roboczej** | Prototype 0.1 |

---

## 2. Aktualny stabilny checkpoint

**Commit:** `09e83e6` — *Add detox schedule simulation vertical slice*

**Zakres:**

- `GameClock` — zegar gry z tickiem minutowym, pauzą, skalowaniem czasu (×1/×2/×4) i zmianą dnia o północy
- `DailySchedule`, `DailyScheduleEntry`, `ScheduleEventType` — model harmonogramu (aktywności trwające vs zdarzenia punktowe)
- `ScheduleRunner` — rozwiązywanie `CurrentActivity`, `NextEntry`, `LastPointEvent`; obsługa skoków czasu
- `DetoxWeekdaySchedule.asset` — 17 wpisów harmonogramu dnia powszedniego na Detoxie (w tym `QuietHours` 22:00–06:00 z wrap przez północ)
- `ScheduleDebugPanel` — panel debugowy uGUI w scenie (`Pause`, `x1`/`x2`/`x4`, `+1h`)
- Podpięcie systemów w `DetoxPrototype.unity` pod rootami `Systems` i `DebugUI`
- `Tools/validate_schedule_logic.py` — walidator logiki harmonogramu (offline, Python)

**Weryfikacja:**

- Logikę harmonogramu potwierdza walidator Python (`python3 Tools/validate_schedule_logic.py`).
- Checkpoint zawiera `ScheduleDebugPanel` przeznaczony do weryfikacji runtime w Unity Play Mode.
- Checkpoint został ręcznie zweryfikowany w Unity Play Mode 13.06.2026. Potwierdzono poprawną kompilację bez błędów, start harmonogramu o 06:00, pojedynczą emisję zdarzenia ActiveCycleRadio, działanie pauzy, prędkości ×1/×2/×4 i skoku +1h, rozdzielenie aktywności od zdarzeń punktowych oraz zmianę dnia o północy. Weryfikację runtime należy powtarzać przy każdym kolejnym checkpointie zmieniającym scenę lub logikę symulacji.

---

## 3. Potwierdzone działające elementy

Poniższe elementy są obecne w repozytorium na commicie `09e83e6`:

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

**Poza zakresem checkpointu (brak w repo):** Patient AI, NavMesh, system potrzeb, kolejki, konflikty, docelowe wyposażenie, produkcyjny HUD, ekonomia, przyjęcia/wypisy, kalendarz niedziel i świąt.

---

## 4. Stan sceny

**Główna scena:** `Assets/_Project/Scenes/DetoxPrototype.unity`

Scena jest **composition rootem** pierwszego vertical slice — systemy gry są podpięte bezpośrednio w hierarchii sceny, bez osobnego bootstrapu ani `GameManager`.

**Rooty sceny (poziom 0):**

| Root | Zawartość |
| :--- | :--- |
| `Main Camera` | Kamera prototypu |
| `Directional Light` | Oświetlenie |
| `Environment` | `Ground Placeholder` + `Rooms` (greybox parteru) |
| `Props` | Kontener na rekwizyty (obecnie pusty) |
| `Systems` | `GameClock` + `ScheduleRunner` (jeden GameObject, dwa komponenty) |
| `DebugUI` | `ScheduleDebugPanel` (tworzy `DebugCanvas` w runtime) |

**`Environment/Rooms`:** Detox Common Room, Corridor Vertical, Corridor Horizontal, Nurse Station, Laundry, Patient Room 1–4, Shared Walls.

---

## 5. Aktualne narzędzia developerskie

Katalog `Tools/` zawiera wyłącznie:

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

## 6. Elementy jeszcze niewykonane

Najważniejsze systemy poza aktualnym checkpointem (planowane, niezaimplementowane):

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

## 7. Pliki świadomie poza commitami

Stan working tree na **13.06.2026** (należy każdorazowo zweryfikować przez `git status` — może się zmienić):

| Plik / katalog | Status | Uwagi |
| :--- | :--- | :--- |
| `.cursor/` | Nieśledzony | Plany i konfiguracja IDE Cursor — lokalne, nie commitować bez decyzji Operatora |
| `ProjectSettings/ShaderGraphSettings.asset` | Zmodyfikowany lokalnie | Zmiana generowana/odkrywana przez edytor Unity — sprawdzić diff przed ewentualnym dodaniem |
| `ProjectSettings/SceneTemplateSettings.json` | Nieśledzony | Plik ustawień edytora — typowo lokalny artefakt |

**Zasada:** Nie używać `git add .`. Dodawać pliki precyzyjnie, po przejrzeniu `git status` i `git diff`.

Ten dokument (`CONTROL_VERSION.md`) po utworzeniu również wymaga świadomego dodania do commita, jeśli Operator zdecyduje o jego wersjonowaniu.

---

## 8. Procedura bezpiecznego commita

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

## 9. Procedura rollbacku

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

Przykład powrotu do aktualnego stabilnego checkpointu:

```bash
git switch -c recovery/schedule-slice 09e83e6
```

**Nie zalecać** `git reset --hard` jako podstawowej procedury — niszczy niezacommitowaną pracę na bieżącej gałęzi.

---

## 10. Zasady dla agentów

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

## 11. Historia kluczowych checkpointów

| Commit | Opis | Znaczenie | Status |
| :--- | :--- | :--- | :--- |
| `09e83e6` | Add detox schedule simulation vertical slice | Pierwszy działający vertical slice: zegar gry, harmonogram dnia powszedniego, runner, panel debugowy, walidator Python | **Aktualny stabilny checkpoint** |
| `8f090d6` | Add Unity scene layout reporting tool | Narzędzie `scene_layout_report.py` do audytu layoutu sceny bez Unity | Potwierdzony |
| `da30738` | Document and finalize detox ground floor greybox | Finalizacja greyboxu + `Dokumenty/PLAN_PARTERU_DETOX.md` | Potwierdzony |
| `d123a76` | Reorganize detox room hierarchy | Uporządkowana hierarchia `Environment/Rooms` (kontenery per pomieszczenie) | Potwierdzony |
| `479ce52` | Build complete Detox greybox layout | Kompletny layout greyboxowy parteru | Potwierdzony |
| `da3dbe6` | Create Detox prototype scene | Utworzenie sceny `DetoxPrototype.unity` | Potwierdzony |
| `3bc3280` | Initial Unity 6 URP project setup | Bazowy projekt Unity 6 URP | Potwierdzony |

---

## 12. Następny bezpieczny krok

> Kolejny system powinien powstawać jako osobny, mały vertical slice na bazie aktualnego stabilnego checkpointu.
