import java.util.Random;
import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.CyclicBarrier;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

public class Simulation {
    private final int farmWidth;
    private final int farmHeight;
    private final int sheepCount;
    private final int dogCount;
    private final Farm farm;
    private final CyclicBarrier barrier;
    private final ExecutorService pool;

    public Simulation(int farmWidth, int farmHeight, int sheepCount, int dogCount) {
        this.farmWidth = farmWidth;
        this.farmHeight = farmHeight;
        this.sheepCount = sheepCount;
        this.dogCount = dogCount;
        this.farm = new Farm(farmWidth, farmHeight);
        this.barrier = new CyclicBarrier(sheepCount + dogCount);
        this.pool = Executors.newFixedThreadPool(sheepCount + dogCount);
    }

    public void start() {
        farm.startDisplayLoop();
        initializeSheep();
        initializeDogs();
        startSimulation();
    }

    private void initializeSheep() {
        Random rand = new Random();
        int thirdHeight = (farmHeight - 2) / 3;
        int thirdWidth = (farmWidth - 2) / 3;

        for (int i = 0; i < sheepCount; i++) {
            int posY, posX;
            do {
                posY = rand.nextInt(thirdHeight + 1, 2 * thirdHeight + 1);
                posX = rand.nextInt(thirdWidth + 1, 2 * thirdWidth + 1);
            } while (!(Farm.Map[posY][posX] instanceof Empty));

            Sheep sheep = new Sheep(Character.toString('A' + i), posX, posY);
            Farm.Map[posY][posX] = sheep;

            pool.submit(() -> sheepThreadLogic(sheep));
        }
    }

    private void sheepThreadLogic(Sheep sheep) {
        try {
            synchronized (Farm.SimStartLock) {
                Farm.SimStartLock.wait();
            }
            while (!Farm.SimEnded) {
                sheep.move();
                if (!Farm.SimEnded) {
                    barrier.await();
                }
                Thread.sleep(200);
            }
        } catch (InterruptedException | BrokenBarrierException e) {
            if (!Farm.SimEnded) {
                System.out.println("Sheep thread interrupted: " + sheep);
                Thread.currentThread().interrupt();
            }
        }
    }

    private void initializeDogs() {
        Random rand = new Random();
        int thirdHeight = (farmHeight - 2) / 3;
        int thirdWidth = (farmWidth - 2) / 3;

        for (int i = 0; i < dogCount; i++) {
            int posY, posX;
            do {
                posY = rand.nextInt(1, farmHeight - 1);
                posX = rand.nextInt(1, farmWidth - 1);
            } while ((posY > thirdHeight && posY <= 2 * thirdHeight) || 
                     (posX > thirdWidth && posX <= 2 * thirdWidth) || 
                     !(Farm.Map[posY][posX] instanceof Empty));

            Dog dog = new Dog(i, posX, posY);
            Farm.Map[posY][posX] = dog;

            pool.submit(() -> dogThreadLogic(dog));
        }
    }

    private void dogThreadLogic(Dog dog) {
        try {
            synchronized (Farm.SimStartLock) {
                Farm.SimStartLock.wait();
            }
            while (!Farm.SimEnded) {
                dog.move();
                if (!Farm.SimEnded) {
                    barrier.await();
                }
                Thread.sleep(200);
            }
        } catch (InterruptedException | BrokenBarrierException e) {
            if (!Farm.SimEnded) {
                System.out.println("Dog thread interrupted: " + dog);
                Thread.currentThread().interrupt();
            }
        }
    }

    private void startSimulation() {
        try {
            Thread.sleep(100);
            synchronized (Farm.SimStartLock) {
                Farm.SimStartLock.notifyAll();
            }
            synchronized (Farm.SimEndLock) {
                Farm.SimEndLock.wait();
            }
            pool.shutdown();
            pool.awaitTermination(200, TimeUnit.MILLISECONDS);
        } catch (InterruptedException e) {
            System.out.println("Simulation interrupted.");
            Thread.currentThread().interrupt();
        }
    }

    public static void main(String[] args) {
        Simulation simulation = new Simulation(14, 14, 10, 5);
        simulation.start();
    }
}
