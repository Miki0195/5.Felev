# **Terv**
A projektem egy átfogó, cégek számára tervezett webes alkalmazás, amely egyszerűsíti a mindennapi működést és a munkaerő-kezelést. A rendszer több kulcsfontosságú funkciót biztosít:

**Alkalmazottak kezelése:**  
- A munkáltatók kiírhatják a beosztásokat, és üzeneteket válthatnak az alkalmazottakkal.
- Az alkalmazottak hozzáférhetnek a beosztásukhoz, és a naptárban jelezhetik, hogy mikor szeretnének pihenőnapot kivenni részmunkaidő esetén, illetve szabadságot kérhetnek.
- Betegség esetén feltölthetik az orvosi igazolásukat, és kommunikálhatnak a munkáltatóval. 
- Alkalmazottak munkdaidejének nyomonkövetése -> Munkaidő megkezdésekor és befejezésekor be kell jelentkezni az oldara és ott valamilyen formában majd (valamilyen gomb használatával) jelezni kell ezeket
- A munkáltató hozzáférhet különböző kimutatásokhoz az alkalmazotakkal kapcsolatban
- (Megbeszélések szervezése) - utána kell még néznem, hogy milyen bonyolúlt egy teams jellegű videóbeszélgetés megvalósítása, de valószínüleg (80%) ez is bele fog kerülni a végleges tervbe

**Adminisztrációs felület:**  
- Egy licenckulcs megvásárlása után férhetnek hozzá a rendszerhez, ami a regisztráció során szükséges, ez egy random generált kód.
- Csak a munkáltatók regisztrálhatják be alkalmazottaikat az oldalra

**Admin oldal:** 
- Én, mint fejlesztő, rendelkezem egy adminisztrációs felülettel, ahol a rendszer kezelése folyik anélkül, hogy hozzáférnék a kényes céges információkhoz.
- Látom az összes regisztrált céget a rendszerben, illetve azok alkalmazottait

Lényegében három különálló oldalról van szó függöen attól, hogy ki használja azt. A cégek számára, az alkalmazottak számára és számomra is különálló felhasználói felületet biztosít az oldal, mindegyik esetben más és más funkciókkal.