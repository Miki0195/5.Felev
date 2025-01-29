import java.util.concurrent.ThreadLocalRandom;

// Ebben a fájlban nem kell módosítást végezned.
public class Utilities {
    public static int getRandomBetween(int min, int max) {
        return ThreadLocalRandom.current().nextInt(min, max + 1);
    }

    public static void sleep(int ms) {
        try {
            Thread.sleep(ms);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }

    public static boolean isBetween(int min, int number, int max) {
        return min <= number && number <= max;
    }
}
