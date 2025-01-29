import java.util.concurrent.BlockingQueue;
import java.util.concurrent.Future;

public class Analyzer implements Runnable {
    private final BlockingQueue<Future<SpacecraftData>> fleetData;
    private final String type;

    public Analyzer(BlockingQueue<Future<SpacecraftData>> fleetData, String type) {
        this.fleetData = fleetData;
        this.type = type;
    }

    @Override
    public void run() {
        int count = 0;

        try {
            Future<SpacecraftData> futureData;
            while ((futureData = fleetData.poll()) != null) { // Remove elements until the queue is empty
                SpacecraftData data = futureData.get(); // Get the spacecraft data
                if ("Survivors".equals(type) && data.isCrewAlive()) {
                    count++;
                } else if ("Lost".equals(type) && !data.isCrewAlive()) {
                    count++;
                }
            }
            System.out.println(type + ": " + count);
        } catch (Exception e) {
            System.err.println("Error in " + type + " analyzer: " + e.getMessage());
        }
    }
}