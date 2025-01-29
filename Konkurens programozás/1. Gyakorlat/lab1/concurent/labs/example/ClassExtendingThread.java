package concurent.labs.example;

/**
 * Thread creation example 1 - Extending the Thread class
 *
 * Override the run() method to implement custom logic.
 *
 */
public class ClassExtendingThread extends Thread{

    @Override
    public void run() {
        System.out.println(Thread.currentThread().getName() +
                ": The following are outputs of a class extending the Thread class");
        ExampleHelper.printHellox100();
    }
}
