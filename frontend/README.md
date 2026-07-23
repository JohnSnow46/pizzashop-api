# PizzaShop frontend (MVP: katalog + koszyk)

React + TypeScript + Vite, poza solucją .NET (osobny toolchain npm) — patrz `docs/decisions.md`
ADR-0035. Zakres MVP: przeglądanie menu i budowanie koszyka client-side (localStorage). Bez
checkoutu, logowania, płatności i live-trackingu — to przyszłe iteracje (miejsce w routingu
zostawione w `src/routes.tsx`).

## Uruchomienie lokalne

1. Backend: `dotnet run --project ../src/PizzaShop.Api` (domyślnie `http://localhost:5105`,
   patrz `src/PizzaShop.Api/Properties/launchSettings.json`, profil `http`).
2. Frontend:
   ```bash
   npm install
   npm run dev
   ```
   Otwiera się na `http://localhost:5173`. `vite.config.ts` proxy'uje `/api/*` do Api pod
   `http://localhost:5105`, więc w dev nie potrzeba CORS-a (choć polityka `"frontend"` w
   `Program.cs` i tak istnieje dla scenariuszy bez proxy, np. build produkcyjny serwowany z
   innego origin niż Api).

Jeśli Api działa pod innym portem/adresem, zmień `target` w `vite.config.ts` (`server.proxy`).

## Build produkcyjny

```bash
npm run build
```

## Struktura

- `src/api/` — ręczne typy TS (mirror DTO Api, `types.ts`), cienki fetch wrapper (`client.ts`),
  wołania do `/api/menu` (`menuApi.ts`).
- `src/hooks/` — `useMenu` (ładowanie listy menu), `useCart` (dostęp do kontekstu koszyka).
- `src/cart/` — `CartContext.tsx` (Context + `useReducer`), `cartStorage.ts` (persystencja w
  `localStorage`), `types.ts` (kształt pozycji koszyka).
- `src/components/`, `src/pages/`, `src/routes.tsx` — UI i routing (`react-router-dom`).

Ceny liczone po stronie klienta w koszyku są **wyłącznie orientacyjne** — źródłem prawdy dla
cen pozostaje Api (przyszły `CreateOrderCommand` przy checkoucie przeliczy wszystko od nowa).
