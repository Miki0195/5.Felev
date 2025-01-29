public class OxygenGenerator implements Runnable {
    private final String spacecraftName;
    private final PowerSupplier powerSupplier;
    private final SpacecraftData spacecraftData;
    private final int powerNeeded;

    public OxygenGenerator(String spacecraftName, PowerSupplier powerSupplier, SpacecraftData spacecraftData, int powerNeeded) {
        this.spacecraftName = spacecraftName;
        this.powerSupplier = powerSupplier;
        this.spacecraftData = spacecraftData;
        this.powerNeeded = powerNeeded;
    }

    @Override
    public void run() {
        try {
            if (!spacecraftData.isCrewAlive()) return;

            if (powerSupplier.consumePower(powerNeeded)) {
                System.out.println("[" + spacecraftName + "]: OxygenGenerator is running, consuming " + powerNeeded + " units of power.");
            } else {
                Thread.sleep(2000); 
                if (powerSupplier.consumePower(powerNeeded)) {
                    System.out.println("[" + spacecraftName + "]: OxygenGenerator is running, consuming " + powerNeeded + " units of power.");
                } else {
                    System.out.println("[" + spacecraftName + "]: the crew has died (OxygenGenerator failed due to insufficient power)!");
                    spacecraftData.setCrewDead();
                }
            }
        } catch (Exception e) {
            System.out.println("[" + spacecraftName + "]: Re-running oxygen checks...");
        }
    }
}