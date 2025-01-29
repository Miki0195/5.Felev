import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Random;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class ProducerConsumerPractice {

    private static final int PRODUCING_PERIOD_MS = 500; // Producer runs faster
    private static final int CONSUME_PERIOD_MS = 200;  // Consumer runs slower
    private static final int PRINT_PERIOD_MS = 2000;
    private static final int SIMULATION_RUNTIME_MS = 50000;

    private final List<Integer> sharedList;
    private final Random random;
    private int totalConsumed = 0;
    private double average = 0;
    private final AtomicBoolean isRunning = new AtomicBoolean(true);

    public ProducerConsumerPractice() {
        this.sharedList = Collections.synchronizedList(new ArrayList<>());
        this.random = new Random();
    }

    public void producerTask() {
        while (isRunning.get()) {
            int value = random.nextInt(100) + 1; // Random number between 1 and 100
            sharedList.add(value);
            System.out.println("Produced: " + value);
            sleep(PRODUCING_PERIOD_MS);
        }
    }

    public void consumerTask() {
        while (isRunning.get() || !sharedList.isEmpty()) {
            synchronized (sharedList) {
                if (!sharedList.isEmpty()) {
                    int index = random.nextInt(sharedList.size());
                    int value = sharedList.remove(index);
                    totalConsumed++;
                    average = ((average * (totalConsumed - 1)) + value) / totalConsumed;
                    System.out.println("Consumed: " + value);
                }
            }
            sleep(CONSUME_PERIOD_MS);
        }
    }

    public void printTask() {
        while (isRunning.get()) {
            synchronized (sharedList) {
                System.out.println("Current List: " + sharedList);
            }
            sleep(PRINT_PERIOD_MS);
        }
    }

    private void sleep(int millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }

    public void startSimulation() {
        ExecutorService executorService = Executors.newCachedThreadPool();

        executorService.submit(this::producerTask);
        executorService.submit(this::producerTask); // Additional producer
        executorService.submit(this::consumerTask);
        executorService.submit(this::consumerTask); // Additional consumer
        executorService.submit(this::printTask);

        // Run the simulation for the defined time
        try {
            Thread.sleep(SIMULATION_RUNTIME_MS);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }

        // Signal all tasks to stop
        isRunning.set(false);

        // Shut down the executor service gracefully
        executorService.shutdown();
        try {
            if (!executorService.awaitTermination(10, TimeUnit.SECONDS)) {
                executorService.shutdownNow();
            }
        } catch (InterruptedException e) {
            executorService.shutdownNow();
        }

        // Final statistics
        System.out.println("Total Elements Consumed: " + totalConsumed);
        System.out.println("Average of Consumed Elements: " + average);
    }

    public static void main(String[] args) {
        ProducerConsumerPractice simulation = new ProducerConsumerPractice();
        simulation.startSimulation();
    }
}
