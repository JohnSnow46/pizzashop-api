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

## Workflow agentów (`.claude/agents/`)
`architect` (projekt, aktualizuje `docs/decisions.md`/`docs/domain-model.md`, NIE pisze
kodu produkcyjnego) → `builder` (implementacja + testy) → `reviewer` (przegląd, NIE
modyfikuje kodu) → poprawki przez `builder`. Wywołuj `architect` na start nowej
funkcjonalności lub przy decyzji strukturalnej.

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
6. Po zadaniu: dopisz wpis na górze `## ADR Notes` w `docs/decisions.md` (szablon w
   pliku) — użyte ADR-y i ich wpływ na implementację, oraz przeczytane-nieużyte.
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
