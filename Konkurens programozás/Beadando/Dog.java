import java.util.Random;

public class Dog {
    private final int id;
    private int posX;
    private int posY;

    private enum Direction {
        UP, DOWN, LEFT, RIGHT;

        public static Direction getRandomDirection() {
            return values()[new Random().nextInt(values().length)];
        }
    }

    public Dog(int id, int posX, int posY) {
        this.id = id;
        this.posX = posX;
        this.posY = posY;
    }
    private boolean isInTheMiddle(int y, int x) {
        int thirdHeight = (Farm.Height - 2) / 3;
        int thirdWidth = (Farm.Width - 2) / 3;
        return (y > thirdHeight && y <= 2 * thirdHeight) || (x > thirdWidth && x <= 2 * thirdWidth);
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

    private boolean isValidMove(int newY, int newX) {
        return newY >= 0 && newY < Farm.Height &&
               newX >= 0 && newX < Farm.Width &&
               !isInTheMiddle(newY, newX);
    }

    public void move() {
        Direction direction;
        Integer[] newPosition;

        do {
            direction = Direction.getRandomDirection();
            newPosition = calculateNewPosition(direction);
        } while (!isValidMove(newPosition[0], newPosition[1]));

        int newY = newPosition[0];
        int newX = newPosition[1];

        boolean moved = false;
        synchronized (Farm.positionLocks[newY][newX]) {
            if (Farm.Map[newY][newX] instanceof Empty || Farm.Map[newY][newX] instanceof Gate) {
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
    }

    @Override
    public String toString() {
        return Integer.toString(id);
    }
}
