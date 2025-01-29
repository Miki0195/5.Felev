import java.util.List;
import java.util.concurrent.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;


/**
 * Ebben a szimulacioban nehany detektiv a brooklyni 99. korzetbol ki fog hallgatni nehany bunozot,
 * annak erdekeben, hogy informaciot szerezzenek a feletteseikrol. Mikor kozosen sikerult az osszes
 * lehetseges informaciot megszerezniuk errol a bunos organizaciorol, egyutt megprobaljak letartoztatni
 * a tobbi bunozot.
 */
public class Simulation {
    private static final int NUMBER_OF_ADDITIONAL_THREADS = 2;
    private static final int PERP_PRODUCER_WAIT_TIME_MSEC = 100;
    private static final int PERP_QUEUE_LIMIT = 5;

    private static final List<Detective> detectives = List.of(
        new Detective("Jake"),
        new Detective("Amy"),
        new Detective("Charles"),
        new Detective("Rosa"),
        new Detective("Terry"),
        new Detective("Hitchcock"),
        new Detective("Scully")
    );

    // TODO 1. Resz: Hozzuk letre detectives.size() merettel
    private static final ExecutorService detectiveExecutor = Executors.newFixedThreadPool(detectives.size());
    // TODO 1. Resz: Hozzuk letre NUMBER_OF_ADDITIONAL_THREADS merettel
    private static final ExecutorService executionExecutor = Executors.newFixedThreadPool(NUMBER_OF_ADDITIONAL_THREADS);
    // TODO 1. Resz: Hozzuk letre PERP_QUEUE_LIMIT limittel
    private static final BlockingQueue<Perpetrator> perpQueue = new LinkedBlockingQueue<>(PERP_QUEUE_LIMIT);
    // TODO 2. Resz: Csinaljunk egy valtozot, amivel nyomon kovetjuk,hogy vege van-e a szimulacionak, vagy nem (simulationOver)
    // TODO 2. Resz: simulationOver = false
    private static final AtomicBoolean simulationOver = new AtomicBoolean(false);
    private static final Object simulationLock = new Object(); 
    // For CountDownLatch
    private static final CountDownLatch detectiveLatch = new CountDownLatch(detectives.size()); 
    // For the other method
    public static int readyDetectives = 0; 
    public static final Object readyLock = new Object();


    /**
     * 1. Inditsunk uj szalat, amivel letrehozzuk a bunozoket (producePerpetrator)
     * 2. Inditsunk uj szalat, amivel elkaphatjuk a tobbi bunozot (catchCrimeBosses)
     * 3. Minden detektivnek inditsuk el a kihallgatasi (interrogation) metodusat
     * 4. Varjunk, amig a szimulacionak vege
     * 5. Menjunk biztosra, hogy a program terminal
     */
    public static void main(String[] args) {
        // TODO 1. Resz: Inditsuk el a producePerpetrator muveletet egy kulon szalon az executionExecutor hasznalataval
        executionExecutor.submit(Simulation::producePerpetrator);

        // TODO 2. Resz: Inditsuk el a catchCrimeBosses muveletet egy kulon szalon az executionExecutor hasznalataval 
        executionExecutor.submit(Simulation::catchCrimeBosses);

        // TODO 1. Resz: Inditsuk el minden detektivnek az interrogate muveletet a detectiveExecutor hasznalataval
        // For CountDownLatch
        // for (Detective detective : detectives) {
        //     detectiveExecutor.submit(() -> detective.interrogate(perpQueue, detectiveLatch));
        // }
        // For the other method
        for (Detective detective : detectives) {
            detectiveExecutor.submit(() -> detective.interrogate(perpQueue));
        }

        // TODO 1. Resz: Varjunk 20 masodpercet (kommenteljuk ki a masodik reszre)
        // try {
        //     Thread.sleep(20000);
        // } catch (InterruptedException e) {
        //     throw new RuntimeException(e);
        // }        

        // TODO 2. Resz: Varjunk, amig vege a szimulacionak (az 1. resz idozitett varakozasa helyett)
        synchronized (simulationLock) {
            while (!simulationOver.get()) {
                try {
                    simulationLock.wait();
                } catch (InterruptedException e) {
                    throw new RuntimeException(e);
                }
            }
        }
        // TODO 1. Resz: Gyozodjunk meg rola, hogy a programunk leall
        shutdownExecutors();
    }

    /**
     * Letrehoz egy uj bunozot minden $PERP_PRODUCER_WAIT_TIME_MSEC idokozonkent - ha egy
     * uj bunozo nem fer bele a queue-ba, akkor maskor lesz kihallgatva (nem foglalkozunk vele)
     */
    private static void producePerpetrator() {
        while(SharedInformation.getInstance().isGatheringInformation()) {
            Perpetrator perp = new Perpetrator();

            // TODO 1. Resz: Adjuk hozza a perp-et a queue-hoz, ha tudjuk, majd logoljuk
            // TODO System.out.println(perp + " is ready to be interrogated");
            // TODO 1. Resz: Ha a queue teli van, logoljuk
            // TODO System.out.println(perp + " will be interrogated another day");
            if (perpQueue.offer(perp)) {
                System.out.println(perp + " is ready to be interrogated");
            }
            else {
                System.out.println(perp + " will be interrogated another day");
            }
            
            // TODO 1. Resz: Varjunk $PERP_PRODUCER_WAIT_TIME_MSEC msec-et
            try {
                Thread.sleep(PERP_PRODUCER_WAIT_TIME_MSEC);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    private static void shutdownExecutors() {
        detectiveExecutor.shutdown();
        executionExecutor.shutdown();
        try {
            detectiveExecutor.awaitTermination(5, TimeUnit.SECONDS);
            executionExecutor.awaitTermination(5, TimeUnit.SECONDS);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        } finally {
            detectiveExecutor.shutdownNow();
            executionExecutor.shutdownNow();
        }
        System.out.println("Simulation terminated");
    }

    // private static void shutdownExecutors() {
    //     try {
    //         detectiveExecutor.shutdown();
    //         executionExecutor.shutdown();
    //         if (!detectiveExecutor.awaitTermination(5, TimeUnit.SECONDS)) {
    //             detectiveExecutor.shutdownNow();
    //         }
    //         if (!executionExecutor.awaitTermination(5, TimeUnit.SECONDS)) {
    //             executionExecutor.shutdownNow();
    //         }
    //     } catch (InterruptedException e) {
    //         detectiveExecutor.shutdownNow();
    //         executionExecutor.shutdownNow();
    //         Thread.currentThread().interrupt();
    //     }
    //     System.out.println("Simulation terminated");
    // }

    /**
     * Varjunk, amig a detektivek megszerzik a szukseges informaciot, majd elkapjak a tobbi bunozot.
     * Ha elkaptak oket, a szimulacio leall.
     */
    private static void catchCrimeBosses() {
        // TODO 2. Resz: Varjunk, amig az osszes informaciot megszereztek a detektivek, es mindegyik keszen all
        // TODO 2. Resz: Allitsuk igazra a simulationOver erteket
        // TODO 2. Resz: Jelezzuk, hogy vege van a szimulacionak
        try {
            SharedInformation.getInstance().waitUntilInformationGathered();
            // For CountDownLatch
            // detectiveLatch.await();
            // For the other method
            synchronized (readyLock) {
                while (readyDetectives < detectives.size()) { 
                    readyLock.wait();
                }
            }

            System.out.println("The detectives caught the crime bosses!");

            synchronized (simulationLock) {
                simulationOver.set(true); 
                simulationLock.notifyAll();
            }
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
    }

}
