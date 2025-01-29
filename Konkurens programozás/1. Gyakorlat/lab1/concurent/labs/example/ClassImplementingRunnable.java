package concurent.labs.example;

/**
 * Thread creation example 2.1 - Implementing Runnable interface
 *
 * Override the run() method to implement custom logic.
 *
 */
public class ClassImplementingRunnable implements Runnable {

    @Override
    public void run() {
        System.out.println(Thread.currentThread().getName() +
                ": The following are outputs of the Thread class using a custom class that implements Runnable");
        ExampleHelper.printHellox100();
    }
}
