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
**Status:** Zaakceptowana (celowo niedookreślona co do przelicznika)

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
**Status:** Zaakceptowana (świadome odłożenie)

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
**Status:** Zaakceptowana (implementacja reguł świadomie odłożona — ADR-0009)

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
  (`LinearLoyaltyPolicy` placeholder, ADR-0014).
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
