## 1. Mi igaz az 1-perzisztens CSMA protokollra?
Az állomás belehallgat a csatornába.
Ha az szabad, akkor küld, különben várakozik,
hogy a csatorna szabaddá várjon. Ha felszabadul,
azonnal küld.  

## 2. Miért van szükség kapcsolat felépítésére TCP esetén?
Állapot kialakítása mindkét végponton (sorszámok)  

## 3. Az adat sík (data plane) a router agya, ami pl. a konfigurálásért, az útvonalmeghatározásért és statisztikák vezetéséért felel.
Hamis

## 4. Adott két végpont, melyeket egy switch/router és a köztük lévő két fizikai link kapcsol össze. Mit nevezünk sorban-állási késleltetésnek (queueing delay) egy csomag átvitele esetén?
Azt az időt, amit a csomag a switch/router várakozási sorában várakozással tölt.

## 5. Melyik multiplexitási technikára igaz? Diszkrét időszeletek használata.
Idő osztásos multiplexitás (TDM)

## 6. Melyik multiplexitási technikára igaz? Teljes frekvencia tartományt szűkebb sávokra bontja.
Frekvencia multiplexitás

## 7. Mi igaz a távolságvektor (distance vector) alapú routing protokollra?
- Minden router csak a szomszédjával kommunikál.
- Lényegében elosztott Bellman-Ford algoritmus.
- Aszinkron működés.

## 8. Mi jellemző az IPv4 csomagok fragmentációjára?
Memória kezelési problémák a cél állomásnál.

## 9. Egy protokoll CRC-t használ hiba felismeréséhez. Az alkalmazott generátor polinom fokszáma 4. Hány biten ábrázolható a CRC ellenőrzősszeg (a maradék polinom)?
4 *(polinom fokszáma = bit)*

## 10. Egy kód Hamming-távolsága 15. Hány egyszerű bithibát tudunk detektálni ezzel a kóddal?
14 *(d-1)*

## 11. Az ISO/OSI modell mely rétegéhez sorolhatók a következő fogalmak: Optikai kábel, Wifi jel, CAT6UTP kábel?
Fizikai réteg/Physical

## 12. Melyik keretezési technikára igaz? A küldő az adatban előforduló minden 11111 sorozat mögé 0 bitet szúr be
Bit beszúrás

## 13. Adott 2^N (kettő az N-ediken) állomás, melyek adaptív fabejárás protokollt használnak a közeghozzáféréshez. 2 állomás áll készen keret küldésére, melyek ütközést okoznak. Egy adatkeret küldése egységesen 1 időegységig tart. Legrosszabb esetben hány időegység szükséges az ütközés feloldásához? [Az első ütközést okozó időt ne vegyük figyelembe. Továbbá tegyük fel, hogy nem érkeznek újabb kérések a rendszerbe!]
2^N *(N)*

## 14. Mi igaz a bridge-eknél (hídaknál) látott MAC címek tanulása módszerre?
A beérkező keretben szereplő forrásállomás MAC címét és a beérkezési portot betesszük a továbbítási táblába.

## 15. Mi igaz a sávszélességű átvitelre?
A jelet modulációval ülteti egy vivőhullámra, egy széles frekvenciatartományban történik az átvitel, nem minden frekvencián kerül átvitelre a jel.

## 16. Minden bridge egyben switch is.
Hamis

## 17. Mit jelent a 3 nyugta duplikátum fogadása a TCP RENO esetén?
Csomagvesztést jelez

## 18. Legyen az átvitel (throughput) a terhelés (G) függvényében: S(G) Dinamikus csatorna kiosztást tekintve ideális esetben milyen értéket vesz fel S(G), ha G=0.5?
0.5 

## 19. Az ISO/OSI modell mely rétege foglalja magába a közeghozzáférés vezérlését (MAC)?
Adatkapcsolati réteg/Data Link

## 20. Hány bájt egy UDP fejléc?
8

## 21. Egy távolságvektor routing protokollt használó hálózatban az A állomás routing táblája a következő:
host | költség | next hop
| - | - | - |
B | 7 | B
C | 10 | C
C | 1 | D
E | 14 | E

## B szomszédtól a következő távolságvektort kapja:
| host | költség |
|-|-|
C | 2
D | 3
E | 3

## Mi lesz C költsége A állomás routing táblájában?
9 (7 + 2)

## 22. Két szimbólum használata esetén a szimbólum ráta 4 Baud. Négy szimbólum használata mellett mekkora lesz a szimbólum ráta, ha semmit sem változtatunk?
4

## 23. A csúszóablak protokoll csatorna kihasználása rosszabb, mint az Alternáló Bit Protokollé.
Hamis

## 24. Alternáló bit protokoll (ABP) esetén mekkora kihasználtság érhető el, ha feltesszük, hogy nincs propagációs késés, továbbá az adatcsomag és a nyugtacsomag azonos méretű? A csatorna szimmetrikus!
0.5

## 25. Adott két végpont, melyeket egy switch/router és a köztük lévő két fizikai link kapcsol össze. Mit nevezünk feldolgozási késleltetésnek (processing delay) egy csomag átvitele esetén?
Az az idő, amit a routeren a csomag fejléceinek feldolgozása és továbbítási döntések meghozatala igényel.

## 26. Melyik multiplexitási technikára igaz? Minden állomás saját időszeletet kap.
Idő osztásos multiplexitás (TDM)

## 27. Az ISO/OSI modell mely rétegéhez sorolhatók a következő fogalmak: BitTorrent, HTTP, BitCoin kliens?
Alkalmazási réteg/Application

## 28. Egy kód Hamming-távolsága 2. Hány egyszerű bithibát tudunk detektálni ezzel a kóddal?
1

## 29. Melyik nyugtázási módszerre igaz az alábbi állítás? Teljes információt ad a forrásnak és jól kezeli a nyugták elvesztését is, azonban az a nagy hálózati overheadje miatt csökkenti a teljesítményt.
Teljes információ visszacsatolás - Full Information Feedback

## 30. Az ISO/OSI mely rétege felelhet szinkronizációs pont menedzsmentéért? (checkpoint beszúrása stb.)
Munkamenet (Ülés) réteg/Session

## 31. Hogyan befolyásolja a minimális keretméret egy CSMA/CD alapú Ethernet hálózatban, ha a sávszélesség 25%-kal lecsökken?
25%-kal csökken *(**Minimális keretméret csökkenhet**, mivel a lassabb adatátviteli sebesség miatt kisebb kerettel is biztosítható az ütközések észlelése.)*

## 32. Mely szolgáltatásokért felel az adatkapcsolati réteg?
- Közeghozzáférés
- Adatok keretekre tördelése
- Per-hop hibakezelés
- Per-hop megbízhatóság

## 33. Melyik protokollhoz tartozik a végtelenig számlálás problémája?
Távolságvektor (distance vector) protokoll

## 34. Melyik nyugtázási módszerre igaz az alábbi állítás? A nyugta a legnagyobb sorszámot tartalmazza, amelyre igaz, hogy az összes kisebb (vagy egyenlő) sorszámú csomag már sikeresen megérkezett a vevőhöz.
Kumulatív nyugta (Cumulative ACK)

## 35. Mivel arányos az átvitel TCP esetén?
Küldési ablakméret/RTT

## 36. Komulatív nyugta (commutative ACK) esetén miként tudjuk detektálni a csomagvesztést?
Az izolált csomagvesztéséket nyugta duplikátumok jelzik.
Emellett timerekkel is dolgozik a módszer.

## 36. Mi igaz a Compound TCP-re?
- Reno alapú
- Késleltetés alapú torlódási ablakot is fenntart
- Csomagvesztés alapú torlódási ablakot is fenntart

## 37. Legyen d(x,y) két kódszó Hamming-távolsága. Hogyan definiálja egy S kód Hamming távolságát?
Az S-beli kódszó párok Hamming távolságainak a minimuma.

## 38. Igaz-e az állítás: TCP SYN flood támadás azt használja ki, hogy egy szerver minden beérkező SYN csomaghoz erőforrást foglal a kapcsolat állapotának nyilvántartásához, mely akar 2 percig is lefoglalva maradhat. Amikor rövid időn belül sok ilyen csomag érkezik a rendelkezésre álló erőforrások elfogynak és a normális kapcsolatok visszautasításra kerülnek/extrém esetben a szerver összeomlik.
Igaz

## 39. Mit nevezünk elnyelődésnek?
A küldési és vételi energiák hányadosát.

## 40. Az ISO/OSI modell mely rétege felel az adatkonverzióért különböző reprezentációk között?
Megjelenítési réteg/Presentation

## 41. Egy bridge a szomszéd bridge-eknek küldi el a konfigurációs üzenetet, mely alapján azok frissítik a gyökér csomópontot és a hozzá vezető úthoz kapcsolódó információkat.
Igaz

## 42. Mik az áramkörkapcsolt hálózat jellemzői?
- Túlterhelés esetén az új résztvevőknek már nem jut erőforrás
- Garantált erőforrást kapnak a résztvevők
- Az erőforrások előre lefoglalásra kerülnek minden kapcsolathoz

## 43. Mi igaz a végpont-végpont megbízhatóságra?
- Az alkalmazhatóságnak nem kell a hálózati problémákkal foglalkozniuk, így a megbízhatóság biztosításával sem. *(Figyelembe kell venniük, hogy milyen protokollt használnak. Ha a protokoll nem nyújt megbízhatóságot (pl. UDP), akkor az alkalmazásnak magának kell ezt kezelnie. Szóval ne miztos, hogy igaz.)* 
- A hálózat legyen a lehető legegyszerűbb, azaz nem biztosit végpont-végpont megbízhatóságot.
- A végpont-végpont megbízhatóságot az L4 (Transport - Szállítói) réteg biztosítja

## 44. A csatorna kihasználtság megadja egy csomag elküldésének idejét
Hamis

## 45. A megbízható adatátvitel 4 fő célja közül melyik szól arról, hogy: „az adat leszállítása biztosított, sorrend helyes és átvitel során nem módosul"?
Helyesség/correctness

## 46. Adott egy hálózat:
A---1 Gbps---B--10 Gbps---C  
és adott 3 folyam:  
1.folyam: A-ból B-be küld adatot  
2.folyam: B-ból C-be küld adatot  
3.folyam: A-ból C-be küld adatot  
## Milyen rátát kap a 2. folyam Mbps-ben, ha a max-minfair allocation-t alkalmazzuk a sávszélességek kiosztására? 
9.5

## 47. Mennyi az átviteli késleltetése egy 1500 bájtos csomagnak egy olyan hálózaton, ahol az elérhető adatráta 12 Gbps? A választ mikromásodpercben (us) adjuk meg!
(segítség 1 us = 10^-6 sec, 1Gbps = 10^9 bps (bits/second))  
1

## 48. Mivel egészíti ki az UDP a hálózati rétegtől kapott szolgáltatást?
Lehetővé teszi a demultiplexálást és hibaellenőrzést a célállomáson.

## 49. Milyen hátrányai vannak az áramkör-kapcsolt hálózatoknak?
- Hiba esetén új áramkör szükséges
- Alacsony hatékonyság löketszerű forgalmak és rövid folyamok esetén
- Bonyolult áramkör felépítés/lebontás

## 50. Egy csúszóablak (sliding window) protokoll esetén a sorszámok tere 0,1,2,3,4,5,6,7, a fogadó 2 csomagot tud pufferelni, a vételi ablakban 2,3 sorszámok szerepelnek. Mit tesz a fogadó egy 1-es sorszámú csomag beérkezése esetén?
Eldobja a csomagot és nyugtát küld.

## 51. Az OSI/ISO modell mely rétegéhez tartozik az UDP protokoll?
Szállítói réteg/Transport

## 52. A pipelineing technika nem segít a csatornakihasználtság javításában.
Hamis

## 53. Milyen viszony az IPS-k között, amikor kölcsönösen fizetség nélkül forgalmazhatnak egymás hálózatában?
Peer

## 54. Az előadáson látott speciális topológiák közül melyik rendelkezik a legjobb hibatoleranciával?
Teljesen összekötött (full-mesh)

## 56. Mi igaz a p-perzisztens CSMA protokollra?
Az állomás belehallgat a csatornába. Ha az szabad, akkor p valószínűséggel küld, és 1-p valószínűséggel vár még egy időegységet (nem küld) és kezdi elölről. Ha foglalt a csatorna, akkor vár 1 időegységet és kezdi elölről. Diszkrét időmodell

## 57. Megoldja-e a torlódás problémáját a TCP esetén a meghirdetett ablak (advertised window) használata?
Nem, mert ez az ablak csak a fogadót védi a túlterheléstől

## 58. A 100 Mbps Ethernetnél alkalmazott 4/5 kódolással _%-ot veszítünk a hatékonyságból
20

## 59. Mi igaz az alternáló bit protokollra (ABP)?
- Vevő oldalon, ha nincs hiba az adatrészt továbbküldi a hálózati rétegnek, végül nyugtázza a keretet és lépteti a sorszámot mod 2. 
- Küldő egyesével küldi a sorszámmal ellátott kereteket (kezdetben 0-s sorszámmal) és addig nem küld újat, meg nem kap nyugtat a vevőtől egy megadott határidőn belül.

## 60. Adott egy Distance Vector protokollt használó hálózat. Az u állomás szomszédjai A, B és C állomások. Adottak az alábbi élköltségek:
c(u, A) = 3,  
c(u, B) = 1,  
c(u, C) = 7.  
## Az u állomás egy adott időpillanatában megkapja mindhárom szomszéd távolságvektorait:
dA(B) = 12,  
dA(C) = 3,  
dA(D) = 4.    
dB(A) = 3,  
dB(C) = 8,  
dB(D) = 2.    
dC(A) = 1,  
dC(B) = 2,  
dC(D) = 1.  
## u vektorainak frissítése után adjuk meg Du (A) távolságot!
3

## 61. Mi igaz az AS-ek közötti ún. inter-domain routingra?
- BGP-t használ
- Útvonal/Távolság vektor protokollt használ

## 62. A vezérlési sík (control plane) a router agya, ami pl. a konfigurálásáért, az útvonalmeghatározásért és statisztikák vezetéséért felel.
Igaz

## 63. Egy végtelen populációjú ALOHA-rendszer mérései azt mutatják, hogy a rések 10%-a tétlen. Mekkora a csatorna terhelés?
G = -ln(0.1)

## 64. Egy kód Hamming-távolsága 8. Hány egyszerű bithibát tudunk detektálni ezzel a kóddal?
7

## 64. Az adat sík (data plane) a csomagok feldolgozásáért és továbbításáért felel.
Igaz

## 65. Mi igaz a fizikai rétegre?
Szolgáltatása, hogy információt (biteket) visz át két fizikailag összekötött eszköz között.

## 66. Melyik keretezési technikára igaz, hogy Egy speciális ESC (Escape) bájtot szúr be az „adat" flag bajtok elé.
Bájt beszúrás

## 67. Egy protokoll CRC-t használ hiba felismeréséhez. Az alkalmazott generátor polinom fokszáma 32. Hány biten ábrázolható a CRC ellenőrzősszeg (a maradék polinom)?
32

## 68. Mi igaz a Hamming-kódra?
- paritást használó technika
- Mindegyik ellenőrző bit a bitek valamilyen csoportjának a paritását állítja be párosra (vagy páratlanra)
- 2 egész hatvány sorszámú pozíciói lesznek az ellenőrző bitek, azaz 1,2,4,8,16,..., a maradék helyeket az üzenet bitjeivel töltjük fel

## 69. Mi az, ami a TCP fejlécében szerepel, de az UDP fejlécében nem?
- Sorszám (sequence number)
- Nyugta szám (Acknowledgement number)

## 70. Bridge-ek egy porton csak egy állomást tudnak kezelni.
Hamis

## 71. Melyik multiplexitási technikára igaz? Vezetékes kommunikáció esetén minden egyes csatornához külön antenna rendelődik.
Térbeli multiplexitás *(Vezetek nelkuli esetre jobban igaz)*  
*Frekvenciaosztásos multiplexelésre*

## 72. Hogyan detektáljuk a helyességet? Egy szállítási mechanizmus helyes, akkor és csak akkor...
Minden elveszett vagy hibás csomagot újraküld.

## 73. Mi igaz a BGP protokollra?
- Megadható olyan szabály, hogy ne legyen átmenő forgalom bizonyos AS-eken keresztül
- A politikai jellegű szabályokat kézzel konfigurálják a BGP-routeren.

## 74. Adjuk meg helyes sorrendben a három-utas kézfogás üzenetváltásait!
1. SYN a kliensről a szerverhez
2. SYN/ACK a szerverről a klienshez
3. ACK a kliensről a szerverhez

## 75. Melyik multiplexitási technikára igaz? Minden állomás saját frekvencia tartományt kap.
Frekvencia multiplexitás

## 76. Melyik keretezési technikára jellemző? A keretben lévő karakterek számának megadása keret fejlécben lévő mezőben
Karakterszámolás

## 77. Igaz-e az állítás: Tipikus webes forgalom esetén a TCP hatékony, képes kihasználni a rendelkezésre álló szabad hálózati kapacitást (sávszélességet).
Hamis

## 78. Mi igaz az ALOHA protokollra?
- Ha van elküldendő adat, akkor elküldi.
- Az adat azonnal kiküldésre kerül.
- Amikor egy keret küldésre kész, az küldésre kerül a (következő) időrés határon.

## 79. Melyik címosztály esetén lehet a legkevesebb hosztot definiálni egy címosztályon belül.
C címosztály

## 80. Mi igaz a kapcsolatállapot (link state) alapú routing protokollra?
- Megméri a szomszédokhoz vezető költséget, majd ezt elküldi minden routernek.
- Dijkstra algoritmust alkalmaz

## 81. Mely csatornára igaz az állítás? Mindkét irányba folyhat kommunikáció, de egyszerre csak egy irány lehet aktiv.
Fél-Duplex csatorna

## 82. Az előadáson látott naiv hibadetektáló megoldás minden keretet kétszer küld el. Ezt követően a két kópia egyezését használja a hibamentes átvitel eldöntésére. Mely állítások igazak erre a módszerre?
- Gyenge hibavédelemmel rendelkezik.
- Túl nagy a költsége.

## 84. Mely csatornára igaz az állítás? Mindkét irányba folyhat kommunikáció szimultán módon.
Duplex csatorna

## 85. Legyen az átvitel (throughput) a terhelés (G) függvényében: S(G) Dinamikus csatornakiosztást tekintve ideális esetben milyen értéket vesz fel S(G), ha G=3?
3 

## 86. Mekkora a következő két bitsorozat Hamming-távolsága? d(1001, 1011)
1 *(Eltero bitek szama)*

## 87. Mely IPv4 mezőnek nincs megfelelő párja az IPv6 fejlécében?
- Checksum (CRC ellenőrzési összeg) -> Ennek?
- Identifier
- Fejléc hossza (IHL)

## 88. Hogyan állítjuk be az újraküldéshez használt időkorlátot (RTO) a TCP esetén?
2 * RTT

## 89. Egy s(t) függvényt a sin(t) vivőhullámra a következőképp kódolunk: sin(t*s(t)). Melyik modulációs technikát alkalmaztuk?
Frekvencia moduláció

## 90. A terhelés (G) a protokoll által közölni kívánt csomagok száma egy csomag kiszolgálásának ideje alatt. Optimális esetben G>1 esetén az átvitel S(G) =_
G

## 91. Adott N állomás, melyek bináris visszaszámlálás protokollt (Mok és Ward féle javítás nélkül) használnak a közeghozzáféréshez. A versengési időrés 1 időegység. Egy adatkeret küldése szintén egységesen 1 időegységig tart. Legjobb esetben hány időegységet kell egy állomásnak várnia a saját kerete átvitelének megkezdése előtt? [Azt az időt már ne számoljuk, amiben a saját keret is átvitelre kerül. Továbbá tegyük fel, hogy közvetlenül a versengési időrés előtt állunk.]
1

## 91. Mi igaz az útvonal meghatározásra (routing)?
- Vezérlési rétegben valósítják meg
- A csomag által követendő útvonalak kiszámítása
- Globális folyamat
- Időskála kb. 10 ezred mp

## 92. Melyik protokollt használjuk un. váratlan események jelzésére?
ICMP

## 93. Igaz-e az állítas: Csomagvesztés utáni kis időben a CUBIC TCP küldési rátájának a felfutása gyorsabb, mint amit a slow start mechanizmus esetén láttunk.
Hamis

## 94. Switchek esetén nincs szükség CSMA/CD-re. Switchek esetén full duplex linkek kötik be az állomásokat
Igaz

## 95. Egy s(t) függvényt a sin(t) vivőhullámra a következőképp kódolunk: sin(t + s(t)). Melyik modulációs technikát alkalmaztuk?
Fázis moduláció

## 96. Adott két végpont, melyek között egy 120 MB-os file letöltése 2 percet vesz igénybe. Mekkora az átviteli ráta (throughput) a két oldal között? Mbps-ben
8

## 97. Az ISO/OSI modell mely rétege felel az üzenetek adott állomáson belüli forgalom multiplexálásáért/demultiplexálásáért?
Szállítói réteg/Transport

## 98. Egy távolságvektor routing protokollt használó hálózatban az A állomás routing táblája a következő:
host | költség | next hop
|-|-|-|
B | 7 | B
C | 10 | C
C | 1 | D
E | 14 | E 

## B szomszédtól a következő távolságvektort kapja:
|host | költség |
|-|-|
|C | 2 |
| D | 3 |
| E | 3 |
## Mi lesz D költsége A állomás routing táblájában?
10 *(7 + 3)*

## 99. Mi igaz az ütközés detektálásra (collision detection)?
- Minden állomás küldés közben megfigyeli a csatornát. 
- Ha ütközést tapasztal, akkor megszakítja az adást, és végtelen ideig várakozik, majd újra elkezdi leadni a keretet.

## 100. Mi igaz egy hálózat C végpontjához készített feszítőfára?
- Minden routert tartalmaz
- C minden routerből elérhető a feszítőfa élei mentén
- Minden router egy kimenő éllel rendelkezik

## 101. Egy optikai gerinchálózaton 2 routert 200 km üvegszál köt össze. Az üvegszálban a jelterjedési sebesség 2*10^8 m/s. Mekkora propagációs késést tapasztalunk a fenti optikai linken ezred mp-ben?
1

## 102. Egy globális továbbítási állapot (global forwarding state) akkor és csak akkor érvényes, ha
- Nincsenek zsákutcák (dead ends) a hálózatban
- Nincsenek hurkok/körök a hálózatban

## 103. Előrefoglalásos erőforrás kezelés esetén P=1 Gbps erőforrást foglalunk le a és b állomások között. Az átlagos ráta A=100 Mbps. Milyen kihasználtsági szintetforwarding látunk (százalékban)?
10%

## 104. Melyik állítás igaz a bridge-eknél (hidaknál) látott feszítőfa protokollra (STP)?
- A fa gyökere a legkisebb ID-val rendelkező bridge, melyet a szomszédoktól kapott üzenetek alapján frissít egy bridge.
- Egy bridge a szomszéd bridge-eknek küldi el a konfigurációs üzenetét, mely alapján azok frissítik a gyökér csomópontot és a hozzá vezető úthoz kapcsolódó információkat.

## 105. Mi a folyam vezérlés (flow control) célja a megbízható adat átvitel során?
A lassú vevő túlterhelésének megakadályozása

## 106. Mely modulációs technika használja a vivőhullám több jellemzőjét is a szimbólumok kifejezésére?
QAM-16 technika

## 107. Mit nevezünk torlódásnak?
A hálózat terhelése nagyobb, mint a kapacitása

## 108. Minek kell teljesülnie a chip vektorokra a CDMA módszer esetén?
Páronként ortogonális vektoroknak kell lenniük.(skaláris szorzatuk 0)

## 109. Mi igaz a csomagtovábbításra (forwarding)?
- A csomagot egy kimenő vonal felé irányítja
- Időskála: nanoszekundum
- Adatsíkban (data plane) valósul meg
- Helyi folyamat

## 110. Mi igaz a TCP lassú indulás (slow start) mechanizmusara?
Minden nyugta fogadása esetén a küldő egy szegmenssel növeli a torlódási ablakot. Az időben ez gyors, exponenciális növekedést jelent a küldési rátában

## 111. Egy kód Hamming-távolsága 25. Hány egyszerű bithibát tudunk javítani ezzel a kóddal?
12

## 112. Adott egy fizikai link, ami két eszközt kapcsol össze, melyek kommunikálni szeretnének. Mit nevezünk propagációs késésnek (propagation delay) ebben az esetben?
Azt az időt, ami a jelnek szükséges ahhoz, hogy áthaladjon a fizikai közegen, ami összeköti a küldő és a cél eszközöket.

## 123. Melyik állítások igazak a Link-State Routing-ra?
- Lokálisan minden router egy Dijkstra algoritmust futtat.
- Elárasztással, minden routernek eljuttatja a lokális információkat.
- A hálózat globális szerkezetét (topológiáját) igényli.

## 124. Egy ISO/OSI modell mely rétege felel az útválasztásért?
Hálózati réteg/Network

## 125. Az Alternáló Bit Protokoll csatorna kihasználtsága azonos a szimplex megáll és vár protokoll esetén látottal.
Igaz

## 126. Egy protokoll CRC-t használ hiba felismeréséhez. Az alkalmazott generátor polinom fokszáma 7. Hány biten ábrázolható a CRC ellenőrzősszeg (a maradék polinom)?
7

## 127. Mi a fő probléma a forrás-cél alapú csomag továbbítással (source- and destination-based forwarding)?
A további táblákban sokkal több (~n^2) bejegyzést kell nyilvántartani, mint célalapú megoldásnál

## 128. Melyik multiplexitási technikára igaz? Vezetékes kommunikáció esetén minden egyes csatornához külön pont-pont fizikai kapcsolat tartozik.
Térbeli multiplexitás

## 129. Mely bit hibákat nem képes felismerni a CRC módszer, ha a generátor polinom x^11+x^9+x+1? 
ahol a hiba polinom E(x) = x^12+x^10+x^2+x

## 130. Az ISO/OSI mely rétegeit nem használjuk az internet architektúrájának leírásához?
- Alkalmazási réteg/Application
- Munkamenet (Ülés) réteg/Session
- Megjelenítési réteg/Presentation

## 131. Adott egy Distance Vector protokollt használó hálózat.
Az u állomás szomszédjai A, B és C állomások.
Adottak az alábbi elköltségek:
c(u, A) = 3,
c(u, B) = 1,
c(u, C) = 7.
Az u állomás egy adott időpillanatában megkapja mindhárom
szomszéd távolság vektorait:
dA(B) = 12,
dA(C) = 3,
dA(D) = 4.
dB(A) = 3,
dB(C) = 8,
dB(D) = 2.
dC(A) = 1,
dC(B) = 2,
dC(D) = 1.
u vektorainak frissítése után adjuk meg Du (C) távolságot!  
**6**

## 132. Mely bit hibákat nem képes felismerni a CRC módszer, ha a generátor polinom x^4+x+1, ahol x^4 jelöli az "x a negyediken hatvány"?
ahol a hiba polinom E(x) = x^5+x^2+x

## 133. Mely réteghez tartozik a VPN alapját adó IPSec?
Hálózati réteg

## 134. Egy s(t) függvényt a sin(t) vivőhullámra a következőképp kódolunk: s(t)*sin(t). Melyik modulációs technikát alkalmaztuk?
Amplitudó moduláció

## 135. Mi az összefüggés a frekvencia (f), a hullámhossz (L (LAMBDA)) és a fénysebesség (c) között?
F*L = c

## 136. Mi az, ami biztosan NEM szerepel egy L3 router routing táblájában?
- cél MAC cím
- forrás MAC cím
- TCP port
- UPD port

## 137. Mely réteghez tartozik a TCP és az UDP protokoll?
Szállítói réteg/Transport

## 138. Mire szolgál a meghirdetett ablak (advertised window) TCP esetén?
A fogadó pufferméretét mutatja

## 139. Az ISO/OSI modell mely rétegéhez tartozik a TCP protokoll?
Szállítói réteg/Transport

## 140. Melyik állítás igaz általában a DSL Internet hozzáférésre?
A letöltési csatorna kapacitása nagyobb, mint a feltöltési csatornáé.

## 141. Mit jelent az optimalitási elv útvonalkiválasztás esetén?
Legyen P az I-ből K állomásba vezető optimális útvonal. Ekkor bármely J állomást véve a P útvonal mentén, a J-ből K-ba vezető optimális útvonal P-re esik (annak része).

## 142. Melyik címosztály esetén osztható ki a legkevesebb ilyen IP tartomány?
A címosztály

## 143. Milyen előnyei vannak a csomagkapcsolt hálózatoknak?
- Hatékony erőforrásgazdálkodás
- Jó hibatolerancia
- Egyszerű megvalósítás

## 144. Mit csinál Nagle algoritmusa a TCP esetén, ha van nem nyugtázott adat és az elérhető adat < MSS?
Várakoztatja az adatot egy pufferben, amig nyugtat nem kap.

## 145. A megbízható adatátvitel 4 fő célja közül melyik szól az adat leszállítási idejének minimalizálásáról?
Időbeliség/Timeliness

## 146. Mikor érvényes egy globális továbbítási állapot (global forwarding state)?
Ha a csomagokat mindig leszállítja a célállomásnak. *(Nincsenek zsákutcák, Nincsenek hurkok/körök)*

## 147. Az ISO/OSI modell mely rétege felel a csomagtovábbításért?
Hálózati réteg/Network

## 148. Mit használ az un. NAT doboz bejövő csomagok esetén a cél IP címek fordításához?
UDP/TCP fejléc cél port mezőjét

## 149. Mi igaz az alapsávú átvitelre?
- A jel minden frekvencián átvitelre kerül.
- A digitális jel direkt árammá vagy feszültséggé alakul.

## 150. Adott N állomás, melyek Alapvető bittérkép protokollt használnak a közeghozzáféréshez. A versengési időrés 1 időegység. Egy adatkeret küldése szintén egységesen 1 időegységig tart. Legrosszabb esetben hány időegységet kell egy állomásnak várnia a saját keretre átvitelének megkezdése előtt? [Azt az időrést már ne számoljuk, amiben a saját keret is átvitelre kerül. Továbbá tegyük fel, hogy közvetlenül a versengési időrés előtt állunk]!
N  *( vagy N-1)* 

## 151. Melyik protokollt használjuk IP címhez tartozó MAC cím feloldására?
ARP

## 152. Mikor van értelme áramkör-kapcsolt hálózatot használni?
Amikor az átlagos kihasználtság nagy.

## 153. Az ISO/OSI modell mely rétege definiálja az átvitelre szánt adatok keretekre tördelését?
Adatkapcsolati réteg/Data Link

## 154. Adott egy fizikai link, ami két eszközt kapcsol össze, melyek kommunikálni szeretnének. Mit nevezünk átviteli késésnek (transmission delay) ebben az esetben?
Az az idő, amely alatt minden küldeni kívánt bit felkerül a linkre

## 155. Mi igaz a TCP AIMD mechanizmusara?
- Gyors újraküldés esetén csomagvesztés során (dupack) felére csökkentjük a torlódási ablakot.
- Minden nyugta fogadása esetén "1/torlódási ablak méret"-tel növeljük torlódási ablakot.

## 156. Négy szimbólum használata esetén hány bitet tudunk egy szimbólumba kódolni?
2

## 157. Mikor használ egy switch elárasztást egy csomag továbbítása során?
Ha a csomag célállomás nem szerepel a továbbítási táblában

## 158. Egy csúszóablak (sliding window) protokoll esetén a sorszámok tere 0,1,2,3,4,5,6,7, a 4 hosszú küldési ablakban az 1,2,3,4 sorszámok várnak. Az 1-es sorszámú nyugta beérkezése után milyen sorszámmal lehetnek elküldött, de nem nyugtázott csomagok?
2,3,4,5

## 159. Egy távolságvektor routing protokollt használó hálózatban az A állomás routing táblája a következő:
host | költség | next hop
|-|-|-
B | 7 | B
C | 10 | C
C | 1 | D
E | 14 | E

## B szomszédtól a következő távolságvektort kapja:
| host | koltseg
|-|-
C | 2
D | 3
E | 3

## Mi lesz E költsége A állomás routing táblájában?
10

## 160. Hogyan tanulják meg a switch-ek a forrás állomás címet?
Ha egy A ponton érkezik egy b csomag B állomástól, és B nem a továbbítási táblában, akkor megtanulja, hogy B állomás az A port irányában érhető el

## 161. Mely csatornára igaz az alábbi állítás? A kommunikáció pusztán az egyik irányba lehetséges?
Szimplex csatorna

## 162. Mi igaz a szimplex megáll és var protokollra (zajos csat.)?
- Nyugta elvesztése esetén duplikátumok adódhatnak át a felsőbb rétegeknek a fogadó oldalon. 
- Csomagvesztés esetén az időzítő lejárta után (timeout) újraküldi a keretet

## 163. Egy kód Hamming-távolsága 13. Hány egyszerű bithibát tudunk javítani ezzel a kóddal?
6

## 164. Az előadáson látott kihasználtság definíció mellett mi az elérhető legnagyobb kihasználtság?
1 vagy 100% 

## 165. Mik történhetnek egy csomaggal átvitel során, melyet egy megbízható végpont-végpont adattranszport protokollnak kezelnie kell?
- duplikátumok - duplicates
- csomagvesztés - loss
- várakoztatás - being delayed
- csomagok sorrendjenek megváltoztatása - reordering

## 166. Mit értünk csomópont-kapacitás alatt?
A linkek száma kellően nagy, hogy a csomópontokon áthaladó forgalmat minden állomás el tudja vezetni.

## 167. Előrefoglalásos erőforrás kezelés esetén P=1 Gbps erőforrást foglalunk le a és b állomások között. Az átlagos ráta A=10 Mbps. Milyen kihasználtsági szintet látunk (százalékban)?
1%

## 168. Mi igaz a csúszóablak protokollra?
- A keret nyugtája tartalmazza a következőnek várt keret sorszámát
- A nem megengedett sorozatszámmal érkező kereteket el kell dobni.
- Adat és nyugta csomagok egyszerre utazhatnak.
- Csak duplex csatorna esetén alkalmazható.

## 169. Melyik keretezési technikára igaz? A keretek rögzített mérettel rendelkeznek, aminek (pl. STS-1 esetén) elküldése 125 us ideig tart
Óra alapú keretezés

## 170. Adott 2^N (kettő az N-ediken) állomás, melyek adaptív fabejárás protokollt használnak a közeghozzáféréshez. 2 állomás áll készen keret küldésére, melyek ütközést okoznak. Egy adatkeret küldése egységesen 1 időegységig tart. Legjobb esetben hány időegység szükséges az ütközés feloldásához? [Az első ütközést okozó időt ne vegyük figyelembe. Továbbá tegyük fel, hogy nem érkeznek újabb kérések a rendszerbe!]
2 *(vagy 1)*

## 171. Hogyan tanulják meg a switch-ek a forrás állomás címet?
Ha egy A ponton érkezik egy b csomag B állomástól, és B nem a továbbítási táblában, akkor megtanulja, hogy B állomás az A port irányában érhető el