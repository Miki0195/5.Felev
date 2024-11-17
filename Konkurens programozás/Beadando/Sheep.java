import java.util.Random;

public class Sheep {
    private final String id;
    private int posX;
    private int posY;
    private final Random rand = new Random();

    private enum Direction {
        UP, DOWN, LEFT, RIGHT;

        public static Direction[] getAllDirections() {
            return values();
        }
    }

    public Sheep(String id, int posX, int posY) {
        this.id = id;
        this.posX = posX;
        this.posY = posY;
    }

    private Integer[] calculateNewPosition(Direction direction) {
        int newY = posY;
        int newX = posX;

        switch (direction) {
            case UP -> newY -= 1;
            case DOWN -> newY += 1;
            case LEFT -> newX -= 1;
            case RIGHT -> newX += 1;
        }

        return new Integer[]{newY, newX};
    }

    private boolean isDogNearby(int checkY, int checkX) {
        if (checkY < 0 || checkY >= Farm.Height || checkX < 0 || checkX >= Farm.Width) {
            return false;
        }
        return Farm.Map[checkY][checkX] instanceof Dog;
    }

    private Direction determineBestDirection() {
        for (Direction dir : Direction.getAllDirections()) {
            Integer[] newPos = calculateNewPosition(dir);
            int checkY = newPos[0];
            int checkX = newPos[1];

            if (checkY >= 0 && checkY < Farm.Height && checkX >= 0 && checkX < Farm.Width && isDogNearby(checkY, checkX)) {
                switch (dir) {
                    case UP:
                        return Direction.DOWN;
                    case DOWN:
                        return Direction.UP;
                    case LEFT:
                        return Direction.RIGHT;
                    case RIGHT:
                        return Direction.LEFT;
                }
            }
        }
        return Direction.getAllDirections()[rand.nextInt(Direction.getAllDirections().length)];
    }

    public void move() {
        Direction direction = determineBestDirection();
        Integer[] newPos = calculateNewPosition(direction);
        int newY = newPos[0];
        int newX = newPos[1];

        if (newY < 0 || newY >= Farm.Height || newX < 0 || newX >= Farm.Width) {
            return; 
        }

        boolean moved = false;
        boolean escaped = false;

        synchronized (Farm.positionLocks[newY][newX]) {
            if (Farm.Map[newY][newX] instanceof Gate) {
                escaped = true;
            }
            if (escaped || Farm.Map[newY][newX] instanceof Empty) {
                Farm.Map[newY][newX] = this;
                moved = true;
            }
        }

        if (moved) {
            synchronized (Farm.positionLocks[posY][posX]) {
                Farm.Map[posY][posX] = new Empty();
            }
            posY = newY;
            posX = newX;
        }

        if (escaped) {
            Farm.sheepEscaped(this);
        }
    }

    @Override
    public String toString() {
        return id;
    }
}
