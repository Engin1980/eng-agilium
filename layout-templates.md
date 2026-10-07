Tento soubor popisuje, jak se bude pracovat s templates pro "Feature", "User Story", "Task" a "Bug".

V popisu je uvedeno, že v projektu jsou prvky feature, která má user-story, která má task nebo bug. Zobrazení detailu těchto položek záleží na "template".

Nyní napíšu, jak to bude (v dokumentech jsou uvedeny starší informace, toto je aktuální informace):

Každá z položek Feature/User story/Task/Bug (od nyní budu nazývat POLOŽKA) bude mít template. Bude existovat jedna default template na celý web, ze které se dělá default template pro projekt. všechny položky v projektu budou zobrazeny pomocí default template projektu.

Template udává, jaké atributy bude konkrétní položka zobrazovat na své stránce detailu.

Rozložení bude následující
- na nejvyšší úrovni se definuje "tabulka", která bude mít libovolný počet sloupců. Každý sloupec má definovanou šířku jako int. Celková šířka sloupců (součet) je 12 (sloupce fungují stejným principem jako bootstrap sloupce).
- těchto tabulek může být několik pod sebou
- do těchto tabulek se definují položky SEKCE. Každá SEKCE má titulek a udaný sloupec, ve kterém se nachází. V jednom sloupci může být více sekcí pod sebou. V budoucnu bude v UI možno sekce vizuálně oddělovat (titulek sekce bude tučně) a případně sbalit (aby nebylo vidět její atributy, viz dále).
- do sekcí se definují atributy. Každý atribut má TITULEK (ve formě textu) a HODNOTU. Atribut se zobrazuje v dané sekci přes celou šířku sloupce.
- HODNOTA může nabývat pouze z výčtu: víceřádkový text, jednořádkový text, celé číslo, desetinné číslo, true/false.


Upřesnění (odsouhlaseno):
- tabulky, sloupce, sekce i atributy mají explicitní `OrderIndex`; prázdná tabulka i prázdná sekce jsou povolené,
- každá tabulka má aspoň 1 sloupec, šířka sloupce je min. 1; součet šířek se předpokládá 12, ale není vynucen (UI jen upozorní),
- atribut je vždy v sekci (titulek sekce smí být prázdný),
- vedle 5 hodnotových typů existují `Comments` (speciální komponenta pro komentáře) a `LabelOnly` (jen popisek bez hodnoty),
- globální šablony jsou dané systémem (zakládají se v DB) a needitují se; při založení projektu se z nich udělá kopie, a ty projektové se v nastavení projektu upravují.

Zapracuj popisy zde do všech dokumentací v rámci tohoto projektu, kde je to relevantní, aby, až to budu chtít implementovat, jsi to dělal podle tohoto popisu. Nechci, ať děláš implementaci, ale chci ať korektně upravíš relevantní soubory (převážně .MD), aby byly konzistentní s tím, co je uvedeno zde.

Pokud něco není jasné, doptej se.