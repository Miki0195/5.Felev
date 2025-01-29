package concurent.labs.example;

/**
 * Hub for all the examples for lab 1
 */
public class ExampleHub {

    /**
     * Running the examples
     *
     * @param args
     */
    public static void main(String[] args) {

        // thread creation example
        runThreadCreationExample();

        // join, sleep, synchronized example
        runJoinSleepSynchronizedExample();

        // the logs of the first 2 are likely to get mixed up,
        // so its recommended to comment out one of them

        // sleep, interrupt, try to restart thread
        runLifecycleInterruptExample();
    }

    /**
     * Creating threads using
     * 1, Class extending Thread
     * 2, Class implementing Runnable
     * 3, Using anonymous class
     * 4, Using method reference
     * 5, Single expression lambda as Runnable
     * 6, Single expression inline lambda
     * 7, Multi expression inline lambda
     */
    private static void runThreadCreationExample(){
        ThreadCreationExample tce = new ThreadCreationExample();
        tce.start();
    }

    /**
     * Starting the example for
     * 1, Joining threads
     * 2, Sleeping used in threads
     * 3, Synchronized methods used by threads
     *
     */
    private static void runJoinSleepSynchronizedExample(){
        JoinSleepSynchronizedExample jsse = new JoinSleepSynchronizedExample();
        jsse.start();
    }

    /**
     * Starting example for
     * 1, Creating a thread with sleep of 1 minute
     * 2, Interrupting the sleep, but catching the exception
     * 3, Thread finishes early because we caught the exception
     * 4, Trying to restart the thread after all this
     */
    private static void runLifecycleInterruptExample(){
        LifecycleInterruptExample lie = new LifecycleInterruptExample();
        lie.start();
    }

}
