import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

public class Spacecraft {
    private final String name;
    private final PowerSupplier powerSupplier;
    private final SpacecraftData spacecraftData = new SpacecraftData();
    private final ScheduledExecutorService scheduler = Executors.newScheduledThreadPool(3);

    public Spacecraft(String name, int initialPower, int powerNeededPrimary, int powerNeededSecondary) {
        this.name = name;
        this.powerSupplier = new PowerSupplier(initialPower);
        scheduler.scheduleAtFixedRate(new OxygenGenerator(name, powerSupplier, spacecraftData, powerNeededPrimary), 0, 1, TimeUnit.SECONDS);
        scheduler.scheduleAtFixedRate(new CommunicationHandler(name, powerSupplier, spacecraftData, powerNeededSecondary), 0, 2, TimeUnit.SECONDS);
        scheduler.scheduleAtFixedRate(new PropulsionController(name, powerSupplier, spacecraftData, powerNeededSecondary), 0, 2, TimeUnit.SECONDS);
    }

    public void stop() {
        scheduler.shutdown();
    }

    public SpacecraftData getSpacecraftData() {
        return spacecraftData;
    }
}
