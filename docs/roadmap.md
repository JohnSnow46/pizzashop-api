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

14. **Walidacja istnienia pozycji katalogu przy dodawaniu do koszyka (`AddCartItemCommandHandler`)**
    — ADR-0043 świadomie zostawił to bez walidacji ("frontend też tego nie robi"), opierając
    się wyłącznie na `CreateOrderCommand` jako jedynym autorytatywnym punkcie kontroli.
    Dziś `AddCartItemCommandHandler` nie sprawdza, czy `MenuItemId`/`VariantId`/`ExtraIds`
    faktycznie istnieją w katalogu — linia koszyka odwołująca się do usunionej/zmienionej
    pozycji jest dziś nieszkodliwa (frontend już ją pomija przy hydratacji, patrz
    `hydrateFromServerCart`), ale to tania, prawdziwa dziura do zamknięcia, zamiast
    pozwalać koszykowi cicho zbierać martwe referencje. Zakres: **mały** — dodanie
    `IMenuItemRepository`/`IIngredientRepository` do handlera + sprawdzenie istnienia
    (bez wywołania `MenuItem.ResolvePrice`, które wymusza reguły specyficzne dla
    zamówienia, nieadekwatne dla szkicu-koszyka). Nie wymaga nowego ADR. **Nie
    zaimplementowane dzisiaj** — znalezione podczas przeglądu, odłożone na kolejny dzień.

## Świadomie pominięte

Multi-tenancy (kilka restauracji) i skalowanie enterprise (sharding, multi-region,
event sourcing) pozostają poza zakresem bez wyraźnej potrzeby zgłoszonej przez usera —
zgodnie z ADR-0003 i sekcją "Krytyczne ograniczenia" w `CLAUDE.md`.
