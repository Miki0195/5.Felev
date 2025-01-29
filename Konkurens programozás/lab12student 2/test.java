import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.concurrent.*;

/**
 * Egy koncertre negyfajta jegy kaphato: az A, B, C es D szektorokba.
 * Mindegyik jegyfajtabol 5 erheto el, osszesen tehat 20 jegy kaphato.
 * Mindegyik vasarlo eseten adott, melyik szektorba tartozo jegyet szeretne venni.
 */
public class test {
    private static final int SLEEP_TIME_MIN = 100;
    private static final int SLEEP_TIME_MAX = 500;
    private static final int SHUTDOWN_TIME = 15000;

    private static final List<String> TICKET_TYPES = List.of("SECTOR A", "SECTOR B", "SECTOR C", "SECTOR D");

    private static final int TICKET_COUNT_PER_SELLER = 5;
    private static final int SELLER_COUNT = TICKET_TYPES.size();
    private static final int CUSTOMER_COUNT = SELLER_COUNT * TICKET_COUNT_PER_SELLER;

    private static final Map<String, Integer> TICKET_INVENTORY = new ConcurrentHashMap<>();
    private static final BlockingQueue<String> TICKET_QUEUE = new ArrayBlockingQueue<>(1);

    private static final ExecutorService executorService = Executors.newFixedThreadPool(CUSTOMER_COUNT + SELLER_COUNT);

    public static void main(String[] args) {
        // Jegykeszlet feltoltese
        TICKET_TYPES.forEach(str -> TICKET_INVENTORY.put(str, TICKET_COUNT_PER_SELLER));

        // Inditsuk el a sellerAction-t minden jegytipusra kulon szalon
        TICKET_TYPES.forEach(ticketType -> executorService.execute(() -> sellerAction(ticketType)));

        // Inditsuk el a customerAction-t minden kliensre a megfelelo jegytipussal kulon szalon
        for (int i = 0; i < CUSTOMER_COUNT; i++) {
            String ticketType = TICKET_TYPES.get(i % SELLER_COUNT);
            int customerId = i + 1;
            executorService.execute(() -> customerAction(customerId, ticketType));
        }

        // A szimulacionak SHUTDOWN_TIME elteltevel le kell allnia
        try {
            Thread.sleep(SHUTDOWN_TIME);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
        executorService.shutdownNow();
    }

    /**
     * Ez a jegyarus mukodesenek megvalositasa.
     */
    private static void sellerAction(String ticketType) {
        System.out.printf("Started seller for %s%n", ticketType);

        while (TICKET_INVENTORY.get(ticketType) > 0) {
            try {
                // Ha a TICKET_INVENTORY-ban az adott jegyfajta meg nem fogyott el:
                // Csokkenjen eggyel a jegyek szama
                TICKET_INVENTORY.computeIfPresent(ticketType, (key, value) -> value - 1);

                // Adjuk a megfelelo fajta jegyet a TICKET_QUEUE-hoz
                TICKET_QUEUE.put(ticketType);

                // Irjuk ki az uzenetet
                System.out.println("New ticket available for " + ticketType);

                // Varjunk valamennyit SLEEP_TIME_MIN es SLEEP_TIME_MAX kozott
                Thread.sleep(ThreadLocalRandom.current().nextInt(SLEEP_TIME_MIN, SLEEP_TIME_MAX));
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                break;
            }
        }

        // Miutan elfogyott a raktarrol az osszes ilyen tipusu jegy
        System.out.printf("Tickets for %s sold out%n", ticketType);
    }

    /**
     * Ez a vasarlo mukodesenek megvalositasa.
     */
    private static void customerAction(int customerId, String ticketType) {
        while (!Thread.currentThread().isInterrupted()) {
            try {
                // Nezze meg, van-e jegy a TICKET_QUEUE-ban a megfelelo jegytipusbol (peek muvelet)
                String availableTicket = TICKET_QUEUE.peek();

                if (ticketType.equals(availableTicket)) {
                    // Vegye ki es menjen el a koncertre
                    TICKET_QUEUE.poll();
                    System.out.printf("Customer %d got a ticket to %s%n", customerId, ticketType);
                    return;
                }

                // Varjon valamennyit SLEEP_TIME_MIN es SLEEP_TIME_MAX kozott
                Thread.sleep(ThreadLocalRandom.current().nextInt(SLEEP_TIME_MIN, SLEEP_TIME_MAX));
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                break;
            }
        }
    }
}