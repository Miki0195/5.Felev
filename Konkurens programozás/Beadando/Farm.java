import java.io.IOException;
import java.util.Random;

public class Farm {
    public static final Object SimStartLock = new Object();
    public static final Object SimEndLock = new Object();
    public static Object[][] positionLocks;
    public static Object[][] Map;
    public static boolean SimEnded = false;
    public static int Width;
    public static int Height;
    public static String WinnerSheep;

    public Farm(int width, int height) {
        Width = width;
        Height = height;
        initializeFarm();
    }

    private void initializeFarm() {
        positionLocks = new Object[Height][Width];
        Map = new Object[Height][Width];
        fillFarm();
        placeGates();
    }

    private void fillFarm() {
        for (int i = 0; i < Height; i++) {
            for (int j = 0; j < Width; j++) {
                if (isBorder(i, j)) {
                    Map[i][j] = new Wall();
                } else {
                    Map[i][j] = new Empty();
                }
                positionLocks[i][j] = new Object();
            }
        }
    }

    private boolean isBorder(int i, int j) {
        return i == 0 || i == Height - 1 || j == 0 || j == Width - 1;
    }

    private void placeGates() {
        Random rand = new Random();
        placeGate(rand.nextInt(1, Width - 1), 0); 
        placeGate(rand.nextInt(1, Width - 1), Height - 1); 
        placeGate(0, rand.nextInt(1, Height - 1)); 
        placeGate(Width - 1, rand.nextInt(1, Height - 1)); 
    }

    
    private void placeGate(int x, int y) {
        Map[y][x] = new Gate();
    }

    
    public synchronized void printMap() {
        try {
            clearConsole();
            for (int i = 0; i < Height; i++) {
                for (int j = 0; j < Width; j++) {
                    synchronized (positionLocks[i][j]) {
                        System.out.print(Map[i][j].toString());
                    }
                }
                System.out.println();
            }
        } catch (IOException e) {
            System.out.println("Error during map printing: " + e.getMessage());
        }
    }

    
    private void clearConsole() throws IOException {
        System.out.print("\033[H\033[2J");
        System.out.flush();
    }

    
    public static void sheepEscaped(Sheep winner) {
        SimEnded = true;
        WinnerSheep = winner.toString();
        synchronized (SimEndLock) {
            SimEndLock.notifyAll();
        }
    }

    
    public void startDisplayLoop() {
        new Thread(() -> {
            try {
                while (!SimEnded) {
                    Thread.sleep(200);
                    printMap();
                }
                printMap();
                System.out.println("\nSheep named " + WinnerSheep + " has escaped!");
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                System.out.println("Display thread interrupted.");
            }
        }).start();
    }
}
