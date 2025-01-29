import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.atomic.*;
/**
 * A bunozoket reprezentalja, akiket ki fognak hallgatni a detektivek
 * informacioert a feletteseikrol.
 * Vagy nem mondanak semmit, vagy keveset.
 * Meg van egy egyedi azonositojuk is.
 */
public class Perpetrator {
    private static final int MINIMUM_INFORMATION_PERCENT = 0;
    private static final int MAXIMUM_INFORMATION_PERCENT = 3;
    private static final AtomicInteger idGenerator = new AtomicInteger(0);
    private final int id;

    public Perpetrator() {
        // TODO 1. Resz: Legyen egyedi azonositojuk, 0-tol indulva, egyessevel novekedve
        this.id = idGenerator.getAndIncrement();
    }

    /**
     * Jelzi, hogy milyen hasznos infot adott a bunozo
     * @return Az egesz info valahany szazaleka
     */
    public int interrogationResult() {
        // TODO 1. Resz: Adjunk vissza $MINIMUM_INFORMATION_PERCENT es $MAXIMUM_INFORMATION_PERCENT kozott egy random erteket
        return ThreadLocalRandom.current().nextInt(MINIMUM_INFORMATION_PERCENT, MAXIMUM_INFORMATION_PERCENT);
        //return ThreadLocalRandom.current().nextInt(MINIMUM_INFORMATION_PERCENT, MAXIMUM_INFORMATION_PERCENT + 1);
    }

    @Override
    public String toString() {
        return "Perpetrator #" + this.id;
    }
}
