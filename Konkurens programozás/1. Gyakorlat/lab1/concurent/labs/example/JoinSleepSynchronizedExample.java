package concurent.labs.example;

import java.util.ArrayList;
import java.util.List;

/**
 * Starting a number of threads that will each wait for a certain amount of time.
 * Creating a monitoring thread to monitor the progress of those threads.
 * The threads will update asynchronously a counter which will be monitored
 * by the monitoring thread to determine the progress of them.
 *
 * The monitoring thread could be omitted if we wait for the other threads to finish
 * as detailed in a comment in the start() method.
 *
 */
public class JoinSleepSynchronizedExample {

    private static final int MSEC_INCREMENT = 500;
    private static final int NUMBER_OF_THREADS = 5;
    private static final int MONITORING_INTERVAL = 100;

    private int threadCompletionCounter = 0;

    public void start() {

        // Container for threads so that we later can access them
        List<Thread> threads = new ArrayList<>();

        // Starting threads that do nothing but sleep for some time
        for(int i = 0; i < NUMBER_OF_THREADS; ++i){
            int multiplier = i + 1;
            Thread thread = new Thread(() -> sleepAction((multiplier) * MSEC_INCREMENT));
            thread.start();
        }

        Thread monitoringThread = new Thread(() -> monitorAction());
        monitoringThread.start();

        // Waiting for the threads to finish
        for(Thread thread : threads){
            try {
                thread.join();
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }

        // With waiting for all the other threads
        // we don't actually need the monitoring thread
        // since the program won't be able to progress
        // past their join part without them finishing


        // Waiting for monitoring thread to finish
        try {
            monitoringThread.join();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        System.out.println("All threads have finished working");

    }

    private void sleepAction(int msec){
        System.out.println(Thread.currentThread().getName() + " is sleeping for " + msec + " milliseconds");
        ExampleHelper.sleepForMsec(msec);
        incrementCounter();
    }

    private void monitorAction(){
        int counter = getCounter();
        while(counter != NUMBER_OF_THREADS){
            System.out.println("Threads finished: " + counter + "/" + NUMBER_OF_THREADS);
            ExampleHelper.sleepForMsec(MONITORING_INTERVAL);
            counter = getCounter();
        }
    }

    private synchronized void incrementCounter(){
        // the synchronized keywords ensures that only one thread can access this
        // method at a time thus making threadCompletionCounter threadsafe
        threadCompletionCounter++;
    }

    private synchronized int getCounter(){
        // the synchronized keywords ensures that only one thread can access this
        // method at a time thus making threadCompletionCounter threadsafe
        return threadCompletionCounter;
    }

}
