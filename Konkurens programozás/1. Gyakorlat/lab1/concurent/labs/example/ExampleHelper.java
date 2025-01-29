package concurent.labs.example;

/***
 * This class is intended to contain the frequently
 * used methods by all the example classes to reduce
 * code duplication.
 */
public class ExampleHelper {

    private static final String HELLO = "Hello ";
    private static final String SOMETHING_ELSE = "Something else ";

    /**
     * Sleeping idle on the given thread for a couple of milliseconds.
     * If something interrupts this sleep, log the exception
     * @param msec
     */
    public static void sleepForMsec(int msec){
        try {
            Thread.sleep(msec);
        } catch (InterruptedException e) {
            System.out.println(Thread.currentThread().getName() + " got interrupted");
            e.printStackTrace();
        }
    }

    /**
     * Prints out the name of the thread running this method and
     * "Hello" 100 times
     */
    public static void printHellox100(){
        printMessagex100(HELLO);
    }

    /**
     * Prints out the name of the thread running this method and
     * "Something else" 100 times
     */
    public static void printSomethingElsex100(){
        printMessagex100(SOMETHING_ELSE);
    }

    /**
     * Prints out the name of the thread running this method and
     * a message 100 times
     *
     * @param message
     */
    private static void printMessagex100(String message){
        for (int i = 0; i < 100; i++){
            System.out.println(Thread.currentThread().getName() + ": " +message + i);
        }
    }

}
