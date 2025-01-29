import java.util.ArrayList;
import java.util.List;

public class Main {
    public final static int NumberOfAgents = 10;
    public final static int NumberOfTenants = 10;

    public static void main(String[] args) throws InterruptedException {
        Agency agency = new Agency();

        List<Agent> agents = new ArrayList<>();
        for (int i = 0; i < NumberOfAgents; i++) {
            // Készíts el egy ingatlanost,
            // add hozzá az `agents` listához,
            // indítsd el a működését
            // TODO
            Agent agent = new Agent(agency);
            agents.add(agent);
            agent.start();
        }

        List<Tenant> tenants = new ArrayList<>();
        for (int i = 0; i < NumberOfTenants; i++) {
            // 3. feladathoz:
            // Készíts el egy bérlőt,
            // add hozzá az `tenants` listához,
            // indítsd el a működését
            // TODO
            Tenant tenant = new Tenant(agency);
            tenants.add(tenant);
            tenant.start(); 
        }

        // Az első feladatokhoz:
        //   - 10 másodpercig altasd ezt a szálat,
        //   - majd `zárd be` az ingatlanirodát
        // TODO
        Thread.sleep(10000);
        agency.isOpen.set(false);
        // System.out.println("The agency is now closed.");

        // Várjuk be az összes ingatlanos működésének befejeződését
        // TODO
        for (Agent agent : agents) {
            agent.join();
        }
        // 3. feladathoz: várjuk be az összes bérlő működésének befejeződését
        // TODO
        for (Tenant tenant : tenants) {
            tenant.join();
        }

        // 2. feladathoz: várjuk be az iroda naplózójának (`agency.logger`) működésének befejeződését
        // TODO
        agency.logger.join();

        // Írjuk ki a szimuláció befejeződésének tényét.
        // TODO
        System.out.println("The simulation has ended.");
    }
}
