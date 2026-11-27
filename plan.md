# Plán implementace

Dokument vznikl z `tasks.md`, `task-stories.md`, `description.md`, `layout-templates.md`, `task-todo.md` (dokumentace je v `.docs/development/`)
a z kontroly zdrojového kódu (stav větve `with-sonnet`, 2026-11-19) a obsahuje rozhodnutí zadavatele z
`questions.md` (sekce 1.5). Jde o jediný zdroj pravdy pro **zbývající práci**; `tasks.md` a `task-todo.md` byly
smazány (jejich podstatný obsah je v sekcích 1, 2 a 9). `.docs/development/task-stories.md` zůstává jako podrobná
historie hotových story. Všechny dosavadní otázky jsou vyřešeny (rozhodnutí v 1.5); `questions.md` je prázdný.

Implementaci dělá Claude. Pravidla práce platí z `CLAUDE.md` (Handler/Endpoint pattern, validace jen přes
`Exceptions/Validation`, HTTP konvence, jedna úroveň složek ve `Features`, styl EF dotazů s `//`).

---

## Stav plnění (legenda: `[x]` hotovo, `[ ]` zbývá)

| Krok | Stav |
|---|---|
| 0 Testovací infrastruktura | hotovo |
| 1 Bezpečnost konfigurace | hotovo (zbývá rotace SMTP hesla — uživatel) |
| 2 Autentizace a autorizace | hotovo (drobné FE dluhy) |
| 3 Členové, role, pozvánky | hotovo (chybí FE testy `/join` a assignee) |
| 4 Smazání projektu | hotovo |
| 5 Správa účtu | hotovo |
| 6 Comments | odloženo |
| 7 UX dotažení | volitelné, nezahájeno |
| 8 Bruno a dokumentace | částečně |

---

## 1. Východisko — co je hotové a co jsem při kontrole našel

### 1.1 Hotovo (stručně; detail v `.docs/development/task-stories.md`)

- **Infrastruktura:** `Agilium.Db` (jen SQL Server), kořenový `docker-compose.yml` (`db`, `be`, `fe`),
  Dockerfiles, `Agilium.Http` (Bruno), migrace při startu BE (`db.Database.Migrate()`), globální šablony se
  seedují při startu (`Features\Templates\DefaultTemplates.cs`).
- **BE:** auth (create user = veřejná registrace, login, refresh, logout), projekty (create/list/update/
  update status, assign/unassign member, state options, list items jako strom), položky (create vč. lazy
  generických kontejnerů, update, assignee, parent, delete kaskádově, type options, get/set field values),
  šablony (get/update projektové šablony jako strom), sprinty (CRUD, assign/unassign, board, backlog,
  update item state), workflow (get/update).
- **FE:** `services` vrstva, `auth-context`, login + registrace, seznam projektů, detail projektu se stromem,
  detail položky dle šablony, editor šablon, sprinty + kanban (nativní drag & drop + `select`), editor workflow.

### 1.2 Inventář endpointů (přeneseno z `tasks.md`) — `/api/v1` + route

| Oblast | Endpoint |
|---|---|
| Health | `Features\Health\Ping.cs`, `Db.cs` |
| Auth | `POST auth/login/admin` (`Login`), `POST auth/refresh`, `POST auth/logout`; `POST auth/users` (`AppUsers\Create`, veřejný, heslo min. 8 znaků, bcrypt, e-mail lowercase) |
| Projekty | `POST projects`, `GET projects?memberId=`, `PATCH projects/{id}` (Update), `PATCH projects/{id}/state` (UpdateStatus), `GET projects/state-options`, `GET projects/{id}/items` (strom, `IsGeneric`), `PUT projects/{id}/membership` (AssignMember, upsert), `DELETE projects/membership/{id}` |
| Položky | `POST projects/items` (Create), `PATCH projects/items/{id}` (Update — název a záměna Task↔Bug), `PATCH projects/items/{id}/assignee`, `PATCH projects/items/{id}/parent`, `DELETE projects/items/{id}`, `GET items/type-options`, `GET/PUT projects/items/{id}/fields` |
| Šablony | `GET/PUT projects/{id}/templates/{itemType}` |
| Sprinty | `POST projects/sprints`, `PATCH projects/sprints/{id}`, `GET projects/{id}/sprints`, `DELETE projects/sprints/{id}`, `POST projects/sprints/{id}/items`, `DELETE projects/sprints/{id}/items/{itemId}`, `GET projects/sprints/{id}/board`, `PATCH projects/sprints/{id}/items/{itemId}/state`, `GET projects/{id}/backlog` |
| Workflow | `GET/PUT projects/{id}/workflow` |

Všechny sprint endpointy používají `BaseRoute.Projects` (samostatné `BaseRoute.Sprints` nevzniklo — rozhodnuto).
(Cesty Auth/Projects/Items ověřeny v kódu; u Sprints/Workflow/Templates vychází z `EndpointRoute` souborů.
`BaseRoute.Items` se používá jen u `type-options`.)

### 1.3 Rozhodnutí a pravidla, která jsou v kódu a nesmí se rozbít (přeneseno z `tasks.md`)

- **Generické kontejnery:** nejvýše jedna generická feature a jedna generická user-story na projekt, vznikají
  líně při první osiřelé položce (`Item.IsGeneric`); nelze je přejmenovat, přesunout ani smazat.
- **Hierarchie:** Feature (bez rodiče) → User-Story (rodič Feature) → Task/Bug (rodič User-Story); validace při
  create i při `UpdateParent` (včetně průchodu předky kvůli cyklům).
- **Delete položky:** kaskádově (položka + potomci + jejich `SprintItem` + `ItemFieldValue`), v transakci.
- **Sprint:** task/bug je v právě jednom sprintu (unikátní `SprintItem.ItemId`); přiřazení do jiného sprintu
  položku přesune a zachová stav; do dokončeného sprintu (`Completed`) přiřadit nejde; počáteční stav = první
  ToDo stav projektu; název sprintu unikátní v projektu (`(ProjectId, Title)`); smazat jde jen prázdný sprint;
  nový sprint je `Planned`; kontroluje se jen konec ≥ začátek; posun mezi sloupci je volný.
- **Odvozený stav feature/user-story:** počítá se **v rámci zobrazeného sprintu** z dětí, které v něm jsou
  (všechny ToDo → ToDo, všechny Done → Done, jinak Active; bez dětí ToDo) — `SprintRules.DeriveStatus`,
  projekce v `GetBoard`, nic se neukládá. Mimo sprint (strom projektu) se stav nezobrazuje. Feature/user-story
  se ve sprintu „objeví“ automaticky, má-li v něm aspoň jedno dítě (bez vlastního `SprintItem`).
  Tím jsou uzavřené i dřívější otevřené body „vazba položky na stav mimo sprint“ a „kontext odvozování“.
- **Šablony:** viz `.docs/development/layout-templates.md` a `CLAUDE.md` (sekce Templates). `Update` šablony = full replace
  stromu: prvky s `Id` se aktualizují, bez `Id` vytvoří, chybějící smažou (atribut i s `ItemFieldValue`;
  změna `Type` existujícího atributu = smazání + nový atribut); `Result.DeletedFieldValues`. Validace:
  tabulka ≥ 1 sloupec, šířka ≥ 1, `Key` unikátní v šabloně, platný `Type`; **součet 12 se nevynucuje**.
  Globální šablony se needitují. `POST …/templates/{itemType}/reset` zadavatel nechtěl (nerealizováno).
- **Field values:** validace dle `TemplateItemType` a `ValidatingRegex`, normalizace desetinné čárky, zákaz
  hodnoty u `LabelOnly`, `TemplateItemId` musí patřit k šabloně položky; `Comments` = prostý text.
  `Items\Update` dovoluje změnu typu jen Task↔Bug a maže staré hodnoty polí.
- **Workflow:** min. 2 sloupce, aspoň jeden ToDo a jeden Done (Active volitelný), název ≤ 256, `Id` patří do
  projektu a neopakuje se; zrušený sloupec → položky se přesunou do nejbližšího levého zachovaného (u prvního
  sloupce do pravého); `OrderIndex` se přečísluje; vše v transakci, `Result` = počet přesunutých položek.
- **Auth (stávající stav, mění se v Kroku 2):** access token (JWT, 15 min) v paměti FE, refresh httpOnly cookie
  `SameSite=Strict`, rotace refresh tokenu; role jsou dnes v JWT jako řetězce `P{projectId}:{5 příznaků}`.
- **Hotové opravy z historie** (nevracet): `AddDefaultTemplates` (`Type = type`), `AddProjectRoles` (role se
  přidávaly mimo `project.Roles`), `AddOwnerMembership` (FK na neuloženou roli → nastavuje se navigace),
  `AppUsers\Create` přijímá heslo, `Login`/`TokenService` načítají `Membership` → `Role` (dřívější NRE).

### 1.4 Nálezy z kontroly kódu (nové — vstup do kroků níže)

1. **Žádný endpoint nevyžaduje přihlášení.** V `Program.cs` je jen `UseAuthentication/UseAuthorization`, nikde
   `RequireAuthorization`/kontrola `LoggedUser` (kromě `Projects\Create`, která potřebuje zakladatele).
   `ValidateRequiredRoles` je zakomentovaná a `RequiredRoles` je všude `[]`. Kdokoli bez tokenu tedy čte i
   mění data všech projektů. → **Krok 2 (priorita).**
2. `AssignMember`/`UnassignMember` nekontrolují nic (kdokoli si může přidělit roli Owner). Do `Projects\List`
   lze poslat libovolné `memberId`. → Krok 2/3.
3. **`appsettings.Development.json` není v gitu** (je v `Agilium.Be\.gitignore`, `git log --all` na něj nenajde
   žádnou revizi) — tvrzení v dřívějším `task-todo.md`, že jsou v gitu SMTP údaje, **neplatí**. Reálný problém
   je jiný: `Agilium.Be\.dockerignore` tento soubor **nevylučuje**, takže `COPY . .` v Dockerfile ho zabalí do
   image (a `dotnet publish` ho zkopíruje), pokud na disku existuje. → Krok 1.
4. Commitnutý `appsettings.json` obsahuje výchozí JWT klíč, placeholder Turnstile secret a hostitele SMTP;
   bez přepsání v jiném než lokálním prostředí jsou tokeny padělatelné. Root `docker-compose.yml` navíc běží s
   `ASPNETCORE_ENVIRONMENT=Development` (Turnstile vypnutý). → Krok 1 (startup validace konfigurace).
5. `GlobalExceptionHandler`: větve pro `AccountDeactivated`, `AccountLocked`, `AuthenticationFailedException`
   (→ 401) a `AuthorizationFailedException` (→ 403) jsou **zakomentované** a obě výjimky dědí z
   `BadRequestException` — tj. dnes by skončily jako 400. Před Krokem 2 je nutné mapování zapnout (nebo
   výjimky přeřadit) a `ErrorKeys` doplnit; ověřit, jak dnes `Login` chyby vrací. → Krok 2.
6. `AssignMember` je `PUT` (upsert jedné entity) — v rozporu s konvencí v `CLAUDE.md`; v Kroku 3 se odstraňuje
   (členové přibývají přes pozvánkový klíč, role se mění přes `PATCH`).
7. Žádné testy (BE ani FE), žádné CI (`.github\workflows` je prázdná složka). → Krok 0.
8. Frontend: assignee se zadává ručně jako číselné ID; žádná správa členů; FE neví o oprávněních.
9. `SprintItem`/`WorkflowState` vazby v migracích — dosud neověřeno (otevřený bod z `tasks.md`). → Krok 1.
10. **Životnosti tokenů jsou v konfiguraci obráceně:** `AccessTokenExpirationMinutes = 15`, ale
    `RefreshTokenExpirationMinutes = 7` (refresh platí kratší dobu než access; vypadá to jako překlep za „dny“).
    Důsledek: po 7 minutách nelze session obnovit. → Krok 1/2 (viz 1.5, 2.1).
11. Registrace dnes rovnou přihlásí (FE `register.tsx` volá `login`) a nic neověřuje. → Krok 5.

### 1.5 Rozhodnutí zadavatele (z `questions.md`; vyřešené otázky z něj byly odstraněny)

| Téma | Rozhodnutí |
|---|---|
| DB pro integrační testy | Testcontainers + skutečný SQL Server (`Testcontainers.MsSql`). |
| Oprávnění v projektu | Zdrojem pravdy je DB (členství + role), ne JWT claimy. **Ne dotaz při každém požadavku:** oprávnění se drží v **cache na serveru** (`IMemoryCache`); při změně (členství, role, pozvánka, smazání projektu) se příslušné záznamy z cache smažou a při dalším požadavku se znovu načtou z DB; po restartu serveru se vše líně načte znovu. Změna se projeví okamžitě. Tokeny: access krátký (15 min), refresh dlouhý (7 dní) — překlep v konfiguraci se opraví (Krok 1, 2.1). |
| Položky a posun karet | Nový příznak `CanEditItems`; Owner ano, Guest ne, nová výchozí role **Member**; `ManageSprints` zvlášť; role v JWT claimech se pro autorizaci nepoužívají. |
| Viditelnost projektů | Seznam jen mých projektů, parametr `memberId` zrušen; neviditelný projekt → 404, viditelný bez práva → 403. |
| Přidání člena | **Pozvánkový klíč**: vygeneruje se, rozdá členům; po registraci se uživatel k projektu připojí zadáním klíče. Klíč má limit počtu použití a datum platnosti. Vyhledávání uživatelů (lookup) se **neděje**. Klíč nese roli (ne Owner); výchozí platnost **7 dní**, výchozí počet použití **10**; detaily Krok 3.2. |
| Smazání a přejmenování projektu | Smazání (tvrdé, kaskádou) i úprava názvu/popisu (`Projects\Update`) smí **jen Owner**. Nová systémová role **Manager** má všechny příznaky jako Owner, ale projekt nesmí přejmenovat ani smazat. Přidělit roli Owner, měnit či odebrat Ownera smí jen Owner. Systémové role: Owner, Manager, Member, Guest. |
| Účty | Ověření e-mailu po registraci **ano**; reset hesla a změna hesla ano; **SuperAdmin ne** (žádná role, žádná správa/deaktivace účtů přes API); SMTP údaje v `.env`. |
| Komentáře (`Comments`) | Zatím neimplementovat — TODO po upřesnění zadání (Krok 6). |
| E2E a CI | E2E ne. Testy se pouští lokálně (skript); GitHub workflow jen ručně spouštěný (Krok 0.4). |
| Nasazení a data | `docker-compose.prod.yml`, startup validace konfigurace, volitelný dev seed (`SEED_DEMO_DATA=true`). Cíl nasazení zatím neznámý → obecný `docker-compose.prod.yml` bez reverzní proxy; doplnit až bude cíl znám. |

---

## 2. Průřezová pravidla pro každý krok

- **Pořadí v kroku:** (1) model + migrace → (2) BE logika → (3) BE testy → (4) FE → (5) FE testy → (6) Bruno +
  dokumentace. Krok je hotový, až prochází `dotnet build`, `dotnet test`, `npm run lint`, `npm run build`,
  `npm test` a odpovídající `[x]` jsou zapsané v `.docs/development/task-stories.md`.
- **Migrace:** `dotnet ef migrations add <Story>_<Popis> --project Agilium.Be`; jedna migrace na krok; po
  přidání ověřit `Migrate()` na prázdné DB (součást integračních testů).
- **Každý nový endpoint:** soubor `Features\<Plural>\<Verb>.cs` přesně podle `createEndpoint.prompt.md`;
  validační atributy jen z `Exceptions\Validation` (chybí-li potřebný, přidat nový `X…Attribute`, ne
  `DataAnnotations`); `EntityNotFoundException`/`BadRequestException`/`EntityAlreadyExistsException`.
- **Dokumentace:** po každém kroku aktualizovat `.docs/development/task-stories.md` (zaškrtnout), `.docs/development/description.md` (pokud se
  mění zadání), `CLAUDE.md` (nové příkazy/konvence) a `Agilium.Http` (nové/změněné požadavky).
- **Commity:** po jednom kroku (nebo logické půlce), zpráva s prefixem `feat:`/`fix:`/`test:`/`docs:`; commituje
  se jen na pokyn uživatele.

---

## Krok 0 — Testovací infrastruktura (BE + FE) a lokální spouštění

**Stav: HOTOVO**

- [x] 0.1 `Agilium.Be.Tests` (xUnit, Testcontainers, Respawn, `TestApi`, jednotkové testy)
- [x] 0.2 FE Vitest + Testing Library + MSW
- [x] 0.3 Zpětné pokrytí hotových funkcí (Auth, Projects, Items, Templates, FieldValues, Sprints, Workflow, Migrations)
- [x] 0.4 `scripts	est-all.ps1` / `.sh`, ručně spouštěný `.github\workflows\ci.yml`
- [ ] FE testy: `template-layout`, `sprint-board`, `auth-context` (z výčtu v 0.2 zatím pokryto: `template-draft`, `http-client`, `attribute-input`, `workflow-editor`)


Dnes neexistuje žádný test. Bez nich nelze bezpečně dělat Krok 2 (autorizace zasahuje všechny endpointy).
Prostředí, kde se pracuje, má Docker (28.5), Node 22 a .NET SDK 10 (projekt cílí na net8.0 — před prvním
`dotnet test` ověřit `dotnet --list-runtimes`, případně doplnit runtime 8 nebo nastavit `RollForward`).

### 0.1 Backend — `Agilium.Be.Tests` (xUnit)

- Nový projekt `Agilium.Be.Tests\Agilium.Be.Tests.csproj` (net8.0), přidat do `eng-agilium.sln`.
  Balíčky: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`,
  `FluentAssertions` (nebo `Shouldly`), `Testcontainers.MsSql`, `Respawn`. Skutečný SQL Server je nutný, protože
  kód používá filtrované unikátní indexy, `Serializable` transakce a další specifika SQL Serveru.
- `Program.cs`: přidat na konec `public partial class Program { }` (aby šlo použít `WebApplicationFactory<Program>`).
  `BuildInitializer` volá `DotNetEnv.Env.Load()` a čte `AppSettings` — testy přepisují konfiguraci přes
  `ConfigureAppConfiguration` (connection string z kontejneru, `Security:Turnstile:Enabled=false`,
  zvýšený `RequestLimiter:PermitLimit`, testovací JWT klíč, `IEmailSender` nahrazený fake implementací).
- Fixtures: `SqlServerFixture` (jeden kontejner na test run, `IAsyncLifetime`, `ICollectionFixture`), `ApiFactory`
  (`WebApplicationFactory<Program>`), reset dat mezi testy přes Respawn (zachovat tabulky globálních šablon, nebo
  po resetu znovu zavolat seed).
- Pomocné třídy `TestApi`: `RegisterVerifyAndLoginAsync()` (vrací klienta s Bearer tokenem; po Kroku 5 projde i
  ověřením e-mailu přes fake sender), `CreateProjectAsync()`, `CreateItemAsync(...)`, `CreateSprintAsync(...)`,
  `JoinProjectAsync(key)`, deserializace problem-details do `ProblemDetailsAssert`.
- **Úrovně testů:**
  - *jednotkové* (bez DB): `SprintRules.DeriveStatus`, `SprintRules.EnsureValidDates`, validační atributy
    (`XNonEmpty`, `XRegex`, …), validace stromu šablony (≥1 sloupec, šířka ≥1, duplicitní klíč), normalizace čísel
    ve field values;
  - *integrační* (HTTP + SQL Server): viz 0.3 (zpětné pokrytí hotových funkcí) a další kroky.
- Konvence názvů: `<Feature>Tests` na soubor (`ItemsCreateTests`, `SprintsBoardTests`…), metody
  `Metoda_Situace_Výsledek`.

### 0.2 Frontend — Vitest

- Do `Agilium.Fe` přidat `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/user-event`,
  `@testing-library/jest-dom`, `msw`; skript `"test": "vitest run"` a `"test:watch": "vitest"`; konfigurace v
  `vite.config.ts` (`test: { environment: 'jsdom', setupFiles: './src/test/setup.ts', globals: true }`).
- `src/test/` — `setup.ts` (jest-dom, MSW server start/reset/close), `server.ts` + `handlers.ts` (MSW handlery
  pro `/api/v1`), `render.tsx` (obal `QueryClientProvider` + `AuthProvider` + memory router).
- Co testovat (čistá logika přednostně): `template-draft.ts` (reducer, `validateDraft`, přečíslování
  `OrderIndex`, převod BE ⇄ draft), `http-client.ts` (Bearer hlavička, tiché obnovení po 401 + jediný retry,
  `ApiError` z problem-details), `auth-context`, komponenty `attribute-input`, `template-layout` (poměrné šířky),
  `workflow-editor` (validace a blokace uložení), `sprint-board` (drop → volání `UpdateItemState`).
- E2E testy (Playwright) se nedělají.

### 0.3 Zpětné pokrytí už hotových funkcí (characterization testy BE)

Cíl: zafixovat chování popsané v 1.3, než se začne sahat na autorizaci. (Testy volají API tak, jak dnes funguje,
tj. bez tokenu tam, kde to ještě jde; v Kroku 2 se upraví na přihlášeného klienta — `TestApi` to už zapouzdřuje.)

- `AuthTests`: registrace (heslo < 8 → 400, duplicitní e-mail → 409), login (špatné heslo, zámek po
  `MaxFailedLoginAttempts`, neaktivní účet), refresh (rotace tokenu), logout (zneplatnění).
- `ProjectsTests`: create (vznikne Owner/Guest, membership zakladatele, 3 workflow stavy, 4 projektové šablony
  zkopírované z globálních), update, update status, list, `ListItems` (strom + `IsGeneric`).
- `ItemsTests`: hierarchie (neplatné kombinace → 400), lazy generické kontejnery (jen jeden na projekt),
  ochrana generických (update/parent/delete → 400), `UpdateParent` cyklus, delete kaskádově vč. `SprintItem` a
  `ItemFieldValue`, update typu Task↔Bug maže hodnoty polí, assignee musí být člen projektu.
- `TemplatesTests`: get/update (create/update/delete prvků, `DeletedFieldValues`, duplicitní `Key` → 400, šířka 0,
  prázdná tabulka), editace nezasáhne globální šablonu.
- `FieldValuesTests`: každý `TemplateItemType` (validní/nevalidní hodnota, `ValidatingRegex`, `LabelOnly`),
  cizí `TemplateItemId`.
- `SprintsTests`: CRUD + unikátní název, smazání neprázdného sprintu → 400, assign/unassign/přesun mezi sprinty,
  `Completed` sprint, jen Task/Bug, `GetBoard` (odvozený stav + automatické zobrazení rodičů), `UpdateItemState`
  (cizí stav → 400), `ListBacklog`.
- `WorkflowTests`: validace (méně než 2 sloupce, chybí ToDo/Done, cizí/duplicitní `Id`), přesun položek ze
  zrušených sloupců (prostřední, první, více sousedních), změna typu sloupce se projeví v odvozeném stavu.
- `MigrationsTests`: `Migrate()` na prázdné DB projde, model nemá nezachycený drift (`HasPendingModelChanges`).

### 0.4 Spouštění testů: lokálně a (volitelně) v GitHub Actions

Repozitář je na GitHubu (`Engin1980/eng-agilium`). Chování aplikace ověřuji **lokálně** — mám k dispozici
Docker (Testcontainers), `dotnet` i `npm`, takže testy skutečně spouštím a vidím výsledek; CI k tomu není nutné.

- `scripts\test-all.ps1` (+ `scripts\test-all.sh`): `dotnet test` → `npm ci`/`npm run lint`/`npm run build`/
  `npm test`; návratový kód ≠ 0 při jakékoli chybě. Popsat v `CLAUDE.md`.
- `.github\workflows\ci.yml` se založí, ale **jen s ručním spuštěním** (`on: workflow_dispatch`), takže nespotřebuje
  žádné minuty, dokud ho někdo nespustí; spouštění na push/PR lze zapnout jedním řádkem.
  Poznámka k ceně: GitHub Actions je **zdarma bez omezení minut pro veřejné repozitáře** (standardní runnery);
  u soukromých repozitářů je v bezplatném plánu měsíční limit minut (naposledy mi známá hodnota: 2 000 min/měsíc
  pro Linux) — aktuální podmínky je třeba ověřit na stránce GitHub Billing, nemohu je z prostředí ověřit.

**Hotovo když:** `scripts\test-all.ps1` zelený lokálně, `CLAUDE.md` obsahuje příkazy pro testy.

---

## Krok 1 — Bezpečnost konfigurace, kontejnery, drobné dluhy

**Stav: HOTOVO, kromě úkolu pro uživatele**

- [x] `.dockerignore` (BE i FE) + `scripts\check-be-image.ps1`
- [ ] **Uživatel:** rotace hesla k SMTP účtu z lokálního `appsettings.Development.json`
- [x] SMTP z `.env` (+ Mailpit v `docker-compose.yml`)
- [x] Životnosti tokenů (access 15 min, refresh 7 dní)
- [x] Startup validace konfigurace + testy
- [x] `docker-compose.prod.yml`, `.env.prod.example`, `Dockerfile.prod`
- [x] Dev seed (`SEED_DEMO_DATA`), včetně pozvánkového klíče `demo-invitation-key-1234`
- [x] `MigrationsTests` (prázdná DB, drift modelu, backfill ověření e-mailu)


(přeneseno z `task-todo.md` + nálezy 1.4 č. 3, 4, 9, 10)

- **`.dockerignore`:** přidat do `Agilium.Be\.dockerignore` `appsettings.Development.json`,
  `appsettings.Production.json`, `.env`, `.logs/`; stejně zkontrolovat `Agilium.Fe\.dockerignore` (`.env`).
  Test: sestavit image (`docker build`) a ověřit, že soubor v `/app` není (`docker run … ls`) — jako skript.
- **Rotace hesla k SMTP účtu**, jehož údaje jsou v lokálním `appsettings.Development.json`: pokud byl soubor
  někdy sdílen/zálohován/zabalen do image, heslo změnit. Údaje přesunout do `.env` (viz níže). Tuto část dělá
  uživatel (já nemám přístup k účtu).
- **SMTP z `.env`:** kořenový `.env.example` rozšířit o `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`,
  `SMTP_PASSWORD`, `SMTP_DEBUG_RECIPIENT`; `docker-compose.yml` je předá BE jako
  `AppSettings__Email__Smtp__Host/Port/Username/Password` a `AppSettings__Email__DebugRecipient`. Hostitel SMTP
  se z commitnutého `appsettings.json` odstraní (`(override)`).
- **Životnosti tokenů:** opravit `Security:Tokens` — `AccessTokenExpirationMinutes: 15`,
  `RefreshTokenExpirationMinutes: 10080` (7 dní); komentář/dokumentace jednotek. Viz 2.1.
- **Startup validace konfigurace** (`AppInitializer`/`BuildInitializer`): mimo `Development` selže start, pokud
  JWT klíč je výchozí z `appsettings.json`/kratší než 32 znaků, Turnstile je vypnutý nebo má placeholder secret,
  connection string je `(override)`, SMTP nastavení chybí. Jednotkový test validátoru + integrační test (start
  s výchozí konfigurací v `Production` selže).
- **`docker-compose.prod.yml`:** bez `Development` prostředí, všechny hodnoty ze `.env`, bez publikování portu DB;
  `docker-compose.yml` zůstává pro lokální vývoj (popsat v `CLAUDE.md`). Bez reverzní proxy, healthchecků a
  záloh — cíl nasazení není znám, doplní se později.
- **Dev seed:** `DevSeed` (jen v `Development`, jen při `SEED_DEMO_DATA=true`): předověřený uživatel
  `demo@example.com`, projekt s položkami, sprintem a pozvánkovým klíčem (po Krocích 3 a 5), aby šlo FE ihned
  zkoušet. Idempotentní.
- **Ověřit migrace** `SprintItem` ↔ `WorkflowState` (FK, unikátní `ItemId`, `Restrict` chování) vůči modelu —
  pokryje `MigrationsTests` z 0.3; případný drift opravit novou migrací.

**Testy:** validátor konfigurace (jednotkově), build image bez tajných souborů, `MigrationsTests`, seed
idempotence.

---

## Krok 2 — Autentizace a autorizace napříč API

**Stav: HOTOVO (BE i FE), zbývají dvě drobnosti**

- [x] `EndpointPolicy` na každém endpointu + maticový test reflexí (`EndpointMatrixTests`)
- [x] `IAccessCache` (verze, TTL 5 min, invalidace) a `IProjectAccess`
- [x] Role pryč z JWT, `GlobalExceptionHandler` 401/403/404
- [x] `Projects\Get`, `Projects\List` jen vlastní projekty, `memberId` zrušen
- [x] FE `useProjectPermissions`, skrývání ovládacích prvků, `GET projects/{id}`
- [x] BE testy autorizace (`AuthorizationTests`)
- [ ] FE: sjednotit guard chráněných rout (`beforeLoad`) — zatím se opakuje v každé routě
- [ ] FE: `http-client` — hláška u 403 „Nemáte oprávnění“ + zneplatnění query `project` (zatím jen bez opakování u 401/403/404)
- [ ] FE testy `useProjectPermissions` a guardu rout
- [ ] Bruno: Bearer + záporné autorizační případy (viz Krok 8)


Největší riziko (nález 1.4 č. 1, 2). Dělat hned po Kroku 0.

### 2.1 Návrh (autorizace z DB přes cache na serveru)

**Proč tento návrh (rozhodnutí zadavatele):** Zdrojem pravdy o oprávněních je DB (`Membership` + `Role`), ale
nečte se při každém požadavku — drží se v **cache na serveru** a při změně se příslušný záznam z cache smaže,
takže se při dalším požadavku znovu načte z DB. Odebrání člena nebo změna role se tak projeví okamžitě a na
délce života tokenů nezáleží. Tokeny: access **krátký** (nelze ho odvolat, platí do vypršení), refresh **dlouhý**
(drží přihlášení, lze ho odvolat, protože je v DB); dnešní 15 min / 7 min byl překlep za „dny“ (viz 1.4 č. 10).
FE po změně členství nic nevynucuje, jen znovu načte oprávnění (viz 2.2).

**Cache oprávnění (`IAccessCache`, nad `IMemoryCache`, v paměti procesu BE):**

- **Obsah:** `(userId, projectId)` → `ProjectAccessEntry` (je člen?, `RoleId`, příznaky `Can…`, je-li člen
  `Owner` systémové role); záporný výsledek (nečlen / neexistující projekt) se cachuje také, aby cizí ID
  neprobíjela DB. Zvlášť `userId` → `UserStatus` (aktivní, ověřený e-mail). Při **cache miss** jeden dotaz
  `Memberships.Include(Role)` (resp. uživatel); při pádu/restartu serveru je cache prázdná a plní se líně od
  prvního požadavku.
- **Invalidace (smazání záznamu) po úspěšném commitu** v handleru, který oprávnění změnil:

  | Změna | Invalidace |
  |---|---|
  | `Projects\Join` (nový člen) | `(user, project)` |
  | `Projects\UpdateMemberRole`, `UnassignMember` | `(user, project)` |
  | `Roles\Update`, `Roles\Delete`, `Roles\Create` | celý projekt (změna příznaků zasáhne všechny členy role) |
  | `Projects\Delete` | celý projekt |
  | ověření e-mailu, reset/změna hesla (Krok 5) | `UserStatus` uživatele |

  Celoprojektová invalidace: každý projekt má verzi (čítač) nebo `CancellationTokenSource` připojený ke všem
  jeho záznamům; invalidace = zvýšení verze/zrušení tokenu.
- **Ochrana proti závodu:** čtení z DB si před dotazem zapamatuje verzi projektu a výsledek do cache zapíše jen
  tehdy, pokud se verze mezitím nezměnila (jinak by souběžný požadavek mohl vrátit starý záznam po invalidaci).
  Invalidace se dělá **po** commitu, nikdy před ním.
- **Pojistka:** absolutní expirace záznamu 5 min — kryje změny provedené mimo API (přímý zásah do DB, např.
  `AppUser.IsActive`) a případnou chybu v invalidaci.
- **Omezení (zdokumentovat v `CLAUDE.md`):** cache je lokální pro proces, tedy platí pro **jednu instanci BE**
  (stav v `docker-compose`). Při škálování na více instancí by bylo nutné sdílené úložiště/invalidaci (např.
  Redis pub/sub) — mimo rozsah.

- **Tokeny:** access JWT obsahuje jen identitu (`sub`, `email`) — role `P…` se do tokenu **přestanou dávat**
  (`Login`, `Refresh`, `TokenService`; `Membership` → `Role` se tam už nenačítá). Access 15 min, refresh 7 dní
  (rotace zůstává). Parsování `P…` v `SetUpLoggedUser` a `LoggedUser.IsSuperAdmin`/`RoleAssignments` se odstraní;
  `IRoleAssignment.ToRoleString/FromRoleString` se ponechají jen pokud je něco používá, jinak se smažou i s testy.
- **Služba `IProjectAccess`** (scoped, `AppDbContext`): `GetUserAsync()` (aktivní + ověřený e-mail, jinak
  `AuthenticationFailedException`), `EnsureCanAsync(projectId, ProjectPermission, ct)`. Data bere z
  `IAccessCache` (miss → jeden dotaz do DB, viz výše). Chování:
  nečlen projektu (nebo neexistující projekt) → `EntityNotFoundException` (**404**, neprozradí existenci);
  člen bez příslušného příznaku → `AuthorizationFailedException` (**403**).
- **Přístup z handleru:** `GenericEndpoint.SetUpLoggedUser` předá handleru i `IProjectAccess`
  (`httpContext.RequestServices`); handler volá `await Access.EnsureCanAsync(project.Id, ProjectPermission.X, ct)`
  hned po načtení entity, ze které zjistí `ProjectId` (u sprintu/položky/členství/role/pozvánky se projekt
  zjišťuje z entity, ne z těla požadavku — nelze jej podvrhnout).
- **Autentizace povinná ve výchozím stavu:** do `GenericEndpoint` přidat `virtual bool RequiresAuthentication =>
  true` a **nahradit** jím `RequiredRoles` (globální role neexistují, `SuperAdmin` se neděje); v
  `InvokeProcessRequestAsync` po `SetUpLoggedUser` vyhodit `AuthenticationFailedException` (→ 401), je-li vyžadováno
  a `LoggedUser == null`, a ověřit aktivní/ověřený účet. Explicitně anonymní: `Health\*`, `Auth\Login`,
  `Auth\Refresh`, `Auth\Logout`, `AppUsers\Create`, `Auth\VerifyEmail*`, `Auth\PasswordReset*`.
  Aktualizovat `CLAUDE.md` a `.github\prompts\createEndpoint.prompt.md` (pole `RequiresAuthentication` místo
  `RequiredRoles`) a mechanicky upravit všechny existující endpointy.
- **Chybové odpovědi:** zapnout v `GlobalExceptionHandler` větve pro `AuthenticationFailedException` (401),
  `AuthorizationFailedException` (403), `AccountLocked`/`AccountDeactivated`/nově `EmailNotVerified` (400 s
  `ErrorKey`) a přeřadit `AuthorizationFailedException`/`AuthenticationFailedException` tak, aby nedědily z
  `BadRequestException` (jinak by se zachytily jako 400).
- **`enum ProjectPermission { ViewProject, ManageProject, ViewMembers, ManageMembers, ManageSprints, EditItems }`**
  (`EditItems` je nový příznak `CanEditItems` — model a migrace v Kroku 3.1, ale zavádí se už zde, protože matice
  níže ho potřebuje; Krok 2 a model příznaku se dělají společně v jedné migraci `Story5_Authorization`).
- **`Projects\List`:** vrací jen projekty, kde je volající členem; parametr `memberId` se zruší (BE i
  `projects-api.ts`).
- **`Projects\Get` (nový, přesunuto z původního Kroku 4):** `GET projects/{id}` — `Id`, `Title`, `Description`,
  `State`, `MemberCount` a `Permissions` volajícího (`canViewProject`, `canManageProject`, `canViewMembers`,
  `canManageMembers`, `canManageSprints`, `canEditItems`, `isOwner` — jen Owner smí projekt přejmenovat a smazat). FE z něj bere oprávnění pro UI i detail projektu
  (dnes se detail dohledává ze seznamu).
- **Matice oprávnění:**

| Endpointy | Oprávnění |
|---|---|
| `Projects\Create`, `StateOptions`, `Items\TypeOptions`, `Projects\List`, `Projects\Join` | jen přihlášený |
| `Projects\Get`, `Projects\ListItems`, `Templates\Get`, `Workflow\Get`, `Items\GetFieldValues`, `Sprints\List/GetBoard/ListBacklog` | `ViewProject` |
| `UpdateStatus`, `Templates\Update`, `Workflow\Update` | `ManageProject` |
| `Projects\Update` (název, popis) | jen Owner (`IProjectAccess.EnsureOwnerAsync`, podle `Role.SystemKind`) |
| `Items\Create/Update/UpdateAssignee/UpdateParent/Delete/SetFieldValues` | `EditItems` |
| `Sprints\Create/Update/Delete` | `ManageSprints` |
| `Sprints\AssignItem/UnassignItem/UpdateItemState` | `EditItems` |
| `Projects\ListMembers`, `Roles\List`, `Invitations\List` | `ViewMembers` |
| role CRUD, `Invitations\Create/Revoke`, změna role člena, `UnassignMember` | `ManageMembers` |
| `Projects\Delete` | jen Owner (Krok 4) |

### 2.2 FE

- `http-client.ts`: 401 → pokus o refresh a retry (už je) → jinak odhlášení a přesměrování na `/login`;
  403 → `ApiError` s hláškou „Nemáte oprávnění“ a zneplatnění query `project` (oprávnění se mohla změnit);
  404 u projektu → „Projekt neexistuje nebo k němu nemáte přístup“.
- `useProjectPermissions(projectId)` čte `permissions` z `useProject` (`GET projects/{id}`), krátký `staleTime`
  (např. 30 s) a refetch při focusu okna; tlačítka/odkazy (nastavení, přidání položky, sprint akce) se podle něj
  skrývají; UI jen skrývá, vynucuje BE. Žádný „vynucený refresh tokenu“ po změně členství není potřeba.
- Chráněné routy: sjednotit guard (`beforeLoad` v `__root`/layoutu místo opakování v jednotlivých routách).
- Odstranit `memberId` z `listProjects`.

### 2.3 Testy

- **Maticový test** (BE, parametrizovaný): pro každý endpoint a každou roli (anonym / přihlášený nečlen / Guest /
  Member / Owner) očekávaný status (401 / 404 / 403 / 2xx). Seznam endpointů se bere reflexí z `IEndpoint` —
  **test selže, pokud nový endpoint nemá deklarovanou politiku** (nelze zapomenout na zámek).
- Jednotkově `IProjectAccess` (nečlen → 404, člen bez příznaku → 403, neaktivní/neověřený účet → 401) a
  `IAccessCache`: druhý požadavek nejde do DB (počítání dotazů přes EF interceptor); invalidace `(user, project)`
  i celého projektu vynutí nové načtení; zápis zastaralého výsledku po souběžné invalidaci se zahodí (kontrola
  verze); expirace po 5 min (fake clock); prázdná cache (simulace restartu) → data se načtou znovu a výsledek je
  shodný.
- **Invalidace v každém mutujícím endpointu** z tabulky v 2.1 (Join, UpdateMemberRole, UnassignMember, role
  Create/Update/Delete, Projects\Delete, ověření e-mailu/reset hesla): test provede požadavek s „zahřátou“ cache a
  hned další požadavek musí vidět novou realitu.
- `GlobalExceptionHandler`: 401/403/404 problem-details (tvar odpovědi shodný s ostatními chybami).
- **Okamžité odebrání práv:** člen s platným access tokenem je odebrán / má změněnou roli → hned další požadavek
  vrací 404/403 (bez čekání na vypršení tokenu).
- `Projects\List` vrací jen vlastní projekty; `Projects\Get` vrací správné `Permissions` pro každou roli.
- Tokeny: access JWT neobsahuje `role` claimy; refresh po 8 dnech selže, po 6 dnech projde (čas řízený
  `TimeProvider`/fake clock).
- FE: `http-client` (401 → refresh → retry → logout; 403 hláška + invalidace), `useProjectPermissions`
  (skrytí tlačítek), guard rout.

**Hotovo když:** maticový test zelený pro všechny endpointy, Bruno kolekce používá Bearer a obsahuje záporné
případy, `CLAUDE.md` popisuje konvenci `RequiresAuthentication` + `IProjectAccess`.

---

## Krok 3 — Členové, role a pozvánkové klíče

**Stav: HOTOVO, zbývají FE testy**

- [x] Model: `Role.SystemKind`, `CanEditItems`, role Manager/Member, `ProjectInvitation`, migrace
- [x] BE: `ListMembers`, `UpdateMemberRole`, `UnassignMember` (vč. odchodu sám), `Roles\*`, `Invitations\*`, `Join` (rate limit, souběh)
- [x] BE testy (`MembersAndInvitationsTests`)
- [x] FE: `members-editor`, `roles-editor`, `/join`, výběr assignee ze členů
- [x] FE testy `members-editor` / `roles-editor` / pozvánky
- [ ] FE testy: stránka `/join` (předvyplnění z `?key=`, uchování klíče přes login) a výběr assignee v `item-tree`
- [ ] BE test migrace `SystemKind`/`CanEditItems`/role Member na existující data


(přeneseno z `tasks.md`/`task-stories.md` Story 5; rozšířeno o pozvánky)

### 3.1 Model (jedna migrace společně s Krokem 2 nebo samostatná `Story5b_MembersInvitations`)

- `Role.SystemKind` (enum `None/Owner/Manager/Member/Guest`) — systémové role nelze smazat ani přejmenovat a Owner
  nelze degradovat; migrace nastaví hodnoty existujícím rolím Owner/Guest (podle `Title`).
- `CanEditItems` (viz Krok 2): `IRoleAssignment`, `Role`; `Projects\Create` zakládá Owner (všechny příznaky + výlučné právo
  přejmenovat/smazat projekt, dané `SystemKind`, ne příznakem), **Manager** (všechny příznaky, bez práva
  přejmenovat/smazat), **Member** (`CanViewProject`, `CanViewMembers`, `CanEditItems`) a Guest (`CanViewProject`,
  `CanViewMembers`); migrace doplní existujícím projektům role Manager a Member a nastaví `CanEditItems = true`
  Ownerovi.
- `ProjectInvitation`: `Id`, `ProjectId`, `RoleId`, `KeyHash` (SHA-256 klíče; samotný klíč se neukládá),
  `KeyPrefix` (prvních 6 znaků pro identifikaci v seznamu), `ExpiresAt`, `MaxUses`, `UsedCount`, `CreatedById`,
  `CreatedAt`, `RevokedAt?`; unikátní index `KeyHash`.

### 3.2 BE endpointy (`Features\Roles\`, `Features\Invitations\`, `Features\Projects\`)

- `GET projects/{id}/members` — `Projects\ListMembers`: `MembershipId`, `UserId`, `Name`, `Surname`, `Email`,
  `RoleId`, `RoleTitle` (`ViewMembers`).
- `PATCH projects/members/{membershipId}` — `Projects\UpdateMemberRole` (`Command(RoleId)`; role musí patřit k
  projektu; nelze degradovat posledního člena s `CanManageMembers` ani posledního Ownera; roli Owner smí přidělit a Ownera
  měnit/odebrat jen Owner — Manager na Ownery nesmí). **Nahrazuje** upsert `AssignMember`
  (`PUT`), který se odstraní (členové přibývají jen přes pozvánku).
- `DELETE projects/membership/{id}` — `UnassignMember` zůstává (zákaz u člena s přiřazenými položkami, zákaz
  odebrání posledního vlastníka; člen smí odejít i sám — rozhodnout při implementaci a zdokumentovat).
- Změny členství a rolí volají invalidaci `IAccessCache` (viz tabulka v 2.1).
- Role: `POST projects/{id}/roles` (`Roles\Create`, `Title` + příznaky, unikátní titulek v projektu), `PATCH
  projects/roles/{id}` (`Roles\Update`; systémové jen čtení), `GET projects/{id}/roles` (`Roles\List`),
  `DELETE projects/roles/{id}` (`Roles\Delete`; zákaz u systémové role a u role s aktivním `Membership`).
- **Pozvánky:**
  - `POST projects/{id}/invitations` — `Invitations\Create`: `Command(RoleId, ExpiresAt?, MaxUses?)`; role nesmí
    být Owner (Ownera lze jmenovat jen změnou role existujícího člena); **výchozí hodnoty: platnost 7 dní od
    vytvoření, `MaxUses` = 10** (při vynechání v požadavku; FE je předvyplní); `ExpiresAt` v budoucnu a
    nejvýše +90 dní, `MaxUses` 1–1000; vrací klíč **jednou**
    (`Result(Id, Key, KeyPrefix, ExpiresAt, MaxUses)`); klíč = 24 náhodných bajtů (`RandomNumberGenerator`),
    base64url.
  - `GET projects/{id}/invitations` — `Invitations\List`: prefix, role, `UsedCount/MaxUses`, platnost, stav
    (aktivní / vypršela / vyčerpána / zrušena); klíč se nikdy nevrací.
  - `DELETE projects/invitations/{id}` — `Invitations\Revoke` (`RevokedAt = now`).
  - `POST projects/join` — `Projects\Join`: `Command(Key)`, jen přihlášený a ověřený uživatel; v transakci
    (`Serializable`) najde pozvánku podle hashe, ověří nezrušená/neprošlá/nevyčerpaná, ověří, že uživatel ještě
    není členem (jinak 409), vytvoří `Membership` s rolí pozvánky, zvýší `UsedCount`; vrací `ProjectId`. Všechny
    neplatné případy vrací **stejnou** obecnou chybu 400 „Neplatný nebo prošlý klíč“ (neprozradit důvod);
    zvláštní přísnější rate limit (např. 10 pokusů/min/IP+uživatel) proti hádání klíčů.
- **Invarianty:** projekt má vždy aspoň jednoho člena s `CanManageMembers`; nelze přidělit roli z jiného
  projektu; smazání role s členy zakázáno.

### 3.3 FE

- `routes\projects\$id\settings\members\index.tsx` + `components\specific\members-editor.tsx` (seznam, změna role
  `select`, odebrání s potvrzením, chyba „uživatel má přiřazené položky“) a sekce **Pozvánky** (vytvoření klíče
  s rolí/platností/počtem použití, jednorázové zobrazení klíče s tlačítkem Kopírovat a odkazem
  `{origin}/join?key=…`, seznam, zrušení).
- `routes\projects\$id\settings\roles\index.tsx` + `roles-editor.tsx` (CRUD vlastních rolí, přepínače příznaků,
  systémové role jen ke čtení). Odkazy v `settings\index.tsx`.
- `routes\join.tsx`: pole pro klíč (předvyplněné z `?key=`); nepřihlášený uživatel je přesměrován na `/login`
  nebo `/register` a klíč se uchová (`sessionStorage`) pro dokončení po přihlášení; po úspěchu přesměrování na
  projekt. Na seznamu projektů tlačítko „Připojit se k projektu“.
- `services\members-api.ts`, `members-queries.ts` (`useMembers`, `useChangeMemberRole`, `useRemoveMember`,
  `useRoles`, `useInvitations`, `useCreateInvitation`, `useRevokeInvitation`, `useJoinProject`); po změnách
  invalidace `project` (oprávnění) a `members`.
- Nahradit ruční zadání assignee číselným ID v `item-tree.tsx` výběrem ze členů projektu (`useMembers`).

### 3.4 Testy

- BE: list members (tvar, oprávnění), změna role (cizí role 400, poslední vlastník 400, souběh dvou požadavků),
  role CRUD (unikátní titulek, systémové role nelze měnit/mazat, role s členy nelze smazat), invarianty při
  `UnassignMember`; **pozvánky**: klíč se vrací jen při vytvoření a v DB je jen hash; join úspěšně vytvoří člena
  se správnou rolí a zvýší `UsedCount`; vyčerpání `MaxUses` (souběžně 2 uživatelé na poslední použití → právě
  jeden uspěje), prošlá, zrušená, neznámý klíč → shodná chyba 400; už člen → 409; role Owner v pozvánce → 400;
  rate limit; výchozí platnost 7 dní a `MaxUses` 10 při vynechání; migrace `SystemKind`/`CanEditItems`/role
  Member na existující data.
- FE: `members-editor` (změna role, odebrání, chyba), sekce pozvánek (klíč zobrazen jednou, kopírování,
  zrušení), `roles-editor` (systémová role disabled), `join` (předvyplnění z `?key=`, uchování klíče přes login),
  `item-tree` výběr assignee ze členů.

---

## Krok 4 — Smazání projektu

**Stav: HOTOVO**

- [x] `Projects\Delete` (jen Owner, tvrdé smazání v transakci, invalidace cache)
- [x] BE testy (404/403, žádné osiřelé řádky, druhý projekt nedotčen, globální šablony zůstanou)
- [x] FE sekce „Nebezpečná zóna“ + potvrzovací dialog s opsáním názvu + test


(Detail projektu `GET projects/{id}` se dělá už v Kroku 2.)

- `DELETE projects/{id}` — `Projects\Delete`: povoleno **jen členovi se systémovou rolí Owner**
  (`Role.SystemKind == Owner`; Manager ne), ostatním 403 (nečlen 404). Tvrdé smazání v jedné transakci kaskádou ručně
  (v modelu je `DeleteBehavior.Restrict`): `ItemFieldValue` → `SprintItem` → `Item` (od listů ke kořenům, resp.
  hromadně přes `ProjectId`) → `Sprint` → `TemplateItem` → `TemplateSection` → `TemplateColumn` → `TemplateTable`
  → `Template` → `WorkflowState` → `ProjectInvitation` → `Membership` → `Role` → `Project`.
  Pořadí ověřit proti FK v `AppDbContextModelSnapshot`.
- FE: tlačítko „Smazat projekt“ v nastavení projektu viditelné jen Ownerovi; potvrzovací dialog s opsáním názvu
  projektu; po smazání návrat na seznam projektů a invalidace cache.
- Neaktivní projekt (`UpdateStatus` → Inactive) zůstává beze změny chování (mutace se neblokují).
- **Testy:** nečlen 404, Guest/Member/Manager 403, Owner uspěje (totéž pro přejmenování přes `Projects\Update`); po smazání nezůstane **žádný** osiřelý řádek v žádné
  tabulce (test počtů řádků všech souvisejících tabulek před/po, včetně projektu s položkami, sprinty, field
  values, pozvánkami a vlastními rolemi); smazání jednoho projektu nezasáhne druhý projekt; globální šablony
  zůstanou; FE dialog (tlačítko disabled, dokud se neopíše název).

---

## Krok 5 — Správa účtu: ověření e-mailu, reset hesla, změna hesla

**Stav: HOTOVO**

- [x] `IEmailSender` (MailKit) + `FakeEmailSender`, české šablony, Mailpit
- [x] Migrace `Step5_EmailVerification` (`EmailVerifiedAt`, existující uživatelé ověřeni), `TokenType.EmailVerification`
- [x] Registrace s ověřením (shodná odpověď pro existující e-mail), `EMAIL_NOT_VERIFIED` při loginu
- [x] `auth/verify-email`, `auth/verify-email/resend`, `auth/password-reset/request|confirm`, `auth/password`
- [x] BE testy (`AccountTests`)
- [x] FE: registrace bez automatického loginu, `/verify-email`, `/forgot-password`, `/reset-password`, `/account`, znovuodeslání na loginu
- [x] FE testy formulářů účtu
- [ ] FE test: tok s uloženým pozvánkovým klíčem přes celé přihlášení (pokryto jen text odkazu v `VerifyEmailResult`)
- [ ] Ověřit odeslání přes skutečný SMTP / Mailpit v běžícím stacku (testy používají jen `FakeEmailSender`)


Bez role SuperAdmin a bez správy/deaktivace účtů přes API (`AppUser.IsActive` zůstává, mění se jen přímo v DB).

- **SMTP služba:** `Services\IEmailSender` + `SmtpEmailSender` (MailKit, nastavení `AppSettings.Email.Smtp`
  z `.env`, viz Krok 1; respektovat `Email:DebugRecipient`); v testech `FakeEmailSender` (zachytává zprávy). V
  `Program.cs` odkomentovat/nahradit zakomentované `SmtpService`/`SmartEmailService` registrací `IEmailSender`.
  Šablony e-mailů (text + HTML) v češtině, odkazy `{FrontendBaseUrl}/verify-email?token=…` a
  `/reset-password?token=…`. Pro lokální vývoj přidat **Mailpit** do `docker-compose.yml` (SMTP 1025,
  web 8025; výchozí hodnoty v `.env.example` míří na něj, reálný SMTP se přepíše v `.env`).
- **Model (migrace `Story6_EmailVerification`):** `AppUser.EmailVerifiedAt` (nullable `DateTime`); migrace
  nastaví existujícím uživatelům `EmailVerifiedAt = now`; `TokenType.EmailVerification = 3` (stávající `Token`
  entita: `Value`, `Type`, `Expiration`, `AppUserId`; hodnota tokenu se ukládá jako hash).
- **Registrace s ověřením:** `AppUsers\Create` založí uživatele s `EmailVerifiedAt = null`, vytvoří token
  `EmailVerification` (platnost 24 h) a pošle e-mail; **neprozrazuje**, zda e-mail už existuje (u existujícího
  pošle upozornění „účet už existuje“ a odpověď je shodná). `Login` odmítne neověřený účet chybou
  `EmailNotVerified` (400, `ErrorKey`). Nová `POST auth/verify-email` (`token`; jednorázové, po použití smazat;
  prošlý/neznámý → 400) a `POST auth/verify-email/resend` (`email`; vždy 204, max. 1× za 60 s na účet + rate
  limit).
- **Reset hesla:** `POST auth/password-reset/request` (`email`; vždy 204; Turnstile + rate limit; token
  `PasswordReset`, nastavení `PasswordResetTokenExpirationMinutes/Length` už existují) a
  `POST auth/password-reset/confirm` (`token`, `newPassword` min. 8 znaků; token jednorázový, po použití smazat,
  zneplatnit všechny refresh tokeny uživatele, vynulovat `FailedLoginAttemptsCount` a `IsLockedUntil`; reset
  potvrdí i ověření e-mailu, protože prokazuje vlastnictví schránky).
- **Změna hesla přihlášeným:** `POST auth/password` (`currentPassword`, `newPassword`; zneplatní ostatní refresh
  tokeny).
- **FE:** po registraci se uživatel **nepřihlašuje** (změna `register.tsx`), zobrazí se „Zkontrolujte e-mail“ s
  tlačítkem „Poslat znovu“; `routes\verify-email.tsx` (zpracuje `?token=`, po úspěchu odkaz na přihlášení; pokud
  je uložený pozvánkový klíč, pokračuje na `/join`); `routes\forgot-password.tsx`, `routes\reset-password.tsx`,
  odkaz „Zapomenuté heslo“ z `/login`; stránka „Můj účet“ (změna hesla); chyba `EmailNotVerified` na loginu
  nabídne znovuodeslání.
- **Testy:** BE s `FakeEmailSender` — registrace odešle právě jeden e-mail s platným tokenem, neověřený účet se
  nepřihlásí (`EmailNotVerified`), po ověření ano, token je jednorázový a po 24 h vyprší (fake clock), resend
  respektuje limit, registrace existujícího e-mailu má shodnou odpověď a neodhalí účet; reset: neznámý e-mail
  také 204 a neodešle se nic, token funguje jednou, po resetu staré refresh tokeny nefungují a zámek je
  vynulován, prošlý token 400; změna hesla (špatné aktuální heslo 400); migrace označí existující uživatele jako
  ověřené. FE: formuláře (shoda hesel, chyby), `verify-email` (úspěch/neplatný token), tok s uloženým klíčem.

---

## Krok 6 — Komponenta `Comments` — ODLOŽENO (TODO po upřesnění zadání)

**Stav: ODLOŽENO**

- [ ] Čeká na upřesnění zadání


Rozhodnutí zadavatele: **zatím neimplementovat**; až se zadání upřesní, doplní se. Do té doby zůstává chování:
typ `Comments` se ukládá jako prostý text do `ItemFieldValue.Value` a FE ho kreslí jako textarea.

Podklad pro budoucí upřesnění (původní návrh, nezávazný): samostatná entita `ItemComment` (`ItemId`,
`TemplateItemId`, `AuthorId`, `Text`, `CreatedAt`, `EditedAt?`), endpointy `GET/POST
projects/items/{id}/comments`, `PATCH/DELETE projects/items/comments/{commentId}` (autor nebo `ManageProject`),
oprávnění `ViewProject`/`EditItems`, migrace stávajícího textu na první komentář, FE `comments-field.tsx`;
otevřené: editace/mazání, řazení, notifikace, zmínky.

---

## Krok 7 — Dotažení UX a drobné FE mezery (volitelné)

**Stav: NEZAHÁJENO (volitelné)**

- [ ] Sbalitelné sekce v detailu položky
- [ ] Drag & drop v editoru šablon
- [ ] Mapování chyb BE na prvek editoru šablon
- [ ] Optimistické zamykání šablony (`RowVersion`)
- [ ] Přístupnost kanbanu


- Sbalitelné sekce v detailu položky (v `.docs/development/layout-templates.md` označeno „v budoucnu“) — jen pokud se zadá.
- Drag & drop v editoru šablon (dnes ↑/↓/„Přesunout do…“) — případné rozšíření, ne požadavek.
- Mapování chyb BE na konkrétní prvek editoru šablon (cesta v problem-details) — dnes jen jedna hláška nad
  editorem, FE validuje předem (`validateDraft`); realizovat jen při potřebě.
- Optimistické zamykání šablony (`RowVersion`, „šablona se mezitím změnila“) — dnes poslední zápis vyhrává.
- Přístupnost kanbanu (klávesnicový posun karty) — `select` je záložní ovládání, ověřit čtečkou.

---

## Krok 8 — Bruno kolekce a dokumentace (průběžně po každém kroku)

**Stav: ČÁSTEČNĚ**

- [x] Bruno: `7. Members & Invitations`, `8. Account`, `Projects` Get/Update/Delete
- [ ] Bruno: `5. Sprints` (create, list, update, assign, backlog, board, move, unassign, delete)
- [ ] Bruno: `6. Workflow` (get, update, záporné případy)
- [ ] Bruno: záporné autorizační případy (bez tokenu, Guest, nečlen → 404) a Bearer ve všech složkách
- [x] `CLAUDE.md` průběžně aktualizován (Kroky 0–5)
- [ ] `.docs/development/description.md` (autorizace, pozvánky, ověření e-mailu, smazání projektu; komentáře jako TODO)
- [ ] `.docs/development/task-stories.md` — nové story pro Kroky 2–5 (zatím odškrtnuty jen starší položky)
- [ ] `questions.md` smazat/ponechat podle potřeby


- Doplnit `Agilium.Http\5. Sprints` (create, list, update, assign, backlog, board, move, unassign, delete),
  `6. Workflow` (get, update, záporné případy), `7. Members & Invitations` (po Kroku 3), `8. Account` (po
  Kroku 5), záporné autorizační případy (bez tokenu, Guest, nečlen → 404). Skripty po odpovědi ukládají
  proměnné pro navazující požadavky. Doplnit `Agilium.Http\README.md`.
- Aktualizovat `CLAUDE.md` (příkazy `dotnet test`/`npm test`/`scripts\test-all.ps1`, konvence
  `RequiresAuthentication` + `IProjectAccess`, testovací projekty, e-mail/Mailpit, pozvánky), `.docs/development/description.md`
  (autorizace, pozvánky, ověření e-mailu, smazání projektu; komentáře jako TODO), `.docs/development/task-stories.md`
  (zaškrtnout, nové story pro Kroky 0–5).
- `tasks.md` a `task-todo.md` jsou již smazány; `questions.md` smazat, nebude-li potřeba.

---

## 9. Přehled otevřených/odložených bodů přenesených z `tasks.md` a `task-todo.md`

| Bod | Stav / kam |
|---|---|
| Ověřit migraci `SprintItem` ↔ `WorkflowState` | Krok 1 (`MigrationsTests`) |
| Registrace: ověření e-mailu, reset hesla | Krok 5 |
| Deaktivace/aktivace účtu, SuperAdmin | rozhodnuto: ne (jen přímo v DB) |
| Autorizace endpointů (`RequiredRoles`, `ValidateRequiredRoles`, příznaky `Can…`) | Krok 2 — nahrazeno `RequiresAuthentication` + `IProjectAccess` (DB) |
| Smazání projektu | Krok 4 (tvrdé, jen Owner) |
| Role CRUD (`Features\Roles\*` — ne `Projects\Roles`), `ListMembers`, UI členů | Krok 3 |
| Přidání člena | Krok 3 (pozvánkové klíče; `AssignMember` se ruší) |
| Automatizované testy | Krok 0 (+ testy v každém kroku) |
| Seed/inicializační data pro lokální DB | Krok 1 (`SEED_DEMO_DATA`) |
| Reálné SMTP údaje v lokálním `appsettings.Development.json` (soubor **není** v gitu) | Krok 1 — rotace hesla + `.dockerignore` + `.env` |
| Root compose běží jako `Development` (Turnstile vypnutý, dev konfigurace v kontejneru) | Krok 1 (`docker-compose.prod.yml`, validace konfigurace) |
| `POST …/templates/{itemType}/reset` | nerealizováno (zadavatel nestál o něj) |
| Mapování BE chyb šablony na prvek, `RowVersion` šablony | Krok 7 (volitelné) |
| `Comments` jako samostatný prvek | Krok 6 — ODLOŽENO (TODO po upřesnění) |
| Stav položek mimo sprint | uzavřeno: nezobrazuje se (1.3) |
| Globální šablony — editace | uzavřeno: needitují se, seed při startu |
| `BaseRoute.Sprints` | uzavřeno: nevzniklo, používá se `BaseRoute.Projects` |

## 10. Doporučené pořadí a závislosti

`0 → 1 → 2 → 3 → 4 → 5 → (7 volitelně)`, `6` odloženo, `8` průběžně po každém kroku. Krok 2 nesmí začít před
Krokem 0 (maticový test je pojistka). Krok 3 závisí na Kroku 2 (`IProjectAccess`, `CanEditItems`) a join na Kroku 5
(ověřený účet) jen volně — do doby Kroku 5 stačí přihlášený uživatel. Krok 4 závisí na Kroku 3 (`SystemKind`,
tabulka `ProjectInvitation` při mazání). Krok 5 mění `register.tsx` a `Login`, proto se `TestApi` z Kroku 0 od
začátku staví nad „registrace + ověření + login“.

---

## Původní zadání (z původního obsahu tohoto souboru)

> Projdi požadavky definované v `tasks.md` a `task-stories.md`, podívej se na `description.md`. Potom projdi
> zdrojový kód implementovaných částí. Na základě toho vytvoř do tohoto souboru plán implementace v
> jednotlivých krocích — každý krok pokrývá implementaci části aplikace i testů. Co není jasné → `questions.md`
> (dotaz, návrh řešení, místo pro vyjádření). Všechno podstatné z `task-todo.md` a `tasks.md` přesuň sem, aby
> je šlo smazat.
