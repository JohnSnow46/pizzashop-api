# PizzaShop

E-commerce app for a single-location pizzeria: menu catalog with variants, cart, guest or
account checkout, online payments via PayU, loyalty points, promotions (including
BuyXGetY), live order-status tracking over SignalR, and separate dashboards for
customers, employees, and admins.

Portfolio project — priorities: a working feature and fast iteration first, not
enterprise-scale architecture.

## Stack

- **Backend:** .NET 8 / ASP.NET Core, EF Core + PostgreSQL, Clean Architecture
  (`Domain` / `Application` / `Infrastructure` / `Api`), JWT, xUnit + Moq + FluentAssertions.
- **Frontend:** React + TypeScript (Vite), Vitest.
- **Payments:** PayU (sandbox).
- **Real-time:** SignalR (live order-status tracking).

## Features

- Menu catalog with product variants, search/filtering.
- Cart (client-side) and checkout: guest or logged-in customer, delivery address with
  area verification (radius from the restaurant), pickup, scheduled fulfillment time.
- PayU payments (initialization, confirmation webhook, guest payment retry).
- Roles: `Customer` / `Employee` / `RestaurantAdmin` / `SuperAdmin`, with a permission
  hierarchy.
- Employee dashboard: order queue, status transitions (accept, start delivery, mark
  ready, complete, reject/cancel).
- Admin dashboard: menu management (including image upload), promotions, sales report
  with CSV export, overview dashboard.
- Loyalty points: earning and redemption at checkout.
- Live order-status tracking (SignalR) for guests (via token) and customers (via
  ownership).

## Repo structure

```
src/
  PizzaShop.Domain/          — entities, business rules, guard clauses
  PizzaShop.Application/     — CQRS (Commands/Queries), validation, ports (interfaces)
  PizzaShop.Infrastructure/  — EF Core, PayU, geocoding, port implementations
  PizzaShop.Api/             — controllers, JWT, role authorization, SignalR, middleware
tests/
  PizzaShop.Domain.Tests/
  PizzaShop.Application.Tests/
  PizzaShop.Infrastructure.Tests/
  PizzaShop.Api.Tests/
frontend/                    — React + TypeScript (Vite)
docs/                        — domain model, Api/Application/Infrastructure layers,
                                architecture decisions (ADR-lite, docs/decisions.md)
```

Architecture documentation (domain model, layers, ADRs) lives in `docs/` — see
`docs/decisions.md` for the full decision index. It's written in Polish, alongside the
project's Claude Code instructions (`CLAUDE.md`).

## Running locally

Requirements: .NET 8 SDK, Node.js, PostgreSQL.

```bash
# Backend
dotnet ef database update -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet run --project src/PizzaShop.Api

# Frontend (in a second terminal)
cd frontend
npm install
npm run dev
```

The frontend (Vite, default `http://localhost:5173`) proxies `/api`, `/hubs`, and
`/uploads` to the backend (`http://localhost:5105` in dev).

## Tests

```bash
dotnet test                 # backend (xUnit)
cd frontend && npm run test # frontend (Vitest)
```

CI (`.github/workflows/ci.yml`) runs `dotnet build` + `dotnet test` on push/PR to `main`.
