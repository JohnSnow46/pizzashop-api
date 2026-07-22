# Warstwa Application — projekt CQRS (wersja 1)

Projekt warstwy `PizzaShop.Application`. Zależy **tylko** od `Domain`. Definiuje use case'y
(CQRS: Commands/Queries), interfejsy portów (repozytoria, serwisy zewnętrzne, polityki),
DTO i walidatory. Implementacje portów żyją w `Infrastructure`/`Api` (Clean Architecture).

Powiązane decyzje: ADR-0012 (kształt CQRS), ADR-0013 (`IPaymentGateway`), ADR-0014
(`ILoyaltyPolicy`), ADR-0015 (dostęp do `Restaurant`), ADR-0017 (hierarchia wyjątków
Application i mapowanie na HTTP), ADR-0018 (domknięcie płatności: refund, persystencja
`ProviderPaymentReference`, `ConflictException`, zakres płatności gościa). Model domenowy:
`docs/domain-model.md`.

---

## 1. Zasady warstwy

- **CQRS**: `Command` = zmiana stanu (zwraca minimum, np. `Id`/`Result`); `Query` = odczyt
  (zwraca DTO). Każdy handler w osobnym pliku (CLAUDE.md).
- **Dyspozytor + pipeline** (ADR-0012): `ICommandHandler<,>` / `IQueryHandler<,>`,
  `ValidationBehavior` uruchamia `IValidator<T>` (FluentValidation) przed handlerem.
- **Podział odpowiedzialności walidacji** (CLAUDE.md):
  - `IValidator<T>` (FluentValidation) — **kształt danych**: wymagane pola, formaty, zakresy,
    dodatnie ilości. NIE dotyka stanu bazy ani reguł biznesowych.
  - **Domain** — reguły zależne od stanu: obszar dostawy, godziny pracy, przejścia statusów,
    min. wartość, min. składnik pizzy, saldo punktów. Handler orkiestruje, Domain decyduje.
- **Handlery są cienkie**: ładują agregaty przez repozytoria, wołają metody Domain, zapisują.
  Nie zawierają logiki biznesowej, którą powinno egzekwować Domain.
- **Autoryzacja ról** (Customer/Employee/RestaurantAdmin/SuperAdmin) egzekwowana w `Api`
  (policy na endpoint), NIE w handlerach (ADR-0004). Handler może dostać już zweryfikowany
  `CustomerId`/`UserAccountId` z kontekstu (przez `ICurrentUser`).
  - **Wyjątek: reguły autoryzacyjne zależne od stanu** (np. „klient anuluje tylko przed
    akceptacją") wymagają załadowanego agregatu, więc żyją w handlerze, nie w policy Api —
    sygnalizowane `ForbiddenOperationException` (sekcja 5, ADR-0017). Domain nadal nie zna
    ról: egzekwuje tylko reguły uniwersalne (np. `Order.Cancel()` blokuje stany terminalne).

---

## 2. Struktura folderów (proponowana)

```
PizzaShop.Application/
  Common/
    Messaging/           # ICommand, IQuery, ICommandHandler<,>, IQueryHandler<,>, dyspozytor
    Behaviors/           # ValidationBehavior
    Exceptions/          # ValidationException, NotFoundException, ForbiddenOperationException, ConflictException, InvalidPaymentNotificationException (Application-level)
    Abstractions/        # ICurrentUser, IUnitOfWork, IClock
    Dtos/                # współdzielone DTO między modułami (MoneyDto, AddressDto, GeoCoordinateDto)
  Abstractions/
    Persistence/         # interfejsy repozytoriów
    Payments/            # IPaymentGateway + DTO płatności
    Geocoding/           # IGeocodingService
    Loyalty/             # ILoyaltyPolicy
    Realtime/            # IOrderNotifier (SignalR port)
  Catalog/
    Queries/  Commands/  Validators/  Dtos/
  Orders/
    Queries/  Commands/  Validators/  Dtos/
  Restaurant/
    Queries/  Commands/  Validators/  Dtos/
  Payments/
    Commands/  Dtos/
  Promotions/
    Queries/  Commands/  Validators/  Dtos/
  Loyalty/
    Queries/  Dtos/
```

> `Common/Dtos/` mieści DTO odwzorowujące wspólne value objecty Domenowe (`Money`, `Address`,
> `GeoCoordinate`), używane przez Queries wielu modułów — Queries nigdy nie zwracają encji/VO
> Domain bezpośrednio (patrz sekcja 4). DTO specyficzne dla jednego modułu żyją w jego
> lokalnym `Dtos/` (np. `Catalog/Dtos/`), nie w `Common/Dtos/`.

---

## 3. Porty (interfejsy definiowane w Application, implementowane w Infrastructure)

### 3.1 Repozytoria
Nazwane pod faktyczne potrzeby handlerów — bez metod na zapas.

- `IRestaurantRepository` — `GetAsync(ct)` (jedyny rekord, ADR-0015), `UpdateAsync(restaurant, ct)`.
- `IMenuItemRepository` — `GetByIdAsync(id, ct)`, `GetManyByIdsAsync(ids, ct)` (dla koszyka),
  `GetMenuAsync(ct)` (dostępne pozycje do menu), `AddAsync`, `UpdateAsync`.
- `IIngredientRepository` — `GetByIdAsync`, `GetManyByIdsAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`.
- `IOrderRepository` — `GetByIdAsync(id, ct)`, `GetByGuestTrackingTokenAsync(token, ct)`,
  `GetQueueAsync(...)` (kolejka dla obsługi), `AddAsync`, `UpdateAsync`, `NextOrderNumberAsync`
  (generacja czytelnego `Number`) oraz **para metod referencji płatności** (ADR-0018):
  - `AddAsync(Order order, Guid? guestTrackingToken, string? providerPaymentReference, ct)` —
    referencja bramki (`PaymentInitResult.ProviderPaymentReference`) zapisywana razem z nowym
    zamówieniem; `null` dla `OnPickup`. Referencja jest znana już przed `AddAsync`, bo
    `CreateOrderCommand` woła `InitializePaymentAsync` w kroku 8, przed persystencją (4.3.1).
  - `SetProviderPaymentReferenceAsync(Guid orderId, string providerPaymentReference, ct)` —
    zapis referencji dla **istniejącego** zamówienia (ścieżka ponowienia płatności przez
    `InitializePaymentCommand`). Nie commituje sama — commit robi `IUnitOfWork`.
  - `GetProviderPaymentReferenceAsync(Guid orderId, ct) → string?` — odczyt referencji do
    refundu przy anulowaniu (`CancelOrderCommand`).
  > Referencja to sidecar persystencji (kolumna obok `Order`), analogiczny do
  > `guestTrackingToken` — Domain jej nie zna (ADR-0002/ADR-0018), `Order` bez zmian.
- `ICustomerRepository` — `GetByIdAsync`, `GetByUserAccountIdAsync`, `AddAsync`, `UpdateAsync`.
- `ILoyaltyAccountRepository` — `GetByCustomerIdAsync`, `AddAsync`, `UpdateAsync`.
- `IPromotionRepository` — `GetByIdAsync`, `GetByCodeAsync(code, ct)`, `GetActiveAutomaticAsync`,
  `GetAllAsync`, `AddAsync`, `UpdateAsync`.
- `IUnitOfWork` — `SaveChangesAsync(ct)` (transakcyjne domknięcie; np. `Order` + `Promotion.RecordUsage`
  + `LoyaltyAccount.Redeem` w jednej transakcji).

### 3.2 Serwisy / polityki
- `IGeocodingService` — `GeocodeAsync(Address) → GeoCoordinate?` (ADR-0006). Zwraca współrzędne
  do walidacji obszaru dostawy; Domain dostaje gotowy `GeoCoordinate`.
- `IPaymentGateway` — ADR-0013 (`InitializePaymentAsync`, `VerifyAndParseNotification`, `RefundAsync`).
  - `InitializePaymentAsync → PaymentInitResult(RedirectUrl, ProviderPaymentReference)` —
    obie wartości są używane: `RedirectUrl` wraca do klienta, `ProviderPaymentReference`
    zapisywana przez `IOrderRepository` (sekcja 3.1, ADR-0018).
  - `RefundAsync(PaymentRefundRequest)` — **musi być idempotentny per zamówienie/referencja**:
    powtórny zwrot już zrefundowanego zamówienia = sukces, nie błąd (ADR-0018; analogicznie
    do idempotencji potwierdzenia notyfikacji, ADR-0013). To pokrywa ryzyko podwójnego zwrotu
    przy ponowieniu operacji anulowania.
- `ILoyaltyPolicy` — ADR-0014 (`CalculatePointsToEarn`, `CalculateRedemptionValue`, `MaxRedeemablePoints`).
- `IOrderNotifier` — port do live-trackingu (SignalR w Api): `OrderStatusChangedAsync(orderId, status, estimatedReadyAt)`.
- `ICurrentUser` — `UserAccountId?`, `CustomerId?`, `Role` z kontekstu żądania (Api dostarcza).
- `IClock` — `DateTimeOffset UtcNow` (testowalność czasu; ADR-0010).

---

## 4. Use case'y (Commands / Queries)

### 4.1 Katalog (MenuItem, Ingredient)
| Use case | Typ | Rola | Uwagi / reguły |
|---|---|---|---|
| `GetMenuQuery` | Query | anonim/wszyscy | Zwraca dostępne pozycje (`IsAvailable`) z wariantami, cenami, dozwolonymi dodatkami. |
| `GetMenuItemByIdQuery` | Query | anonim | Szczegóły pozycji (konfigurator pizzy). |
| `CreateMenuItemCommand` | Command | RestaurantAdmin | Tworzy `MenuItem`; po zbudowaniu `EnsureValidCatalogConfiguration()` (min. 1 składnik dla pizzy, 1 domyślny wariant). |
| `UpdateMenuItemCommand` | Command | RestaurantAdmin | Rename, opis, cena, dostępność, warianty, składniki/dodatki. |
| `SetMenuItemAvailabilityCommand` | Command | RestaurantAdmin (Employee?) | `MarkAvailable`/`MarkUnavailable`. |
| `CreateIngredientCommand` | Command | RestaurantAdmin | Słownik składników/dodatków. |
| `UpdateIngredientCommand` | Command | RestaurantAdmin | Cena dodatku, dostępność. |

### 4.2 Restaurant (konfiguracja)
| Use case | Typ | Rola | Uwagi |
|---|---|---|---|
| `GetRestaurantConfigQuery` | Query | wszyscy (część publiczna) | Godziny, adres, promień, progi — do prezentacji. |
| `UpdateOpeningHoursCommand` | Command | RestaurantAdmin | `Restaurant.UpdateOpeningHours`. |
| `UpdateDeliveryAreaCommand` | Command | RestaurantAdmin | `UpdateDeliveryArea` (lokalizacja + promień). |
| `UpdateOrderingThresholdsCommand` | Command | RestaurantAdmin | Min. wartość, próg darmowej dostawy, opłata. |
| `ToggleAcceptingOrdersCommand` | Command | RestaurantAdmin/Employee | `Start/StopAcceptingOrders`. |

### 4.3 Zamówienie (Order) — rdzeń
| Use case | Typ | Rola | Uwagi / reguły |
|---|---|---|---|
| `CheckDeliveryAvailabilityQuery` | Query | anonim | Krok flow 2: geokoduje adres (`IGeocodingService`) i sprawdza `Restaurant.IsWithinDeliveryArea`. Zwraca tak/nie + opłatę. |
| `CreateOrderCommand` | Command | anonim (gość) / Customer | Rdzeń — patrz 4.3.1. |
| `GetOrderByIdQuery` | Query | Customer (własne) / obsługa | Dostęp zalogowanego właściciela lub obsługi. |
| `GetOrderByTrackingTokenQuery` | Query | anonim (gość) | Bezpieczny dostęp gościa przez nieodgadnalny GUID (flow 3). Brak JWT — autoryzacja = posiadanie tokenu. |
| `GetOrderQueueQuery` | Query | Employee+ | Kolejka przychodzących zamówień dla obsługi. |
| `AcceptOrderCommand` | Command | Employee+ | `Order.Accept()` (dla Online wymaga `Paid` — ADR-0007); może ustawić `EstimatedReadyAt`. |
| `RejectOrderCommand` | Command | Employee+ | `Order.Reject()`. |
| `AdvanceOrderStatusCommand` | Command | Employee+ | `StartPreparation`/`MarkReady`/`StartDelivery`/`Complete` (jeden command z docelowym statusem lub osobne — patrz 4.3.2). Po zmianie: `IOrderNotifier`. |
| `SetEstimatedReadyAtCommand` | Command | Employee+ | `Order.SetEstimatedReadyAt` (od `Accepted` wzwyż, nie w przeszłości). Notyfikacja SignalR. |
| `CancelOrderCommand` | Command | Customer (przed Accepted) / obsługa | `Order.Cancel()`; regułę „klient anuluje tylko przy `PendingAcceptance`" egzekwuje handler → `ForbiddenOperationException` (403, ADR-0017), NIE Domain. **Refund online (ADR-0018):** jeśli `PaymentMethod == Online && PaymentStatus == Paid` ⇒ ścieżka `GetProviderPaymentReferenceAsync` → `IPaymentGateway.RefundAsync` → `Order.RefundPayment()`. Orkiestracja i obsługa awarii: 4.3.3. |

#### 4.3.1 `CreateOrderCommand` — orkiestracja (najważniejszy handler)
Wejście (DTO): `CustomerId?` (z `ICurrentUser`, null = gość), `Contact` (imię/telefon/email),
`FulfillmentType`, `DeliveryAddress?` (adres surowy, do geokodowania), `Items[]`
(`MenuItemId`, `VariantId?`, `Quantity`, `ExtraIngredientIds[]`, `Notes?`),
`RequestedFulfillmentTime?`, `PaymentMethod`, `PromotionCode?`, `PointsToRedeem?`.

Walidator (kształt): Contact wymagany, telefon/format email, `Items` niepuste, `Quantity >= 1`,
`Delivery ⇒ DeliveryAddress != null` (kształtowo; reguła obszaru w Domain), `PointsToRedeem >= 0`.

Kroki handlera:
1. `IRestaurantRepository.GetAsync` (ADR-0015).
2. Dla `Delivery`: `IGeocodingService.GeocodeAsync(address)` → `GeoCoordinate` (jeśli null ⇒
   `NotFoundException`/błąd walidacji adresu). Buduje `DeliveryAddress` VO.
3. `IMenuItemRepository.GetManyByIdsAsync(ids)`; dla każdej pozycji koszyka:
   `menuItem.ResolvePrice(variantId)` + `menuItem.EnsureExtraAllowed(ingredient)` dla każdego
   dodatku → buduje `OrderItem` (snapshoty nazw/cen — Domain wylicza `LineTotal`).
4. `IOrderRepository.NextOrderNumberAsync` → `Number`.
5. `Order.Create(...)` z `restaurant` jako kolaboratorem — Domain egzekwuje niezmienniki 5.4
   (min. pozycja, adres w obszarze, min. wartość, godziny pracy, opłata dostawy).
6. Promocja (jeśli `PromotionCode`): patrz 4.5 — `IsQualifiedFor` → `CalculateDiscount` →
   `Order.ApplyPromotion` + `Promotion.RecordUsage`.
7. Punkty (jeśli `PointsToRedeem` i `CustomerId != null`): `ILoyaltyPolicy.CalculateRedemptionValue`
   → `Order.RedeemLoyaltyPoints` + `LoyaltyAccount.Redeem` (saldo sprawdza Domain).
8. Płatność: `Online` ⇒ `IPaymentGateway.InitializePaymentAsync` → `PaymentInitResult`
   (`RedirectUrl` + `ProviderPaymentReference`); referencję przekazujemy do `AddAsync`
   (krok 9), `RedirectUrl` do wyniku (krok 10). `OnPickup` ⇒ brak wywołania bramki,
   referencja `null`, zamówienie od razu gotowe do przyjęcia (ADR-0007).
9. `IOrderRepository.AddAsync(order, guestTrackingToken, providerPaymentReference, ct)` +
   `IUnitOfWork.SaveChangesAsync` (jedna transakcja: Order + referencja + RecordUsage + Redeem).
10. Zwraca: `OrderId`, `Number`, `GuestTrackingToken` (dla gościa), `PaymentRedirectUrl?`.

**Uwaga o tokenie gościa:** nieodgadnalny GUID generowany dla zamówienia gościa (flow 3);
przechowywany na `Order` po stronie persystencji (kolumna) — to detal Infrastructure/Application,
nie zmienia Domain (Domain ma już `Id` typu Guid; token śledzenia to osobna wartość, żeby nie
eksponować klucza głównego). Token = osobne pole `GuestTrackingToken` (nie `Order.Id`), zapisany
razem z zamówieniem przez `IOrderRepository.AddAsync`. `ProviderPaymentReference` idzie tym samym
kanałem (ten sam `AddAsync`) — patrz 3.1 i ADR-0018.

#### 4.3.2 Przejścia statusu — jeden vs. wiele commandów
Rekomendacja: **osobne commandy** per przejście (`StartPreparationCommand`, `MarkReadyCommand`,
`StartDeliveryCommand`, `CompleteOrderCommand`) — czytelniejsze autoryzacyjnie i w API niż
jeden `AdvanceOrderStatusCommand` z enumem. Każdy woła odpowiednią metodę `Order` (guard clauses
w Domain pilnują legalności) i po sukcesie `IOrderNotifier`.

#### 4.3.3 `CancelOrderCommand` — orkiestracja refundu (ADR-0018)
Warunek refundu: `mustRefund = order.PaymentMethod == Online && order.PaymentStatus == Paid`.
Kolejność w handlerze:
1. `GetByIdAsync` → `NotFoundException` jeśli brak.
2. `EnsureAccessAllowed(order)` (staff dowolne / klient tylko własne, inaczej `NotFoundException`)
   → `EnsureCustomerCanStillCancel(order, isStaff)` (klient tylko przy `PendingAcceptance`,
   inaczej `ForbiddenOperationException`) — bez zmian względem ADR-0017.
3. Jeśli `mustRefund`: `reference = GetProviderPaymentReferenceAsync(order.Id)`; jeśli `null`
   ⇒ `InvalidOperationException` (naruszenie niezmiennika: opłacone online bez referencji —
   nie powinno wystąpić; mapowane na 500, wymaga interwencji), NIE cichy no-op.
4. `order.Cancel()` (Domain: reguła terminalna).
5. Jeśli `mustRefund`: `await IPaymentGateway.RefundAsync(new PaymentRefundRequest(order.Id, reference, order.Total), ct)`
   → następnie `order.RefundPayment()` (`Paid → Refunded`).
6. `IOrderRepository.UpdateAsync(order)` + `IUnitOfWork.SaveChangesAsync` — atomowo
   `Cancelled` + (ewentualnie) `Refunded`.
7. `IOrderNotifier.OrderStatusChangedAsync(order.Id, order.Status, order.EstimatedReadyAt)`.

**Awaria `RefundAsync`** (błąd sieci/bramki): wyjątek propaguje, krok 6 się nie wykonuje →
nic nie zapisane, zamówienie zostaje w poprzednim stanie (nie `Cancelled`), operacja do
ponowienia. Świadomie **brak** stanu „RefundPending" (ADR-0007 nienaruszony). Residualne
ryzyko (refund udany, `SaveChanges` pada) pokrywa idempotencja `RefundAsync` (3.2). Escalation
(outbox + `PaymentStatus.RefundPending`) to przyszły ADR, tylko jeśli dane operacyjne pokażą
realny problem. Przypadek `Authorized` (nie `Paid`) przy anulowaniu online — poza zakresem
tej iteracji (środki nieprzechwycone; ewentualny void authorization to osobny follow-up).

### 4.4 Płatność (PayU, ADR-0013)
| Use case | Typ | Rola | Uwagi |
|---|---|---|---|
| `InitializePaymentCommand` | Command | **Customer (właściciel) / obsługa** | Ponowienie płatności dla istniejącego zamówienia (właściwa inicjalizacja przy checkoucie dzieje się inline w `CreateOrderCommand`, krok 8). Po `InitializePaymentAsync` zapisuje referencję: `SetProviderPaymentReferenceAsync` + `IUnitOfWork.SaveChangesAsync` (ADR-0018). Stan niedozwolony (nie-`Online` lub już `Paid`) ⇒ `ConflictException` (409), NIE `ForbiddenOperationException`. **Wariant gościa** (klucz = `GuestTrackingToken`) świadomie odłożony — patrz niżej. |
| `ConfirmPaymentFromNotificationCommand` | Command | **anonim (webhook PayU)** | Endpoint bez JWT; `IPaymentGateway.VerifyAndParseNotification` (podpis!) → mapuje na `PaymentStatus` → `Order.ConfirmPayment/AuthorizePayment/FailPayment/RefundPayment`. Idempotentny (opiera się na guard clauses Domain). Nieważny podpis ⇒ `InvalidPaymentNotificationException` → 400/401 (sekcja 5). |
| `GetPaymentStatusQuery` | Query | właściciel/obsługa | Podgląd statusu płatności. |

**Zakres `InitializePaymentCommand` a gość (ADR-0018).** Command obsługuje zalogowanego
właściciela/obsługę (ownership jak `GetOrderByIdQuery`/`CancelOrderCommand`). Gość NIE jest tu
osiągalny (`CustomerId is null`), i to jest świadome:
- Płatność online gościa przy składaniu zamówienia jest już obsłużona inline w
  `CreateOrderCommand` (krok 8 zwraca `PaymentRedirectUrl`).
- `InitializePaymentCommand` służy tylko **ponowieniu** płatności. Wariant gościa wymagałby
  anonimowego endpointu inicjującego akcję płatniczą kluczowanego `GuestTrackingToken` — to
  powierzchnia do świadomego przeglądu bezpieczeństwa, nie „przy okazji". Odłożone do momentu,
  gdy powstanie flow „wznów płatność" dla gościa: przyszły `InitializeGuestPaymentCommand`
  kluczowany tokenem, analogiczny do `GetOrderByTrackingTokenQueryHandler`.

### 4.5 Promocje
| Use case | Typ | Rola | Uwagi |
|---|---|---|---|
| `ValidatePromotionCodeQuery` | Query | anonim/Customer | Sprawdza `Promotion.IsQualifiedFor(subtotal, now, code)` dla koszyka; zwraca podgląd rabatu. |
| `CreatePromotionCommand` | Command | RestaurantAdmin | `Promotion.Create`; **odrzuca `BuyXGetY`** dopóki nieobsłużony (ADR-0011). |
| `UpdatePromotionCommand` | Command | RestaurantAdmin | Aktywacja/dezaktywacja, okno, wartość, limit. |
| `GetPromotionsQuery` | Query | RestaurantAdmin | Lista do zarządzania. |

Zastosowanie promocji do zamówienia dzieje się wewnątrz `CreateOrderCommand` (krok 6), nie
osobnym commandem — spójność z jedną transakcją i `RecordUsage`.

### 4.6 Lojalność
| Use case | Typ | Rola | Uwagi |
|---|---|---|---|
| `GetLoyaltyBalanceQuery` | Query | Customer | Saldo + historia transakcji. |
| Naliczanie punktów | (efekt) | — | Nie osobny command użytkownika: przy `CompleteOrderCommand` handler woła `ILoyaltyPolicy.CalculatePointsToEarn` → `Order.SetPointsToEarn` → `LoyaltyAccount.Earn` (dla zamówień z kontem). |

Wymiana punktów: część `CreateOrderCommand` (krok 7), nie osobny command.

---

## 5. Mapowanie wyjątków (Application → HTTP, egzekwuje middleware Api)

Oś decyzyjna (ADR-0017, rozszerzona przez ADR-0018) — jeden rodzaj błędu = jeden typ = jeden
kod. Obowiązuje wszystkie handlery, także Iterację 3 (płatności/webhook PayU):

| Rodzaj błędu | Typ | Warstwa | HTTP | Przykład |
|---|---|---|---|---|
| **Kształt danych** (wymagane pola, formaty, zakresy) | `ValidationException` | Application (FluentValidation) | **400** | pusty `Items`, zły format e-mail |
| **Zasób nie istnieje / niedostępny dla wykonawcy** (celowo nieodróżnialny, by nie potwierdzać cudzych zasobów) | `NotFoundException` | Application | **404** | brak `Order`/`MenuItem` po Id/tokenie; klient pyta o cudze zamówienie |
| **Operacja legalna, ale wykonawca nie ma uprawnień w tym stanie/kontekście** (decyzja zależna od roli/kontekstu, którego Domain nie zna — ADR-0004/0005) | `ForbiddenOperationException` | Application | **403** | klient anuluje zamówienie po `Accepted` (obsługa mogłaby) |
| **Konflikt stanu, nielegalny dla każdego wykonawcy, WYKRYWANY W APPLICATION** (bo operacja nie jest przejściem stanu agregatu — brak metody Domain do wywołania; ADR-0018) | `ConflictException` | Application | **409** | `InitializePaymentCommand` dla zamówienia `OnPickup` albo już `Paid` |
| **Konflikt stanu domenowego, nielegalny niezależnie od wykonawcy** (egzekwowany przez Domain) | `DomainException` i podtypy | Domain | **409/422/400** wg typu | `InvalidOrderStatusTransitionException` (anulowanie `Completed`), `BelowMinimumOrderValueException` |
| **Nieufny/sfałszowany webhook** (nieważny podpis notyfikacji PayU) | `InvalidPaymentNotificationException` | Application | **400** (ew. 401) | podrobione „potwierdzenie" płatności |
| **Jawnie niezaimplementowana gałąź** (ADR-0011) | `NotSupportedException` | Domain | **501/500** | `Promotion.CalculateDiscount` dla `BuyXGetY` — nie powinien wystąpić (Application odrzuca tworzenie takich promocji) |
| **Naruszenie niezmiennika danych** (stan wewnętrznie sprzeczny, nie wina wykonawcy) | `InvalidOperationException` | Application | **500** | opłacone online zamówienie bez `ProviderPaymentReference` przy refundzie (4.3.3) |

**Rozróżnienie 403 vs 409-Application vs 409-Domain (ADR-0017/ADR-0018).**
- **403 (`ForbiddenOperationException`)** = operacja *jest* legalna (uprzywilejowany wykonawca
  może ją wykonać), ale bieżący wykonawca nie ma uprawnień w tym stanie — decyzja zależna od
  roli/kontekstu, której Domain celowo nie zna.
- **409 (`ConflictException`, Application)** = konflikt stanu nielegalny dla *każdego*
  wykonawcy, ale wykryty w Application, bo operacja nie odpowiada żadnemu przejściu agregatu
  (np. inicjalizacja sesji bramki nie mutuje `Order`), więc nie ma metody Domain do wywołania,
  a wciąganie pojęcia bramki do Domain złamałoby ADR-0002.
- **409/422 (`DomainException`, Domain)** = realny konflikt stanu domenowego, egzekwowany
  przez guard clause agregatu (np. `InvalidOrderStatusTransitionException`).
Nie mieszać: reguły uniwersalne z metodą Domain zostają w Domain (`DomainException`);
uniwersalne bez metody Domain → `ConflictException`; zależne od roli → `ForbiddenOperationException`.

**Webhook PayU (ADR-0013).** Endpoint anonimowy (bez JWT); bezpieczeństwo = weryfikacja
podpisu w `IPaymentGateway.VerifyAndParseNotification`.
- Nieważny/sfałszowany podpis ⇒ `InvalidPaymentNotificationException` → **400** (payload
  niezaufany/malformed, bez ujawniania szczegółów); dopuszczalne **401** jeśli implementacja
  sygnalizuje „nieuwierzytelnione". To **nie** `ForbiddenOperationException` (brak pojęcia
  wykonawcy-roli) ani `ValidationException` (to nie walidacja kształtu DTO klienta).
- Poprawna notyfikacja o już ustawionym stanie ⇒ **200** (idempotencja, ADR-0013).

**Zasada projektowa.** Nie reużywać `ValidationException` do sygnalizowania reguł
biznesowych/autoryzacyjnych — jest zarezerwowana wyłącznie dla kształtu danych (400). Odmowy
zależne od roli/stanu → `ForbiddenOperationException` (403); konflikty stanu bez metody Domain
→ `ConflictException` (409). Wyjątki Application nie mają wspólnej klasy bazowej — middleware
mapuje po typie konkretnym (świadomie, YAGNI; ADR-0017).

---

## 6. Kolejność implementacji dla buildera

Iteracja 1 — szkielet i katalog (fundament pod resztę):
1. `Common/Messaging` (`ICommand`/`IQuery`/handlery/dyspozytor) + `Behaviors/ValidationBehavior`
   + `Common/Exceptions` (`ValidationException`, `NotFoundException`, `ForbiddenOperationException`).
2. `Common/Abstractions`: `IClock`, `ICurrentUser`, `IUnitOfWork`.
3. `IRestaurantRepository` + `GetRestaurantConfigQuery` + commandy konfiguracji restauracji.
4. `IIngredientRepository`, `IMenuItemRepository` + katalog: `GetMenuQuery`,
   `GetMenuItemByIdQuery`, `Create/Update MenuItem`, `Create/Update Ingredient`,
   `SetMenuItemAvailability`.

Iteracja 2 — zamówienie (rdzeń):
5. `IGeocodingService` (port) + `CheckDeliveryAvailabilityQuery`.
6. `IOrderRepository` + `CreateOrderCommand` (bez promocji/punktów/płatności na pierwszy
   przebieg — potem rozszerzany), z pełną walidacją i orkiestracją 4.3.1 kroki 1–5, 9–10.
7. `GetOrderByIdQuery`, `GetOrderByTrackingTokenQuery`, `GetOrderQueueQuery`.
8. Przejścia statusu: `AcceptOrderCommand`, `RejectOrderCommand`, `StartPreparationCommand`,
   `MarkReadyCommand`, `StartDeliveryCommand`, `CompleteOrderCommand`, `CancelOrderCommand`,
   `SetEstimatedReadyAtCommand` + port `IOrderNotifier`.

Iteracja 3 — płatności:
9. `IPaymentGateway` (port + DTO, ADR-0013) + `InitializePaymentCommand`, wpięcie do
   `CreateOrderCommand` (krok 8).
10. `ConfirmPaymentFromNotificationCommand` (idempotentny) + `GetPaymentStatusQuery`.
11. **Domknięcie płatności (ADR-0018):** `Common/Exceptions/ConflictException.cs`; rozszerzenie
    `IOrderRepository` (`AddAsync` +`providerPaymentReference`, `Set/GetProviderPaymentReferenceAsync`);
    persystencja referencji w `CreateOrderCommandHandler` i `InitializePaymentCommandHandler`;
    ścieżka refundu w `CancelOrderCommandHandler` (4.3.3); zmiana wyjątku w
    `InitializePaymentCommandHandler.EnsureCanInitializePayment` na `ConflictException`.

Iteracja 4 — promocje i lojalność:
12. `IPromotionRepository` + `ValidatePromotionCodeQuery`, `Create/Update/Get PromotionCommand`
    (`BuyXGetY` w pełni obsłużony, ADR-0034 — patrz też domain-model.md 8.2), wpięcie do
    `CreateOrderCommand` (krok 6).
13. `ILoyaltyPolicy` (port + finalna impl w Infrastructure, ADR-0014, reguła sfinalizowana ADR-0033),
    `ILoyaltyAccountRepository`, `ICustomerRepository`, `GetLoyaltyBalanceQuery`, wpięcie
    wymiany (krok 7) i naliczania (przy `CompleteOrderCommand`).

Każdy Command/Query z odpowiadającym testem jednostkowym (CLAUDE.md) — handler mockuje porty
(Moq), asercje na wywołanych metodach Domain i wynikach.
