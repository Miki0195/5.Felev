import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;

public class test {

    private static final int PRODUCING_PERIOD_MS = 500;
    private static final int CONSUME_PERIOD_MS = 100;
    private static final int PRINT_PERIOD_MS = 2000;

    private static final List<Integer> collection = Collections.synchronizedList(new ArrayList<>());
    private static final AtomicBoolean shouldProduce = new AtomicBoolean(true);
    private static final AtomicBoolean shouldConsume = new AtomicBoolean(true);
    private static final Object consumerChecker = new Object();

    private static final AtomicInteger totalConsumed = new AtomicInteger(0);
    private static final AtomicInteger sumOfConsumed = new AtomicInteger(0);

    public static void main(String[] args) {
        ExecutorService executorService = Executors.newCachedThreadPool();

        // Start producers and consumers
        executorService.submit(test::producer);
        executorService.submit(test::consumer);
        executorService.submit(test::printer);

        // Stop producers after 30 seconds
        producerStopper();

        // Stop consumers if the list is empty for 5 seconds
        consumerStopper();

        // Stop the simulation after 50 seconds
        executorService.shutdown();
        try {
            executorService.awaitTermination(5000, TimeUnit.MILLISECONDS);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        } finally {
            executorService.shutdownNow();
        }

        // Final statistics
        double average = totalConsumed.get() > 0 ? (double) sumOfConsumed.get() / totalConsumed.get() : 0;
        System.out.println("Total Elements Consumed: " + totalConsumed.get());
        System.out.println("Average of Consumed Elements: " + average);
    }

    private static void producer() {
        while (shouldProduce.get()) {
            int count = ThreadLocalRandom.current().nextInt(1, 6); // Produce 1-5 items
            for (int i = 0; i < count; i++) {
                int value = ThreadLocalRandom.current().nextInt(1, 101); // Values between 1 and 100
                collection.add(value);
                synchronized (consumerChecker) {
                    consumerChecker.notify(); // Wake up consumer if waiting
                }
            }

            try {
                Thread.sleep(PRODUCING_PERIOD_MS);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    private static void producerStopper() {
        try {
            Thread.sleep(30000);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
        shouldProduce.set(false);
    }

    private static void consumer() {
        while (shouldConsume.get()) {
            synchronized (collection) {
                if (!collection.isEmpty()) {
                    int randomIndex = ThreadLocalRandom.current().nextInt(collection.size());
                    int value = collection.remove(randomIndex); // Remove a random value
                    totalConsumed.incrementAndGet();
                    sumOfConsumed.addAndGet(value);
                }
            }

            try {
                Thread.sleep(CONSUME_PERIOD_MS);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    private static void consumerStopper() {
        synchronized (consumerChecker) {
            try {
                consumerChecker.wait(5000); // Wait for 5 seconds
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
        shouldConsume.set(false);
    }

    private static void printer() {
        while (shouldProduce.get() || shouldConsume.get()) {
            synchronized (collection) {
                System.out.println("Current List: " + collection);
            }

            try {
                Thread.sleep(PRINT_PERIOD_MS);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    
}
