// Ebben a fájlban nem kell módosítást végezned.
public class Apartment implements Comparable<Apartment> {
    private static int counter = 1;

    public final int id;
    public final int size;
    public final int rent;

    public Apartment(int size, int rent) {
        this.id = counter++;
        this.size = size;
        this.rent = rent;
    }

    @Override
    public int compareTo(Apartment other) {
        return Integer.compare(id, other.id);
    }

    @Override
    public String toString() {
        return id + " - " + size + " m2 - " + rent;
    }
}
