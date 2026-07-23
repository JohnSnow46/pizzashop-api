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
