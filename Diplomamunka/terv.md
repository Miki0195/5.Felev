# Webes Alkalmazás Projektterv Cégek Számára

Ez a projektterv egy olyan webes alkalmazás létrehozására irányul, amely segíti a cégek mindennapi működését és a munkaerő hatékony kezelését. Az alkalmazás különböző felhasználói szerepköröket és jogosultságokat biztosít mind a cégeknek, mind az alkalmazottaknak, valamint egy adminisztrátori felületet a rendszer felügyeletéhez. Az alábbiakban a rendszer kulcsfontosságú funkciói, valamint a javasolt fejlesztések találhatók.

## Fő funkciók:

### Alkalmazottak kezelése
- **Beosztás kezelés:**  
A munkáltatók megadhatják az alkalmazottak heti/havi beosztását, amit az alkalmazottak megtekinthetnek a felületükön. A dolgozók ezen keresztül kérhetnek pihenőnapot vagy szabadságot is.
- **Üzenetküldés:**  
Az alkalmazottak és a munkáltatók egy beépített üzenetküldő rendszer segítségével kommunikálhatnak egymással, így az ütemezések és feladatok változásai gyorsan kommunikálhatók.
- **Betegség jelentése és orvosi igazolások feltöltése:**  
Az alkalmazottak bejelenthetik a betegséget és feltölthetik az orvosi igazolásukat a rendszerbe. Ez automatikusan értesíti a munkáltatót és rögzíti az információkat a rendszerben.
- **Munkaidő nyilvántartás:**  
Az alkalmazottak be- és kijelentkezhetnek a munkaidő megkezdésekor és befejezésekor. A rendszer naplózza az időpontokat, ami alapján a munkáltató nyomon követheti a munkaidőt.
- **Munkáltatói kimutatások:**  
A munkáltatók különböző jelentéseket és kimutatásokat láthatnak az alkalmazottak munkaidejéről, teljesítményéről, távolléteiről, betegségeiről stb.
- **Céges hierarchia és jogosultságok:**  
A rendszer támogatja a cégek hierarchikus felépítését. Különböző csoportok és csoportvezetők definiálhatók. A csoportvezetők részleges jogosultságokkal rendelkeznek az általuk vezetett csoport alkalmazottainak beosztására, munkavégzésének ellenőrzésére, illetve a szabadságok, pihenőnapok jóváhagyására.
### Home Office és Task-kezelés
- **Home Office támogatás:**  
Az alkalmazás támogatja a távoli munkavégzést. A munkáltatók feladatokat (taskokat) rendelhetnek az alkalmazottakhoz, és követhetik azok teljesítését. Az alkalmazottak feladatuk elvégzése után megadhatják, hogy mennyi időt töltöttek vele. A rendszer ezeket az adatokat felhasználva készít kimutatásokat a munkaidő és feladatok hatékonyságáról.
- **Teljesítménymérés:**  
A feladatok elvégzésének ideje alapján a rendszer kimutatásokat készít, így a munkáltatók láthatják az alkalmazottak teljesítményét. Lehetőség van határidők megadására is, amelyek alapján figyelmeztetéseket vagy ösztönzőket lehet alkalmazni a feladatok időben történő elvégzése érdekében.
### Adminisztrációs felület
- **Licenckulcs kezelés:**
A cégek csak licenckulcs megvásárlása és aktiválása után férhetnek hozzá a rendszerhez. A licenckulcs egy véletlenszerűen generált kód, amelyet a regisztráció során kell megadni.
- **Alkalmazottak regisztrációja:**
Csak a munkáltatók regisztrálhatják alkalmazottaikat a rendszerbe. Az alkalmazottak meghívó kód alapján kapnak hozzáférést.
### Fejlesztői adminisztrációs oldal
- **Cégek és alkalmazottak áttekintése:**  
A fejlesztő/admin hozzáférhet az összes regisztrált cég és alkalmazott adatbázisához. Ez lehetőséget biztosít a rendszer technikai karbantartására, de a céges és alkalmazotti érzékeny adatokhoz nem fér hozzá.
- **Rendszer karbantartás:**  
A fejlesztői adminisztrációs oldal lehetőséget biztosít a licenckulcsok kezelésére, új cégek hozzáadására, illetve a rendszer állapotának figyelésére (például a szerver erőforrásainak monitorozása).
### Megbeszélések szervezése (Tervbe vett funkció)
- **Videókonferencia integráció:**  
A rendszer tartalmazhat egy videókonferencia funkciót is, amely a távoli munka során lehetővé teszi a csoportok közötti hatékony kommunikációt. Ehhez harmadik féltől származó API-k (például Zoom vagy Teams integráció) is használhatók. A csoportvezetők megbeszéléseket szervezhetnek, és az alkalmazottakat értesíthetik a tervezett időpontokról.
### Kiegészítő funkciók 
- **Értesítési rendszer:**  
A felhasználók értesítéseket kaphatnak a feladatok, beosztások, megbeszélések, szabadságkérelmek és más fontos események változásairól. Az értesítések megjelenhetnek a webes felületen, e-mailben, vagy akár mobilalkalmazás push értesítése formájában is.
- **Naptár integráció:**  
A feladatok, megbeszélések és szabadságok könnyen átláthatók egy naptár nézetben. Az alkalmazás lehetőséget biztosít arra is, hogy a felhasználók exportálják naptári adataikat más külső naptáralkalmazásokba (pl. Google Calendar, Outlook).
- **Munkaidő-ellenőrző pontok:**  
A rendszer lehetővé teszi munkaidő ellenőrző pontok (checkpointok) beállítását, amelyek során a munkavállalóknak bizonyos időközönként be kell jelentkezniük a rendszerbe a munka folytonosságának biztosítása érdekében. Ez különösen a home office-ban dolgozó alkalmazottak esetében lehet hasznos.
- **Jogosultsági szintek fejlesztése:**  
A cégek belső hierarchiája alapján a jogosultságokat tovább lehet finomítani, például a csoportvezetők csak saját csoportjuk számára férhetnek hozzá bizonyos adatokhoz, míg a magasabb szintű vezetők az egész szervezetre vonatkozóan kapnak áttekintést. Ez fokozza az adatvédelem és a biztonság szintjét.
- **Teljesítményértékelés és bónuszrendszer:**  
A rendszer integrálhatna egy teljesítményértékelési modult, amely az alkalmazottak munkavégzését pontozza a feladatok elvégzése és a munkaidő nyilvántartás alapján. Ehhez egy bónuszrendszer kapcsolódhat, amely ösztönzőként szolgálhat a teljesítmény javítására.