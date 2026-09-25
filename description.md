# Program descripiton

## Funkcionalita 

Cílem programu je udělat něco jako pro projekty agilní nástroj/kanban na vedení tasků.

Základní popis:
* aplikace obsahuje seznam uživatelů
* v systému lze vytvářet projekty, na každém projektu je více uživatelů, uživatel může být na více projektech
* v každém projektu lze vytvářet featury, v nich user-story, v nich dělat tasks a bugs
* v rámci projektu je dělat sprinty a na ně přiřazovat "tasky". přiřazení featur/user-story na sprint závisí na tom, jestli tam je přiřazený alespoň nějaký jeho task.
* tasky mají stavy; dle toho se zobrazují na kanbanu sprintu. Základní stavy jsou "TODO", "ACTIVE" a "DONE". Obdobně, podle toho se dává stav u user-story (pokud jsou všechny jeho tasky TODO, tak je user-story TODO, pokud je alespoň jeden active, tak je user-story active, pokud jsou všechny done, tak je user-story done; obdobně pro features; pokud user-story nemá žádný task, tak je vždy TODO)
* v systému lze pro uživatele vidět seznam projektů; v daném porjektu lze zobrazit buď přehled features - v nich user-story - v nich tasks, nebo pohled přes sprinty - zobrazení všech sprintů a pro konkrétní sprint pro jeho kanban.
* uživatel má k dispozici stránku, na které může editovat detail konkrétní položky (feature / user-story / task/bug) - jaké položky/pole se u dané položky zobrazují a editují, určuje **šablona** (viz `Template`, `TemplateColumn`, `TemplateItem` v `Model\Db`). Existuje univerzální (výchozí) šablona pro každý typ položky (`ItemType`), kterou lze pro konkrétní projekt přizpůsobit/přepsat vlastní šablonou. Šablona definuje pole (`TemplateItem` - klíč, pořadí, typ pole jako `InlineText`, `NextlineTextArea`, `InlineInt`, `Comments` apod., volitelný validační regex), která se pozicují pomocí **CSS grid** (každá položka šablony má vlastní údaje o tom, kam a jak se v gridu vykresluje, obdoba `grid-column`/`grid-row` v CSS).
* autentizace/autorizace uživatelů bude řešena přes uživatelské účty a per-projektová oprávnění (viz `Role`/`Membership`/`IRoleAssignment` v `Model\Db` a JWT autentizace v BE) - konkrétní podoba (registrace, správa hesel, SSO apod.) zatím není definitivně specifikována a bude upřesněna později.

## User stories

* Uživatel může vytvořit projekt (zadává nejméně název)
* Uživatel může do projektu přidat feature
* Uživatel může do projektu přidat user-story. User-story patří pod feature, pokud nemá nadřazenou, tak udělat nějakou generickou feature. Případná generická feature je společná pro všechny user-story bez specifické feature.
* Uživatel může do user-story přidat task nebo bug. Task a bug jsou na stejné úrovni (oba patří pod user-story), ale bug má vlastní, samostatnou šablonu odlišnou od šablony pro task. Pokud task/bug nebude mít nadřazenou user-story, tak se udělá nějaká generická. Tahle generická user-story je společná pro všechny task/bug bez specifické user-story.
* V systému jsou definované šablony pro feature, user-story, task a bug.
* Při vytvoření projektu se udělá lokální kopie šablon feature, user-story, task, bug.
* Uživatel může u šablon měnit jejich nastavení.
* U každé šablony se definují položky šablony. U každé položky je udáno, kam a jak se pozicuje v CSS gridu (obdoba `grid-column`/`grid-row`), jaký má název a jaký má datový typ (číslo, line-text, multi-line text, checkbox, label-only)
* uživatel může v porjektu udělat sprint
* uživatel může do sprintu přiřadit tasky
* každý sprint má kanban, ve kterém jsou sloupce
* defaultně jsou sloupce TODO, Active, Done
* pro projekt lze definovat vlastní sloupce, jejich pořadí a přiřazení do jenoho z typu "todo/active/done"
* každý sprint má datum od-do
* název sprintu v rámci projektu musí být jednoznačný
* uživatel může v rámci sprintu a jeho kanbanu posunovat tasky mezi sloupečky (volně)
* uživatel může vidět pohled buď přes všechny features a jejich user-story a jejich tasks
* uživatel může vidět pohled na všechny sprinty
* uživatel může vidět pro každý sprint pohled na jeho kanban

### Upřesnění k User stories

Následující body upřesňují výše uvedené user stories (odpovědi na otevřené otázky):

* **Generická feature / generická user-story** - na projekt existuje nejvýše jedna generická feature (kontejner pro user-story bez konkrétní feature) a nejvýše jedna generická user-story (kontejner pro tasky/bugy bez konkrétní user-story). Vznikají **líně** (lazy) - až ve chvíli, kdy je poprvé potřeba (vznikne první osiřelá user-story/task), ne automaticky při založení projektu. V UI jsou **viditelné** (uživatel je uvidí jako běžnou položku, např. "Bez feature" / "Bez user-story"), ale nejde je přejmenovat ani smazat - slouží jen jako pevný "koš" pro nezatříděné položky.
* **Typy položek** - hierarchie je Feature → User-story → Task/Bug. Typ `Epic` se ruší/slučuje do `Feature` - v systému, pojmenování i v kódu (enum `ItemType`) se používá výhradně "Feature" (žádná zvláštní úroveň nad feature, slovo "Epic" se má odstranit úplně). Aktuálně je `Epic` v `ItemType` enumu pořád vedená jako samostatná hodnota vedle `Feature` - je potřeba ji z enumu odstranit (vč. EF migrace).
* **Rozvržení polí šablony** - položky šablony (`TemplateItem`) se neumisťují do pevného počtu sloupců (`TemplateColumn` + `WidthWeight`), ale pozicují pomocí **CSS grid** - toto je závazné rozhodnutí, ne jen návrh. Přesný tvar těchto údajů (počet sloupců gridu na šablonu, konkrétní pole pro pozici/rozpětí) je potřeba doladit při implementaci; aktuální DB model (`TemplateColumn.WidthWeight`) tomu ještě neodpovídá a je potřeba ho upravit.
* **Datové typy položek šablony** - současný `TemplateItemType` (`InlineText`, `NextlineText`, `NextlineTextArea`, `InlineInt`, `NextlineInt`, `InlineDouble`, `NNextlineDouble`, `Comments`, `Untemplated`) zůstává beze změny a považuje se za dostatečně jemný nadmnožinový výčet pokrývající kategorie z user stories (číslo, line-text, multi-line text, checkbox, label-only) - checkbox a label-only zatím v enumu chybí a je potřeba je doplnit.
* **Terminologie stavů** - "ACTIVE" a "InProgress" jsou totéž; v systému (kód, enum `WorkflowStateType`) se má používat výhradně `Active`, `InProgress` se má přejmenovat.
* **Datumy sprintu** - sprint má pouze jeden pár datumů, `StartDateTime`/`EndDateTime` ("od-do" z user stories). Aktuální model `Sprint` navíc obsahuje `ExpectedStartDateTime`/`ExpectedEndDateTime` (plánované vs. skutečné datumy) - tato pole se mají z modelu odstranit (vč. EF migrace), plán/skutečnost se nerozlišuje.

## Architektura

* aplikace je tří-vrstvá - agilium.be je backend v .net, agilium.fe je frontent v reactu, agilium.db je projekt pro databázi

### DB
Databáze je ms-sql server. Primární klíče jsou autoincrement int. 

### Backend
Backend je aplikace v .NET (ASP.NET Core minimal API). Endpointy jsou organizované ve `Features` po jednotlivých akcích - každá akce (např. `Features\Items\Create.cs`) obsahuje vždy dvojici `Handler` + `Endpoint`:

* `Handler` dědí z generické třídy `GenericHandler<TCommand, TParameters, TResult>` (`Features\GenericHandler.cs`) a obsahuje samotnou byznys logiku v metodě `HandleAsync`. Pro jednoduché CRUD operace pracuje přímo s `AppDbContext`; pro průřezové věci (tokeny, nastavení, ověření Turnstile captchi apod.) si nechává vstříknout příslušnou třídu ze `Services` (např. `TokenService`, `AppSettingsService`, `TurnstileService`) - tedy `Services` slouží jako pomocná/infrastrukturní vrstva pro handlery, nikoliv jako povinný mezikrok pro všechny handlery.
* `Endpoint` dědí z `GenericEndpoint<TCommand, TParameters, THandler>` (resp. jeho potomků `GenericOkEndpoint`/`GenericCreatedEndpoint` z `Features\GenericEndpoint.cs`) a stará se o namapování HTTP requestu (`Command` = tělo JSON, `Parameters` = query/route parametry) na volání handleru a převod výsledku na HTTP odpověď (200/201/204). Definuje HTTP metodu, routu (`BaseRoute` + `EndpointRoute`) a požadované role.
* Vstupní/výstupní typy jsou jednoduché `record`y (`Command`, `Parameters`, `Result`); pro prázdné případy se používají sdílené typy `EmptyCommand`, `EmptyParameters`, `EmptyResult`, `IdParameters`, `IdResult` (`Features\GenericTypes.cs`).
* Handlery i endpointy se do DI registrují automaticky reflexí nad sestavením (viz `Program.cs`), stačí tedy dodržet výše popsanou strukturu a nový endpoint se sám "zapojí".
* Validace vstupů se dělá vlastními atributy z `Exceptions\Validation` (ne standardními .NET DataAnnotations), chybové stavy se hlásí vyhazováním typových výjimek z `Exceptions` (`EntityNotFoundException`, `BadRequestException`, `ValidationException` atd.), které zpracovává `GlobalExceptionHandler`.

Modelové třídy jsou v `Model`, entity mapované do databáze jsou v `Model\Db` (např. `Project`, `Item`, `Sprint`, `SprintItem`, `Template`/`TemplateColumn`/`TemplateItem`, `Role`, `Membership`, `WorkflowState`).

### Frontend

Frontend je React + TypeScript (Vite) aplikace. Zamýšlená (cílová) architektura je třívrstvá segmentace:

* **routes** (`src\routes`) - stránky napojené na file-based routing přes TanStack Router; obsahují layout stránky a napojení na data (přes TanStack Query) a komponenty.
* **components** (`src\components`) - `global` obsahuje znovupoužitelné obecné prvky (dialogy, formulářové prvky, loadery apod.), `specific` je určen pro komponenty vázané na konkrétní doménovou funkcionalitu (např. kanban karta, detail feature/user-story/tasku, editor šablony).
* **services** - vrstva volání backendového API (fetch/HTTP klient) a mapování na TanStack Query hooky, přes kterou by routes/components měly komunikovat s BE. Tato vrstva zatím v repozitáři neexistuje a je potřeba ji doplnit (viz sekce "Co má AI ještě dodělat" níže).

Aktuální stav frontendu je zatím převážně scaffold/prototyp: routy pracují s mockovanými daty přímo v komponentě (např. `routes\projects\index.tsx`), bez reálného napojení na BE endpointy.

## Co má AI ještě dodělat

Následující body popisují známé mezery mezi zadáním/DB modelem a aktuální implementací; slouží jako backlog pro další práci AI na projektu.

### Backend

* **Sprint endpointy** - v `Model\Db` existují entity `Sprint` a `SprintItem`, ale ve `Features` pro ně zatím neexistuje žádná složka/endpointy. Je potřeba doplnit minimálně:
  * CRUD nad sprinty v rámci projektu (vytvoření, úprava, výpis sprintů projektu, případně smazání/zrušení).
  * přiřazení/odebrání položky (tasku) na/ze sprintu (`SprintItem`) - podle zadání se feature/user-story na sprint "objevuje" automaticky, pokud na něj má přiřazený alespoň jeden svůj task, takže půjde spíš o endpoint pracující nad tasky.
  * endpoint pro načtení kanban dat konkrétního sprintu (položky rozřazené dle stavu/`WorkflowState`).
* **Odvozování stavu feature/user-story** - v modelu (`Item`) zatím není vidět implementace pravidla "stav rodiče = odvozeno ze stavů dětí" popsaného výše v sekci Funkcionalita; je potřeba doplnit logiku (buď při čtení/promítnutí do DTO, nebo jako výpočet při načítání stromu feature → user-story → task).
* **Šablony položek** - entity `Template`/`TemplateColumn`/`TemplateItem` v DB existují, ale ve `Features` zatím nejsou vidět odpovídající endpointy (CRUD nad šablonou projektu/typu položky, načtení šablony pro vykreslení detailu položky, uložení hodnot polí položky dle šablony). Je potřeba domyslet i to, kam/jak se hodnoty jednotlivých `TemplateItem` polí pro konkrétní `Item` reálně ukládají (v `Item` entitě zatím není vidět žádné úložiště pro tyto hodnoty).
* **Autentizace/autorizace** - JWT infrastruktura a role/oprávnění (`Role`, `Membership`, `IRoleAssignment`) v BE existují, ale konkrétní požadavky na registraci/správu uživatelů, reset hesla apod. nejsou definitivně specifikované - bude upřesněno.

### Frontend

* **Napojení na reálné API** - založit `services` vrstvu (HTTP klient + TanStack Query hooky) a nahradit mock data v routách (např. `routes\projects\index.tsx`) reálnými voláními na BE endpointy.
* **Pohled přes sprinty** - chybí stránka/routa se seznamem sprintů projektu a kanban view pro konkrétní sprint (sloupce dle `WorkflowState`, karty = tasky/bugy).
* **Detail položky (feature/user-story/task) dle šablony** - komponenta, která na základě šablony projektu (`Template`/`TemplateColumn`/`TemplateItem`) dynamicky vyrenderuje editovatelný formulář pro danou položku, včetně editace samotné šablony (globální i projektová varianta).
* **Správa členství a rolí v projektu** - UI nad již existujícími BE endpointy `Features\Projects\AssignMember.cs` / `UnassignMember.cs` (přidání/odebrání uživatele z projektu, přiřazení role).
* **Přihlášení/autentizace na FE** - zatím jen jako TODO, dokud nebude upřesněno finální řešení autentizace.
* **`components\specific`** - složka je zatím prázdná; postupně do ní doplňovat doménové komponenty vzniklé při implementaci výše uvedených bodů.