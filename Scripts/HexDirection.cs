// Enum representing the six possible directions in a hexagonal grid.
public enum HexDirection {
    NE, // North-East
    E,  // East
    SE, // South-East
    SW, // South-West
    W,  // West
    NW  // North-West
}

// Static class containing extension methods for the HexDirection enum.
public static class HexDirectionExtensions {

    // Returns the opposite direction of the given HexDirection.
    public static HexDirection Opposite (this HexDirection direction) {
        // If the direction is in the first half (NE, E, SE), add 3 to get the opposite.
        // Otherwise, subtract 3 to get the opposite.
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    // Returns the previous direction in the clockwise order.
    public static HexDirection Previous (this HexDirection direction) {
        // If the direction is NE, wrap around to NW.
        // Otherwise, subtract 1 to get the previous direction.
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    // Returns the next direction in the clockwise order.
    public static HexDirection Next (this HexDirection direction) {
        // If the direction is NW, wrap around to NE.
        // Otherwise, add 1 to get the next direction.
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    // Returns the direction two steps counterclockwise.
    public static HexDirection Previous2 (this HexDirection direction) {
        // Subtract 2 from the current direction.
        // If the result is less than NE, wrap around by adding 6.
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    // Returns the direction two steps clockwise.
    public static HexDirection Next2 (this HexDirection direction) {
        // Add 2 to the current direction.
        // If the result is greater than NW, wrap around by subtracting 6.
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}