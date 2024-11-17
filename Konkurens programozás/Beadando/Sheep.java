import java.util.Random;

class Sheep implements Runnable {
    private final Farm farm;
    private int x, y;
    private String name;
    private static final int SLEEP_TIME = 200;

    public Sheep(Farm farm) {
        this.farm = farm;
        int[] pos = getRandomStartPosition();
        this.x = pos[0];
        this.y = pos[1];
        farm.moveAnimal(-1, -1, x, y, this);
    }

    @Override
    public void run() {
        this.name = Thread.currentThread().getName();
        while (farm.isRunning()) {
            try {
                Thread.sleep(SLEEP_TIME);
                move();

                // Ellenőrizzük, hogy a juh kapura lépett-e
                if (farm.isGate(x, y)) {
                    System.out.println("A sheep has escaped!");
                    //print the coordinates of the escaped sheep
                    System.out.println("Coordinates: " + x + ", " + y);
                    farm.stopSimulation();  // Leállítjuk a szimulációt
                }
                
                farm.display();
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }
    }

    private void move() {
        int[] direction = determineDirection();
        int newX = x + direction[0];
        int newY = y + direction[1];

        // Próbáljuk meg a juhot új pozícióba mozgatni, ha lehetséges
        if (farm.moveAnimal(x, y, newX, newY, this)) {
            x = newX;
            y = newY;
        }
    }

    private int[] determineDirection() {
        int[] direction = new int[2];
        boolean dogNearbyX = false;
        boolean dogNearbyY = false;

        // Ellenőrizzük a szomszédos mezőket kutyák jelenléte szempontjából
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 && j == 0) continue;
                
                if (isDogNearby(x + i, y + j)) {
                    if (i != 0) {
                        dogNearbyX = true;
                        direction[0] = -i;  // ellentétes irány az X tengelyen
                    }
                    if (j != 0) {
                        dogNearbyY = true;
                        direction[1] = -j;  // ellentétes irány az Y tengelyen
                    }
                }
            }
        }

        // Véletlenszerű irány, ha nincs kutya az adott dimenzióban
        Random rand = new Random();
        if (!dogNearbyX) {
            direction[0] = rand.nextInt(3) - 1;  // -1, 0 vagy 1
        }
        if (!dogNearbyY) {
            direction[1] = rand.nextInt(3) - 1;  // -1, 0 vagy 1
        }

        // Ellenőrizzük, hogy a mozgás ne legyen mindkét irányban 0
        while (direction[0] == 0 && direction[1] == 0) {
            direction[0] = rand.nextInt(3) - 1;
            direction[1] = rand.nextInt(3) - 1;
        }

        // Ellenőrizzük, hogy a mozgás ne legyen átlós
        if (direction[0] != 0 && direction[1] != 0) {
            if (rand.nextBoolean()) {
                direction[0] = 0;
            } else {
                direction[1] = 0;
            }
        }

        return direction;
    }

    private boolean isDogNearby(int checkX, int checkY) {
        // Ellenőrizzük, hogy az adott pozíció határokon belül van-e
        if (checkX < 0 || checkX >= farm.getHeight() || checkY < 0 || checkY >= farm.getWidth()) {
            return false;
        }
        
        // Ellenőrizzük, hogy a pozícióban kutya található-e
        Object cell = farm.getField()[checkX][checkY];
        return cell instanceof Dog;
    }
    
    // Random pozíció középső kilencedben
    private int[] getRandomStartPosition() {
        Random rand = new Random();
        int startX = rand.nextInt(farm.getHeight() / 3) + farm.getHeight() / 3;
        int startY = rand.nextInt(farm.getWidth() / 3) + farm.getWidth() / 3;
        return new int[]{startX, startY};
    }

    // A juh nevét adja vissza, azaz a szál nevét
    @Override
    public String toString() {
        return name;
    }
}
