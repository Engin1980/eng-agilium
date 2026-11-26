# Role a oprávnění v projektu

Přehled toho, kdo co smí v rámci projektu. Oprávnění patří **roli**, role patří **projektu**, uživatel má v projektu
právě jednu roli (přes `Membership`). Globální role (např. SuperAdmin) neexistují.

Zdroj: `Model\Db\Role.cs`, `Features\Projects\Create.cs` (stav kódu) a `plan.md` Kroky 2–4 (cílový stav).

## 1. Příznaky oprávnění

| Příznak | Co dovoluje | Stav |
|---|---|---|
| `CanViewProject` | Vidět projekt, jeho položky, šablony, workflow, sprinty, board, backlog | v kódu |
| `CanManageProject` | Změnit stav projektu (aktivní/neaktivní), upravit šablony a workflow | v kódu |
| `CanViewMembers` | Vidět seznam členů, rolí a pozvánek | v kódu |
| `CanManageMembers` | Měnit role členů, odebírat členy, spravovat role, vytvářet/rušit pozvánkové klíče | v kódu |
| `CanManageSprints` | Vytvářet, upravovat a mazat sprinty | v kódu |
| `CanEditItems` | Vytvářet/upravovat/mazat položky, měnit přiřazení, rodiče a hodnoty polí, přiřazovat položky do sprintů a posouvat karty na kanbanu | **plánováno** (Krok 2/3) |
| *Přejmenovat a smazat projekt* | Není příznak: smí jen role se `SystemKind = Owner` | **plánováno** (Krok 2/4) |

## 2. Výchozí role projektu

| | Owner | Manager | Member | Guest |
|---|:-:|:-:|:-:|:-:|
| `CanViewProject` | ✔ | ✔ | ✔ | ✔ |
| `CanViewMembers` | ✔ | ✔ | ✔ | ✔ |
| `CanEditItems` | ✔ | ✔ | ✔ | ✘ |
| `CanManageSprints` | ✔ | ✔ | ✘ | ✘ |
| `CanManageProject` | ✔ | ✔ | ✘ | ✘ |
| `CanManageMembers` | ✔ | ✔ | ✘ | ✘ |
| **Přejmenovat projekt (název, popis)** | ✔ | ✘ | ✘ | ✘ |
| **Smazat projekt** | ✔ | ✘ | ✘ | ✘ |
| Systémová role (nelze smazat/přejmenovat) | ano | ano | ano | ano |

Manager = Owner bez práva projekt přejmenovat a smazat.

Stav kódu dnes: existují jen **Owner** (všechny příznaky) a **Guest** (`CanViewProject`, `CanViewMembers`); role
**Manager**, **Member** a příznak `CanEditItems` se zavádějí v Krocích 2–3. Zakladatel projektu se stává Ownerem.
Projekt může mít i vlastní role s libovolnou kombinací příznaků (Krok 3); právo přejmenovat/smazat projekt ale
vlastní role mít nemohou (je vázané na systémovou roli Owner).

## 3. Co smí která role v aplikaci

| Akce | Potřebné oprávnění | Owner | Manager | Member | Guest |
|---|---|:-:|:-:|:-:|:-:|
| Založit projekt, vidět seznam svých projektů, připojit se klíčem | jen přihlášený | ✔ | ✔ | ✔ | ✔ |
| Zobrazit projekt, strom položek, detail, šablony, workflow | `CanViewProject` | ✔ | ✔ | ✔ | ✔ |
| Zobrazit sprinty, kanban board, backlog | `CanViewProject` | ✔ | ✔ | ✔ | ✔ |
| Vytvořit/upravit/smazat položku, změnit assignee/rodiče, vyplnit pole | `CanEditItems` | ✔ | ✔ | ✔ | ✘ |
| Přiřadit/odebrat položku ve sprintu, posunout kartu mezi sloupci | `CanEditItems` | ✔ | ✔ | ✔ | ✘ |
| Vytvořit/upravit/smazat sprint | `CanManageSprints` | ✔ | ✔ | ✘ | ✘ |
| Změnit stav projektu (aktivní/neaktivní) | `CanManageProject` | ✔ | ✔ | ✘ | ✘ |
| Upravit šablony položek a workflow (sloupce kanbanu) | `CanManageProject` | ✔ | ✔ | ✘ | ✘ |
| Zobrazit členy, role a pozvánky | `CanViewMembers` | ✔ | ✔ | ✔ | ✔ |
| Změnit roli člena, odebrat člena, spravovat role | `CanManageMembers` | ✔ | ✔ | ✘ | ✘ |
| Vytvořit/zrušit pozvánkový klíč | `CanManageMembers` | ✔ | ✔ | ✘ | ✘ |
| Jmenovat Ownera, změnit roli Ownera nebo ho odebrat | jen Owner | ✔ | ✘ | ✘ | ✘ |
| **Přejmenovat projekt (název, popis)** | jen Owner | ✔ | ✘ | ✘ | ✘ |
| **Smazat projekt** | jen Owner | ✔ | ✘ | ✘ | ✘ |

Odpovědi při nedostatku práv: **nečlen** projektu (nebo neexistující projekt) → 404, **člen bez práva** → 403,
nepřihlášený → 401.

## 4. Pravidla a invarianty

- **Pozvánkový klíč** nese roli, kterou získá ten, kdo ho použije; role **Owner** v klíči být nesmí (Ownera lze
  jmenovat jen změnou role existujícího člena, a to jen Ownerem). Role Manager v klíči povolena je. Výchozí
  platnost klíče 7 dní, 10 použití.
- Manager nesmí zasahovat do Ownerů (jmenovat, degradovat, odebrat) — jinak by mohl obejít omezení přejmenování
  a smazání. *(Doplněno mnou jako důsledek zadání; viz `plan.md`, Krok 3.2.)*
- Projekt má vždy aspoň jednoho Ownera a aspoň jednoho člena s `CanManageMembers`.
- Systémové role (Owner, Manager, Member, Guest) nelze smazat ani přejmenovat; Owner nelze degradovat jinak než
  jiným Ownerem.
- Roli s přiřazenými členy nelze smazat; nelze přidělit roli z jiného projektu.
- Oprávnění se berou z DB (přes cache na serveru, která se při změně maže), ne z JWT — změna role či odebrání
  člena platí od dalšího požadavku.
- Oprávnění se vynucují na všech endpointech (Krok 2); správa členů, rolí a pozvánek je hotová (Krok 3). Člen smí
  odejít z projektu sám (`DELETE projects/membership/{id}` na vlastní členství), pokud tím projekt nepřijde o posledního
  Ownera / správce členů a nemá přiřazené položky.
