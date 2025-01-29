package concurent.labs.task;

/**
 * Resources used by ThreadCraft
 *
 * Includes goldmine capacity, gold owned by the player, houses built by the player
 * All the actions manipulating these stats should be implemented here
 */
public class Resources {

    // TODO: Make the methods thread safe
    // Multiple threads will try to access these resources at the same time

    private int goldmineCapacity = Configuration.GOLDMINE_CAPACITY;
    private int gold = 0;
    private int houses = 0;

    /**
     * If the goldmine hasn't run out yet, mines some gold
     * and adds it to the gold resource
     * @return Whether mining has been successful or not
     */
    public boolean tryToMineGold(){
        if (goldmineCapacity > 0) {
            this.goldmineCapacity -= Configuration.MINING_AMOUNT;
            this.gold += Configuration.MINING_AMOUNT;
            return true;
        }
        return false;
    }

    /**
     * Returns number of houses built
     * @return
     */
    public int getHouses() {
        return houses;
    }

    /**
     * If there is enough gold to build a house, it does
     * Increments number of houses, removes the cost from gold
     * @return Whether building was successful or not
     */
    public boolean tryToBuildHouse() {
        if (this.gold >= Configuration.HOUSE_COST) {
            this.houses++;
            this.gold -= Configuration.HOUSE_COST;
            return true;
        }
        return false;
    }

    /**
     * Returns gold owned by the player
     * @return
     */
    public int getGold(){
        return this.gold;
    }

    /**
     * Return how much gold can be mined from the mine
     * @return
     */
    public int getGoldmineCapacity(){
        return this.goldmineCapacity;
    }

}
