# Warstwa Api — projekt (wersja 1)

Projekt warstwy `PizzaShop.Api`. Zależy od `Application` (i przez nią od `Domain`) oraz od
`Infrastructure` (tylko po to, by wpiąć `AddInfrastructure` w kompozycji DI — Api jest hostem).
Api dostarcza: kontrolery HTTP, tożsamość/JWT, autoryzację ról, middleware wyjątków, SignalR
Hub oraz implementacje portów inherentnie webowych (`ICurrentUser`, `IOrderNotifier`,
`IJwtTokenGenerator`) — ADR-0024/0026/0027/0028.

Powiązane decyzje: ADR-0004/0005 (role, tożsamość vs. `Customer`, gość), ADR-0007 (płatność
niezależna od realizacji), ADR-0008 (`EstimatedReadyAt`), ADR-0013/0022 (webhook PayU bez JWT),
ADR-0017/0018 (oś wyjątków Application → HTTP), ADR-0024 (granica kompozycji), **ADR-0026**
(tożsamość/JWT), **ADR-0027** (middleware wyjątków + autoryzacja ról + kontrolery), **ADR-0028**
(SignalR). Porty Application: `docs/application-layer.md` sekcja 3. Use case'y: sekcja 4.
Graf `OrderStatus`: `docs/domain-model.md` 5.3.

---

## 1. Zasady warstwy

- **Cienkie kontrolery.** Kontroler: (1) mapuje request → Command/Query, (2) woła
  `IDispatcher.Send(...)`, (3) mapuje wynik na `IActionResult`. Żadnej logiki biznesowej,
  żadnej autoryzacji zależnej od stanu (ta żyje w handlerach, ADR-0017), żadnego dostępu do
  repozytoriów.
- **Autoryzacja ról w Api** (ADR-0004): atrybutami `[Authorize(Roles=...)]` per endpoint, z
  **jawnie wypisaną hierarchią** (CLAUDE.md). Autoryzacja zależna od stanu agregatu (np. „klient
  anuluje tylko przed akceptacją", „klient widzi tylko własne zamówienie") jest w handlerze i
  wraca jako `ForbiddenOperationException`/`NotFoundException` (ADR-0017), NIE jako `[Authorize]`.
- **Wejście przez `IDispatcher`** (ADR-0012). Kontrolery nie znają handlerów wprost.
- **Wynik i błędy w formacie `ProblemDetails`** (RFC 7807) — mapowanie wyjątków w jednym miejscu
  (globalny handler, sekcja 4), kontrolery nie łapią wyjextów Application/Domain.
- **`ICurrentUser`** to jedyny kanał tożsamości do handlerów — kontrolery nie przekazują
  `CustomerId`/roli parametrami; handler czyta je z `ICurrentUser`.

### 1.1 Identyfikator zasobu: route vs. body (route = jedyne źródło prawdy)

Dla endpointów mutujących (PUT/PATCH) z `{id}` w ścieżce, gdzie Command niesie to samo pole
identyfikatora (`Id`/`MenuItemId`/`PromotionId`/`OrderId`), **route jest jedynym źródłem prawdy**.
Kontroler nadpisuje pole id w Commandzie wartością z trasy przy mapowaniu — Commandy to `record`,
więc:

```
await _dispatcher.Send(command with { Id = id }, cancellationToken);
```

(analogicznie `command with { MenuItemId = id }` / `{ PromotionId = id }` / `{ OrderId = id }`).
**Bez** gałęzi decyzyjnej i **bez** `BadRequest()` przy rozbieżności route↔body. To celowo część
kroku (1) „mapuj request→Command", a nie dodatkowa semantyka błędu w kontrolerze (zasada „cienki
kontroler" wyklucza logikę decyzyjną).

Konsekwencje:
- **Zapobiega** klasie błędu „zaktualizowano nie ten zasób": handler zawsze operuje na agregacie
  wskazanym w URL. Wariant z `BadRequest()` taką rozbieżność jedynie *wykrywa* — nadpisanie ją
  *eliminuje*.
- **Nie tworzy** obejścia scentralizowanego mapowania błędów (sekcja 4, ADR-0027): kontroler nie
  emituje własnego `IActionResult` błędu poza osią wyjątków → `ProblemDetails`. Surowy
  `BadRequest()` i tak omijałby format `ProblemDetails` (brak `type`/`traceId`/spójnego kształtu).
- Pole id w body jest redundantne (URL identyfikuje zasób — idiom REST); jego wartość jest
  **ignorowana**, nie walidowana.

**Reguła wiążąca dla wszystkich kontrolerów mutujących** — obecnych (`MenuController`,
`IngredientsController`, `PromotionsController` w 6.2–6.5) i przyszłych (`OrdersController` PUT
`/{id}/estimated-ready-at` w 6.6 oraz dalsze). Nie powielać guardu route↔body w kontrolerach.

**Rozważone i odrzucone.**
- *Guard + `BadRequest()` przy niezgodności (obecny kod Iteracji 2).* Instytucjonalizuje 4. krok
  (gałąź + wynik błędu) w każdym kontrolerze mutującym i tworzy błąd poza osią `ProblemDetails`
  (sekcja 4). Odrzucone: łamie „cienki kontroler" i rozszczelnia zasadę „błędy w jednym miejscu"
  (ADR-0027).
- *Osobne request-DTO bez pola id (identyfikator wyłącznie z trasy).* Usuwa redundancję u źródła,
  ale dokłada warstwę DTO + mapowanie dla każdego PUT, niespójnie z wiązaniem Command bezpośrednio
  z body na endpointach POST (6.2/6.3/6.5). Over-engineering przy tej skali (YAGNI) — odrzucone.

*Trade-off wybranego wariantu:* rozbieżne id w body jest po cichu ignorowane (maskuje ewentualny
błąd klienta). Akceptowalne — URL jest kontraktem tożsamości zasobu, a id w body jest redundantne
i nie powinno sterować zachowaniem. Gdyby kiedyś zaszła potrzeba twardego odrzucania rozbieżności,
właściwym miejscem jest **scentralizowany** `ActionFilter`/filtr produkujący `ProblemDetails`
(spójnie z sekcją 4), nie gałąź w kontrolerze — to przyszły ADR, tylko na realną potrzebę.

---

## 2. Tożsamość i uwierzytelnianie (ADR-0026)

### 2.1 Wybór: własny `UserAccount` + BCrypt (NIE ASP.NET Core Identity)
Konta modelujemy własną, minimalną tabelą `UserAccount` z hasłem hashowanym BCrypt, zamiast
pełnego ASP.NET Core Identity. Uzasadnienie i trade-offy: ADR-0026. `UserAccount` **nie jest
agregatem Domain** (ADR-0005 — tożsamość żyje poza Domain); to model warstwy Application
(moduł `Identity`), utrwalany przez EF Core w Infrastructure.

### 2.2 Model `UserAccount` (Application/Identity)
| Pole | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | Klucz; trafia do JWT jako `sub`. |
| `Email` | string | Login; **unikalny** (unikalny indeks w Infrastructure). |
| `PasswordHash` | string | BCrypt (`IPasswordHasher`). |
| `Role` | `UserRole` | Jedna rola na koncie (ADR-0004). |
| `IsActive` | bool | Dezaktywacja konta bez usuwania (blokuje login). |
| `CreatedAt` | DateTimeOffset | UTC (`IClock`). |

Plik: `src/PizzaShop.Application/Identity/UserAccount.cs` — klasa z prywatnym ctorem + fabryką
`Create(email, passwordHash, role, now)`, metodami `Deactivate()`/`Activate()`. Prywatny ctor
bezparametrowy dla EF (jak ADR-0020). Domain jej nie zna.

### 2.3 Porty (Application/Identity/Abstractions)
- `IUserAccountRepository` — `GetByEmailAsync(email, ct)`, `GetByIdAsync(id, ct)`,
  `ExistsByEmailAsync(email, ct)`, `AddAsync(account, ct)`. Commit przez `IUnitOfWork`
  (ten sam scoped `DbContext`, więc rejestracja klienta jest atomowa — 2.6).
- `IPasswordHasher` — `Hash(password) → string`, `Verify(password, hash) → bool`.
  Impl: `BcryptPasswordHasher` w **Infrastructure** (util, jak `IClock`; ADR-0024).
- `IJwtTokenGenerator` — `Generate(UserAccount account, Guid? customerId) → string` (podpisany
  JWT). Impl: **Api** (`JwtTokenGenerator`) — potrzebuje konfiguracji podpisu, symetrycznie do
  `ICurrentUser` czytającego claimy (ADR-0024/0026). Infrastructure nie zna JWT.

### 2.4 CQRS modułu Identity (Application)
| Use case | Typ | Rola | Uwagi |
|---|---|---|---|
| `RegisterCustomerCommand` | Command | anonim | Tworzy `UserAccount(Customer)` + `Customer` + `LoyaltyAccount` w jednej transakcji (2.6). Zwraca JWT (auto-login) + `CustomerId`. |
| `LoginCommand` | Command | anonim | `GetByEmailAsync` → `IPasswordHasher.Verify` → jeśli OK i `IsActive`: `IJwtTokenGenerator.Generate`. Błąd = `ForbiddenOperationException` **lub** dedykowany 401 (2.7) — jednolity komunikat „invalid credentials" (nie zdradzać, czy email istnieje). |
| `RegisterStaffAccountCommand` | Command | RestaurantAdmin/SuperAdmin | Tworzy konto `Employee`/`RestaurantAdmin`/`SuperAdmin` bez profilu `Customer` (ADR-0004). Ograniczenie kto kogo tworzy: `RestaurantAdmin` → tylko `Employee`; `SuperAdmin` → dowolna rola. Walidacja tej reguły w handlerze (`ICurrentUser.Role`) → `ForbiddenOperationException`. |

Walidatory FluentValidation (kształt): email format, hasło min. długość/złożoność, rola z enuma.

### 2.5 Powiązanie `UserAccount.Id` ↔ `Customer.UserAccountId` (ADR-0005/0026/0029)
`RegisterCustomerCommand` **jest jedynym miejscem tworzącym `Customer`**. Tworzy razem, w tej
kolejności (ADR-0029 — relacja Customer↔LoyaltyAccount jest jednokierunkowa, więc `Customer`
musi istnieć zanim powstanie `LoyaltyAccount`):
1. `UserAccount.Create(email, hash, UserRole.Customer, now)`.
2. `Customer.Create(userAccount.Id, ...)` — generuje własne `Id` wewnętrznie.
3. `LoyaltyAccount.Create(customer.Id)` (szkielet, ADR-0009) — jedyny nośnik powiązania 1:1 jest
   `LoyaltyAccount.CustomerId`; `Customer` **nie ma** `LoyaltyAccountId` (usunięte w ADR-0029,
   dostęp wyłącznie przez `ILoyaltyAccountRepository.GetByCustomerIdAsync`).
Wszystko commitowane jednym `IUnitOfWork.SaveChangesAsync` (atomowo). Personel (`Employee`/
`RestaurantAdmin`/`SuperAdmin`) **nie dostaje** `Customer` (ADR-0004) — ma tylko `UserAccount`.

**`CustomerId` w tokenie.** JWT klienta zawiera claim `customerId` (obok `sub` = `UserAccountId`
i `role`), żeby `ICurrentUser.CustomerId` nie wymagał zapytania do bazy per żądanie. Konta
personelu nie mają claimu `customerId`.

### 2.6 Atomowość rejestracji
`IUserAccountRepository`, `ICustomerRepository`, `ILoyaltyAccountRepository` współdzielą ten sam
scoped `PizzaShopDbContext` (Infrastructure). Handler dodaje trzy encje (`AddAsync` bez commitu),
potem jeden `IUnitOfWork.SaveChangesAsync` → jedna transakcja. Duplikat emaila: sprawdzenie
`ExistsByEmailAsync` **oraz** unikalny indeks bazodanowy jako ostateczny strażnik → kolizja
mapowana na `ConflictException` (409).

### 2.7 JWT — generacja i walidacja
- **Generacja** (`JwtTokenGenerator`, Api): claimy `sub`=UserAccountId, `role`=nazwa roli
  (`ClaimTypes.Role`), `email`, opcjonalnie `customerId`. Podpis HMAC-SHA256 kluczem z
  konfiguracji (`Jwt:SigningKey`), `issuer`/`audience`/`expiry` z `Jwt:*`. Sekcja `Jwt` w
  `appsettings` + user-secrets (klucz nie w repo).
- **Walidacja** (`AddAuthentication().AddJwtBearer(...)` w Program.cs): `TokenValidationParameters`
  z tego samego `Jwt:*`, `ValidateIssuer/Audience/Lifetime/IssuerSigningKey = true`,
  `RoleClaimType = ClaimTypes.Role`.
- **Bootstrap `SuperAdmin`.** Bez konta nie da się utworzyć personelu. Startowy `SuperAdmin`
  seedowany przy starcie z konfiguracji (`Seed:SuperAdminEmail`/`Password`) jeśli nie istnieje —
  seeder w Api (`DbSeeder`, wywołany po `Database.Migrate()`), idempotentny.

---

## 3. `ICurrentUser` — implementacja Api (`HttpContextCurrentUser`)
`src/PizzaShop.Api/Auth/HttpContextCurrentUser.cs`, `Scoped`, zależny od `IHttpContextAccessor`.
Czyta claimy z `HttpContext.User`:
- `UserAccountId` = `sub` (parsuj Guid; null gdy anonim).
- `CustomerId` = claim `customerId` (parsuj Guid; null dla gościa i personelu).
- `Role` = claim roli → `UserRole` (null gdy anonim).
Brak `HttpContext`/niezalogowany ⇒ wszystkie null (kontrakt `ICurrentUser` — gość, ADR-0005).
Rejestracja: `AddHttpContextAccessor()` + `AddScoped<ICurrentUser, HttpContextCurrentUser>()`.

---

## 4. Middleware wyjątków — `ProblemDetails` (ADR-0027)

Jeden globalny punkt mapowania: `IExceptionHandler` (ASP.NET Core 8, `AddExceptionHandler` +
`UseExceptionHandler`) albo klasyczny middleware. Kontrolery **nie** łapią wyjątków Application/
Domain. Wynik = `ProblemDetails` (`application/problem+json`): `status`, `title`, `detail`
(= `exception.Message`, bezpieczny do zwrócenia — ADR-0017), `type`, `traceId`. Dla
`ValidationException` dodatkowo `errors` (słownik pole→komunikaty), format zgodny z
`ValidationProblemDetails`.

**Kontrolery nie emitują własnych wyników błędu** (np. surowego `BadRequest()`/`Conflict()`) —
jedynym źródłem odpowiedzi błędnych jest ta oś (wyjątek Application/Domain → `ProblemDetails`).
Rozbieżność route↔body id **nie jest** błędem 400 — patrz 1.1 (route nadpisuje pole id w Commandzie,
bez guardu). Jedyne dopuszczalne „nie-wyjątkowe" wyniki błędu poza tą osią to natywne 401/403 z
warstwy autoryzacji (`[Authorize]`, `FallbackPolicy` — sekcja 5) oraz błędy modelu bindingu
`[ApiController]` (400 dla niedeserializowalnego body/nieparsowalnego `{id:guid}`), które i tak
przyjmują kształt `ProblemDetails`.

### 4.1 Oś mapowania (Application — zgodna z application-layer.md sekcja 5)
| Wyjątek | Warstwa | HTTP |
|---|---|---|
| `ValidationException` | Application (FluentValidation) | **400** (+ `errors`) |
| `NotFoundException` | Application | **404** |
| `ForbiddenOperationException` | Application | **403** |
| `ConflictException` | Application | **409** |
| `InvalidPaymentNotificationException` | Application (webhook) | **400** (payload niezaufany) |
| `NotSupportedException` | Domain (BuyXGetY, ADR-0011) | **501** |
| `InvalidOperationException` | Application (naruszenie niezmiennika, 4.3.3) | **500** |
| `DomainException` i podtypy | Domain | **409 / 422** wg typu (4.2) |
| pozostałe / nieobsłużone | — | **500** (bez `detail` z wyjątku; log + `traceId`) |

### 4.2 Mapowanie `DomainException` (podtypy → 409 vs 422)
Reguła: **409 Conflict** = konflikt ze *stanem* zasobu (przejścia, podwójne zastosowanie);
**422 Unprocessable Entity** = operacja biznesowa niewykonalna dla podanych danych/reguł, mimo
poprawnego kształtu. Middleware mapuje po typie konkretnym (słownik `Type→HttpStatus`), domyślnie
`DomainException` → **422**.

**→ 409 (konflikt stanu):**
`InvalidOrderStatusTransitionException`, `InvalidPaymentStatusTransitionException`,
`PromotionAlreadyAppliedException`, `LoyaltyPointsAlreadyRedeemedException`.

**→ 422 (reguła biznesowa / niezmiennik na danych):**
`EmptyOrderException`, `DeliveryAddressRequiredException`, `AddressOutsideDeliveryAreaException`,
`BelowMinimumOrderValueException`, `RestaurantClosedException`, `PastFulfillmentTimeException`,
`PaymentRequiredBeforeAcceptanceException`, `InvalidEstimatedReadyAtException`,
`PizzaWithoutIngredientException`, `MenuItemUnavailableException`,
`VariantSelectionRequiredException`, `InvalidVariantConfigurationException`,
`CannotRemoveLastVariantException`, `ExtraNotAllowedException`,
`PromotionNotApplicableException`, `InsufficientLoyaltyPointsException`,
`LoyaltyRedemptionNotAllowedException`, `AddressNotInAddressBookException`.

`ArgumentException`/`ArgumentOutOfRangeException` z Domain (np. edycja `Promotion`, ADR-0019) →
**400** defensywnie (walidator FluentValidation jest głównym strażnikiem kształtu; te wyjątki nie
powinny wyciekać). Middleware ma jawny wpis, żeby nie wpadły do 500.

---

## 5. Autoryzacja ról — hierarchia jawna (ADR-0027)

Hierarchia (`SuperAdmin ⊇ RestaurantAdmin ⊇ Employee`) egzekwowana przez `[Authorize(Roles=...)]`
z **jawnie wypisanymi** rolami (CLAUDE.md — nie przez token, nie przez Domain). Żeby uniknąć
literówek, listy ról trzymamy w stałych (`src/PizzaShop.Api/Auth/AuthRoles.cs`):

```
public static class AuthRoles
{
    public const string Staff  = "Employee,RestaurantAdmin,SuperAdmin"; // Employee i wyżej
    public const string Admin  = "RestaurantAdmin,SuperAdmin";          // RestaurantAdmin i wyżej
    public const string Owner  = "SuperAdmin";                          // tylko SuperAdmin
    public const string Customer = "Customer";                          // tylko klient (dane własne)
}
```

Użycie: `[Authorize(Roles = AuthRoles.Staff)]` itd. Stała rozwija się do jawnej listy w atrybucie
(spełnia wymóg „jawnie wypisanej hierarchii"), a nie duplikujemy stringów po kontrolerach.
Endpointy publiczne: `[AllowAnonymous]`. Domyślny `FallbackPolicy` = wymaga uwierzytelnienia,
żeby zapomniany atrybut nie zostawił endpointu otwartego (endpointy publiczne jawnie
`[AllowAnonymous]`).

---

## 6. Kontrolery per moduł (ADR-0027)

Wszystkie pod `/api`. Kolumna „Autoryzacja" = wartość `[Authorize(Roles=...)]` lub `[AllowAnonymous]`.
Kolumna „Use case" = Command/Query wołany przez `IDispatcher`.

> **Endpointy z `{id}` w ścieżce + id w body Commandu** (PUT/PATCH) stosują regułę **1.1**: route
> nadpisuje pole id w Commandzie (`command with { Id = id }`), **bez** guardu route↔body i bez
> `BadRequest()`. Dotyczy 6.2/6.3/6.5 (Iteracja 2) oraz 6.6 (Iteracja 3, PUT
> `/{id}/estimated-ready-at`).

### 6.1 `AuthController` (`/api/auth`) — Iteracja 1
| Metoda | Ścieżka | Use case | Autoryzacja |
|---|---|---|---|
| POST | `/register` | `RegisterCustomerCommand` | AllowAnonymous |
| POST | `/login` | `LoginCommand` | AllowAnonymous |
| POST | `/staff` | `RegisterStaffAccountCommand` | `Admin` (SuperAdmin dla ról admin — reguła w handlerze) |
| GET | `/me` | (z `ICurrentUser`, bez CQRS) | Authorize (dowolna rola) |

### 6.2 `MenuController` (`/api/menu`) — Iteracja 2
| Metoda | Ścieżka | Use case | Autoryzacja |
|---|---|---|---|
| GET | `/` | `GetMenuQuery` | AllowAnonymous |
| GET | `/{id}` | `GetMenuItemByIdQuery` | AllowAnonymous |
| POST | `/` | `CreateMenuItemCommand` | `Admin` |
| PUT | `/{id}` | `UpdateMenuItemCommand` | `Admin` |
| PATCH | `/{id}/availability` | `SetMenuItemAvailabilityCommand` | `Staff` (Employee może wyłączyć pozycję) |

> PUT `/{id}` i PATCH `/{id}/availability`: route nadpisuje `Id`/`MenuItemId` w Commandzie (1.1).

### 6.3 `IngredientsController` (`/api/ingredients`) — Iteracja 2
| POST | `/` | `CreateIngredientCommand` | `Admin` |
| PUT | `/{id}` | `UpdateIngredientCommand` | `Admin` |
> Lista składników dla admina (GET) — jeśli potrzebna w UI, dodać `GetIngredientsQuery`
> w Application (nie ma jej dziś w 4.1); nie tworzyć na zapas.
> PUT `/{id}`: route nadpisuje `Id` w Commandzie (1.1).

### 6.4 `RestaurantController` (`/api/restaurant`) — Iteracja 2
| GET | `/config` | `GetRestaurantConfigQuery` | AllowAnonymous (część publiczna) |
| PUT | `/opening-hours` | `UpdateOpeningHoursCommand` | `Admin` |
| PUT | `/delivery-area` | `UpdateDeliveryAreaCommand` | `Admin` |
| PUT | `/ordering-thresholds` | `UpdateOrderingThresholdsCommand` | `Admin` |
| POST | `/accepting-orders` | `ToggleAcceptingOrdersCommand` | `Staff` |
> Restauracja jest singletonem (ADR-0003/0015) — brak `{id}` w ścieżce, więc reguła 1.1 nie
> dotyczy tych PUT-ów (nie ma route-id do uzgadniania).

### 6.5 `PromotionsController` (`/api/promotions`) — Iteracja 2
| POST | `/validate` | `ValidatePromotionCodeQuery` | AllowAnonymous (podgląd rabatu dla koszyka) |
| GET | `/` | `GetPromotionsQuery` | `Admin` |
| POST | `/` | `CreatePromotionCommand` | `Admin` |
| PUT | `/{id}` | `UpdatePromotionCommand` | `Admin` |
> PUT `/{id}`: route nadpisuje `PromotionId` w Commandzie (1.1).

### 6.6 `OrdersController` (`/api/orders`) — Iteracja 3
| POST | `/check-delivery` | `CheckDeliveryAvailabilityQuery` | AllowAnonymous |
| POST | `/` | `CreateOrderCommand` | AllowAnonymous (gość lub klient; `ICurrentUser.CustomerId` decyduje) |
| GET | `/{id}` | `GetOrderByIdQuery` | Authorize (własne/obsługa — scoping w handlerze) |
| GET | `/track/{trackingToken}` | `GetOrderByTrackingTokenQuery` | AllowAnonymous (token = autoryzacja, ADR-0005) |
| GET | `/queue` | `GetOrderQueueQuery` | `Staff` |
| POST | `/{id}/accept` | `AcceptOrderCommand` | `Staff` |
| POST | `/{id}/reject` | `RejectOrderCommand` | `Staff` |
| POST | `/{id}/start-preparation` | `StartPreparationCommand` | `Staff` |
| POST | `/{id}/mark-ready` | `MarkReadyCommand` | `Staff` |
| POST | `/{id}/start-delivery` | `StartDeliveryCommand` | `Staff` |
| POST | `/{id}/complete` | `CompleteOrderCommand` | `Staff` |
| PUT | `/{id}/estimated-ready-at` | `SetEstimatedReadyAtCommand` | `Staff` |
| POST | `/{id}/cancel` | `CancelOrderCommand` | Authorize (klient przed `Accepted` / obsługa — reguła w handlerze, ADR-0017) |

> PUT `/{id}/estimated-ready-at`: route nadpisuje `OrderId` w `SetEstimatedReadyAtCommand` (1.1) —
> ten sam wzorzec co PUT-y katalogu, bez guardu route↔body. Endpointy `POST /{id}/...` (przejścia
> statusu) niosą `OrderId` w route i również mapują go do Commandu z trasy (`command with
> { OrderId = id }` gdy Command ma to pole; jeśli Command przyjmuje tylko `OrderId`, konstruuje się
> go bezpośrednio z route — bez id w body do uzgadniania).

### 6.7 `PaymentsController` (`/api/payments`) — Iteracja 3
| POST | `/orders/{id}/initialize` | `InitializePaymentCommand` | Authorize (właściciel/obsługa — scoping w handlerze; gość odłożony, ADR-0018) |
| GET | `/orders/{id}/status` | `GetPaymentStatusQuery` | Authorize (właściciel/obsługa) |
| POST | `/payu/webhook` | `ConfirmPaymentFromNotificationCommand` | **AllowAnonymous** (raw body — sekcja 7) |
> `POST /orders/{id}/initialize`: `OrderId` pochodzi z route; jeśli Command niesie `OrderId`,
> nadpisać z trasy (1.1). Webhook nie ma `{id}` — poza regułą 1.1.

### 6.8 `LoyaltyController` (`/api/loyalty`) — Iteracja 4
| GET | `/balance` | `GetLoyaltyBalanceQuery` | `Customer` (saldo własne — scoping po `ICurrentUser.CustomerId`) |

---

## 7. Webhook PayU — surowe body (ADR-0022/0027)

Endpoint `POST /api/payments/payu/webhook`, `[AllowAnonymous]`. Bezpieczeństwo = weryfikacja
podpisu `OpenPayU-Signature` w `IPaymentGateway.VerifyAndParseNotification` (Infrastructure,
ADR-0013/0022) — **NIE JWT**. Weryfikacja podpisu wymaga *dokładnie* surowego body (bajt w bajt),
więc kontroler NIE może polegać na deserializacji modelu:

- Metoda akcji **bez** `[FromBody]`; czyta surowe body:
  `using var reader = new StreamReader(Request.Body, Encoding.UTF8); var rawBody = await reader.ReadToEndAsync(ct);`
  (dla pewności `Request.EnableBuffering()` we wczesnym middleware lub czytanie strumienia zanim
  cokolwiek go skonsumuje).
- Przekazuje `rawBody` + istotne nagłówki (min. `OpenPayU-Signature`) do
  `ConfirmPaymentFromNotificationCommand`.
- Zwroty: **200** dla obsłużonej notyfikacji (także idempotentnej powtórki — ADR-0013);
  `InvalidPaymentNotificationException` → **400** (middleware, 4.1). Nie zwracać szczegółów błędu
  podpisu (nie pomagać atakującemu).
- Uwaga konfiguracyjna: upewnić się, że globalny wymóg autoryzacji (`FallbackPolicy`, sekcja 5)
  nie obejmuje tego endpointu — jawny `[AllowAnonymous]`.

---

## 8. SignalR — `OrderTrackingHub` + `IOrderNotifier` (ADR-0028)

### 8.1 Hub
`OrderTrackingHub : Hub` w `src/PizzaShop.Api/Realtime/`, mapowany na `/hubs/order-tracking`,
`[AllowAnonymous]` (gość musi móc śledzić bez JWT — ADR-0005). **Grupy per `OrderId`**
(nazwa grupy = `orderId.ToString()`). Push idzie zawsze do grupy `OrderId`, więc `IOrderNotifier`
pozostaje kluczowany wyłącznie `OrderId` (zgodny z istniejącym portem).

Metody subskrypcji (autoryzacja dostępu przy subskrypcji, nie push):
- `SubscribeToGuestOrder(string trackingToken)` — ścieżka gościa. Hub woła
  `GetOrderByTrackingTokenQuery` (przez `IDispatcher`); jeśli zwróci zamówienie, dodaje połączenie
  do grupy `order.Id` (`Groups.AddToGroupAsync`). Token nieodgadnalny = autoryzacja (jak endpoint
  trackingu, ADR-0005). Brak/nieprawidłowy token → brak subskrypcji (Hub nie ujawnia istnienia).
- `SubscribeToOrder(Guid orderId)` — ścieżka zalogowanego. Hub woła `GetOrderByIdQuery` (handler
  scope'uje po `ICurrentUser` — własne/obsługa); sukces ⇒ dodanie do grupy `orderId`, inaczej brak
  subskrypcji. Tożsamość w SignalR pochodzi z tego samego JWT (`AddJwtBearer` obsługuje
  `access_token` z query stringu dla WebSocketów — `OnMessageReceived`).

Rozstrzygnięcie „grupy per OrderId czy per GuestTrackingToken": **per `OrderId`**. Token służy
tylko do *autoryzacji subskrypcji* (rozwiązywany na `OrderId` przy `SubscribeToGuestOrder`), a nie
jako klucz grupy — dzięki temu notifier nie musi znać tokenu, a oba typy odbiorców (gość i klient)
lądują w jednej grupie tego samego zamówienia.

### 8.2 `IOrderNotifier` — impl Api (`SignalROrderNotifier`)
`src/PizzaShop.Api/Realtime/SignalROrderNotifier.cs`, przez `IHubContext<OrderTrackingHub>`
(ADR-0024). `OrderStatusChangedAsync(orderId, status, estimatedReadyAt)` →
`_hub.Clients.Group(orderId.ToString()).SendAsync("OrderStatusChanged", payload, ct)` gdzie payload
= `{ orderId, status, estimatedReadyAt }`. Wołany przez handlery przejść statusu i
`SetEstimatedReadyAtCommand` (application-layer.md 4.3). Rejestracja:
`AddScoped<IOrderNotifier, SignalROrderNotifier>()`.

---

## 9. Program.cs / kompozycja DI (ADR-0027)

Kolejność w `Program.cs`:
1. `builder.Services.AddApplication()` (dyspozytor, walidatory, handlery — ADR-0012).
2. `builder.Services.AddInfrastructure(builder.Configuration)` (DbContext, repozytoria, UoW, PayU,
   geocoding, clock, loyalty policy — infrastructure-layer.md 8).
3. Porty webowe Api: `AddHttpContextAccessor()`,
   `AddScoped<ICurrentUser, HttpContextCurrentUser>()`,
   `AddScoped<IOrderNotifier, SignalROrderNotifier>()`,
   `AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>()`.
4. `AddControllers()` (+ `AddProblemDetails()`), `AddEndpointsApiExplorer()`,
   `AddSwaggerGen(...)` z definicją `Bearer` (żeby Swagger UI wysyłał JWT).
5. Auth: `AddAuthentication(JwtBearerDefaults...).AddJwtBearer(o => { o.TokenValidationParameters = ...; o.Events = ...` (query-string token dla SignalR) `})`.
6. `AddAuthorization(o => o.FallbackPolicy = wymaga uwierzytelnienia)` (sekcja 5).
7. `AddSignalR()`.
8. `AddExceptionHandler<...>()` + `AddProblemDetails()` (middleware wyjątków, sekcja 4).
9. `AddCors(...)` — nazwana polityka pod przyszły frontend (origin z konfiguracji `Cors:Origins`);
   domyślnie restrykcyjna. Włączyć w pipeline tylko jeśli skonfigurowano origin.

Pipeline (`app`): `UseExceptionHandler()` → `UseHttpsRedirection()` → (`UseCors`) →
`UseAuthentication()` → `UseAuthorization()` → `MapControllers()` →
`MapHub<OrderTrackingHub>("/hubs/order-tracking")`. Swagger w Development. `DbSeeder`
(bootstrap SuperAdmin, 2.7) po `Database.Migrate()` w Development/na starcie.

Pakiety NuGet Api (`.csproj`): `Microsoft.AspNetCore.Authentication.JwtBearer`,
`Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.SignalR` (część frameworka), referencje projektowe
do `Application` i `Infrastructure`. BCrypt (`BCrypt.Net-Next`) — w Infrastructure (impl hashera).

Usuń szablonowy `WeatherForecast` z `Program.cs`.

---

## 10. Iteracje dla buildera

Każda iteracja jest samodzielnie budowalna, testowalna i możliwa do zatwierdzenia przez reviewera
osobno.

- **Iteracja 1 — tożsamość + JWT + middleware wyjątków + szkielet DI.** Moduł `Identity`
  (`UserAccount`, porty, `RegisterCustomerCommand`, `LoginCommand`, `RegisterStaffAccountCommand`),
  `IJwtTokenGenerator`/`JwtTokenGenerator`, `IPasswordHasher`/`BcryptPasswordHasher`,
  `HttpContextCurrentUser`, middleware wyjątków (`ProblemDetails`, pełna oś), `AuthController`,
  `Program.cs` (AddApplication/AddInfrastructure/JWT/authorization/Swagger/ProblemDetails), EF:
  `DbSet<UserAccount>` + konfiguracja + migracja, seeder SuperAdmin. **Szczegółowe kroki: sekcja 11.**
- **Iteracja 2 — kontrolery odczytu + admin (Catalog, Restaurant, Promotions).** `MenuController`,
  `IngredientsController`, `RestaurantController`, `PromotionsController` (6.2–6.5). Endpointy
  publiczne (menu, config, validate promo) + admin. PUT/PATCH z `{id}` stosują regułę 1.1 (route
  nadpisuje id w Commandzie, bez guardu). Testy Api (`WebApplicationFactory`) na
  autoryzacji (anonim vs. rola) i mapowaniu wyjątków.
- **Iteracja 3 — zamówienia + płatności + webhook.** `OrdersController` (6.6), `PaymentsController`
  (6.7), surowe body webhooka (sekcja 7). PUT `/{id}/estimated-ready-at` i akcje `/{id}/...`
  mapują `OrderId` z route (1.1). Testy: składanie zamówienia gość/klient, tracking po
  tokenie (anonim), przejścia statusu (rola `Staff`), scoping `GetOrderByIdQuery` (403/404),
  webhook (200 idempotentny / 400 zły podpis).
- **Iteracja 4 — SignalR + Loyalty.** `OrderTrackingHub` + `SignalROrderNotifier` (sekcja 8),
  `LoyaltyController` (6.8), token JWT w query dla WebSocketów. Testy: subskrypcja gościa po tokenie
  vs. zalogowanego po ownership, dostarczenie `OrderStatusChanged` do grupy.

Każdy kontroler i handler Identity z testem (CLAUDE.md). Testy Api integracyjne przez
`WebApplicationFactory` (uwierzytelnianie: testowy handler auth lub realny JWT); testy handlerów
Identity jednostkowo z mockowanymi portami (Moq).

---

## 11. Kroki Iteracji 1 (kolejność implementacji)

1. **Porty i model Identity (Application).** `Identity/UserAccount.cs` (prywatny ctor + `Create`);
   `Identity/Abstractions/IUserAccountRepository.cs`, `IPasswordHasher.cs`, `IJwtTokenGenerator.cs`.
2. **Wyjątek (jeśli brak).** Upewnić się, że `ConflictException` istnieje (ADR-0018) — użyty przy
   duplikacie emaila. Reużyć, nie dodawać nowego.
3. **Commands + walidatory + testy (Application/Identity).** `LoginCommand`(+handler+validator),
   `RegisterCustomerCommand` (handler tworzy `UserAccount`+`Customer`+`LoyaltyAccount`, atomowo,
   2.5/2.6), `RegisterStaffAccountCommand` (reguła kto-kogo, `ForbiddenOperationException`). Testy
   handlerów z mockami portów.
4. **Persystencja `UserAccount` (Infrastructure).** `UserAccountConfiguration` (unikalny indeks
   `Email`, `Id` `ValueGeneratedNever`), `DbSet<UserAccount>` w `PizzaShopDbContext`,
   `UserAccountRepository`, rejestracja w `AddInfrastructure`. `BcryptPasswordHasher` (`IPasswordHasher`)
   + pakiet `BCrypt.Net-Next` + rejestracja. Migracja `AddUserAccount`.
5. **JWT (Api).** `Auth/JwtOptions.cs`, `Auth/JwtTokenGenerator.cs` (`IJwtTokenGenerator`, claimy
   `sub`/`role`/`email`/`customerId`, HMAC-SHA256). Sekcja `Jwt` w `appsettings` (klucz z
   user-secrets).
6. **`ICurrentUser` (Api).** `Auth/HttpContextCurrentUser.cs` (sekcja 3).
7. **Autoryzacja (Api).** `Auth/AuthRoles.cs` (stałe list ról, sekcja 5).
8. **Middleware wyjątków (Api).** `Middleware/ExceptionHandler.cs` (`IExceptionHandler`) — pełna oś
   4.1 + słownik `DomainException`→409/422 (4.2), format `ProblemDetails`/`ValidationProblemDetails`.
9. **`AuthController` (Api).** `/register`, `/login`, `/staff`, `/me` (6.1).
10. **`Program.cs` (Api).** Kompozycja z sekcji 9 (AddApplication → AddInfrastructure → porty webowe
    → controllers/ProblemDetails/Swagger+Bearer → JwtBearer → Authorization+FallbackPolicy →
    ExceptionHandler). Usunąć `WeatherForecast`.
11. **Seeder (Api).** `DbSeeder` — bootstrap `SuperAdmin` z konfiguracji, idempotentny (2.7).
12. **Testy Api (Iteracja 1).** `WebApplicationFactory`: rejestracja→login→wywołanie `/me` z tokenem;
    `/staff` bez roli admina → 403; duplikat emaila → 409; zły login → 401/403; nieobsłużony wyjątek
    → `ProblemDetails` 500. Nowy projekt `tests/PizzaShop.Api.Tests` (jeśli nie istnieje).

Po Iteracji 1: `reviewer` przegląda (Clean Architecture — Api nie przecieka do Domain, porty w
właściwych warstwach; format błędów; brak logiki biznesowej w kontrolerach), potem Iteracja 2.
