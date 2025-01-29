import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Egy koncertre negyfajta jegy kaphato: az A, B, C es D szektorokba.
 * Mindegyik jegyfajtabol 5 erheto el, osszesen tehat 20 jegy kaphato.
 * Mindegyik vasarlo eseten adott, melyik szektorba tartozo jegyet szeretne venni.
 */
public class TicketSaleSimulator {
    private static final int SLEEP_TIME_MIN = 100;
    private static final int SLEEP_TIME_MAX = 500;
    private static final int SHUTDOWN_TIME = 15000;

    private static final List<String> TICKET_TYPES = List.of("SECTOR A", "SECTOR B", "SECTOR C", "SECTOR D");

    private static final int TICKET_COUNT_PER_SELLER = 5;
    private static final int SELLER_COUNT = TICKET_TYPES.size();
    private static final int CUSTOMER_COUNT = SELLER_COUNT * TICKET_COUNT_PER_SELLER;
    
    private static Map<String, Integer> TICKET_INVENTORY = new ConcurrentHashMap<>();/* TODO Hozzuk letre a map-et */
    private static BlockingQueue<String> TICKET_QUEUE = new LinkedBlockingQueue<>(1);/* TODO Hozzunk letre egy sort 1-es kapacitassal */
    // TODO Hozzuk letre a megfelelo meretu ExecutorService-t: minden vasarlo es mindegyik elado szamara legyen egy szalnak hely benne
    private static ExecutorService executor = Executors.newFixedThreadPool(SELLER_COUNT + CUSTOMER_COUNT);
    private static int idGenerator = 0;
    // private static AtomicInteger idGenerator = new AtomicInteger(0); // Konkurens megoldas

    public static void main(String[] args) {
        // Jegykeszlet feltoltese
        TICKET_TYPES.forEach(str -> TICKET_INVENTORY.put(str, TICKET_COUNT_PER_SELLER));

        // TODO Inditsuk el a sellerAction-t minden jegytipusra kulon szalon
        TICKET_TYPES.forEach(str -> executor.submit(() -> sellerAction(str)));
        // TODO Inditsuk el a customerAction-t minden kliensre a megfelelo jegytipussal kulon szalon
        // TODO - Jegyfajtankent 5 vasarlonak kell lennie
        TICKET_TYPES.forEach(type -> {
            for (int i = 0; i < TICKET_COUNT_PER_SELLER; i++) {
                executor.submit(() -> customerAction(idGenerator++, type));
                // executor.submit(() -> customerAction(idGenerator.incrementAndGet(), type)); // Konkurens megoldas
            }
        });
        // TODO A szimulacionak SHUTDOWN_TIME elteltevel le kell allnia
        executor.shutdown();
        try {
            executor.awaitTermination(CUSTOMER_COUNT, TimeUnit.MILLISECONDS);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        } finally {
            executor.shutdownNow();
        }
    }

    /**
     * Ez a jegyarus mukodesenek megvalositasa.
     */
    private static void sellerAction(String ticketType) {
        System.out.printf("Started seller for %s%n", ticketType);
        
        // TODO Ha a TICKET_INVENTORY-ban az adott jegyfajta meg nem fogyott el:
        // TODO - A TICKET_INVENTORY-ban csokkenjen eggyel a jegyek szama
        // TODO - Adjuk a megfelelo fajta jegyet a TICKET_QUEUE-hoz
        // TODO - Irjuk ki az alabbi szoveget:
        // System.out.println("New ticket available for " + ticketType);
        // TODO - Varjunk valamennyit SLEEP_TIME_MIN es SLEEP_TIME_MAX kozott a jegy eladasa utan

        while(TICKET_INVENTORY.get(ticketType) > 0){
            TICKET_INVENTORY.put(ticketType, TICKET_INVENTORY.get(ticketType) - 1);
            boolean isSuccesful = false;
            while(!isSuccesful){
                try {
                    isSuccesful = TICKET_QUEUE.offer(ticketType, SLEEP_TIME_MIN, TimeUnit.MILLISECONDS);
                } catch (InterruptedException e) {
                    throw new RuntimeException(e);
                }
            }
            System.out.println("New ticket available for " + ticketType);
            try {
                Thread.sleep(ThreadLocalRandom.current().nextInt(SLEEP_TIME_MIN, SLEEP_TIME_MAX));
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }


        // TODO Miutan elfogyott a raktarrol az osszes ilyen tipusu jegy, irjuk ki az alabbit, es fejezzuk be a futast
        System.out.printf("Tickets for %s sold out%n", ticketType);
    }

    /**
     * Ez a vasarlo mukodesenek megvalositasa.
     */
    private static void customerAction(int customerId, String ticketType) {
        // TODO Amig a vasarlo nem vett meg jegyet, tegye a kovetkezoket:
        // TODO - Nezze meg, van-e jegy a TICKET_QUEUE-ban a megfelelo jegytipusbol (peek muvelet)
        // TODO - Ha van, akkor vegye ki es menjen el a koncertre: irja ki az alabbi szoveget, es fejezze be a futasat
        // System.out.printf("Customer %d got a ticket to %s%n", customerId, ticketType);
        // TODO - Varjon valamennyit SLEEP_TIME_MIN es SLEEP_TIME_MAX kozott a kovetkezo vizsgalat elott

        boolean hasTicket = false;
        while(!hasTicket){
            String head = null;
            synchronized (TICKET_QUEUE) {
                head = TICKET_QUEUE.peek(); 
                if (head.equals(ticketType)) {
                    TICKET_QUEUE.poll();
                    hasTicket = true;
                }
                else{
                    continue;
                }
            }
            try {
                Thread.sleep(ThreadLocalRandom.current().nextInt(SLEEP_TIME_MIN, SLEEP_TIME_MAX));
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }
}
