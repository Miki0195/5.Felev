public class SpacecraftData {
    private boolean isCrewAlive = true;
    
    public synchronized boolean isCrewAlive(){
        return isCrewAlive;
    }

    public synchronized void setCrewDead(){
        isCrewAlive = false;
    }
}
