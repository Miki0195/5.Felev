package concurent.labs.task;

/**
 * Configurations for ThreadCraft
 */
public class Configuration {

    // NO NEED TO CHANGE

    // time related configs
    public static final int LOGGING_FREQUENCY = 1000; // msec
    public static final int MINING_FREQUENCY = 500; // msec
    public static final int BUILD_TIME = 2500; // msec
    public static final int SLEEP_TIME = 200; // msec

    // "gameplay" related configs
    public static final int GOLDMINE_CAPACITY = 1000;
    public static final int NUMBER_OF_MINERS = 3;
    public static final int NUMBER_OF_BUILDERS = 2;

    public static final int HOUSE_COST = 200;
    public static final int MINING_AMOUNT = 20;
    public static final int HOUSE_LIMIT = GOLDMINE_CAPACITY/HOUSE_COST;

}
