# Tasks

Přehled toho, co je v systému hotové (a stačí zkontrolovat/otestovat) a co je ještě potřeba
naimplementovat. Vychází z `description.md` a aktuálního stavu kódu v `Agilium.Be` / `Agilium.Fe`.
Podrobnější body obsahují konkrétní návrh (route, soubory, tvar příkazu/výsledku) navazující na
existující konvence v repozitáři (viz `CLAUDE.md` — Handler/Endpoint pattern) — návrh lze při
implementaci upravit, jde hlavně o to mít jasný, postupně odškrtávatelný plán.

Legenda: `[x]` hotovo (zkontrolovat/otestovat), `[ ]` chybí / je potřeba doimplementovat.

## Lokální infrastruktura (DB kontejner)

- [x] `Agilium.Db\docker-compose.yml` — SQL Server 2022 kontejner (`eng-agilium-db`), port `1433`,
      heslo z `DB_PASSWORD`, perzistentní volume `eng_agilium_mssql_data`.
- [x] `Agilium.Db\.env` — lokální proměnné (heslo DB) pro `docker compose up`.
- [x] `Agilium.Db\.env.example` — demo verze `.env` s placeholder heslem pro ostatní vývojáře.
- [x] `Agilium.Db\.gitignore` — ignoruje `.env` (ověřeno v `git ls-files`).

## Backend — databáze / model

- [x] DB schéma a entity: `AppUser`, `Project`, `Role`, `Membership`, `Item`, `Sprint`, `SprintItem`,
      `WorkflowState`, `Template`, `TemplateTable`, `TemplateColumn`, `TemplateSection`, `TemplateItem`, `ItemFieldValue`, `Token` (`Model\Db`).
- [x] Počáteční EF Core migrace (`Migrations\20260318163756_Initial_DB_Structure*`).
- [ ] Zkontrolovat, že `SprintItem` a vazby na `WorkflowState` jsou v migraci kompletní a odpovídají
      aktuální podobě modelu (u nových funkcí níže bude potřeba přidávat další migrace).
- [x] **Bug v `Projects\Create.cs` → `AddDefaultTemplates`** — opraveno na `Type = type` v obou smyčkách
      (dřív se `Template.Type` nastavovalo natvrdo na `ItemType.Task`).
- [x] Odstranit `ItemType.Epic` (`Item.cs`) a nahradit ho všude `ItemType.Feature` — hotovo (enum hodnota
      odstraněna, `AddDefaultTemplates` → `highTypes` upraveno, v repu nejsou další výskyty).
- [x] Přejmenovat `WorkflowStateType.InProgress` na `WorkflowStateType.Active` (`WorkflowState.cs`) —
      hotovo, `AddDefaultWorkflowStates` upraveno (zobrazovaný `Title = "In Progress"` zůstal).
- [x] Odstranit `ExpectedStartDateTime`/`ExpectedEndDateTime` ze `Sprint` entity (`Sprint.cs`) — hotovo.
- [x] Doplnit `TemplateItemType.Checkbox`/`LabelOnly` (`TemplateItem.cs`) — hotovo.
- [x] EF migrace `Story0_ModelCleanup` pro všechny výše uvedené změny — přidána (zachytila i starší
      nedotaženou drift změnu `Projects.Status` → `Projects.State`).
- [x] Bonus nález v `Projects\Create.cs` → `AddProjectRoles`: role se nikdy nepřidávaly do
      `project.Roles` (jen do lokální proměnné) a `roles.ForEach(roles.Add)` navíc vždy spadl na
      `InvalidOperationException` (modifikace kolekce během iterace) — založení projektu by tedy vždy
      selhalo. Opraveno na `project.Roles.Add(...)`.
- [x] ~~Přepracovat pozicování polí šablony na CSS grid~~ — hotovo ve Story 3, ale **nahrazeno** novým zadáním
      (`layout-templates.md`), viz další bod. (Starší duplicitní body o opravě `AddDefaultTemplates`,
      odstranění `Epic`, přejmenování `InProgress` → `Active` a odstranění `Expected*DateTime` jsou hotové
      — viz Story 0 v `task-stories.md`.)
- [x] Přepracovat rozložení šablony na hierarchii tabulka → sloupec (šířka) → sekce (titulek) → atribut
      (titulek + hodnota), viz `layout-templates.md` a `description.md` ("Rozvržení polí šablony"):
  - entity `Template` → `TemplateTable` (`TemplateId`, `OrderIndex`) → `TemplateColumn` (`TemplateTableId`,
    `Width` int min. 1, `OrderIndex`) → `TemplateSection` (`TemplateColumnId`, `Title` smí být prázdný,
    `OrderIndex`) → `TemplateItem` (`TemplateSectionId`, `Key`, `Title`, `Type`, `ValidatingRegex`,
    `OrderIndex`); z `TemplateItem` odstranit `ColumnStart/ColumnSpan/RowStart/RowSpan`, z `Template`
    `ColumnCount`,
  - validace na BE při ukládání: každá tabulka má ≥ 1 sloupec, šířka sloupce ≥ 1, `Key` unikátní v rámci
    šablony; **součet šířek = 12 se nevynucuje** (jen se předpokládá, FE upozorní), prázdné tabulky a sekce
    jsou povolené,
  - upravit `DefaultTemplates.cs` a `Projects\Create.cs` (kopie globální šablony do projektu — kopíruje celý
    strom) do nového tvaru,
  - EF migrace bez převodu starých grid dat — stávající šablony a `ItemFieldValue` se zahodí, šablony
    vznikají jen kopírováním z globálních.
- [x] Zúžit/přemapovat `TemplateItemType` enum (`TemplateItem.cs`) na 7 hodnot: `SingleLineText`,
      `MultiLineText`, `Integer`, `Decimal`, `Boolean`, `Comments` (speciální komponenta pro komentáře;
      zatím prostý text) a `LabelOnly` (jen popisek bez hodnoty). Odpadá Inline/Nextline, `Checkbox` →
      `Boolean`, `Untemplated` → `LabelOnly`. Migrace převede stávající hodnoty enumu.
- [x] Úložiště hodnot polí šablony pro konkrétní `Item` — `ItemFieldValue` (`ItemId`, `TemplateItemId`,
      `Value`), hotovo ve Story 3 (zůstává beze změny, váže se na `TemplateItem.Id`).
- [ ] Doplnit vazbu položky na aktuální stav v rámci sprintu — `Item` sama o sobě nemá `WorkflowState`,
      ten je jen přes `SprintItem.WorkflowStateId` (tj. stav položky má smysl jen v kontextu konkrétního
      sprintu/kanbanu). Ověřit, že tohle je žádaný design (položka mimo sprint nemá žádný stav — pak je
      potřeba doladit, jak se taková položka zobrazuje v pohledu "features → user-story → tasks", viz
      bod "Odvozování stavu" níže).

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
      rolí v `GenericEndpoint.ValidateRequiredRoles` je zakomentovaná (`Features\GenericEndpoint.cs`,
      řádky ~85, ~90-100). Je potřeba:
  - odkomentovat/dokončit `ValidateRequiredRoles` (aktuálně porovnává jen `LoggedUser.RoleName`, který
    ale `LoggedUser` typ nemá — porovnání bude nejspíš přes `IRoleAssignment`/`Can...` flagy pro daný
    `ProjectId`, ne přes obecný název role),
  - endpointy nad projektovými daty osadit reálnými požadavky na oprávnění (`CanViewProject`,
    `CanManageProject`, `CanViewMembers`, `CanManageMembers`, `CanManageSprints` z `IRoleAssignment` /
    `Role`), s výjimkou `Features\Projects\Create.cs` (zakládá projekt, oprávnění zatím nedává smysl) a
    `Health`/`Auth` endpointů.

## Backend — projekty

- [x] Vytvoření projektu vč. založení výchozích rolí (Owner/Guest), members (owner), výchozích
      workflow stavů a výchozích šablon pro typy položek (`Features\Projects\Create.cs`) — pozor na bug
      v `AddDefaultTemplates`, viz sekce výše.
- [x] Úprava projektu — název/popis (`Features\Projects\Update.cs`).
- [x] Změna stavu projektu Active/Inactive (`Features\Projects\UpdateStatus.cs`).
- [x] Výpis projektů, volitelně filtrovaný podle člena (`Features\Projects\List.cs`).
- [x] Výpis položek projektu jako stromu (feature → user-story → task/bug) (`Features\Projects\ListItems.cs`).
- [x] Přiřazení/aktualizace role uživatele v projektu (`Features\Projects\AssignMember.cs`).
- [x] Odebrání členství z projektu, včetně kontroly, že uživatel nemá přiřazené položky (`Features\Projects\UnassignMember.cs`).
- [x] Číselník stavů projektu pro FE (`Features\Projects\StateOptions.cs`).
- [ ] Smazání projektu — `DELETE /api/v1/projects/{id}` (`Features\Projects\Delete.cs`), `IdParameters`,
      `EmptyResult`. Promyslet, zda jde o hard delete (smazat i navázané `Item`/`Sprint`/`Template`/
      `Role`/`Membership` kaskádově) nebo jen změnu `ProjectState` (v tom případě už `UpdateStatus.cs`
      tohle řeší a samostatný delete endpoint možná není potřeba — potvrdit se zadavatelem).
- [ ] Správa vlastních rolí projektu — CRUD nad `Role` nad rámec výchozích Owner/Guest:
  - `POST /api/v1/projects/{id}/roles` (`Features\Projects\Roles\Create.cs`) — `Command` s `Title` +
    `Can...` flagy, `IdParameters` (projekt), `IdResult`.
  - `PATCH /api/v1/projects/roles/{id}` (`Features\Projects\Roles\Update.cs`) — úprava názvu/flagů.
  - `GET /api/v1/projects/{id}/roles` (`Features\Projects\Roles\List.cs`) — výpis rolí projektu.
  - `DELETE /api/v1/projects/roles/{id}` (`Features\Projects\Roles\Delete.cs`) — zvážit ochranu proti
    smazání role, která má aktivní `Membership`, a proti smazání vestavěných rolí Owner/Guest.
- [ ] Výpis členů projektu — `GET /api/v1/projects/{id}/members` (`Features\Projects\ListMembers.cs`),
      vrátí `Membership` vč. `AppUser` (Id, Name, Surname, Email) a `Role` (Id, Title) — zatím dostupné
      jen nepřímo přes `ListItems` (assignee).

## Backend — položky (Feature/User-Story/Task/Bug)

- [x] Vytvoření položky vč. validace projektu a rodiče (`Features\Items\Create.cs`).
- [x] Úprava položky — název, typ (`Features\Items\Update.cs`).
- [x] Změna přiřazeného uživatele (assignee) vč. kontroly členství v projektu (`Features\Items\UpdateAssignee.cs`).
- [x] Změna rodiče položky vč. kontroly cyklu a příslušnosti k projektu (`Features\Items\UpdateParent.cs`).
- [x] Číselník typů položek pro FE (`Features\Items\TypeOptions.cs`).
- [ ] Smazání položky — `DELETE /api/v1/projects/items/{id}` (`Features\Items\Delete.cs`),
      `IdParameters`, `EmptyResult`. Promyslet chování u položky, která má podřízené položky (feature se
      user-story, user-story s tasky) — buď zakázat (`BadRequestException`), nebo kaskádově smazat i
      děti a příslušné `SprintItem` záznamy; zvážit i ochranu "generických" feature/user-story (viz
      `description.md`), které se nesmí smazat vůbec.
- [ ] Nastavení/změna stavu položky v rámci sprintu — `Item` sám o sobě `WorkflowState` nemá, stav se
      váže přes `SprintItem` (viz bod v sekci "databáze/model"). Konkrétní endpoint pro posun na
      kanbanu patří spíš pod sprinty, viz `PATCH .../sprints/{sprintId}/items/{itemId}/state` níže.
- [ ] Odvození stavu feature/user-story ze stavů podřízených položek (pravidlo z `description.md`:
      TODO/ACTIVE/DONE podle dětí; user-story bez tasků = vždy TODO). Protože stav je vázaný na
      `SprintItem` (konkrétní sprint), je potřeba nejdřív ujasnit, v jakém kontextu se toto pravidlo
      počítá (např. "poslední/aktivní sprint položky" vs. přes všechny sprinty najednou) — pak doplnit
      výpočet, nejspíš jako projekci v `ListItems`/novém detailu položky (ne jako uložené pole, aby se
      nerozcházelo se skutečnými stavy dětí).
- [ ] CRUD nad hodnotami polí položky podle šablony (`TemplateItem`) — po doplnění úložiště hodnot
      (`ItemFieldValue`, viz sekce výše):
  - `GET /api/v1/projects/items/{id}/fields` (`Features\Items\GetFieldValues.cs`) — vrátí šablonu pro
    `Item.Type` daného projektu (viz endpoint šablony níže) spolu s aktuálními hodnotami.
  - `PUT /api/v1/projects/items/{id}/fields` (`Features\Items\SetFieldValues.cs`) — uloží/aktualizuje
    hodnoty; validace podle `TemplateItem.ValidatingRegex` a `TemplateItemType` (custom validation
    atribut, viz `Exceptions\Validation`).

## Backend — sprinty a kanban

- [ ] CRUD sprintu v rámci projektu — entita `Sprint` existuje, endpointy ve `Features` zcela chybí:
  - `POST /api/v1/projects/sprints` (`Features\Sprints\Create.cs`) — `Command(Title, ProjectId,
    StartDateTime, EndDateTime)`, kontrola jednoznačnosti `Title` v rámci projektu
    (`EntityAlreadyExistsException`), `IdResult`.
  - `PATCH /api/v1/projects/sprints/{id}` (`Features\Sprints\Update.cs`) — úprava názvu/datumů/stavu
    sprintu (`SprintState`).
  - `GET /api/v1/projects/{id}/sprints` (`Features\Sprints\List.cs`) — výpis sprintů projektu.
  - `DELETE /api/v1/projects/sprints/{id}` (`Features\Sprints\Delete.cs`) — zvážit, co se stane se
    `SprintItem` záznamy (kaskádový delete vs. zákaz smazání neprázdného sprintu).
  - Zvážit přidání `BaseRoute.Sprints` do `GenericEndpoint.cs` (obdoba `BaseRoute.Items`), pokud bude
    více endpointů operovat přímo nad `sprintId` bez potřeby `projectId` v command.
- [ ] Přiřazení/odebrání položky (tasku/bugu) na/ze sprintu přes `SprintItem`:
  - `POST /api/v1/projects/sprints/{sprintId}/items` (`Features\Sprints\AssignItem.cs`) —
    `Command(ItemId)`, ověřit, že item patří do stejného projektu jako sprint a že ještě není v tomto
    sprintu; založí `SprintItem` s počátečním `WorkflowState` (typicky první `ToDo` stav projektu).
  - `DELETE /api/v1/projects/sprints/{sprintId}/items/{itemId}` (`Features\Sprints\UnassignItem.cs`).
  - Podle `description.md` lze na sprint přiřazovat jen tasky/bugy (ne feature/user-story přímo) —
    validovat `Item.Type`.
- [ ] Endpoint pro načtení kanban dat konkrétního sprintu — `GET /api/v1/projects/sprints/{id}/board`
      (`Features\Sprints\GetBoard.cs`):
  - vrátí sloupce projektu (`WorkflowState`, seřazené dle `OrderIndex`) a v nich položky (`SprintItem`
    → `Item`) přiřazené danému sprintu,
  - zahrnuje i "odvozené" zobrazení feature/user-story (viz bod níže) nad tasky/bugy v daném sprintu.
- [ ] Posun položky mezi sloupci kanbanu — `PATCH /api/v1/projects/sprints/{sprintId}/items/{itemId}/state`
      (`Features\Sprints\UpdateItemState.cs`) — `Command(WorkflowStateId)`, ověří, že cílový stav patří
      do stejného projektu jako sprint; dle `description.md` jde o **volný** posun (bez omezení
      pořadí/přechodů mezi stavy).
- [ ] Automatické „zobrazení“ feature/user-story ve sprintu, pokud má alespoň jeden svůj task/bug ve
      sprintu (odvozené pravidlo z `description.md`) — promítnout do `GetBoard`/`List` odpovědi: pro
      každou feature/user-story s alespoň jedním potomkem v daném sprintu ji zahrnout do výstupu (bez
      vlastního `SprintItem` záznamu, je jen odvozená).

## Backend — šablony

- [x] Univerzální (globální) šablony — `Template.ProjectId IS NULL`, zakládají se idempotentně při startu
      (`Features\Templates\DefaultTemplates.cs`); **needitují se** (žádný CRUD/UI), v budoucnu se případně
      upraví přímo v DB/seedu. Při založení projektu se z nich kopíruje celý strom do projektu.
- [x] Odstranit endpointy a FE pro úpravu globální šablony (`Features\Templates\GetGlobal.cs`,
      `UpdateGlobal.cs`, `BaseRoute.Templates`, `routes\templates\index.tsx` + odkaz ze seznamu projektů,
      globální varianta v `templates-api.ts`/`templates-queries.ts`) — nově se editují jen projektové.
- [ ] Přepsat `Features\Templates\Get.cs`/`Update.cs` (`GET`/`PUT /api/v1/projects/{id}/templates/{itemType}`)
      na hierarchii tabulka → sloupec → sekce → atribut:
  - `Get` vrací strom včetně `Id` a `OrderIndex` na každé úrovni,
  - `Update` přijímá celý strom; prvky s `Id` se aktualizují, bez `Id` vytvoří, ty, které chybí, se smažou
    (u atributů i s jejich `ItemFieldValue`; změna `Type` existujícího atributu se bere jako smazání +
    nový atribut, aby zůstala hodnota konzistentní); `Result` vrací počet smazaných hodnot,
  - validace: ≥ 1 sloupec v tabulce, šířka ≥ 1, `Key` unikátní, platný `Type`; součet 12 se nevynucuje.
- [ ] Samostatný endpoint šablony pro vykreslení detailu položky není potřeba — `GetFieldValues` vrací
      šablonu + hodnoty (viz sekce "položky").

## Backend — obecné / infrastruktura

- [x] Generický Handler/Endpoint pattern, DI auto-registrace, validace, zpracování chyb
      (`Features\GenericHandler.cs`, `GenericEndpoint.cs`, `GenericTypes.cs`, `Exceptions\*`).
- [x] Health-check endpointy (`Features\Health\Db.cs`, `Ping.cs`).
- [x] Rate limiting, CORS, Serilog logování, Swagger (`Program.cs`).
- [ ] Autorizační kontrola `RequiredRoles`/oprávnění — viz detailní rozpad v sekci "uživatelé a
      autentizace" výše.

## Frontend

- [x] Scaffold projektu (Vite + React 19 + TypeScript + TanStack Router/Query + Tailwind v4 + Radix UI).
- [x] Základní globální komponenty: `Dialog`, formulářové prvky (`MiniForm`, `TextInputBlock`,
      `SubmitInput`), loader `Working` (`src\components\global`).
- [x] Routa se seznamem projektů, prozatím na mock datech, vč. dialogu pro založení projektu
      (`src\routes\projects\index.tsx`) — je potřeba jen napojit na `services`/API (viz níže).
- [x] Routa detailu projektu — zatím scaffold (`src\routes\projects\$id\index.tsx`), potřeba napojit na
      `GET /projects/{id}/items` a doplnit strom features → user-story → tasks.
- [ ] `services` vrstva — HTTP klient (fetch wrapper, base URL, JWT/refresh handling) + TanStack Query
      hooky pro volání BE endpointů (neexistuje, `src\services` chybí zcela). Založit strukturu např.
      `src\services\http-client.ts` (fetch wrapper) + `src\services\<domain>\*.ts` (queries/mutations
      per doména — projects, items, sprints, templates, members).
- [ ] Napojení routy seznamu projektů na reálné API místo mock dat (`GET /api/v1/projects`,
      `POST /api/v1/projects` pro dialog založení projektu v `handleCreate`).
- [ ] Detail projektu — přehled features → user-story → tasks (strom, napojení na
      `GET /projects/{id}/items` v `routes\projects\$id\index.tsx`).
- [ ] Pohled přes sprinty — čeká na BE endpointy (sekce "sprinty a kanban" výše):
  - routa se seznamem sprintů projektu (`src\routes\projects\$id\sprints\index.tsx`),
  - routa kanban view pro konkrétní sprint (`src\routes\projects\$id\sprints\$sprintId\index.tsx`) —
    sloupce dle `WorkflowState`, karty = tasky/bugy, přiřazování a posun mezi sloupci (drag & drop nebo
    alespoň select/tlačítka jako MVP).
- [x] Detail položky (feature/user-story/task/bug) s formulářem generovaným dle šablony (hotová verze ze
      Story 3 je nad CSS gridem a je potřeba ji přepsat na novou hierarchii, Story 3X):
  - komponenta v `src\components\specific` (`item-detail-form.tsx`), dynamicky vyrenderuje
    tabulky/sloupce/sekce/atributy dle šablony (viz `layout-templates.md`) a typ hodnoty atributu;
    layout renderer vyčlenit do sdílené komponenty, kterou použije i náhled v editoru šablon,
- [x] Editor projektových šablon v nastavení projektu (`template-editor.tsx`, Story 3Y) —
      přidávání/mazání/přesun tabulek, sloupců (šířka ≥ 1, upozornění když součet ≠ 12), sekcí a atributů;
      globální šablony se needitují.
- [ ] Správa členství a rolí v projektu — UI nad `AssignMember`/`UnassignMember` a novými endpointy
      pro role/výpis členů (sekce "projekty" výše): seznam členů, dialog pro přidání/změnu role,
      odebrání člena.
- [ ] Přihlašovací obrazovka a napojení na JWT autentizaci — čeká na dopracování BE
      autorizace/registrace (zatím jen TODO).
- [ ] Založení projektu — dialog v `projects\index.tsx` je zatím jen mock (`handleCreate` upravuje
      lokální state), je potřeba napojit na `POST /api/v1/projects` (přes `services` vrstvu).
- [ ] `src\components\specific` — zatím prázdné, postupně doplnit doménové komponenty (kanban karta,
      formulář položky dle šablony, editor šablony, správa členů apod.).

## Ostatní

- [ ] Automatizované testy (unit/integration) — v repozitáři zatím žádný testovací projekt neexistuje.
- [ ] `Agilium.Db` obsahuje jen `docker-compose.yml` pro lokální SQL Server — zkontrolovat, zda není
      potřeba doplnit seed data / inicializační skripty.
