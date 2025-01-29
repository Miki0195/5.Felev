public class Agent extends Thread {
    private final Agency agency;
    
    public Agent(Agency agency) {
        this.agency = agency;
    }
    
    @Override
    public void run() {
        // Amíg az iroda nyitva van (`isOpen`):
        //   - generálj egy random számot 30 és 60 között (`size`),
        //   - generálj egy random számot 150 és 250 között, majd ezt szorozd meg 1000-rel (`rent`),
        //   - ezekkel hozz létre egy új Apartment objektumot,
        //   - az ingatlanos `gondolkodjon` (`think`),
        //   - az új lakás legyen beregisztrálva az irodához.
        // TODO
        while (agency.isOpen.get()) { 
            int size = Utilities.getRandomBetween(30, 60);
            int rent = Utilities.getRandomBetween(150, 250) * 1000;

            Apartment apartment = new Apartment(size, rent);

            think();

            if (agency.registerApartment(apartment)) {
                System.out.println("Agent registered a new apartment: " + apartment);
            } else {
                System.out.println("Agent failed to register the apartment: " + apartment);
            }
        }
    }

    private void think() {
        int ms = Utilities.getRandomBetween(1000, 3000);
        Utilities.sleep(ms);
    }
}
