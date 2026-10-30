# Roadmap — dalszy rozwój

MVP jest kompletny: katalog z wariantami/dodatkami, koszyk→zamówienie, płatności PayU
(w tym checkout gościa i retry), role (Customer/Employee/RestaurantAdmin/SuperAdmin),
program lojalnościowy, promocje (w tym BuyXGetY), live-tracking statusu (SignalR),
frontend React. Pełna historia decyzji: `docs/decisions.md` (41 ADR). Ten plik to lista
konkretnych, realistycznych kierunków dalszego rozwoju — nie lista życzeń — wywiedzionych
z rzeczy świadomie odłożonych w istniejących ADR-ach i notatkach projektowych
(`docs/domain-model.md` §10), a nie z generycznych pomysłów.

## Propozycje

1. **Rozszerzenie reguł BuyXGetY** — obecna implementacja (ADR-0011/ADR-0034) celowo
   ogranicza się do konkretnego produktu jako wyzwalacza/nagrody, bez auto-dodawania
   darmowego produktu do koszyka i bez edycji już utworzonej reguły
   (`domain-model.md` §10). Naturalne domknięcie zaplanowanej ścieżki: wyzwalacz per
   kategoria, auto-dołożenie nagrody, `UpdateRule`. Zakres: **średni**. **Wymaga nowego
   ADR** (rozszerza ADR-0011/0034).

2. ~~**Trwały koszyk / odzyskiwanie porzuconych koszyków**~~ — **Slice 1 zrobiony
   (ADR-0043, 2026-09-30).** Nowy agregat `Cart`/`CartItem`: tylko zalogowani klienci,
   bez snapshotów cen (referencje jak dzisiejszy koszyk client-side), merge pozycji po
   (MenuItemId, VariantId, ExtraIds). Backend kompletny (Domain+Application+
   Infrastructure+Api, `GET/POST/PATCH/DELETE /api/cart[...]`) + testy. **Świadomie
   NIE w tym slice:** TTL/auto-czyszczenie starych koszyków (wymaga infrastruktury
   zadań w tle, której repo nie ma), integracja frontendu (`CartContext` nadal używa
   `localStorage` — przełączenie na ten backend to osobne zadanie), scalanie koszyka
   gościa po zalogowaniu.

3. **Hardening retry płatności gościa** — ADR-0041 wprost odkłada rate-limiting i
   jednorazowość `GuestTrackingToken` przy retry płatności PayU ("bez rate-limitingu/
   jednorazowości tokenu w tej iteracji"). To zamknięcie już zidentyfikowanej luki
   bezpieczeństwa (odziedziczonej z ADR-0018), nie nowy pomysł. Zakres: **mały-średni**.
   **Wymaga nowego ADR** (dotyka bezpieczeństwa/płatności wprost, zgodnie z tabelą
   trybów w `CLAUDE.md`).

4. **Druga metoda płatności obok PayU** (np. BLIK bezpośrednio przez PSP inny niż PayU,
   lub Stripe) — port `IPaymentGateway` (ADR-0013) już abstrahuje dostawcę, więc druga
   implementacja to naturalny test tej abstrakcji zamiast przebudowy. Dobrze pokazuje
   wzorzec strategii na żywym przykładzie. Zakres: **średni**. **Wymaga nowego ADR**
   (wybór dostawcy, routing wyboru metody w checkout).

5. ~~**Oceny i recenzje zamówień/produktów**~~ — **zrobione (ADR-0042, 2026-09-28).** Nowy
   agregat `Review`: rejestrowany klient ocenia (1-5, opcjonalny komentarz) pozycję menu z
   własnego, `Completed` zamówienia; jedna recenzja per (Order, MenuItem). Backend
   kompletny (Domain+Application+Infrastructure+Api+33 testy) — `POST /api/reviews`,
   `GET /api/reviews/mine`, `GET /api/menu-items/{id}/reviews`. Frontend świadomie poza
   zakresem tego ADR (backend-first, jak ADR-0039).

6. ~~**Panel raportowy dla RestaurantAdmin**~~ — **już zrobione.** `frontend/src/pages/AdminReportsPage.tsx`
   już implementuje pełny dashboard na `GetSalesReportQuery` (filtry zakresu dat i top-N,
   liczba zamówień, przychód, tabela najlepiej sprzedających się pozycji, eksport CSV
   z escapowaniem przed CSV injection). Ta pozycja była nieaktualna w chwili spisania
   tego pliku — zweryfikowano 2026-09-25.

7. ~~**Asynchroniczne powiadomienia dla gościa**~~ — **już zrobione.** `IEmailSender`
   (Application/Abstractions/Email, `LoggingEmailSender` w Infrastructure) już istnieje
   i jest wywoływany z każdego handlera zmieniającego status zamówienia
   (`CreateOrderCommandHandler` → `SendOrderConfirmationEmailAsync`;
   `AcceptOrderCommandHandler`/`RejectOrderCommandHandler`/`MarkReadyCommandHandler`/
   `StartDeliveryCommandHandler`/`CompleteOrderCommandHandler`/`CancelOrderCommandHandler`
   → `SendOrderStatusChangedEmailAsync`), każde wywołanie opakowane w try/catch żeby
   błąd wysyłki nie psuł transakcji. Ta pozycja była nieaktualna w chwili spisania tego
   pliku — zweryfikowano 2026-09-30. Realny dostawca SMTP/SendGrid zamiast
   `LoggingEmailSender` to osobna, mniejsza decyzja (podmiana jednej implementacji
   portu), nie warta nowego ADR sama w sobie.

## Notatka 2026-09-28

User zatwierdził realizację pozycji wcześniej oznaczonych "wymaga nowego ADR". Dziś
zrealizowano wyłącznie poz. 5 (Reviews, ADR-0042) — solidnie, z pełnym testem. Poz. 1
(rozszerzenie BuyXGetY) świadomie NIE ruszona dziś: po ponownej analizie w trakcie pisania
ADR okazało się, że część żądanego zakresu ("edycja reguły") wprost koliduje z istniejącą,
świadomą decyzją `domain-model.md` §8.1 ("Reguła BuyXGetY... jest niemutowalna po
utworzeniu... zmiana = nowa promocja") — wymaga to osobnej decyzji (czy odwracać tamtą
decyzję), nie mieści się w prostym "rozszerz o kategorię". Poz. 2 (trwały koszyk) i 4
(druga metoda płatności) pozostają nietknięte — największy zakres/ryzyko z zatwierdzonej
listy, celowo zostawione na osobne dni. Poz. 3 (hardening retry płatności) i 7
(powiadomienia e-mail) też jeszcze nie zrobione — kolejność z braku czasu, nie z powodu
problemów.

## Notatka 2026-09-30

Zrealizowano poz. 2 (Cart, Slice 1, ADR-0043) i skorygowano poz. 7 (powiadomienia e-mail —
okazała się już zaimplementowana, nie nowa funkcja). Poz. 3 (hardening retry płatności) i 4
(druga metoda płatności) świadomie NIE ruszone: próba ich zlecenia w tej samej turze
została zablokowana przez klasyfikator bezpieczeństwa sesji (prawdopodobnie ze względu na
wrażliwość kodu płatniczego) — obie wymagają bezpośredniej realizacji przez usera, nie będą
ponawiane automatycznie w tej rutynie. Poz. 1 (rozszerzenie BuyXGetY) wciąż nietknięta —
patrz notatka 2026-09-28, wymaga osobnej decyzji o (nie)odwróceniu niemutowalności reguły.

## Kolejne kroki (2026-09-30)

Wszystkie pozycje 1-7 powyżej są już zrobione, zablokowane (klasyfikator/potrzebna
decyzja usera) albo okazały się nieaktualne. Poniższe to naturalne kontynuacje już
podjętych decyzji (ADR-0042/ADR-0043) plus jedna nowa, uzasadniona luka — nie wymagają
nowego ADR, chyba że zaznaczono inaczej.

8. ~~**Integracja frontendu z koszykiem serwerowym**~~ — **zrobione (2026-10-09).**
   `CartContext` przełącza źródło prawdy na `GET/POST/PATCH/DELETE /api/cart[...]` dla
   zalogowanych klientów (`isAuthenticated && customerId`), goście bez zmian
   (`useContext(AuthContext)` zamiast rzucającego `useAuth()` — `CartContext.test.tsx`
   renderuje `CartProvider` bez `AuthProvider` nad nim, więc musi bezpiecznie
   degradować do trybu gościa). Backend nie trzyma nazw/cen (ADR-0043 §2) —
   `hydrateFromServerCart` odtwarza je z żywego katalogu (`getMenu()`), pomijając
   pozycję, jeśli referencja już nie istnieje. Mutacje optymistyczne +
   `pendingAddByKey`/`backendIdByKey` obsługują realny wyścig (remove tuż po add, zanim
   POST wróci z prawdziwym id) — pokryte osobnym testem. Scalanie koszyka gościa po
   zalogowaniu świadomie NIE zaimplementowane (patrz poz. 9 — osobna, nierozstrzygnięta
   decyzja); koszyk gościa zostaje nietknięty w `localStorage`. 119/119 testów
   frontendu, typy/lint/build czyste; nie testowane ręcznie w przeglądarce.

9. ~~**Scalanie koszyka gościa po zalogowaniu**~~ — **zrobione (2026-10-10).** Decyzja na
   konflikt: sumować ilości, nie nadpisywać — to już ustalony sposób łączenia identycznej
   selekcji gdzie indziej w koszyku (reducer `add`, i backendowy `addCartItem`, ADR-0043
   §B). `mergeGuestCartIntoServerCart` w `CartContext.tsx` po prostu odtwarza każdą
   linię gościa przez `addCartItem` przy przejściu na koszyk serwerowy — backend już sam
   scala po `(menuItemId, variantId, extraIds)` i sumuje ilość, więc nie trzeba
   duplikować tej logiki po stronie klienta. Linia znika z `localStorage` tylko po
   udanym POST-cie, więc częściowy błąd zostawia jedynie nieschowaną resztę do ponowienia
   przy następnym montowaniu, bez utraty czy podwójnego scalenia tego co już przeszło.

10. **TTL/auto-czyszczenie starych koszyków** — jawnie odłożone w ADR-0043 z powodu braku
    infrastruktury zadań w tle (Hangfire/scheduled job) w tym repo. Realny dopiero gdy
    pojawi się pierwszy inny powód do wprowadzenia takiej infrastruktury (np. czyszczenie
    wygasłych `GuestTrackingToken` — patrz poz. 3) — nie warto dodawać jej wyłącznie dla
    tego. Zakres: **średni**, wymaga nowej decyzji infrastrukturalnej, nie tylko ADR.

11. ~~**UI recenzji na froncie**~~ — **zrobione (2026-10-08).** `MenuItemCard` pokazuje
    średnią ocenę/liczbę recenzji (`MenuItem.averageRating`/`reviewCount`, dotąd
    nieobecne w `frontend/src/api/types.ts` mimo że backend je zwracał od #12) i rozwijaną
    listę recenzji (`GET /api/menu-items/{id}/reviews`, leniwie ładowaną przez
    `useMenuItemReviews`). `OrderStatusPage` renderuje `OrderReviewForm` dla zamówień
    `Completed` — formularz ocen per pozycja zamówienia, pomija pozycje już ocenione
    (`GET /api/reviews/mine`), wysyła `POST /api/reviews`. Zakres: mały-średni, zgodnie z
    szacunkiem; decyzja z ADR-0042, bez zmian backendowych.

12. ~~**Średnia ocena na liście/karcie pozycji menu**~~ — **zrobione (2026-10-01).**
    `IReviewRepository.GetRatingSummariesAsync` agreguje `Average`/`Count` po stronie
    bazy (EF `GroupBy`), `MenuItemDto` ma teraz `AverageRating`/`ReviewCount`, oba
    handlery katalogu (`GetMenuQueryHandler`/`GetMenuItemByIdQueryHandler`) go
    wypełniają. Po drodze wykryto i naprawiono brakujący `InMemoryReviewRepository` w
    `PizzaShop.Api.Tests` — oba handlery zyskały nową zależność od `IReviewRepository`,
    której `ApiTestFactory` dotąd nie podmieniał, więc testy katalogu próbowały łączyć
    się z prawdziwym Postgresem. 4 nowe testy jednostkowe (Application), 130/130 Api
    testów zielonych po poprawce.

## Kolejne kroki (2026-10-11)

13. ~~**Edycja pozycji w książce adresowej klienta**~~ — **zrobione (2026-10-11).**
    `CustomerAddress.Update(label, deliveryAddress)` istniał w Domain od początku
    (ADR dot. książki adresowej), ale nic go nie wywoływało — klient mógł tylko dodać i
    usunąć adres, więc poprawka literówki w ulicy gubiła `IsDefault`, jeśli to był adres
    domyślny (usuń+dodaj od nowa). `Customer.UpdateAddress` (nowa metoda, wzorzec 1:1 z
    `SetDefaultAddress` — `AddressNotInAddressBookException`, jeśli id nie jest w
    książce), `UpdateCustomerAddressCommand`/Handler/Validator (wzorzec 1:1 z Add*,
    w tym re-geokodowanie nowego adresu), `PUT /api/customers/addresses/{id}`. 11 nowych
    testów (Domain + Application), zielone.

14. ~~**Walidacja istnienia pozycji katalogu przy dodawaniu do koszyka (`AddCartItemCommandHandler`)**~~
    — **zrobione (2026-10-11).** ADR-0043 świadomie zostawił to bez walidacji ("frontend też
    tego nie robi"), opierając się wyłącznie na `CreateOrderCommand` jako jedynym
    autorytatywnym punkcie kontroli. `AddCartItemCommandHandler` teraz sprawdza przez
    `IMenuItemRepository`/`IIngredientRepository`, że `MenuItemId`/`VariantId`/`ExtraIds`
    faktycznie istnieją w katalogu (`NotFoundException`) i że każdy `ExtraId` jest
    dozwolony dla tej pozycji (`MenuItem.EnsureExtraAllowed`) — bez wywołania
    `MenuItem.ResolvePrice`, które wymusza reguły specyficzne dla zamówienia
    (dostępność, wymagany wariant), nieadekwatne dla szkicu-koszyka. 6 nowych testów
    jednostkowych (Application), 440/440 zielonych.

## Kolejne kroki (2026-10-13)

15. **Frontend dla edycji pozycji w książce adresowej** — **zrobione (2026-10-13).**
    Poz. 13 dodała `PUT /api/customers/addresses/{id}` na backendzie, ale
    `MyAccountPage` wołał tylko `addAddress`/`removeAddress`/`setDefaultAddress` —
    edycja istniejącego wpisu wymagała usunięcia i ponownego dodania (gubiąc
    `isDefault`, ten sam problem co poz. 13 na poziomie API). `updateAddress`
    w `customersApi.ts` + przycisk "Edytuj" ładujący wpis do istniejącego formularza
    "Dodaj adres" (teraz dzielonego między add/edit, przełącznik przez
    `editingAddressId`), przycisk zmienia się na "Zapisz zmiany" + "Anuluj". 2 nowe
    testy (`MyAccountPage.test.tsx`), 123/123 zielonych.

## Kolejne kroki (2026-10-15)

16. **Ocena widoczna w panelu admina katalogu** — **zrobione (2026-10-15).** Poz. 12
    dodała `averageRating`/`reviewCount` do `MenuItemDto`/`MenuItemCard` (widok klienta),
    ale `MenuItemList` (tabela `/admin/menu` do zarządzania katalogiem) nigdy nie dostała
    tej samej kolumny — admin zarządzający katalogiem nie widział, które pozycje są
    dobrze/słabo oceniane, bez przechodzenia na stronę klienta. Nowa kolumna "Ocena" w
    `MenuItemList.tsx`, ten sam format `★ X.X (N)` / "—" co `MenuItemCard`. Czysto
    frontendowe — pole już istniało w typie `MenuItem`, zero zmian backendowych. 2 nowe
    testy (`MenuItemList.test.tsx`, nowy plik — komponent nie miał dotąd własnych testów),
    125/125 zielonych.

## Kolejne kroki (2026-10-16)

17. **Filtr i polskie etykiety kategorii w panelu admina katalogu** — **zrobione
    (2026-10-16).** Dwie luki w tym samym pliku: (a) `MenuList` (widok klienta) od dawna
    ma wyszukiwanie + filtr kategorii, ale `MenuItemList` (`/admin/menu`) pokazywał
    wszystkie pozycje bez żadnego filtra — niewygodne, gdy katalog rośnie; (b) `MenuList`
    renderuje kategorię przez `CATEGORY_LABELS` (polskie etykiety), a `MenuItemList`
    renderował surową wartość enuma (`Drink`, `Side`...) mimo że cała resztę UI jest po
    polsku. Dodano ten sam search+select wzorzec z `MenuList` + poprawiono wyświetlanie
    kategorii. 4 nowe testy, 129/129 zielonych.

18. **Walidacja długości etykiety adresu w panelu konta klienta** — **zrobione
    (2026-10-16).** `AddCustomerAddressCommandValidator`/`UpdateCustomerAddressCommandValidator`
    ograniczają `Label` do 100 znaków (ADR-0012, shape-only), a frontendowy `validateAddress`
    już konsekwentnie mirroruje każdy limit długości `Address` — ale `MyAccountPage`'s
    `handleSaveAddress` sprawdzał `newLabel` tylko na puste pole, nigdy na limit 100 znaków,
    więc za długa etykieta przechodziła walidację frontu i trafiała na gołe query 400 z
    backendu. Nowa `validateAddressLabel` w `checkout/validation.ts` (ten sam wzorzec co
    `validateFullName`/`validatePromotionCode`), użyta w `MyAccountPage`. 5 nowych testów,
    134/134 zielonych.

## Kolejne kroki (2026-10-18)

19. **Limit długości komentarza recenzji** — **zrobione (2026-10-18).** Ten sam wzorzec
    luki co poz. 18, w innym miejscu: `Review.MaxCommentLength` (Domain, 1000 znaków) jest
    wymuszany przez `CreateReviewCommandValidator`, ale `textarea` komentarza w
    `OrderReviewForm.tsx` nie miała żadnego limitu po stronie frontu — za długi komentarz
    przechodził przez jedyny istniejący check ("czy wybrano ocenę") i trafiał na goły 400 z
    backendu. Naprawione najprościej z możliwych sposobów, zgodnym z istniejącym
    precedensem w tym repo (`PromotionField.tsx`'s `maxLength={50}`): atrybut HTML
    `maxLength={1000}` na `textarea`, bez osobnej walidacji JS. 3 nowe testy
    (`OrderReviewForm.test.tsx`, nowy plik — komponent nie miał dotąd własnych testów),
    137/137 zielonych.

20. **Limit maksymalnej ilości pozycji w koszyku** — **zrobione (2026-10-18).** Ten sam
    wzorzec luki co poz. 18/19, trzeci raz: `AddCartItemCommandValidator`/
    `UpdateCartItemQuantityCommandValidator` ograniczają `Quantity` do `InclusiveBetween(1,
    100)`, ale input liczby w `CartView.tsx` miał tylko `min={1}`, bez `max`. Co ważniejsze:
    atrybut `max` na `<input type="number">` i tak nie blokuje wpisania większej wartości
    (tylko strzałki stepper'a) — a koszyk gościa (bez backendu) nie miał WOGÓLE żadnej
    walidacji ilości. Dodano realne przycinanie do 100 w `cartReducer` (`add`/`setQuantity`)
    i w `CartContext.setQuantity`'s wywołaniu PATCH, plus `max={100}` na input jako
    kosmetyczny sygnał. 3 nowe testy, 140/140 zielonych.

## Kolejne kroki (2026-10-19)

21. **Polskie etykiety kategorii w formularzu pozycji menu** — **zrobione (2026-10-19).**
    Poz. 16 (2026-10-16) podpięła `CATEGORY_LABELS` pod tabelę i filtr w `MenuItemList`,
    ale `<select>` kategorii w `MenuItemForm.tsx` (dodawanie/edycja pozycji, ten sam
    ekran `/admin/menu`) dalej renderował surowe angielskie wartości enuma (`Drink`,
    `Side`...) — admin widział "Napoje" w tabeli, a "Drink" w formularzu tuż obok.
    Etykiety opcji idą teraz przez `CATEGORY_LABELS`, `value` zostaje surowym enumem
    wysyłanym do backendu. 2 nowe testy (`MenuItemForm.test.tsx`, nowy plik), 142/142
    zielonych.

22. **Limity długości w adminowych formularzach katalogu** — **zrobione (2026-10-19).**
    Ten sam wzorzec luki co poz. 18-20, tym razem po stronie admina:
    `Create/UpdateMenuItemCommandValidator` ograniczają `Name` do 200, `Description` do
    1000 i nazwę wariantu do 100 znaków, a `Create/UpdateIngredientCommandValidator` —
    `Name` do 200 i `Category` do 100, ale żaden z inputów w `MenuItemForm`,
    `MenuItemVariantsEditor` i `IngredientForm` nie miał `maxLength`, więc za długa
    wartość kończyła się dopiero listą błędów 400 z backendu. Dodano atrybut HTML
    `maxLength` (ten sam prosty wariant co poz. 19 — formularz wysyłany jednorazowo, bez
    reducera). 3 nowe testy (`IngredientForm.test.tsx` nowy plik), 145/145 zielonych.

## Kolejne kroki (2026-10-20)

23. **Limity długości w formularzu promocji** — **zrobione (2026-10-20).** Ten sam wzorzec
    co poz. 22, ostatni adminowy formularz z tym brakiem: `CreatePromotionCommandValidator`
    ogranicza `Name` do 200, a `Code` do 50 znaków (ten sam limit, który `PromotionField`
    w checkoucie mirroruje od dawna przez `maxLength={50}`), ale inputy nazwy i kodu w
    `PromotionForm.tsx` nie miały żadnego `maxLength`. Dodano atrybut HTML. 1 nowy test
    (`PromotionForm.test.tsx`, nowy plik).

24. **Walidacja hasła w formularzu konta pracownika** — **zrobione (2026-10-20).**
    `RegisterStaffAccountCommandValidator` ma dokładnie te same reguły hasła co
    `RegisterCustomerCommandValidator` (8-100 znaków, co najmniej jedna litera i jedna
    cyfra), a `RegisterPage` od dawna mirroruje je po stronie frontu przez
    `validateNewPassword` — ale `StaffAccountForm.tsx` (admin `/admin/staff`) nie
    sprawdzał niczego poza `required`, więc słabe hasło wracało dopiero jako lista błędów
    400. Ten sam `validateEmail`/`validateNewPassword` co w `RegisterPage`, błędy pod
    polami, plus `maxLength={200}` na e-mailu. 4 nowe testy (`StaffAccountForm.test.tsx`,
    nowy plik), 150/150 zielonych.

25. **Polskie etykiety ról w liście kont pracowników** — **zrobione (2026-10-20).** Ten
    sam wzorzec co poz. 16/21 (etykiety kategorii), tym razem dla ról: `StaffAccountForm`
    miał lokalne `ROLE_LABELS` ("Pracownik", "Administrator restauracji"), ale
    `StaffAccountList` tuż pod nim na `/admin/staff` renderował surowy enum
    (`RestaurantAdmin`). `ROLE_LABELS` przeniesione do współdzielonego `auth/roleLabels.ts`
    (analogicznie do `menuCategoryLabels.ts`) i użyte w obu miejscach. 1 nowy test
    (`StaffAccountList.test.tsx`, nowy plik), 151/151 zielonych.

## Kolejne kroki (2026-10-21)

26. **Rzeczywisty status promocji w tabeli admina** — **zrobione (2026-10-21).** Kolumna
    "Aktywna" w `PromotionList.tsx` pokazywała surową flagę `isActive`, więc promocja po
    `validTo`, przed `validFrom` albo z wyczerpanym `usageLimit` dalej świeciła "Tak", choć
    `Promotion.IsQualifiedFor` (Domain) już jej nie zastosuje. Nowa kolumna "Status"
    (`promotionStatus.ts`): Nieaktywna / Zaplanowana / Wygasła / Wyczerpana / Aktywna — te
    same warunki okna czasu i limitu użyć co w Domain, bez nowego endpointu. 6 nowych
    testów (`PromotionList.test.tsx`, nowy plik), 157/157 zielonych.

27. **Pełna wartość promocji w tabeli admina** — **zrobione (2026-10-21).** Kolumna
    "Wartość" w `PromotionList.tsx` umiała opisać tylko `Percentage`/`FixedAmount` — dla
    `BuyXGetY` i `FreeDelivery` pokazywała "—", a `minOrderValue` nie był widoczny w ogóle
    (tylko w formularzu edycji). Teraz reguła BuyXGetY jest opisana ("Kup 2, 1 taniej o
    100%"), a próg minimalnego zamówienia doklejany ("10%, od 50.00 PLN"). 2 nowe testy,
    159/159 zielonych.

28. **Adres dostawy w kolejce pracownika** — **zrobione (2026-10-21).** `EmployeeOrderRow`
    pokazywał numer, status, kontakt i kwotę, ale nigdy `deliveryAddress` — pracownik
    wysyłający zamówienie "Dostawa" miał telefon klienta, ale nie adres, choć jest on na
    każdym takim zamówieniu (razem z notatką typu "domofon nie działa"). Nowa linia
    "Adres: ..." dla zamówień z dostawą; `formatAddress` przeniesiony z `MyAccountPage` do
    współdzielonego `shared/formatAddress.ts`. 2 nowe testy (`EmployeeOrderRow.test.tsx`,
    nowy plik), 161/161 zielonych.

29. **Zamówiona godzina realizacji w kolejce pracownika** — **zrobione (2026-10-21).**
    Krok 4 checkoutu pozwala zamówić na konkretną godzinę (`requestedFulfillmentTime`,
    ADR-0036), ale ta wartość nie była nigdzie wyświetlana — w kolejce zamówienie na 18:30
    wyglądało identycznie jak "jak najszybciej", więc pracownik nie wiedział, z którym
    poczekać. Pogrubiona linia "Na godzinę: ..." w `EmployeeOrderRow`, gdy pole jest
    ustawione (`null` = ASAP, bez linii). 2 nowe testy, 163/163 zielonych.

## Kolejne kroki (2026-10-22)

30. **Zamówiona godzina na stronie śledzenia zamówienia (klient)** — **zrobione
    (2026-10-22).** Odpowiednik poz. 29 po stronie klienta: `OrderTrackingStatus` (strona
    potwierdzenia, `/orders/:id`, śledzenie po tokenie) pokazywał tylko status i
    `estimatedReadyAt`, nigdy wybranej w kroku 4 checkoutu godziny — przy zamówieniu na
    później ETA łatwo było odczytać jako "przyjedzie wcześniej, niż prosiłem". Nowa linia
    "Zamówione na: ...", gdy `requestedFulfillmentTime` jest ustawione. 2 nowe testy
    (`OrderTrackingStatus.test.tsx`, nowy plik), 165/165 zielonych.

31. **Lista pozycji w kolejce pracownika** — **zrobione (2026-10-22).** `EmployeeOrderRow`
    pokazywał tylko liczbę pozycji ("3 poz."), więc osoba przygotowująca zamówienie
    musiała szukać gdzie indziej, co w nim właściwie jest — warianty, dodatki i uwagi do
    pozycji były w `OrderDto`, ale nie w kolejce. Każda pozycja w formacie "2× Margherita —
    32 cm (oliwki)" (nowy `formatOrderItem.ts`, ten sam kształt co `OrderSummary` w
    checkoucie) plus "uwagi: ..." gdy są. 1 nowy test, 166/166 zielonych.

32. **Pozycje i suma na stronie śledzenia zamówienia (klient)** — **zrobione
    (2026-10-22).** `OrderTrackingStatus` (potwierdzenie, `/orders/:id`, śledzenie po
    tokenie) pokazywał wyłącznie status — klient sprawdzający zamówienie po wyjściu z
    checkoutu (albo gość z samym linkiem śledzenia) nie widział ani jego zawartości, ani
    kwoty. Lista pozycji przez ten sam `formatOrderItem` co kolejka pracownika (poz. 31)
    plus "Razem". 1 nowy test, 167/167 zielonych.

33. **Sposób realizacji i adres na stronie śledzenia zamówienia (klient)** — **zrobione
    (2026-10-22).** `OrderTrackingStatus` nie pokazywał, czy zamówienie jest z dostawą, ani
    dokąd jedzie — gość z samym linkiem śledzenia nie mógł sprawdzić adresu wpisanego w
    checkoucie. Nowa linia "Dostawa: <adres>" / "Odbiór osobisty". Przy okazji
    zduplikowane `FULFILLMENT_LABELS` (`MyAccountPage` i `EmployeeOrderRow`) przeniesione do
    wspólnego `orderStatusLabels.ts`. 2 nowe testy, 169/169 zielonych.

## Kolejne kroki (2026-10-23)

34. **Skład pizzy na karcie menu** — **zrobione (2026-10-23).** Każdy `MenuItem` przychodzi z
    `baseIngredients`, ale używał ich wyłącznie adminowy formularz — klient sprawdzający,
    czy pizza zawiera składnik, którego unika, miał tylko wolnotekstowy opis. `MenuItemCard`
    pokazuje teraz "Składniki: mozzarella, sos pomidorowy" (gdy lista nie jest pusta). 2 nowe
    testy, 171/171 zielonych.

35. **Wyszukiwanie w menu po składniku** — **zrobione (2026-10-23).** Naturalny ciąg dalszy
    poz. 34: karta pokazuje już skład, ale wyszukiwarka `MenuList` dopasowywała tylko nazwę
    pozycji, więc "pieczarki" nie znajdowało Funghi. Zapytanie dopasowuje teraz także nazwy
    `baseIngredients`; placeholder zmieniony na "Szukaj po nazwie lub składniku...". 1 nowy
    test (`MenuList.test.tsx`, nowy plik), 172/172 zielonych.

## Kolejne kroki (2026-10-24)

36. **Wyszukiwarka w adminowej sekcji składników** — **zrobione (2026-10-24).** `MenuItemList`
    na `/admin/menu` ma wyszukiwarkę od poz. 16, ale `IngredientsSection` tuż obok — nie,
    choć słownik składników tylko rośnie, a znalezienie "mozzarella", żeby przełączyć jej
    dostępność, wymagało przewijania całej tabeli. Pole "Szukaj składnika" filtruje po
    nazwie i (wolnotekstowej) kategorii. 2 nowe testy (`IngredientsSection.test.tsx`, nowy
    plik), 174/174 zielonych.
37. **Wyszukiwarka na liście promocji** — **zrobione (2026-10-27).** Tabele menu i
    składników mają wyszukiwarkę (poz. 16, 36), a `PromotionList` na `/admin/promotions` —
    nie, choć promocje są tylko dezaktywowane, nigdy usuwane, więc lista tylko rośnie.
    Pole "Szukaj promocji" filtruje po nazwie i kodzie (np. gdy klient pyta o "JESIEN10").
    1 nowy test (`PromotionList.test.tsx`), 175/175 zielonych.
38. **Komunikat "brak wyników" w nowych wyszukiwarkach** — **zrobione (2026-10-28).**
    `MenuItemList` i `MenuList` przy wyszukiwaniu bez trafień pokazują "Brak pozycji
    spełniających kryteria wyszukiwania.", ale wyszukiwarki z poz. 36 i 37
    (`IngredientsSection`, `PromotionList`) zostawiały wtedy pustą tabelę z samym
    nagłówkiem. Oba miejsca dostały ten sam komunikat. 2 nowe testy, 177/177 zielonych.
39. **Zaznaczenie bieżącej strony w nagłówku** — **zrobione (2026-10-28).** Adminowi
    nagłówek pokazuje do ośmiu linków (koszyk, konto, panel pracownika, panel admina,
    promocje, restauracja, pracownicy, raporty), ale żaden nie mówił, na której stronie
    jesteś. Linki to teraz `NavLink` (`aria-current="page"` + klasa `.active`, akcent i
    podkreślenie); "Panel admina" dopasowuje `/admin` dokładnie (`end`), żeby nie świecił
    obok konkretnej podstrony. 2 nowe testy (`Layout.test.tsx`, nowy plik), 179/179 zielonych.
40. **Potwierdzenie przed dezaktywacją promocji** — **zrobione (2026-10-29).** Dezaktywacja
    jest jednokierunkowa (nie ma akcji "aktywuj"), a wykonywała się po jednym kliknięciu.
    Teraz pyta przez `window.confirm` — ten sam wzorzec, którego `EmployeeOrderRow` używa
    przed anulowaniem zamówienia. 2 nowe testy, 181/181 zielonych.
41. **Potwierdzenie przed wyczyszczeniem koszyka** — **zrobione (2026-10-29).** "Wyczyść
    koszyk" stoi tuż obok "Przejdź do kasy" i jednym kliknięciem usuwał wszystkie pozycje
    razem z wariantami i dodatkami. Teraz pyta przez `window.confirm` (jak poz. 40).
    2 nowe testy (`CartView.test.tsx`, nowy plik), 183/183 zielonych.
42. **Potwierdzenie przed usunięciem zapisanego adresu** — **zrobione (2026-10-29).** "Usuń"
    przy adresie na `/account` kasował go od razu, a odtworzenie oznaczało przepisanie całego
    adresu przy następnym zamówieniu. Teraz pyta przez `window.confirm` z etykietą adresu
    (jak poz. 40-41). 1 nowy test + istniejący test błędu API potwierdza dialog, 184/184 zielonych.
43. **Potwierdzenie przed usunięciem zdjęcia pozycji menu** — **zrobione (2026-10-29).**
    W przeciwieństwie do reszty `MenuItemForm` "Usuń zdjęcie" zapisuje od razu (bez kroku
    "Zapisz"), a przywrócenie zdjęcia wymaga ponownego wgrania pliku. Teraz pyta przez
    `window.confirm` (jak poz. 40-42). 1 nowy test, 185/185 zielonych.

## Kolejne kroki (2026-10-31)

44. **`lang="pl"` w `index.html`** — **zrobione (2026-10-31).** Cały interfejs jest po polsku,
    ale dokument deklarował `lang="en"`, więc czytniki ekranu czytały polskie teksty
    angielskim syntezatorem, a przeglądarka proponowała tłumaczenie "z angielskiego" i
    dzieliła wyrazy wg reguł angielskich. Jednolinijkowa zmiana atrybutu.
45. **Tytuł karty przeglądarki per strona (klient)** — **zrobione (2026-10-31).** Każda trasa
    miała ten sam statyczny tytuł "PizzaShop" z `index.html`, więc koszyk, checkout i
    śledzone w drugiej karcie zamówienie wyglądały w pasku kart identycznie. Nowy hook
    `useDocumentTitle` ustawia "<strona> — PizzaShop" (i przywraca poprzedni tytuł przy
    odmontowaniu); użyty na wszystkich stronach klienta. Panel admina/pracownika to osobny
    krok. 3 nowe testy, 188/188 zielonych.

## Świadomie pominięte

Multi-tenancy (kilka restauracji) i skalowanie enterprise (sharding, multi-region,
event sourcing) pozostają poza zakresem bez wyraźnej potrzeby zgłoszonej przez usera —
zgodnie z ADR-0003 i sekcją "Krytyczne ograniczenia" w `CLAUDE.md`.
