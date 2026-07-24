# PizzaShop — kontekst projektu dla Claude Code

E-commerce do zamawiania pizzy: katalog, koszyk, zamówienia, płatności PayU, panel
klienta/pracownika/admina, live-tracking statusu (SignalR). Jedna pizzeria, jedna
lokalizacja. Stack: .NET 8 / ASP.NET Core / EF Core + PostgreSQL, Clean Architecture
(`Domain`/`Application`/`Infrastructure`/`Api`), JWT, xUnit+Moq(+FluentAssertions).
Frontend: React + TypeScript (Vite) w `frontend/`.

## Źródła prawdy (nie duplikuj ich treści tutaj)

| Obszar | Plik |
|---|---|
| Model domenowy: encje, flow zamówienia, role biznesowe, punkty lojalnościowe | `docs/domain-model.md` (ma `## Indeks`) |
| Warstwa Api: kontrolery, JWT, autoryzacja ról, middleware wyjątków, SignalR, webhook PayU | `docs/api-layer.md` (ma `## Indeks`) |
| Warstwa Application: CQRS, walidacja, porty | `docs/application-layer.md` (ma `## Indeks`) |
| Warstwa Infrastructure: EF Core, PayU, geokodowanie | `docs/infrastructure-layer.md` (ma `## Indeks`) |
| Decyzje architektoniczne | `docs/decisions.md` = indeks → `docs/adr/ADR-NNNN.md` = treść; `## ADR Notes` = log użycia per zadanie |

## Tryby pracy i workflow agentów (`.claude/agents/`)
Domyślny tryb to **fast**. Tryb wybiera główny wątek sam, na podstawie tabeli poniżej —
bez pytania usera i bez spawnowania dodatkowego agenta tylko po to, żeby zaklasyfikować
zadanie. W razie wątpliwości wybierz niższy tryb i eskaluj do wyższego dopiero, gdy w
trakcie implementacji faktycznie pojawi się potrzeba decyzji architektonicznej.

| Sygnał | Fast (small) | Normal (medium) | Deep (large) |
|---|---|---|---|
| Warstwy dotknięte | 1 (tylko `Api`/tylko frontend) | 2-3 warstwy | zmiana granic warstw/wzorca |
| Reguła biznesowa w `Domain` | brak nowej | pojedyncza, prosta | złożona / wiele agregatów |
| Migracja EF Core | brak | pojedyncza, addytywna | zmiana istniejącego schematu/relacji |
| Bezpieczeństwo/płatności/role | nie dotyczy | pośrednio | wprost |
| Przykłady | nowa strona, komponent UI, stylowanie, prosty endpoint CRUD | nowy moduł, integracja zewnętrzna | duży refaktor, zmiana architektury, decyzja krytyczna |

**Workflow per tryb:**
- **Fast** (domyślny): `builder` → `reviewer-lite`.
- **Normal**: `architect-lite` → `builder` → `reviewer-lite`.
- **Deep**: `architect` → `builder` → `reviewer`.

`architect`/`reviewer` (pełne) — WYŁĄCZNIE deep mode. `architect-lite` nie pisze ADR ani
nie modyfikuje `docs/decisions.md`/`docs/domain-model.md` — daje tylko krótki plan (cel,
dotknięte pliki, rozwiązanie, ryzyka, walidacja). `reviewer-lite` sprawdza wyłącznie rzeczy
blokujące (build/test, bezpieczeństwo, oczywiste błędy) — pomija pełny audyt Clean
Architecture i stylu. `task-classifier` istnieje na wypadek naprawdę niejasnego zakresu —
w typowym przypadku klasyfikację robi główny wątek wg tabeli wyżej, bez spawnowania go.

Liczba dotkniętych warstw to sygnał **pomocniczy**, nie samodzielne kryterium: prosty
endpoint CRUD z natury dotyka Api+Application (czasem +Infrastructure), a mimo to zostaje
fast mode, dopóki nie wprowadza nowej reguły biznesowej w `Domain`, nieaddytywnej migracji
ani niczego z bezpieczeństwem/płatnościami/rolami wprost. Nie eskaluj trybu tylko dlatego,
że zmiana dotyka więcej niż jednego pliku.

### Reguły obowiązkowe

**Domyślne zachowanie:** Małe zadania MUSZĄ iść przez `builder` → `reviewer-lite`. Nie
spawnuj `architect` ani `reviewer` (pełnych), dopóki zadanie jednoznacznie nie spełnia
kryteriów normal/deep z tabeli wyżej.

**Dyscyplina zakresu:** Agenci mają dostarczać działające zmiany, nie dokumentację.
Dokumentacja (ADR, pełny plan architekta) powstaje tylko tam, gdzie faktycznie zapada nowa
decyzja i ułatwia to dalszą pracę — nie jest celem samym w sobie.

**Warunki zakończenia:** Każdy agent kończy pracę, gdy osiągnie swój zdefiniowany output
(patrz jego plik w `.claude/agents/`). Nie kontynuuje eksploracji niepowiązanych części
repo "przy okazji".

**Priorytet — projekt portfolio:** Optymalizuj w tej kolejności: 1) działająca funkcja,
2) szybka iteracja, 3) spójność z istniejącym kodem. Nie projektuj pod skalę enterprise
(sharding, multi-region, event sourcing itp.) bez wyraźnej potrzeby zgłoszonej przez usera.

## Zasady korzystania z dokumentacji
1. Zadanie w znanym obszarze (checkout, płatności, promocje, autoryzacja...) → najpierw
   sprawdź `## ADR Notes` w `docs/decisions.md`.
2. Brak trafienia → `## Indeks` w `docs/decisions.md`, wybierz konkretne numery ADR.
3. Otwórz (albo zleć architektowi) WYŁĄCZNIE `docs/adr/ADR-000X.md` dla wybranych
   numerów — nigdy cały katalog `docs/adr/`, nigdy `docs/decisions.md` w całości jako
   sposób na dotarcie do treści ADR.
4. Nigdy nie czytaj w głównym wątku plików >150 linii (`docs/domain-model.md`,
   `docs/api-layer.md`, pojedynczych ADR) bez offsetu — użyj ich `## Indeks` +
   `Read offset/limit`, albo zleć subagentowi konkretne pytanie z limitem odpowiedzi
   (maks. 15 punktów, bez cytatów, bez przepisywania treści). Do głównego wątku wraca
   wyłącznie streszczenie. Wyjątek: plik, który za chwilę edytujesz.
5. Szukanie przed czytaniem: `rg -n "wzorzec" docs/` → `Read` z offset/limit. Nigdy
   `Read` bez limitu na pliku o nieznanym rozmiarze.
6. Po zadaniu w **normal/deep mode**: dopisz wpis na górze `## ADR Notes` w
   `docs/decisions.md` (szablon w pliku) — użyte ADR-y i ich wpływ na implementację, oraz
   przeczytane-nieużyte. W **fast mode** pomiń ten krok, jeśli nic z ADR nie było użyte.
7. Nie pytaj, jeśli można sprawdzić: `git log --oneline -20`, `git status`, pliki repo.
   `AskUserQuestion` tylko o decyzje produktowe niewywnioskowalne z repo, max. 1 runda
   pytań na sesję.
8. Po rozpoznaniu, przed implementacją: "Rozpoznanie zakończone, wczytano ~Nk tokenów.
   Sugeruję /compact."

## Krytyczne ograniczenia (skrót — pełna treść w źródłach prawdy powyżej)
- **Webhook PayU nie może wymagać JWT** — weryfikacja przez podpis/sekcję żądania PayU,
  inaczej podatny na sfałszowane potwierdzenia płatności.
  → `docs/api-layer.md` §7, ADR-0013/ADR-0022/ADR-0027.
- **Respektuj hierarchię ról w `[Authorize]`**: endpoint `Employee` musi jawnie dopuszczać
  też `RestaurantAdmin`,`SuperAdmin` (analogicznie `RestaurantAdmin` → `SuperAdmin`) —
  hierarchię egzekwuje `Api`, nie token ani `Domain`; używaj stałych `AuthRoles`.
  → `docs/api-layer.md` §5, ADR-0004/ADR-0027.
- **Single-tenant** — nie projektuj encji/relacji pod multi-tenancy (`RestaurantId` jako
  partycjonowanie) bez nowego ADR zmieniającego zakres. → ADR-0003.
- **Reguły biznesowe zależne od stanu** (obszar dostawy, godziny pracy, przejścia
  statusów) żyją jako guard clauses w `Domain`, nie w FluentValidation — walidator
  sprawdza tylko kształt danych. → `docs/application-layer.md` §1, ADR-0012.
- **Nie zakładaj platformy hostingu/CI** (Azure/AWS/VPS) bez wyraźnej decyzji.

## Konwencje globalne
- Nullable reference types: włączone. Async wszędzie gdzie I/O, `CancellationToken`
  przekazywany dalej. Wyjątki domenowe dziedziczą po `DomainException`.
- CQRS: `Commands/`, `Queries/` — jeden handler = jeden plik = jeden test jednostkowy.
- Nazewnictwo: encje l. pojedyncza (`Order`), `CreateOrderCommand`, `GetOrderByIdQuery`,
  testy `MethodName_Scenario_ExpectedResult`.

## Komendy
```bash
dotnet build
dotnet test
dotnet ef migrations add <Nazwa> -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet ef database update -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet run --project src/PizzaShop.Api
```

## Środowisko
VS Code, przegląd lokalny (Source Control diff, GitLens, Git Graph) — bez integracji
GitHub/GitLab PR. CI: `.github/workflows/ci.yml` (`dotnet build`/`dotnet test` na
push/PR do `main`).
