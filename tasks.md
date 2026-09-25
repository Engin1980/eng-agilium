# Tasks

Přehled toho, co je v systému hotové (a stačí zkontrolovat/otestovat) a co je ještě potřeba
naimplementovat. Vychází z `description.md` a aktuálního stavu kódu v `Agilium.Be` / `Agilium.Fe`.

Legenda: `[x]` hotovo (zkontrolovat/otestovat), `[ ]` chybí / je potřeba doimplementovat.

## Backend — databáze / model

- [x] DB schéma a entity: `AppUser`, `Project`, `Role`, `Membership`, `Item`, `Sprint`, `SprintItem`,
      `WorkflowState`, `Template`, `TemplateColumn`, `TemplateItem`, `Token` (`Model\Db`).
- [x] Počáteční EF Core migrace (`Migrations\20260318163756_Initial_DB_Structure*`).
- [ ] Zkontrolovat, že `SprintItem` a vazby na `WorkflowState` jsou v migraci kompletní a odpovídají
      aktuální podobě modelu (u nových funkcí níže bude potřeba přidávat další migrace).
- [ ] Doplnit úložiště hodnot polí šablony pro konkrétní `Item` — v `Item` entitě zatím není žádné
      pole/tabulka pro reálné hodnoty definované přes `TemplateItem` (jen definice šablony existuje).
- [ ] Odstranit `Epic` z `ItemType` enumu (`Item.cs`) — dle `description.md` je `Epic` zrušen/sloučen
      do `Feature`, enum ale `Epic` pořád má jako samostatnou hodnotu; vyžaduje EF migraci.
- [ ] Přejmenovat `InProgress` na `Active` v `WorkflowStateType` enumu (`WorkflowState.cs`) — sjednotit
      terminologii s `description.md` ("ACTIVE"); vyžaduje EF migraci.
- [ ] Odstranit `ExpectedStartDateTime`/`ExpectedEndDateTime` ze `Sprint` entity (`Sprint.cs`) — dle
      `description.md` má sprint jen jeden pár datumů (`StartDateTime`/`EndDateTime`); vyžaduje EF migraci.
- [ ] Přepracovat `TemplateColumn`/`TemplateItem` z pevných sloupců (`WidthWeight`) na CSS grid
      pozicování (viz `description.md`, sekce "Upřesnění k User stories") — potřeba doladit přesný tvar
      polí pro pozici/rozpětí v gridu a udělat EF migraci.
- [ ] Založit šablonu i pro `Bug` při vytvoření projektu (`Features\Projects\Create.cs`) — dle
      `description.md` má `Bug` vlastní šablonu odlišnou od `Task` a při vytvoření projektu se má
      vytvořit lokální kopie i pro ni.

## Backend — uživatelé a autentizace

- [x] Vytvoření uživatele (`Features\AppUsers\Create.cs`) — zkontrolovat, zda `BaseRoute.Auth` je
      správné umístění endpointu (v kódu je u toho TODO poznámka).
- [x] Přihlášení (`Features\Auth\Login.cs`) — JWT access token + refresh token cookie, Turnstile
      captcha, lockout po `MaxFailedLoginAttempts`.
- [x] Obnovení access tokenu (`Features\Auth\Refresh.cs`).
- [x] Odhlášení (`Features\Auth\Logout.cs`).
- [ ] Registrace uživatele „samoobslužně“ / reset hesla / správa účtu (aktivace, deaktivace) — zatím
      neřešeno, čeká na upřesnění (viz `description.md`).
- [ ] Autorizace endpointů podle rolí — `RequiredRoles` je u všech endpointů zatím prázdné a validace
      rolí v `GenericEndpoint.ValidateRequiredRoles` je zakomentovaná; je potřeba ji dopracovat a
      endpointy osadit reálnými požadovanými rolemi/oprávněními.

## Backend — projekty

- [x] Vytvoření projektu vč. založení výchozích rolí (Owner/Guest), members (owner), výchozích
      workflow stavů a výchozích šablon pro typy položek (`Features\Projects\Create.cs`).
- [x] Úprava projektu — název/popis (`Features\Projects\Update.cs`).
- [x] Změna stavu projektu Active/Inactive (`Features\Projects\UpdateStatus.cs`).
- [x] Výpis projektů, volitelně filtrovaný podle člena (`Features\Projects\List.cs`).
- [x] Výpis položek projektu jako stromu (feature → user-story → task/bug) (`Features\Projects\ListItems.cs`).
- [x] Přiřazení/aktualizace role uživatele v projektu (`Features\Projects\AssignMember.cs`).
- [x] Odebrání členství z projektu, včetně kontroly, že uživatel nemá přiřazené položky (`Features\Projects\UnassignMember.cs`).
- [x] Číselník stavů projektu pro FE (`Features\Projects\StateOptions.cs`).
- [ ] Smazání projektu (pokud má být podporováno) — endpoint chybí.
- [ ] Správa vlastních rolí projektu (vytvoření/úprava role a jejích oprávnění nad rámec výchozích
      Owner/Guest) — entita `Role` existuje, ale CRUD endpoint chybí.
- [ ] Výpis členů projektu (seznam `Membership` vč. uživatele a role) — zatím dostupné jen nepřímo
      přes `ListItems` (assignee), chybí samostatný endpoint pro správu členů.

## Backend — položky (Feature/User-Story/Task/Bug)

- [x] Vytvoření položky vč. validace projektu a rodiče (`Features\Items\Create.cs`).
- [x] Úprava položky — název, typ (`Features\Items\Update.cs`).
- [x] Změna přiřazeného uživatele (assignee) vč. kontroly členství v projektu (`Features\Items\UpdateAssignee.cs`).
- [x] Změna rodiče položky vč. kontroly cyklu a příslušnosti k projektu (`Features\Items\UpdateParent.cs`).
- [x] Číselník typů položek pro FE (`Features\Items\TypeOptions.cs`).
- [ ] Smazání položky — endpoint chybí.
- [ ] Nastavení/změna stavu položky (`WorkflowState`) — v modelu `Item` zatím není vidět vazba na
      aktuální stav (`WorkflowState`/`SprintItem`); chybí endpoint pro posun položky na kanbanu.
- [ ] Odvození stavu feature/user-story ze stavů podřízených položek (pravidlo z `description.md`:
      TODO/ACTIVE/DONE podle dětí) — zatím nikde implementováno, je potřeba doplnit výpočet (např.
      v `ListItems`/detailu položky).
- [ ] CRUD nad hodnotami polí položky podle šablony (`TemplateItem`) — endpoint pro čtení/uložení
      hodnot dle šablony chybí (viz i bod výše u modelu).

## Backend — sprinty a kanban

- [ ] CRUD sprintu v rámci projektu (vytvoření, úprava, výpis, případně zrušení/smazání) — entita
      `Sprint` existuje, endpointy ve `Features` zcela chybí.
- [ ] Přiřazení/odebrání položky (tasku) na/ze sprintu přes `SprintItem` — chybí.
- [ ] Endpoint pro načtení kanban dat konkrétního sprintu (položky rozřazené podle `WorkflowState`) — chybí.
- [ ] Automatické „zobrazení“ feature/user-story ve sprintu, pokud má alespoň jeden svůj task ve
      sprintu (odvozené pravidlo z `description.md`) — chybí.

## Backend — šablony

- [ ] CRUD nad univerzální (výchozí) šablonou pro daný `ItemType` — chybí (aktuálně se výchozí
      šablony jen vytvářejí natvrdo při vzniku projektu v `Projects\Create.cs`).
- [ ] CRUD nad projektově specifickou šablonou (přepis/úprava výchozí šablony pro konkrétní projekt) — chybí.
- [ ] Endpoint pro načtení šablony pro konkrétní typ položky v projektu (pro vykreslení detailu na FE) — chybí.

## Backend — obecné / infrastruktura

- [x] Generický Handler/Endpoint pattern, DI auto-registrace, validace, zpracování chyb
      (`Features\GenericHandler.cs`, `GenericEndpoint.cs`, `GenericTypes.cs`, `Exceptions\*`).
- [x] Health-check endpointy (`Features\Health\Db.cs`, `Ping.cs`).
- [x] Rate limiting, CORS, Serilog logování, Swagger (`Program.cs`).
- [ ] Autorizační kontrola `RequiredRoles`/oprávnění (`CanViewProject`, `CanManageMembers`, ...) —
      zatím se nikde v handlerech/endpointech reálně nevyhodnocuje, kód je připravený, ale
      zakomentovaný (`GenericEndpoint.ValidateRequiredRoles`).

## Frontend

- [x] Scaffold projektu (Vite + React 19 + TypeScript + TanStack Router/Query + Tailwind v4 + Radix UI).
- [x] Základní globální komponenty: `Dialog`, formulářové prvky (`MiniForm`, `TextInputBlock`,
      `SubmitInput`), loader `Working` (`src\components\global`).
- [x] Routa se seznamem projektů, prozatím na mock datech, vč. dialogu pro založení projektu
      (`src\routes\projects\index.tsx`) — je potřeba jen napojit na `services`/API (viz níže).
- [ ] `services` vrstva — HTTP klient a TanStack Query hooky pro volání BE endpointů (neexistuje).
- [ ] Napojení routy seznamu projektů na reálné API místo mock dat (`GET /api/v1/projects`).
- [ ] Detail projektu — přehled features → user-story → tasks (strom, napojení na `GET /projects/{id}/items`).
- [ ] Pohled přes sprinty — seznam sprintů projektu + kanban pro konkrétní sprint (čeká i na BE endpointy výše).
- [ ] Detail položky (feature/user-story/task/bug) s formulářem generovaným dle šablony
      (`Template`/`TemplateColumn`/`TemplateItem`), vč. editace samotné šablony.
- [ ] Správa členů projektu a jejich rolí (UI nad `AssignMember`/`UnassignMember`).
- [ ] Přihlašovací obrazovka a napojení na JWT autentizaci (zatím jen TODO, čeká na upřesnění návrhu).
- [ ] Založení projektu — dialog v `projects\index.tsx` je zatím jen mock (`handleCreate` upravuje
      lokální state), je potřeba napojit na `POST /api/v1/projects`.
- [ ] `src\components\specific` — zatím prázdné, postupně doplnit doménové komponenty (kanban karta,
      formulář položky dle šablony, editor šablony, správa členů apod.).

## Ostatní

- [ ] Automatizované testy (unit/integration) — v repozitáři zatím žádný testovací projekt neexistuje.
- [ ] `Agilium.Db` obsahuje jen `docker-compose.yml` pro lokální SQL Server — zkontrolovat, zda není
      potřeba doplnit seed data / inicializační skripty.
