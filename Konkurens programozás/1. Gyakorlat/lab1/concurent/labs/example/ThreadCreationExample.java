package concurent.labs.example;

public class ThreadCreationExample {

    /**
     * Showcasing the potential ways to start a thread.
     * Leveraging methods from the {@link concurent.labs.example.ExampleHelper} class,
     * custom threads will print out "Hello" 100 times, while main thread will print out
     * "something else" 100 times
     *
     */
    public void start(){
        // Thread creation example 1
        runExtendExample();
        // Thread creation example 2
        runImplementsExample();
        // Thread creation example 3
        runAnonymClass();
        // Thread creation example 4
        runMethodReference();
        // Thread creation example 5
        runWithRunnableAsLambda();
        // Thread creation example 6
        runLamdaSingleExpression();
        // Thread creation example 7
        runLambdaMultipleExpression();

        ExampleHelper.printSomethingElsex100();
    }

    /**
     * Thread creation example 1 invocation
     */
    private void runExtendExample(){
        ClassExtendingThread thread = new ClassExtendingThread();
        thread.start();
        // potential bug here - when using run() instead of start(), the thread
        // won't be running asynchronously; run() just runs the run() just like any
        // other function, while start() starts a new async thread and has that thread run
        // the run() method
        //
        //thread.run();
    }

    /**
     * Thread creation example 2.2 - Creating a new Thread with custom Runnable class
     */
    private void runImplementsExample(){
        // Create a new thread using the constructor that requires a Runnable
        Thread thread = new Thread(new ClassImplementingRunnable());
        thread.start();
    }

    /**
     * Thread creation example 3 - Using anonymous Thread class
     *
     * Creating an anonymous class extending Thread and starting it
     *
     */
    private void runAnonymClass(){
        new Thread(){
            @Override
            public void run() {
                System.out.println(Thread.currentThread().getName() +
                        ": The following are outputs of an anonymous class");
                ExampleHelper.printHellox100();
            }
        }.start();
    }

    /**
     * Thread creation example 4 - Using method reference
     *
     * Since Runnable is a functional interface with a single method
     * that returns with void and needs 0 parameters, we can use the
     * method reference operator (class::method) with ExampleHelper's
     * printHellox100, since it needs 0 parameters and returns with void
     *
     */
    private void runMethodReference(){
        Thread thread = new Thread(ExampleHelper::printHellox100);
        thread.start();
    }

    /**
     * Thread creation example 5
     *
     * Explicitly creating a new Runnable with a single expression lambda
     */
    private void runWithRunnableAsLambda(){
        Runnable task = () -> ExampleHelper.printHellox100();
        Thread thread = new Thread(task);
        thread.start();
    }

    /**
     * Thread creation example 6
     *
     * Using single expression inline lambda to create a Runnable
     */
    private void runLamdaSingleExpression(){
        Thread thread = new Thread(() -> ExampleHelper.printHellox100());
        thread.start();
    }

    /**
     * Thread creation example 7
     *
     * Using multi expression inline lambda to create a Runnable
     */
    private void runLambdaMultipleExpression(){
        Thread thread = new Thread(() -> {
            System.out.println(Thread.currentThread().getName() +
                    ": The following are outputs of a multi expression lambda");
            ExampleHelper.printHellox100();
        });
        thread.start();
    }

}
