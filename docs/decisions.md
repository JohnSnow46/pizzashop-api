# Decyzje architektoniczne (ADR-lite)

Format: każdy wpis ma **Kontekst → Decyzja → Konsekwencje**.
Wpisy są dopisywane, nie nadpisywane. Numeracja rosnąca.

---

## ADR-0001: Baza danych — PostgreSQL

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
CLAUDE.md dopuszczał PostgreSQL lub SQL Server. Potrzebna jest jedna decyzja, żeby
Infrastructure (EF Core) i migracje miały spójny provider.

**Decyzja.**
Używamy **PostgreSQL** jako bazy produkcyjnej i deweloperskiej. Provider EF Core:
`Npgsql.EntityFrameworkCore.PostgreSQL`.

**Konsekwencje.**
- Migracje generowane pod dialekt PostgreSQL (`dotnet ef migrations add ...`).
- Typy: `timestamptz` dla wszystkich znaczników czasu (patrz ADR-0010 o UTC).
- Otwiera drogę do rozszerzenia PostGIS w przyszłości, gdyby walidacja promienia dostawy
  miała trafić do bazy (na start liczymy dystans w Domain — patrz ADR-0006).
- Domain i Application pozostają niezależne od providera; zależność żyje tylko w
  Infrastructure/Api.

---

## ADR-0002: Dostawca płatności — PayU (tryb Sandbox na start)

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Potrzebny dostawca płatności online dla rynku PL. Na etapie budowy nie chcemy realnych
transakcji.

**Decyzja.**
Integrujemy **PayU** jako dostawcę płatności online, startowo w trybie **Sandbox**.
Integracja żyje w Infrastructure za interfejsem zdefiniowanym w Application
(np. `IPaymentGateway`). Domain nie wie nic o PayU.

**Konsekwencje.**
- Application definiuje abstrakcję płatności (inicjalizacja płatności, obsługa
  callbacku/notyfikacji o zmianie statusu). Podmiana dostawcy = nowa implementacja w
  Infrastructure.
- Statusy PayU mapowane na wewnętrzny `PaymentStatus` (patrz ADR-0007) — nie przeciekają
  do Domain.
- Wymagany endpoint webhooka/notyfikacji w Api do asynchronicznego potwierdzenia płatności.
- Przełączenie Sandbox → produkcja to zmiana konfiguracji (klucze POS), nie kodu.

---

## ADR-0003: Model jednej lokalizacji (single-tenant)

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Aplikacja obsługuje jedną restaurację/pizzerię. Multi-tenant (wiele niezależnych
restauracji z izolacją danych) znacząco komplikuje model i autoryzację.

**Decyzja.**
Modelujemy **jedną lokalizację**. Encja `Restaurant` istnieje jako pojedynczy rekord
(konfiguracja: dane kontaktowe, lokalizacja, promień dostawy, godziny pracy). Nie
wprowadzamy izolacji per-tenant ani `TenantId` na encjach.

**Konsekwencje.**
- Menu, zamówienia, pracownicy należą niejawnie do tej jednej restauracji — nie potrzeba
  filtrowania po tenancie.
- `Restaurant` pozostaje osobną encją (nie stałą w kodzie), bo jej atrybuty (godziny,
  promień, adres) są edytowalne przez `RestaurantAdmin` i mogą się zmieniać.
- Gdyby w przyszłości pojawiła się potrzeba wielu lokalizacji — będzie to nowy ADR i
  refaktor (dodanie `RestaurantId` na Order/MenuItem/Employee). Świadomie odkładamy.

---

## ADR-0004: Role użytkowników — Customer / Employee / RestaurantAdmin / SuperAdmin

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
CLAUDE.md wymieniał role Customer/RestaurantAdmin/SuperAdmin. Zakres biznesowy dodaje
personel obsługujący zamówienia (przyjmowanie, ustawianie `EstimatedReadyAt`, zmiana
statusów kuchnia/dostawa) — to inny poziom uprawnień niż administrator.

**Decyzja.**
Definiujemy cztery role:
- **Customer** — składa zamówienia, zarządza własnym kontem, adresami, punktami.
- **Employee** — obsługuje zamówienia (przyjęcie, statusy realizacji, `EstimatedReadyAt`),
  bez dostępu do konfiguracji restauracji i menu.
- **RestaurantAdmin** — zarządza menu, godzinami pracy, promieniem dostawy, promocjami,
  kontami pracowników; wszystko co Employee.
- **SuperAdmin** — pełny dostęp techniczny/systemowy, zarządzanie kontami adminów.

**Decyzja o mapowaniu na konta.**
Wszystkie role to jeden mechanizm tożsamości (`UserAccount` + rola w tokenie JWT).
`Customer` to **profil domenowy** powiązany z kontem o roli Customer (dane zakupowe,
adresy, punkty). Personel (Employee/RestaurantAdmin/SuperAdmin) NIE ma profilu `Customer`
— reprezentowany jest przez `Employee`/konto z rolą. Szczegóły rozdziału tożsamość vs.
profil domenowy: ADR-0005.

**Konsekwencje.**
- Autoryzacja w Api oparta na rolach z JWT (policy per rola).
- Role są hierarchiczne uprawnieniami (SuperAdmin ⊇ RestaurantAdmin ⊇ Employee), ale
  modelujemy je jako rozłączne wartości roli konta; hierarchię egzekwują policy, nie Domain.
- Model tożsamości (ASP.NET Identity vs. własny) doprecyzuje osobny ADR przy implementacji
  autentykacji — teraz zakładamy tylko istnienie roli na koncie.

---

## ADR-0005: Tożsamość (konto) vs. profil domenowy; zamówienie gościa

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Trzeba rozstrzygnąć, czy `Customer` to zawsze konto oraz jak obsłużyć zamówienie gościa
(bez rejestracji). Mieszanie tożsamości (login, hasło, rola) z danymi zakupowymi
(adresy, punkty) zabetonowałoby Domain w zależność od mechanizmu logowania.

**Decyzja.**
- **Rozdzielamy tożsamość od profilu domenowego.** Uwierzytelnianie (login/hasło/rola)
  żyje po stronie Application/Infrastructure jako `UserAccount` (konto). W Domain
  `Customer` to profil zakupowy, opcjonalnie powiązany z kontem przez `UserAccountId`.
- **Zamówienie gościa NIE tworzy encji `Customer`.** Dane kontaktowe gościa (imię,
  telefon, e-mail) przechowujemy bezpośrednio na `Order` jako value object
  `ContactDetails`. Zamówienie ma opcjonalne `CustomerId` (null = gość).
- Punkty lojalnościowe i historia zamówień przysługują wyłącznie zarejestrowanym
  klientom (bo wymagają trwałej tożsamości).

**Konsekwencje.**
- `Order.CustomerId` jest nullable; `Order.Contact` (ContactDetails) jest zawsze wymagany
  i jest źródłem prawdy dla danych kontaktowych do tego zamówienia (nawet dla zalogowanego
  klienta — snapshot w chwili zamówienia).
- Gość nie zbiera punktów; ewentualne przypisanie historycznego zamówienia do konta po
  rejestracji to przyszła funkcja (poza zakresem teraz).
- Domain nie zależy od mechanizmu auth — testowalny bez Identity.

---

## ADR-0006: Obszar dostawy jako promień od restauracji

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Trzeba określić, czy dany adres kwalifikuje się do dostawy. Modelowanie stref
wielokątami (geofencing) jest elastyczne, ale kosztowne na start.

**Decyzja.**
Obszar dostawy to **okrąg**: środek = lokalizacja restauracji (`GeoCoordinate`), promień
= `Restaurant.DeliveryRadius` (w kilometrach). Adres kwalifikuje się do dostawy, gdy
dystans geodezyjny (Haversine) między jego `GeoCoordinate` a lokalizacją restauracji
`<= DeliveryRadius`. Reguła i obliczenie dystansu żyją w Domain (metoda na value objekcie
`GeoCoordinate` / domenowa usługa dostawy).

**Konsekwencje.**
- Adres dostawy musi mieć współrzędne (geokodowanie po stronie Application/Infrastructure
  za interfejsem `IGeocodingService`; Domain dostaje już gotowe współrzędne).
- Prosta, deterministyczna, testowalna reguła bez zależności bazodanowych.
- Ograniczenie: nie odwzorowuje realnej geografii (rzeki, granice dzielnic). Zmiana na
  wielokąty/PostGIS to przyszły ADR, jeśli zajdzie potrzeba.

---

## ADR-0007: Płatność i realizacja jako niezależne stany

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Zamówienie może być płatne online (przedpłata) albo przy odbiorze/dostawie (gotówka/
terminal). Status płatności nie może blokować toku realizacji w sposób sztywny —
np. zamówienie za pobraniem jest realizowane mimo `PaymentStatus = Pending`.

**Decyzja.**
`Order` ma **dwa niezależne cykle stanów**:
- `OrderStatus` (realizacja): `PendingAcceptance → Accepted → InPreparation → Ready →
  (OutForDelivery →) Completed`, plus `Rejected`/`Cancelled`.
- `PaymentStatus` (płatność): `Pending → Authorized/Paid → (Refunded)` oraz `Failed`.
Dodatkowo `PaymentMethod`: `Online` (PayU) lub `OnPickup` (przy odbiorze/dostawie —
gotówka/terminal).

Reguły wiążące oba tory są jawne i minimalne:
- `Online` + `PaymentStatus != Paid` ⇒ zamówienie nie przechodzi do realizacji
  (`Accepted`) dopóki płatność nie potwierdzona. (Przedpłata warunkuje przyjęcie.)
- `OnPickup` ⇒ realizacja przebiega niezależnie; `PaymentStatus` przechodzi w `Paid`
  w momencie odbioru (obsługa/kurier oznacza wpłatę). Brak wpłaty nie blokuje kuchni.

**Konsekwencje.**
- Dwa osobne enumy w Domain, dwa osobne zestawy dozwolonych przejść (guard clauses).
- Anulowanie opłaconego zamówienia online ⇒ ścieżka `Refunded` (zwrot przez PayU) —
  logika zwrotu w Infrastructure, stan w Domain.
- UI/obsługa widzi oba stany rozdzielnie; raportowanie po każdym z torów.

---

## ADR-0008: Zamówienia z wyprzedzeniem (scheduling) i EstimatedReadyAt

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Klient może chcieć zamówić „na teraz" albo zaplanować na konkretną godzinę. Czas
gotowości zależy od obłożenia kuchni i jest znany dopiero po przyjęciu zamówienia przez
obsługę.

**Decyzja.**
- `Order.RequestedFulfillmentTime` (nullable `DateTimeOffset`): null = „jak najszybciej
  (ASAP)"; wartość = żądany czas odbioru/dostawy. Musi mieścić się w godzinach pracy
  restauracji i nie być w przeszłości.
- `Order.EstimatedReadyAt` (nullable `DateTimeOffset`): ustawiany przez `Employee`/
  `RestaurantAdmin` **w momencie przyjęcia zamówienia** (`Accepted`) lub później
  aktualizowany. Domyślnie null przy `PendingAcceptance`.

**Konsekwencje.**
- Walidacja `RequestedFulfillmentTime` względem `Restaurant.OpeningHours` w Domain.
- Przejście do `Accepted` może (ale nie musi na poziomie modelu) wiązać się z ustawieniem
  `EstimatedReadyAt` — reguła „przyjęcie wymaga podania szacowanego czasu" do potwierdzenia
  z biznesem; na start `EstimatedReadyAt` opcjonalny, ustawiany akcją obsługi.
- SignalR live-tracking prezentuje `EstimatedReadyAt` klientowi.

---

## ADR-0009: Punkty lojalnościowe jako elastyczny szkielet

**Data:** 2026-07-16
**Status:** Zaakceptowana (celowo niedookreślona co do przelicznika) — **domknięta przez ADR-0033** (przelicznik sfinalizowany 2026-07-22; szkielet pozostaje w mocy)

**Kontekst.**
Program lojalnościowy ma istnieć, ale reguły naliczania/wymiany (ile punktów za złotówkę,
próg wymiany, wygasanie) nie są jeszcze ustalone. Nie chcemy zabetonować przelicznika w
modelu.

**Decyzja.**
Modelujemy **szkielet**, nie politykę:
- `LoyaltyAccount` (1:1 z `Customer`) — przechowuje aktualne saldo (`PointsBalance`) jako
  wielkość wyliczalną/utrzymywaną z historii.
- `LoyaltyTransaction` — append-only historia: `Type` (`Earned`/`Redeemed`/`Adjusted`/
  `Expired`), `Points` (dodatnie/ujemne), `Reason`, opcjonalne `OrderId`, `OccurredAt`.
- Sam **przelicznik** (ile punktów, za co) NIE jest częścią encji — trafi za abstrakcję
  polityki (`ILoyaltyPolicy` w Application) doprecyzowaną osobnym ADR, gdy biznes ustali
  zasady.

**Konsekwencje.**
- Saldo jest zawsze sumą transakcji (źródło prawdy = historia), co daje audytowalność.
- Zmiana zasad naliczania = nowa implementacja polityki, bez migracji modelu punktów.
- Gość nie ma `LoyaltyAccount` (ADR-0005).
- Wygasanie punktów (`Expired`) jest przewidziane w typie transakcji, ale mechanizm
  (job/termin) odkładamy.

---

## ADR-0010: Czas w UTC (DateTimeOffset), znaczniki timestamptz

**Data:** 2026-07-16
**Status:** Zaakceptowana

**Kontekst.**
Zamówienia planowane, godziny pracy, `EstimatedReadyAt` wymagają jednoznacznej strefy
czasowej. Rozbieżności stref to typowe źródło błędów.

**Decyzja.**
Wszystkie znaczniki czasu w Domain to `DateTimeOffset` przechowywane w UTC
(PostgreSQL `timestamptz`). Godziny pracy restauracji modelujemy jako czas lokalny
restauracji + jej strefę (`Restaurant.TimeZoneId`), bo dotyczą kalendarza lokalnego.

**Konsekwencje.**
- Konwersje strefowe (lokalna restauracji ↔ UTC) w warstwie prezentacji/Application, nie
  w Domain (Domain operuje na UTC + jawnej strefie restauracji do reguł godzinowych).
- Spójne porównania czasów planowanych i szacowanych.

---

## ADR-0011: BuyXGetY — wyliczenie rabatu odłożone

**Data:** 2026-07-17
**Status:** Zaakceptowana (świadome odłożenie) — **domknięta przez ADR-0034** (BuyXGetY zaimplementowany 2026-07-22)

**Kontekst.**
`Promotion` jest agregatem definicji promocji. `Promotion.CalculateDiscount` dla typów
`Percentage`/`FixedAmount`/`FreeDelivery` wylicza rabat z samego subtotalu i opłaty za
dostawę. Typ `BuyXGetY` („kup X, dostań Y gratis/taniej") wymaga natomiast wiedzy o
konkretnych pozycjach zamówienia — które produkty, w jakich ilościach, po jakiej cenie —
żeby rozstrzygnąć, który egzemplarz jest gratisowy. Agregat `Promotion` tej informacji nie
ma i nie powinien jej trzymać (to należy do `Order`/`OrderItem`).

**Decyzja.**
`Promotion.CalculateDiscount` dla `Type == BuyXGetY` rzuca `NotSupportedException` (nie
`DomainException` — to nie błąd biznesowy użytkownika, lecz jawnie niezaimplementowana
gałąź). Pełna obsługa `BuyXGetY` wymaga przeprojektowania API promocji tak, by przyjmowało
kontekst zamówienia (lista pozycji, ceny) zamiast samego subtotalu — i jest odłożona do
momentu, gdy powstanie faktyczny use case promocji „kup X dostań Y". Wtedy osobny ADR
doprecyzuje kształt tego kontekstu (np. `CalculateDiscount(OrderDiscountContext ctx)`).

**Konsekwencje.**
- Warstwa Application NIE może kwalifikować/stosować promocji `BuyXGetY` na start — walidacja
  Application powinna odrzucić utworzenie/aktywację promocji tego typu, albo świadomie nie
  udostępniać go w API zarządzania promocjami, dopóki nie powstanie implementacja.
- Pozostałe typy działają w pełni.
- Zmiana sygnatury `CalculateDiscount` w przyszłości jest przewidziana i nie jest traktowana
  jako złamanie kontraktu (API promocji jest wewnętrzne, nie publiczne).

---

## ADR-0012: Struktura warstwy Application — CQRS z cienką własną abstrakcją (bez MediatR)

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
CLAUDE.md wymaga CQRS w Application (`Commands/`, `Queries/`, każdy handler w osobnym
pliku) oraz FluentValidation dla walidacji kształtu requestów. Potrzebny jest mechanizm
dyspozytora komend/zapytań i pipeline walidacji. Dominującym gotowym rozwiązaniem jest
MediatR, ale od 2025 przeszedł on na model komercyjny (płatny do zastosowań produkcyjnych).

**Decyzja.**
Wprowadzamy **własne, cienkie abstrakcje** zamiast MediatR:
- `ICommand<TResult>` / `ICommand` (marker), `IQuery<TResult>` (marker).
- `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>` — jeden handler
  = jeden plik, jak wymaga CLAUDE.md.
- Dyspozytor `ISender`/`IDispatcher` (implementacja w Infrastructure lub cienkim
  `Application.Common`), rozwiązujący handler z DI i uruchamiający pipeline.
- **Walidacja**: `ValidationBehavior` uruchamia wszystkie zarejestrowane
  `IValidator<TRequest>` (FluentValidation) przed handlerem; niepowodzenie ⇒
  `ValidationException` (Application) mapowany na 400 w middleware Api. Walidatory sprawdzają
  wyłącznie **kształt danych** (wymagane pola, formaty, zakresy) — reguły biznesowe zależne
  od stanu żyją w Domain (CLAUDE.md).

**Konsekwencje.**
- Brak zależności od komercyjnej biblioteki; pełna kontrola nad pipeline.
- Nieco więcej boilerplate niż MediatR (rejestracja handlerów, dyspozytor) — akceptowalny
  koszt przy tej skali.
- **Alternatywa:** MediatR (mniej kodu, dojrzały pipeline behaviors) — odrzucona z powodu
  licencji; albo starsza darmowa wersja MediatR — odrzucona (brak wsparcia, dług).
- Handlery pozostają czystymi klasami zależnymi tylko od interfejsów (repozytoria, serwisy)
  — testowalne przez Moq bez hostingu.

---

## ADR-0013: Kształt IPaymentGateway i przepływ potwierdzenia płatności PayU

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
ADR-0002 ustalił PayU za `IPaymentGateway` (interfejs w Application, implementacja w
Infrastructure). Trzeba określić kształt tej abstrakcji oraz jak potwierdzenie płatności
z asynchronicznej notyfikacji PayU trafia do `Order.PaymentStatus`, nie przeciekając
detali PayU do Domain/Application.

**Decyzja.**
- `IPaymentGateway` (Application) eksponuje operacje niezależne od dostawcy:
  - `InitializePaymentAsync(PaymentInitRequest, CancellationToken) → PaymentInitResult`
    (zwraca `RedirectUrl` do bramki + `ProviderOrderId`/referencję).
  - `VerifyAndParseNotification(rawBody, headers) → PaymentNotification` (weryfikacja
    podpisu + mapowanie surowego statusu PayU na wewnętrzny `PaymentStatus`).
  - opcjonalnie `RefundAsync(...)` dla ścieżki `Refunded` (ADR-0007).
- **Weryfikacja podpisu** notyfikacji dzieje się w implementacji Infrastructure (zna sekret
  PayU), NIE w kontrolerze — kontroler webhooka w Api tylko przekazuje surowe body/nagłówki.
  Endpoint webhooka jest **bez JWT** (CLAUDE.md), bezpieczeństwo = weryfikacja podpisu.
- Przepływ: kontroler webhooka (anonimowy) → `ConfirmPaymentFromNotificationCommand`
  (Application) → gateway weryfikuje+parsuje → handler ładuje `Order` po referencji →
  wywołuje `Order.ConfirmPayment()` / `FailPayment()` / `AuthorizePayment()` w zależności od
  zmapowanego statusu → zapis. Idempotencja: powtórna notyfikacja o już `Paid` nie jest
  błędem (handler sprawdza aktualny stan i nie robi nielegalnego przejścia).

**Konsekwencje.**
- Domain nie zna PayU; mapowanie statusów żyje w Infrastructure, kontrakt w Application.
- Podmiana dostawcy = nowa implementacja `IPaymentGateway` + mapowania, bez zmian w Domain
  i handlerach.
- Handler potwierdzenia musi być odporny na duplikaty i spóźnione notyfikacje (idempotencja
  po stanie `Order`).

---

## ADR-0014: ILoyaltyPolicy — polityka naliczania/wymiany punktów w Application

**Data:** 2026-07-17
**Status:** Zaakceptowana (implementacja reguł świadomie odłożona — ADR-0009) — **domknięta przez ADR-0033** (reguła sfinalizowana 2026-07-22; port pozostaje bez zmian strukturalnych)

**Kontekst.**
ADR-0009 odłożył przelicznik punktów za abstrakcję `ILoyaltyPolicy` w Application. Encje
(`LoyaltyAccount`, `Order`) rejestrują tylko skutki (ile naliczyć/wydać). Trzeba określić
kształt interfejsu, żeby handlery zamówień mogły go używać, nie znając konkretnej reguły.

**Decyzja.**
`ILoyaltyPolicy` (Application) definiuje:
- `int CalculatePointsToEarn(Order order)` — ile punktów naliczyć po `Completed` (wołane przy
  domknięciu zamówienia; wynik trafia do `Order.SetPointsToEarn`).
- `Money CalculateRedemptionValue(int points)` — wartość rabatu za wskazaną liczbę punktów
  (wołane przy wymianie; wynik trafia do `Order.RedeemLoyaltyPoints` + `LoyaltyAccount.Redeem`).
- opcjonalnie `int MaxRedeemablePoints(Order order, int balance)` — górny limit wymiany na
  zamówienie (np. „punktami pokrywasz max 50% wartości").
- Na start rejestrujemy implementację **tymczasową/no-op lub prostą liniową** (np. 1 pkt = 1 zł
  wydany, 1 pkt = 0,05 zł rabatu) oznaczoną jako placeholder — konkretną regułę doprecyzuje
  osobny ADR, gdy biznes ustali zasady. Handlery są już napisane pod interfejs.

**Konsekwencje.**
- Zmiana reguł = nowa implementacja `ILoyaltyPolicy`, bez zmian w handlerach ani Domain.
- Domain pozostaje wolny od przelicznika (ADR-0009).
- Testy handlerów mockują `ILoyaltyPolicy` — deterministyczne, niezależne od realnej reguły.

---

## ADR-0015: Dostęp do konfiguracji Restaurant przez repozytorium (pojedynczy rekord)

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
`Order.Create` i walidacja godzin/obszaru dostawy potrzebują aktualnej konfiguracji
`Restaurant` (godziny, promień, progi, opłata). W modelu single-tenant (ADR-0003) istnieje
dokładnie jeden rekord, ale jego atrybuty są edytowalne przez `RestaurantAdmin`, więc nie
może być stałą w kodzie.

**Decyzja.**
Handlery pobierają konfigurację przez `IRestaurantRepository.GetAsync(CancellationToken)`
zwracające jedyny rekord `Restaurant` (nie po `Id` — jest jeden). Zapis zmian konfiguracji
(godziny, promień, progi, wyłącznik przyjmowania) przez ten sam interfejs
(`UpdateAsync`). Handlery zarządzania konfiguracją restauracji dostępne tylko dla
`RestaurantAdmin`/`SuperAdmin` (egzekwuje autoryzacja Api, ADR-0004).

**Konsekwencje.**
- Handlery nie zakładają cache'owanego singletona — zawsze aktualny stan z repozytorium;
  ewentualne cache'owanie to decyzja Infrastructure (poza tym ADR).
- Brak `RestaurantId` na innych encjach (spójne z ADR-0003).
- Gdyby doszła multi-lokalizacja — `GetAsync()` zmieni się na `GetById`, co jest sygnałem
  refaktoru przewidzianym w ADR-0003.

---

## ADR-0016: Edycja wariantów MenuItem — jawny SetDefaultVariant zamiast auto-promocji

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
Iteracja 1 warstwy Application (katalog) ujawniła lukę w agregacie `MenuItem`: był tylko
`AddVariant`, brakowało zmiany domyślności na istniejącej liście, usuwania wariantu oraz
edycji nazwy/ceny wariantu; `Description`/`ImageUrl` nie miały metody aktualizacji.
W efekcie `UpdateMenuItemCommandHandler.ReconcileVariants` **po cichu ignorował** próbę
zmiany `IsDefault` na istniejącym wariancie (żądanie API nic nie robiło, bez błędu), a
handler świadomie pomijał opis/zdjęcie. Przy projektowaniu `RemoveVariant` pojawia się
pytanie, co zrobić przy usuwaniu wariantu domyślnego, gdy istnieją inne: automatycznie
promować któryś pozostały czy wymusić jawny wybór.

**Decyzja.**
- Domyślność zmienia się wyłącznie jawną metodą `SetDefaultVariant(Guid variantId)` na
  korzeniu agregatu (koniec cichego ignorowania; guard, gdy wariant nie istnieje).
- `RemoveVariant(Guid variantId)` **odmawia** usunięcia wariantu domyślnego, gdy istnieją
  inne warianty, rzucając `InvalidVariantConfigurationException` — admin musi najpierw
  wskazać nowy domyślny (`SetDefaultVariant`), a dopiero potem usunąć stary. **Nie**
  auto-promujemy pozostałego wariantu.
- `RemoveVariant` odmawia też usunięcia **jedynego** wariantu, rzucając nowy
  `CannotRemoveLastVariantException`.
- Cała edycja wariantów (dodanie, usunięcie, zmiana domyślnego, `RenameVariant`,
  `UpdateVariantPrice`) przechodzi przez korzeń `MenuItem`; mutatory `MenuItemVariant` są
  `internal`.
- `Description`/`ImageUrl` aktualizuje jedna metoda `UpdateDetails(string?, string?)`
  (semantyka PUT — pełne podstawienie, `null` = wyczyść), nie dwa osobne settery — oba pola
  są opcjonalne i bez niezmienników.

**Alternatywa (odrzucona): auto-promocja.**
`RemoveVariant` usuwający domyślny automatycznie promuje pierwszy pozostały wariant na
domyślny. Mniej kroków dla admina, ale wybór „pierwszy pozostały" zależy od kolejności
listy (niedeterministyczny biznesowo) i podejmuje za admina decyzję cenową widoczną dla
klienta (domyślny wariant = domyślna cena pozycji). Odrzucona z tego samego powodu, co
wcześniejsze ciche ignorowanie zmiany `IsDefault`: model nie powinien po cichu podejmować
decyzji biznesowych. Jawny `SetDefaultVariant` trzyma admina przy sterach kosztem jednego
dodatkowego kroku.

**Konsekwencje.**
- Nowy wyjątek `CannotRemoveLastVariantException` (mapowany jak pozostałe `DomainException`);
  dopisany do domain-model.md sekcja 9.
- `UpdateMenuItemCommandHandler.ReconcileVariants` musi orkiestrować kolejność: najpierw
  `SetDefaultVariant` na nowy domyślny, potem `RemoveVariant` starego — inaczej dostanie
  `InvalidVariantConfigurationException`.
- MenuItem raz skonfigurowany z wariantami nie wraca do trybu bez wariantów przez usuwanie
  (ostatni wariant nieusuwalny); konwersja na pozycję bez wariantów = celowe odtworzenie
  pozycji. Świadome uproszczenie — pozycja bez wariantów istnieje tylko, jeśli tak powstała.
- Szczegóły sygnatur i reguł: domain-model.md sekcja 4.2 (mutatory wariantu) i 4.4
  (metody korzenia). To źródło prawdy dla buildera; kod jeszcze nie istnieje.

---

## ADR-0017: Reguły zależne od roli/kontekstu w Application — `ForbiddenOperationException` (403), nie reużycie `ValidationException` ani przeciek roli do Domain

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
`CancelOrderCommandHandler` musi wyrazić regułę: **klient (nie personel) może anulować
zamówienie tylko dopóki status to `PendingAcceptance`**; po przyjęciu anuluje już tylko
obsługa. To reguła zależna od stanu *oraz* od roli wykonawcy. Świadomie NIE trafiła do
`Order.Cancel()`, bo Domain celowo nie zna ról (ADR-0004/0005) — `Order.Cancel()` egzekwuje
tylko regułę uniwersalną (nie można anulować ze stanu terminalnego: `Completed`/`Rejected`/
`Cancelled`), niezależną od tego, kto anuluje.

Builder zasygnalizował złamanie tej reguły przez `ValidationException` — typ zarezerwowany
(CLAUDE.md, application-layer.md sekcja 5) dla błędów **kształtu danych** z FluentValidation,
mapowany na **HTTP 400**. „Nie możesz anulować po akceptacji, bo jesteś klientem" nie jest
błędem kształtu requestu (OrderId jest poprawny) — to odmowa autoryzacyjna zależna od stanu.
Reużycie `ValidationException` jest semantycznie mylące (zły komunikat, zły kod HTTP) i
zaciera podział z sekcji 5.

**Rozważane opcje.**
- **(a)** Nowy, odrębny wyjątek Application-level dla odmów autoryzacyjnych zależnych od
  roli/stanu wykrywanych w Application (bo zależą od kontekstu, którego Domain nie zna).
- **(b)** Przenieść regułę do Domain przez `Order.Cancel(bool isPrivilegedCancellation)`
  rzucające istniejący `InvalidOrderStatusTransitionException`, gdy
  `!isPrivilegedCancellation && Status != PendingAcceptance`; handler przekazuje
  `isPrivilegedCancellation = isStaff`.

**Decyzja: (a).** Wprowadzamy nowy wyjątek warstwy Application:

```
namespace PizzaShop.Application.Common.Exceptions;

/// Rzucany, gdy operacja jest sama w sobie dozwolona, ale bieżący wykonawca nie ma
/// uprawnień, by ją wykonać w tym stanie/kontekście — decyzja zależy od roli/kontekstu,
/// którego Domain celowo nie zna (ADR-0004/0005). Mapowany na HTTP 403 w middleware Api.
public sealed class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException(string message) : base(message) { }
}
```

- **Gdzie żyje:** `src/PizzaShop.Application/Common/Exceptions/ForbiddenOperationException.cs`
  (obok `ValidationException`, `NotFoundException`), NIE w Domain.
- **Mapowanie HTTP (intencja dla przyszłego middleware Api):** `ForbiddenOperationException`
  → **403 Forbidden**. Komunikat wyjątku bezpieczny do zwrócenia klientowi (nie ujawnia
  cudzych danych).
- **Użycie w `CancelOrderCommandHandler`:** `EnsureCustomerCanStillCancel` rzuca
  `ForbiddenOperationException` z komunikatem
  „Customers can only cancel an order before it has been accepted by the restaurant."
  zamiast `ValidationException`. `Order.Cancel()` pozostaje bez zmian (dalej tylko reguła
  terminalna). Rozróżnienie „nie znaleziono / nie twoje" (404) pozostaje na
  `NotFoundException` (bez zmian) — ukrywanie cudzych zamówień to nie 403, tylko 404.

**Dlaczego 403, a nie 409.**
409 Conflict jest zarezerwowany dla realnych konfliktów **stanu domenowego**, które są
nielegalne *niezależnie od wykonawcy* — te egzekwuje Domain przez `DomainException`
(np. `InvalidOrderStatusTransitionException`, gdy nawet obsługa nie może anulować
`Completed`). Tu przejście `Accepted → Cancelled` jest legalne (obsługa robi je normalnie);
klienta blokuje wyłącznie brak uprawnień w tym stanie — to semantyka autoryzacyjna → 403.
Trzymanie 409 = konflikt Domain, a 403 = odmowa Application zależna od roli, daje rozłączne,
czytelne mapowanie.

**Dlaczego odrzucono (b).**
- Semantyka wyjątku: `InvalidOrderStatusTransitionException` znaczy „to przejście jest
  nielegalne w grafie statusów". Ale `Accepted → Cancelled` legalne *jest* (dla obsługi).
  Reużycie tego typu dla „nielegalne tylko dla tego wykonawcy" zaciemnia jego znaczenie i
  jego mapowanie na 409 — dokładnie ten sam rodzaj pomyłki semantycznej, co reużycie
  `ValidationException`.
- Domain: `bool isPrivilegedCancellation` to wciąż flaga *wyprowadzona z roli* — Domain
  zaczyna rozgałęziać zachowanie zależnie od uprawnień wykonawcy, co ADR-0004/0005 celowo
  trzymają poza Domain. Reguły Domain mają obowiązywać niezależnie od tego, kto woła.
  Czystszy Domain to „Cancel legalny, chyba że stan terminalny", kropka.
- HTTP: (b) wymusiłoby 409 dla przypadku, który jest 403.

**Dlaczego to zostaje w Application, a nie w policy autoryzacji Api.**
Reguła jest **zależna od stanu** (`Order.Status`) — wymaga załadowanego agregatu, którego
policy na endpoincie nie ma. Handler jest właściwym miejscem: zna i rolę (`ICurrentUser`),
i stan (załadowany `Order`).

**Wzorzec globalny dla Application (oś decyzyjna dla buildera).**
Ustalamy jednoznaczne odwzorowanie rodzaju błędu na typ i kod — do stosowania też w
Iteracji 3 (płatności/webhook PayU) i dalej:
- **Kształt danych** (wymagane pola, formaty, zakresy) → `ValidationException` → **400**.
- **Zasób nie istnieje / niedostępny dla wykonawcy** (celowo nieodróżnialny od „nie
  istnieje", by nie potwierdzać cudzych zasobów) → `NotFoundException` → **404**.
- **Operacja legalna, ale wykonawca nie ma uprawnień w tym stanie/kontekście**
  (decyzja zależna od roli/kontekstu, którego Domain nie zna) → `ForbiddenOperationException`
  → **403**.
- **Konflikt stanu domenowego, nielegalny niezależnie od wykonawcy** → `DomainException`
  i podtypy (w Domain) → **409/422** wg typu.
- **Webhook PayU / brak zaufanego uwierzytelnienia** (nieważny podpis notyfikacji — żądanie
  nieuwierzytelnione/sfałszowane, brak zalogowanego wykonawcy) → traktowane osobno: zwrot
  **400** (payload niezaufany/malformed, bez ujawniania szczegółów), ewentualnie **401**
  jeśli implementacja zdecyduje sygnalizować „nieuwierzytelnione". To **nie** jest
  `ForbiddenOperationException` (brak pojęcia wykonawcy-roli) ani `ValidationException`
  (to nie walidacja kształtu DTO) — dokładny typ/kształt doprecyzuje ADR Iteracji 3 przy
  `ConfirmPaymentFromNotificationCommand`; poprawna notyfikacja o już ustawionym stanie
  zwraca **200** (idempotencja, ADR-0013).

**Konsekwencje.**
- Nowy plik `Common/Exceptions/ForbiddenOperationException.cs` (Application). Bez wspólnej
  klasy bazowej dla wyjątków Application — middleware mapuje po typie konkretnym, jak dla
  `ValidationException`/`NotFoundException` (świadomie bez refaktoru istniejących typów;
  YAGNI).
- `CancelOrderCommandHandler.EnsureCustomerCanStillCancel` zmienia rzucany typ na
  `ForbiddenOperationException`; usuwa użycie `ValidationException`/`ValidationError` w tej
  ścieżce. `Order.Cancel()` bez zmian (Domain pozostaje bez wiedzy o rolach).
- Test handlera aktualizuje asercję na `ForbiddenOperationException` (scenariusz: klient
  próbuje anulować po `Accepted`).
- Przyszłe middleware Api (jeszcze nie istnieje) mapuje `ForbiddenOperationException` → 403;
  intencja zapisana w application-layer.md sekcja 5.
- Wzorzec jest wiążący dla kolejnych handlerów — analogiczne odmowy zależne od roli/stanu
  używają `ForbiddenOperationException`, nie `ValidationException`.

---

## ADR-0018: Domknięcie płatności — refund przy anulowaniu, persystencja `ProviderPaymentReference`, klasyfikacja wyjątku „konflikt stanu wykryty w Application" i zakres płatności gościa

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
Reviewer wstrzymał Iterację 3 (płatności PayU) z jednym problemem blokującym i dwoma
niespójnościami:
1. `IPaymentGateway.InitializePaymentAsync` zwraca `PaymentInitResult.ProviderPaymentReference`
   (referencja PayU potrzebna do `RefundAsync`), ale nikt jej nie zapisuje. W efekcie
   `CancelOrderCommandHandler` anuluje zamówienie, lecz dla `Online` + `Paid` NIE woła
   `RefundAsync` i NIE zmienia `PaymentStatus` na `Refunded` — klient traci pieniądze.
   Sekcja 4.3 wprost wymagała ścieżki refundu.
2. `InitializePaymentCommandHandler.EnsureCanInitializePayment` rzuca
   `ForbiddenOperationException` (403) dla sytuacji nielegalnej dla **każdego** wykonawcy
   (zamówienie `OnPickup` albo już `Paid`) — profil bliższy 409, ale nie ma metody Domain
   do wywołania (inicjalizacja sesji bramki nie zmienia stanu `Order`). ADR-0017 nie
   przewidział „stanu uniwersalnie nielegalnego wykrywanego w Application, bez udziału Domain".
3. Sekcja 4.4 wymieniała rolę „anonim/Customer" dla `InitializePaymentCommand`, a
   implementacja obsługuje tylko zalogowanego właściciela/personel (gość ma tylko
   `GuestTrackingToken`, nie `CustomerId`).

**Decyzja 1 — gdzie żyje `ProviderPaymentReference` i jak działa refund.**

*Miejsce referencji: POZA Domain, jako sidecar persystencji korelowany z `Order`* —
analogicznie do mechanizmu `GuestTrackingToken` z Iteracji 2
(`IOrderRepository.AddAsync(Order, Guid? guestTrackingToken, …)` + `GetByGuestTrackingTokenAsync`).
Rozstrzygnięcie dylematu z ADR-0002 („Domain nie wie nic o PayU"): sama referencja jako
string mogłaby uchodzić za neutralny identyfikator (jak `GuestTrackingToken`, który też jest
tylko `Guid`), ale ustanowiony precedens jest jasny — wartości korelujące ze światem
zewnętrznym przechodzą przez repozytorium, nie przez agregat. Co więcej, referencja jest
*specyficzna dla dostawcy* (jej istnienie ma sens wyłącznie dlatego, że istnieje zewnętrzna
bramka), więc argument za trzymaniem jej poza Domain jest *silniejszy* niż dla tokenu.
`Order` pozostaje bez zmian.

*Kontrakt `IOrderRepository` (rozszerzenie):*
- `AddAsync` zyskuje parametr: `AddAsync(Order order, Guid? guestTrackingToken, string? providerPaymentReference, CancellationToken ct)`. Referencja jest znana już w chwili tworzenia (krok 8 `CreateOrderCommand` woła `InitializePaymentAsync` **przed** `AddAsync`), więc zapisuje się razem z zamówieniem w jednej transakcji; `null` dla `OnPickup`.
- nowa `Task SetProviderPaymentReferenceAsync(Guid orderId, string providerPaymentReference, CancellationToken ct)` — ścieżka **ponowienia** płatności (`InitializePaymentCommand`, zamówienie już istnieje). Nie commituje sama — commit robi `IUnitOfWork` (spójne z wzorcem `UpdateAsync` + `SaveChangesAsync`).
- nowa `Task<string?> GetProviderPaymentReferenceAsync(Guid orderId, CancellationToken ct)` — odczyt do refundu przy anulowaniu.

*Idempotencja `RefundAsync` (wymóg dla implementacji Infrastructure):* powtórny refund już
zrefundowanego zamówienia MUSI być traktowany jako sukces (nie błąd) — analogicznie do
idempotencji `ConfirmPaymentFromNotificationCommand` (ADR-0013). To domyka ryzyko podwójnego
zwrotu przy ponowieniu operacji, bez wprowadzania nowego stanu domenowego.

*Orkiestracja `CancelOrderCommandHandler`:*
Warunek refundu: `mustRefund = order.PaymentMethod == Online && order.PaymentStatus == Paid`.
Kolejność:
1. Load + `EnsureAccessAllowed` + `EnsureCustomerCanStillCancel` (bez zmian, ADR-0017).
2. Jeśli `mustRefund`: `reference = GetProviderPaymentReferenceAsync(order.Id)`; jeśli `null`
   → `InvalidOperationException` (naruszenie niezmiennika danych: opłacone online bez
   referencji — nie powinno wystąpić; mapowane na 500, wymaga interwencji). NIE cichy no-op.
3. `order.Cancel()` (Domain: reguła terminalna).
4. Jeśli `mustRefund`: `await RefundAsync(new PaymentRefundRequest(order.Id, reference, order.Total))`,
   następnie `order.RefundPayment()` (`Paid → Refunded`).
5. `UpdateAsync(order)` + `SaveChangesAsync` — atomowo `Cancelled` + `Refunded`.
6. `OrderStatusChangedAsync` (SignalR).

*Awaria `RefundAsync` (błąd sieci/bramki):* wyjątek propaguje, krok 5 się nie wykonuje →
**nic nie jest zapisane**, zamówienie zostaje w poprzednim stanie (nie `Cancelled`), operacja
do ponowienia przez wykonawcę. Świadomie **nie** wprowadzamy stanu „RefundPending" — ADR-0007
(zestaw `PaymentStatus`) pozostaje nienaruszony (YAGNI). Residualne ryzyko (refund udany, ale
`SaveChanges` pada) pokrywa wymagana wyżej idempotencja `RefundAsync`. Escalation
(asynchroniczny refund przez outbox + `PaymentStatus.RefundPending` + ręczna kolejka) to
przyszły ADR — tylko jeśli dane operacyjne pokażą realny problem z nieudanymi zwrotami.

*Przypadek `Authorized` (nie `Paid`) przy anulowaniu online:* poza zakresem tej iteracji —
środki nie są przechwycone (capture), więc nie refundujemy; ewentualny „void authorization"
to odrębny follow-up, nie blokuje bieżącej ścieżki.

*Skutki dla pozostałych handlerów:*
- `CreateOrderCommandHandler`: `InitializeOnlinePaymentAsync` zwraca pełny `PaymentInitResult`
  (redirect + referencja); referencja przekazana do `AddAsync`.
- `InitializePaymentCommandHandler`: po `InitializePaymentAsync` woła
  `SetProviderPaymentReferenceAsync` + `IUnitOfWork.SaveChangesAsync` (dodaje zależność
  `IUnitOfWork`).
- `CancelOrderCommandHandler`: dodaje zależność `IPaymentGateway`.

**Decyzja 2 — nowy typ `ConflictException` (409) dla konfliktu stanu wykrytego w Application.**
Wprowadzamy w Application nowy wyjątek `ConflictException` mapowany na **HTTP 409**, dla
przypadków: **konflikt stanu zasobu nielegalny dla każdego wykonawcy, ale wykrywany w
Application, bo operacja nie jest przejściem stanu agregatu** (więc brak metody Domain do
wywołania, a `DomainException` nie pasuje bez wciągania pojęcia bramki płatniczej do Domain,
co złamałoby ADR-0002). To domknięcie luki ADR-0017: dotąd oś miała 409 wyłącznie w Domain.

- Plik: `src/PizzaShop.Application/Common/Exceptions/ConflictException.cs` (obok pozostałych;
  bez wspólnej klasy bazowej — YAGNI, jak w ADR-0017).
- Sygnatura: `public sealed class ConflictException : Exception { public ConflictException(string message) : base(message) { } }`.
- Użycie: `InitializePaymentCommandHandler.EnsureCanInitializePayment` rzuca `ConflictException`
  (zamiast `ForbiddenOperationException`) dla `PaymentMethod != Online` oraz `PaymentStatus == Paid`.
- Różnica względem `ForbiddenOperationException`: tam operacja *jest* legalna dla
  uprzywilejowanego wykonawcy (403 to odmowa zależna od roli); tu operacja jest nielegalna
  dla wszystkich (409 to konflikt stanu). Różnica względem `DomainException`: konflikt
  wykryty w Application, bo nie odpowiada mu żadne przejście agregatu.

*Odrzucone:* dodać do Domain read-only guard (np. `Order.EnsureOnlinePaymentPending()`)
rzucający `DomainException`. Wciągałoby pojęcie „inicjalizacja płatności online / bramka" do
Domain wbrew ADR-0002, a Domain jest poza zakresem tej iteracji. Reguła „czy zamówienie
kwalifikuje się do rozpoczęcia sesji bramki" to wiedza orkiestracyjna Application, nie
niezmiennik agregatu.

**Decyzja 3 — `InitializePaymentCommand` na start tylko dla zalogowanego właściciela/obsługi; wariant gościa świadomie odłożony.**
Korygujemy dokument (sekcja 4.4): `InitializePaymentCommand` obsługuje **Customer (właściciel)
/ obsługa**, nie „anonim". Uzasadnienie odłożenia wariantu gościa:
- Podstawowa ścieżka płatności online gościa jest **już** obsłużona *inline* w
  `CreateOrderCommand` (krok 8 zwraca `PaymentRedirectUrl`) — nie przechodzi przez
  `InitializePaymentCommand`. Gość płaci przy składaniu zamówienia.
- `InitializePaymentCommand` służy wyłącznie **ponowieniu** płatności (porzucona/nieudana
  sesja). Wariant gościa wymagałby anonimowego endpointu inicjującego akcję płatniczą,
  kluczowanego `GuestTrackingToken` — a to powierzchnia wymagająca świadomego przeglądu
  bezpieczeństwa (token jako jedyny czynnik autoryzacji akcji płatniczej). Nie jest
  krytyczna dla checkoutu i nie powinna powstać „przy okazji".

*Konkretna specyfikacja odłożonego follow-upu* (żeby nie było ciche): przyszły
`InitializeGuestPaymentCommand` kluczowany `GuestTrackingToken`, analogiczny do
`GetOrderByTrackingTokenQueryHandler` (load po tokenie → ta sama logika inicjalizacji +
`SetProviderPaymentReferenceAsync`), do zbudowania gdy powstanie flow „wznów płatność" dla
gościa.

**Konsekwencje.**
- `IOrderRepository`: zmiana sygnatury `AddAsync` (+`providerPaymentReference`), dwie nowe
  metody (`Set/GetProviderPaymentReferenceAsync`). Implementacja w Infrastructure trzyma
  referencję jako kolumnę obok `Order` (jak `GuestTrackingToken`), niewidoczną dla Domain.
- Nowy plik `Common/Exceptions/ConflictException.cs` (→ 409); dopisany do osi wyjątków w
  application-layer.md sekcja 5.
- `RefundAsync` (Infrastructure, PayU) MUSI być idempotentny per zamówienie/referencja.
- Testy do zaktualizowania/dodania: `CancelOrderCommandHandler` (scenariusze:
  online+Paid ⇒ `RefundAsync` + `RefundPayment` + `Refunded`; OnPickup ⇒ brak refundu;
  online+Pending ⇒ brak refundu; `RefundAsync` rzuca ⇒ nic nie zapisane, brak `Cancelled`;
  brak referencji dla Paid ⇒ `InvalidOperationException`), `InitializePaymentCommandHandler`
  (OnPickup/Paid ⇒ `ConflictException`; happy path ⇒ `SetProviderPaymentReferenceAsync`
  wołane + zapis), `CreateOrderCommandHandler` (referencja przekazana do `AddAsync` dla
  Online, `null` dla OnPickup).
- ADR-0007 pozostaje nienaruszony (brak nowego `PaymentStatus`). ADR-0002 utrzymany
  (referencja nie trafia do Domain). ADR-0017 rozszerzony o kategorię `ConflictException`.

---

## ADR-0019: Edycja Promotion — celowe metody `UpdateWindow`/`UpdateValue`/`UpdateUsageLimit`; `Type` niemutowalny; `UsageLimit` poniżej `UsageCount` dozwolony

**Data:** 2026-07-17
**Status:** Zaakceptowana

**Kontekst.**
Iteracja 4 warstwy Application (promocje) ujawniła lukę w agregacie `Promotion` — analogiczną
do luki `MenuItem` domkniętej w ADR-0016. `application-layer.md` 4.5 wymaga `UpdatePromotionCommand`
obsługującego „aktywacja/dezaktywacja, okno, wartość, limit", ale `Promotion` eksponuje tylko
`Activate()`/`Deactivate()` — brak mutatorów dla `ValidFrom`/`ValidTo` (okno), `Value` (wartość)
i `UsageLimit`. Builder świadomie zaimplementował `UpdatePromotionCommand` tylko dla
aktywacji/dezaktywacji i zatrzymał się, nie dodając kodu do Domain samodzielnie.

Otwarte pytania projektowe: (1) jedna zbiorcza metoda vs. kilka celowych; (2) czy `ValidTo`
musi być późniejsze niż `ValidFrom` i czy okno wolno zmienić względem `UsageCount`; (3) czy
`UsageLimit` może zejść poniżej bieżącego `UsageCount`; (4) czy zmiana `Value`/`Type` jest
dozwolona przy `UsageCount > 0`; (5) czy potrzebne nowe wyjątki domenowe.

**Decyzja.**

*Kilka celowych metod, nie jedna zbiorcza `UpdateDetails` (spójnie z ADR-0016).* Dodajemy do
korzenia `Promotion`:
- `UpdateWindow(DateTimeOffset validFrom, DateTimeOffset validTo)` — oba końce okna razem,
  bo są sprzężone niezmiennikiem `ValidTo > ValidFrom`. Guard: `validTo > validFrom`, inaczej
  `ArgumentException(nameof(validTo))` — ten sam typ i reguła co `Create`.
- `UpdateValue(decimal? value)` — guard: ta sama zależna od typu walidacja co `Create`
  (`ValidateValue(Type, value)`), inaczej `ArgumentOutOfRangeException(nameof(value))`.
- `UpdateUsageLimit(int? usageLimit)` — guard: `usageLimit > 0`, gdy ustawiony (`null` =
  bez limitu), inaczej `ArgumentOutOfRangeException(nameof(usageLimit))`.

Analogia do ADR-0016: pola sprzężone niezmiennikiem grupujemy w jedną metodę (tam warianty
pod korzeniem; tu okno), pola z własną walidacją zostają osobno (`UpdateValue`,
`UpdateUsageLimit`), nie mnożymy metod bez powodu.

*(2) Okno bez sprzężenia z `UsageCount`.* Wolno je przesunąć/skrócić/wydłużyć dowolnie —
także tak, że bieżący czas wypada poza oknem (promocja przestaje kwalifikować się na
przyszłość). `UsageCount` nie ma znaczników czasu w agregacie, a poszczególne użycia są
snapshotowane na `Order` (domain-model.md 5.1), więc zmiana okna niczego wstecznie nie
unieważnia. Żaden guard wiążący okno z `UsageCount` nie powstaje. Jedyny guard okna to
`ValidTo > ValidFrom` (jak w `Create`).

*(3) `UsageLimit` poniżej `UsageCount` — DOZWOLONE (trade-off).* Np. promocja użyta 5 razy,
admin ustawia limit 3. Skutek: natychmiastowe zamknięcie promocji na nowe użycia
(`IsQualifiedFor` i `RecordUsage` już odrzucają przy `UsageCount >= UsageLimit`), bez
naruszenia niezmiennika. `UsageCount` to fakt historyczny (użycia utrwalone na zamówieniach),
którego obniżenie limitu nie cofa.
  - **Alternatywa (odrzucona): blokować** (`usageLimit >= UsageCount`, inaczej wyjątek). Argument
    za: „mylący" stan `limit < count`. Argument przeciw (przeważa): zablokowanie odbiera adminowi
    najprostszą operacyjnie drogę „domknij tę promocję limitem" i zmusza do obejścia
    (`Deactivate`), mimo że stan `count > limit` jest w pełni obsłużony przez istniejącą regułę
    kwalifikacji. Nie ma niezmiennika, który by tu pękał — więc nie wprowadzamy sztucznej blokady
    (ani nowego wyjątku). `Deactivate()` pozostaje osobną, komplementarną drogą wyłączenia.

*(4) Zmiana `Value` przy `UsageCount > 0` — DOZWOLONA.* `Order.DiscountAmount` jest
snapshotowany w chwili złożenia (domain-model.md 5.1), więc zmiana `Value` nie wpływa na już
złożone zamówienia — dotyczy wyłącznie przyszłych zastosowań. Blokada „nie zmieniaj wartości
po pierwszym użyciu" byłaby myląca (sugerowałaby wpływ na przeszłość, którego nie ma), więc
jej nie wprowadzamy. **`Type` natomiast pozostaje niemutowalny** — brak metody zmiany: 4.5 nie
wymienia typu, a zmiana typu to w praktyce inna promocja (zmieniłaby regułę walidacji `Value`
i mogłaby przemycić `BuyXGetY`, odłożony w ADR-0011). Zmiana typu = utworzenie nowej promocji.

*(5) Bez nowych wyjątków domenowych.* Wszystkie guardy reużywają typów argumentowych z
`Create` (`ArgumentException`/`ArgumentOutOfRangeException`); decyzja „limit poniżej
`UsageCount` dozwolony" celowo nie wprowadza wyjątku blokującego. Katalog
`src/PizzaShop.Domain/Exceptions/` bez zmian.

**Poza zakresem (świadomie).** `Name`, `Code`, `MinOrderValue` nie dostają mutatorów —
`application-layer.md` 4.5 ich nie wymaga. Dodać w razie realnej potrzeby, nie na zapas.

**Konsekwencje.**
- `Promotion` zyskuje trzy metody (`UpdateWindow`, `UpdateValue`, `UpdateUsageLimit`); `Type`
  bez settera. Szczegóły sygnatur i reguł: domain-model.md sekcja 8.1 (źródło prawdy dla buildera).
- `UpdatePromotionCommandHandler` uzupełnia obsługę o okno/wartość/limit (dotąd tylko
  aktywacja/dezaktywacja) — woła odpowiednie metody `Promotion` zależnie od tego, które pola
  są w requeście. Kolejność wywołań dowolna (metody niezależne, brak sprzężenia między nimi).
- Walidator FluentValidation `UpdatePromotionCommand` sprawdza kształt (np. `Value` w zakresie
  dla typu, `ValidTo > ValidFrom` na poziomie requestu, `UsageLimit > 0`), ale ostatecznym
  strażnikiem niezmienników pozostają guardy Domain (CLAUDE.md: walidator = kształt, Domain =
  dozwolona operacja).
- Nowe testy Domain: `UpdateWindow` (happy path; `validTo <= validFrom` ⇒ `ArgumentException`;
  okno przesunięte poza „teraz" ⇒ `IsQualifiedFor` false, bez wyjątku), `UpdateValue` (happy
  path per typ; wartość poza zakresem ⇒ `ArgumentOutOfRangeException`; zmiana przy
  `UsageCount > 0` dozwolona), `UpdateUsageLimit` (happy path; `<= 0` ⇒
  `ArgumentOutOfRangeException`; limit poniżej `UsageCount` dozwolony i zamyka kwalifikację;
  `null` = zdjęcie limitu).
- ADR-0011 utrzymany (`BuyXGetY` nadal odłożony; brak zmiany `Type` nie otwiera go tylnymi
  drzwiami). ADR-0016 to wzorzec, którego ta decyzja jest kontynuacją.

---

## ADR-0020: Strategia mapowania EF Core — DbContext, konfiguracje per agregat, mapowanie Value Objectów, konstruktory perystencyjne w Domain

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Warstwa Infrastructure (pusty projekt) musi utrwalać agregaty Domain przez EF Core + PostgreSQL
(ADR-0001). Domain jest już zaimplementowany i przetestowany: encje mają **prywatne konstruktory
+ fabryki statyczne**, kolekcje jako `readonly` pola wystawiane jako `IReadOnlyCollection`, oraz
Value Objecty (`Money`, `Address`, `GeoCoordinate`, `DeliveryAddress`, `ContactDetails`,
`OpeningHours`, `OrderItemExtra`). Trzeba zdecydować: (a) jak EF materializuje encje bez ctorów
bezparametrowych; (b) jak mapować każdy VO (owned type vs. value converter); (c) jak mapować
kolekcje (owned vs. many-to-many); (d) kształt DbContextu.

**Decyzja.**

*Jeden `PizzaShopDbContext`* z `DbSet`ami tylko dla korzeni agregatów (`Restaurant`, `MenuItem`,
`Ingredient`, `Order`, `Customer`, `LoyaltyAccount`, `Promotion`). Konfiguracje jako osobne pliki
`IEntityTypeConfiguration<T>` w `Persistence/Configurations/`, wpinane
`ApplyConfigurationsFromAssembly`. Klucze `Guid` generowane w Domain (`Guid.NewGuid()` w
fabrykach) ⇒ `Id` korzeni `ValueGeneratedNever()`.

*Materializacja — prywatny konstruktor bezparametrowy.* Builder dodaje `private Xxx() { }` do
każdego typu Domain, który EF materializuje (korzenie, encje podrzędne: `MenuItemVariant`,
`OrderItem`, `CustomerAddress`, `LoyaltyTransaction`; oraz VO mapowane jako owned: `Address`,
`GeoCoordinate`, `ContactDetails`, `DeliveryAddress`, `OrderItemExtra`). To wyłącznie koncesja
perystencyjna — nie dodaje żadnej zależności (Domain nadal nie referuje niczego), ctor jest
`private` (używa go tylko EF przez refleksję), a inline-inicjalizowane pola kolekcji (`= new()`)
działają z ctorem bezparametrowym. Kolekcje read-only i skip-navigation mapowane przez
`PropertyAccessMode.Field`.
  - **Alternatywa (odrzucona): osobne modele perystencyjne** (persistence POCO + mappery
    domain↔persistence). Daje pełną izolację Domain od EF, ale podwaja liczbę klas i wymaga
    mapperów w obie strony dla każdego agregatu — nieproporcjonalny koszt przy tej skali.
    Prywatny ctor to standardowy, minimalnie inwazyjny kompromis DDD+EF.

*Mapowanie Value Objectów (pełna tabela — infrastructure-layer.md 2.2):*
- **`Money` → ValueConverter na `decimal`** (`numeric(12,2)`, PLN implikowane), rejestrowany
  globalnie w `ConfigureConventions`. Single-currency (domain-model.md 2.1) czyni drugą kolumnę
  waluty martwą; konwerter (`m.Amount` ↔ `new Money(amount)`) daje jedną czystą kolumnę na kwotę.
  Jeśli kiedyś dojdzie multi-currency — migracja do owned type z kolumną waluty (przyszły ADR).
- **`Address`, `GeoCoordinate`, `ContactDetails` → owned (`OwnsOne`)** — kilka kolumn skalarnych.
- **`DeliveryAddress` → owned zagnieżdżony** (`OwnsOne` z wewnętrznym `OwnsOne(Address)` +
  `OwnsOne(Coordinate)`); opcjonalny na `Order`, wymagany na `CustomerAddress`.
- **`OpeningHours` → ValueConverter na `jsonb` (+ `ValueComparer`)** — słownik dzień→zakresy jest
  zbyt złożony na kolumny/tabelę, a to VO, nie encja; serializacja do DTO perystencyjnego i
  odtworzenie publicznym ctorem. `TimeRange` żyje wewnątrz tego JSON (brak osobnego mapowania).
- **`OrderItemExtra` → owned collection (`OwnsMany`)** — tabela z FK do `OrderItem`.

*Mapowanie kolekcji/encji podrzędnych:*
- `MenuItem.Variants` → `OwnsMany` (`MenuItemVariants`).
- `MenuItem.BaseIngredients` i `AllowedExtras` → **dwie osobne relacje many-to-many do wspólnego
  słownika `Ingredient`** (osobny korzeń z `DbSet`), z jawnie nazwanymi join-table
  (`MenuItemBaseIngredients`, `MenuItemAllowedExtras`) i field-accessem. To najtrudniejszy
  fragment mapowania — dwie kolekcje do tej samej encji muszą rozróżniać się nazwą join-table.
- `Order.Items` → `OwnsMany` z zagnieżdżonym `OwnsMany` (Extras).
- `Customer.AddressBook` → `OwnsMany` (+ owned `DeliveryAddress`).
- `LoyaltyAccount.Transactions` → `OwnsMany` (append-only).

Snapshoty na `OrderItem` (`MenuItemId`/`VariantId` jako gołe `Guid`, nazwy/ceny) to zwykłe
kolumny bez FK do katalogu — zamówienie jest niezależne od zmian menu (domain-model.md 5.2).

**Konsekwencje.**
- Builder dodaje prywatne ctory bezparametrowe do wymienionych typów Domain — jedyna zmiana w
  już zreviewowanym Domain, uzasadniona i pozbawiona zależności; reviewer powinien to potwierdzić.
- Schemat: jedna kolumna na każdą `Money`, `jsonb` na `OpeningHours`, tabele owned dla kolekcji,
  dwa join-table dla katalogu. Szczegóły i nazwy kolumn: infrastructure-layer.md 2.
- Czas: Npgsql mapuje `DateTimeOffset`→`timestamptz` przy offsecie zero (ADR-0010) — `IClock`
  zwraca UTC (offset 0); nie włączać `EnableLegacyTimestampBehavior`.

---

## ADR-0021: Dane sidecar (`GuestTrackingToken`, `ProviderPaymentReference`) jako shadow properties na tabeli `Orders`

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
ADR-0018 ustalił, że `GuestTrackingToken` i `ProviderPaymentReference` żyją **poza Domain**, jako
sidecar korelowany z `Order` („kolumna obok Order"). `IOrderRepository` ma metody operujące na
tych wartościach (`AddAsync` z oboma, `Get/SetProviderPaymentReferenceAsync`,
`GetByGuestTrackingTokenAsync`). Trzeba rozstrzygnąć fizyczne mapowanie: kolumny na tabeli `Orders`
(shadow properties) czy osobna tabela 1:1.

**Decyzja.**
**Shadow properties na tabeli `Orders`**: `builder.Property<Guid?>("GuestTrackingToken")` (z
unikalnym indeksem) i `builder.Property<string?>("ProviderPaymentReference")`, niewidoczne dla
klasy `Order`. `OrderRepository` operuje na nich przez ChangeTracker (`Entry(order).Property(...)`)
i `EF.Property<>` w predykatach:
- `AddAsync`: `Add(order)` + ustawienie obu shadow properties (bez commitu).
- `GetByGuestTrackingTokenAsync`: `FirstOrDefault(o => EF.Property<Guid?>(o, "GuestTrackingToken") == token)`.
- `SetProviderPaymentReferenceAsync`: load + set shadow property (commit przez `IUnitOfWork`).
- `GetProviderPaymentReferenceAsync`: projekcja shadow property.

**Alternatywa (odrzucona): osobna tabela 1:1** (`OrderPaymentReference`/`OrderGuestToken`).
ADR-0018 wprost mówi „kolumna obok Order"; osobna tabela dokłada join bez zysku. Shadow property
daje tę samą izolację od Domain (klasa `Order` ich nie widzi) przy jednej tabeli.

**Konsekwencje.**
- Tabela `Orders` ma dwie dodatkowe kolumny nieobecne w modelu Domain; dostęp wyłącznie przez
  `OrderRepository`. Unikalny indeks na `GuestTrackingToken` (bezpieczny, szybki lookup gościa).
- Test integracyjny (ADR-0025) musi pokryć round-trip obu shadow properties.

---

## ADR-0022: Implementacja PayU w Infrastructure (OAuth, inicjalizacja, weryfikacja podpisu, mapowanie statusów, idempotentny refund)

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
ADR-0002/0013 ustaliły PayU za `IPaymentGateway` (kontrakt w Application). Trzeba zaprojektować
konkretną implementację w Infrastructure: uwierzytelnianie do API PayU, inicjalizację zamówienia,
weryfikację podpisu notyfikacji webhook (endpoint bez JWT — bezpieczeństwo = podpis) i idempotentny
refund (ADR-0018).

**Decyzja.**
`PayUPaymentGateway : IPaymentGateway` w `Payments/PayU/`, typed `HttpClient`, konfiguracja przez
`IOptions<PayUOptions>` (POS id, `client_id`/`client_secret` OAuth, drugi klucz podpisu, `BaseUrl`).
**Sandbox = tylko inne wartości konfiguracji** (`BaseUrl = https://secure.snd.payu.com`, testowe POS),
przełączenie na produkcję to zmiana konfiguracji, nie kodu (ADR-0002).
- **OAuth** `client_credentials` z cache tokenu do wygaśnięcia (in-memory).
- **`InitializePaymentAsync`**: `POST /api/v2_1/orders` (kwota w groszach, PLN, `continueUrl`,
  `notifyUrl` = webhook Api); odpowiedź → `PaymentInitResult(RedirectUrl, ProviderPaymentReference
  = PayU orderId)`. `HttpClient` bez auto-redirectów.
- **`VerifyAndParseNotification`**: NAJPIERW weryfikacja nagłówka `OpenPayU-Signature` (MD5 z
  `body + drugi klucz`), potem parsowanie statusu. Nieprawidłowy podpis ⇒ port sygnalizuje błąd →
  handler rzuca `InvalidPaymentNotificationException` (Application → 400/401, application-layer.md 5).
  Weryfikacja żyje w Infrastructure, nie w kontrolerze (ADR-0013).
- **Mapowanie statusów** (`PayUStatusMapper`): `PENDING→Pending`, `WAITING_FOR_CONFIRMATION→Authorized`,
  `COMPLETED→Paid`, `CANCELED/REJECTED→Failed`. Handler woła odpowiednią metodę `Order`
  idempotentnie (guard clauses Domain, ADR-0013).
- **`RefundAsync`**: `POST /api/v2_1/orders/{ref}/refunds`, **idempotentny** — powtórny refund już
  zrefundowanego = sukces (ADR-0018).

**Konsekwencje.**
- Domain nie zna PayU; mapowanie i podpis w Infrastructure. Podmiana dostawcy = nowa implementacja
  bez zmian w Domain/Application.
- Testy: `PayUStatusMapper` i weryfikacja podpisu na wektorach testowych bez sieci; wywołania HTTP
  przez mock `HttpMessageHandler` (nie strzelać do realnego PayU).
- Endpoint webhooka w Api pozostaje anonimowy; bezpieczeństwo w pełni na podpisie.

---

## ADR-0023: Geokodowanie — Nominatim (OSM) jako implementacja `IGeocodingService`

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
ADR-0006 wymaga współrzędnych adresu do walidacji promienia dostawy; `IGeocodingService`
(`GeocodeAsync(Address) → GeoCoordinate?`) czeka na implementację. Trzeba wybrać dostawcę.

**Decyzja.**
`NominatimGeocodingService : IGeocodingService` (`Geocoding/`), typed `HttpClient` do OpenStreetMap
Nominatim. Wybór: **Nominatim** — darmowy, bez klucza, wystarczający dla jednej pizzerii o niskim
wolumenie. Konfiguracja `GeocodingOptions`: `BaseUrl` (domyślnie `https://nominatim.openstreetmap.org`),
**wymagany `UserAgent`** (polityka Nominatim), `TimeoutSeconds`. Brak wyniku ⇒ `null` (handler
`CreateOrderCommand` krok 2 traktuje jako błąd adresu).

**Alternatywy.**
- Google/Mapbox Geocoding — dokładniejsze, płatne, wymaga klucza — odłożone do realnej potrzeby.
- Prosty `ConfiguredGeocodingService` (współrzędne z appsettings) — przydatny dev/test bez sieci,
  opcjonalnie rejestrowany warunkowo po środowisku.

**Konsekwencje.**
- Ograniczenia operacyjne Nominatim (max ~1 req/s, wymagany User-Agent) — dla produkcji rozważyć
  własny hosting lub płatnego dostawcę (przyszły ADR); wymiana = nowa implementacja portu, bez
  zmian w Domain/Application.
- Testy przez mock `HttpMessageHandler`, bez realnych zapytań do OSM.

---

## ADR-0024: Granica kompozycji — które porty implementuje Infrastructure, a które Api (SignalR i `ICurrentUser` w Api)

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Nie każdy port z Application jest naturalnie „infrastrukturą danych/integracji". `IOrderNotifier`
to live-tracking przez SignalR (Hub to endpoint webowy), a `ICurrentUser` zależy od `HttpContext`/
JWT. Trzeba jednoznacznie wskazać, gdzie żyje implementacja każdego portu, żeby builder nie umieścił
SignalR w Infrastructure.

**Decyzja.**
- **Infrastructure** implementuje: 7 repozytoriów + `IUnitOfWork` (EF Core), `IPaymentGateway`
  (PayU), `IGeocodingService` (Nominatim), `IClock` (`SystemClock`, UTC), `ILoyaltyPolicy`
  (`LinearLoyaltyPolicy`, ADR-0014; reguła sfinalizowana w ADR-0033).
- **Api** implementuje: `IOrderNotifier` (`SignalROrderNotifier` przez `IHubContext<OrderTrackingHub>`)
  oraz `ICurrentUser` (`HttpContextCurrentUser`). SignalR **Hub** (`OrderTrackingHub`) też w Api.
  Infrastructure **nie** referuje `Microsoft.AspNetCore.SignalR`.

Uzasadnienie: Api zależy od Application i legalnie implementuje porty inherentnie webowej dostawy;
wciąganie SignalR/HttpContext do Infrastructure mieszałoby warstwę dostępu do danych z warstwą
prezentacji. To rozstrzyga pytanie „hub tu czy w Api": **w Api**.

**Konsekwencje.**
- `Infrastructure/DependencyInjection.cs` (`AddInfrastructure(IServiceCollection, IConfiguration)`)
  rejestruje wyłącznie porty z listy Infrastructure; `IOrderNotifier`/`ICurrentUser` rejestruje Api.
- Api w `Program.cs`: `AddApplication().AddInfrastructure(config)` + rejestracja `ICurrentUser`,
  `IOrderNotifier`, mapowanie `OrderTrackingHub` (warstwa Api, osobna iteracja).

---

## ADR-0025: Migracje EF Core, design-time factory i strategia testów integracyjnych (Testcontainers PostgreSQL)

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Migracje generuje się w projekcie Infrastructure ze startupem Api (CLAUDE.md). `dotnet ef` musi umieć
zbudować DbContext bez pełnego bootstrapu Api. Osobno: jak testować warstwę perystencji, skoro mapowania
(konwertery, jsonb, many-to-many, owned, shadow properties) nie są weryfikowane przez testy jednostkowe.

**Decyzja.**
- **Design-time**: `DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PizzaShopDbContext>` w
  `Persistence/`, czyta connection string ze zmiennej środowiskowej/appsettings, buduje opcje
  `UseNpgsql`. Migracje: `dotnet ef migrations add InitialCreate -p src/PizzaShop.Infrastructure -s
  src/PizzaShop.Api -o Persistence/Migrations`. Pakiet `Microsoft.EntityFrameworkCore.Design` w
  Infrastructure (hostuje factory).
- **Testy integracyjne**: nowy projekt `tests/PizzaShop.Infrastructure.Tests` z **Testcontainers
  PostgreSQL** (`Testcontainers.PostgreSql`). Zakres: round-trip każdego agregatu (w tym trudne:
  `Money` konwerter, `OpeningHours` jsonb, dwie many-to-many katalogu, sidecar shadow properties,
  zagnieżdżone owned), smoke test budowy modelu + `Database.Migrate()` na świeżym kontenerze, testy
  mapperów PayU/geocoding bez sieci (mock `HttpMessageHandler`).

**Dlaczego Testcontainers, nie InMemory/SQLite.** `EF InMemory` ignoruje konwertery/jsonb/
many-to-many/owned → fałszywe zielone; SQLite różni się typami (brak `jsonb`, `timestamptz`).
Testcontainers testuje docelowy provider Npgsql. Wymaga Dockera — GitHub Actions to wspiera (CI z
CLAUDE.md); testy integracyjne oznaczyć traitem/kategorią, by dało się je pominąć lokalnie bez Dockera.

**Konsekwencje.**
- Nowy projekt testowy `PizzaShop.Infrastructure.Tests` poza dotychczasową listą z CLAUDE.md
  (Domain/Application/Api) — do dodania i wpięcia w CI.
- CI (`dotnet test`) musi mieć dostępny runtime Dockera dla testów integracyjnych; unit-testy
  Domain/Application dalej bez Dockera.
- `IClock` (`SystemClock`) używany w testach przez podstawialną implementację (deterministyczny czas).

---

## ADR-0026: Tożsamość i uwierzytelnianie — własny `UserAccount` + BCrypt (nie ASP.NET Core Identity), JWT, powiązanie konta z `Customer`

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Warstwa Api (dziś szkielet) potrzebuje mechanizmu tożsamości: rejestracji/logowania, hashowania
haseł, wydawania i walidacji JWT (CLAUDE.md — JWT, role Customer/Employee/RestaurantAdmin/SuperAdmin).
ADR-0004/0005 ustaliły, że tożsamość (`UserAccount`) żyje **poza Domain**, a `Customer` to profil
domenowy powiązany z kontem przez `UserAccountId`. Otwarte: (1) ASP.NET Core Identity czy własna
tabela + BCrypt; (2) gdzie fizycznie żyje `UserAccount` i moduł auth w Clean Architecture; (3) kto
i kiedy tworzy `Customer` przy rejestracji oraz jak `ICurrentUser.CustomerId` trafia do handlerów
bez zapytania do bazy per żądanie.

**Decyzja.**

*(1) Własny `UserAccount` + BCrypt, NIE ASP.NET Core Identity.* Konta modelujemy minimalną klasą
`UserAccount` (Id, Email, PasswordHash, Role, IsActive, CreatedAt) z hasłem hashowanym BCrypt
(`BCrypt.Net-Next`) za portem `IPasswordHasher`. Uzasadnienie: pełne Identity dokłada rozbudowany
schemat (tabele ról, claimów, loginów zewnętrznych, tokenów), `UserManager`/`SignInManager` i model
ról jako danych — a my mamy **stały enum 4 ról** (ADR-0004), single-tenant (ADR-0003), bez loginów
zewnętrznych ani flow potwierdzania e-mail na start. Projekt świadomie rezygnuje z ciężkich
zależności (własne CQRS zamiast MediatR — ADR-0012), więc spójnie wybieramy lekką tożsamość.
  - **Alternatywa (odrzucona): ASP.NET Core Identity.** Zalety: dojrzały `PasswordHasher`, lockout,
    2FA, gotowe stores. Wady przy tej skali: narzucony schemat i abstrakcje pod wymagania, których
    nie mamy; mieszanie modelu ról-jako-danych z naszym enumem ról; większa powierzchnia w warstwie,
    która ma pozostać cienka. Gdyby doszły logowania zewnętrzne/2FA/samoobsługowy reset — powrót do
    Identity to przyszły ADR (wymiana modułu `Identity`, bez wpływu na Domain).

*(2) Umiejscowienie.* `UserAccount` to **model warstwy Application** (moduł `Identity`,
`src/PizzaShop.Application/Identity/UserAccount.cs`) — nie agregat Domain (ADR-0005), ale Application
może posiadać własne modele. Prywatny ctor bezparametrowy dla EF (spójnie z ADR-0020). Porty w
`Application/Identity/Abstractions/`: `IUserAccountRepository`, `IPasswordHasher`, `IJwtTokenGenerator`.
Implementacje: `UserAccountRepository` i `BcryptPasswordHasher` w **Infrastructure** (persystencja +
util, jak `IClock`; `DbSet<UserAccount>` dodany do `PizzaShopDbContext` z unikalnym indeksem `Email`,
nowa migracja); `JwtTokenGenerator` w **Api** (potrzebuje konfiguracji podpisu; symetrycznie do
`ICurrentUser` czytającego claimy — ADR-0024). Infrastructure pozostaje wolne od JWT. Rejestracja/
logowanie jako **CQRS Application** (`RegisterCustomerCommand`, `LoginCommand`,
`RegisterStaffAccountCommand`) — spójnie z konwencją i testowalnością handlerów (Moq).

*(3) Powiązanie `UserAccount.Id` ↔ `Customer.UserAccountId` i `CustomerId` w tokenie.*
`RegisterCustomerCommand` jest **jedynym miejscem tworzącym `Customer`** — tworzy atomowo (jeden
`IUnitOfWork.SaveChangesAsync`, wspólny scoped `DbContext`): `UserAccount(Customer)` +
`LoyaltyAccount` (ADR-0009) + `Customer` z `UserAccountId = userAccount.Id`. Personel
(`Employee`/`RestaurantAdmin`/`SuperAdmin`) tworzony przez `RegisterStaffAccountCommand` (rola admin)
**nie dostaje** profilu `Customer` (ADR-0004). Reguła kto-kogo tworzy (RestaurantAdmin → tylko
Employee; SuperAdmin → dowolna rola) egzekwowana w handlerze przez `ICurrentUser.Role` →
`ForbiddenOperationException` (ADR-0017). Duplikat e-mail: `ExistsByEmailAsync` + unikalny indeks
bazodanowy → `ConflictException` (409, ADR-0018).

JWT klienta niesie claimy `sub` (=`UserAccountId`), `role`, `email` oraz `customerId`, żeby
`HttpContextCurrentUser` odtwarzał `ICurrentUser.CustomerId` z tokenu bez zapytania do bazy per
żądanie. Konta personelu nie mają claimu `customerId`. Bootstrap: startowy `SuperAdmin` seedowany
z konfiguracji (`Seed:*`), idempotentnie — bez niego nie dałoby się utworzyć pierwszego personelu.

*Logowanie a bezpieczeństwo komunikatu.* Nieudany login (nieznany e-mail lub złe hasło) zwraca
jednolity błąd „invalid credentials" (401), bez ujawniania, czy konto istnieje. Konto z
`IsActive == false` nie może się zalogować.

**Konsekwencje.**
- Nowy moduł `Application/Identity` (model `UserAccount`, 3 porty, 3 komendy + walidatory + testy).
- Infrastructure: `DbSet<UserAccount>` + konfiguracja + migracja `AddUserAccount`;
  `UserAccountRepository`, `BcryptPasswordHasher`; pakiet `BCrypt.Net-Next`; rejestracja w
  `AddInfrastructure`. `UserAccount` to jedyna nie-domenowa encja w `PizzaShopDbContext` — świadomie
  (tożsamość poza Domain, ADR-0005).
- Api: `JwtTokenGenerator` (`IJwtTokenGenerator`), sekcja `Jwt` w konfiguracji (klucz z user-secrets).
- `ICurrentUser` zasilany z claimów JWT (`sub`/`role`/`customerId`) — patrz ADR-0027 (impl w Api).
- Wymiana na ASP.NET Core Identity w przyszłości = przepisanie modułu `Identity` za tymi samymi
  portami, bez wpływu na Domain/handlery zamówień. Szczegóły: api-layer.md sekcja 2.

---

## ADR-0027: Warstwa Api — middleware wyjątków (ProblemDetails), autoryzacja ról z jawną hierarchią, cienkie kontrolery, webhook PayU z surowym body

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Po zaprojektowaniu tożsamości (ADR-0026) trzeba domknąć powierzchnię HTTP: (1) jak mapować wyjątki
Application/Domain na kody HTTP w jednym miejscu i w jakim formacie odpowiedzi; (2) jak egzekwować
hierarchię ról (SuperAdmin ⊇ RestaurantAdmin ⊇ Employee) zgodnie z CLAUDE.md („jawnie wypisana w
`[Authorize(Roles=...)]`, nie przez token"); (3) jaki kształt mają kontrolery i które endpointy są
anonimowe; (4) jak kontroler webhooka PayU odbiera surowe body wymagane do weryfikacji podpisu.

**Decyzja.**

*(1) Jeden globalny middleware wyjątków → `ProblemDetails` (RFC 7807).* `IExceptionHandler`
(ASP.NET Core 8) mapuje po **typie konkretnym** (bez wspólnej klasy bazowej wyjątków Application —
YAGNI, ADR-0017/0018), zgodnie z osią z application-layer.md sekcja 5:
`ValidationException`→400 (+`errors`), `NotFoundException`→404, `ForbiddenOperationException`→403,
`ConflictException`→409, `InvalidPaymentNotificationException`→400, `NotSupportedException`→501,
`InvalidOperationException`→500, nieobsłużone→500 (bez `detail` z wyjątku, tylko `traceId`+log).
`DomainException` i podtypy mapowane **409 vs 422** wg tabeli: **409** dla konfliktu stanu zasobu
(`InvalidOrderStatusTransitionException`, `InvalidPaymentStatusTransitionException`,
`PromotionAlreadyAppliedException`, `LoyaltyPointsAlreadyRedeemedException`), **422** dla naruszeń
reguł biznesowych na danych (pozostałe wyjątki domenowe z domain-model.md 9, np.
`BelowMinimumOrderValueException`, `MenuItemUnavailableException`, `AddressOutsideDeliveryAreaException`);
domyślnie nieznany `DomainException`→422. `ArgumentException`/`ArgumentOutOfRangeException` z Domain
(ADR-0019) → 400 defensywnie (walidator FluentValidation jest głównym strażnikiem kształtu).
Kontrolery **nie** łapią wyjątków. Pełna tabela: api-layer.md sekcja 4.
  - *Dlaczego 409/422 tak podzielone:* 409 komunikuje „stan zasobu jest w konflikcie z żądaną
    zmianą" (przejścia/duplikaty), 422 „żądanie poprawne składniowo, ale niewykonalne wg reguł
    biznesu". Rozłączne i czytelne dla klienta API; spójne z intencją „409 = konflikt stanu" z ADR-0017.

*(2) Autoryzacja ról — jawna hierarchia w atrybutach.* Zgodnie z CLAUDE.md, każdy endpoint niesie
`[Authorize(Roles=...)]` z **jawnie wypisaną** listą ról (nie przez token, nie przez Domain).
Żeby uniknąć literówek, listy trzymamy w stałych `AuthRoles` (`Staff = "Employee,RestaurantAdmin,SuperAdmin"`,
`Admin = "RestaurantAdmin,SuperAdmin"`, `Owner = "SuperAdmin"`, `Customer = "Customer"`) — stała
rozwija się do jawnej listy w atrybucie (spełnia wymóg jawności) bez powielania stringów. Domyślna
`FallbackPolicy` wymaga uwierzytelnienia (zapomniany atrybut nie zostawia endpointu otwartego);
endpointy publiczne jawnie `[AllowAnonymous]`. Autoryzacja **zależna od stanu** (klient widzi/anuluje
tylko własne zamówienie) pozostaje w handlerach → `NotFoundException`/`ForbiddenOperationException`
(ADR-0017), NIE w policy Api.

*(3) Cienkie kontrolery przez `IDispatcher`.* Kontroler mapuje request→Command/Query, woła
`IDispatcher.Send`, mapuje wynik→`IActionResult`. Zero logiki biznesowej i dostępu do repozytoriów.
Tożsamość do handlerów wyłącznie przez `ICurrentUser` (nie parametrami). Kontrolery per moduł i
mapowanie endpoint→use case→autoryzacja: api-layer.md sekcja 6. Endpointy anonimowe: przeglądanie
menu, `GetRestaurantConfigQuery` (część publiczna), `ValidatePromotionCodeQuery`,
`CheckDeliveryAvailabilityQuery`, `CreateOrderCommand` (gość — `ICurrentUser` zasila `CustomerId`
jeśli jest token), `GetOrderByTrackingTokenQuery` (token = autoryzacja, ADR-0005), webhook PayU,
rejestracja/logowanie.

*(4) `ICurrentUser` w Api + webhook PayU z surowym body.* `HttpContextCurrentUser`
(`IHttpContextAccessor`, scoped) czyta `sub`/`customerId`/`role` z claimów (ADR-0024/0026); brak
tokenu ⇒ wszystkie null (gość). Webhook `POST /api/payments/payu/webhook` jest `[AllowAnonymous]` i
czyta **surowe body** (`StreamReader` na `Request.Body`, bez `[FromBody]`), bo weryfikacja podpisu
`OpenPayU-Signature` (Infrastructure, ADR-0022) wymaga bajt-w-bajt oryginału. Body + nagłówki →
`ConfirmPaymentFromNotificationCommand`. Zwrot: 200 dla obsłużonej/idempotentnej notyfikacji, 400 dla
nieważnego podpisu (bez szczegółów). Bezpieczeństwo endpointu = podpis, nie JWT (ADR-0013).

**Konsekwencje.**
- Api: `Middleware/ExceptionHandler.cs` (pełna oś + słownik `DomainException`→409/422),
  `Auth/HttpContextCurrentUser.cs`, `Auth/AuthRoles.cs`, kontrolery per moduł (iteracje 2–3),
  `PaymentsController` z surowym body. `Program.cs`: `AddProblemDetails`+`AddExceptionHandler`,
  `AddAuthentication().AddJwtBearer`, `AddAuthorization` z `FallbackPolicy`, `AddControllers`,
  Swagger z `Bearer`. Szczegóły kompozycji: api-layer.md sekcja 9.
- Format błędów spójny (`application/problem+json`); `detail` z bezpiecznych komunikatów wyjątków
  Application (ADR-0017), 500 bez ujawniania szczegółów.
- Kontrolery testowalne integracyjnie (`WebApplicationFactory`): autoryzacja (anonim/rola →
  401/403), mapowanie wyjątków (404/409/422), webhook (200/400).
- Ta decyzja realizuje zapisaną w application-layer.md sekcja 5 „intencję dla przyszłego middleware
  Api" — oś wyjątków ma teraz konkretną implementację warstwy Api.

---

## ADR-0028: SignalR live-tracking — `OrderTrackingHub` w Api, grupy per `OrderId`, subskrypcja gościa przez token i zalogowanego przez ownership

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
ADR-0008/0024 ustaliły live-tracking `EstimatedReadyAt`/statusu przez SignalR z Hubem w Api oraz
`IOrderNotifier` (`OrderStatusChangedAsync(orderId, status, estimatedReadyAt)`) implementowany w Api.
Trzeba rozstrzygnąć: (1) klucz grup SignalR — per `OrderId` czy per `GuestTrackingToken`; (2) jak
subskrybuje gość (bez JWT, ADR-0005) i jak zalogowany klient/obsługa, tak by nie eksponować cudzych
zamówień; (3) jak `IOrderNotifier` (kluczowany tylko `OrderId`) trafia do właściwych odbiorców.

**Decyzja.**
`OrderTrackingHub : Hub` w `src/PizzaShop.Api/Realtime/`, mapowany na `/hubs/order-tracking`,
`[AllowAnonymous]` (gość musi śledzić bez JWT). **Grupy kluczowane `OrderId`** (nazwa grupy =
`orderId.ToString()`). `SignalROrderNotifier` (impl `IOrderNotifier`, przez
`IHubContext<OrderTrackingHub>`) pushuje do `Clients.Group(orderId.ToString())` — port pozostaje
kluczowany wyłącznie `OrderId`, bez wiedzy o tokenie.

Autoryzacja **przy subskrypcji**, nie przy pushu:
- `SubscribeToGuestOrder(string trackingToken)` — Hub woła `GetOrderByTrackingTokenQuery`
  (`IDispatcher`); sukces ⇒ `Groups.AddToGroupAsync(connectionId, order.Id)`. Token nieodgadnalny =
  autoryzacja (jak endpoint trackingu gościa, ADR-0005). Nieprawidłowy token ⇒ brak subskrypcji, bez
  ujawniania istnienia zamówienia.
- `SubscribeToOrder(Guid orderId)` — Hub woła `GetOrderByIdQuery` (handler scope'uje po
  `ICurrentUser` — własne/obsługa, ADR-0017); sukces ⇒ dodanie do grupy `orderId`, inaczej brak
  subskrypcji. Tożsamość z tego samego JWT — `AddJwtBearer` z obsługą `access_token` w query stringu
  (`OnMessageReceived`), bo klient WebSocket nie ustawia nagłówka `Authorization`.

Rozstrzygnięcie „grupy per OrderId czy per GuestTrackingToken": **per `OrderId`**. Token służy tylko
do autoryzacji subskrypcji (rozwiązywany na `OrderId`), nie jako klucz grupy — dzięki temu notifier
nie musi znać tokenu, a gość i zalogowany właściciel/obsługa lądują w jednej grupie tego samego
zamówienia. Push zawiera `{ orderId, status, estimatedReadyAt }` (event `OrderStatusChanged`).
  - **Alternatywa (odrzucona): grupy per `GuestTrackingToken`** (osobny kanał gościa). Wymagałaby
    rozszerzenia `IOrderNotifier` o token (lub podwójnego pushu), a token to detal
    persystencji/gościa (ADR-0021), którego port świadomie nie zna. Klucz `OrderId` jest wspólnym
    mianownikiem obu typów odbiorców i minimalizuje kontrakt notifiera.

**Konsekwencje.**
- Api: `Realtime/OrderTrackingHub.cs`, `Realtime/SignalROrderNotifier.cs` (rejestracja
  `AddScoped<IOrderNotifier, SignalROrderNotifier>()`), `MapHub<OrderTrackingHub>("/hubs/order-tracking")`,
  `AddSignalR()`, konfiguracja JWT dla WebSocketów (query-string token). Infrastructure nie referuje
  SignalR (ADR-0024).
- Handlery przejść statusu i `SetEstimatedReadyAtCommand` (application-layer.md 4.3) wołają
  `IOrderNotifier` bez zmian — dostają działającą implementację dopiero w Iteracji 4 Api.
- Testy: subskrypcja gościa po tokenie (poprawny → dołączony do grupy; zły → nie), zalogowanego po
  ownership (własne → tak; cudze → nie), dostarczenie `OrderStatusChanged` do grupy `OrderId`.
- Iteracja 4 Api (SignalR + Loyalty) — szczegóły w api-layer.md sekcje 8 i 10.

---

## ADR-0029: Powiązanie `Customer` ↔ `LoyaltyAccount` jednokierunkowe (FK na `LoyaltyAccount.CustomerId`) — usunięcie cyklu tworzenia, odrzucenie opcjonalnego `Guid? id` w fabrykach

**Data:** 2026-07-20
**Status:** Zaakceptowana

**Kontekst.**
Iteracja 1 warstwy Api (ADR-0026, api-layer.md 2.5–2.6) ujawniła cykliczną zależność przy
tworzeniu: `Customer` trzymał `LoyaltyAccountId` (walidowany jako niepusty), a `LoyaltyAccount`
trzyma `CustomerId` (walidowany jako niepusty). Każda fabryka wymagała więc Id drugiej strony —
klasyczny „chicken-and-egg" (żadnej nie da się utworzyć pierwszej bez Id drugiej).

Builder rozwiązał to doraźnie: dodał opcjonalny parametr `Guid? id` do `Customer.Create(...)`
i `LoyaltyAccount.Create(...)`, a `RegisterCustomerCommandHandler` generuje wspólny `customerId`
z wyprzedzeniem i wstrzykuje go do `Customer.Create(..., id: customerId)`. To zmiana w Domain,
świadomie niedokumentowana przez buildera, zgłoszona do decyzji architekta.

Analiza istniejącego kodu ujawniła fakt decydujący: **żaden handler nie nawiguje przez
`Customer.LoyaltyAccountId`**. Cała Application pobiera konto lojalnościowe wyłącznie przez
`ILoyaltyAccountRepository.GetByCustomerIdAsync(customerId)` (`GetLoyaltyBalanceQueryHandler`,
`CreateOrderCommandHandler`, `CompleteOrderCommandHandler`). Referencja `Customer.LoyaltyAccountId`
jest zatem **martwa nawigacyjnie** — a to ona (obok walidacji „niepusty") wytwarza cały cykl. W
tej sytuacji cykl jest artefaktem podwójnej referencji, nie realną potrzebą modelu.

**Rozważane opcje.**
- **(a)** Rozwiązanie buildera: zachować obie referencje, opcjonalny `Guid? id` w obu fabrykach,
  wspólny `customerId` generowany w handlerze.
- **(b)** Zachować obie referencje, ale zamiast opcjonalnego publicznego `id` tworzyć w dwóch
  krokach: `Customer.Create` bez `loyaltyAccountId`, potem `LoyaltyAccount.Create(customer.Id)`,
  potem jednorazowy, strzeżony `customer.LinkLoyaltyAccount(loyalty.Id)`.
- **(c)** **Uczynić powiązanie jednokierunkowym**: usunąć `Customer.LoyaltyAccountId`, zostawić
  wyłącznie `LoyaltyAccount.CustomerId`. Fabryki generują własne Id, bez parametru `id`.

**Decyzja: (c). Odrzucamy rozwiązanie buildera (a).**
Usuwamy `Customer.LoyaltyAccountId`. Powiązanie 1:1 trzyma **tylko strona zależna**
(`LoyaltyAccount.CustomerId`) — `LoyaltyAccount` nie istnieje bez `Customer`, więc to ono jest
naturalnym nosicielem FK (konwencja DDD/EF: dependent references principal; strona zależna
wskazuje na korzeń). Skutki dla kodu (zlecenie dla buildera):
- `Customer`: usunąć właściwość `LoyaltyAccountId`, usunąć parametr `loyaltyAccountId` **oraz**
  opcjonalny `Guid? id` z `Customer.Create`; fabryka znów generuje `Id` wewnętrznie
  (`Guid.NewGuid()`).
- `LoyaltyAccount.Create`: usunąć opcjonalny `Guid? id` (był nieużywany przez jedynego wołającego —
  handler i tak nie przekazywał `id` do `LoyaltyAccount.Create`); zostaje `Create(Guid customerId)`
  generujące własne `Id`.
- `RegisterCustomerCommandHandler`: usunąć wstępnie generowany `customerId`. Nowa kolejność bez
  cyklu: `customer = Customer.Create(userAccount.Id, fullName, email, now, phone)` →
  `loyalty = LoyaltyAccount.Create(customer.Id)`. Atomowość i jeden `SaveChangesAsync` bez zmian
  (ADR-0026, api-layer.md 2.6).
- Infrastructure: `CustomerConfiguration` — usunąć mapowanie `LoyaltyAccountId`; nowa migracja
  usuwająca kolumnę `Customers.LoyaltyAccountId`. `LoyaltyAccountConfiguration.CustomerId`
  dostaje **unikalny indeks** (twardy strażnik reguły „jedno konto lojalnościowe na klienta" —
  zastępuje w bazie utracony niezmiennik jednokierunkowo). Istniejący `IX_LoyaltyAccounts_CustomerId`
  zmienić na `IsUnique()`.
- Testy Domain (`Customer`) aktualizuje builder (usunięcie asercji o `LoyaltyAccountId`, korekta
  wywołań `Customer.Create`); testy handlera rejestracji — korekta kolejności tworzenia.

**Dlaczego odrzucamy (a) — opcjonalny `Guid? id` w fabrykach.**
- **Rozszczelnia niezmiennik 1:1.** Publiczny, opcjonalny `id` pozwala **dowolnemu** wołającemu
  (nie tylko handlerowi rejestracji) wstrzyknąć arbitralne lub kolidujące Id. Nic w Domain nie
  wymusza spójności czwórki `Customer.Id` / `Customer.LoyaltyAccountId` / `LoyaltyAccount.Id` /
  `LoyaltyAccount.CustomerId` — łatwo w dobrej wierze utworzyć `Customer` wskazujący na jedno
  konto, a `LoyaltyAccount` wskazujący na innego klienta, cicho łamiąc powiązanie. To dokładnie
  ten rodzaj „model po cichu dopuszcza niespójny stan", który odrzucaliśmy w ADR-0016.
- **Martwy parametr.** `id` na `LoyaltyAccount.Create` nie był w ogóle używany przez jedynego
  wołającego — dodany „dla symetrii", zwiększa powierzchnię API bez pokrycia.
- **Rozszerza publiczną powierzchnię fabryki o troskę perystencyjno-koordynacyjną** (uzgadnianie
  Id między dwoma agregatami), która należy wyłącznie do handlera rejestracji.

**Dlaczego odrzucamy (b).**
Rozwiązuje ryzyko wstrzyknięcia (link strzeżony, jednorazowy) i nie wymaga migracji, ale **zostawia
martwe pole** `Customer.LoyaltyAccountId`, którego nic nie czyta, oraz wprowadza dwustopniowe
tworzenie z przejściowym, niespójnym `Customer` (puste `LoyaltyAccountId` między krokami). Skoro
pole i tak jest nawigacyjnie nieużywane, utrzymywanie go setterem to czysta ceremonia — czystsze
jest usunięcie pola (c).

**Trade-off decyzji (c).**
Wymaga tknięcia Infrastructure (konfiguracja + migracja usuwająca kolumnę) — więcej plików niż
czysto additive (a). Koszt akceptowalny: brak danych produkcyjnych, migracja trywialna (drop jednej
kolumny + unikalny indeks). W zamian: brak cyklu u źródła, brak ryzyka wstrzyknięcia Id, brak
martwego pola i martwego parametru, model zgodny z faktyczną nawigacją (`GetByCustomerIdAsync`).

**Konsekwencje.**
- Zmiana w już zreviewowanym Domain: usunięcie `Customer.LoyaltyAccountId` i parametrów
  `loyaltyAccountId`/`id` z `Customer.Create` oraz `id` z `LoyaltyAccount.Create`. Reviewer
  powinien potwierdzić brak innych czytelników pola (Find All References — patrz CLAUDE.md flow).
- domain-model.md zaktualizowany: sekcja 6 (usunięty wiersz `LoyaltyAccountId`), sekcja 7.1
  (`CustomerId` = jedyna strona powiązania, unikalny), sekcja 10 (nowa notatka) i diagram sekcja 11.
- ADR-0009 („`LoyaltyAccount` 1:1 z `Customer`") pozostaje w mocy — zmienia się tylko strona
  trzymająca referencję, nie liczność relacji. ADR-0026 (atomowa rejestracja) bez zmian co do
  atomowości; zmienia się jedynie kolejność tworzenia i znika wspólny, wstępnie generowany `Id`.
- Wzorzec wiążący na przyszłość: relacje 1:1 między osobnymi agregatami trzymamy jednokierunkowo,
  FK po stronie zależnej; nie wprowadzamy publicznych, opcjonalnych parametrów `id` w fabrykach do
  uzgadniania tożsamości między agregatami (koordynacja Id należy do handlera/persystencji, nie do
  publicznej powierzchni Domain).

---

## ADR-0030: Reconciliacja route-id vs. body-id w kontrolerach mutujących — route jako jedyne źródło prawdy (nadpisanie), bez guardu `BadRequest()`

**Data:** 2026-07-21
**Status:** Zaakceptowana

**Kontekst.**
Iteracja 2 Api: builder dodał w `MenuController`/`IngredientsController`/`PromotionsController`
(PUT/PATCH z `{id}` w route i id w body Commandu) guard `if (id != command.Id) return BadRequest()`
— niedokumentowany 4. krok kontrolera, zgłoszony jako możliwe przekroczenie zasady „cienki
kontroler". Reviewer wskazał, że wzorzec wróci w `OrdersController`/`PaymentsController`
(Iteracja 3, PUT `/{id}/estimated-ready-at`) i poprosił o rozstrzygnięcie: (a) nadpisać body-id
route-idem (bez gałęzi), (b) zachować i sformalizować guard `BadRequest()`.

**Decyzja: (a).** Route = jedyne źródło prawdy; kontroler mapuje `command with { Id = routeId }`
(Commandy to recordy), bez guardu i bez `BadRequest()`. Pole id w body jest redundantne i ignorowane.

**Dlaczego nie (b).** Guard to logika decyzyjna + wynik błędu w kontrolerze (łamie „cienki
kontroler", sekcja 1 api-layer.md) i tworzy błąd poza scentralizowaną osią `ProblemDetails`
(ADR-0027) — surowy `BadRequest()` omija format `type`/`traceId`. (a) *eliminuje* rozbieżność
(handler zawsze operuje na zasobie z URL), (b) ją tylko *wykrywa*. Odrzucono też osobne
request-DTO bez id (redundancję usuwa u źródła, ale dokłada DTO+mapowanie do każdego PUT,
niespójnie z wiązaniem Command wprost z body na POST; YAGNI).

**Konsekwencje.**
Reguła w `docs/api-layer.md` sekcja 1.1 (wiążąca dla obecnych i przyszłych kontrolerów
mutujących). Builder usuwa 4 guardy i stosuje `with { ... = routeId }`. Trade-off: rozbieżne
body-id ignorowane po cichu — akceptowalne (URL = kontrakt tożsamości). Ewentualne twarde
odrzucanie w przyszłości = scentralizowany `ActionFilter` → `ProblemDetails`, nie gałąź w
kontrolerze (przyszły ADR na realną potrzebę).

---

## ADR-0031: Addendum do ADR-0028 — `NoopOrderNotifier` w Iteracji 3; live-tracking (SignalR) realnie nieaktywny do Iteracji 4

**Data:** 2026-07-21
**Status:** Zaakceptowana (addendum do ADR-0028)

**Kontekst.**
ADR-0028 założył, że handlery przejść statusu wołają `IOrderNotifier` bez zmian i „dostają działającą
implementację (`SignalROrderNotifier`) dopiero w Iteracji 4 Api". Nie przewidział jednak, że `IOrderNotifier`
jest **twardą zależnością konstruktora** w 8 handlerach przejść statusu Orders (`AcceptOrderCommandHandler`,
`CancelOrderCommandHandler`, `CompleteOrderCommandHandler`, `MarkReadyCommandHandler`, `RejectOrderCommandHandler`,
`SetEstimatedReadyAtCommandHandler`, `StartDeliveryCommandHandler`, `StartPreparationCommandHandler`), a te
handlery zaczynają być realnie wołane po HTTP już w **Iteracji 3** (`OrdersController`). Bez *jakiejkolwiek*
rejestracji portu w DI kontener nie rozwiąże zależności — 500 w runtime na każdym przejściu statusu, mimo że
`SignalROrderNotifier` planowo powstaje dopiero w Iteracji 4.

**Decyzja.**
Na czas Iteracji 3 port `IOrderNotifier` dostaje tymczasową, jawnie no-opową implementację:
`src/PizzaShop.Api/Realtime/NoopOrderNotifier.cs` (`OrderStatusChangedAsync` → `Task.CompletedTask`, zero
side-effectów), zarejestrowaną w `Program.cs` w miejscu docelowego `SignalROrderNotifier`. Zakres czysto Api
(porty webowe implementowane w Api — ADR-0024), zgodny z kontraktem portu z ADR-0028. Iteracja 4 podmienia
**tylko tę jedną linię rejestracji** (`AddScoped<IOrderNotifier, SignalROrderNotifier>()`) i dodaje Hub — reszta
kompozycji bez zmian. Rozwiązanie zweryfikowane przez reviewera jako poprawne, bezpieczne i poprawnie oznaczone
jako tymczasowe w kodzie (XML-doc / komentarz przy rejestracji).

**Konsekwencja widoczna dla biznesu.**
Do wdrożenia Iteracji 4 **live-tracking (SignalR) realnie nie działa**: klienci ani personel **nie otrzymują
żadnych powiadomień o zmianie statusu zamówienia ani o `EstimatedReadyAt`**, mimo że endpointy przejść statusu
w `OrdersController` (Iteracja 3) już działają i są używane. Zmiana statusu utrwala się w bazie i jest widoczna
przez zapytania (poll/refresh), ale push w czasie rzeczywistym jest ciszą. To znany, zamierzony stan
przejściowy — nie regres i nie luka bezpieczeństwa (no-op nic nie eksponuje) — ale funkcjonalnie klient nie
dostaje obiecanego śledzenia „na żywo" aż do Iteracji 4. Ślad zostaje tu, w logu decyzyjnym, nie tylko
w komentarzu w kodzie.

**Konsekwencje techniczne.**
- Api: `Realtime/NoopOrderNotifier.cs` (tymczasowy), rejestracja w `Program.cs` z adnotacją „Iteracja 4
  podmienia tę linię na `SignalROrderNotifier`". Brak zmian w handlerach Application (kontrakt `IOrderNotifier`
  bez zmian).
- Iteracja 4: rejestracja produkcyjna wskazuje `SignalROrderNotifier` + `MapHub` + `AddSignalR()` (per
  ADR-0028); `NoopOrderNotifier` można zostawić jako fallback do testów. Definition of done Iteracji 4 obejmuje
  potwierdzenie, że powiadomienia realnie docierają do grupy `OrderId`.
- Ryzyko przeoczenia: no-op nie zgłasza się sam (brak błędu przy braku pushu). Dlatego zależność jest
  udokumentowana tu jako otwarty dług, nie tylko w komentarzu w kodzie.

---

## ADR-0032: `HubHttpContextFilter` (`IHubFilter`) re-kotwiczy `IHttpContextAccessor.HttpContext` na czas wywołania metody Huba — naprawa cichej utraty `ICurrentUser` w SignalR

**Data:** 2026-07-22
**Status:** Zaakceptowana (rozszerza ADR-0028; addendum do ADR-0024/0026 — `HttpContextCurrentUser`)

**Kontekst.**
Bug wykryty w toku przeglądu Iteracji 4 (SignalR + Loyalty), potwierdzony na kodzie — nie tylko
luka testowa, lecz realny cichy błąd runtime. `HttpContextCurrentUser`
(`src/PizzaShop.Api/Auth/HttpContextCurrentUser.cs`) czyta claimy wyłącznie z
`IHttpContextAccessor.HttpContext` (holder oparty na `AsyncLocal`). `SubscribeToOrder` na
`OrderTrackingHub` woła `GetOrderByIdQuery`, którego handler (`GetOrderByIdQueryHandler`) egzekwuje
własność zamówienia przez `ICurrentUser` (`Role`, `CustomerId`) — czyli pośrednio przez
`IHttpContextAccessor`.

ASP.NET Core czyści `AsyncLocal`-owy holder `IHttpContextAccessor.HttpContext` z chwilą zakończenia
konkretnego żądania HTTP, które dostarczyło wywołanie metody Huba (np. pojedynczy POST long-pollingu
niosący komunikat `SubscribeToOrder`) — mimo że asynchroniczna kontynuacja tej metody Huba dalej
działa poza cyklem życia tamtego żądania. W efekcie `await` wewnątrz `GetOrderByIdQueryHandler`
wznawiał się z `IHttpContextAccessor.HttpContext == null`, choć `HubCallerContext.User` i
`Context.GetHttpContext()` na samym Hubie nadal widziały poprawną tożsamość. Skutek produkcyjny: dla
zalogowanego właściciela `_currentUser.CustomerId` było `null`, kontrola własności rzucała
`NotFoundException`, Hub łapał ją cicho i **nigdy nie dołączał połączenia do grupy `OrderId`** —
zachowanie nieodróżnialne od nieistniejącego zamówienia (żadnego wyjątku, żadnego logu, klient nie
dostaje pushy). Klasyczny cichy błąd: niewidoczny w zwykłym flow request/response (tam holder
czyszczony jest dopiero po realnym końcu obsługi), ale bardzo realny w SignalR, gdzie metoda Huba
rutynowo żyje dłużej niż pojedyncze żądanie transportu, które ją wyzwoliło.

Kluczowa uwaga o zasięgu: **ścieżka gościa `SubscribeToGuestOrder` nie była realnie dotknięta tym
bugiem.** Jej handler `GetOrderByTrackingTokenQueryHandler` zależy **wyłącznie** od
`IOrderRepository` — nie czyta `ICurrentUser` ani `IHttpContextAccessor` (autoryzacją jest samo
posiadanie nieodgadnalnego tokenu, ADR-0005). Zweryfikowane na kodzie. Ścieżka gościa działała
poprawnie niezależnie od stanu `HttpContext`.

**Decyzja.**
Globalny `IHubFilter`: `src/PizzaShop.Api/Realtime/HubHttpContextFilter.cs`. Na czas każdego
`InvokeMethodAsync` zapisuje poprzednią wartość, ustawia
`_httpContextAccessor.HttpContext = invocationContext.Context.GetHttpContext()` (setter tworzy
świeży, niezależny holder `AsyncLocal`, który propaguje w dół całego łańcucha wywołania Huba i
przeżywa zakończenie żądania transportu), a w `finally` przywraca poprzednią wartość. Zarejestrowany
w `Program.cs`: `AddSignalR(options => options.AddFilter<HubHttpContextFilter>())`. Chroni **każdą**
obecną i przyszłą metodę Huba, nie tylko `SubscribeToOrder`.

**Ocena poprawności (architekt, na kodzie).** Fix jest poprawny i idiomatyczny — to standardowy
wzorzec dla tego dokładnie problemu. Filtr owija wywołanie metody Huba wraz z jej kontynuacjami
async, a przypisanie przez setter `IHttpContextAccessor` (mechanika holder-object) sprawia, że
wartość płynie w dół, ale zmiany nie wyciekają w górę — dokładnie to jest potrzebne. Względem
ścieżki gościa: pokryta prewencyjnie (gdyby przyszły handler gościa zaczął czytać `ICurrentUser`,
filtr już go chroni), choć dziś jej nie potrzebuje.

**Alternatywy rozważone i odrzucone.**
- *Czytanie `Context.User` bezpośrednio w każdej metodzie Huba i przekazywanie tożsamości jawnie do
  zapytań* — odrzucone: łamie abstrakcję `ICurrentUser` jako jedynego źródła tożsamości, duplikuje
  logikę autoryzacji w Api, **nie pokrywa** kodu warstwy Application, który i tak czyta `ICurrentUser`
  (musiałby przyjmować tożsamość parametrem — przeciek transportu do kontraktów Application), i
  wymaga dotknięcia każdej metody z osobna. Filtr zostawia `ICurrentUser` przezroczyste dla Application.
- *Dedykowana implementacja `ICurrentUser` dla SignalR czytająca z kontekstu Huba* — odrzucone:
  Hub i handlery Application dzielą ten sam scope DI, więc dwie implementacje `ICurrentUser` zależne
  od ścieżki (HTTP vs Hub) to komplikacja rejestracji i ryzyko rozjazdu. Filtr utrzymuje jedną
  implementację (`HttpContextCurrentUser`) dla obu ścieżek.

Filtr globalny jest podejściem właściwym; obecny fix jest najlepszą z rozważanych opcji.

**Konsekwencje.**
- Globalna, jednolita ochrona wszystkich metod Huba — nowe Huby/metody dostają poprawną propagację
  `ICurrentUser` bez dodatkowej pracy, o ile polegają na `IHttpContextAccessor`/`ICurrentUser`
  (nie czytają tożsamości „ręcznie" z `Context`).
- **Ograniczenie do udokumentowania dla przyszłych Hubów:** `HubHttpContextFilter` nadpisuje tylko
  `InvokeMethodAsync`. `IHubFilter` ma osobne `OnConnectedAsync`/`OnDisconnectedAsync` (tu
  niezaimplementowane — działa domyślne przejście `next`), więc **zdarzenia cyklu życia połączenia
  NIE są re-kotwiczone**. Obecny `OrderTrackingHub` nie czyta `ICurrentUser` w
  `OnConnected/OnDisconnected`, więc dziś to nie problem. Jeśli przyszły Hub zacznie rozstrzygać
  tożsamość w tych zdarzeniach, trzeba albo dopisać re-kotwiczenie w tych dwóch metodach filtra, albo
  świadomie czytać `Context.User` na miejscu. Przekazane builderowi jako świadome ograniczenie, nie
  aktywny bug.
- Test regresyjny: `tests/PizzaShop.Api.Tests/Realtime/OrderTrackingHubIntegrationTests.cs` — realny
  `HubConnection` (SignalR.Client, long polling nad `TestServer`), realny JWT w query stringu
  `access_token`, realne przejście statusu przez personel; potwierdza push `OrderStatusChanged` dla
  właściciela i jego brak dla nie-właściciela. Unit-testy z mockiem `HubCallerContext` z zasady nie
  wykryłyby tej regresji (cicha nie-subskrypcja jest nieodróżnialna od „nie ma zamówienia").
- Build zielony (0 błędów); testy Domain 205, Application 260, Api 104 (Infrastructure.Tests wymagają
  lokalnego Dockera).
- ADR-0028 pozostaje w mocy (grupy per `OrderId`, autoryzacja przy subskrypcji); ta decyzja domyka
  jego założenie, że tożsamość z `access_token` „po prostu dociera" do handlera wołanego z Huba — bez
  filtra nie docierała. api-layer.md sekcja 8.1 uzupełniona o ten mechanizm.

---

## ADR-0033: Finalizacja przelicznika punktów lojalnościowych (domknięcie ADR-0009/ADR-0014)

**Data:** 2026-07-22
**Status:** Zaakceptowana

**Kontekst.**
ADR-0009 zaprojektował szkielet punktów lojalnościowych (`LoyaltyAccount` + `LoyaltyTransaction`)
celowo bez przelicznika, a ADR-0014 wprowadził port `ILoyaltyPolicy` z tymczasową, „placeholderową"
implementacją `LinearLoyaltyPolicy`. Reguła czekała na decyzję biznesu. Właściciel produktu ją podjął:
**obecny przelicznik jest przelicznikiem docelowym/finalnym**. Nic nie zmienia się liczbowo — zmienia
się status: to już nie jest tymczasowy placeholder, lecz zaakceptowana reguła.

**Decyzja.**
Przelicznik z `LinearLoyaltyPolicy` staje się regułą **finalną**, bez zmian liczbowych:
- **Naliczanie:** 1 punkt za każdy pełny 1 PLN wartości `Subtotal` zamówienia
  (`CalculatePointsToEarn`, `Math.Floor`).
- **Wymiana:** 1 punkt = 0,05 PLN rabatu (`CalculateRedemptionValue`; 5% wartości punktu w złotówce
  naliczania).
- **Brak górnego limitu procentowego** pokrycia zamówienia punktami — jedyny naturalny limit to saldo
  klienta (`MaxRedeemablePoints` zwraca całe saldo).

Zmienia się wyłącznie **status** reguły (z „tymczasowa/placeholder" na „zaakceptowana docelowa"), nie
zachowanie ani sygnatury.

**Co świadomie NIE ulega zmianie.**
- **Port `ILoyaltyPolicy` i klasa `LinearLoyaltyPolicy` pozostają bez zmian strukturalnych** —
  sygnatury, rejestracja DI (`AddScoped<ILoyaltyPolicy, LinearLoyaltyPolicy>()`, ADR-0024),
  umiejscowienie w Infrastructure. Szkielet z ADR-0009/0014 (polityka za portem) pozostaje słuszny
  **również po ustaleniu reguły**: to nadal wymienialna implementacja portu, gdyby biznes kiedyś
  zmienił zasady — wtedy nowa implementacja, bez migracji modelu punktów. Utrzymujemy abstrakcję mimo
  ustalenia reguły, bo koszt jej utrzymania jest zerowy, a wtapianie reguły w handlery byłoby regresją
  elastyczności bez zysku (spójnie z filozofią ADR-0009).
- **Model Domain** (`LoyaltyAccount`, `LoyaltyTransaction`, `Order.SetPointsToEarn`/
  `RedeemLoyaltyPoints`) bez zmian — Domain nadal nie zna przelicznika.

**Konsekwencje (kosmetyczne — bez zmiany zachowania, sygnatur ani testów zachowania).**
- `LinearLoyaltyPolicy.cs` / `ILoyaltyPolicy.cs`: usunięcie słów „placeholder"/„tymczasowy"/
  „has not finalized" z docstringów i XML-doc; docstring opisuje regułę jako finalną (z odwołaniem do
  ADR-0033). Zero zmian w ciele metod, stałych i sygnaturach.
- Dokumentacja: `infrastructure-layer.md` (linie 63, 281, 300) i `application-layer.md` (345) — usunięcie
  adnotacji „placeholder"; `domain-model.md` sekcja 7.2 zaktualizowana (reguła finalna, nie „do ustalenia"),
  sekcja 10 (nowa notatka). W tej samej zmianie: wzmianka w ADR-0024 („`LinearLoyaltyPolicy` placeholder")
  skorygowana na „`LinearLoyaltyPolicy`, reguła sfinalizowana w ADR-0033".
- Żaden test nie asertuje słowa „placeholder"; testy zachowania przelicznika (jeśli istnieją przez mock
  `ILoyaltyPolicy` w testach handlerów) pozostają bez zmian.
- ADR-0009 i ADR-0014 dostają w nagłówku `Status` adnotację „domknięta przez ADR-0033" (treść historyczna
  nienaruszona — ADR-y się nie nadpisują).

**Zakres dla buildera (kosmetyka, bez zmiany zachowania).**
1. `src/PizzaShop.Infrastructure/Loyalty/LinearLoyaltyPolicy.cs` — przepisać XML-doc (reguła finalna,
   ADR-0033; usunąć „Placeholder"/„has not finalized").
2. `src/PizzaShop.Application/Abstractions/Loyalty/ILoyaltyPolicy.cs` — dostroić docstring (usunąć sugestię
   tymczasowości; port pozostaje wymienialny, ale reguła nie jest już „do ustalenia").
3. Docs: `infrastructure-layer.md` (63/281/300), `application-layer.md` (345) — usunąć „placeholder".
   (Aktualizacja sekcji „Świadomie odłożone" w `CLAUDE.md` — po stronie właściciela, nie ruszać jej jako
   konfiguracji projektu.)

---

## ADR-0034: Implementacja promocji BuyXGetY — konfiguracja `BuyXGetYRule`, `OrderDiscountContext`, nowa sygnatura `CalculateDiscount` (domknięcie ADR-0011)

**Data:** 2026-07-22
**Status:** Zaakceptowana

**Kontekst.**
ADR-0011 odłożył wyliczenie rabatu dla `PromotionType.BuyXGetY`, bo agregat `Promotion` nie zna pozycji
zamówienia, a `CalculateDiscount` operował tylko na subtotalu/opłacie za dostawę.
`CreatePromotionCommandValidator` jawnie odrzucał tworzenie promocji tego typu;
`Promotion.CalculateDiscount` rzucał `NotSupportedException`. ADR-0011 zapowiedział, że pełna obsługa
wymaga przekazania kontekstu zamówienia (pozycje, ilości, ceny) i osobnego ADR doprecyzowującego jego
kształt. Powstał faktyczny use case — implementujemy.

Dostępne dane pozycji (`OrderItem`, domain-model.md 5.2): `MenuItemId`, `VariantId`, `UnitPrice` (cena
jednostkowa bazowa, bez dodatków), `Quantity`, `Extras`, `LineTotal`. To wystarcza do rozstrzygnięcia
kwalifikacji i kwoty rabatu.

**Decyzja — zakres funkcjonalny (minimalny, ale użyteczny).**

*Konfiguracja BuyXGetY jako owned Value Object `BuyXGetYRule` na `Promotion`* (obecny **iff**
`Type == BuyXGetY`):
- `TriggerMenuItemId: Guid` — konkretny produkt-wyzwalacz (musi być kupiony); `!= Guid.Empty`.
- `BuyQuantity: int` (X ≥ 1) — ile sztuk wyzwalacza uruchamia jeden zestaw.
- `RewardMenuItemId: Guid` — konkretny produkt-nagroda (może równać się wyzwalaczowi); `!= Guid.Empty`.
- `GetQuantity: int` (Y ≥ 1) — ile sztuk nagrody rabatowanych na zestaw.
- `RewardDiscountPercentage: decimal` (0 < pct ≤ 100; 100 = gratis) — procent rabatu na sztukę nagrody.

`Promotion.Value` pozostaje `null` dla BuyXGetY (cała konfiguracja w regule — grupujemy sprzężone
parametry w jeden VO, spójnie z filozofią ADR-0016/0019, root bez rozlanych nullowalnych kolumn).

*Świadomie w zakresie:* reward = ten sam produkt co trigger (klasyczne „3 za 2" na jednej pozycji) LUB
inny produkt (kup pizzę → napój taniej); rabat procentowy na nagrodę (100% = gratis, <100% = taniej);
**wielokrotna kwalifikacja (stacking zestawów)** — floor z dzielenia, wiele zestawów w jednym zamówieniu
(naturalne i użyteczne, minimalny dodatkowy koszt).

*Świadomie poza zakresem (przyszły ADR na realną potrzebę):*
- „dowolna pizza / zakres kategorii" jako wyzwalacz/nagroda (dziś tylko konkretny `MenuItemId`) —
  wymagałoby referencji kategorii na promocji.
- Automatyczne **dodanie** gratisowej pozycji do koszyka — rabatujemy wyłącznie sztuki nagrody
  **faktycznie obecne** w zamówieniu; promocja nie dokłada pozycji.
- Rabat liczony od ceny z dodatkami — wartościujemy nagrodę po `UnitPrice` (cena bazowa wariantu),
  dodatki wyłączone (prostsze, deterministyczne, gwarantuje rabat ≤ subtotal).
- Edycja parametrów `BuyXGetYRule` po utworzeniu — jak `Type` (ADR-0019) reguła definiuje istotę
  promocji; zmiana = nowa promocja. `UpdatePromotionCommand` nie dotyka reguły.
- Stackowanie z innymi promocjami — nadal jedna promocja na zamówienie (`Order.AppliedPromotionId`).

**Decyzja — semantyka wyliczenia (`CalculateDiscount` dla BuyXGetY).**
Niech `triggerUnits` = Σ `Quantity` linii, gdzie `MenuItemId == TriggerMenuItemId`.
- **Nagroda == wyzwalacz** (`RewardMenuItemId == TriggerMenuItemId`): rozmiar zestawu = `X + Y`;
  `zestawy = floor(triggerUnits / (X + Y))`; sztuk rabatowanych = `zestawy * Y`. (Semantyka „N za M": w
  koszyku masz X+Y, płacisz za X.)
- **Nagroda ≠ wyzwalacz:** `zestawy = floor(triggerUnits / X)`; `rewardUnits` = Σ `Quantity` linii
  nagrody; sztuk rabatowanych = `min(zestawy * Y, rewardUnits)`.
- Sztuki rabatowane wybierane jako **najtańsze** jednostki produktu-nagrody (po `UnitPrice`) —
  deterministyczne i jednoznaczne przy różnych wariantach.
- Rabat = Σ po sztukach rabatowanych `round(UnitPrice * pct/100, 2)`, waluta z subtotalu. Wartościowanie
  po `UnitPrice` (bez dodatków) gwarantuje rabat ≤ subtotal.
- *Kwalifikacja liniowa:* jeśli `triggerUnits` nie tworzy choćby jednego pełnego zestawu, albo
  (cross-product) `rewardUnits == 0` → `PromotionNotApplicableException` (jak dziś dla `!IsQualifiedFor`).
  Generyczne bramki (`IsActive`, okno, `MinOrderValue`, `UsageLimit`, kod) nadal obowiązują przez
  `IsQualifiedFor`.

**Decyzja — `OrderDiscountContext` (Domain VO/rekord, bez referencji do encji `Order`).**
`OrderDiscountContext(Money Subtotal, Money DeliveryFee, DateTimeOffset When, string? SuppliedCode,
IReadOnlyList<OrderDiscountLine> Lines)`, gdzie `OrderDiscountLine(Guid MenuItemId, Money UnitPrice,
int Quantity)`. Zawiera tylko dane, nie trzyma referencji do `Order`/`OrderItem` — `Promotion` pozostaje
odsprzężone od agregatu `Order` (ADR-0011, granice agregatów). Kontekst buduje warstwa Application
(handler) z `order.Items` — brak zależności `Promotion → Order`. Typy są przejściowe (nie persystowane).

**Decyzja — sygnatura `CalculateDiscount`: zmiana (breaking), nie przeciążenie.**
Zastępujemy `CalculateDiscount(Money subtotal, Money deliveryFee, DateTimeOffset when, string? suppliedCode)`
przez `CalculateDiscount(OrderDiscountContext ctx)`. ADR-0011 wprost dopuścił zmianę sygnatury (API
promocji jest wewnętrzne, nie publiczne). Jedna ścieżka zamiast dwóch — wymusza podanie pełnego kontekstu
i eliminuje martwą gałąź `NotSupportedException`. `IsQualifiedFor(subtotal, when, code)` **zostaje bez
zmian** (bramki generyczne; używany też do szybkiego podglądu, gdzie pozycje nie zawsze są znane).

**Zmiana w `CreatePromotionCommandValidator`.**
- Usunąć blokadę `RuleFor(c => c.Type).NotEqual(BuyXGetY)`.
- Dodać `When(Type == BuyXGetY)`: wymagane `BuyXGetYRuleDto` (`TriggerMenuItemId != Empty`,
  `BuyQuantity ≥ 1`, `RewardMenuItemId != Empty`, `GetQuantity ≥ 1`, `RewardDiscountPercentage` w (0,100]);
  `Value` musi być `null` (kształt).
- Dla pozostałych typów: `BuyXGetYRuleDto` musi być `null`.
`CreatePromotionCommand` zyskuje pole `BuyXGetYRuleDto? BuyXGetY`. Handler mapuje na Domain VO i przekazuje
do `Promotion.Create` (rozszerzonego o opcjonalny `BuyXGetYRule? buyXGetYRule`; walidacja fabryki:
`Type == BuyXGetY` ⇒ reguła wymagana i `Value == null`; inne typy ⇒ reguła `null`).

**Wywołania `CalculateDiscount` w Application (do dostosowania).**
1. `CreateOrderCommandHandler.ApplyPromotionAsync` — buduje `OrderDiscountContext` z `order` (`Subtotal`,
   `DeliveryFee`, `clock.UtcNow`, `promotionCode`, `Lines` z `order.Items` → `MenuItemId`/`UnitPrice`/
   `Quantity`) i woła `promotion.CalculateDiscount(ctx)`. Reszta (`ApplyPromotion` + `RecordUsage`) bez zmian.
2. `ValidatePromotionCodeQueryHandler` — dziś ma tylko `subtotal`/`deliveryFee`; dla poprawnego podglądu
   BuyXGetY musi znać pozycje. Rozszerzamy `ValidatePromotionCodeQuery` o listę pozycji (`MenuItemId`,
   `UnitPrice`, `Quantity`); handler buduje `OrderDiscountContext` identycznie. Dla typów niezależnych od
   pozycji lista jest nieużywana. Jeśli `CalculateDiscount` rzuci `PromotionNotApplicableException`
   (BuyXGetY: za mało wyzwalaczy / brak nagrody) → zwraca `IsValid = false` (podgląd „nie kwalifikuje się").

**Bez nowych wyjątków domenowych.** Niekwalifikująca się BuyXGetY reużywa `PromotionNotApplicableException`
(domain-model.md 9). Guardy konfiguracji reużywają `ArgumentException`/`ArgumentOutOfRangeException`
(jak `Create`).

**Mapowanie EF Core.** `BuyXGetYRule` jako owned type (`OwnsOne`, nullable) na `Promotion` — spójnie z
ADR-0020 (VO owned). Nowa migracja dodająca kolumny reguły na tabeli `Promotions`.

**Konsekwencje / testy dla buildera.**
- Domain (`BuyXGetYRule`): guardy konstruktora (X/Y ≥ 1, pct w (0,100], Id `!= Empty`).
- Domain (`Promotion.Create`): BuyXGetY wymaga reguły i odrzuca `Value != null`; inne typy odrzucają regułę.
- Domain (`CalculateDiscount`, BuyXGetY):
  - same-product: dokładne zestawy, reszta (np. 4 szt przy 2+1 → 1 rabatowana), **wiele zestawów (stacking)**,
    wybór najtańszych sztuk przy mieszanych wariantach, `pct = 100` (gratis) i `pct < 100` (taniej);
  - za mało wyzwalaczy (`triggerUnits < X+Y`) → `PromotionNotApplicableException`;
  - cross-product: brak nagrody w koszyku → not applicable; liczba nagród ogranicza rabatowane sztuki;
  - generyczne bramki (okno/min/limit/kod) nadal odrzucają.
  - **USUNĄĆ/przepisać** istniejący test `CalculateDiscount_BuyXGetY_ThrowsNotSupportedException`;
    dostosować wszystkie testy `PromotionTests` do nowej sygnatury `CalculateDiscount(OrderDiscountContext)`.
- Application (`CreatePromotionCommandValidator`): BuyXGetY z poprawną regułą przechodzi; brak/niepoprawna
  reguła odrzucona; `Value != null` dla BuyXGetY odrzucone.
- Application (`CreateOrderCommandHandler`): stosuje BuyXGetY, kontekst zbudowany z pozycji, `Order.Total`
  odzwierciedla rabat; ścieżka not-applicable.
- Application (`ValidatePromotionCodeQueryHandler`): podgląd BuyXGetY z pozycjami; not-applicable →
  `IsValid = false`.

ADR-0011 pozostaje w historii, domknięty tą decyzją. ADR-0019 (`Type` niemutowalny) utrzymany — reguła
BuyXGetY również niemutowalna, zmiana = nowa promocja. Szczegóły modelu: domain-model.md sekcja 8.2
(źródło prawdy dla buildera).

---

## ADR-0035: Frontend — React + TypeScript (Vite) w `frontend/`, MVP katalog+koszyk, ręczne typy TS, koszyk client-side (localStorage), nazwana polityka CORS

**Data:** 2026-07-23
**Status:** Zaakceptowana

**Kontekst.**
CLAUDE.md dotąd trzymał frontend „poza zakresem na start (API-first)". Właściciel produktu zdecydował
o wejściu we frontend i ustalił (nie podlega renegocjacji): stack **React + TypeScript + Vite**; zakres
**MVP = tylko katalog + koszyk** (przeglądanie menu, budowanie koszyka). Wprost **poza** MVP: checkout
(adres/dostawa, termin, płatność), logowanie/rejestracja, live-tracking (SignalR), panel admina/obsługi —
to kolejne iteracje; teraz zostawiamy tylko miejsce (routing), nie projektujemy ich szczegółowo.

Stan Api istotny dla frontendu (zweryfikowany na kodzie):
- `GET /api/menu` i `GET /api/menu/{id:guid}` (`[AllowAnonymous]`) → `MenuItemDto[]` / `MenuItemDto`
  (`src/PizzaShop.Application/Catalog/Dtos/MenuItemDto.cs` + `MenuItemVariantDto`, `IngredientDto`,
  wspólny `MoneyDto`). Enumy serializowane jako **stringi** (`JsonStringEnumConverter` w `Program.cs`) —
  `MenuCategory` przychodzi jako `"Pizza"`/`"Drink"`/`"Side"`/`"Dessert"`/`"Sauce"`.
- `GET /api/restaurant/config` (`[AllowAnonymous]`) → `RestaurantConfigDto` (dane restauracji, godziny,
  progi) — na potrzeby nagłówka/informacji, nie krytyczny dla MVP.
- **`Program.cs` nie ma skonfigurowanego CORS** (sekcja 8: „CORS is future work… deliberately left out").
  Swagger/OpenAPI **jest** skonfigurowany (`AddSwaggerGen` + `UseSwagger`/`UseSwaggerUI` w Development).

**Decyzja.**

*(1) Umiejscowienie: `frontend/` w root repo, obok `src/`/`tests/`/`docs/`.* Osobny toolchain Node/npm,
**poza** solucją .NET (`.sln` nie referuje frontendu). Uzasadnienie: czysty rozdział światów (dotnet vs
npm), builder może pracować niezależnie, brak ryzyka, że `dotnet build`/`dotnet test` wciąga Node.

*(2) CI: na razie NIE dodajemy frontendu do `ci.yml`.* Job pozostaje `dotnet build`/`dotnet test` (CLAUDE.md).
Powód: MVP jest wczesny, nie ma jeszcze testów ani lintu frontendu, więc dokładanie kroku `npm ci && npm run
build` do CI dziś tylko wydłuża pipeline bez realnej bramki jakości i wiąże wersję Node bez potrzeby. Zostaje
**świadomy TODO** (tu, w ADR): gdy pojawią się testy/lint/typecheck frontendu → **osobny job** (`frontend`)
w `ci.yml` (albo osobny workflow) z `node-version`, `npm ci`, `npm run build`, `npm run lint`,
`npm run test` — uruchamiany warunkowo na zmiany w `frontend/**` (path filter), żeby nie blokować zmian
czysto backendowych. To przyszły, mały krok — nie robimy go teraz.

*(3) Typy/klient API: ręczne typy TS mirrorujące DTO (nie generacja z OpenAPI) — na start.* Choć Swagger
istnieje (naturalny kandydat do `openapi-typescript`), MVP dotyka **dwóch** publicznych, stabilnych
endpointów (menu + config). Ręczne, małe typy w `src/api/types.ts` (mirror `MenuItemDto` i zależnych,
`MoneyDto`, `MenuCategory` jako union stringów) to najprostsza droga bez dokładania toolchainu generacji i
zależności od uruchomionego Api w czasie builda frontendu. To ta sama filozofia, co Application mirrorujące
Domain VO ręcznymi DTO. **Punkt przełomu (zapisany, nie na zapas):** gdy powierzchnia API urośnie (checkout,
auth, admin — wiele endpointów i typów), przejść na generację `openapi-typescript` ze Swaggera (`/swagger/
v1/swagger.json`) — wtedy osobny ADR/krok. Do tego czasu ręczne typy.

*(4) Koszyk: wyłącznie client-side (localStorage + React Context/`useReducer`).* Koszyk w tym MVP **nie ma
backendu** — nie tworzymy encji/endpointu „Cart". Byłoby to sprzeczne z modelem z CLAUDE.md/Domain, gdzie
koszyk staje się `Order` dopiero przy złożeniu zamówienia (przyszła iteracja checkoutu). Koszyk żyje w stanie
aplikacji (Context + reducer) i jest utrwalany w `localStorage` (przetrwanie odświeżenia). Pozycja koszyka
referuje `menuItemId` + `variantId` (+ wybrane `extras` jako lista `ingredientId`) + `quantity`; ceny liczone
**po stronie klienta wyłącznie do wyświetlenia** — źródłem prawdy dla cen pozostaje Api (przy checkoucie
`CreateOrderCommand` przeliczy wszystko od nowa; klient-side total jest orientacyjny). Brak `AllowCredentials`
w CORS na start jest z tym spójny (MVP anonimowy, bez cookies/JWT).

*(5) CORS: nazwana polityka `"frontend"` z originami z konfiguracji — jedyna zmiana w Api w tej iteracji.*
Frontend na innym originie (dev: `http://localhost:5173` — domyślny port Vite) niż Api wymaga CORS, inaczej
przeglądarkowy `fetch` do `/api/menu` jest blokowany. Dodajemy nazwaną politykę czytającą originy z
konfiguracji (`Cors:Origins`, tablica), `AllowAnyHeader()`, `AllowAnyMethod()`, **bez `AllowCredentials`**
(MVP anonimowy — brak ciasteczek/nagłówka Authorization z frontendu; gdy dojdzie auth, poszerzymy politykę
i włączymy credentials z jawną listą originów — przyszły krok). `app.UseCors("frontend")` w potoku **przed**
`UseAuthentication`/`UseAuthorization`. To realizuje wcześniej zapowiedzianą w `api-layer.md` (linia ~452)
„nazwaną politykę pod przyszły frontend".

**Alternatywy rozważone.**
- *Generacja typów z OpenAPI od razu* — odrzucona na teraz (dwa endpointy, koszt toolchainu > zysk; wróci
  przy wzroście API, pkt 3).
- *Frontend w `src/`* — odrzucona (mieszanie światów dotnet/npm w jednym drzewie, ryzyko dla `dotnet` build/test).
- *CORS `AllowAnyOrigin()`* — odrzucona (origins z konfiguracji są bezpieczniejsze i wymagane, gdy w przyszłości
  dojdą credentials — `AllowAnyOrigin` jest wtedy niedozwolone; lepiej od razu przyzwyczaić do listy originów).
- *Koszyk na backendzie* — odrzucona (konflikt z modelem `Order`, przedwczesne; MVP client-side wystarcza).

**Konsekwencje.**
- Nowy katalog `frontend/` (Vite + React + TS) poza solucją .NET; niezależny toolchain npm. `.gitignore`
  uzupełniony o `frontend/node_modules`, `frontend/dist` (build już jest lokalnie modyfikowany — patrz status repo).
- Jedyna zmiana w kodzie Api: `Program.cs` — rejestracja `AddCors` z polityką `"frontend"` (origins z
  `Cors:Origins`) + `app.UseCors("frontend")` przed auth; sekcja `Cors:Origins` w `appsettings.Development.json`
  (`["http://localhost:5173"]`). Zastępuje komentarz „CORS is future work" realną polityką. `api-layer.md`
  sekcja 9 do zaktualizowania (CORS z „planowane" na „zaimplementowane, polityka `frontend`").
- CI bez zmian teraz; otwarty, świadomy TODO na osobny job frontendu przy pierwszych testach/lincie (pkt 2).
- Typy TS mirrorują DTO ręcznie — przy każdej zmianie kontraktu menu trzeba zaktualizować `types.ts` (koszt
  akceptowalny przy dwóch endpointach; przy wzroście → generacja, pkt 3).
- Reszta flow (checkout, auth, tracking, admin) świadomie poza MVP; routing zostawia na nie miejsce, ale bez
  implementacji — nie projektujemy ich teraz (osobne przyszłe ADR-y/iteracje).
- CLAUDE.md (sekcja „Frontend" i „Status projektu") aktualizuje właściciel — nie ruszamy go z poziomu
  architekta (analogicznie do zasady z ADR-0033: CLAUDE.md to konfiguracja projektu po stronie właściciela).

**Zakres/kroki dla buildera.**

*A. Api (jedyna zmiana backendowa):*
1. `src/PizzaShop.Api/Program.cs` — dodać przed `builder.Build()`:
   `builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
     .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
     .AllowAnyHeader().AllowAnyMethod()));`
   oraz w potoku (po `app.UseHttpsRedirection()`, **przed** `app.UseAuthentication()`): `app.UseCors("frontend");`.
   Zastąpić komentarz sekcji 8 „CORS is future work" krótkim odwołaniem do ADR-0035.
2. `src/PizzaShop.Api/appsettings.Development.json` — dodać `"Cors": { "Origins": [ "http://localhost:5173" ] }`.
3. Test integracyjny (`WebApplicationFactory`, opcjonalny lecz zalecany): preflight/`Origin` z listy dostaje
   nagłówek `Access-Control-Allow-Origin`; origin spoza listy — nie.

*B. Frontend (nowy `frontend/`):*
4. Scaffold: `npm create vite@latest frontend -- --template react-ts` (w root repo); następnie `npm install`
   w `frontend/`. Dodać `react-router-dom` (routing pod przyszłe ekrany).
5. Konfiguracja proxy dev (`vite.config.ts`): proxy `"/api"` → `http://localhost:5000` (lub aktualny origin
   Api z `launchSettings`), żeby w devie unikać CORS i mieć jeden origin; CORS z pkt A i tak potrzebny dla
   scenariusza bez proxy (np. build produkcyjny na innym hoście).
6. Struktura folderów w `frontend/src/`:
   - `api/` — `types.ts` (ręczny mirror: `Money`, `MenuCategory` union, `MenuItemVariant`, `Ingredient`,
     `MenuItem`, `RestaurantConfig`), `client.ts` (cienki `fetch` wrapper: bazowy URL, parsowanie JSON,
     błędy), `menuApi.ts` (`getMenu()`, `getMenuItem(id)`).
   - `hooks/` — `useMenu()` (fetch `/api/menu`, stan loading/error/data), `useCart()` (dostęp do kontekstu
     koszyka). Prosty fetch + `useEffect`/`useState` wystarcza; nie wprowadzać React Query w MVP (można
     dopisać później).
   - `cart/` — `CartContext.tsx` (Context + `useReducer`: akcje `add`/`remove`/`setQuantity`/`clear`),
     `cartStorage.ts` (serializacja do/z `localStorage`), typy pozycji koszyka (`menuItemId`, `variantId`,
     `extras: string[]`, `quantity`).
   - `components/` — `MenuList`, `MenuItemCard` (nazwa, opis, warianty, cena, przycisk „dodaj"),
     `VariantPicker`, `ExtrasPicker` (z `AllowedExtras`), `CartDrawer`/`CartView` (pozycje, ilości, suma
     orientacyjna, „wyczyść"), `Layout`/`Header`.
   - `pages/` — `MenuPage` (lista + koszyk), placeholder `CartPage` (jeśli osobny widok).
   - `routes.tsx` — `react-router` z trasą `/` (menu). Zostawić komentarz-miejsce na przyszłe trasy
     (`/checkout`, `/login`, `/orders/:trackingToken`) — **bez** implementacji.
7. Wołanie `/api/menu`: `menuApi.getMenu()` → `MenuItem[]`; render w `MenuList`/`MenuItemCard`. Ceny z
   `MoneyDto` (`amount`+`currency`) formatować do PLN. Pamiętać: `category` to string (union), nie liczba.
8. Koszyk w UI: dodanie pozycji z `MenuItemCard` (wybrany wariant + extras) → `dispatch(add)`; `CartView`
   pokazuje pozycje, pozwala zmienić ilość/usunąć, liczy sumę orientacyjną client-side; stan utrwalany w
   `localStorage` przez `cartStorage`. Podkreślić w kodzie (komentarz), że cena to podgląd — źródłem prawdy
   jest Api przy przyszłym checkoucie.
9. README krótkie w `frontend/` (jak uruchomić: `npm install`, `npm run dev`; gdzie ustawić URL Api).
10. `.gitignore` (root lub `frontend/`) — `frontend/node_modules/`, `frontend/dist/`.

*Poza zakresem buildera teraz:* checkout, auth, SignalR, panel admina, testy/lint frontendu w CI, generacja
typów z OpenAPI. Nie implementować — tylko zostawić miejsce w routingu (pkt 6).

---

## ADR-0036: Frontend — iteracja checkout jako gość (wizard jednostronicowy + osobna trasa potwierdzenia, mapping koszyk→CreateOrder, walidacja ręczna, obsługa ProblemDetails)

**Data:** 2026-07-23
**Status:** Zaakceptowana

**Kontekst.**
Po MVP katalog+koszyk (ADR-0035) właściciel zdecydował o kolejnej iteracji frontendu:
**checkout wyłącznie jako gość** — bez logowania/rejestracji/JWT i bez punktów lojalnościowych
(osobna, przyszła iteracja; nie projektujemy jej teraz, zostawiamy tylko miejsce w routingu,
analogicznie jak ADR-0035 zostawił miejsce na checkout). Live-tracking (SignalR) także poza
zakresem — po złożeniu zamówienia pokazujemy tylko stronę potwierdzenia z numerem i
`GuestTrackingToken`, bez realnego trackingu.

Kontrakty Api zweryfikowane na kodzie (nie z pamięci):
- `POST /api/orders/check-delivery` (`[AllowAnonymous]`) — `CheckDeliveryAvailabilityQuery(AddressDto Address)`
  → `DeliveryAvailabilityDto(bool IsAvailable, double? DistanceKm, MoneyDto? DeliveryFee)`. `DistanceKm`
  i `DeliveryFee` są `null`, gdy `IsAvailable == false`. Fee to stawka standardowa restauracji — próg
  darmowej dostawy nie jest tu jeszcze znany (subtotal koszyka nieznany na tym kroku).
- `POST /api/orders` (`[AllowAnonymous]`) — `CreateOrderCommand(ContactDetailsDto Contact,
  FulfillmentType FulfillmentType, AddressDto? DeliveryAddress, IReadOnlyList<CreateOrderItemDto> Items,
  DateTimeOffset? RequestedFulfillmentTime, PaymentMethod PaymentMethod, string? PromotionCode = null,
  int? PointsToRedeem = null)` → `CreateOrderResultDto(Guid OrderId, string Number, Guid? GuestTrackingToken,
  string? PaymentRedirectUrl)`. `GuestTrackingToken` ustawiany tylko dla gościa; `PaymentRedirectUrl`
  tylko dla `PaymentMethod.Online` (`null` dla `OnPickup`). `CustomerId` czytany po stronie handlera z
  `ICurrentUser` (dla gościa `null`) — ciało żądania nie może go sfałszować.
  - `ContactDetailsDto(string FullName, string PhoneNumber, string? Email = null)`.
  - `AddressDto(string Street, string BuildingNumber, string City, string PostalCode,
    string? ApartmentNumber = null, string? Notes = null)`.
  - `CreateOrderItemDto(Guid MenuItemId, Guid? VariantId, int Quantity,
    IReadOnlyList<Guid> ExtraIngredientIds, string? Notes = null)`.
  - `PointsToRedeem` — zawsze `null` w tym MVP (gość nie ma punktów).
  - Walidacja backendu (`CreateOrderCommandValidator`): `FullName` niepusty; `PhoneNumber` wg wzorca PL
    `^(\+48[\s-]?)?\d{3}([\s-]?\d{3}){2}$`; `Email` — format e-mail tylko gdy niepusty; `Items` niepuste,
    każdy `Quantity >= 1`; `DeliveryAddress` wymagany, gdy `FulfillmentType == Delivery`.
- `POST /api/promotions/validate` (`[AllowAnonymous]`) — `ValidatePromotionCodeQuery(string Code,
  MoneyDto Subtotal, MoneyDto DeliveryFee, IReadOnlyList<PromotionDiscountLineDto>? Lines = null)`
  → `PromotionDiscountPreviewDto(bool IsQualified, MoneyDto? DiscountAmount)` (`DiscountAmount` `null`,
  gdy `IsQualified == false`). `PromotionDiscountLineDto(Guid MenuItemId, MoneyDto UnitPrice, int Quantity)`
  potrzebne dla `BuyXGetY` (ADR-0034); dla pozostałych typów może być puste/pominięte. To wyłącznie
  **podgląd** — rzeczywisty rabat liczy `CreateOrderCommand` po stronie serwera.
- `GET /api/restaurant/config` (`[AllowAnonymous]`) → `RestaurantConfigDto(Guid Id, string Name,
  AddressDto Address, GeoCoordinateDto Location, double DeliveryRadiusKm, string TimeZoneId,
  OpeningHoursDto OpeningHours, string ContactPhone, bool IsAcceptingOrders, MoneyDto? MinimumOrderValue,
  MoneyDto? FreeDeliveryThreshold, MoneyDto DeliveryFee)`. Frontendowy `RestaurantConfig`/`OpeningHours`/
  `Address` w `types.ts` **już istnieje i jest kompletny** (ADR-0035) — nie trzeba dociągać pól.
- Enumy serializują się jako **stringi** (`JsonStringEnumConverter`, ADR-0035):
  `FulfillmentType` = `"Delivery"|"Pickup"`, `PaymentMethod` = `"Online"|"OnPickup"`,
  `DayOfWeek` jako nazwy (`"Monday"`…).
- Błędy Api → `ProblemDetails`/`ValidationProblemDetails` (`ExceptionHandler`, ADR-0027): 400 walidacja
  (`ValidationProblemDetails` z `errors: { pole: [komunikaty] }`), 404 not found, 409 konflikt stanu,
  422 naruszenie reguły domenowej (m.in. adres poza obszarem dostawy wykryty przy submicie),
  501 `NotSupported`. Ciało niesie `title` i `detail` (bezpieczne do pokazania).

Stan frontendu istotny dla iteracji: `apiClient` (`api/client.ts`) obsługuje **tylko GET**
(`apiClient.get`) — trzeba dodać `post`. `CartItem` (`cart/types.ts`) niesie `menuItemId`,
`variantId: string|null`, `extraIds: string[]`, `quantity` (+ pola displayowe i orientacyjną cenę) —
mapuje się 1:1 na `CreateOrderItemDto`. `PayU:ContinueUrl` w `appsettings.json` jest **pusty** (`""`);
w `appsettings.Development.json` brak sekcji `PayU`.

**Decyzja.**

*(1) Struktura ekranów: JEDEN komponent-wizard `pages/CheckoutPage.tsx` ze stanem kroku (nie osobne
pod-trasy `/checkout/step-x`).* Kroki 1–7 współdzielą jeden, spójny stan (tryb, adres+wynik
`check-delivery`, kontakt, termin, płatność, kod promo, podsumowanie). Osobne pod-trasy wymuszałyby
serializację/synchronizację tego stanu z URL i głębokie linkowanie w środek niedokończonego zamówienia
— nadmiarowe dla MVP. Nawigacja „dalej/wstecz" to lokalny stan (`useReducer`). **Wyjątek — potwierdzenie
jest OSOBNĄ trasą `/checkout/confirmation`**, bo musi przetrwać pełne przeładowanie strony przy powrocie
z zewnętrznej domeny PayU (redirect `ContinueUrl`), czego stan wizardu w pamięci nie przetrwa.

*(2) Kanał przekazania wyniku zamówienia do strony potwierdzenia: `sessionStorage`, jednakowy dla Online
i OnPickup.* `CreateOrderResultDto` (orderId, number, guestTrackingToken, paymentRedirectUrl) zapisujemy do
`sessionStorage` **przed** opuszczeniem SPA (przed `window.location.href = paymentRedirectUrl` dla Online;
przed `navigate('/checkout/confirmation')` dla OnPickup). `PayU:ContinueUrl` jest **statyczny** w konfiguracji
i nie zawiera `orderId` (gateway używa `_options.ContinueUrl` bez doklejania id), więc strona potwierdzenia
nie odczyta zamówienia z URL — musi z `sessionStorage`. Jeden kanał dla obu ścieżek upraszcza kod i zachowanie.
PayU może dokleić `?error=...` do `ContinueUrl` przy nieudanej/anulowanej płatności — strona potwierdzenia
czyta ten parametr i pokazuje „płatność niepotwierdzona/w toku", ale i tak wyświetla numer zamówienia +
`GuestTrackingToken` z `sessionStorage`, bo **zamówienie istnieje niezależnie od wyniku płatności**
(`PaymentStatus` ⟂ `OrderStatus`, ADR-0007); potwierdzenie płatności przychodzi asynchronicznie webhookiem.

*(3) `PayU:ContinueUrl` wskazuje na stronę potwierdzenia frontendu (zmiana tylko w konfiguracji dev).*
Ustawiamy `PayU:ContinueUrl = "http://localhost:5173/checkout/confirmation"` w `appsettings.Development.json`.
To jedyna zmiana backendowa tej iteracji i jest czysto konfiguracyjna (bez zmian w kodzie/gateway).
Produkcyjny origin ustawi się przy wdrożeniu (poza zakresem).

*(4) Mapping `CartItem` → `CreateOrderItemDto` jako czysta funkcja w module checkoutu (`checkout/mapCartToOrder.ts`),
nie w `apiClient`.* `apiClient` zostaje cienki (tylko transport). Mapping: `{ menuItemId → MenuItemId,
variantId → VariantId (null przechodzi na null/pominięcie), quantity → Quantity, extraIds → ExtraIngredientIds,
Notes: null }`. Ceny z koszyka są orientacyjne (ADR-0035) i **nie** są wysyłane — serwer przelicza wszystko.
Ten sam moduł buduje `PromotionDiscountLineDto[]` z koszyka dla podglądu promocji.

*(5) Walidacja formularzy: ręczna, spójna z resztą MVP — bez `zod`/`react-hook-form`.* `package.json` nie ma
bibliotek walidacyjnych; checkout to kilka prostych pól. Dokładamy tylko małe funkcje w `checkout/validation.ts`
mirrorujące reguły backendu (wymagany `FullName`; telefon PL tym samym wzorcem co
`CreateOrderCommandValidator`; e-mail opcjonalny, format tylko gdy podany; pola adresu wymagane dla dostawy).
Walidacja frontendu **ułatwia** (blokuje „dalej", komunikaty inline) — źródłem prawdy pozostaje
Api/Domain. Punkt przełomu (zapisany, nie na zapas): gdy dojdą złożone formularze (auth, panel admina),
rozważyć `react-hook-form`+`zod` osobnym ADR.

*(6) Termin realizacji walidowany na UI względem `RestaurantConfig.openingHours` — tylko orientacyjnie.*
Domyślnie „na teraz" (`RequestedFulfillmentTime = null`). Przy „zaplanuj" — pokazujemy okna z `openingHours`
dla wybranego dnia, blokujemy przeszłość i godziny poza oknem. Ostateczną walidację (godziny pracy, strefa
czasowa `TimeZoneId`) i tak robi Domain. Wysyłamy `DateTimeOffset` (ISO 8601 z offsetem) — nie „gołą" lokalną
godzinę. `IsAcceptingOrders == false` + „na teraz" → blokada z komunikatem (można zaplanować w godzinach pracy).

*(7) Obsługa błędów Api: rozszerzamy `ApiError` o sparsowany `ProblemDetails`.* `apiClient.post` przy `!response.ok`
parsuje ciało jako `problem+json`: dla 400 `ValidationProblemDetails` wyciąga `errors` (mapa pole→komunikaty) →
błędy inline przy polach; dla 422/409/404/501 pokazuje `title`+`detail` jako baner na kroku podsumowania.
Scenariusz szczególny: adres poza obszarem dostawy przy submicie mimo wcześniejszego `check-delivery` (race) →
422 → baner + akcja „wróć do wyboru odbioru osobistego". Minimalny próg zamówienia (`MinimumOrderValue`) i próg
darmowej dostawy (`FreeDeliveryThreshold`) pokazujemy orientacyjnie i blokujemy submit poniżej progu — backend
egzekwuje to twardo.

*(8) Routing: dodajemy realne `/checkout` i `/checkout/confirmation`; `/login` i `/orders/:trackingToken`
zostają jako przyszłe.* Z listy TODO-placeholderów w `routes.tsx` usuwamy tylko `/checkout`.

**Alternatywy rozważone.**
- *Pod-trasy per krok (`/checkout/mode`, `/checkout/address`…)* — odrzucone: koszt synchronizacji stanu z URL i
  deep-linki w środek niedokończonego zamówienia bez wartości dla MVP.
- *Przekazanie wyniku zamówienia przez React Router `navigate(state)`* — odrzucone jako jedyny kanał: nie
  przetrwa zewnętrznego redirectu PayU (pełne przeładowanie z innej domeny). `sessionStorage` działa dla obu ścieżek.
- *Dynamiczny `ContinueUrl` per zamówienie (z `orderId`)* — odrzucone teraz: wymaga zmiany gateway/kontraktu
  (backend), a `sessionStorage` rozwiązuje problem bez ruszania backendu. Do rozważenia, gdyby doszedł
  wieloetapowy powrót/deep-link po płatności.
- *`zod`+`react-hook-form`* — odrzucone na teraz (rozdmuchanie zależności dla kilku pól; wróci przy auth/adminie).
- *Generacja typów z OpenAPI* — nadal odłożone (ADR-0035 pkt 3); dokładamy ręczne typy checkoutu. Powierzchnia
  API rośnie — to sygnał do przyszłej migracji na `openapi-typescript` (osobny ADR).

**Konsekwencje.**
- Rośnie ręczny mirror typów (`api/types.ts`): dochodzą `FulfillmentType`, `PaymentMethod`, `ContactDetails`,
  `CreateOrderItem`, `CreateOrderCommand`, `CreateOrderResult`, `DeliveryAvailability`, `ValidatePromotionCode`
  (request), `PromotionDiscountPreview`, `PromotionDiscountLine`. Każda zmiana kontraktu Orders/Promotions/Restaurant
  wymaga aktualizacji `types.ts` — koszt akceptowalny, ale zbliża moment migracji na generację (pkt wyżej).
- `apiClient` przestaje być GET-only: dochodzi `post` + bogatszy `ApiError` (status + `title`/`detail`/`errors`).
  Istniejące wywołania GET bez zmian.
- Jedyna zmiana backendowa: `PayU:ContinueUrl` w `appsettings.Development.json` (`http://localhost:5173/checkout/confirmation`).
  Bez zmian w kodzie Api/gateway. Produkcyjny origin — przy wdrożeniu.
- Koszyk po udanym złożeniu zamówienia jest czyszczony (`clear()`), ale dopiero po zapisaniu wyniku do
  `sessionStorage` i (dla Online) tuż przed redirectem — żeby powrót „wstecz" z PayU nie zostawił pustego koszyka
  bez potwierdzenia. Szczegóły w planie buildera.
- Punkty lojalnościowe, logowanie, live-tracking, panel obsługi — nadal poza zakresem; routing zostawia miejsce,
  nie implementujemy (przyszłe ADR-y/iteracje). `PointsToRedeem` zawsze `null`.
- CI bez zmian (spójne z ADR-0035 pkt 2; testy/lint frontendu = przyszły osobny job).

**Zakres/kroki dla buildera.**

*B0. Domknięcie logu decyzji:* wykonane przez architekta bezpośrednio w `docs/decisions.md` (ten wpis) — brak
osobnego kroku dla buildera.

*B1. Backend (jedyna zmiana, konfiguracja):*
1. `src/PizzaShop.Api/appsettings.Development.json` — dodać sekcję
   `"PayU": { "ContinueUrl": "http://localhost:5173/checkout/confirmation" }`. Bez zmian w kodzie.

*B2. Warstwa API frontendu (`frontend/src/api/`):*
2. `api/client.ts` — dodać `post<T>(path, body): Promise<T>` (`fetch` z `method: 'POST'`,
   `headers: { 'Content-Type': 'application/json' }`, `body: JSON.stringify(body)`). Rozszerzyć `ApiError`
   o `title?: string`, `detail?: string`, `errors?: Record<string, string[]>`; przy `!ok` próbować sparsować
   ciało jako JSON `problem+json` i wypełnić te pola (fallback do dotychczasowego zachowania, gdy nie-JSON).
3. `api/types.ts` — dodać mirror-typy: `FulfillmentType = 'Delivery' | 'Pickup'`,
   `PaymentMethod = 'Online' | 'OnPickup'`, `ContactDetails { fullName; phoneNumber; email: string | null }`,
   `CreateOrderItem { menuItemId; variantId: string | null; quantity; extraIngredientIds: string[];
   notes: string | null }`, `CreateOrderCommand { contact; fulfillmentType; deliveryAddress: Address | null;
   items: CreateOrderItem[]; requestedFulfillmentTime: string | null; paymentMethod; promotionCode: string | null;
   pointsToRedeem: number | null }`, `CreateOrderResult { orderId; number; guestTrackingToken: string | null;
   paymentRedirectUrl: string | null }`, `DeliveryAvailability { isAvailable; distanceKm: number | null;
   deliveryFee: Money | null }`, `PromotionDiscountLine { menuItemId; unitPrice: Money; quantity }`,
   `ValidatePromotionCodeRequest { code; subtotal: Money; deliveryFee: Money; lines: PromotionDiscountLine[] }`,
   `PromotionDiscountPreview { isQualified; discountAmount: Money | null }`. (Komentarze mirrorujące ścieżki C#,
   jak reszta pliku.)
4. `api/ordersApi.ts` — `checkDelivery(address: Address): Promise<DeliveryAvailability>`
   (`post('/orders/check-delivery', { address })`) oraz `createOrder(cmd: CreateOrderCommand): Promise<CreateOrderResult>`
   (`post('/orders', cmd)`).
5. `api/promotionsApi.ts` — `validatePromotion(req: ValidatePromotionCodeRequest): Promise<PromotionDiscountPreview>`
   (`post('/promotions/validate', req)`).
6. `api/restaurantApi.ts` — `getRestaurantConfig(): Promise<RestaurantConfig>` (`get('/restaurant/config')`).

*B3. Moduł checkoutu (`frontend/src/checkout/`):*
7. `checkout/mapCartToOrder.ts` — czyste funkcje:
   `cartItemsToOrderItems(items: CartItem[]): CreateOrderItem[]` (mapping z pkt Decyzja 4, `notes: null`,
   `variantId` `null`→`null`) oraz `cartItemsToPromotionLines(items: CartItem[]): PromotionDiscountLine[]`
   (`{ menuItemId, unitPrice: { amount: unitPriceAmount, currency }, quantity }`). Komentarz: ceny orientacyjne,
   serwer przelicza.
8. `checkout/validation.ts` — walidatory pól zwracające komunikat lub `null`: `validateFullName`,
   `validatePhoneNumber` (ten sam regex PL co backend), `validateEmailOptional`, `validateAddress`
   (Street/BuildingNumber/City/PostalCode wymagane; PostalCode w formacie PL `NN-NNN` — miękko, jeśli backend
   nie wymaga ostrzej). Eksport agregujący błędy per krok.
9. `checkout/openingHours.ts` — helper: `getWindowsForDate(config, date): TimeRange[]`, `isWithinOpeningHours(config, when): boolean`,
   `isInPast(when): boolean`. Do UI kroku terminu; ostateczna walidacja po stronie Domain.
10. `checkout/orderResultStorage.ts` — `saveOrderResult(r: CreateOrderResult)` / `loadOrderResult(): CreateOrderResult | null`
    / `clearOrderResult()` na `sessionStorage` (klucz np. `pizzashop.lastOrder`).
11. `checkout/checkoutState.ts` — typ stanu wizardu (`step`, `fulfillmentType`, `address`, `deliveryCheck`,
    `contact`, `schedule: { mode: 'now'|'scheduled'; at: string | null }`, `paymentMethod`, `promotionCode`,
    `promotionPreview`) + `useReducer` (akcje: `setFulfillment`, `setAddress`, `setDeliveryCheck`, `setContact`,
    `setSchedule`, `setPayment`, `setPromotion`, `goNext`, `goBack`).

*B4. Komponenty i strony:*
12. `pages/CheckoutPage.tsx` — wizard sterujący krokami, guard: pusty koszyk → redirect do `/cart` (lub `/`).
    Ładuje `RestaurantConfig` na wejściu (loading/error). Renderuje krok wg stanu:
    - Krok 1 `FulfillmentStep` — wybór `Delivery`/`Pickup`.
    - Krok 2 `DeliveryAddressStep` (tylko `Delivery`) — formularz `Address` → `checkDelivery` →
      jeśli `isAvailable == false`: komunikat + przycisk „wybierz odbiór osobisty" (cofa do kroku 1 z ustawionym
      `Pickup`); jeśli `true`: zapamiętać `deliveryFee`/`distanceKm`, „dalej". Dla `Pickup` krok pomijany całkowicie.
    - Krok 3 `ContactStep` — `ContactDetails` (FullName, PhoneNumber wymagane; Email opcjonalny) + walidacja ręczna.
    - Krok 4 `FulfillmentTimeStep` — radio „na teraz" / „zaplanuj"; przy „zaplanuj" picker ograniczony
      `openingHours` wybranego dnia, blokada przeszłości; wynik jako ISO `DateTimeOffset` lub `null`.
    - Krok 5 `PaymentStep` — wybór `Online`/`OnPickup`.
    - Krok 6 `PromotionField` (może być częścią podsumowania) — pole kodu + „sprawdź" → `validatePromotion`
      (subtotal = orientacyjny total koszyka; deliveryFee = z `deliveryCheck` dla dostawy albo `0` dla odbioru;
      lines = `cartItemsToPromotionLines`); pokazać `isQualified`/`discountAmount` informacyjnie.
    - Krok 7 `OrderSummary` — pozycje, orientacyjny subtotal, dostawa (info o `FreeDeliveryThreshold`), podgląd
      rabatu, dane kontaktowe/adres/termin/płatność; guard `MinimumOrderValue`; przycisk „Zamawiam".
13. Submit w `CheckoutPage`: zbudować `CreateOrderCommand` (`items = cartItemsToOrderItems`,
    `deliveryAddress` tylko dla `Delivery`, `requestedFulfillmentTime` z kroku 4, `promotionCode` lub `null`,
    `pointsToRedeem: null`) → `createOrder`. Po sukcesie: `saveOrderResult(result)`, `clear()` koszyka, a następnie:
    - `Online` (jest `paymentRedirectUrl`): `window.location.href = result.paymentRedirectUrl`.
    - `OnPickup`: `navigate('/checkout/confirmation')`.
    Obsłużyć `ApiError`: 400 → błędy inline/baner z `errors`; 422/409 → baner z `detail` (adres poza obszarem →
    dodatkowo akcja powrotu do wyboru odbioru); inne → baner ogólny.
14. `pages/OrderConfirmationPage.tsx` (trasa `/checkout/confirmation`) — `loadOrderResult()`; jeśli brak →
    komunikat + link do menu. Jeśli jest: pokazać `Number`, `GuestTrackingToken` (jako informację/nieodgadnalny
    identyfikator śledzenia — bez realnego trackingu SignalR w tej iteracji, ADR-0028/0031), odczytać `?error`
    z query (PayU) i przy jego obecności pokazać „płatność niepotwierdzona/w toku" (bez blokowania widoku
    zamówienia). Link „śledź zamówienie" może na razie prowadzić do placeholderu/`/orders/:trackingToken` (trasa
    przyszła) lub być nieaktywny z adnotacją.
15. `components/checkout/` — komponenty kroków z pkt 12 (`FulfillmentStep`, `DeliveryAddressStep`, `ContactStep`,
    `FulfillmentTimeStep`, `PaymentStep`, `PromotionField`, `OrderSummary`) + opcjonalny `CheckoutStepper`
    (pasek postępu). Trzymać spójny, prosty styl z istniejącymi klasami CSS (`cart-*`, `empty-state` itd.).
16. `components/CartView.tsx` (lub `pages/CartPage.tsx`) — dodać przycisk/link „Przejdź do kasy" → `/checkout`
    (widoczny, gdy koszyk niepusty).
17. `routes.tsx` — dodać `<Route path="/checkout" element={<CheckoutPage />} />` i
    `<Route path="/checkout/confirmation" element={<OrderConfirmationPage />} />`; z komentarza-TODO usunąć
    tylko `/checkout` (zostawić `/login`, `/orders/:trackingToken`).

*Poza zakresem buildera teraz:* logowanie/rejestracja, punkty lojalnościowe (`PointsToRedeem` zawsze `null`),
live-tracking SignalR, panel obsługi/admina, testy/lint frontendu w CI, generacja typów z OpenAPI, dynamiczny
`ContinueUrl` per zamówienie. Nie implementować — zostawić miejsce w routingu (pkt 17).
