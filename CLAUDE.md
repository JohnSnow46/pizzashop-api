# PizzaShop — kontekst projektu dla Claude Code

## Czym jest ten projekt
Aplikacja e-commerce do zamawiania pizzy online: katalog produktów (pizze, dodatki, promocje),
koszyk, składanie zamówień, płatności, panel klienta, panel administratora/restauracji,
śledzenie statusu zamówienia w czasie rzeczywistym.

## Stack technologiczny
- **.NET 8**, C#
- **ASP.NET Core Web API** (kontrolery, minimal API tam gdzie to sensowne)
- **Entity Framework Core** + **PostgreSQL**
- **Clean Architecture**: warstwy `Domain`, `Application`, `Infrastructure`, `Api`
- **JWT** do autentykacji/autoryzacji (role: Customer, Employee, RestaurantAdmin, SuperAdmin — patrz "Role i uprawnienia")
- **SignalR** do live-trackingu statusu zamówienia — dokładny graf stanów `OrderStatus`
  jest jednym źródłem prawdy w `docs/domain-model.md` (sekcja 5.3), nie duplikować go tutaj
- **Płatności: PayU** — integracja przez `IPaymentGateway` (interfejs w `Application`,
  implementacja w `Infrastructure`). Na dev/test używamy środowiska **PayU Sandbox**
  (osobne konto testowe, testowe transakcje, przełączenie na produkcję to tylko zmiana
  kluczy API w konfiguracji). Endpoint webhooka/notyfikacji PayU w `Api` **nie może** być
  chroniony JWT (wywołuje go PayU, nie zalogowany użytkownik) — musi zamiast tego
  weryfikować podpis/sekcję żądania zgodnie z dokumentacją PayU, inaczej jest podatny na
  sfałszowane potwierdzenia płatności
- **xUnit + Moq** do testów jednostkowych, **FluentAssertions** jeśli potrzebne
- Frontend: poza zakresem na start (API-first). Jeśli dojdzie frontend, ustalimy osobno.

## Zakres biznesowy
Jedna pizzeria, jedna lokalizacja (nie multi-tenant). Rola `RestaurantAdmin` to rola
operacyjna dla personelu tej jednej restauracji — nie zakładamy modelu wielu niezależnych
restauracji na wspólnej platformie. Nie projektuj encji/relacji pod multi-tenancy
(np. `RestaurantId` jako partycjonowanie danych) bez wyraźnej decyzji zmieniającej ten zakres.

### Role i uprawnienia
- **Customer** — klient. Przegląda menu, składa zamówienia (jako gość albo zalogowany),
  śledzi status własnego zamówienia, zbiera i wydaje punkty lojalnościowe (tylko z kontem).
- **Employee** (pracownik) — obsługa kuchni/dostawy. Widzi kolejkę przychodzących zamówień,
  zmienia status realizacji (Kitchen → Delivery/Ready for pickup → Delivered/Collected),
  ustawia `EstimatedReadyAt`. Brak dostępu do zarządzania menu, cenami, promocjami czy
  obszarem dostawy.
- **RestaurantAdmin** — admin restauracji. Wszystko co `Employee`, plus zarządzanie menu
  (pizze, dodatki, ceny), promocjami, obszarem dostawy (promień), godzinami pracy oraz
  zarządzanie kontami `Employee`.
- **SuperAdmin** — rola techniczna/właściciela platformy, ponad `RestaurantAdmin` (np.
  zarządzanie kontami adminów, pełny dostęp do wszystkiego). Utrzymywana na wypadek
  rozrostu do wielu lokalizacji w przyszłości — nie projektować pod nią żadnej logiki
  multi-tenant już teraz (patrz wyżej), to czysto rola uprawnień.

**Konwencja autoryzacji:** role są rozłącznymi wartościami na koncie (JWT zawiera jedną
rolę), ale uprawnieniami są hierarchiczne (`SuperAdmin` ⊇ `RestaurantAdmin` ⊇ `Employee`).
Każdy endpoint wymagający roli `Employee` musi jawnie dopuszczać też `RestaurantAdmin` i
`SuperAdmin` w `[Authorize(Roles = "Employee,RestaurantAdmin,SuperAdmin")]` (analogicznie
endpointy `RestaurantAdmin` dopuszczają też `SuperAdmin`) — hierarchię egzekwuje warstwa
autoryzacji w `Api`, nie sam token ani `Domain`.

### Flow zamówienia
1. **Tryb realizacji** — klient na starcie wybiera **dostawę** albo **odbiór osobisty**
   w lokalu.
2. **Adres (tylko dla dostawy)** — przy trybie "dostawa" klient podaje lub geolokalizuje
   adres i system od razu sprawdza, czy mieści się w obsługiwanym obszarze dostawy (patrz
   niżej) — jeśli nie, zamówienie z dostawą nie jest możliwe (klient może cofnąć się do
   kroku 1 i wybrać odbiór osobisty). Przy odbiorze osobistym ten krok jest całkowicie
   pomijany — adres nie jest zbierany.
3. **Koszyk i zamówienie bez/z kontem** — klient może dodać pizze do koszyka i złożyć
   zamówienie jako gość (bez rejestracji) albo zalogowany (z kontem). Punkty lojalnościowe
   nalicza się tylko przy zamówieniach z kontem. Zamówienie gościa musi mieć bezpieczny,
   nieodgadnalny identyfikator do śledzenia statusu (np. GUID w URL, nie sekwencyjne ID) —
   gość nie ma JWT, więc dostęp do podglądu statusu nie może opierać się na autoryzacji roli.
4. **Termin realizacji** — klient może zamówić "na teraz" albo zaplanować zamówienie na
   wybraną godzinę (nawet jeśli restauracja jest aktualnie zamknięta), o ile mieści się to
   w godzinach pracy restauracji dla wybranego dnia. `Order` przechowuje
   `RequestedFulfillmentTime`.
5. **Płatność** — klient wybiera płatność **online (PayU)** albo **przy odbiorze/dostawie**
   (gotówka/karta kurierowi/w lokalu). Status płatności (`PaymentStatus`) jest niezależny od
   statusu realizacji zamówienia (`OrderStatus`, patrz `docs/domain-model.md` sekcja 5.3) —
   zamówienie z płatnością "przy odbiorze" trafia do kuchni od razu, zamówienie płatne
   online czeka z przyjęciem do kuchni na potwierdzenie płatności.

### Obszar dostawy
Admin definiuje obszar dostawy jako **promień (w km) od adresu restauracji**. Walidacja
adresu klienta = odległość w linii prostej (lub przez API mapowe, do ustalenia przez
architekta) od punktu restauracji ≤ skonfigurowany promień. Jeden promień na start; wiele
stref z różnymi opłatami za dostawę to możliwe rozszerzenie, nie projektować na zapas bez
wyraźnej potrzeby.

### Punkty lojalnościowe
Klienci z kontem zbierają punkty za zamówienia i mogą nimi zapłacić (częściowo lub
całościowo) za kolejne zamówienie. Dokładny przelicznik (zł → punkty) i reguła wymiany
(rabat kwotowy vs. konkretne nagrody) **nie są jeszcze ustalone** — architekt ma zaprojektować
elastyczny szkielet (np. `LoyaltyAccount` + historia transakcji punktowych jako osobna
encja/value object w `Domain`), tak żeby konkretną regułę naliczania/wymiany dało się
dostosować później bez przebudowy modelu.

### Szacowany czas realizacji (ustawiany przez obsługę)
Po złożeniu zamówienia obsługa restauracji (rola `Employee`, ewentualnie `RestaurantAdmin`) ustawia
**szacowany czas, po którym pizza będzie gotowa/dostarczona** (`EstimatedReadyAt` lub
podobne pole na `Order`). To odrębna wartość od `RequestedFulfillmentTime` (życzenia klienta
przy składaniu zamówienia) — jest ustawiana/aktualizowana przez personel już po przyjęciu
zamówienia do realizacji i powinna być widoczna dla klienta przez live-tracking (SignalR).

## CI
- **GitHub Actions**: build + testy (`dotnet build`, `dotnet test`) uruchamiane przy każdym
  PR i push do głównej gałęzi. Docelowy hosting jeszcze nie ustalony — nie zakładaj
  konkretnej platformy (Azure/AWS/VPS) w kodzie czy konfiguracji bez wyraźnej decyzji.

## Struktura repozytorium (docelowa)
```
src/
  PizzaShop.Domain/           # encje, value objecty, reguły biznesowe, brak zależności na zewnątrz
  PizzaShop.Application/      # use case'y (CQRS: Commands/Queries), interfejsy, DTO
  PizzaShop.Infrastructure/   # EF Core, repozytoria, integracje zewnętrzne (płatności, e-mail)
  PizzaShop.Api/              # kontrolery, middleware, konfiguracja DI, SignalR huby
tests/
  PizzaShop.Domain.Tests/
  PizzaShop.Application.Tests/
  PizzaShop.Api.Tests/
docs/
  decisions.md                 # log decyzji architektonicznych (ADR-lite)
  domain-model.md               # opis modelu domenowego
```

## Reguły pracy z agentami
Ten projekt korzysta z trzech subagentów zdefiniowanych w `.claude/agents/`:

- **architect** — projektuje, aktualizuje `docs/decisions.md` i `docs/domain-model.md`, NIE pisze kodu produkcyjnego. Wywołuj go na start nowej funkcjonalności lub gdy trzeba podjąć decyzję strukturalną.
- **builder** — implementuje kod zgodnie z ustaleniami architekta i konwencjami poniżej. Pisze też testy do własnego kodu.
- **reviewer** — czyta diff/kod, sprawdza zgodność z Clean Architecture, konwencjami, testami. NIE modyfikuje kodu — tylko raportuje uwagi.

Typowy przepływ dla nowej funkcjonalności: `architect` (projekt) → `builder` (implementacja) → `reviewer` (przegląd) → poprawki przez `builder`.

## Konwencje kodu
- Nullable reference types: włączone (`<Nullable>enable</Nullable>`)
- Async wszędzie tam, gdzie jest I/O (`async`/`await`, `CancellationToken` przekazywany dalej)
- CQRS w warstwie Application: `Commands/`, `Queries/`, każdy handler w osobnym pliku
- Walidacja wejścia: **FluentValidation w `Application`** dla walidacji DTO/requestów
  (kształt danych, wymagane pola, formaty). Reguły biznesowe zależne od stanu (np. adres w
  promieniu dostawy, godziny pracy, przejścia statusów) żyją jako guard clauses/metody w
  `Domain`, nie w walidatorach — walidator sprawdza "czy dane są poprawnej postaci",
  Domain pilnuje "czy operacja jest dozwolona"
- Wyjątki domenowe dziedziczą po `DomainException`, mapowane na odpowiednie kody HTTP w middleware
- Repozytoria przez interfejsy w `Application`, implementacje w `Infrastructure`
- Każdy nowy use case (Command/Query) ma odpowiadający test jednostkowy

## Konwencje nazewnictwa
- Encje: liczba pojedyncza (`Pizza`, `Order`, `OrderItem`)
- Commands: `CreateOrderCommand`, `AddPizzaToCartCommand` (czasownik + rzeczownik + `Command`)
- Queries: `GetOrderByIdQuery`, `GetMenuQuery`
- Testy: `MethodName_Scenario_ExpectedResult`

## Komendy
```bash
dotnet build
dotnet test
dotnet ef migrations add <Nazwa> -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet ef database update -p src/PizzaShop.Infrastructure -s src/PizzaShop.Api
dotnet run --project src/PizzaShop.Api
```

## Środowisko do przeglądania kodu
VS Code (zainstalowany 2026-07-20) + rozszerzenia: C# Dev Kit/C#, ESLint, Prettier,
GitLens, Git Graph, Error Lens, Todo Tree, Better Comments, Code Spell Checker,
indent-rainbow, Material Icon Theme, EditorConfig. Bez integracji GitHub/GitLab PR —
przegląd lokalny (diff, blame, historia).

**Flow przeglądu zmian od agentów:**
1. Source Control (`Ctrl+Shift+G`) — diff inline zmienionych plików.
2. Error Lens — od razu widać błędy kompilacji C# / warningi ESLint przy linii.
3. `Shift+F12` (Find All References) na zmienionych publicznych metodach — sprawdzić, czy
   zmiana w `Domain` nie psuje `Application`/`Api`.
4. Todo Tree — sprawdzić niedomknięte `TODO`/`FIXME` (konwencja: `TODO(architect):` przy
   miejscach wymagających decyzji architektonicznej).
5. Git Graph — podejrzeć historię/branch przed zatwierdzeniem.

Inne skróty: `Ctrl+T` szybki skok do klasy, `F12`/`Alt+F12` go to/peek definition,
GitLens blame inline przy najechaniu na linię.

## Status projektu
`docs/decisions.md` (ADR-lite, 29 wpisów), `docs/domain-model.md`, `docs/api-layer.md` i
`docs/infrastructure-layer.md` są aktualnym źródłem prawdy o modelu i warstwach; ten plik
opisuje tylko ogólny zakres i konwencje, nie duplikuj z niego szczegółów.

Zaimplementowane i zbudowane (0 błędów kompilacji):
- **Domain** — pełny model (VO, wyjątki, enumy, encje katalogu, `Restaurant`, `Order`/
  `OrderItem`, `Customer`/`LoyaltyAccount`, `Promotion`), pełne pokrycie testami.
- **Application** — CQRS use case'y, porty, DTO, walidatory FluentValidation.
- **Infrastructure** — EF Core + PostgreSQL (konfiguracje mapowania, 3 migracje), 7
  repozytoriów + `UnitOfWork`, `PayUPaymentGateway`, `NominatimGeocodingService`, testy
  integracyjne na Testcontainers.
- **Api — Iteracja 1** (tożsamość + JWT): `UserAccount` (Application/Identity), BCrypt,
  `RegisterCustomerCommand`/`LoginCommand`/`RegisterStaffAccountCommand`, `JwtTokenGenerator`,
  `HttpContextCurrentUser`, globalny middleware wyjątków → `ProblemDetails`, `AuthController`
  (`/register`, `/login`, `/staff`, `/me`), `DbSeeder` (bootstrap `SuperAdmin`).

Ważna decyzja po drodze: **ADR-0029** — powiązanie `Customer`↔`LoyaltyAccount` jest
jednokierunkowe (`LoyaltyAccount.CustomerId` jedyny nośnik FK; `Customer.LoyaltyAccountId`
usunięty, migracja `DropCustomerLoyaltyAccountId`). Wzorzec wiążący na przyszłość: relacje 1:1
między agregatami — FK po stronie zależnej, bez opcjonalnych `id` w fabrykach do uzgadniania
tożsamości między agregatami.

Zaprojektowane, jeszcze niezaimplementowane — Api Iteracje 2–4 (`docs/api-layer.md` sekcja 10):
- **Iteracja 2** (w toku — patrz TodoWrite/ostatni commit) — `MenuController`,
  `IngredientsController`, `RestaurantController`, `PromotionsController` (`docs/api-layer.md`
  sekcje 6.2–6.5): endpointy odczytu publiczne + admin, testy `WebApplicationFactory` na
  autoryzacji i mapowaniu wyjątków.
- **Iteracja 3** — `OrdersController`, `PaymentsController` (webhook PayU surowe body, bez JWT).
- **Iteracja 4** — SignalR (`OrderTrackingHub`, `SignalROrderNotifier`), `LoyaltyController`.
