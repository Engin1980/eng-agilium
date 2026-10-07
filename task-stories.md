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

> **Pozor:** tato story je hotová ve variantě s CSS gridem. Nové zadání (`layout-templates.md`) grid
> **nahrazuje** hierarchií tabulka → sloupec → sekce → atribut a globální šablony se už **needitují** —
> viz Story 3X (přechod) a Story 3Y (editace projektových šablon). Body níže zůstávají jako historie
> toho, co bylo implementováno.

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

## Story 3X — Přechod šablon na hierarchii tabulka → sloupec → sekce → atribut

Navazuje na Story 3 — po upřesnění zadání (`layout-templates.md`) se CSS grid z Story 3 **nahrazuje**
hierarchií tabulka → sloupec (šířka) → sekce (titulek) → atribut (titulek + hodnota). Hodnoty polí
(`ItemFieldValue`) a obecná kostra detailu položky ze Story 3 zůstávají, mění se model šablony, jeho
endpointy a renderování. Globální šablony se **needitují** (jsou dané systémem), editují se jen projektové.

- [ ] DB: entity `TemplateTable` → `TemplateColumn` (`Width` ≥ 1) → `TemplateSection` (`Title` smí být
      prázdný) → `TemplateItem` (atribut), každá úroveň s `OrderIndex`; odstranit CSS grid pole z
      `TemplateItem` (`ColumnStart/ColumnSpan/RowStart/RowSpan`) a `Template.ColumnCount`.
- [ ] DB: zúžit `TemplateItemType` na `SingleLineText`, `MultiLineText`, `Integer`, `Decimal`, `Boolean`,
      `Comments`, `LabelOnly` (`Checkbox` → `Boolean`, `Untemplated` → `LabelOnly`, Inline/Nextline odpadá).
- [ ] DB: EF migrace nového modelu — bez převodu starých grid dat (stávající šablony a `ItemFieldValue` se
      zahodí, šablony vznikají jen kopírováním z globálních).
- [ ] BE: `DefaultTemplates.cs` (globální šablony) a `Projects\Create.cs` (kopie celého stromu do projektu)
      v novém tvaru.
- [ ] BE: přepsat `Templates\Get.cs`/`Update.cs` na stromovou strukturu (viz `tasks.md`, "Backend — šablony");
      validace sdílená v `TemplateGridValidation` přejmenovat/přepsat (≥ 1 sloupec, šířka ≥ 1, unikátní
      `Key`, platný typ; součet 12 se nevynucuje).
- [ ] BE: odstranit editaci globální šablony (`GetGlobal.cs`, `UpdateGlobal.cs`, `BaseRoute.Templates`).
- [ ] BE: `Items\GetFieldValues.cs`/`SetFieldValues.cs` — upravit na novou strukturu šablony; validace typů
      zůstává, doplnit nové názvy typů.
- [ ] FE: sdílený renderer layoutu (`template-layout.tsx` — tabulky pod sebou, sloupce přes CSS grid s
      `fr` jednotkami podle `Width`, sekce s titulkem, atributy přes celou šířku sloupce) a přepis
      `item-detail-form.tsx` nad ním; komponenty pro jednotlivé typy hodnot, `LabelOnly` jen text,
      `Comments` zatím jako textarea (speciální komponenta později).
- [ ] FE: aktualizovat `templates-api.ts`/`templates-queries.ts` a typy na nový tvar, odstranit globální
      editor (`routes\templates\index.tsx`) a odkaz na něj ze seznamu projektů.
- [ ] FE: původní `template-fields-editor.tsx` (grid editor) se nahradí editorem ze Story 3Y.

## Story 3Y — Editace projektových šablon v nastavení projektu

Navazuje na Story 3X (BE strom šablony + sdílený renderer layoutu). Uživatel s přístupem k projektu může
v nastavení projektu upravit rozložení a atributy šablony pro každý z typů **Feature / User-Story / Task /
Bug** — každý typ má v projektu vlastní kopii šablony (vznikla při založení projektu z globální). Globální
šablony se v UI neupravují. Tato story je převážně FE; BE doplňuje jen to, co 3X nepokrývá.

### Rozhodnutí návrhu

- Úpravy se dělají na **lokálním draftu** celého stromu šablony jednoho typu a uloží se jedním `PUT`
  (atomicky, stejně jako `Templates\Update.cs`). Žádné průběžné ukládání po jednotlivých změnách.
- Každý prvek stromu (tabulka/sloupec/sekce/atribut) má `Id` z BE, nově přidaný dočasné klientské ID
  (`crypto.randomUUID()`), které se při `PUT` neposílá — BE tak pozná, co vytvořit, co aktualizovat
  a co smazat (chybějící prvky = smazané).
- Smazání atributu, který už existoval, smaže i jeho hodnoty u všech položek daného typu (`ItemFieldValue`)
  — před uložením musí uživatel potvrdit (dialog uvádí počet smazaných atributů; skutečný počet smazaných
  hodnot vrací `Update` v `Result`). Změna typu existujícího atributu = smazání + nový atribut, proto je
  v editoru typ u již uloženého atributu **needitovatelný** (u nového ano).
- Součet šířek sloupců = 12 se **nevynucuje** (viz `layout-templates.md`): editor jen zobrazí neblokující
  upozornění u tabulky, jejíž součet ≠ 12. Blokující je jen šířka < 1, tabulka bez sloupce, prázdný titulek
  atributu a duplicitní `Key`.
- Prázdné tabulky a prázdné sekce jsou povolené; titulek sekce smí být prázdný.
- Pořadí (`OrderIndex`) se spravuje pořadím v poli draftu a při uložení se přečísluje 0..n. MVP přesouvání =
  tlačítka ↑/↓ a "Přesunout do…" (select cílové sekce/sloupce/tabulky); drag & drop je případné rozšíření.
- Oprávnění: `RequiredRoles` je zatím prázdné (Story 6); editor má v budoucnu vyžadovat `CanManageProject`.

### BE (drobné doplňky, pokud nejsou ve 3X)

- [ ] `Templates\Update.cs` vrací v `Result` počet smazaných `ItemFieldValue` (pro potvrzovací/hlášku po
      uložení) a při chybě validace vrací problem-details s cestou k vadnému prvku (např. `tables[1]
      .columns[0].width`), aby FE umělo chybu zobrazit u správného prvku.
- [ ] Volitelně: `POST /api/v1/projects/{id}/templates/{itemType}/reset` — obnoví projektovou šablonu z
      globální (smaže hodnoty u atributů, které v globální nejsou). Jen pokud ho zadavatel chce.

### FE — navigace

- [ ] Stránka nastavení projektu `routes\projects\$id\settings\index.tsx` (rozcestník sekcí nastavení;
      odkaz z detailu projektu) a podstránka `routes\projects\$id\settings\templates\index.tsx` se
      záložkami/přepínačem typu položky (Feature / User-Story / Task / Bug), typ v search param
      (`?type=`). Stávající `routes\projects\$id\templates\index.tsx` přesměrovat/odstranit.
- [ ] Při přepnutí typu nebo odchodu ze stránky s neuloženými změnami zobrazit potvrzení (router
      blocker + `beforeunload`).

### FE — editor (`src\components\specific\template-editor\`)

- [ ] `template-editor.tsx` — kontejner: načte šablonu (`useProjectTemplate`), drží draft ve `useReducer`,
      počítá `isDirty`, liší tlačítka Uložit / Zahodit změny, zobrazí chyby z API, po úspěchu
      invaliduje query šablony i `fields` položek daného typu.
- [ ] `template-draft.ts` — typy draftu, převod BE ⇄ draft, reducer s akcemi (add/remove/move/update pro
      tabulku, sloupec, sekce, atribut), `validateDraft()` (blokující chyby + neblokující upozornění),
      přečíslování `OrderIndex`. Čistá logika, bez React závislostí → snadno testovatelná.
- [ ] `table-editor.tsx` — hlavička tabulky (přidat sloupec, přesunout nahoru/dolů, smazat tabulku —
      i neprázdnou, po potvrzení), upozornění na součet šířek ≠ 12, plus tlačítko "Přidat tabulku" pod
      posledním.
- [ ] `column-editor.tsx` — vstup pro šířku (int ≥ 1), přidat sekci, přesun sloupce doleva/doprava, smazání
      sloupce (nejde smazat poslední sloupec tabulky; neprázdný jen po potvrzení).
- [ ] `section-editor.tsx` — titulek sekce (smí být prázdný), přidat atribut, přesun nahoru/dolů, "Přesunout
      do…" (jiný sloupec/tabulka), smazání sekce (neprázdná po potvrzení).
- [ ] `attribute-editor.tsx` — titulek, `Key` (generuje se z titulku, u existujícího atributu
      needitovatelný), typ (7 hodnot z `TemplateItemType`, u uloženého needitovatelný), volitelný
      `ValidatingRegex` (jen u textových typů), přesun nahoru/dolů / do jiné sekce, smazání (u uloženého
      atributu varování o smazání hodnot).
- [ ] Živý náhled — přepínač "Editace / Náhled", náhled používá stejný `template-layout.tsx` jako
      detail položky (prázdný nevyplněný formulář), takže editor a skutečný detail se nerozejdou.
- [ ] Dialogy: potvrzení smazání (atribut s hodnotami, neprázdná sekce/sloupec/tabulka), souhrnné
      potvrzení při `Uložit`, pokud draft odstraňuje existující atributy ("Smazáním atributů přijdete o
      jejich hodnoty u všech položek typu X"), využít `components\global\Dialog`.
- [ ] Přístupnost a UX: popisky `aria-label` u ikonových tlačítek, focus po přidání prvku na jeho první
      vstup, klávesové ovládání přesunu, stavy načítání/chyby přes `Working`.

### Pořadí implementace

1. 3X kompletně hotová (BE strom + renderer layoutu) — bez toho nemá editor nad čím stát.
2. `template-draft.ts` (typy, reducer, validace) + jednotkové testy (pokud už existuje testovací projekt
   z Story 7; jinak aspoň ručně ověřit okrajové případy níže).
3. Statický editor: načtení → vykreslení stromu bez úprav → náhled.
4. Úpravy po úrovních: atributy → sekce → sloupce → tabulky; vždy s validací a přesunem.
5. Uložení (`PUT`), mapování chyb na prvky, potvrzovací dialogy, blokování odchodu s neuloženými změnami.
6. Navigace (stránka nastavení, přepínač typu), úklid starých rout a komponent.

### Okrajové případy k ověření

- tabulka bez sloupce / sloupec šířky 0 → blokující chyba, `Uložit` zakázáno,
- součet šířek ≠ 12 → pouze upozornění, uložit jde a layout se vykreslí proporčně,
- prázdná tabulka a prázdná sekce se uloží a v detailu položky se vykreslí bez chyby,
- smazání uloženého atributu → hodnoty zmizí z detailu existujících položek, nové položky ok,
- smazání celé sekce/sloupce/tabulky obsahující uložené atributy → souhrnné potvrzení,
- dva atributy se stejným `Key` → blokující chyba,
- souběžná úprava (jiný uživatel uložil šablonu mezi načtením a uložením) — MVP: poslední zápis vyhrává;
  zvážit `RowVersion` ("šablona se mezitím změnila, načíst znovu") jako rozšíření.

### Mimo rozsah

- Editace globálních šablon (needitují se), speciální komponenta pro `Comments`, sbalování sekcí v detailu
  položky, drag & drop přesun, autorizace editoru (Story 6).

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
