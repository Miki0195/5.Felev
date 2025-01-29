import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;

public class ProducerConsumerPracticeSolved {
    // TODO The actual contents of the list should be printed out periodically
    
    // TODO The whole simulation should end after 50 seconds
    
    // TODO At the end print out the number of elements consumed and the total average
    // TODO of the elements that have been in the list
    
    private static final int PRODUCING_PERIOD_MS = 500;
    private static final int CONSUME_PERIOD_MS = 100;
    private static final int PRINT_PERIOD_MS = 2000;
    
    // TODO After all this is done, try with more producers and more consumers
    
    // TODO Create a list for integers
    private static final List<Integer> collection = Collections.synchronizedList(new ArrayList<>());
    private static final AtomicBoolean shouldProduce = new AtomicBoolean(true);
    private static final AtomicBoolean shouldConsume = new AtomicBoolean(true);
    private static final Object consumerChecker = new Object();
    public static void main(String[] args) {
        
        // TODO The producers should stop working after 30 seconds (part1)
        try{
            Thread.sleep(30000);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
        shouldProduce.set(false);
        consumerStopper();
    }
    
    // TODO Create a producer that will periodically put random numbers ( between 1 and 5 )
    // TODO between 1 and 100 to this list
    // TODO The producers should stop working after 30 seconds (part2)
    private static void producer(){
        while (shouldProduce.get()) {
            int count = ThreadLocalRandom.current().nextInt(5);
            for (int i = 0; i < count; i++) {
                int value = ThreadLocalRandom.current().nextInt(0,100) + 1;
                collection.add(value);
                // System.out.println("Produced: " + value);
                synchronized (consumerChecker) {
                    consumerChecker.notify();
                }
            }
    
            try {
                Thread.sleep(PRODUCING_PERIOD_MS);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    // TODO Create a consumer that will periodically pick values randomly out of the list
    // TODO The consumer then will keep track of the average of the number that have been
    // TODO in the list
    // TODO The consumer stop when the list have been empty for 5 seconds
    private static void consumer(){
        while (shouldConsume.get()) {
            synchronized (collection) {
                if (!collection.isEmpty()) {
                    int randomIndex = ThreadLocalRandom.current().nextInt(collection.size());
                    collection.remove(randomIndex);
                }
            }

            try {
                Thread.sleep(CONSUME_PERIOD_MS);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
    }

    private static void consumerStopper(){

        synchronized (consumerChecker) {
            try {
                consumerChecker.wait(5000);
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }
        }
        shouldConsume.set(false);
    }
}