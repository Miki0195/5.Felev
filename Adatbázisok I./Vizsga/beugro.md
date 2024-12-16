<style>
r { color: Red }
o { color: Orange }
g { color: Green }
</style>

# Beugró

## 1. Adott az alábbi _Artist_ tábla

| name      | year | country |
| --------- | ---- | ------- |
| Big Giant | NULL | USA     |

## Mi lesz az alábbi lekérdezés eredménye? SELECT \* FROM Artist WHERE year < 1992 OR year >= 1992;

<g>Üres eredményhalmaz lesz

## 2. Mikor kerül egy sor be az eredménybe egy lekérdezéskor (WHERE)?

<g>Ha a WHERE záradék TRUE értéket ad.

## 3. Mire szolgál a DISTINCT függvény egy összesítésen belül?

<g>Ennek használatával az összesítések előállítása előtt az érintett oszlopból a duplikátumokat kihagyjuk.

## 4. Mi a külső összekapcsolás jellemzője?

<g>A külső összekapcsolás megjegyzi az úgynevezett lógó sorokat, NULL értékkel helyettesítve a hiányzó értékeket.

## 5. Melyek lehetnek a nullérték jelentései a relációk soraiban az alábbiak küzül?

- <g>Hiányzó érték
- <g>Értelmetlen érték

## 6. Melyik tanult záradékokban szerepelhetnek az összesítő (aggregáló) függvények? (Az alkérdésektől eltekintve és az ORDER BY záradékon kívül.)

<g>A SELECT és a HAVING záradékokban.

## 7. Ha egy értéket (NULL értéket is beleértve) NULL-lal hasonlítunk, milyen logikai értéket kapunk?

<g>UNKNOWN

## 8. Adott a _Hallgatók(név, cím)_ relációséma. Az alábbi választási lehetőségek közül melyik lekérdezés eredményében szerepelnek biztosan azok és csak azok a hallgatók, akiknek a neve ‘R’ betűvel kezdődik, és a nevük utolsó előtti karaktere ‘z’?

<g>SELECT \* FROM Hallgatók WHERE név LIKE 'R%z\_';

## 9. Sorolja fel a tanult, lehetséges összesítő (aggregáló) függvényeket! Nagybetűkkel adja meg a válaszokat!

- <g>SUM
- <g>AVG
- <g>COUNT
- <g>MIN
- <g>MAX

## 10. Milyen logikai értékek vannak megengedve az SQL-ben?

- <g>TRUE
- <g>FALSE
- <g>UNKNOWN

## 11. A HAVING feltételére vonatkozó megszorítások (az alkérdésen kívül):

- <g>Csak a GROUP BY záradékban is megtalálható attribútumok jelenhetnek meg összesítési operátor nélkül.
- <g>Összesítések szerepelhetnek itt, amelyekben egy összesítési operátort alkalmazunk egy attribútumra vagy egy attribútumot tartalmazó kifejezésre.

## 12. Ha összesítés is szerepel a lekérdezésben, akkor a SELECT-ben szereplő attribútumokra és az összesítésekre vonatkozó szabályok:

- <g>Összesítések szerepelhetnek itt, amelyekben egy összesítési operátort alkalmazunk egy attribútumra vagy egy attribútumot tartalmazó kifejezésre.
- <g>Csak a GROUP BY záradékban is megtalálható attribútumok jelenhetnek meg összesítési operátor nélkül.

## 13. Mi az eredménye a HAVING záradéknak?

<g>Ha egy csoport nem teljesíti a HAVING után megadott feltételt, nem lesz benne az eredményben.

## 14. Mi a különbség az elsődleges kulcs (PRIMARY KEY) és az egyedi értékű kulcs (UNIQUE) fogalma között:

- <g>Az elsődleges kulcs egyetlen attribútuma sem kaphat NULL értéket. Az egyedi értékű kulcs megszorításánál szerepelhetnek NULL értékek egy soron belül akár több is.
- <g>Egy relációhoz egyetlen elsődleges kulcs tartozhat és több egyedi értékű kulcs megszorítás.

## 15. Válassza ki azokat a tanult, standard (!) SQL utasításokat az alábbiak közül, amelyekkel egy tranzakciót be lehet fejezni!

- <g>COMMIT
- <g>ROLLBACK

## 16. Soroljon fel néhány, megadható attribútum típust!

- Egész szám -> INT/INTEGER
- Valós szám -> FLOAT/REAL
- Rögzített hosszúságú sztring n karakter hosszú -> CHAR(n)
- Változó hosszúságú sztring legfeljebb n karakter hosszú -> VARCHAR(n)
- Naptári idő -> DATE
- Idő -> TIME

## 17. Relációk létrehozására melyik SQL résznyelv szolgál?

<g>Data Definition Language(DDL)

## 18. Melyek igazak a tranzakciókra az alábbiak közül?

- <g>Adatbázis lekérdezéseket, módosításokat tartalmazó folyamat.
- <g>Az általuk tartalmazott utasítások egy "értelmes egészt" alkotnak.

## 19. A globális megszorításokra melyek igazak az alábbiak közül?

- <g>Az adatbázissémához tartoznak a relációsémákhoz és nézetekhez hasonlóan.
- <g>A feltétel tetszőleges táblára és oszlopra hivatkozhat az adatbázissémából.
- <g>Deklarációjuk "CREATE ASSERTION <név> CHECK (<feltétel>);" alakban történik.

## 20. Miért hasznosak a triggerek? (Válassza ki az összes kapcsolódó választ!)

- <g>A globális megszorításokkal sok mindent le tudunk írni, az ellenőrzésük azonban gondot jelenthet.
- <g>Az attribútum- és oszlop-alapú (sorra vonatkozó) megszorítások ellenőrzése egyszerűbb (tudjuk mikor történik), mint a globális megszorításoké, ám ezekkel nem tudunk mindent kifejezni.
- <g>A triggerek esetén a felhasználó mondja meg, hogy egy megszorítás mikor kerüljön ellenőrzésre.

## 21. Válassza ki a sor-alapú (sorra vonatkozó) megszorítások tulajdonságait az alábbiak közül!

- <g>Egy ilyen megszorítás feltételében tetszőleges oszlop és reláció szerepelhet bizonyos limitációval.
- <g>Csak beszúrásnál és módosításnál ellenőrzi a rendszer.
- <g>Egy ilyen megszorítás feltételében más relációk attribútumai csak alkérdésben jelenhetnek meg.
- <g>Deklarációjuk a tábla létrehozásánál "CHECK(feltétel)" alakú (relációs)-séma elemként történik (az attribútumok, a kulcsok és az idegen kulcson deklarációja után).

## 22. Válassza ki az alábbiak közül az összes igaz állítást az attribútum alapú megszorításokra!

- <g>A feltételben csak az adott attribútum neve szerepelhet, más attribútumok (más relációk attribútumai is) csak alkérdésben szerepelhetnek.
- <g>Egy adott oszlop értékeire vonatkozóan tudunk ellenőrzéseket definiálni.
- <g>Egy tábla létrehozásakor az attribútum deklarációjához kell hozzáadni CHECK(<feltétel>) alakban.

## 23. Válassza ki az alábbiak közül az összes megszorítás típust!

- <g>Sor-alapú (sorra vonatkozó) megszorítás
- <g>Idegen kulcs
- <g>Érték-alapú (attribútum-alapú) megszorítás
- <g>Globláis megszorítás

## 24. A funkcionális függőségek jobboldalainak szétvágására <g>VAN</g> általános szabály. <r>(Baloldal szétvágására nincs!!)</r>

## 25. Tegyük fel, hogy egy Y attribútumhalmazra alkalmazzuk a lezárási algoritmust és az jön ki, hogy Y+ az összes attribútumot tartalmazza. Mi igaz Y-ra?

<g>Y biztosan egy szuperkulcs

## 26. Ha vesszük a funkcionális függőségek geometriai reprezentációját (egy reláció összes lehetséges előfordulásaihoz kapcsolódóan), akkor mi lesz igaz A->B és B->C, valamint A->C funkcionális függőség régióira?

<g>A->C-hez kapcsolódó régió tartalmazni fogja az A->B és B->C régiók metszetét.

## 27. Legyen X és Y az R relációnak az attribútumhalmazai, illetve A egy attribútuma. Igaz-e, hogy az XY ->A funkcionális függőség az X ->A funkcionális függőségből minden esetben következik?

<g>Igaz

## 28. Igaz-e az Armstrong-axiómák alapján, hogy ha AB->CD adott, akkor ABE->CDE is teljesül? (Tegyük fel, hogy az A, B, C, D, E valamely R reláció attribútumai.)

<g>Igaz

## 29. K az R reláció kulcsa, … Fejezze be a mondatot az összes helyes módon az alábbi választási lehetőségek közül!

- <g> .. ha nem lehet két olyan R-beli sor, amelyek K attribútumain megegyeznek, valamint nincs olyan valódi részhalmaza K-nak, amely funkcionálisan meghatározná R összes többi attribútumát.
- <g> ha K funkcionálisan meghatározza R attribútumait, de K-ból bárhogy hagyunk el egy attribútumot az már nem fogja funkcionálisan meghatározni R attribútumait.

## 30. R-nek legyenek A, B és C az attribútumai. Feltéve, hogy csak A kulcs, hány szuperkulcsa van R-nek?

<g>4(2<sup>n-1</sup> . n=3)

## 31. Melyek igazak az alábbiak közül a nézettáblákra?

- <g>Nézettáblákat tárolt táblák (alaptáblák) és más  nézettáblák felhasználásával tudunk definiálni.
- <g>Van virtuális és materializált nézettábla is.

## 32. Válassza ki az alábbiak közül a hangolási szakértő által végzett adatbázis hangolásának (database tuning) összes lehetségés lépését!

- <g>A szakértő megvizsgálja a létrehozott indexek hatását (pl. a lekérdezés optimalizáló valóban használja-e ezeket, illetve javul-e a lekérdezések végrahajtási ideje).
- <g>A szakértő létrehozza a szerinte fontos indexeket (a lekérdezés terhelési kimutatás (query load) alapján).
- <g>A tervező átad egy minta lekérdezés terhelést (query load) a szakértőnek.
- <g>Valakik véletlenszerűen lekérdezéseket választanak a korábban végrehajtottak közül, és ezt a lekérdezés terhelési kimutatást (query load) átadják a szakértőnek.

## 33. Melyek igazak az alábbiak közül az indexekre?

- <g>Több mezőre, attribútumra is lehet indexet készíteni.
- <g>Kereséseket, lekérdezések végrehajtását gyorsító adatszerkezetek, segédstruktúrák.

## 34. Mit jelent a függőségek megőrzése?

<g>A vetített relációk segítségével is kikényszeríthetők az előre megadott függőségek.

## 35. Mit jelent a felesleges (redundáns) adat a relációs adatmodell tervezésénél?

<g>Azt, hogy kikövetkeztethető a többi adatból, (főleg) a funkcionális függőségekből.

## 36. 3NF megőrzi a funkcionális függőségeket?

<g>Igaz

## 37. BCNF megszünteti a funkcionális függőségekből eredő redundanciát?

<g>Igaz

## 38. Adott egy R(A,B,C,D) reláció és a rá vonatkozó AD->B, B->C és C->A funkcionális függőségek. Tegyük fel, hogy R relációt felbontottuk S(A,C) és T(A,B,D) relációkra. Igaz-e, hogy B->A is fennáll a T(A,B,D) reláción?

<g>Igaz

## 39. Tegyük fel, hogy az alábbi R(A,B,C) relációt szétvágjuk az alábbi R1(A,C) és R2(B,C) relációkra. Veszteségmentes lesz-e a keletkező relációk összekapcsolása?

<g>Igaz

## 40. Mit jelentenek az anomáliák?

## 41. Egy R(A<sub>1</sub>,...,A<sub>n</sub>) relációt, amikor felbontunk kisebb relációkra: S(B<sub>1</sub>,...,B<sub>m</sub>) és T(C<sub>1</sub>,...,C<sub>k</sub>), akkor az alábbiak közül melyek igazak?


- <g>A T megegyezik az R-nek C<sub>1</sub>,...,C<sub>k</sub> attribútumokra való vetületével. 
- <g>
- <g>

## 42. Miként lehet meghatározni a 3. normálformát (3NF)? Jelölje be az összes jó választ az alábbiak közül!

- <g>R reláció 3. normálformában van, ha minden X -> Y nemtriviális funkcionális függőségre R-ben X szuperkulcs, vagy jobb oldala csak elsődleges attribútumokra tartalmaz.
- <g>R reláció 3. normálformában van, ha minden X -> Y nemtriviális funkcionális függőségre R-ben X szuperkulcs, vagy jobb oldala csak olyan attribútumokat tartalmaz, amelyekre igaz, hogy legalább egy kulcsnak elemei.
- <g>X -> A megsérti a 3NF-t, akkor és csak akkor, ha X nem szuperkulcs és A nem prím (elsődleges attribútum).

## 43. 

## 44. 4NF megszünteti a funkcionális függőségekből eredő redundanciát?
<g>Igaz

## 45. Mi az "Egyed-kapcsolat modell"?
<g>Segítségével az adatbázissémát vázolhatjuk fel.

## 46. Létezhet-e olyan R reláció, amely BCNF-ben van, de nincs 4NF-ben?
<g>Igaz

## 47. Az alábbi közül melyik egy gyenge egyedhalmazra vonatkozó szabály?
<g>A támogató kapcsolat(ok)nak kerek nyílban kell végződniük az egy oldalon (azaz minden entitásnak (egyednek) a gyenge egyedhalmazból pontosan egy egyedhez kell kapcsolódnia a támogató egyedhalmazból).

## 48. Mi igaz az Egyed-kapcsolat modellel kapcsolatos alosztályokra (részosztályokra)?
<g>Ha egy entitás (egyed) szerepel egy alosztályban (részosztályban), akkor szerepel az ősosztály(ok)ban is.

## 49. 3NF megszünteti a többértékű függőségekből eredő redundanciát?
<g>Hamis

## 50. A többértékű függőségre (TÉF) vonatkozó szabályok:
- <g>Minden funkcionális függőség TÉF.
- <g>A TÉF baloldala nem bontható fel.
- <g>A TÉF jobboldala nem bontható fel.
- <g>Ha X ->-> Y és Z jelöli az összes többi (azaz X-en és Y-on kívüli) attribútum halmazát, akkor X ->-> Y.

## 51. Mit jelent az E/K diagramon, hogy ha A egyedhalmaz, B egyedhalmaz és C egyedhalmaz között van egy ternáris kapcsolat és nyíl mutat a C-be?
<g>A kapcsolathalmazban egy sornál az A és a B egyedhalmazokhoz artozó elemek együttese egyértelműenn meghatározza a C egyedhalmazhoz tartozó elemet.

## 52. Mit jelent az E/K diagramon, hogy ha A egyedhalmaz és B egyedhalmaz között van egy kapcsolat és lekerekített nyíl van B-nél (azaz A-ból B-be mutat a lekerekített nyíl, másik irányba nem mutat semmilyen nyíl)?
<g>Minden A halmazbeli entitás pontosan egy entitáshoz kapcsolódhat a B egyedhalmazból.

## 53. Tegyük fel, hogy A egyedhalmaz és B egyedhalmaz között van egy kapcsolat és lekerekített nyíl van B-nél az E/K diagramon (azaz A-ból B-be mutat a lekerekített nyíl). Tanult módon van-e lehetőség másik jelölést használni a lekerekített nyíl helyett?
<g>Igaz

## 54. Helytelen tervezés-e ha a focista egyedhalmaznál a focista neve, TAJ száma, klubja attribútumok mellett a klubja címét, mint a focista egyedhalmaz egy ráadás attribútumát is tároljuk.
<g>Igaz

## 55. Előfordulhat-e olyan E/K diagram, ahol egy gyenge egyedhalmaz egy másik gyenge egyedhalmazhoz kapcsolódik?
<g>Igaz

## 56. Milyen kapcsolat köti össze az egyedhalmazt az alosztályaival?
<g> "az-egy" kapcsolat

## 57.

## 58.

## 59. Melyek a tervezési technikák, ökölszabályok egyed (entitás) kapcsolat modell esetében az alábbiak közül?
- <g>A gyenge egyedhalmazok óvatos használata.
- <g>Ne használjunk egyedhalmazt, ha egy attribútum éppúgy megfelelne a célnak.
- <g>Redundancia elkerülése.

## 60. Az alábbiak közül válassza ki azokat, amelyek egy egyed (entitás)-kapcsolat modell relációsémává történő átírásának szabályai!
- <g>Egy egyedhalmaznak egy reláció felel meg, melynek neve megegyezik az egyedhalmaz nevével, attribútumai az egyedhalmaz attribútumai.
- <g>Gyenge egyedhalmazok esetében a kapott relációhoz hozzá kell még venni azokat az attribútumokat, amelyek egyértelműen azonosítják az egyedhalmazt.
- <g>Egy kapcsolatnak szintén egy relációt feleltetünk meg, melynek neve a kapcsolat neve, attribútumai pedig a kapcsolatban részt vevő egyedhalmazok kulcsai. Amennyiben két attribútum neve megegyezne, egyiket értelemszerűen át kell neveznünk.

## 61. Az alábbiak közül milyen feltételek vonatkozhatnak egy egyedhalmazra?
- <g>Többnek kell lennie, mint egy egyszerű név, azaz legalább egy nem kulcs attribútumnak lennie kell.
- <g>A "sok" végén szerepel egy sok-egy kapcsolatnak.
- <g>Egy egyedhalmaz hasonló egyedek (entitások) kollekciója.

## 62. CREATE TYPE BarType AS ( name CHAR(20), addr CHAR(20) ); SELECT * FROM Bars; Milyen a sorok formátuma az eredményben az alábbi példák közül?
<g>BarType('Joe"s Bar','Maple St.')

## 63. Adja meg az UDT felhasználásával, mint sortípussal való relációk deklarálásának módját! Csak nagybetűket használjon!
<g>CREATE TABLE <táblanév> OF (UDT neve);


## 64. CREATE TYPE MenuType AS ( bar REF BarType, beer REF BeerType, price FLOAT ); CREATE TABLE Sells OF MenuType; Melyik lekérdezés adja vissza az alábbiak közül Joe kocsmájában árult sörök listáját olvasható formában?
<g>SELECT DEREF(ss.beer) FROM Sells ss WHERE ss.bar.name = 'Joe"s Bar';

## 65. CREATE TABLE Drinkers ( name CHAR(30), addr AddrType, favBeer BeerType ); Melyik SELECT kérdés ad értelmes eredményt az alábbiak közül?
<g>SELECT dd.favBeer.name From Drinkers dd

## 66. Az alábbiak közül válassza ki, hogy mire való a PRAGMA RESTRICT_REFERENCES a metódus deklarációjakor!
<g>Ezzel lehet kontrollálni, hogy milyen "mellékhatásai" vannak a metódusnak.

## 67. Mit jelentenek az objektum-relációs hivatkozások (References)?
<g>Egy mutató egy felhasználó által definiált adattípust (UDT) objektumra.
<g>"Objektum azonosítónak" (OID) is hívják objektum-orientált rendszerekben (azaz ahhoz nagyon hasonló fogalom)

## 68. Az ORACLE esetében hol történik egy típus metódusának a definíciója az alábbiak közül?
<g>CREATE TYPE BODY

## 69. Felhasználó által definiált adattípusok (User Defined Types, UDT) használati módjai:
- <g>Egy reláció attribútumoknak a típusa (oszloptípus)
- <g>Sortípus

## 70. Melyek igazak az alábbiak közül a beágyazott táblákra?
- <g>A létrehozásnál a megfelelő utasításban a "CREATE TABLE ... NESTED TABLE ... STORE AS ...;" kulcsszavak szerepelnek.
- <g>Megengedi, hogy a sorok egyes mezői teljes relációk legyenek.

## 71. Ha egy relációs táblát egy sortípus segítségével, mint sémával definiáltunk (az elemeinek felsorolása helyett), akkor ORACLE esetén mit kell használni az alábbiak közül, hogy egy darab sort be tudjunk szúrni a táblába?
- <g>típuskonstruktor
- <g>INSERT

## 72. Melyek igazak a "TABLE" függvényre az alábbiak közül?
- <g>Egy beágyazott táblát hagyományos relációvá lehet konvertálni az alkalmazásával. 
- <g>A kimeneteként kapott relációt a FROM záradékban lehet alkalmazni.

## 73. Mikre lehet használni a “CAST” parancsot az alábbiak közül?
- <g>Beágyazott táblák létrehozásánál használjuk.
- <g>A MULTISET operátorral együtt használva valamilyen objektumok halmazát beágyazott relációvá tudjuk alakítani.

## 74. Milyen attribútumnév-érték pár megadásával lehet kifejezni az XML sémánál azt a multiplicitást, amelyre a DTD-nél a "?" szimbólumot kellett használni?
<g>minOccures = "0" és maxOccures = "1"

## 75. Milyen attribútumnév-érték pár megadásával lehet kifejezni az XML sémánál azt a multiplicitást, amelyre a DTD-nél a "+" szimbólumot kellett használni?
<g>minOccures = "1" és maxOccures = "unbounded"

## 76. Milyen attribútumnév-érték pár megadásával lehet kifejezni az XML sémánál azt a multiplicitást, amelyre a DTD-nél a "*" szimbólumot kellett használni?
<g>minOccures = "0" és maxOccures = "unbounded"

## 77. Az alábbiak közül melyikkel lehet több megszorítást előírni az XML dokumentumok sémájára?
<g>XML séma

## 78. Milyen elemekből áll egy útkifejezés eredménye, ha attribútummal végződik?
<g>A lista atomi típusú elemekből áll (pl. sztringekből).

## 79. Mit értünk jól formált XML alatt? Az összes jó választ adja meg az alábbiak közül!
- <g>Nem hiányzik a deklaráció: \<?xml ... ?>
- <g>Megengedi, hogy önálló tageket vezessünk be.
- <g>Minden nyitó tagnek megvan a záró párja

## 80. DTD elemek (DTD ELEMENT) megadásának formalizmusa
- <g>A levelek (szöveges elemek) típusa #PCDATA (Parsed Character DATA).
- <g>Egy-egy elem leírása az elem nevét és zárójelek között az alelemek megadását jelenti.
- <g>Az elemek megadása megában foglalja az alelemek sorrendjét és multiplicitását.

## 81. DTD-ben ID-k létrehozatalának módja:
- <g>Adjunk meg egy E elemet és egy A attribútumát, aminek típusa: ID.
- <g>Amikor az E elemet (\<E>) egy XML dokumentumba használjuk, az ID típusú A attribútumának egy máshol nem szereplő értéket kell adnunk.

## 82. DTD-ben IDREF-k létrehozatalának módja:
- <g>Az IDREFS típusú attribútummal több másik elemre is hivatkozhat.
- <g>Egy F elem az IDREF attribútum segítségével hivatkozhat egy másik elemre annak ID attribútumán keresztül.

## 83. A DTD-nél az attribútumokról szóló leírásoknál az alábbiak közül milyen típusok adhatók meg?
- <g>ID
- <g>IDREF
- <g>CDATA
- <g>IDREFS

## 84. Hogyan jelöljük az attribútumokat az utakban (XML)?
<g>Az attribútumokat a @ jel jelöli, ez után következik az attribútum neve.

## 85. /zzzz/yyyy/aaa[. < 10 000]. Ez a kifejezés minek a megfogalmazására szolgál?
<g>Az /zzzz/yyyy/aaa elem értéke (belseje) kisebb, mint 10000.

## 86. Mire szolgálnak ezek az összehasonlító műveletek << és >>.
<g>Dokumentumbeli sorrend szerinti összehasonlítás.

## 87. Az E elem esetében a data függvény használatával mit kapunk meg? (data(E)).
<g>Az E elem értékét

## 88. FOR záradék sajátosságai, jellemzése:
- <g>for <változó> in <kifejezés>
- <g>Változója egy ciklusban sorra bejárja a kifejezés eredményének összes tételét.
- <g>Az utána megadott rlszek minden egyes tételre végrehajtódnak egyszer.
- <g>A változók $ jellel kezdődnek.

## 89. LET záradék sajátosságai, jellemzése:
- <g>A változó értéke tételek listája lesz, ez a lista a kifejezés eredménye.
- <g>A változók $ jellel kezdődnek.
- <g>let <változó> := <kifejezés>
- <g>A záradék hatására nem indul el ciklus

## 90. Mit értünk FLWR (vagy FLWOR, flower) kifejezések alatt? (Válassza ki az összes releváns opciót!)
- <g>Végül egy return záradék.
- <g>Opcionálisan egy where záradék.
- <g>Egy vagy több for és/vagy let záradék
- <g>Order-by záradék előzheti meg a return záradékot.

## 91. Az XPath/XQuery adatmodell, komponensei, fogalmai:
- <g>Egy tétel lehet: egyszerű érték. 
- <g>A bemenetet, a köztes lépések eredményeit és a végeredményt is tételek listájakként kezeljük.
- <g>A relációk megfelelője ebben a tételek (item) listája (sequence)
- <g>Egy tétel lehet: csomópont.

## 92. Döntsük el, hogy létezik-e olyan E és F kifejezés, hogy az every $x in E satisfies F kifejezés igaz, de a some $x in E satisfies F hamis? (Adja meg az összes jó választ!)
- <g>Nem, mert ha E összes értéke kielégíti F-t, akkor biztosan létezik legalább egy érték E-ben, amelyik kielégíti F-t.
- <g>Nem létezik ilyen E és F kifejezés.

## 93. Mi az alapértelmezett (default) tengely és meghatározása?
- <g>A default tengely a child::
- <g>A default tengely az összes gyermekét veszi az aktuális pontnak.

## 94. Mire használható az XPath?
- <g>Navigációs útvonalakat lehet megadni a dokumnetumban.
- <g>Segítségével az XML dokumentumokat járhatjuk be.

## 95. Az XQuery nyelv jellemzése, és tulajdonságai:
- <g>Egy SQL-hez hasonló lekérdezőnyelv, ami XPath kifejezéseket használ.
- <g>Egy funkcionális nyelv.
- <g>A tételek listája adatmodellt használja.

## 96. Melyek jellemzik az alábbiak közül az OLAP (On-line Analytical Processing) rendszereket?
- <g>A lekérdezések nem igénylik az abszolút időben pontos adatbázist.
- <g>Általában az adatbázis nagyobb részét érintik az idetartozó tranzakciók.
- <g>Kisebb számú, de összetett lekérdezések jellemzik, amelyek órákig is futhatnak.

## 97. Az alábbiak közül melyek a MOLAP és az adatkockák kialakításának elvei?
- <g>A függő attribútumok a kocka "belső" pontjaiban jelennek meg.
- <g>A dimenzió táblák kulcsai a hiper-kocka dimenziói.

## 98. Mit jelent a ‘lefúrás’ (“Drill-Down”)?
- <g>Egy aggregálás lebontása az elkotóelemeire.
- <g>Egy összesítés lebontása az alkotóelemeire.

## 99. Válassza ki az adattárház sajátosságait, tipikus jellemzőit az alábbiak közül!
- <g>Módszere: periodikus aktualizálás, gyakran éjszaka.
- <g>Gyakran az analitikus lekérdezések végett hozzák létre.
- <g>Egyetlen egy közös adatbázisba másolják az adatokat, - ez az adattárház, - és (lehetőleg) napra készen tartják.

## 100. Melyek jellemzők az alábbiak közül a tanult, tipikus adattárház szervezési architektúrára?
- <g>Az áruházláncok egyes áruházai OLTP szinten dolgoznak, és a helyi adatbázisaikat éjszakánként feltöltik a központi adattárházba.
- <g>Az adatelemzők az adattárházat OLAP elemzésekre használják fel. 

## 101. Melyek jellemzik az alábbiak közül az OLTP (On-line transaction Processing) rendszereket?
- <g>A kérdések viszonylag kevés sort adnak vissza válaszként.
- <g>Rövid, egyszerű, gyakran feltett kérdések.

## 102. Mi az a REVOKE?
Ez által <g>visszavonódnak</g> az általunk kiadott jogosultságok.

## 103. Mit jelent a ‘felgörgetés’ (“Roll-Up”)?
<g>Aggregálás egy vgay több dimenzió mentén.

## 104. Az adatkocka (Data Cube) szerkezete:
- <g>Az adatkocka egy pontja, amelynek koordinátái egy vgay több "*" értéket tartalmaznak, összesítódnek azon dimenziók fölött, melyek, amelyek koordinátái tartalmaznak "*" értéket.
- <g>Mindegyik dimenziót kiegészítjük "*" értékkel.

## 105. Nem lehet visszavonni REVOKE-kal az adott jogosultságnak, jogosultságoknak az engedélyezési képességét.
<g> Hamis

## 106. Melyek azok az adatbázis „objektumok” (“adatbáziselemek”) az alábbiak közül, amelyekre megadhatók jogosultságok?
- <g>Relációs táblák
- <g>Materializált nézetek
- <g>Attribútumok
- <g>Nézetek

## 107. Adattárházban mely állítások igazak a ténytáblákra?
- <g>Az attribútumai között vannak függő attribútumok, amelyek a sorban a dimenzió attribútumok által meghatározott értékek.
- <g>Az attribútumai között vannak dimenzió attribútumok, amelyek a dimenzió táblák kulcsai. (Idegen kulcsok)

## 108. Miért van szükség grant diagramokra? Melyek a jellemzői? Az alábbiak közül válassza ki az összes helyes választ!
- <g>Áttekinthetőség miatt szükséges.
- <g>Egy csúcsnál számít, hogy a felhasználó tulajdonos-e
- <g>Ez egy gráf, amelynek csúcsai egy felhasználót és egy jogosultságot képviselnek.
- <g>Egy csúcsnál számít, hogy van-e grant option

## 109. Monoton-e a metszet művelet (operátor)?
<g>Igen

## 110. Monoton-e a különbség művelet (operátor)?
<g>Nem

## 111. Hány különböző módon reprezentálható egy reláció-előfordulás (az attribútumok és sorok sorrendjét figyelembe véve), ha az előfordulásnak m attribútuma és n sora van?
<g>m! * n!

## 112. Hány különböző módon reprezentálható egy reláció-előfordulás (az attribútumok és sorok sorrendjét figyelembe véve), ha az előfordulásnak 2 attribútuma és 3 sora van?
<g>12

## 113. some $x in E1 satisfies E2 kifejezés kiértékelésének lépései:
- <g>E1-t ki kell értékelni.
- <g>Az eredmény IGAZ, ha $x legalább egy értékre E2 igaz.
- <g>$x vegye fel sor E1 eredményének értékeit, és értékeljük ki ezzel az értékekkel E2-t.

## 114. Tegyük fel, hogy egy cégnél az iratokat egy olyan szobában gyűjtik, ahol a szoba ajtaján egy lyuk van vágva, ahová az iratokért felelős dolgozó egyszerűen csak bedobja a dokumentumokat. Tekinthető-e hétköznapi értelemben adatbázisnak az így összegyűjtött iratok összessége?
<g>Nem

## 115. Tegyük fel, hogy egy egyetemen az oktatókról (személyi igazolvány-szám, név, lakcím, tanszék neve, bér) és a tanszékekről (név, tanszékvezető, cím) az információkat két külön excel fájlban tárolják. Hétköznapi értelemben adatbázis lesz-e?
<g>Igen

## 116. Lehet-e kulcs a név, város, szakma attribútumok halmaza az Dolgozó(név, város, szakma, fizetés) relációnál, ha aktuális „verziója” a relációnak az alábbi?
<g>Nem

## 117. Lehet-e kulcs az album neve, sorszám attribútumok halmaza a Műsorszám(album neve, sorszám, szám címe, hossz) relációnál, ha aktuális „verziója” a relációnak az alábbi?
<g>Igen, de azon is múlik, hogy a "valóvilágot" hogyan modellezük.

## 118. Tegyük fel, hogy az R relációnak n, az S relációnak pedig m sora van. Adjuk meg a következő kifejezések eredményeiben keletkezhető sorok maximális és minimális számát.
- <g> R |X| S sorainak lehetséges maximális száma: **n*m**
- <g> R |X| S sorainak lehetséges minimális száma: **0 / üres**  
</g>(Az alsó indexben szereplő L az R-nek azokat az attribútumait jelöli, amelyek S reláció attribútumai
is.)
- <g>Π<sub>L</sub>(R) - S sorainak lehetséges maximális száma: **n**
- <g>Π<sub>L</sub>(R) - S sorainak lehetséges minimális száma: **0 / üres**
- <g>Ha R és S sorai különböznek, R U S sorainak lehetséges maximális száma: **n+m**
- <g>Ha R és S sorai különböznek, R U S sorainak lehetséges minimális száma: **n+m**
- <g>R U S sorainak lehetséges minimális száma (ha nincs kikötve, hogy a sorok különböznek): **max(n,m)**  
</g><r>(A kvízben ez kell!!!)

## 119. A lekérdezésfában a csúcsokból kiinduló él vagy élek miket kötnek velük össze?
- <g>Vagy az eredeti teljes lekérdezés bemenet-relációit, vagy pedig a csúcsban szereplő művelethez képest
korábban végrehajtott művelete(ke)t.
- <g>A csúcsban szereplő művelet operandusát vagy operandusait.

## 120. A relációs adatmodell kapcsán mi NEM számít atomi értéknek az alábbiak közül?
- <g>halmaz
- <g>tömb

## 121. Adott egy tábla: Az alábbiak közül válassza ki a számokhoz tartozó helyes fogalmakat!
- <g>1: Attribútum
- <g>2: Mező
- <g>3: Előfordulás
- <g>4: Sor

## 122. Adott az R |X| S természetes összekapcsolás és az R |X|C S théta-összekapcsolás. A C feltétel az összes olyan A attribútumra, amely az R-ben és S-ben is szerepel, tartalmazza R.A = S.A egyenlőséget. Mi a különbség a két összekapcsolás között?
- <g>R |X| S természetes összekapcsolás esetében az összekapcsolásban az egyenlőségen összekapcsolódó oszlop
párok közül csak az egyik oszlop és annak értékei jelennek meg.
- <g>R |X|C S théta-összekapcsolásban az egyenlőség révén összekapcsolódó attribútumok és értékeik, amelyek
egyenlőek, kétszer jelennek meg.

## 123. Miként lehet meghatározni a Boyce-Codd normálformát? Jelölje be az összes jó választ az alábbiak közül!
- <g>R reláció Boyce-Codd normálformában van, ha minden X->Y olyan funkcionális függőségre R-ben, amelynél
Y nem része X-nek, X szuperkulcs.
- <g>R reláció Boyce-Codd normálformában van, ha minden X->Y nemtriviális funkcionális függőségre R-ben X
szuperkulcs.

## 124. A többértékű függőség (TÉF) meghatározása, definíciója (válassza ki az összes helyes választ):
- <g>Az R reláció fölött X ->->Y teljesül: ha bármely két sorra, amelyek megegyeznek az X minden attribútumán,
az Y attribútumaihoz tartozó értékek felcserélhetőek, azaz a keletkező két új sor R-beli lesz.
- <g>Az R reláció fölött X ->->Y teljesül: ha X minden értéke esetén az Y-hoz tartozó értékek függetlenek az R-X-
Y értékeitől.

## 125. 4NF megszünteti a többértékű függőségekből eredő redundanciát
<g>Igaz

## 126. BCNF megszünteti a többértékű függőségekből eredő redundanciát
<g>Hamis

## 127. Mit jelent az E/K diagramon, hogyha A egyedhalmaz és B egyedhalmaz között van egy kapcsolat és lekerekített nyíl van B-nél (azaz A-ból B-be mutat a lekerekített nyíl)?
<g>Minden A halmazbeli entitásnak pontosan egy párja van a B egyedhalmazból

## 128. Az egyed (entitás)-kapcsolat diagramoknál melyek igazak a kapcsolatokra, illetve jelölésükre az alábbiak közül?
- <g>A kapcsolatoknak is lehetnek attribútumai.
- <g>Valamely egy-egy kapcsolat esetén minden egyes entitás (egyed) legfeljebb egyetlen másik entitáshoz
(egyedhez) kapcsolódhat.
- <g>A sok-egy kapcsolat “egy oldalát” egy nyíl jelzi.

## 129. A TÉF-ek esetén fel lehet-e általános szabályként bontani a jobb oldalakat, mint ahogy FF-ek esetén?
<g>Hamis

## 130. Melyek az alábbiak közül az egyed (entitás)-kapcsolat diagram, modell főbb alkotórészei, alapfogalmai?
- <g>Egyedhalmazok
- <g>Attribútumok
- <g>Kapcsolathalmazok

## 131. ORACLE esetében az alábbiak közül mikre lehet használni a "." (pont) karaktert a SELECT záradékban?
- <g>Például egy objektum valamely mezőjének elérésére
- <g>Például arra, amire SQL-99-ben a generátor való
- <g>Például egy metódus használatára, ha az objektum neve után szerepel

## 132. Az alábbiak közül mire igaz az, hogy XML lekérdező nyelv?
- <g>XPath
- <g>XQuery