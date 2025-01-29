package concurent.labs.example;

/**
 * Starting a thread with 1 minute sleep.
 * Interrupting the thread after 2 seconds so the example won't take a minute to complete
 * We really want to wait a minute, so try to restart the thread,
 * but fortunately JVM won't let us do such silly things
 *
 */
public class LifecycleInterruptExample {

    private static final int SLEEP_TIME_MSEC = 60000;
    private static final int MSEC_TO_INTERRUPT = 2000;

    public void start(){
        Thread thread = new Thread(() -> {
            ExampleHelper.sleepForMsec(SLEEP_TIME_MSEC);
            System.out.println("Thread finished because we handled the exception");
        });
        thread.start();
        ExampleHelper.sleepForMsec(MSEC_TO_INTERRUPT);
        thread.interrupt();

        try {
            System.out.println("Restarting the previous thread - we will get an exception");
            thread.start();
        } catch (IllegalThreadStateException e){
            e.printStackTrace();
            System.out.println("Told you :)");
        }

    }
}
