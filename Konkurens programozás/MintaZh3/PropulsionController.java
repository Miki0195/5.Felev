public class PropulsionController implements Runnable {
    private final String spacecraftName;
    private final PowerSupplier powerSupplier;
    private final SpacecraftData spacecraftData;
    private SecondarySystemState state = SecondarySystemState.HEALTHY;
    private final int powerNeeded;

    public PropulsionController(String spacecraftName, PowerSupplier powerSupplier, SpacecraftData spacecraftData, int powerNeeded) {
        this.spacecraftName = spacecraftName;
        this.powerSupplier = powerSupplier;
        this.spacecraftData = spacecraftData;
        this.powerNeeded = powerNeeded;
    }

    @Override
    public void run() {
        if (!spacecraftData.isCrewAlive() || state == SecondarySystemState.MALFUNCTIONED) return;

        switch (state) {
            case HEALTHY:
                if (!powerSupplier.consumePower(powerNeeded)) {
                    state = SecondarySystemState.UNHEALTHY;
                    System.out.println("[" + spacecraftName + "]: Not enough power for PropulsionController. Switching off...");
                } else {
                    System.out.println("[" + spacecraftName + "]: PropulsionController is running, consuming " + powerNeeded + " units of power.");
                }
                break;

            case UNHEALTHY:
                if (!powerSupplier.consumePower(powerNeeded)) {
                    state = SecondarySystemState.CRITICAL;
                    System.out.println("[" + spacecraftName + "]: the crew has died (A malfunction occurred in the PropulsionController)!");
                    spacecraftData.setCrewDead();
                } else {
                    state = SecondarySystemState.HEALTHY;
                    System.out.println("[" + spacecraftName + "]: PropulsionController recovered and is now HEALTHY.");
                }
                break;

            case CRITICAL:
                System.out.println("[" + spacecraftName + "]: the crew has died (A malfunction occurred in the PropulsionController)!");
                state = SecondarySystemState.MALFUNCTIONED;
                spacecraftData.setCrewDead();
                break;

            case MALFUNCTIONED:
                // System is already failed; no further actions are taken
                break;
        }
    }
}
