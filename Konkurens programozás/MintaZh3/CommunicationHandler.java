public class CommunicationHandler implements Runnable {
    private final String spacecraftName;
    private final PowerSupplier powerSupplier;
    private final SpacecraftData spacecraftData;
    private SecondarySystemState state = SecondarySystemState.HEALTHY;
    private final int powerNeeded;

    public CommunicationHandler(String spacecraftName, PowerSupplier powerSupplier, SpacecraftData spacecraftData, int powerNeeded) {
        this.spacecraftName = spacecraftName;
        this.powerSupplier = powerSupplier;
        this.spacecraftData = spacecraftData;
        this.powerNeeded = powerNeeded;
    }

    @Override
    public void run() {
        if (!spacecraftData.isCrewAlive() || state == SecondarySystemState.MALFUNCTIONED) return;

        if (state == SecondarySystemState.HEALTHY) {
            if (!powerSupplier.consumePower(powerNeeded)) {
                state = SecondarySystemState.UNHEALTHY;
                System.out.println("[" + spacecraftName + "]: Not enough power for CommunicationHandler. Switching off...");
            } else {
                System.out.println("[" + spacecraftName + "]: CommunicationHandler is running, consuming " + powerNeeded + " units of power.");
            }
        } else if (state == SecondarySystemState.UNHEALTHY) {
            if (!powerSupplier.consumePower(powerNeeded)) {
                state = SecondarySystemState.CRITICAL;
                System.out.println("[" + spacecraftName + "]: the crew has died (A malfunction occurred in the CommunicationHandler)!");
                spacecraftData.setCrewDead();
            } else {
                state = SecondarySystemState.HEALTHY;
                System.out.println("[" + spacecraftName + "]: CommunicationHandler recovered and is now HEALTHY.");
            }
        }
    }
}