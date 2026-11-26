# Program description

## Funkcionalita 

Cílem programu je udělat něco jako pro projekty agilní nástroj/kanban na vedení tasků.

Základní popis:
* aplikace obsahuje seznam uživatelů
* v systému lze vytvářet projekty, na každém projektu je více uživatelů, uživatel může být na více projektech
* v každém projektu lze vytvářet featury, v nich user-story, v nich dělat tasks a bugs
* v rámci projektu je dělat sprinty a na ně přiřazovat "tasky". přiřazení featur/user-story na sprint závisí na tom, jestli tam je přiřazený alespoň nějaký jeho task.
* tasky mají stavy; dle toho se zobrazují na kanbanu sprintu. Základní stavy jsou "TODO", "ACTIVE" a "DONE". Obdobně, podle toho se dává stav u user-story (pokud jsou všechny jeho tasky TODO, tak je user-story TODO, pokud je alespoň jeden active, tak je user-story active, pokud jsou všechny done, tak je user-story done; obdobně pro features; pokud user-story nemá žádný task, tak je vždy TODO)
* v systému lze pro uživatele vidět seznam projektů; v daném porjektu lze zobrazit buď přehled features - v nich user-story - v nich tasks, nebo pohled přes sprinty - zobrazení všech sprintů a pro konkrétní sprint pro jeho kanban.
* uživatel má k dispozici stránku, na které může editovat detail konkrétní položky (feature / user-story / task/bug) - jaké položky/pole se u dané položky zobrazují a editují, určuje **šablona** (viz `Template`, `TemplateTable`, `TemplateColumn`, `TemplateSection`, `TemplateItem` v `Model\Db`; přesný tvar entit viz `layout-templates.md`). Existuje globální (výchozí) šablona pro každý typ položky (`ItemType`), která je dána systémem (zakládá se v DB při startu, **needituje se** v UI); při vytvoření projektu se z ní udělá kopie, takže každý projekt má vlastní šablony, které se v nastavení projektu upravují. Rozložení šablony je hierarchické: šablona obsahuje jednu nebo víc **tabulek** pod sebou, každá tabulka má aspoň jeden **sloupec** se zadanou šířkou (int, min. 1), kde se předpokládá, že součet šířek sloupců jedné tabulky je 12 (obdoba bootstrap gridu), ale není to vynuceno; do sloupců se umisťují **sekce** (titulek + příslušný sloupec, v jednom sloupci jich může být víc pod sebou) a do sekcí **atributy** (titulek + hodnota, zobrazené přes celou šířku sloupce sekce). Hodnota atributu může nabývat z výčtu: víceřádkový text, jednořádkový text, celé číslo, desetinné číslo, true/false. Vedle toho existují dva speciální typy atributu bez "hodnoty" v tomto smyslu: `Comments` (komplexní prvek pro diskuzi/komentáře k položce) a `LabelOnly` (jen text/popisek bez vstupu). Detailní popis viz `layout-templates.md`.
* autentizace/autorizace uživatelů bude řešena přes uživatelské účty a per-projektová oprávnění (viz `Role`/`Membership`/`IRoleAssignment` v `Model\Db` a JWT autentizace v BE) - konkrétní podoba (registrace, správa hesel, SSO apod.) zatím není definitivně specifikována a bude upřesněna později.

## User stories

* Uživatel může vytvořit projekt (zadává nejméně název)
* Uživatel může do projektu přidat feature
* Uživatel může do projektu přidat user-story. User-story patří pod feature, pokud nemá nadřazenou, tak udělat nějakou generickou feature. Případná generická feature je společná pro všechny user-story bez specifické feature.
* Uživatel může do user-story přidat task nebo bug. Task a bug jsou na stejné úrovni (oba patří pod user-story), ale bug má vlastní, samostatnou šablonu odlišnou od šablony pro task. Pokud task/bug nebude mít nadřazenou user-story, tak se udělá nějaká generická. Tahle generická user-story je společná pro všechny task/bug bez specifické user-story.
* V systému jsou definované šablony pro feature, user-story, task a bug.
* Při vytvoření projektu se udělá lokální kopie šablon feature, user-story, task, bug.
* Uživatel může v nastavení projektu měnit projektové šablony (globální šablony se needitují).
* U každé šablony se definují tabulky (každá aspoň 1 sloupec o šířce min. 1, suma šířek se předpokládá 12), v nich sekce (titulek + sloupec, prázdné sekce i tabulky jsou povolené) a v sekcích atributy (titulek + hodnota). U každého atributu je udáno jeho umístění (tabulka/sloupec/sekce) a typ (víceřádkový text, jednořádkový text, celé číslo, desetinné číslo, true/false, komentáře, nebo jen popisek bez vstupu). Tabulky, sloupce, sekce i atributy mají explicitní pořadí (`OrderIndex`).
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
* **Typy položek** - hierarchie je Feature → User-story → Task/Bug. Typ `Epic` se ruší/slučuje do `Feature` - v systému, pojmenování i v kódu (enum `ItemType`) se používá výhradně "Feature" (žádná zvláštní úroveň nad feature, slovo "Epic" se má odstranit úplně). `Epic` už je z enumu `ItemType` odstraněn (vč. EF migrace).
* **Rozvržení polí šablony** - aktuální a závazné zadání je v `layout-templates.md`; **nahrazuje** dřívější rozhodnutí o pozicování polí pomocí CSS grid (`grid-column`/`grid-row`). Šablona je tvořena jednou nebo víc **tabulkami** pod sebou; každá tabulka má libovolný počet sloupců se zadanou šířkou (int), kde se součet šířek sloupců v rámci jedné tabulky předpokládá 12 (obdoba bootstrap gridu), ale není vynucen (min. šířka sloupce je 1, tabulka má aspoň 1 sloupec; prázdné tabulky a sekce jsou povolené; každá úroveň má `OrderIndex`). Do sloupců se vkládají **sekce** (titulek + příslušný sloupec; v jednom sloupci může být víc sekcí pod sebou; v budoucnu půjde sekce v UI vizuálně oddělit tučným titulkem a sbalit, aby nebyly vidět její atributy). Do sekcí se vkládají **atributy** (titulek + hodnota, zobrazené přes celou šířku sloupce sekce). Cílové entity: `Template` → `TemplateTable` → `TemplateColumn` (`Width`) → `TemplateSection` (`Title`) → `TemplateItem` (atribut); DB model tomuto tvaru odpovídá (Story 3X, CSS grid z dřívější varianty je pryč). Atribut je vždy v sekci (titulek sekce smí být prázdný). **Globální** šablony (`Template.ProjectId IS NULL`) jsou dané systémem a v UI se needitují; editují se jen projektové kopie.
* **Datové typy položek šablony** - dle `layout-templates.md` (aktuální a závazné zadání, **nahrazuje** předchozí řádek) může "hodnotový" atribut nabývat pouze z uzavřeného výčtu pěti typů: víceřádkový text, jednořádkový text, celé číslo, desetinné číslo, true/false. Vedle těchto pěti typů existují ještě dva speciální typy atributu: `Comments` - komplexní prvek, který slouží k zobrazení/zadávání komentářů (diskuze) k položce, a `LabelOnly` - čistě textový popisek/poznámka bez jakéhokoliv vstupu. `TemplateItemType` má přesně těchto 7 hodnot: `SingleLineText`, `MultiLineText`, `Integer`, `Decimal`, `Boolean`, `Comments`, `LabelOnly`. Rozlišení Inline/Nextline odpadlo (titulek atributu se zobrazuje samostatně a hodnota přes celou šířku sloupce sekce), `Checkbox` je sloučen do `Boolean`, `Untemplated` do `LabelOnly`. `Comments` bude mít speciální komponentu pro práci s komentáři (zatím se ukládá jako prostý text), `LabelOnly` se používá jen pro popisek bez hodnoty.
* **Terminologie stavů** - "ACTIVE" a "InProgress" jsou totéž; v systému (kód, enum `WorkflowStateType`) se používá výhradně `Active` (přejmenování z `InProgress` je hotové).
* **Datumy sprintu** - sprint má pouze jeden pár datumů, `StartDateTime`/`EndDateTime` ("od-do" z user stories). Dřívější pole `ExpectedStartDateTime`/`ExpectedEndDateTime` byla z modelu odstraněna (vč. EF migrace), plán/skutečnost se nerozlišuje.

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

Modelové třídy jsou v `Model`, entity mapované do databáze jsou v `Model\Db` (např. `Project`, `Item`, `Sprint`, `SprintItem`, `Template`/`TemplateTable`/`TemplateColumn`/`TemplateSection`/`TemplateItem`, `ItemFieldValue`, `Role`, `Membership`, `WorkflowState`).

### Nasazení / lokální běh

`Agilium.Db\docker-compose.yml` spouští jen SQL Server; kořenový `docker-compose.yml` (s `Dockerfile` v `Agilium.Be` a `Agilium.Fe`) spouští celý stack (`db`, `be`, `fe`). `Agilium.Http` je Bruno kolekce pro ruční testování BE API.

### Frontend

Frontend je React + TypeScript (Vite) aplikace s třívrstvou segmentací:

* **routes** (`src\routes`) - stránky napojené na file-based routing přes TanStack Router; obsahují layout stránky a napojení na data (přes TanStack Query) a komponenty.
* **components** (`src\components`) - `global` obsahuje znovupoužitelné obecné prvky (dialogy, formulářové prvky, loadery apod.), `specific` je určen pro komponenty vázané na konkrétní doménovou funkcionalitu (strom položek, detail položky dle šablony, layout a editor šablony, kanban sprintu, editor workflow).
* **services** (`src\services`) - vrstva volání backendového API (`http-client.ts` + `*-api.ts` a TanStack Query hooky `*-queries.ts` per doména), přes kterou routes/components komunikují s BE.

Hotové je přihlášení a registrace, seznam projektů, detail projektu se stromem položek, detail položky dle šablony, pohled přes sprinty s kanbanem a nastavení projektu (editor šablon, editor workflow).

## Co ještě zbývá

Podrobný stav je v `plan.md` / `task-stories.md`; hlavní otevřené body:

* **Autorizace** - `RequiredRoles` / `ValidateRequiredRoles` v `GenericEndpoint` zatím nevynucuje oprávnění (`Role`, `Membership`, `IRoleAssignment` existují).
* **Členové a role projektu** - BE výpis členů a CRUD vlastních rolí (`Features\Roles\*`), FE UI pro správu členů (nad existujícími `AssignMember`/`UnassignMember`).
* **Účty** - reset hesla a aktivace/deaktivace účtu; konkrétní podoba není specifikována.
* **Smazání projektu** - potvrdit se zadavatelem, zda je potřeba (existuje `UpdateStatus`).
* **`Comments`** - speciální komponenta (diskuze), zatím prostý text.
* **Testy** - žádný testovací projekt zatím neexistuje.
* **Bezpečnost/konfigurace** - viz `plan.md`, Krok 1 (commitnuté SMTP údaje, konfigurace kontejneru mimo lokální vývoj).