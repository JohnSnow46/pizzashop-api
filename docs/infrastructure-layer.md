# Warstwa Infrastructure — projekt (wersja 1)

Projekt warstwy `PizzaShop.Infrastructure`. Zależy od `Application` (i przez nią od `Domain`).
Dostarcza **implementacje portów** zdefiniowanych w `Application` (repozytoria, `IUnitOfWork`,
`IPaymentGateway`, `IGeocodingService`, `IClock`, `ILoyaltyPolicy`) oraz perystencję EF Core +
PostgreSQL. **Nie** implementuje portów czysto webowych (`IOrderNotifier` — SignalR, `ICurrentUser`
— HttpContext); te żyją w `Api` (ADR-0024).

Powiązane decyzje: ADR-0001 (PostgreSQL/Npgsql), ADR-0010 (UTC/timestamptz), ADR-0002/0013
(PayU + `IPaymentGateway`), ADR-0006 (Haversine w Domain, adres wymaga współrzędnych z
`IGeocodingService`), ADR-0015 (jedyny rekord `Restaurant`), ADR-0018 (`ProviderPaymentReference`
jako sidecar), **ADR-0020..0025** (ta warstwa). Model domenowy: `docs/domain-model.md`.
Porty: `docs/application-layer.md` sekcja 3.

---

## 1. Struktura folderów

```
PizzaShop.Infrastructure/
  Persistence/
    PizzaShopDbContext.cs              # jeden DbContext, DbSet-y korzeni agregatów
    UnitOfWork.cs                      # IUnitOfWork -> DbContext.SaveChangesAsync
    DesignTimeDbContextFactory.cs      # IDesignTimeDbContextFactory dla `dotnet ef`
    Configurations/                    # IEntityTypeConfiguration<T> per agregat
      RestaurantConfiguration.cs
      IngredientConfiguration.cs
      MenuItemConfiguration.cs
      OrderConfiguration.cs
      CustomerConfiguration.cs
      LoyaltyAccountConfiguration.cs
      PromotionConfiguration.cs
    Converters/                        # ValueConverter/ValueComparer wspólne
      MoneyConverter.cs                # Money <-> decimal (PLN implikowane)
      OpeningHoursConverter.cs         # OpeningHours <-> jsonb (+ ValueComparer)
    Configurations/Shared/             # helpery mapowania VO reużywane w wielu konfiguracjach
      OwnedAddress.cs                  # OwnsOne(Address) -> kolumny
      OwnedGeoCoordinate.cs            # OwnsOne(GeoCoordinate) -> kolumny
      OwnedDeliveryAddress.cs          # OwnsOne(DeliveryAddress { Address, Coordinate })
      OwnedContactDetails.cs           # OwnsOne(ContactDetails)
    Repositories/
      RestaurantRepository.cs
      MenuItemRepository.cs
      IngredientRepository.cs
      OrderRepository.cs
      CustomerRepository.cs
      LoyaltyAccountRepository.cs
      PromotionRepository.cs
    Migrations/                        # generowane przez `dotnet ef migrations`
  Payments/
    PayU/
      PayUPaymentGateway.cs            # IPaymentGateway (init / verify+parse / refund)
      PayUOptions.cs                   # POS id, klucze, base URL (sandbox/prod), sekret podpisu
      PayUStatusMapper.cs              # surowy status PayU -> PaymentStatus (Domain)
      PayUSignatureVerifier.cs         # weryfikacja OpenPayU-Signature (webhook bez JWT)
      (DTO wewnętrzne PayU: request/response order, notyfikacja)
  Geocoding/
    NominatimGeocodingService.cs       # IGeocodingService (OSM Nominatim)
    GeocodingOptions.cs                # base URL, User-Agent, timeout
  Time/
    SystemClock.cs                     # IClock -> DateTimeOffset.UtcNow
  Loyalty/
    LinearLoyaltyPolicy.cs             # ILoyaltyPolicy placeholder (ADR-0014)
  DependencyInjection.cs               # AddInfrastructure(IServiceCollection, IConfiguration)
```

> SignalR Hub oraz implementacja `IOrderNotifier` **nie są tutaj** — żyją w `Api` (ADR-0024).
> `ICurrentUser` (HttpContext) także w `Api`.

---

## 2. Mapowanie EF Core — zasady ogólne (ADR-0020)

### 2.1 Materializacja encji Domain
Wszystkie encje Domain mają **prywatne konstruktory + fabryki statyczne**, brak konstruktora
bezparametrowego. EF Core nie umie związać istniejących konstruktorów (parametry typu
`IEnumerable<OrderItem>` itd.). Dlatego builder **dodaje prywatny bezparametrowy konstruktor**
do każdego typu Domain, który EF materializuje (korzenie i encje podrzędne oraz VO mapowane jako
owned). To wyłącznie koncesja perystencyjna — **nie wprowadza żadnej zależności** (Domain nadal
nie referuje niczego), a konstruktor jest `private` (używa go tylko EF przez refleksję).

Reguła dla buildera:
- Korzenie: `Restaurant`, `MenuItem`, `Ingredient`, `Order`, `Customer`, `LoyaltyAccount`,
  `Promotion` — dodać `private Xxx() { }`.
- Encje podrzędne: `MenuItemVariant`, `OrderItem`, `CustomerAddress`, `LoyaltyTransaction`
  — dodać `private Xxx() { }`.
- VO mapowane jako owned: `Address`, `GeoCoordinate`, `ContactDetails`, `DeliveryAddress`,
  `OrderItemExtra` — dodać `private Xxx() { }` (upraszcza binding; nie polegamy na constructor
  binding EF dla zagnieżdżonych owned).
- Kolekcje read-only (`_items`, `_variants`, `_baseIngredients`, `_allowedExtras`,
  `_addressBook`, `_transactions`, `_extras`) mają inline init `= new()` — zostają; EF zapisuje
  do nich przez **field access** (patrz 2.4).
- Pola z `get; private set;` i `get;` (auto-property, np. `Order.Subtotal`, `OrderItem.LineTotal`)
  EF zapisuje przez setter/backing field — bez zmian w Domain.

**Alternatywa odrzucona:** osobne modele perystencyjne (persistence POCO) + mapowanie ręczne
domain↔persistence. Pełna izolacja Domain od EF, ale podwaja liczbę klas i wymaga mapperów w obie
strony dla każdego agregatu — nieproporcjonalny koszt przy tej skali. Prywatny ctor to standardowy
kompromis DDD+EF.

### 2.2 Value Objecty — strategia per VO

| VO | Mapowanie | Kolumny / typ | Uzasadnienie |
|---|---|---|---|
| `Money` | **ValueConverter → `decimal`** | jedna kolumna `numeric(12,2)` (PLN implikowane) | Single-currency (domain-model.md 2.1) — waluta stała, więc druga kolumna byłaby martwa. Konwerter: zapis `m.Amount`, odczyt `new Money(amount)` (ctor domyślnie PLN). Czysty schemat: jedna kolumna na kwotę zamiast `_Amount`+`_Currency`. |
| `Address` | **Owned (`OwnsOne`)** | `Street`, `BuildingNumber`, `ApartmentNumber?`, `City`, `PostalCode`, `Notes?` | Kilka pól tekstowych, przydatne do zapytań/raportów. |
| `GeoCoordinate` | **Owned (`OwnsOne`)** | `Latitude double`, `Longitude double` | 2 kolumny; dystans liczy Domain (ADR-0006), nie baza. |
| `DeliveryAddress` | **Owned zagnieżdżony** | `OwnsOne(DeliveryAddress)` → wewnątrz `OwnsOne(Address)` + `OwnsOne(Coordinate)` | Kompozycja Address+Coordinate. Na `Order` opcjonalny (nullable), na `CustomerAddress` wymagany. |
| `ContactDetails` | **Owned (`OwnsOne`)** | `FullName`, `PhoneNumber`, `Email?` | Zawsze na `Order`. |
| `OpeningHours` | **ValueConverter → `jsonb`** (+ `ValueComparer`) | jedna kolumna `jsonb` | Dictionary<DayOfWeek, IReadOnlyList<TimeRange>> — zbyt złożone na kolumny/tabelę, a to VO nie encja. Konwerter serializuje do DTO perystencyjnego (mapa `int day → [{start,end}]`) i odtwarza przez publiczny ctor `OpeningHours(IReadOnlyDictionary…)`. `ValueComparer` używa `Equals`/`GetHashCode` VO. |
| `TimeRange` | **brak osobnego mapowania** | (część JSON `OpeningHours`) | Nigdzie indziej nieużywany samodzielnie. |
| `OrderItemExtra` | **Owned collection (`OwnsMany`)** | tabela `OrderItemExtras` (FK do `OrderItem`), `IngredientId`, `Name`, `Price` (przez `MoneyConverter`) | Snapshot dodatków; queryowalny, kaskada z pozycją. |

> Konwerter `Money` stosujemy **globalnie** przez `configurationBuilder.Properties<Money>()`
> w `ConfigureConventions` DbContextu — nie trzeba go powtarzać przy każdej właściwości `Money`.
> Precyzja `numeric(12,2)` też ustawiana w konwencji.

### 2.3 Encje podrzędne i kolekcje

| Kolekcja (korzeń) | Typ elementu | Mapowanie | Tabela |
|---|---|---|---|
| `MenuItem.Variants` (`_variants`) | `MenuItemVariant` (encja z `Id`) | `OwnsMany` (owned entity, dostęp tylko przez korzeń) | `MenuItemVariants` |
| `MenuItem.BaseIngredients` (`_baseIngredients`) | `Ingredient` (osobny słownik, własny `DbSet`) | **many-to-many** skip-navigation | join `MenuItemBaseIngredients` |
| `MenuItem.AllowedExtras` (`_allowedExtras`) | `Ingredient` (ten sam słownik) | **many-to-many** skip-navigation | join `MenuItemAllowedExtras` |
| `Order.Items` (`_items`) | `OrderItem` (encja) | `OwnsMany` (+ zagnieżdżony `OwnsMany` Extras) | `OrderItems` |
| `Customer.AddressBook` (`_addressBook`) | `CustomerAddress` (encja z VO) | `OwnsMany` (+ owned `DeliveryAddress`) | `CustomerAddresses` |
| `LoyaltyAccount.Transactions` (`_transactions`) | `LoyaltyTransaction` (encja, append-only) | `OwnsMany` | `LoyaltyTransactions` |

**`Ingredient` jest współdzielonym słownikiem** (osobny korzeń z `DbSet`), więc `BaseIngredients`
i `AllowedExtras` to **dwie osobne relacje wiele-do-wielu do tej samej encji** `Ingredient`.
Konfiguracja: dwa jawnie nazwane join-table przez `HasMany(...).WithMany().UsingEntity(...)`
z field-accessem do `_baseIngredients` / `_allowedExtras`. To najtrudniejszy fragment mapowania
katalogu — musi rozróżnić dwie kolekcje po nazwie join-table, żeby nie kolidowały.

**`OrderItem`, `MenuItemVariant`, `CustomerAddress`, `LoyaltyTransaction`, `OrderItemExtra`** nie
mają własnych `DbSet` — są mapowane jako owned/część agregatu i ładowane z korzeniem. Snapshoty na
`OrderItem` (nazwa, cena) to zwykłe kolumny, nie FK do katalogu (`MenuItemId`/`VariantId` to gołe
`Guid`, bez relacji — zamówienie jest niezależne od późniejszych zmian menu; domain-model.md 5.2).

### 2.4 Field access dla kolekcji read-only
Nawigacje wystawiane jako `IReadOnlyCollection` (przez `_field.AsReadOnly()`) muszą być zapisywane
przez pole. Dla każdej takiej nawigacji builder ustawia `PropertyAccessMode.Field` (EF znajduje
konwencją `_items` dla `Items` itd.). Analogicznie dla dwóch skip-navigation many-to-many.

### 2.5 Czas — timestamptz / UTC (ADR-0010)
Npgsql mapuje `DateTimeOffset` na `timestamptz` automatycznie, **pod warunkiem offsetu zero**.
Domain trzyma UTC (offset 0) — `IClock.UtcNow` musi zwracać `DateTimeOffset` z offsetem zero
(`DateTimeOffset.UtcNow`). Nie ustawiać `EnableLegacyTimestampBehavior`. `TimeZoneId` na
`Restaurant` to zwykły `string` (IANA), używany w Domain do reguł godzinowych.

---

## 3. Dane sidecar poza Domain (ADR-0021)

`GuestTrackingToken` (Iteracja 2) i `ProviderPaymentReference` (ADR-0018) **nie są na `Order`**.
Perystujemy je jako **shadow properties na tabeli `Orders`** (kolumny obok zamówienia, niewidoczne
dla Domain — dokładnie „kolumna obok Order" z ADR-0018):

```
OrderConfiguration:
  builder.Property<Guid?>("GuestTrackingToken");
  builder.Property<string?>("ProviderPaymentReference");
  builder.HasIndex("GuestTrackingToken").IsUnique();   // szybki i bezpieczny lookup gościa
```

`OrderRepository` operuje na nich przez ChangeTracker / `EF.Property`:
- `AddAsync(order, guestTrackingToken, providerPaymentReference, ct)`:
  `_ctx.Orders.Add(order); _ctx.Entry(order).Property("GuestTrackingToken").CurrentValue = guestTrackingToken; …ProviderPaymentReference… ;` (bez SaveChanges — commit robi `IUnitOfWork`).
- `GetByGuestTrackingTokenAsync(token, ct)`:
  `_ctx.Orders.FirstOrDefaultAsync(o => EF.Property<Guid?>(o, "GuestTrackingToken") == token, ct)`
  (z `Include`/owned ładowane automatycznie).
- `SetProviderPaymentReferenceAsync(orderId, reference, ct)`: załaduj `Order`, ustaw shadow property
  (bez commitu — `IUnitOfWork`).
- `GetProviderPaymentReferenceAsync(orderId, ct)`: projekcja
  `_ctx.Orders.Where(o => o.Id == orderId).Select(o => EF.Property<string?>(o, "ProviderPaymentReference")).FirstOrDefaultAsync`.

**Alternatywa odrzucona:** osobna tabela 1:1 (`OrderPaymentReference`). ADR-0018 wprost mówi
„kolumna obok Order"; osobna tabela dokłada join bez zysku — shadow property daje pełną izolację od
Domain przy jednej tabeli.

---

## 4. DbContext, UnitOfWork, design-time (ADR-0025)

### 4.1 `PizzaShopDbContext`
- `DbSet<Restaurant>`, `DbSet<MenuItem>`, `DbSet<Ingredient>`, `DbSet<Order>`, `DbSet<Customer>`,
  `DbSet<LoyaltyAccount>`, `DbSet<Promotion>` (tylko korzenie).
- `OnModelCreating`: `modelBuilder.ApplyConfigurationsFromAssembly(typeof(PizzaShopDbContext).Assembly)`.
- `ConfigureConventions`: globalny `MoneyConverter` + precyzja `numeric(12,2)`.
- Klucze `Guid` generowane po stronie Domain (`Guid.NewGuid()` w fabrykach) → `ValueGeneratedNever()`
  dla `Id` korzeni (EF nie generuje). To ważne: nie używać bazowej sekwencji dla PK.

### 4.2 `UnitOfWork : IUnitOfWork`
Cienki wrapper: `Task<int> SaveChangesAsync(ct) => _ctx.SaveChangesAsync(ct)`. Wstrzykuje ten sam
scoped `PizzaShopDbContext`, co repozytoria (jedna instancja per request → jedna transakcja).
Repozytoria `AddAsync`/`UpdateAsync` **nie** commitują — dodają/oznaczają encje; commit robi
`IUnitOfWork.SaveChangesAsync` (spójne z handlerami z application-layer.md 4.3.1 krok 9, 4.3.3 krok 6).
`UpdateAsync` dla encji załadowanej przez `GetByIdAsync` jest praktycznie no-opem (change tracking),
ale zostawiamy metodę w kontrakcie — implementacja może zrobić `_ctx.Update(entity)` defensywnie.

### 4.3 `NextOrderNumberAsync` (czytelny `Order.Number`)
Numer czytelny (domain-model.md 5, `Order.Number`) generowany w Infrastructure. Rekomendacja:
sekwencja PostgreSQL dzienna, np. `YYYYMMDD-NNNN` (osobna sekwencja/`bigint` licznik w tabeli
pomocniczej albo sekwencja bazodanowa). Prosta wersja na start: sekwencja bazodanowa
`order_number_seq` + format `YYYYMMDD-{seq:D4}`. Detal Infrastructure — nie przecieka do Domain.

### 4.4 Design-time + migracje
- `DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PizzaShopDbContext>` w `Persistence/`
  — czyta connection string z `appsettings`/zmiennej środowiskowej (`PIZZASHOP_DB` / user-secrets),
  buduje `DbContextOptions` z `UseNpgsql`. Dzięki temu `dotnet ef` działa bez uruchamiania Api.
- Komenda (zgodnie z CLAUDE.md):
  `dotnet ef migrations add InitialCreate -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api -o Persistence/Migrations`
- `dotnet ef database update -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api`

---

## 5. Płatności — PayU (ADR-0022)

`PayUPaymentGateway : IPaymentGateway` w `Payments/PayU/`. Typed `HttpClient`. Konfiguracja przez
`IOptions<PayUOptions>` (POS id, `client_id`/`client_secret` OAuth, drugi klucz do podpisu
notyfikacji, `BaseUrl`). **Sandbox = tylko inne wartości konfiguracji** (`BaseUrl` =
`https://secure.snd.payu.com`, testowe POS) — przełączenie na produkcję to zmiana konfiguracji, nie
kodu (ADR-0002).

- **OAuth**: PayU wymaga tokenu `client_credentials` (`POST /pl/standard/user/oauth/authorize`).
  Token cache'owany do wygaśnięcia (in-memory).
- **`InitializePaymentAsync`**: `POST /api/v2_1/orders` (kwota w groszach = `Total.Amount*100`,
  waluta PLN, `continueUrl`, `notifyUrl` = webhook Api, `buyer`, `products`). Odpowiedź → mapujemy
  na `PaymentInitResult(RedirectUrl = redirectUri, ProviderPaymentReference = orderId PayU)`.
  `HttpClient` musi mieć wyłączone auto-redirecty (PayU zwraca 302 z `Location`).
- **`VerifyAndParseNotification(rawBody, headers)`**: **najpierw** weryfikacja podpisu z nagłówka
  `OpenPayU-Signature` (MD5 z `body + drugi klucz`) — to jedyne zabezpieczenie webhooka (endpoint
  bez JWT, ADR-0013). Nieprawidłowy podpis ⇒ port sygnalizuje błąd, handler rzuca
  `InvalidPaymentNotificationException` (Application → 400/401, application-layer.md 5). Po
  weryfikacji: parsuj status i zmapuj (`PayUStatusMapper`).
- **Mapowanie statusów** (`PayUStatusMapper`, PayU → Domain `PaymentStatus`):

  | PayU | `PaymentStatus` (Domain) | Metoda Order (handler) |
  |---|---|---|
  | `PENDING` | Pending | (bez zmiany) |
  | `WAITING_FOR_CONFIRMATION` | Authorized | `AuthorizePayment()` |
  | `COMPLETED` | Paid | `ConfirmPayment()` |
  | `CANCELED` / `REJECTED` | Failed | `FailPayment()` |

  Mapowanie żyje **w Infrastructure** — Domain nie zna PayU (ADR-0002). Handler
  `ConfirmPaymentFromNotificationCommand` woła metodę `Order` wg zmapowanego statusu, idempotentnie
  (powtórka o `Paid` nie robi nielegalnego przejścia — guard clauses Domain).
- **`RefundAsync(PaymentRefundRequest)`**: `POST /api/v2_1/orders/{providerRef}/refunds`. **Musi być
  idempotentny** per zamówienie/referencja: powtórny refund już zrefundowanego = sukces, nie błąd
  (ADR-0018). Jeśli PayU zwróci „już zrefundowane", traktować jako sukces.

---

## 6. Geokodowanie (ADR-0023)

`NominatimGeocodingService : IGeocodingService` (`Geocoding/`). Typed `HttpClient` do OpenStreetMap
Nominatim (`GeocodeAsync(Address) → GeoCoordinate?`, ADR-0006). Wybór: **Nominatim (OSM)** — darmowy,
bez klucza, wystarczający dla jednej pizzerii o niskim wolumenie geokodowań.
- Konfiguracja `GeocodingOptions`: `BaseUrl` (domyślnie `https://nominatim.openstreetmap.org`),
  **`UserAgent`** (wymagany polityką Nominatim — bez niego blokada), `TimeoutSeconds`.
- Zapytanie: `/search?format=jsonv2&street=…&city=…&postalcode=…&country=Poland&limit=1`.
  Brak wyniku ⇒ zwraca `null` (handler `CreateOrderCommand` krok 2 traktuje to jako błąd adresu).
- **Uwaga operacyjna**: polityka Nominatim = max 1 req/s + wymagany User-Agent; dla produkcji
  rozważyć własny hosting Nominatim lub płatnego dostawcę (Google/Mapbox) — to przyszła decyzja,
  wymiana = nowa implementacja `IGeocodingService`, bez zmian w Domain/Application.

**Alternatywy:** Google Geocoding (dokładniejszy, płatny, wymaga klucza) — odłożony do realnej
potrzeby; prosty stub konfiguracyjny (`ConfiguredGeocodingService` zwracający współrzędne z
appsettings) przydatny w dev/test bez sieci — opcjonalnie do rejestracji warunkowej po środowisku.

---

## 7. Granica kompozycji — który port gdzie (ADR-0024)

| Port (Application) | Implementacja | Warstwa | Powód |
|---|---|---|---|
| Repozytoria (7×), `IUnitOfWork` | `*Repository`, `UnitOfWork` | **Infrastructure** | Perystencja EF Core. |
| `IPaymentGateway` | `PayUPaymentGateway` | **Infrastructure** | Integracja zewnętrzna (ADR-0013). |
| `IGeocodingService` | `NominatimGeocodingService` | **Infrastructure** | Integracja zewnętrzna. |
| `IClock` | `SystemClock` | **Infrastructure** | Prosty utility, UTC. |
| `ILoyaltyPolicy` | `LinearLoyaltyPolicy` (placeholder) | **Infrastructure** | ADR-0014, reguła odłożona. |
| `IOrderNotifier` | `SignalROrderNotifier` (używa `IHubContext<OrderTrackingHub>`) | **Api** | SignalR to endpoint webowy; Hub żyje w Api. |
| `ICurrentUser` | `HttpContextCurrentUser` | **Api** | Zależny od `HttpContext`/JWT. |

SignalR **Hub** (`OrderTrackingHub`) i implementacja `IOrderNotifier` należą do `Api` — Api zależy
od Application i legalnie implementuje porty inherentnie webowe. Infrastructure **nie** referuje
`Microsoft.AspNetCore.SignalR`. To rozstrzyga pytanie „hub tu czy w Api": **w Api**.

---

## 8. DependencyInjection (Infrastructure)

`AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`:
1. `AddDbContext<PizzaShopDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("Postgres")))`.
2. `AddScoped<IUnitOfWork, UnitOfWork>()`.
3. Rejestracja 7 repozytoriów (`AddScoped<IOrderRepository, OrderRepository>()` itd.).
4. `AddHttpClient<IPaymentGateway, PayUPaymentGateway>()` + `services.Configure<PayUOptions>(configuration.GetSection("PayU"))`.
5. `AddHttpClient<IGeocodingService, NominatimGeocodingService>()` + `Configure<GeocodingOptions>(...)`.
6. `AddSingleton<IClock, SystemClock>()`.
7. `AddScoped<ILoyaltyPolicy, LinearLoyaltyPolicy>()` (placeholder, ADR-0014).

Api w `Program.cs`: `builder.Services.AddApplication().AddInfrastructure(builder.Configuration)`;
dodatkowo rejestruje `ICurrentUser`, `IOrderNotifier` i mapuje `OrderTrackingHub` (warstwa Api,
poza tym dokumentem). Wymaga też pakietu `Microsoft.EntityFrameworkCore.Design` (migracje) — patrz 4.4.

Pakiety NuGet Infrastructure (`.csproj`):
- `Microsoft.EntityFrameworkCore` (8.x)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.x)
- `Microsoft.EntityFrameworkCore.Design` (host `DesignTimeDbContextFactory` + tooling)
- `Microsoft.Extensions.Http` (typed HttpClient)
- `Microsoft.Extensions.Options.ConfigurationExtensions`

---

## 9. Testy warstwy Infrastructure (ADR-0025)

Dodać projekt **`tests/PizzaShop.Infrastructure.Tests`** (integracyjny). Zakres:
- **Round-trip repozytoriów** na realnym PostgreSQL przez **Testcontainers** (`Testcontainers.PostgreSql`):
  zapis+odczyt każdego agregatu, w tym trudne przypadki — owned `Money` (konwerter), `OpeningHours`
  (jsonb), dwie relacje many-to-many `MenuItem`↔`Ingredient`, sidecar shadow properties
  (`GuestTrackingToken` lookup, `ProviderPaymentReference` set/get), zagnieżdżone owned (`Order.Items`
  → `Extras`, `Customer.AddressBook` → `DeliveryAddress`).
- **Smoke test budowy modelu**: konstrukcja `PizzaShopDbContext.Model` + `Database.Migrate()` na
  świeżym kontenerze — wykrywa błędy konfiguracji zanim trafią na runtime.
- **PayU/geocoding**: testy mapperów (`PayUStatusMapper`, weryfikacja podpisu na znanych wektorach)
  bez sieci; wywołania HTTP mockowane (`HttpMessageHandler` stub) — nie strzelamy do realnego PayU/OSM.

**Dlaczego Testcontainers, nie InMemory/SQLite:** `EF InMemory` nie waliduje mapowania relacyjnego
(ignoruje konwertery, jsonb, many-to-many, owned) → dałby fałszywe zielone. SQLite różni się typami
(brak `jsonb`, `timestamptz`). Testcontainers PostgreSQL testuje dokładnie docelowy provider.
Wymaga Dockera w CI — GitHub Actions to wspiera (ADR-0025). Testy integracyjne trzymać osobno od
szybkich unitów (kategoria/trait), żeby dało się je pominąć lokalnie bez Dockera.

---

## 10. Kolejność implementacji dla buildera

Patrz `docs/decisions.md` ADR-0020..0025 dla uzasadnień. Kolejność w sekcji „Kroki" poniżej
(numeracja spójna z finalną listą przekazaną builderowi).
