import java.util.List;

public class Tenant extends Thread {
    private static int counter = 1;

    public final int id;
    private final Agency agency;
    private boolean isSearching = true;
    
    public Tenant(Agency agency) {
        this.id = counter++;
        this.agency = agency;
    }

    @Override
    public void run() {
        /* TODO Amíg a bérlő keres és az iroda is nyitva van (`isOpen`) */
        while (isSearching && agency.isOpen.get()) {
            int minSize = Utilities.getRandomBetween(30, 50);
            int maxSize = minSize + 10;
            int minRent = Utilities.getRandomBetween(150, 230) * 1000;
            int maxRent = minRent + 20000;
            
            think();
            
            List<Apartment> searchResults = agency.search(minSize, maxSize, minRent, maxRent);
            // A mintának megfelelően írd ki a keresés eredményét.
            // TODO
            synchronized (System.out) {
                System.out.println("Tenant " + id + " searched for apartments (Size: " + minSize + "-" + maxSize +
                        ", Rent: " + minRent + "-" + maxRent + "). Results: " + searchResults);
            }

            think();

            // Generáljunk egy random számot a [-1, |searchResults|-1] intervallumon.
            // Ha ez a generált szám -1, akkor a continue-val folytassuk a bérlő működését.
            // Ha ez a szám nem -1, akkor pedig tekintsünk rá indexként, 
            // és foglaljuk le az index-edik lakást, valamint az isSearching-et állítsuk `false`-ra.
            // TODO
            int randomIndex = Utilities.getRandomBetween(-1, searchResults.size() - 1);

            if (randomIndex == -1) {
                continue; 
            }

            Apartment selectedApartment = searchResults.get(randomIndex);
            if (agency.reserveApartment(this, selectedApartment)) {
                isSearching = false; 
                synchronized (System.out) {
                    System.out.println("Tenant " + id + " successfully reserved apartment: " + selectedApartment);
                }
            } else {
                synchronized (System.out) {
                    System.out.println("Tenant " + id + " failed to reserve apartment: " + selectedApartment);
                }
            }
        }
    }

    private void think() {
        int ms = Utilities.getRandomBetween(3000, 5000);
        Utilities.sleep(ms);
    }
}
