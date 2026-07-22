# Model domenowy — PizzaShop (wersja 1)

Dokument opisuje encje, value objecty, relacje i reguły biznesowe warstwy `Domain`.
Domain nie zależy od żadnej innej warstwy. Wszystkie znaczniki czasu: `DateTimeOffset`
w UTC (ADR-0010).

Legenda:
- **Encja** — ma tożsamość (`Id`), cykl życia, jest trwała.
- **Value Object (VO)** — bez tożsamości, porównywany po wartości, niemutowalny.
- **Agregat** — granica spójności; korzeń agregatu odpowiada za niezmienniki wewnątrz.

---

## 1. Przegląd agregatów

| Agregat (korzeń) | Zawiera | Odpowiedzialność |
|---|---|---|
| `Restaurant` | OpeningHours, GeoCoordinate, DeliveryRadius | Konfiguracja lokalizacji, godziny, obszar dostawy |
| `MenuItem` (Pizza) | Variants, ingredienty bazowe, dostępne dodatki | Katalog i ceny |
| `Order` | OrderItem[], ContactDetails, DeliveryAddress | Realizacja + płatność zamówienia |
| `Customer` | DeliveryAddress[] (książka adresowa) | Profil zakupowy klienta |
| `LoyaltyAccount` | LoyaltyTransaction[] | Saldo i historia punktów |
| `Promotion` | reguły kwalifikacji/rabatu | Definicja promocji |

`UserAccount` (tożsamość/login/rola) jest bytem Application/Infrastructure, nie Domain
(ADR-0004, ADR-0005). Domain zna jedynie `UserAccountId` jako referencję.

---

## 2. Value Objecty

### 2.1 `Money`
- `Amount` (decimal), `Currency` (na start stałe `PLN`).
- Reguły: `Amount >= 0` (dla cen); operacje `Add`, `Subtract`, `Multiply(int qty)`
  zachowują walutę; nie można dodawać różnych walut.
- Używany wszędzie zamiast gołego `decimal`.

### 2.2 `GeoCoordinate`
- `Latitude` (-90..90), `Longitude` (-180..180).
- Metoda `DistanceKmTo(GeoCoordinate other)` — dystans Haversine (ADR-0006).
- Walidacja zakresów w konstruktorze.

### 2.3 `Address`
- `Street`, `BuildingNumber`, `ApartmentNumber?`, `City`, `PostalCode`, `Notes?`.
- Czysto adresowy VO (bez geolokalizacji ani tożsamości).

### 2.4 `DeliveryAddress`
- Kompozycja: `Address` + `GeoCoordinate` (współrzędne wymagane do walidacji promienia).
- W książce adresowej klienta jest encją podrzędną (ma `Id`, alias `Label` np. „Dom",
  „Praca"); w `Order` zapisywany jako **snapshot VO** (kopia w chwili zamówienia).

### 2.5 `ContactDetails`
- `FullName`, `PhoneNumber`, `Email?`.
- Reguły: `FullName` i `PhoneNumber` wymagane; `Email` opcjonalny (wymagany, jeśli
  płatność online — do notyfikacji PayU; do potwierdzenia z biznesem).
- Zawsze obecny na `Order` (także dla gościa — ADR-0005).

### 2.6 `OpeningHours`
- Zbiór wpisów per dzień tygodnia: `DayOfWeek → (OpenTime, CloseTime)` lub „zamknięte".
- Obsługuje wiele przedziałów na dzień (np. przerwa) — lista `TimeRange` per dzień.
- Metoda `IsOpenAt(DateTimeOffset instant, TimeZoneInfo tz)` — używana do walidacji
  `RequestedFulfillmentTime` i możliwości składania zamówień.
- `TimeRange` (VO): `Start`, `End` (czas lokalny, bez daty).

### 2.7 `MenuItemVariant` — patrz sekcja MenuItem (wariant rozmiaru/ceny).

---

## 3. Restaurant (agregat, single-tenant — ADR-0003)

Pojedynczy rekord konfiguracyjny lokalizacji.

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `Name` | string | Nazwa pizzerii |
| `Address` | Address | Adres siedziby |
| `Location` | GeoCoordinate | Środek obszaru dostawy |
| `DeliveryRadiusKm` | double | Promień dostawy (ADR-0006), `> 0` |
| `TimeZoneId` | string (IANA) | Strefa lokalna (ADR-0010) |
| `OpeningHours` | OpeningHours | |
| `ContactPhone` | string | |
| `IsAcceptingOrders` | bool | Ręczny wyłącznik przyjmowania zamówień |
| `MinimumOrderValue` | Money? | Minimalna wartość zamówienia (opcjonalna) |
| `FreeDeliveryThreshold` | Money? | Próg darmowej dostawy (opcjonalny) |
| `DeliveryFee` | Money | Opłata za dostawę (poniżej progu) |

**Reguły biznesowe:**
- `DeliveryRadiusKm > 0`.
- `IsWithinDeliveryArea(GeoCoordinate point)` ⇒ `Location.DistanceKmTo(point) <=
  DeliveryRadiusKm`.
- `CanAcceptOrderAt(DateTimeOffset when)` ⇒ `IsAcceptingOrders &&
  OpeningHours.IsOpenAt(when, tz)`.
- Zmiana godzin/promienia dozwolona tylko dla RestaurantAdmin (egzekwuje Application/Api,
  nie Domain).

---

## 4. Katalog: MenuItem, Pizza, Ingredient, dodatki

Modelujemy katalog przez ogólny `MenuItem` z podtypami; `Pizza` to główny przypadek.
Alternatywę (osobna hierarchia klas per typ) rozważono — wybrano `MenuItem` + `Category`
+ warianty dla prostoty (patrz „Notatki projektowe").

### 4.1 `MenuItem` (agregat)

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `Name` | string | |
| `Description` | string? | |
| `Category` | MenuCategory (enum) | `Pizza`, `Drink`, `Side`, `Dessert`, `Sauce` |
| `BasePrice` | Money | Cena wariantu domyślnego / pozycji bez wariantów |
| `IsAvailable` | bool | Widoczność/dostępność w menu |
| `Variants` | List<MenuItemVariant> | Rozmiary (np. 30/40/50 cm) |
| `BaseIngredients` | List<Ingredient ref> | Składniki wliczone (tylko dla pizzy) |
| `AllowedExtras` | List<Ingredient ref> | Dozwolone dodatki (tylko dla pizzy) |
| `ImageUrl` | string? | |

### 4.2 `MenuItemVariant` (encja podrzędna)
- `Id`, `Name` (np. „Mała 30cm"), `Price` (Money), `IsDefault` (bool).
- Reguła: dokładnie jeden `IsDefault == true`, gdy lista wariantów niepusta.
- **Mutatory są `internal`** — wariant edytuje wyłącznie korzeń agregatu (`MenuItem`),
  nigdy Application bezpośrednio. Metody wariantu:
  - `Rename(string name)` — guard: nazwa niepusta.
  - `UpdatePrice(Money price)` — guard: `price != null`.
  - `MarkDefault()` / `UnsetDefault()` — przełączanie flagi domyślności; woła je tylko
    `MenuItem` (np. z `SetDefaultVariant`/`AddVariant`).

### 4.3 `Ingredient` (encja słownikowa)
- `Id`, `Name`, `ExtraPrice` (Money — koszt jako dodatek), `IsAvailable`,
  `Category` (np. `Cheese`, `Meat`, `Vegetable`, `Sauce`) — opcjonalnie.

**Reguły biznesowe MenuItem:**
- **Pizza musi mieć minimum jeden składnik bazowy** (`Category == Pizza` ⇒
  `BaseIngredients.Count >= 1`). (Reguła z CLAUDE.md, żyje w Domain.)
- Dodatek dodawany do pozycji koszyka musi należeć do `AllowedExtras` danego MenuItem.
- Nie można zamówić pozycji z `IsAvailable == false`.
- Jeśli MenuItem ma warianty, wybór wariantu jest wymagany przy zamawianiu; cena pozycji
  bierze się z wariantu, nie z `BasePrice`.
- `ResolvePrice(variantId)` w Domain rozstrzyga cenę jednostkową i wybrany wariant przy
  dodawaniu pozycji do zamówienia, egzekwując dostępność, poprawność konfiguracji katalogu
  i regułę „wariant wymagany, gdy istnieją warianty".

### 4.4 Cykl życia i edycja `MenuItem` (metody agregatu)

Edycja katalogu (use case `UpdateMenuItemCommand`, RestaurantAdmin) przechodzi wyłącznie
przez metody korzenia agregatu — Application nie mutuje `MenuItemVariant` bezpośrednio.
Istniejące metody: `Rename(string)`, `UpdateBasePrice(Money)`, `MarkAvailable()`,
`MarkUnavailable()`, `Add/RemoveBaseIngredient`, `Allow/DisallowExtra`, `AddVariant`.
Poniżej metody domykające lukę edycji (Iteracja 1 Application):

**Pola opisowe:**
- `UpdateDetails(string? description, string? imageUrl)` — pełne podstawienie obu pól
  (semantyka PUT: `null` = wyczyść wartość). Bez guardów — oba pola są opcjonalne i nie
  niosą niezmienników. Świadomie **jedna** metoda zbiorcza zamiast dwóch osobnych setterów:
  w panelu admina opis i zdjęcie edytuje się łącznie, a brak guardów nie uzasadnia
  osobnych metod (w przeciwieństwie do `Rename`/`UpdateBasePrice`, które mają własne
  walidacje i zostają rozdzielone). Alternatywa (osobne `UpdateDescription`/`UpdateImageUrl`)
  — odrzucona jako mnożenie metod bez korzyści.

**Warianty (rozmiary):**
- `SetDefaultVariant(Guid variantId)` — ustawia wskazany wariant jako domyślny i odwołuje
  poprzedni domyślny (`MarkDefault` na wskazanym, `UnsetDefault` na pozostałych).
  Idempotentne, gdy wskazany już jest domyślny. Guard: `variantId` musi istnieć na liście,
  inaczej `InvalidVariantConfigurationException`. To **jedyna** droga zmiany domyślności na
  istniejącej liście (wcześniej dawał to tylko `AddVariant` nowego wariantu — stąd wcześniej
  ciche ignorowanie zmiany `IsDefault` na istniejącym wariancie; patrz ADR-0016).
- `RemoveVariant(Guid variantId)` — usuwa wariant, pilnując niezmienników:
  - guard: wariant musi istnieć — inaczej `InvalidVariantConfigurationException`;
  - guard: nie można usunąć **jedynego** wariantu (`Variants.Count == 1`) ⇒
    `CannotRemoveLastVariantException`. MenuItem raz skonfigurowany z wariantami nie wraca do
    trybu cenowego bez wariantów przez usuwanie; konwersja na pozycję bez wariantów = celowe
    odtworzenie pozycji (rzadka akcja admina). Uwaga: pozycja może istnieć **bez żadnych**
    wariantów (wtedy cena = `BasePrice`) — ale tylko gdy taka powstała; `RemoveVariant` nie
    prowadzi do stanu pustej listy;
  - guard: nie można usunąć wariantu **domyślnego**, gdy istnieją inne, bez uprzedniego
    wskazania nowego domyślnego ⇒ `InvalidVariantConfigurationException` (komunikat kieruje
    do `SetDefaultVariant`). Decyzja świadoma — wymuszenie jawnego wyboru zamiast
    auto-promocji dowolnego pozostałego wariantu (ADR-0016).
- `RenameVariant(Guid variantId, string name)` — zmiana nazwy istniejącego wariantu przez
  korzeń (deleguje do `internal MenuItemVariant.Rename`). Guard: wariant musi istnieć
  (`InvalidVariantConfigurationException`), nazwa niepusta (guard w wariancie).
- `UpdateVariantPrice(Guid variantId, Money price)` — zmiana ceny istniejącego wariantu
  przez korzeń (deleguje do `internal MenuItemVariant.UpdatePrice`). Guard: wariant musi
  istnieć, `price != null`.

**Kolejność przy edycji zestawu wariantów (dla handlera):** jeśli edycja zmienia domyślny i
usuwa stary domyślny, handler musi najpierw `SetDefaultVariant(nowy)`, potem
`RemoveVariant(stary)` — odwrotna kolejność rzuci `InvalidVariantConfigurationException`.
Po zakończeniu edycji handler woła `EnsureValidCatalogConfiguration()` jako siatkę
bezpieczeństwa (min. 1 składnik dla pizzy, dokładnie jeden domyślny wariant przy niepustej
liście) przed zapisem.

---

## 5. Order (agregat) — serce domeny

Zamówienie wiąże pozycje, dane kontaktowe, tryb realizacji, płatność i statusy.

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `Number` | string | Ludzki numer zamówienia (sekwencyjny/czytelny) |
| `CustomerId` | Guid? | null = zamówienie gościa (ADR-0005) |
| `Contact` | ContactDetails | Zawsze wymagany (snapshot) |
| `FulfillmentType` | enum `Delivery` \| `Pickup` | |
| `DeliveryAddress` | DeliveryAddress? | Wymagany dla `Delivery`, null dla `Pickup` (snapshot VO) |
| `Items` | List<OrderItem> | ≥ 1 |
| `PlacedAt` | DateTimeOffset | Moment złożenia |
| `RequestedFulfillmentTime` | DateTimeOffset? | null = ASAP (ADR-0008) |
| `EstimatedReadyAt` | DateTimeOffset? | Ustawiane przez obsługę po przyjęciu (ADR-0008) |
| `Status` | OrderStatus | Cykl realizacji (ADR-0007) |
| `PaymentMethod` | enum `Online` \| `OnPickup` | ADR-0007 |
| `PaymentStatus` | PaymentStatus | Cykl płatności, niezależny (ADR-0007) |
| `AppliedPromotionId` | Guid? | Zastosowana promocja (jeśli jest) |
| `PointsToEarn` | int | Punkty do naliczenia po `Completed` (wyliczane przez politykę) |
| `PointsRedeemed` | int | Punkty użyte na to zamówienie |
| Kwoty | patrz niżej | |

### 5.1 Kwoty (wyliczane / utrwalane jako snapshot)
- `Subtotal` (Money) — suma `OrderItem.LineTotal`.
- `DiscountAmount` (Money) — rabat z promocji + wartość punktów.
- `DeliveryFee` (Money) — 0 dla Pickup lub gdy `Subtotal >= FreeDeliveryThreshold`.
- `Total` (Money) = `Subtotal - DiscountAmount + DeliveryFee`.
- Kwoty wyliczane w Domain przy budowaniu/aktualizacji zamówienia i utrwalane (żeby
  późniejsza zmiana cennika nie zmieniała historycznego zamówienia).

### 5.2 `OrderItem` (encja podrzędna)

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `MenuItemId` | Guid | Referencja do katalogu |
| `MenuItemName` | string | Snapshot nazwy |
| `VariantId` | Guid? | Wybrany wariant |
| `VariantName` | string? | Snapshot |
| `UnitPrice` | Money | Cena jednostkowa (wariant lub base) — snapshot |
| `Quantity` | int | ≥ 1 |
| `Extras` | List<OrderItemExtra> | Dodatki (snapshot: nazwa + cena) |
| `Notes` | string? | Uwagi klienta (np. „bez cebuli") |
| `LineTotal` | Money | `(UnitPrice + Σ Extras) * Quantity` |

`OrderItemExtra` (VO): `IngredientId`, `Name`, `Price` (snapshot).

**Zasada snapshotów:** OrderItem kopiuje nazwy i ceny z katalogu w chwili zamówienia.
Zamówienie jest niezależne od późniejszych zmian menu/cen.

### 5.3 Enumy statusów

`OrderStatus` (realizacja):
```
PendingAcceptance  → Accepted → InPreparation → Ready → OutForDelivery → Completed
                   ↘ Rejected
(dowolny przed Completed) → Cancelled
```
- `OutForDelivery` tylko dla `FulfillmentType == Delivery`; dla `Pickup` przejście
  `Ready → Completed` (odbiór).
- `Rejected` — odrzucenie przez obsługę (np. poza godzinami, brak możliwości realizacji).
- `Cancelled` — anulowanie (klient przed `Accepted`, lub obsługa; reguły kto/kiedy w
  Application).

`PaymentStatus` (płatność) — dozwolone przejścia (jednoznacznie, zgodnie z `Order.cs`):

```
        ┌────────────────→ Failed ←──────────────┐
        │                                         │
Pending ──→ Authorized ──→ Paid ──→ Refunded
   │                        ↑
   └────────────────────────┘   (Pending → Paid: ścieżka minimalna, bez etapu Authorized)
```

Lista dozwolonych przejść:
- `Pending → Authorized` — autoryzacja środków przez PayU (etap opcjonalny).
- `Pending → Paid` — natychmiastowe potwierdzenie płatności (minimalna ścieżka).
- `Pending → Failed` — płatność nieudana/odrzucona zanim doszło do autoryzacji.
- `Authorized → Paid` — pobranie zautoryzowanych środków (capture).
- `Authorized → Failed` — capture nieudany po wcześniejszej autoryzacji.
- `Paid → Refunded` — zwrot (np. anulowanie opłaconego zamówienia online).

**Decyzja świadoma:** `Failed` jest osiągalny zarówno z `Pending`, jak i z `Authorized`,
bo płatność online może zawieść na dwóch etapach — przy inicjalnej autoryzacji oraz przy
pobraniu wcześniej zautoryzowanych środków (capture). Modelowanie tylko `Pending → Failed`
maskowałoby ten drugi, realny scenariusz PayU.

`PaymentMethod`: `Online` | `OnPickup`.
`FulfillmentType`: `Delivery` | `Pickup`.

### 5.4 Reguły biznesowe Order (niezmienniki agregatu)
1. **Minimum jedna pozycja:** `Items.Count >= 1` — zamówienie bez pozycji nie istnieje.
2. **Delivery wymaga adresu:** `FulfillmentType == Delivery ⇒ DeliveryAddress != null`.
3. **Adres w obszarze dostawy:** dla Delivery — `DeliveryAddress` musi spełniać
   `Restaurant.IsWithinDeliveryArea(coord)` (walidacja przy składaniu; ADR-0006).
4. **Minimalna wartość:** jeśli `Restaurant.MinimumOrderValue` ustawione ⇒
   `Subtotal >= MinimumOrderValue`.
5. **Darmowa dostawa:** `DeliveryFee == 0`, gdy `Pickup` lub
   `Subtotal >= FreeDeliveryThreshold`; inaczej `DeliveryFee == Restaurant.DeliveryFee`.
   (Reguła progu z CLAUDE.md — żyje w Domain.)
6. **Godziny pracy:** `RequestedFulfillmentTime`, jeśli podany, musi mieścić się w
   `OpeningHours` i nie być w przeszłości; ASAP dozwolony tylko gdy `CanAcceptOrderAt(now)`.
7. **Przejścia statusów** dozwolone wyłącznie zgodnie z grafem (guard clauses); próba
   nielegalnego przejścia ⇒ `DomainException`.
8. **Sprzężenie płatności z realizacją (ADR-0007):**
   - `PaymentMethod == Online` ⇒ przejście do `Accepted` dozwolone dopiero, gdy
     `PaymentStatus == Paid`.
   - `PaymentMethod == OnPickup` ⇒ realizacja niezależna od `PaymentStatus`; `Paid`
     ustawiane przy odbiorze.
9. **EstimatedReadyAt** ustawiany od stanu `Accepted` wzwyż; nie może być w przeszłości
   względem `PlacedAt`.
10. **Punkty:** `PointsRedeemed` (i wynikający rabat) tylko gdy `CustomerId != null`
    i saldo `LoyaltyAccount` wystarcza; `PointsToEarn` naliczane po `Completed`.

---

## 6. Customer (agregat)

Profil zakupowy zarejestrowanego klienta (ADR-0005). Gość nie ma tej encji.

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `UserAccountId` | Guid | Referencja do tożsamości (rola Customer) |
| `FullName` | string | |
| `Email` | string | |
| `PhoneNumber` | string? | |
| `AddressBook` | List<DeliveryAddress> | Zapisane adresy (encje podrzędne z `Label`) |
| `CreatedAt` | DateTimeOffset | |

**Reguły:**
- `Email` unikalny (egzekwowane w Infrastructure/Application, spójne z kontem).
- Adres w książce może być domyślny (`IsDefault`) — max jeden.
- Odwołanie do adresu spoza książki klienta ⇒ `AddressNotInAddressBookException`.
- Usunięcie konta ≠ usunięcie historycznych zamówień (te trzymają snapshoty).
- **Powiązanie 1:1 z `LoyaltyAccount` jest jednokierunkowe** — trzyma je wyłącznie strona
  zależna (`LoyaltyAccount.CustomerId`), `Customer` nie przechowuje `LoyaltyAccountId`
  (ADR-0029). Konto lojalnościowe klienta odnajduje się przez repozytorium
  (`ILoyaltyAccountRepository.GetByCustomerIdAsync(customerId)`), nie przez nawigację z
  `Customer`. Dzięki temu `Customer.Create` nie zależy od `LoyaltyAccount` (brak cyklu przy
  tworzeniu — ADR-0029).

---

## 7. LoyaltyAccount + LoyaltyTransaction (agregat — szkielet, ADR-0009; przelicznik ADR-0033)

### 7.1 `LoyaltyAccount`
| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `CustomerId` | Guid | 1:1 — **jedyna** strona powiązania (ADR-0029); unikalny w bazie |
| `PointsBalance` | int | Saldo = Σ transakcji (utrzymywane/wyliczalne) |
| `Transactions` | List<LoyaltyTransaction> | Append-only |

`LoyaltyAccount` jest stroną **zależną** relacji 1:1 (nie istnieje bez `Customer`), więc to
ono trzyma referencję `CustomerId`; `Customer` nie trzyma referencji zwrotnej (ADR-0029).
`CustomerId` ma unikalny indeks (twardy strażnik reguły „jedno konto na klienta"). Przy
rejestracji `LoyaltyAccount.Create(customerId)` wołane jest **po** utworzeniu `Customer`
(kolejność łamie cykl bez wspólnego, wstępnie generowanego Id).

### 7.2 `LoyaltyTransaction` (encja podrzędna, niemutowalna)
| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `Type` | enum `Earned` \| `Redeemed` \| `Adjusted` \| `Expired` | |
| `Points` | int | dodatnie (Earned/Adjusted+) lub ujemne (Redeemed/Expired) |
| `Reason` | string | Opis (np. „Zamówienie #1234", „Korekta") |
| `OrderId` | Guid? | Powiązanie z zamówieniem, jeśli dotyczy |
| `OccurredAt` | DateTimeOffset | |

**Reguły:**
- Historia jest append-only; nie modyfikujemy istniejących transakcji.
- `PointsBalance` nigdy < 0 — nie można wydać więcej punktów niż saldo.
- **Przelicznik NIE jest w Domain encji** — żyje za `ILoyaltyPolicy` w Application (ADR-0009).
  Encje tylko rejestrują skutki. Reguła przelicznika jest już **finalna** (ADR-0033), nie
  placeholder: naliczanie **1 pkt za każdy pełny 1 PLN** wartości `Subtotal` (floor), wymiana
  **1 pkt = 0,05 PLN** rabatu, **bez górnego limitu procentowego** pokrycia zamówienia punktami
  (jedyny naturalny limit = saldo klienta). Abstrakcja `ILoyaltyPolicy` pozostaje mimo ustalenia
  reguły — to nadal wymienialna implementacja portu, gdyby biznes kiedyś zmienił zasady, bez
  migracji modelu punktów. Wygasanie (`Expired`) przewidziane w typie transakcji, mechanizm
  (job/termin) nadal odłożony.

---

## 8. Promotion (agregat)

Promocje wymienione w zakresie (katalog: pizze, dodatki, promocje). Projektujemy zarys
elastyczny; typy rabatowe: `Percentage`, `FixedAmount`, `FreeDelivery`, `BuyXGetY`
(ten ostatni zaimplementowany w ADR-0034 — patrz 8.2).

| Atrybut | Typ | Uwagi |
|---|---|---|
| `Id` | Guid | |
| `Name` | string | |
| `Code` | string? | Kod kuponu (opcjonalny; null = automatyczna). Normalizowany do UPPER-INVARIANT |
| `Type` | enum `Percentage` \| `FixedAmount` \| `FreeDelivery` \| `BuyXGetY` | Niemutowalny po utworzeniu (patrz 8.1) |
| `Value` | decimal? | % lub kwota, zależnie od typu; **null** dla `FreeDelivery` i `BuyXGetY` |
| `BuyXGetYRule` | BuyXGetYRule? | Konfiguracja BuyXGetY (owned VO), obecna **iff** `Type == BuyXGetY` (8.2, ADR-0034) |
| `MinOrderValue` | Money? | Próg kwalifikacji |
| `ValidFrom` / `ValidTo` | DateTimeOffset | Okno ważności |
| `IsActive` | bool | |
| `UsageLimit` | int? | Globalny limit użyć (null = bez limitu) |
| `UsageCount` | int | Licznik wykorzystań (domyślnie 0, inkrementowany przez `RecordUsage()`) |

**Reguły (zarys):**
- Promocja kwalifikuje się (`IsQualifiedFor`), gdy: `IsActive`, w oknie `ValidFrom..ValidTo`,
  `Subtotal >= MinOrderValue`, `UsageLimit` nie wyczerpany (`UsageCount < UsageLimit`),
  ewentualny kod zgodny. Dla `BuyXGetY` `IsQualifiedFor` sprawdza wyłącznie te **generyczne
  bramki** (nie zna pozycji); kwalifikację liniową (dość sztuk wyzwalacza) rozstrzyga
  `CalculateDiscount` na kontekście — patrz 8.2.
- **`UsageCount` + `RecordUsage()`** — licznik wykorzystań utrzymywany w agregacie, bo bez
  niego `UsageLimit` byłby polem martwym: kwalifikacja musi móc odrzucić promocję po
  osiągnięciu limitu. `RecordUsage()` inkrementuje licznik przy zastosowaniu promocji do
  zamówienia i rzuca `PromotionNotApplicableException`, jeśli limit już wyczerpany.
  Warstwa Application wywołuje `RecordUsage()` w tej samej transakcji, w której utrwala
  `Order.AppliedPromotionId` (spójność licznika z faktycznymi użyciami).
- Wyliczenie rabatu (`CalculateDiscount(OrderDiscountContext ctx)`, ADR-0034): `Percentage`
  (% od subtotalu, zaokrąglony do 2 miejsc), `FixedAmount` (kwota, nie więcej niż subtotal),
  `FreeDelivery` (= `DeliveryFee`), `BuyXGetY` (rabat na sztuki nagrody wg reguły — 8.2).
  Sygnatura przyjmuje **kontekst zamówienia** (pozycje) zamiast samego subtotalu, bo `BuyXGetY`
  potrzebuje pozycji; `IsQualifiedFor` (bramki generyczne) pozostaje na subtotalu.
- Na jedno zamówienie na start: **jedna** promocja (`Order.AppliedPromotionId`).
  Stackowanie promocji = przyszła decyzja.

### 8.1 Cykl życia i edycja `Promotion` (metody agregatu)

Edycja promocji (use case `UpdatePromotionCommand`, RestaurantAdmin) przechodzi przez metody
korzenia agregatu. Istniejące metody: `Activate()`, `Deactivate()`. Poniżej metody domykające
lukę edycji (Iteracja 4 Application) — zakres z application-layer.md 4.5: „aktywacja/
dezaktywacja, okno, wartość, limit". Wzorzec ten sam co dla `MenuItem` (4.4, ADR-0016): kilka
małych, celowych metod z własnymi guardami zamiast jednej zbiorczej `UpdateDetails(...)` —
grupujemy tylko pola sprzężone niezmiennikiem (okno) w jedną metodę.

- `UpdateWindow(DateTimeOffset validFrom, DateTimeOffset validTo)` — podstawia oba końce okna
  ważności naraz. Guard: `validTo > validFrom`, inaczej `ArgumentException(nameof(validTo))` —
  ten sam typ i reguła co w `Create` (nie `DomainException`: to niezmiennik argumentów, nie
  stanowa reguła biznesowa). **Oba pola w jednej metodzie**, bo są sprzężone niezmiennikiem
  (`ValidTo > ValidFrom`) — dwie osobne metody `UpdateValidFrom`/`UpdateValidTo` dopuszczałyby
  przejściowy stan łamiący niezmiennik (analogia: warianty grupowane pod korzeniem MenuItem).
  **Brak sprzężenia z `UsageCount`:** okno wolno przesunąć/skrócić/wydłużyć dowolnie, także
  tak, że bieżący czas wypada poza nim (promocja przestaje kwalifikować się na przyszłość).
  Już zarejestrowane użycia (`UsageCount`) nie mają znaczników czasu w agregacie i są
  snapshotowane na `Order` (5.1), więc zmiana okna niczego wstecznie nie unieważnia — żadnego
  guardu wiążącego okno z `UsageCount` nie ma.
- `UpdateValue(decimal? value)` — podstawia wartość rabatu. Guard: ta sama, zależna od typu
  walidacja co w `Create` (`Percentage` ⇒ `0 < value <= 100`; `FixedAmount` ⇒ `value > 0`;
  `FreeDelivery`/`BuyXGetY` nie walidują wartości — parytet z `Create`), inaczej
  `ArgumentOutOfRangeException(nameof(value))`. **Dozwolone niezależnie od `UsageCount > 0`:**
  zmiana wartości nie wpływa na już złożone zamówienia, bo `Order.DiscountAmount` jest
  snapshotowany w chwili złożenia (5.1) — zmiana dotyczy wyłącznie przyszłych zastosowań.
  Nie wprowadzamy więc blokady „nie zmieniaj wartości po pierwszym użyciu" (byłaby myląca:
  sugerowałaby wpływ na przeszłość, którego nie ma).
- `UpdateUsageLimit(int? usageLimit)` — podstawia globalny limit użyć (`null` = bez limitu).
  Guard: `usageLimit > 0`, gdy ustawiony (`null` dozwolony), inaczej
  `ArgumentOutOfRangeException(nameof(usageLimit))` — ten sam co w `Create`.
  **Ustawienie limitu poniżej bieżącego `UsageCount` jest DOZWOLONE** (ADR-0019): np. promocja
  użyta 5 razy, admin ustawia limit 3. Skutkiem jest natychmiastowe zamknięcie promocji na
  nowe użycia (`IsQualifiedFor` i `RecordUsage` już odrzucają, gdy `UsageCount >= UsageLimit`),
  bez naruszenia jakiegokolwiek niezmiennika — `UsageCount` to fakt historyczny (użycia
  snapshotowane na zamówieniach), którego obniżenie limitu nie cofa. Nie blokujemy tego, bo
  jest to legalna operacyjnie droga „domknij tę promocję limitem" obok `Deactivate()`.

**Typ (`Type`) pozostaje niemutowalny** — brak metody zmiany. Zakres 4.5 nie wymienia typu, a
zmiana typu to w praktyce inna promocja (zmieniłaby regułę walidacji `Value`/reguły BuyXGetY).
Zmiana typu = utworzenie nowej promocji. **Reguła BuyXGetY (`BuyXGetYRule`) również jest
niemutowalna** po utworzeniu (8.2) — z tego samego powodu co `Type`: definiuje istotę promocji;
`UpdatePromotionCommand` jej nie dotyka, zmiana = nowa promocja.

**Poza zakresem tej iteracji:** `Name`, `Code`, `MinOrderValue` nie mają jeszcze mutatorów —
application-layer.md 4.5 ich nie wymaga. Dodać w razie realnej potrzeby (analogicznie: `Name`
z guardem niepustości, `Code` przez `NormalizeCode`, `MinOrderValue` bez guardu), nie na zapas.

**Bez nowych wyjątków domenowych:** wszystkie guardy powyżej reużywają typów argumentowych z
`Create` (`ArgumentException`/`ArgumentOutOfRangeException`), a świadoma decyzja „limit poniżej
`UsageCount` dozwolony" celowo nie wprowadza wyjątku blokującego.

### 8.2 BuyXGetY — konfiguracja i wyliczenie (ADR-0034)

`BuyXGetYRule` (owned VO na `Promotion`, obecny **iff** `Type == BuyXGetY`):

| Pole | Typ | Reguła |
|---|---|---|
| `TriggerMenuItemId` | Guid | Produkt-wyzwalacz; `!= Guid.Empty` |
| `BuyQuantity` (X) | int | `>= 1` — ile sztuk wyzwalacza uruchamia jeden zestaw |
| `RewardMenuItemId` | Guid | Produkt-nagroda; `!= Guid.Empty`; może równać się wyzwalaczowi |
| `GetQuantity` (Y) | int | `>= 1` — ile sztuk nagrody rabatowanych na zestaw |
| `RewardDiscountPercentage` | decimal | `0 < pct <= 100` (100 = gratis, <100 = taniej) |

Cała konfiguracja BuyXGetY żyje w tym VO; `Promotion.Value` pozostaje `null` dla tego typu.
`Promotion.Create` przyjmuje opcjonalny `BuyXGetYRule`; walidacja fabryki: `Type == BuyXGetY`
⇒ reguła wymagana i `Value == null`; inne typy ⇒ reguła `null`. Guardy pól w konstruktorze VO
(`ArgumentException`/`ArgumentOutOfRangeException`).

**`OrderDiscountContext`** (VO Domain przekazywany do `CalculateDiscount`, **bez** referencji do
encji `Order`/`OrderItem` — Promotion pozostaje odsprzężone od agregatu Order, ADR-0011):
- `Subtotal` (Money), `DeliveryFee` (Money), `When` (DateTimeOffset), `SuppliedCode` (string?),
- `Lines`: lista `OrderDiscountLine(Guid MenuItemId, Money UnitPrice, int Quantity)`.

Kontekst buduje warstwa Application (handler) z `order.Items` — Domain nie tworzy zależności
Promotion → Order.

**Semantyka `CalculateDiscount` dla `BuyXGetY`:**
- `triggerUnits` = Σ `Quantity` linii z `MenuItemId == TriggerMenuItemId`.
- **Nagroda == wyzwalacz** (`RewardMenuItemId == TriggerMenuItemId`): rozmiar zestawu = `X + Y`;
  `zestawy = floor(triggerUnits / (X + Y))`; sztuk rabatowanych = `zestawy * Y`. (Semantyka „N za
  M": w koszyku masz X+Y, płacisz za X.)
- **Nagroda ≠ wyzwalacz:** `zestawy = floor(triggerUnits / X)`; `rewardUnits` = Σ `Quantity` linii
  produktu-nagrody; sztuk rabatowanych = `min(zestawy * Y, rewardUnits)`.
- Rabatowane są **najtańsze** sztuki produktu-nagrody (po `UnitPrice`) — deterministycznie i
  jednoznacznie przy różnych wariantach. Rabat = Σ po sztukach rabatowanych `round(UnitPrice *
  pct/100, 2)`, waluta z subtotalu.
- Wartościujemy po `UnitPrice` (cena bazowa wariantu, **bez dodatków**) — gwarantuje rabat ≤
  subtotal.
- **Kwalifikacja liniowa:** za mało wyzwalaczy (brak choćby jednego pełnego zestawu) lub
  (cross-product) brak nagrody w koszyku ⇒ `PromotionNotApplicableException`. Bramki generyczne
  (`IsActive`, okno, `MinOrderValue`, `UsageLimit`, kod) obowiązują jak dla innych typów.

**Wielokrotna kwalifikacja (stacking zestawów)** jest wspierana (floor z dzielenia — wiele
zestawów w jednym zamówieniu). **Świadomie poza zakresem startowym:** wyzwalacz/nagroda jako
„dowolna pizza / zakres kategorii" (dziś tylko konkretny `MenuItemId`), automatyczne dodanie
gratisowej pozycji do zamówienia (rabatujemy tylko sztuki nagrody faktycznie zamówione), edycja
reguły po utworzeniu (zmiana = nowa promocja — jak `Type`).

---

## 9. Wyjątki domenowe

Wszystkie dziedziczą po `DomainException` (bazowy), mapowanym na kody HTTP w middleware
`Api` (CLAUDE.md). Pełna lista zaimplementowanych wyjątków (stan zgodny z kodem
`src/PizzaShop.Domain/Exceptions/`):

| Wyjątek | Kontekst / kiedy |
|---|---|
| `EmptyOrderException` | Zamówienie bez pozycji (`Items.Count == 0`). |
| `DeliveryAddressRequiredException` | `FulfillmentType == Delivery` bez adresu dostawy. |
| `AddressOutsideDeliveryAreaException` | Adres poza promieniem dostawy (ADR-0006). |
| `BelowMinimumOrderValueException` | `Subtotal < Restaurant.MinimumOrderValue`. |
| `RestaurantClosedException` | Żądany/aktualny czas realizacji poza godzinami pracy. |
| `PastFulfillmentTimeException` | `RequestedFulfillmentTime` w przeszłości. |
| `InvalidOrderStatusTransitionException` | Niedozwolone przejście `OrderStatus`. |
| `InvalidPaymentStatusTransitionException` | Niedozwolone przejście `PaymentStatus`. |
| `PaymentRequiredBeforeAcceptanceException` | `Online` + `PaymentStatus != Paid` przy `Accept()` (ADR-0007). |
| `InvalidEstimatedReadyAtException` | `EstimatedReadyAt` w złym stanie zamówienia lub w przeszłości. |
| `PizzaWithoutIngredientException` | Pizza bez składnika bazowego. |
| `MenuItemUnavailableException` | Próba zamówienia pozycji `IsAvailable == false`. |
| `VariantSelectionRequiredException` | Brak wyboru wariantu, gdy warianty istnieją. |
| `InvalidVariantConfigurationException` | Zła konfiguracja wariantów (np. wariant nie należy do pozycji, brak/wielu domyślnych, usunięcie domyślnego bez wskazania nowego). |
| `CannotRemoveLastVariantException` | Próba usunięcia jedynego wariantu MenuItem — lista nie może zostać pusta po skonfigurowaniu wariantów (ADR-0016). |
| `ExtraNotAllowedException` | Dodatek spoza `AllowedExtras` danego MenuItem. |
| `PromotionNotApplicableException` | Promocja niekwalifikująca się / limit użyć wyczerpany / (BuyXGetY) za mało sztuk wyzwalacza lub brak nagrody w koszyku (8.2). |
| `PromotionAlreadyAppliedException` | Druga promocja na tym samym zamówieniu. |
| `InsufficientLoyaltyPointsException` | Próba wydania/odjęcia więcej punktów niż saldo. |
| `LoyaltyRedemptionNotAllowedException` | Wymiana punktów przy zamówieniu gościa (`CustomerId == null`). |
| `LoyaltyPointsAlreadyRedeemedException` | Powtórna wymiana punktów na tym samym zamówieniu. |
| `AddressNotInAddressBookException` | Odwołanie do adresu spoza książki adresowej klienta. |

Edycja `Promotion` (8.1) oraz implementacja `BuyXGetY` (8.2) **nie** wprowadzają nowych wyjątków
domenowych — guardy okna/wartości/limitu i reguły BuyXGetY reużywają `ArgumentException`/
`ArgumentOutOfRangeException` (jak `Promotion.Create`), a niekwalifikująca się BuyXGetY reużywa
`PromotionNotApplicableException`.

---

## 10. Notatki projektowe / świadome uproszczenia

- **MenuItem + Category zamiast hierarchii klas per produkt** — mniej typów, łatwiejsza
  perzystencja w EF; specyfika pizzy (składniki bazowe/dodatki, reguła min. 1 składnik)
  egzekwowana warunkowo przez `Category == Pizza`. Jeśli produkty mocno się rozjadą,
  wrócimy do hierarchii (przyszły ADR).
- **Snapshoty na Order/OrderItem** — zamówienie jest niezmiennym zapisem transakcji;
  ceny/nazwy kopiowane w chwili złożenia.
- **Koszyk (Cart)** — świadomie NIE modelujemy jako trwałego agregatu Domain na tym etapie.
  Koszyk może być stanem po stronie klienta/Application; zamówienie powstaje z „draftu".
  Jeśli pojawi się wymóg trwałego koszyka (np. porzucone koszyki), będzie osobny ADR.
- **UserAccount** poza Domain — Domain zna tylko `UserAccountId`.
- **BuyXGetY zaimplementowany** (ADR-0034) — `Promotion.CalculateDiscount(OrderDiscountContext)`
  wylicza rabat na sztuki nagrody na podstawie pozycji zamówienia; konfiguracja w owned VO
  `BuyXGetYRule` (8.2). Zakres startowy: konkretny produkt wyzwalacz/nagroda, rabat procentowy na
  nagrodę (100% = gratis), wielokrotna kwalifikacja (stacking zestawów). Poza zakresem: wyzwalacz
  „dowolna pizza/kategoria", auto-dokładanie gratisu do koszyka, edycja reguły. Domyka ADR-0011.
- **Przelicznik lojalności sfinalizowany** (ADR-0033) — 1 pkt / 1 PLN naliczania (floor),
  1 pkt = 0,05 PLN wymiany, bez górnego limitu pokrycia (limit = saldo). `ILoyaltyPolicy`/
  `LinearLoyaltyPolicy` bez zmian strukturalnych (nadal wymienialny port). Domyka ADR-0009/0014.
- **Edycja wariantów przez korzeń** — mutatory `MenuItemVariant` są `internal`, cała edycja
  (dodanie/usunięcie/domyślność/rename/cena) przechodzi przez `MenuItem`; usunięcie
  domyślnego wariantu wymaga jawnego `SetDefaultVariant`, nie auto-promocji (ADR-0016).
- **Edycja Promotion przez celowe metody** — `UpdateWindow`/`UpdateValue`/`UpdateUsageLimit`
  (8.1), `Type` i `BuyXGetYRule` niemutowalne; obniżenie `UsageLimit` poniżej `UsageCount`
  dozwolone (domyka promocję limitem), zmiana `Value` dozwolona mimo `UsageCount > 0` (snapshot
  na Order) — ADR-0019.
- **Powiązanie Customer ↔ LoyaltyAccount jednokierunkowe** — referencję trzyma tylko strona
  zależna (`LoyaltyAccount.CustomerId`, unikalny); `Customer` nie ma `LoyaltyAccountId`.
  Usuwa sztuczny cykl przy tworzeniu (fabryki generują własne Id, bez przekazywania Id z
  zewnątrz) i martwą nawigację (konto lojalnościowe pobiera się przez repozytorium) — ADR-0029.

---

## 11. Diagram relacji (skrót tekstowy)

```
Restaurant (1) --- konfiguruje ---> (globalnie) Menu, Orders

MenuItem (1) --< MenuItemVariant
MenuItem (1) --< BaseIngredients (Ingredient)
MenuItem (1) --< AllowedExtras (Ingredient)

Customer (1) --- (0..1) --- UserAccount [ref]
Customer (1) --< DeliveryAddress (książka adresowa)
LoyaltyAccount (1) --- (1) --> Customer [ref: LoyaltyAccount.CustomerId; jednokierunkowe, ADR-0029]
LoyaltyAccount (1) --< LoyaltyTransaction

Promotion (1) --- (0..1) BuyXGetYRule (VO owned, tylko Type==BuyXGetY, ADR-0034)

Order (0..1) --> Customer            (null = gość)
Order (1) --< OrderItem
OrderItem (1) --< OrderItemExtra
Order (1) --- (0..1) --> Promotion [ref]
Order (1) --- Contact: ContactDetails (VO)
Order (1) --- DeliveryAddress (VO snapshot, dla Delivery)
```
