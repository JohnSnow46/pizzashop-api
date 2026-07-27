# PizzaShop

E-commerce do zamawiania pizzy dla jednej pizzerii (single-location): katalog menu z
wariantami, koszyk, zamówienia jako gość lub zalogowany klient, płatności online przez
PayU, punkty lojalnościowe, promocje (w tym BuyXGetY), live-tracking statusu zamówienia
przez SignalR oraz panele dla klienta, pracownika i administratora.

Projekt portfolio — priorytet: działająca funkcja i szybka iteracja, nie architektura pod
skalę enterprise.

## Stack

- **Backend:** .NET 8 / ASP.NET Core, EF Core + PostgreSQL, Clean Architecture
  (`Domain` / `Application` / `Infrastructure` / `Api`), JWT, xUnit + Moq + FluentAssertions.
- **Frontend:** React + TypeScript (Vite), Vitest.
- **Płatności:** PayU (sandbox).
- **Real-time:** SignalR (live-tracking statusu zamówienia).

## Funkcjonalności

- Katalog menu z wariantami produktów, wyszukiwanie/filtrowanie.
- Koszyk (client-side) i checkout: gość lub zalogowany klient, adres dostawy z
  weryfikacją obszaru (promień od restauracji), odbiór osobisty, wybór terminu.
- Płatności PayU (inicjalizacja, webhook potwierdzenia, retry płatności dla gościa).
- Role: `Customer` / `Employee` / `RestaurantAdmin` / `SuperAdmin`, z hierarchią uprawnień.
- Panel pracownika: kolejka zamówień, zmiana statusu (przyjęcie, realizacja, gotowe,
  dostawa, zakończenie, odrzucenie/anulowanie).
- Panel administratora: zarządzanie menu (w tym upload zdjęć), promocjami, raport
  sprzedaży z eksportem CSV, dashboard.
- Punkty lojalnościowe: naliczanie i wymiana przy checkout.
- Live-tracking statusu zamówienia (SignalR) dla gościa (token) i klienta (ownership).

## Struktura repo

```
src/
  PizzaShop.Domain/          — encje, reguły biznesowe, guard clauses
  PizzaShop.Application/     — CQRS (Commands/Queries), walidacja, porty (interfejsy)
  PizzaShop.Infrastructure/  — EF Core, PayU, geokodowanie, implementacje portów
  PizzaShop.Api/             — kontrolery, JWT, autoryzacja ról, SignalR, middleware
tests/
  PizzaShop.Domain.Tests/
  PizzaShop.Application.Tests/
  PizzaShop.Infrastructure.Tests/
  PizzaShop.Api.Tests/
frontend/                    — React + TypeScript (Vite)
docs/                        — model domenowy, warstwy Api/Application/Infrastructure,
                                decyzje architektoniczne (ADR-lite, docs/decisions.md)
```

Dokumentacja architektury (model domenowy, warstwy, ADR) żyje w `docs/` — zobacz
`docs/decisions.md` po pełny indeks decyzji.

## Uruchomienie lokalne

Wymagania: .NET 8 SDK, Node.js, PostgreSQL.

```bash
# Backend
dotnet ef database update -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet run --project src/PizzaShop.Api

# Frontend (w drugim terminalu)
cd frontend
npm install
npm run dev
```

Frontend (Vite, domyślnie `http://localhost:5173`) proxuje `/api`, `/hubs` i `/uploads`
do backendu (`http://localhost:5105` w dev).

## Testy

```bash
dotnet test                 # backend (xUnit)
cd frontend && npm run test # frontend (Vitest)
```

CI (`.github/workflows/ci.yml`) uruchamia `dotnet build` + `dotnet test` na push/PR do
`main`.
