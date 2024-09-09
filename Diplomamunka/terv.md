# **Terv**
A projektem egy átfogó, cégek számára tervezett webes alkalmazás, amely egyszerűsíti a mindennapi működést és a munkaerő-kezelést. A rendszer több kulcsfontosságú funkciót biztosít:

**Raktárkészlet- és rendelések kezelése:**  
- Hatékonyan követhető a raktárkészlet -> A cégek ezt teljesen személyre szabhatják, vehetnek fel új termékeket, törölhetnek belőle, képet tudnak hozzá feltölteni az adott termékről, ki tudják nyomtatni az egész raktárkészlete vagy annak egy részét
- A weboldalon regisztrált cégek tudnak egymástól árut rendelni -> Az adott cég meg tudja adni, hogy mit szeretne (ha szeretne) árusítani, amit a többi cég egy "piac" oldalon lát és azt meg tudja rendelni és nyomonkövetni, hogy milyen fázisban és hol van a csomagja

**Alkalmazottak kezelése:**  
- A munkáltatók kiírhatják a beosztásokat, és üzeneteket válthatnak az alkalmazottakkal.
- Az alkalmazottak hozzáférhetnek a beosztásukhoz, és a naptárban jelezhetik, hogy mikor szeretnének pihenőnapot kivenni részmunkaidő esetén, illetve szabadságot kérhetnek.
- Betegség esetén feltölthetik az orvosi igazolásukat, és kommunikálhatnak a munkáltatóval. 

**Adminisztrációs felület:**  
- Egy licenckulcs megvásárlása után férhetnek hozzá a rendszerhez, ami a regisztráció során szükséges, ez egy random generált kód.
- Csak a munkáltatók regisztrálhatják be alkalmazottaikat az oldalra

**Admin oldal:** 
- Én, mint fejlesztő, rendelkezem egy adminisztrációs felülettel, ahol a rendszer kezelése folyik anélkül, hogy hozzáférnék a kényes céges információkhoz.
- Látom az összes regisztrált céget a rendszerben, illetve azok alkalmazottait

Lényegében három különálló oldalról van szó függöen attól, hogy ki használja azt. A cégek számára, az alkalmazottak számára és számomra is különálló felhasználói felületet biztosít az oldal, mindegyik esetben más és más funkciókkal.