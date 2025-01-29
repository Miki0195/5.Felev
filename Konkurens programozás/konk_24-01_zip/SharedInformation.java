import java.util.concurrent.atomic.*;
/**
 * Singletion osztaly a kozos informaciok tarolasara, amit a detektivek a kihallgatasok alatt
 * szereznek.
 * Amikor a megszerzett informacio 100% lesz, abbahagyjak a nyomozas kihallgatasi fazisat.
 */
public class SharedInformation {
    private static final int INFORMATION_PERCENT_GOAL = 100;

    private static SharedInformation instance;
    // TODO 1. Resz: Keszitsuk el az informationPercent valtozot, hogy nyomon kovessuk a megszerzett informacio szazalekat
    // TODO 1. Resz: informationPercent = 0
    private final AtomicInteger informationPercent = new AtomicInteger(0);

    // TODO 1. Resz: Keszitsuk el az gatheringInformation valtozot, hogy nyomon kovessuk, hogy meg info gyujtesi fazis van, vagy nem
    // TODO 1. Resz: gatheringInformation = true
    private final AtomicBoolean gatheringInformation = new AtomicBoolean(true);

    /**
     * A singleton privat konstruktora
     */
    private SharedInformation() {}

    /**
     * Amennyiben letezik mar objektum, azt visszaadja, ha nem, letrehoz egyet
     * @return A peldany
     */
    public static SharedInformation getInstance() {
        if (instance == null)
            instance = new SharedInformation();
        return instance;
    }

    /**
     * Egy detektiv ezzel a metodussal kontributal a megszerzett informaciok tarhazahoz.
     * Ha ez a szazalek eleri a 100-at, befejezzuk a kihallgatasi fazist.
     * Ez a szazalek sosem mehet 100 fole
     * @param percent Amennyivel egy detektiv hozzajarul
     */
    public void addNewInformation(final int percent) {
        // TODO 1. Resz: Biztositsuk be, hogy az informationPercent nem mehet 100 fole, adjuk hozza a percent erteket
        // TODO 1. Resz: Mikor eloszor eleri a 100-at az informationPercent, irjuk ki
        // TODO 1. Resz: System.out.println("Got all the information needed!");
        // TODO 1. Resz: Es allitsuk at a gatheringInformation erteket false-ra
        // TODO 1. Resz: Ha az informationPercent a percent hozzaadasa utan nem erte meg el a 100-at,
        // TODO 1. Resz: irjuk ki az informationPercent uj erteket
        // TODO 1. Resz: System.out.println("Information gathering is at " + $(informationValue uj erteke) + "%");
        if(gatheringInformation.get()){
            int newPercent = Math.min(INFORMATION_PERCENT_GOAL, informationPercent.addAndGet(percent));
            if (newPercent >= INFORMATION_PERCENT_GOAL) {
                System.out.println("Got all the information needed!");
                gatheringInformation.set(false);
                synchronized (this) { 
                    notifyAll();
                }
            }
            else {
                System.out.println("Information gathering is at " + (newPercent) + "%");
            }
        }
    }

    /**
     * Tart-e meg a kihallgatasi fazis, vagy mar vege
     */
    public boolean isGatheringInformation() {
        // TODO 1. Resz: Adjuk vissza a gatheringInformation erteket
        return gatheringInformation.get();
    }

    public synchronized void waitUntilInformationGathered() throws InterruptedException {
        while (gatheringInformation.get()) {
            wait();
        }
    }
}
