import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.atomic.AtomicBoolean;

public class Agency {
    /* TODO Használj egy megfelelő `Set` implementációt. */
    private final Set<Apartment> apartments =  new HashSet<>();
    /* TODO Hozz létre egy AtomicBoolean objektumot, `true` alapértelmezett értékkel. */
    public final AtomicBoolean isOpen =  new AtomicBoolean(true);
    // TODO 2. feladathoz: hozz létre egy public final logger adattagot.
    public final Logger logger;
    private int numberOfReservedApartments = 0; // 3. feladathoz: a lefoglalt lakások száma

    // 2. feladathoz: készíts konstruktort, amely létrehozza és elindítja a logger működését
    public Agency() {
        logger = new Logger(this);
        logger.start();
    }
    
    // Add hozzá a paraméterként megadott lakást az `apartments` halmazhoz.
    // A visszatérési érték a művelet elvégzésének sikerességét mutassa.
    // Az első feladatokhoz: amennyiben sikerült ezt a műveletet elvégezni, 
    // írjuk ki a standart output-ra a lakás hozzáadásának tényét (lásd: minta).
    // 2. feladathoz: a kiírást a `logger` objektumon keresztül végezzük.
    public boolean registerApartment(Apartment apartment) {
        boolean added = apartments.add(apartment);
        if (added) {
            logger.logMessage("Apartment registered: " + apartment);
        }
        return added;
    }

    // Töröld a paraméterként megadott lakást az `apartments` halmazból.
    // A visszatérési érték a művelet elvégzésének sikerességét mutassa.
    public boolean removeApartment(Apartment apartment) {
        boolean deleted = apartments.remove(apartment);
        if (deleted) {
            logger.logMessage("Apartment deleted: " + apartment);
        }
        return deleted;
    }

    // 3. feladathoz: a megadott paraméterek alapján válogassuk ki az `apartments` halmazból a megfelelő elemeket
    // (amelyeknek a mérete és bérleti díja a megfelelő minimum és maximum értékeken belül van).
    // Tipp: használd a `Utilities` függvényeit, és ügyelj a megfelelő szinkronizációra!
    public List<Apartment> search(int minSize, int maxSize, int minRent, int maxRent) {
        List<Apartment> result = new ArrayList<>();
        synchronized(apartments){
            for (Apartment apartment : apartments) {
                if (Utilities.isBetween(minSize, apartment.size, maxSize) &&
                    Utilities.isBetween(minRent, apartment.rent, maxRent)) {
                    result.add(apartment);
                }
            }
        }
        return result;
    }

    // 3. feladathoz: a paraméterként megadott lakást `foglald le`:
    // Amennyiben az `apartments` tartalmazza a lakást:
    //   - töröljük a halmazból a lakást,
    //   - a mintának megfelelően írjuk ki a képernyőre, hogy a lakás le lett foglalva (a logger-en keresztül),
    //   - növeljük a lefoglalt lakások számát,
    //       - ha a lefoglalt lakások száma elérte a Main-ben megadott NumberOfTenants értéket, `zárjuk be` az irodát
    //   - adjunk vissza `true` értéket.
    // Amennyiben a lakást nem tartalmazza az `apartments`: adjunk vissza `false` értéket.
    // Tipp: ügyelj a szinkronizációra!
    public synchronized boolean reserveApartment(Tenant tenant, Apartment apartment) {
        if (!apartments.contains(apartment)) {
            return false;
        }
    
        apartments.remove(apartment);
        logger.logMessage("Apartment reserved by Tenant " + tenant.id + ": " + apartment);
        numberOfReservedApartments++;
    
        if (numberOfReservedApartments >= Main.NumberOfTenants) {
            isOpen.set(false);
            logger.logMessage("Agency is now closed. All apartments reserved.");
        }
    
        return true;
    }
}
