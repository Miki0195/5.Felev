import java.util.Random;

class Dog implements Runnable {
    private final Farm farm;
    private int x, y;
    private String name;  // Új mező az egyedi azonosító tárolására
    private static final int SLEEP_TIME = 300;

    public Dog(Farm farm) {
        this.farm = farm;
        int[] pos = getRandomStartPosition();
        this.x = pos[0];
        this.y = pos[1];
        farm.moveAnimal(-1, -1, x, y, this);
    }

    @Override
    public void run() {
        this.name = Thread.currentThread().getName();  // A szál nevét elmentjük a name mezőbe
        while (farm.isRunning()) {
            try {
                Thread.sleep(SLEEP_TIME);
                move();
                farm.display();
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }
        System.out.println("Dog " + name + " has stopped.");
    }

    private void move() {
        int[] direction = determineDirection();
        int newX = x + direction[0];
        int newY = y + direction[1];
        if (farm.moveAnimal(x, y, newX, newY, this)) {
            x = newX;
            y = newY;
        }
    }

    private int[] determineDirection() {
        Random rand = new Random();
        int[] direction = new int[2];
        do {
            direction[0] = rand.nextInt(3) - 1;
            direction[1] = rand.nextInt(3) - 1;
        } while ((direction[0] == 0 && direction[1] == 0) || !isValidMove(x + direction[0], y + direction[1]));
        return direction;
    }

    private boolean isValidMove(int newX, int newY) {
        // Ellenőrizzük, hogy az új pozíció a külső nyolc kilencedben van-e
        int midX = farm.getHeight() / 3;
        int midY = farm.getWidth() / 3;
        return newX >= 0 && newX < farm.getHeight() && newY >= 0 && newY < farm.getWidth() &&
               !(newX >= midX && newX < 2 * midX && newY >= midY && newY < 2 * midY);
    }

    private int[] getRandomStartPosition() {
        Random rand = new Random();
        int startX, startY;
        int midX = farm.getHeight() / 3;
        int midY = farm.getWidth() / 3;
        do {
            startX = rand.nextInt(farm.getHeight());
            startY = rand.nextInt(farm.getWidth());
        } while (startX >= midX && startX < 2 * midX && startY >= midY && startY < 2 * midY);
        return new int[]{startX, startY};
    }

    @Override
    public String toString() {
        return name;
    }
}