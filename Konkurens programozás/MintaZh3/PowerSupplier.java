public class PowerSupplier{
    private int power;
    public PowerSupplier(int power){
        this.power = power;
    }   

    public synchronized boolean consumePower(int amount){
        if(power >= amount){
            power -= amount;
            return true;
        }
        return false;
    }

    public synchronized void addPower(int amount){
        power += amount;
    }
}