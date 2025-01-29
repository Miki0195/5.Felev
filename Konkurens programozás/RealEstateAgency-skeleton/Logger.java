import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;

public class Logger extends Thread {
    private final Agency agency;
    /* TODO Használj egy alkalmas BlockingQueue implementációt, valamilyen korláttal (pl.: 50-nel). */
    public final BlockingQueue<String> messages =  new LinkedBlockingQueue<>(50);

    public Logger(Agency agency) {
        this.agency = agency;
    }

    @Override
    public void run() {
        // Amíg az iroda nyitva van (`isOpen`):
        //   - a sorból vegyünk ki egy elemet,
        //   - írd ki a kivett elemet (szinkronizáció szükséges).
        // Tipp: a kivétel-kezeléssel foglalkoznod kell.
        // TODO
        while (agency.isOpen.get() || !messages.isEmpty()) { 
            String message = messages.poll(); // Use poll to avoid indefinite blocking
            if (message != null) {
                synchronized (System.out) {
                    System.out.println(message);
                }
            }
        }
    }

    public void logMessage(String message) {
        // TODO Tegyük bele a paraméterként kapott elemet a sorba.
        try {
            messages.put(message);
        } catch (InterruptedException e) {
            throw new RuntimeException();
        }
    }
}
