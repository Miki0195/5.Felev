import java.util.concurrent.*;

public class SpaceFleet {
    private final BlockingQueue<Future<SpacecraftData>> fleetData = new LinkedBlockingQueue<>();
    private final ExecutorService fleetExecutor = Executors.newFixedThreadPool(10);

    public void launchFleet(int initialPower, int powerNeededPrimary, int powerNeededSecondary) {
        for (int i = 0; i < 10; i++) {
            String name = "Endymion-" + i;
            Spacecraft spacecraft = new Spacecraft(name, initialPower, powerNeededPrimary, powerNeededSecondary);

            Future<SpacecraftData> futureData = fleetExecutor.submit(() -> {
                Thread.sleep(10000); // Simulate spacecraft operation for 10 seconds
                spacecraft.stop(); // Stop spacecraft systems
                return spacecraft.getSpacecraftData(); // Return the spacecraft data
            });

            fleetData.add(futureData);
        }
    }

    public void analyzeFleet() throws InterruptedException {
        ExecutorService analyzerExecutor = Executors.newFixedThreadPool(2);

        // Create two analyzers: one for survivors, one for lost ships
        analyzerExecutor.submit(new Analyzer(fleetData, "Survivors"));
        analyzerExecutor.submit(new Analyzer(fleetData, "Lost"));

        analyzerExecutor.shutdown();
        analyzerExecutor.awaitTermination(2, TimeUnit.SECONDS); // Wait for analyzers to finish
    }

    public void shutdown() {
        fleetExecutor.shutdown();
        try {
            if (!fleetExecutor.awaitTermination(5, TimeUnit.SECONDS)) {
                fleetExecutor.shutdownNow();
            }
        } catch (InterruptedException e) {
            fleetExecutor.shutdownNow();
            Thread.currentThread().interrupt();
        }
    }
}