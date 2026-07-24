# Decyzje architektoniczne (ADR-lite)

Format: każdy wpis ma **Kontekst → Decyzja → Konsekwencje**.
Wpisy są dopisywane, nie nadpisywane. Numeracja rosnąca.

Pełna treść każdego ADR żyje w osobnym pliku w `docs/adr/` (`docs/adr/ADR-NNNN.md`).
Ten plik to wyłącznie indeks — **przy dopisywaniu nowego ADR dodaj tu jedną linię i
utwórz `docs/adr/ADR-NNNN.md`**, nie dopisuj treści bezpośrednio tutaj.

---

## Indeks

- [ADR-0001](adr/ADR-0001.md): Baza danych — PostgreSQL
- [ADR-0002](adr/ADR-0002.md): Dostawca płatności — PayU (tryb Sandbox na start)
- [ADR-0003](adr/ADR-0003.md): Model jednej lokalizacji (single-tenant)
- [ADR-0004](adr/ADR-0004.md): Role użytkowników — Customer / Employee / RestaurantAdmin / SuperAdmin
- [ADR-0005](adr/ADR-0005.md): Tożsamość (konto) vs. profil domenowy; zamówienie gościa
- [ADR-0006](adr/ADR-0006.md): Obszar dostawy jako promień od restauracji
- [ADR-0007](adr/ADR-0007.md): Płatność i realizacja jako niezależne stany
- [ADR-0008](adr/ADR-0008.md): Zamówienia z wyprzedzeniem (scheduling) i EstimatedReadyAt
- [ADR-0009](adr/ADR-0009.md): Punkty lojalnościowe jako elastyczny szkielet
- [ADR-0010](adr/ADR-0010.md): Czas w UTC (DateTimeOffset), znaczniki timestamptz
- [ADR-0011](adr/ADR-0011.md): BuyXGetY — wyliczenie rabatu odłożone
- [ADR-0012](adr/ADR-0012.md): Struktura warstwy Application — CQRS z cienką własną abstrakcją (bez MediatR)
- [ADR-0013](adr/ADR-0013.md): Kształt IPaymentGateway i przepływ potwierdzenia płatności PayU
- [ADR-0014](adr/ADR-0014.md): ILoyaltyPolicy — polityka naliczania/wymiany punktów w Application
- [ADR-0015](adr/ADR-0015.md): Dostęp do konfiguracji Restaurant przez repozytorium (pojedynczy rekord)
- [ADR-0016](adr/ADR-0016.md): Edycja wariantów MenuItem — jawny SetDefaultVariant zamiast auto-promocji
- [ADR-0017](adr/ADR-0017.md): Reguły zależne od roli/kontekstu w Application — `ForbiddenOperationException` (403), nie reużycie `ValidationException` ani przeciek roli do Domain
- [ADR-0018](adr/ADR-0018.md): Domknięcie płatności — refund przy anulowaniu, persystencja `ProviderPaymentReference`, klasyfikacja wyjątku „konflikt stanu wykryty w Application" i zakres płatności gościa
- [ADR-0019](adr/ADR-0019.md): Edycja Promotion — celowe metody `UpdateWindow`/`UpdateValue`/`UpdateUsageLimit`; `Type` niemutowalny; `UsageLimit` poniżej `UsageCount` dozwolony
- [ADR-0020](adr/ADR-0020.md): Strategia mapowania EF Core — DbContext, konfiguracje per agregat, mapowanie Value Objectów, konstruktory perystencyjne w Domain
- [ADR-0021](adr/ADR-0021.md): Dane sidecar (`GuestTrackingToken`, `ProviderPaymentReference`) jako shadow properties na tabeli `Orders`
- [ADR-0022](adr/ADR-0022.md): Implementacja PayU w Infrastructure (OAuth, inicjalizacja, weryfikacja podpisu, mapowanie statusów, idempotentny refund)
- [ADR-0023](adr/ADR-0023.md): Geokodowanie — Nominatim (OSM) jako implementacja `IGeocodingService`
- [ADR-0024](adr/ADR-0024.md): Granica kompozycji — które porty implementuje Infrastructure, a które Api (SignalR i `ICurrentUser` w Api)
- [ADR-0025](adr/ADR-0025.md): Migracje EF Core, design-time factory i strategia testów integracyjnych (Testcontainers PostgreSQL)
- [ADR-0026](adr/ADR-0026.md): Tożsamość i uwierzytelnianie — własny `UserAccount` + BCrypt (nie ASP.NET Core Identity), JWT, powiązanie konta z `Customer`
- [ADR-0027](adr/ADR-0027.md): Warstwa Api — middleware wyjątków (ProblemDetails), autoryzacja ról z jawną hierarchią, cienkie kontrolery, webhook PayU z surowym body
- [ADR-0028](adr/ADR-0028.md): SignalR live-tracking — `OrderTrackingHub` w Api, grupy per `OrderId`, subskrypcja gościa przez token i zalogowanego przez ownership
- [ADR-0029](adr/ADR-0029.md): Powiązanie `Customer` ↔ `LoyaltyAccount` jednokierunkowe (FK na `LoyaltyAccount.CustomerId`) — usunięcie cyklu tworzenia, odrzucenie opcjonalnego `Guid? id` w fabrykach
- [ADR-0030](adr/ADR-0030.md): Reconciliacja route-id vs. body-id w kontrolerach mutujących — route jako jedyne źródło prawdy (nadpisanie), bez guardu `BadRequest()`
- [ADR-0031](adr/ADR-0031.md): Addendum do ADR-0028 — `NoopOrderNotifier` w Iteracji 3; live-tracking (SignalR) realnie nieaktywny do Iteracji 4
- [ADR-0032](adr/ADR-0032.md): `HubHttpContextFilter` (`IHubFilter`) re-kotwiczy `IHttpContextAccessor.HttpContext` na czas wywołania metody Huba — naprawa cichej utraty `ICurrentUser` w SignalR
- [ADR-0033](adr/ADR-0033.md): Finalizacja przelicznika punktów lojalnościowych (domknięcie ADR-0009/ADR-0014)
- [ADR-0034](adr/ADR-0034.md): Implementacja promocji BuyXGetY — konfiguracja `BuyXGetYRule`, `OrderDiscountContext`, nowa sygnatura `CalculateDiscount` (domknięcie ADR-0011)
- [ADR-0035](adr/ADR-0035.md): Frontend — React + TypeScript (Vite) w `frontend/`, MVP katalog+koszyk, ręczne typy TS, koszyk client-side (localStorage), nazwana polityka CORS
- [ADR-0036](adr/ADR-0036.md): Frontend — iteracja checkout jako gość (wizard jednostronicowy + osobna trasa potwierdzenia, mapping koszyk→CreateOrder, walidacja ręczna, obsługa ProblemDetails)
- [ADR-0037](adr/ADR-0037.md): Frontend — iteracja auth (logowanie/rejestracja klienta), token w `localStorage`, `AuthContext`, brak zmian backendowych
- [ADR-0038](adr/ADR-0038.md): Frontend — live-tracking statusu zamówienia (SignalR), hook `useOrderTracking`, nowa trasa `/orders/track/:trackingToken`, świadome odłożenie Vitest
- [ADR-0039](adr/ADR-0039.md): Panel "Moje konto" — nowy `GET /api/orders/mine` (historia zamówień klienta), sortowanie historii punktów w istniejącym `GET /api/loyalty/balance` (bez nowego endpointu), `PointsRedeemed` w checkoucie świadomie odłożone
- [ADR-0040](adr/ADR-0040.md): Wykorzystanie punktów lojalnościowych w checkoucie (`PointsToRedeem`) — UI dla zalogowanego klienta, guard clause w Domain przeciw rabatowi ponad wartość zamówienia, optymistyczna współbieżność (`xmin`) na `LoyaltyAccount`, zwrot punktów przy anulowaniu/odrzuceniu zamówienia

---

## ADR Notes

Log wykorzystania ADR-ów per zadanie — **dopisywany po każdym zadaniu**, nowe wpisy na
górze. `docs/adr/ADR-NNNN.md` pozostaje jedynym źródłem prawdy o treści decyzji; ten log
to wyłącznie historia **które ADR-y okazały się istotne dla którego zadania i z jakim
skutkiem**. Sprawdź go w pierwszej kolejności, zanim zaczniesz przeszukiwać `docs/adr/`
od nowa dla zadania w znanym już obszarze (patrz `CLAUDE.md` → „Zasady pracy z
kontekstem").

Szablon wpisu:

```
### YYYY-MM-DD — <krótki opis zadania>

**Wykorzystane ADR:**
- ADR-000X — <tytuł/temat>
  - <konkretny element decyzji użyty w zadaniu>

**Wpływ na implementację:**
- <co powstało/zmieniło się w kodzie, albo że nie zmieniło się nic>

**Przeczytane, nieużyte:**
- ADR-000Y — <dlaczego sprawdzony, ale ostatecznie nieistotny dla tego zadania>
```

---

### 2026-07-24 — Panel admina: zarządzanie kontami pracowników (/admin/staff)

**Wykorzystane ADR:**
- ADR-0004 — Role użytkowników (Customer/Employee/RestaurantAdmin/SuperAdmin)
  - `GetStaffAccountsQueryHandler` filtruje `UserRole.Customer` — panel pokazuje wyłącznie
    konta personelu.
- ADR-0017 — `ForbiddenOperationException` dla reguł zależnych od roli/kontekstu w Application
  - Bez zmian w kodzie: istniejący `RegisterStaffAccountCommandHandler` (RestaurantAdmin może
    tworzyć tylko Employee, SuperAdmin — dowolną rolę personelu) ponownie użyty bez modyfikacji.
- ADR-0027 — Autoryzacja ról z jawną hierarchią
  - Nowy `GET /api/auth/staff` pod `[Authorize(Roles = AuthRoles.Admin)]`, analogicznie do
    istniejącego `POST /api/auth/staff`.

**Wpływ na implementację:**
- Backend: `IUserAccountRepository.GetAllAsync`, `UserAccountDto` (bez `PasswordHash`),
  `GetStaffAccountsQuery`/Handler, `GET /api/auth/staff` w `AuthController`. Dopisany wiersz w
  `docs/api-layer.md` §6.1.
- Frontend: `/admin/staff` (lista + formularz tworzenia), reużywa istniejący
  `POST /api/auth/staff`. Selektor roli w UI celowo ogranicza się do
  Employee/RestaurantAdmin (SuperAdmin niedostępny w formularzu jako dodatkowe
  zabezpieczenie warstwy UI — realna reguła i tak żyje w handlerze).
- Żadna migracja EF Core ani nowa reguła domenowa nie była potrzebna.

**Przeczytane, nieużyte:**
- ADR-0026 — sprawdzony (kontekst `UserAccount`/JWT), ale nie wniósł nic ponad już
  istniejący kod, który tylko reużyto.

---

### 2026-07-24 — Wykorzystanie punktów lojalnościowych w checkoucie

**Wykorzystane ADR:**
- ADR-0033 — Finalizacja przelicznika punktów lojalnościowych
  - `LinearLoyaltyPolicy.RedemptionValuePerPoint` (0,05 PLN/punkt) ponownie użyty jako
    autorytatywne źródło przeliczenia po stronie serwera; front (`LoyaltyPointsField.tsx`)
    mirroruje tę samą stałą wyłącznie do UX, bez własnej logiki biznesowej
  - potwierdzone: brak % limitu redemption per zamówienie pozostaje w mocy — nowy guard
    z ADR-0040 to poprawność matematyczna (rabat ≤ pozostała kwota), nie nowa polityka
- ADR-0036 — Checkout jako gość
  - guest checkout bez zmian: `[AllowAnonymous] POST /api/orders`, `pointsToRedeem: null`
    dla niezalogowanego klienta; redemption wymaga zalogowania (jak przewidziano w ADR-0039)
- ADR-0039 — Panel "Moje konto"
  - decyzja (C) zrealizowana w tym zadaniu jako ADR-0040; nowy typ transakcji `Reversed`
    wpięty w istniejące sortowanie/wyświetlanie historii punktów (`MyAccountPage.tsx`)
- ADR-0012 — Guard clauses w Domain zamiast FluentValidation dla reguł stanowych
  - wzorzec zastosowany w nowym guard clause `Order.RedeemLoyaltyPoints` (limit rabatu)
    oraz w `LoyaltyAccount.Reverse`
- ADR-0018 — wzorzec obsługi refundu przy anulowaniu
  - wzorzec (jedna transakcja DB, atomowa zmiana statusu + efekt uboczny) powtórzony dla
    zwrotu punktów w `CancelOrderCommandHandler`/`RejectOrderCommandHandler`

**Wpływ na implementację:**
- Nowy ADR-0040 (`docs/adr/ADR-0040.md`): decyzja architektoniczna — UI wyboru punktów
  w checkoucie tylko dla zalogowanego klienta, `LoyaltyRedemptionExceedsOrderValueException`
  (422) jako nowy guard clause w `Order.RedeemLoyaltyPoints`, optymistyczna współbieżność
  (`UseXminAsConcurrencyToken()`) na `LoyaltyAccount` + `DbUpdateConcurrencyException` → 409,
  nowa metoda `LoyaltyAccount.Reverse(...)` + `LoyaltyTransactionType.Reversed` wołane z
  `CancelOrderCommandHandler`/`RejectOrderCommandHandler` (dotąd punkty ginęły bezpowrotnie
  przy anulowaniu/odrzuceniu zamówienia z `PointsRedeemed > 0` — realna luka zamknięta).
- Backend: `Order.cs`, `LoyaltyAccount.cs`, `LoyaltyTransactionType.cs`, nowy
  `LoyaltyRedemptionExceedsOrderValueException.cs`, `LoyaltyAccountConfiguration.cs` (+
  migracja `AddLoyaltyAccountConcurrencyToken`, zweryfikowana przez reviewera jako no-op
  względem realnego SQL — Npgsql pomija DDL dla systemowej kolumny `xmin`),
  `ExceptionHandler.cs`, `CancelOrderCommandHandler.cs`, `RejectOrderCommandHandler.cs`.
  `CreateOrderCommand`/`CreateOrderCommandHandler` (krok 7, redemption przy tworzeniu
  zamówienia) już istniały z wcześniejszej iteracji i nie wymagały zmian — IDOR
  zweryfikowany: `CustomerId` pochodzi wyłącznie z `ICurrentUser`, nie z payloadu.
- Frontend: nowy `components/checkout/LoyaltyPointsField.tsx` (widoczny tylko dla
  zalogowanego klienta z saldem > 0), integracja w `OrderSummary.tsx`/`CheckoutPage.tsx`
  (front przestaje wysyłać `pointsToRedeem: null` na sztywno; reset pola przy 422/409),
  etykieta `Reversed` w `MyAccountPage.tsx`.
- Reviewer: brak błędów blokujących (zgodność z ADR-0040 potwierdzona punkt po punkcie,
  brak IDOR, migracja `xmin` zweryfikowana empirycznie jako no-op). Doprosił o dwa
  brakujące testy graniczne (`discountAmount == remainingPayable`, `Reverse` z wartością
  ujemną) i aktualizację nieaktualnego komentarza XML nad `RedeemLoyaltyPoints` — poprawki
  wykonane przez buildera, `dotnet test` dla `PizzaShop.Domain.Tests`: 243/243.
- `dotnet build`/`dotnet test` (Domain.Tests 243/243, Application.Tests 279/279,
  Api.Tests 109/109) i `npm run build`/`npm run lint` (frontend) — PASS.
  `PizzaShop.Infrastructure.Tests` pominięte (środowisko bez Dockera, niezwiązane z tym
  zadaniem). Świadomie NIE dodano auto-anulowania zamówienia przy `PaymentStatus.Failed`
  — poza zakresem, ryzyko rezydualne opisane w ADR-0040.

**Przeczytane, nieużyte:**
- brak — zadanie dotyczyło wyłącznie obszaru ADR-0033/ADR-0036/ADR-0039/ADR-0012/ADR-0018/
  nowy ADR-0040

---

### 2026-07-24 — Panel "Moje konto": historia zamówień + historia punktów lojalnościowych

**Wykorzystane ADR:**
- ADR-0033 — Finalizacja przelicznika punktów lojalnościowych
  - potwierdzenie, że `LoyaltyAccount.Transactions` (append-only) jest już kompletnym źródłem
    historii punktów — nie trzeba nowego mechanizmu domenowego, tylko odczyt + sortowanie
- ADR-0036 — Checkout jako gość
  - potwierdzenie, że `PointsRedeemed = null` w `CreateOrderCommand` z frontendu to świadoma,
    udokumentowana decyzja tamtego zadania, nie przeoczenie — punkt wyjścia do decyzji (C)
    w nowym ADR-0039 (wykorzystanie punktów w checkoucie zostaje osobną iteracją)
- ADR-0037 — Frontend auth (logowanie/rejestracja)
  - wzorzec `AuthContext`/`useAuth`, `RedirectIfAuthenticated` jako szablon dla nowego
    `RequireAuth` (odwrotna logika: brak sesji → redirect do `/login`)

**Wpływ na implementację:**
- Nowy ADR-0039 (`docs/adr/ADR-0039.md`): backend dostaje nowy `GET /api/orders/mine`
  (`GetMyOrdersQuery`/`GetMyOrdersQueryHandler`, `OrderSummaryDto`,
  `IOrderRepository.GetByCustomerIdAsync`, bez paginacji) — ale historia punktów **nie**
  dostaje nowego endpointu: okazało się, że istniejący `GET /api/loyalty/balance` już zwracał
  pełną historię transakcji (`LoyaltyBalanceDto.Transactions`), tylko niesortowaną; jedyna
  zmiana backendu po stronie punktów to `LoyaltyMapper.ToDto` sortujący malejąco po
  `OccurredAt`. Reviewer nie zgłosił błędów blokujących (brak IDOR — `ICurrentUser.CustomerId`
  poprawnie scope'uje `/orders/mine`), tylko doprosił o e2e testy autoryzacji/IDOR dla nowego
  endpointu w `OrdersEndpointsTests.cs` (dwóch klientów, brak tokenu, rola Staff) — dopisane.
- Frontend: `frontend/src/api/loyaltyApi.ts` (nowy), `ordersApi.getMyOrders`, nowe typy w
  `api/types.ts` (`OrderSummary`, `LoyaltyBalance`, `LoyaltyTransaction`,
  `LoyaltyTransactionType`), `components/auth/RequireAuth.tsx` (nowy, analogiczny do
  `RedirectIfAuthenticated` ale odwrócony), trasa `/account` w `routes.tsx`, link "Moje konto"
  w `Layout.tsx`, nowa strona `pages/MyAccountPage.tsx`. `STATUS_LABELS` z
  `OrderTrackingStatus.tsx` wydzielony do osobnego `orders/orderStatusLabels.ts`, żeby uniknąć
  ostrzeżenia lintera (`react/only-export-components`) przy re-eksporcie stałej z pliku
  komponentu.
- Świadomie NIE dodano linku do widoku szczegółu zamówienia z listy "Moje konto" — apka nie ma
  jeszcze żadnej trasy szczegółu zamówienia dla zalogowanego właściciela (tylko
  `/orders/track/:token` dla gości i live-tracking tuż po checkoucie); lista pokazuje tylko
  podsumowanie (`OrderSummaryDto`).
- `PointsRedeemed` w checkoucie: potwierdzona rekomendacja architekta z ADR-0039 (C) —
  **osobna iteracja/ADR**, nie ruszane w tym zadaniu. Panel konta w tej iteracji tylko
  wyświetla saldo/historię.
- `dotnet build`/`dotnet test` (Application.Tests 275/275, Api.Tests 109/109) i
  `npm run build`/`npm run lint` (frontend) — PASS, bez nowych ostrzeżeń.
  `PizzaShop.Infrastructure.Tests` pominięte (środowisko bez Dockera, niezwiązane z tym
  zadaniem).

**Przeczytane, nieużyte:**
- brak — zadanie dotyczyło wyłącznie obszaru ADR-0033/ADR-0036/ADR-0037/nowy ADR-0039

---

### 2026-07-24 — Frontend: live-tracking statusu zamówienia (SignalR) na OrderConfirmationPage

**Wykorzystane ADR:**
- ADR-0028 — `OrderTrackingHub`, grupy per `OrderId`, `SubscribeToGuestOrder(trackingToken)` /
  `SubscribeToOrder(orderId)`, autoryzacja przy subskrypcji (cicha przy błędzie)
  - dobór metody Huba i wariantu REST (`GET /orders/{id}` vs `GET /orders/track/{token}`) 1:1 z
    wariantem `orderId`/`trackingToken` po stronie frontendu
  - JWT przekazany przez `accessTokenFactory` (query string `access_token`), nie nagłówek —
    zgodnie z konfiguracją `Program.cs`
- ADR-0032 — `HubHttpContextFilter` (fix backendowy `ICurrentUser` w Hubie)
  - brak wpływu na frontend poza potwierdzeniem, że `SubscribeToOrder` faktycznie działa dla
    zalogowanego właściciela; zweryfikowane, że frontend nie próbuje obchodzić/duplikować tego
    fixu (nie przekazuje tożsamości ręcznie)
- `docs/domain-model.md` sekcja 5.3 (graf `OrderStatus`) — komplet 8 wartości w
  `OrderTrackingStatus.tsx`, `Rejected`/`Cancelled` odróżnione wizualnie od `Completed`

**Wpływ na implementację:**
- Nowy ADR-0038 (`docs/adr/ADR-0038.md`): klient SignalR (`@microsoft/signalr`), hook
  `useOrderTracking` (jedno połączenie per komponent, `withAutomaticReconnect`), komponent
  prezentacyjny `OrderTrackingStatus`, nowa trasa publiczna `/orders/track/:trackingToken`
  (`TrackOrderPage`) — uzasadniona wymogiem "GUID w URL" z `CLAUDE.md` i tym, że
  `sessionStorage` nie przenosi się między kartami/urządzeniami gościa.
- `OrderConfirmationPage` zastąpiła placeholder tekstowy realnym live-trackingiem + klikalnym
  linkiem do trasy trackingu.
- Świadomie NIE wprowadzono Vitest/RTL (frontend nadal bez testów — spójne z ADR-0035/36/37).
- Po przeglądzie (reviewer) doprecyzowano `useOrderTracking` o wariant `source: null` (bez
  zbędnego fetchu, gdy nie ma czego śledzić) i usunięto z `OrderTrackingStatus` wyświetlanie
  surowej treści błędu REST na rzecz komunikatu ogólnego.
- `npm run build` (tsc + vite) i `npm run lint` (oxlint) — PASS, bez nowych ostrzeżeń.

**Przeczytane, nieużyte:**
- brak — zadanie dotyczyło wyłącznie obszaru ADR-0028/ADR-0032/domain-model 5.3

---

### 2026-07-23 — Podział `decisions.md` na `docs/adr/*.md`; weryfikacja checkoutu gościa

**Wykorzystane ADR:**
- ADR-0035 — Frontend React + TypeScript
  - struktura aplikacji frontendowej
  - koszyk w localStorage
- ADR-0036 — Checkout jako gość
  - mapowanie koszyka na CreateOrder
  - walidacja checkoutu
  - obsługa ProblemDetails

**Wpływ na implementację:**
- utworzono CheckoutPage
- wykorzystano localStorageCart
- dodano mapowanie do CreateOrderRequest
- (weryfikacja) checkout był już w pełni zaimplementowany w `ed79783` zgodnie z
  ADR-0036 — brak nowych zmian w kodzie frontendu w tym zadaniu

**Przeczytane, nieużyte:**
- brak — zadanie dotyczyło wyłącznie obszaru ADR-0035/ADR-0036
