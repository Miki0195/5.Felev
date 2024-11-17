import java.util.*;
import java.util.concurrent.locks.ReentrantLock;

class Farm {
    private final int width;
    private final int height;
    private final Object[][] field;
    private final ReentrantLock lock = new ReentrantLock();
    private final List<Sheep> sheepList = new ArrayList<>();
    private final List<Dog> dogList = new ArrayList<>();
    private final List<int[]> gates = new ArrayList<>();
    private volatile boolean running; // A szimuláció futásának jelzése

    public Farm(int width, int height) {
        this.width = width;
        this.height = height;
        this.running = true;
        field = new Object[height][width];
        initializeField();
        placeGates();
        placeSheepAndDogs();
    }

    private void initializeField() {
        // Építsük fel a falakat
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++) {
                if (i == 0 || i == height - 1 || j == 0 || j == width - 1) {
                    field[i][j] = new Wall();  // Fal
                } else {
                    field[i][j] = new Empty();  // Üres mező
                }
            }
        }
    }

    private void placeGates() {
        // Négy kapu elhelyezése a falakon, véletlenszerű, nem sarok pozíciók
        Random rand = new Random();
        int[] positions = new int[4];
        positions[0] = rand.nextInt(width - 2) + 1; // Felső fal
        positions[1] = rand.nextInt(width - 2) + 1; // Alsó fal
        positions[2] = rand.nextInt(height - 2) + 1; // Bal fal
        positions[3] = rand.nextInt(height - 2) + 1; // Jobb fal

        field[0][positions[0]] = new Gate();
        gates.add(new int[]{0, positions[0]});
        field[height - 1][positions[1]] = new Gate();
        gates.add(new int[]{height - 1, positions[1]});
        field[positions[2]][0] = new Gate();
        gates.add(new int[]{positions[2], 0});
        field[positions[3]][width - 1] = new Gate();
        gates.add(new int[]{positions[3], width - 1});
    }

    private void placeSheepAndDogs() {
        // Helyezzünk el 10 juhot és 5 kutyát a farm különböző zónáiban
        for (int i = 0; i < 10; i++) {
            Sheep sheep = new Sheep(this);
            sheepList.add(sheep);
            new Thread(sheep, String.valueOf((char) ('A' + i))).start();
        }
        for (int i = 0; i < 5; i++) {
            Dog dog = new Dog(this);
            dogList.add(dog);
            new Thread(dog, String.valueOf(i)).start();
        }
    }

    public void display() {
        lock.lock();
        try {
            System.out.print("\033[H\033[2J");
            for (Object[] row : field) {
                for (Object cell : row) {
                    System.out.print(cell.toString());
                }
                System.out.println();
            }
        } finally {
            lock.unlock();
        }
    }

    public boolean isGate(int x, int y) {
        return gates.stream().anyMatch(gate -> gate[0] == x && gate[1] == y);
    }

    public boolean moveAnimal(int oldX, int oldY, int newX, int newY, Object animal) {
        lock.lock();
        try {
            if (newX < 0 || newX >= height || newY < 0 || newY >= width) {
                return false;
            }
            if (!(field[newX][newY] instanceof Empty) && !(field[newX][newY] instanceof Gate)) {
                return false;
            }
            if (oldX != -1 && oldY != -1) {
                field[oldX][oldY] = new Empty();
            }
            // Ellenőrizzük, hogy a célpozíció egy Gate és az animal egy Sheep
            if (animal instanceof Sheep && field[newX][newY] instanceof Gate) {
                System.out.println("Egy juh megszökött!");
                System.out.println("Coordinates: " + newX + ", " + newY);
                stopSimulation();  // Leállítjuk a szimulációt
            }
            field[newX][newY] = animal;
            return true;
        } finally {
            lock.unlock();
        }
    }

    public int[] getRandomEmptyPosition() {
        Random rand = new Random();
        int x, y;
        do {
            x = rand.nextInt(height - 2) + 1;
            y = rand.nextInt(width - 2) + 1;
        } while (!(field[x][y] instanceof Empty));
        return new int[]{x, y};
    }

    public int getHeight() {
        return height;
    }

    public int getWidth() {
        return width;
    }

    public Object[][] getField() {
        return field;
    }

    public boolean isRunning() {
        return running;
    }

    public void stopSimulation() {
        this.running = false;
        System.out.println("A szimuláció leállt!");
    }
}