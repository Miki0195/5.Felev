public class main {
    public static void main(String[] args) throws InterruptedException {
        SpaceFleet fleet = new SpaceFleet();
        fleet.launchFleet(100, 10, 5);
        fleet.analyzeFleet();
        fleet.shutdown();
    }
}