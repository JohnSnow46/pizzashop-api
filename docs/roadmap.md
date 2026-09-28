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

2. **Trwały koszyk / odzyskiwanie porzuconych koszyków** — koszyk świadomie NIE jest
   dziś agregatem Domain (`domain-model.md` §10) — zamówienie powstaje wprost z
   "draftu". Dodanie trwałego `Cart` (TTL, powiązanie z zalogowanym klientem, recovery
   po powrocie) to konkretny, wcześniej przewidziany krok, dobrze pokazujący
   projektowanie nowego agregatu w dojrzałym repo. Zakres: **duży** (nowy agregat,
   migracja, endpointy, frontend). **Wymaga nowego ADR**.

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

7. **Asynchroniczne powiadomienia dla gościa** (e-mail przy kluczowych przejściach
   statusu: potwierdzone / gotowe / dostarczone) — dziś status widoczny wyłącznie przez
   live-tracking SignalR, czyli tylko gdy klient ma aktywne połączenie. E-mail jako
   fallback adresuje realny UX gap gościa offline. Zakres: **średni** (nowy port
   `INotificationSender`, implementacja np. SMTP/SendGrid w Infrastructure). **Wymaga
   nowego ADR** (wybór dostawcy, momenty wysyłki, szablon).

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

## Świadomie pominięte

Multi-tenancy (kilka restauracji) i skalowanie enterprise (sharding, multi-region,
event sourcing) pozostają poza zakresem bez wyraźnej potrzeby zgłoszonej przez usera —
zgodnie z ADR-0003 i sekcją "Krytyczne ograniczenia" w `CLAUDE.md`.
