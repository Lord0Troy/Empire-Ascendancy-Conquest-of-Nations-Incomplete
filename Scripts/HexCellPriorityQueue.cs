using System.Collections.Generic;

public class HexCellPriorityQueue {

    // List to store HexCell objects based on their priority
    List<HexCell> list = new List<HexCell>();

    // Number of elements in the queue
    int count = 0;

    // Minimum priority value in the queue
    int minimum = int.MaxValue;

    // Property to get the count of elements in the queue
    public int Count {
        get {
            return count;
        }
    }

    // Method to add a HexCell to the queue
    public void Enqueue (HexCell cell) {
        count += 1; // Increment the count
        int priority = cell.SearchPriority; // Get the priority of the cell
        if (priority < minimum) {
            minimum = priority; // Update the minimum priority if necessary
        }
        while (priority >= list.Count) {
            list.Add(null); // Ensure the list has enough space for the priority
        }
        cell.NextWithSamePriority = list[priority]; // Link the cell with the existing cells of the same priority
        list[priority] = cell; // Add the cell to the list at its priority index
    }

    // Method to remove and return the HexCell with the highest priority (lowest value)
    public HexCell Dequeue () {
        count -= 1; // Decrement the count
        for (; minimum < list.Count; minimum++) {
            HexCell cell = list[minimum]; // Get the cell at the current minimum priority
            if (cell != null) {
                list[minimum] = cell.NextWithSamePriority; // Update the list to point to the next cell with the same priority
                return cell; // Return the dequeued cell
            }
        }
        return null; // Return null if no cell is found
    }

    // Method to change the priority of a HexCell
    public void Change (HexCell cell, int oldPriority) {
        HexCell current = list[oldPriority]; // Get the current cell at the old priority
        HexCell next = current.NextWithSamePriority; // Get the next cell with the same priority
        if (current == cell) {
            list[oldPriority] = next; // Update the list if the current cell is the one to be changed
        }
        else {
            while (next != cell) {
                current = next; // Traverse the list to find the cell
                next = current.NextWithSamePriority;
            }
            current.NextWithSamePriority = cell.NextWithSamePriority; // Update the links to remove the cell from the old priority
        }
        Enqueue(cell); // Enqueue the cell with the new priority
        count -= 1; // Adjust the count as Enqueue increments it
    }

    // Method to clear the queue
    public void Clear () {
        list.Clear(); // Clear the list
        count = 0; // Reset the count
        minimum = int.MaxValue; // Reset the minimum priority
    }
}