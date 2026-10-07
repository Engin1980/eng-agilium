# Task stories

Přeorganizovaná verze `tasks.md` — místo rozpadu podle vrstvy (BE/FE/DB) jde o pořadí podle toho, jak
by šla funkcionalita postupně dodávat a používat z pohledu uživatele: nejdřív založit projekt, pak do
něj přidávat obsah (features/user-story/tasky), pak šablony a detail položky, pak sprinty a kanban,
pak správu členů, a nakonec napříč tím vším autorizaci. U každé story je BE/DB/FE rozpad, protože bez
alespoň minimálního BE/FE není story reálně použitelná. Podrobný technický rozpad (konkrétní routy,
`Command`/`Result` tvary, soubory) zůstává v `tasks.md` — tady je hlavně pořadí a vazby mezi kroky.

Legenda: `[x]` hotovo (zkontrolovat/otestovat), `[ ]` chybí / je potřeba doimplementovat.

## Story -1 — Lokální infrastruktura (DB kontejner)

Úplně první krok, ještě před jakýmkoliv kódem — bez běžící DB se nedá spustit ani `dotnet run`
(`Program.cs` volá `db.Database.Migrate()` při startu), natož testovat další story.

- [x] `Agilium.Db\docker-compose.yml` — SQL Server 2022 kontejner (`eng-agilium-db`), port `1433`,
      heslo z `DB_PASSWORD`, perzistentní volume `eng_agilium_mssql_data`.
- [x] `Agilium.Db\.env` — lokální proměnné (heslo DB) pro `docker compose up`.
- [x] `Agilium.Db\.env.example` — demo verze `.env` s placeholder heslem, sloužící jako vzor pro
      ostatní vývojáře.
- [x] `Agilium.Db\.gitignore` — obsahuje `.env`, takže se reálné heslo negituje (ověřeno —
      `.env` není v `git ls-files`, `.env.example`/`docker-compose.yml`/`.gitignore` ano).

## Story 0 — Úklid modelu před dalším rozvojem

Menší, ale průřezové změny, na kterých staví defaultní data zakládaná v dalších storách (projekt při
založení vytváří workflow stavy a šablony) — proto dává smysl je udělat jako první, ne až dodatečně.

- [x] Odstranit `ItemType.Epic` (`Model\Db\Item.cs`), používat všude jen `Feature` (dle `description.md`).
- [x] Přejmenovat `WorkflowStateType.InProgress` na `.Active` (`Model\Db\WorkflowState.cs`).
- [x] Odstranit `Sprint.ExpectedStartDateTime`/`ExpectedEndDateTime` (`Model\Db\Sprint.cs`) — sprint má
      jen `StartDateTime`/`EndDateTime`.
- [x] Doplnit `TemplateItemType.Checkbox` a `.LabelOnly` (`Model\Db\TemplateItem.cs`).
- [x] Opravit bug v `Features\Projects\Create.cs` → `AddDefaultTemplates`: v obou `foreach` smyčkách se
      `Template.Type` nastavovalo natvrdo na `ItemType.Task` místo na iterovanou proměnnou `type` —
      opraveno na `Type = type`, zároveň odebrán `ItemType.Epic` z pole `highTypes`.
- [x] Bonus nález přímo v `AddProjectRoles` (stejný soubor): role se nikdy nepřidávaly do
      `project.Roles` a `roles.ForEach(roles.Add)` navíc vždy shodil `InvalidOperationException`
      (modifikace kolekce během iterace) — tzn. založení projektu by vždy spadlo. Opraveno na
      `project.Roles.Add(...)`.
- [x] Přidána souhrnná EF Core migrace `Story0_ModelCleanup` pro výše uvedené změny modelu (zachytila i
      starší nedotaženou drift změnu `Projects.Status` → `Projects.State`, viz `Migrations\`).

## Story 1 — Uživatel si založí a spravuje projekty

Základní vstupní bod do aplikace — bez projektu nejde dělat nic dalšího.

- [x] BE: vytvoření projektu vč. výchozích rolí (Owner/Guest), membershipu zakladatele, výchozích
      workflow stavů a výchozích šablon (`Features\Projects\Create.cs`) — po opravě bugu ze Story 0.
- [x] BE: úprava projektu — název/popis (`Features\Projects\Update.cs`).
- [x] BE: změna stavu projektu Active/Inactive (`Features\Projects\UpdateStatus.cs`) + číselník stavů
      (`Features\Projects\StateOptions.cs`).
- [x] BE: výpis projektů, volitelně podle člena (`Features\Projects\List.cs`).
- [x] BE bonus nález: `AddOwnerMembership` (`Features\Projects\Create.cs`) nastavovala `RoleId` na
      `.Id` role, která v tu chvíli ještě nebyla uložená do DB (tedy `0`) → `POST /api/v1/projects`
      vždy spadl na FK violaci. Opraveno nastavením navigation property `Role` místo `RoleId`.
- [x] BE bonus nález: `Features\AppUsers\Create.cs` nepřijímal heslo (`PasswordHash` se natvrdo
      nastavovalo na `string.Empty`), takže se takto vytvořeným uživatelem nedalo nikdy přihlásit.
      Doplněn `Password` do `Command` (min. 8 znaků, `XRegex`), hashování přes `BCrypt.Net.BCrypt.HashPassword`,
      e-mail normalizován na lowercase stejně jako v `Login.cs`.
- [x] FE: `services` vrstva — `src\services\http-client.ts` (fetch wrapper s `credentials:'include'`,
      Authorization header z access tokenu v paměti, tiché obnovení session přes `/auth/refresh` při
      401 a jeden retry), `src\services\auth-api.ts`, `src\services\projects-api.ts` a
      `src\services\projects-queries.ts` (TanStack Query hooky `useProjects`/`useProject`/`useCreateProject`).
- [x] FE: `src\contexts\auth-context.tsx` — `AuthProvider`/`useAuth`; access token se drží jen v paměti
      (ne v `localStorage`, kvůli XSS), session po refreshi stránky se obnovuje tiše přes httpOnly
      `refreshToken` cookie (`SameSite=Strict`). Zapojeno v `main.tsx` spolu s `QueryClientProvider`.
- [x] FE: minimální přihlašovací stránka (`src\routes\login.tsx`) — nutný předpoklad pro založení
      projektu, protože zakladatel musí být `LoggedUser` (viz `AddOwnerMembership`); Story 6
      (kompletní auth/autorizace) zůstává neimplementovaná.
- [x] FE: napojení seznamu projektů (`routes\projects\index.tsx`) na `GET /api/v1/projects` místo
      mock dat; routa je chráněná přesměrováním na `/login`, pokud uživatel není přihlášený.
- [x] FE: napojení dialogu založení projektu (`handleCreate`) na `POST /api/v1/projects` přes
      `useCreateProject` (vč. zobrazení chyby z API a invalidace seznamu po úspěchu).
- [x] FE: routa detailu projektu (`routes\projects\$id\index.tsx`) — zobrazuje název, stav a popis
      projektu (načteno přes `useProject`, zatím bez samostatného BE `GET /projects/{id}` endpointu —
      ten nebyl v `tasks.md` požadován, detail se dohledává z existujícího seznamu).

## Story 2 — Uživatel v projektu buduje strom Feature → User-story → Task/Bug

Jádro "agilní" funkcionality — vytváření a organizace položek.

- [x] BE: vytvoření položky vč. validace projektu a rodiče (`Features\Items\Create.cs`).
- [x] BE: úprava položky — název, typ (`Features\Items\Update.cs`).
- [x] BE: změna rodiče položky vč. kontroly cyklu a příslušnosti k projektu (`Features\Items\UpdateParent.cs`)
      — oprava: původní kontrola cyklu řešila jen přímé `pId == item.Id`, ne cyklus přes delší řetězec
      předků; nahrazeno průchodem od nového rodiče směrem nahoru ke kořeni.
- [x] BE: číselník typů položek (`Features\Items\TypeOptions.cs`).
- [x] BE: výpis položek projektu jako stromu (`Features\Projects\ListItems.cs`) — doplněn příznak
      `IsGeneric` do `ItemResult`, aby FE poznalo automatické kontejnery.
- [x] BE: **lazy vytváření generické feature / generické user-story** (`Features\Items\Create.cs`) —
      `Item.IsGeneric` (nový sloupec, migrace `Story2_ItemsGenericAndHierarchy`), založí se nejvýše
      jedna generická feature/user-story na projekt, líně při první potřebě (generická user-story si
      při svém vzniku stejnou cestou zajistí i generickou feature jako rodiče).
- [x] BE: ochrana generické feature/user-story proti přejmenování (`Update.cs`), přesunu
      (`UpdateParent.cs`) a smazání (`Delete.cs`).
- [x] BE: smazání položky — `DELETE /api/v1/projects/items/{id}` (`Features\Items\Delete.cs`); zvoleno
      kaskádové smazání (položka + všichni potomci + jejich `SprintItem` záznamy, v transakci), protože
      zákaz mazání neprázdné feature/user-story by byl pro reálné použití zbytečně svazující.
- [x] BE: doplněna validace hierarchie Feature → User-Story → Task/Bug při zakládání (`Create.cs`) i při
      přesunu (`UpdateParent.cs`) — Feature nesmí mít rodiče, User-Story musí mít rodiče typu Feature,
      Task/Bug musí mít rodiče typu User-Story (dřív šlo namíchat libovolné typy do libovolné hierarchie).
- [x] BE bonus nález: `Login.cs` a `TokenService.ObtainAppUserByTokenAsync` (refresh) načítaly
      `Membership` bez `.ThenInclude(m => m.Role)` → jakýkoliv uživatel, který byl členem alespoň
      jednoho projektu (tedy i zakladatel po Story 1), dostal při loginu/refreshi `NullReferenceException`
      (500). Opraveno doplněním `ThenInclude`.
- [x] FE: zobrazení stromu features → user-story → tasks v detailu projektu
      (`src\components\specific\item-tree.tsx`, napojeno na `GET /projects/{id}/items`).
- [x] FE: UI pro přidání feature / user-story / task / bug — inline formulář u kořene (jen Feature) a u
      každého uzlu podle dovolených typů potomků (Feature→User-Story, User-Story→Task/Bug).
- [x] FE: UI pro přesun rodiče (`select` s validními cíli dle hierarchie, generické položky nejde
      přesouvat), smazání (potvrzovací dialog, zakázáno pro generické položky) a přiřazení assignee
      (`UpdateParent`/`Delete`/`UpdateAssignee`). Assignee se zatím zadává ručně jako ID uživatele —
      výběr ze seznamu členů projektu čeká na `ListMembers` ze Story 5.

## Story 3 — Uživatel edituje detail položky podle šablony

Navazuje na Story 2 — jakmile existují položky, potřebují editovatelný detail. Toto je zároveň
nejnáročnější story na model (hierarchie tabulka → sloupec → sekce → atribut, viz
`layout-templates.md`) a bez ní nejde plnohodnotně vyplňovat popis/prioritu/atd. u položek.

- [x] DB: přepracovat `TemplateColumn`/`TemplateItem` z pevných sloupců (`WidthWeight`) na CSS grid
      pozicování — `TemplateColumn` odstraněn, `TemplateItem` má `ColumnStart`/`ColumnSpan`/`RowStart`/
      `RowSpan` (1-based, obdoba `grid-column`/`grid-row`) a `Template` má `ColumnCount`
      (migrace `Story3_TemplateGridAndFieldValues`).
- [x] DB: nová entita pro hodnoty polí položky — `ItemFieldValue` (`ItemId`, `TemplateItemId`, `Value`),
      unikátní index `(ItemId, TemplateItemId)`.
- [x] Rozhodnutí návrhu pro univerzální šablonu: `Template.ProjectId` je nyní nullable —
      `ProjectId IS NULL` označuje jedinou globální výchozí šablonu daného `ItemType` (filtrovaný
      unikátní index), vedle stávajícího unikátního `(ProjectId, Type)` pro projektové šablony.
      `AppInitializer` tyto 4 globální šablony idempotentně zakládá při startu
      (`Features\Templates\DefaultTemplates.cs`); úprava globální šablony **nemá** zpětný vliv na už
      založené projekty (ty mají svou vlastní zkopírovanou šablonu).
- [x] BE: CRUD nad projektově specifickou šablonou pro daný `ItemType`
      (`Features\Templates\Get.cs`/`Update.cs`, `GET`/`PUT /api/v1/projects/{id}/templates/{itemType}`).
- [x] BE: CRUD nad univerzální (výchozí) šablonou (`Features\Templates\GetGlobal.cs`/`UpdateGlobal.cs`,
      `GET`/`PUT /api/v1/templates/{itemType}`, nová `BaseRoute.Templates`); validace gridu (duplicitní
      klíče, meze sloupce/řádku, platný typ pole) sdílená mezi oběma variantami přes
      `TemplateGridValidation.ApplyReplaceAsync`.
- [x] BE: čtení/uložení hodnot polí položky dle šablony
      (`Features\Items\GetFieldValues.cs`/`SetFieldValues.cs`, `GET`/`PUT
      /api/v1/projects/items/{id}/fields`) — validace podle `TemplateItem.ValidatingRegex`/
      `TemplateItemType` (celá/desetinná čísla vč. normalizace desetinné čárky, checkbox, zákaz hodnoty
      u `LabelOnly`), kontrola že `TemplateItemId` patří k šabloně daného itemu (jinak by šlo zapisovat
      do polí cizího projektu/typu). `Comments` se zatím ukládá jako prostý text (bez samostatného
      vlákna komentářů — čeká na budoucí story).
- [x] Návazné opravy nutné kvůli `ItemFieldValue` (`DeleteBehavior.Restrict`, žádný FK cascade v tomto
      projektu): `Items\Delete.cs` teď maže i `ItemFieldValue` záznamy celého mazaného podstromu;
      `Items\Update.cs` dovoluje změnu typu položky jen Task↔Bug (jediná záměna, která nerozbije
      hierarchii ze Story 2) a při ní smaže staré hodnoty polí (patřily k jiné šabloně).
- [x] BE bonus nález: `AddDefaultTemplates` (`Features\Projects\Create.cs`) sestavovala
      `TemplateColumn`/`TemplateItem` objekty, které nikdy nebyly součástí sledovaného grafu (nepřidané
      do `Template.TemplateColumns`), takže se přes všech 24 řádků `Templates` v dev DB nikdy neuložil
      ani jeden `TemplateColumn`/`TemplateItem`. Přepsáno na kopírování z globální šablony (viz výše).
- [x] FE: komponenta pro dynamické vykreslení formuláře detailu položky podle šablony
      (`src\components\specific\item-detail-form.tsx`), napojena na novou routu
      `routes\projects\$id\items\$itemId\index.tsx` (odkaz z názvu položky ve stromu ze Story 2).
- [x] FE: editor šablony (`src\components\specific\template-fields-editor.tsx`) — přidání/úprava/
      smazání/přesun polí v gridu (číselné vstupy pro pozici + živý náhled rozložení), obě varianty:
      projektová (`routes\projects\$id\templates\index.tsx`, odkaz z detailu projektu) i globální
      (`routes\templates\index.tsx`, odkaz ze seznamu projektů).

## Story 3X - Update zadání 3

Navazuje na Story 3 -- jedná se o update story 3 po upřesnění zadání.
Jakmile existují položky, potřebují editovatelný detail. Toto je zároveň nejnáročnější story na model (hierarchie tabulka → sloupec → sekce → atribut, viz layout-templates.md) a bez ní nejde plnohodnotně vyplňovat popis/prioritu/atd. u položek.

    DB: přepracovat TemplateColumn/TemplateItem z pevných sloupců (WidthWeight) na hierarchii tabulka → sloupec (šířka, suma 12) → sekce (titulek) → atribut (titulek + hodnota) — viz layout-templates.md a description.md (nahrazuje dřívější plán na CSS grid pozicování).
    DB: nová entita pro hodnoty polí položky (např. ItemFieldValue: ItemId, TemplateItemId, Value) — v Item zatím není žádné úložiště pro reálná data zadaná přes šablonu.
    BE: CRUD nad projektově specifickou šablonou pro daný ItemType (Features\Templates\Get.cs/Update.cs).
    BE: CRUD nad univerzální (výchozí) šablonou nezávislou na projektu — dnes Template.ProjectId je povinné, takže není jasné, kde "globální" výchozí šablona žije; potřeba nejdřív rozhodnout návrh.
    BE: čtení/uložení hodnot polí položky dle šablony (Features\Items\GetFieldValues.cs/SetFieldValues.cs), vč. validace podle TemplateItem.ValidatingRegex/TemplateItemType.
    FE: komponenta pro dynamické vykreslení formuláře detailu položky podle šablony (src\components\specific\item-detail-form.tsx).
    FE: editor šablony (přidání/úprava/smazání/přesun tabulek, sloupců, sekcí a atributů dle layout-templates.md), globální i projektová varianta.


## Story 4 — Uživatel plánuje a řídí práci přes sprinty a kanban

Staví na Story 2 (existující tasky/bugy) — sprint samotný o hodnotách polí (Story 3) nezávisí, dá se
dělat paralelně s ní.

- [ ] BE: CRUD sprintu v rámci projektu (`Features\Sprints\Create.cs`/`Update.cs`/`List.cs`/`Delete.cs`)
      — kontrola jednoznačnosti názvu sprintu v projektu.
- [ ] BE: přiřazení/odebrání tasku/bugu na/ze sprintu (`SprintItem`) —
      `Features\Sprints\AssignItem.cs`/`UnassignItem.cs`; validace, že jde jen o task/bug, ne feature/
      user-story přímo.
- [ ] BE: endpoint pro kanban data sprintu (`Features\Sprints\GetBoard.cs`) — sloupce dle
      `WorkflowState`, položky rozřazené podle aktuálního stavu.
- [ ] BE: posun položky mezi sloupci kanbanu (`Features\Sprints\UpdateItemState.cs`) — volný posun bez
      omezení přechodů.
- [ ] BE: automatické zobrazení feature/user-story ve sprintu, pokud má aspoň jeden vlastní task/bug ve
      sprintu (odvozené pravidlo, promítnout do `GetBoard`).
- [ ] BE: odvození stavu feature/user-story ze stavů podřízených položek (TODO/ACTIVE/DONE podle dětí) —
      napojeno na `SprintItem`, nejdřív ujasnit kontext (stav v rámci jednoho sprintu vs. napříč sprinty).
- [ ] FE: routa se seznamem sprintů projektu.
- [ ] FE: routa/kanban board pro konkrétní sprint — sloupce, karty, přiřazování a posun mezi sloupci
      (drag & drop nebo jednodušší MVP přes select/tlačítka).

## Story 5 — Uživatel spravuje členy a role projektu

Dá se dělat kdykoliv po Story 1 (BE už z velké části existuje) — zařazeno později, protože pro
jednouživatelské testování není blokující, ale je potřeba dřív, než se projekt reálně použije ve více
lidech.

- [x] BE: přiřazení/aktualizace role uživatele v projektu (`Features\Projects\AssignMember.cs`).
- [x] BE: odebrání členství vč. kontroly, že uživatel nemá přiřazené položky (`Features\Projects\UnassignMember.cs`).
- [ ] BE: výpis členů projektu (`Features\Projects\ListMembers.cs`) — dnes dostupné jen nepřímo přes
      `ListItems` (assignee).
- [ ] BE: CRUD vlastních rolí projektu nad rámec Owner/Guest (`Features\Projects\Roles\*.cs`).
- [ ] FE: UI pro seznam členů, přidání/změnu role, odebrání člena (nad `AssignMember`/`UnassignMember`
      a novými endpointy).

## Story 6 — Autentizace a autorizace napříč aplikací

Login/logout/refresh už funguje a stačí pro jednouživatelské testování ostatních story. Reálné
vynucení oprávnění a self-service registrace/reset hesla je průřezová věc, kterou má smysl dotáhnout
až poté, co existují endpointy, na které se oprávnění vážou (Story 1-5) — jinak by se muselo psát
naslepo.

- [x] BE: přihlášení, refresh, odhlášení (`Features\Auth\Login.cs`/`Refresh.cs`/`Logout.cs`).
- [x] BE: vytvoření uživatele (`Features\AppUsers\Create.cs`) — zkontrolovat umístění pod `BaseRoute.Auth`.
- [x] BE: self-service registrace — `Features\AppUsers\Create.cs` (`POST /api/v1/auth/users`) nevyžaduje
      autentizaci a od Story 1 přijímá heslo (viz bonus nález tamtéž), takže funkčně už je to veřejná
      registrace; reset hesla a aktivace/deaktivace účtu zůstávají otevřené (čeká na upřesnění zadání).
- [ ] BE: dopracovat `ValidateRequiredRoles` (`Features\GenericEndpoint.cs`) a osadit endpointy nad
      projektovými daty reálnými požadavky na oprávnění (`CanViewProject`, `CanManageProject`,
      `CanViewMembers`, `CanManageMembers`, `CanManageSprints`).
- [x] FE: přihlašovací obrazovka (`src\routes\login.tsx`, ze Story 1) a nová registrační obrazovka
      (`src\routes\register.tsx`) nad stejným `POST /auth/users` — po úspěšné registraci se uživatel
      rovnou přihlásí a je přesměrován na `/projects`; `/login` a `/register` na sebe teď vzájemně
      odkazují.

## Story 7 — Podpůrné věci (napříč, bez pevného pořadí)

- [ ] Automatizované testy (unit/integration) — v repozitáři zatím žádný testovací projekt neexistuje;
      má smysl začít zakládat průběžně s výše uvedenými story, ne až na konci.
- [ ] `Agilium.Db` obsahuje jen `docker-compose.yml` — zkontrolovat, zda není potřeba doplnit seed data
      / inicializační skripty (užitečné hlavně pro Story 1-2, ať je s čím testovat).
- [ ] `src\components\specific` — postupně doplňovat doménové komponenty vzniklé při Story 2-5 (kanban
      karta, formulář položky dle šablony, editor šablony, správa členů).
